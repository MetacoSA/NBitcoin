#if HAS_SPAN
using System;
using System.IO;
using System.Linq;
using NBitcoin.BIP352;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Xunit;

namespace NBitcoin.Tests;

public class SilentPaymentTests
{
	[Theory]
	[MemberData(nameof(SilentPaymentTestVector.TestCasesData), MemberType = typeof(SilentPaymentTestVector))]
	public void TestVectors(SilentPaymentTestVector test)
	{
		// Sending functionality
		foreach (var sending in test.Sending)
		{
			var given = sending.Given;
			var expected = sending.Expected;
			try
			{
				var utxos = given.Vin.Select(x => new Utxo(
					new OutPoint(uint256.Parse(x.TxId), x.Vout), new Key(Encoders.Hex.DecodeData(x.Private_Key)),
					Script.FromHex(x.PrevOut.ScriptPubKey.Hex))).ToArray();
				var recipients = given.Recipients.Select(x => SilentPaymentAddress.Parse(x, Network.Main));
				var xonlyPks = SilentPayment.GetPubKeys(recipients, utxos);
				var actual = xonlyPks.SelectMany(x => x.Value).Select(x => Encoders.Hex.EncodeData(x.ToBytes()));

				Assert.Subset(expected.Outputs.SelectMany(x => x).ToHashSet(), actual.ToHashSet());
			}
			catch (ArgumentException e) when(e.Message.Contains("Invalid ec private key") && test.Comment.Contains("point at infinity"))
			{
				// ignore because it is expected to fail;
			}
		}

		// Receiving functionality

		// message and auxiliary data used in signature
		// see: https://github.com/bitcoinops/taproot-workshop/blob/master/1.1-schnorr-signatures.ipynb
		var msg = Crypto.Hashes.SHA256(Encoders.ASCII.DecodeData("message"));
		var aux = Crypto.Hashes.SHA256(Encoders.ASCII.DecodeData("random auxiliary data"));

		foreach (var receiving in test.Receiving)
		{
			var given = receiving.Given;
			var expected = receiving.Expected;
			try
			{
				var prevOuts = given.Vin.Select(x => OutPoint.Parse(x.TxId + "-" + x.Vout)).ToArray();
				var pubKeys = given.Vin.Select(ExtractPubKey).DropNulls().ToArray();
				if (!pubKeys.Any())
				{
					continue; // if there are no pubkeys then nothing can be done
				}

				// Parse key material (scan and spend keys)
				using var scanKey = ParsePrivKey(given.Key_Material.scan_priv_key);
				using var spendKey = ParsePrivKey(given.Key_Material.spend_priv_key);

				// Addresses
				var baseAddress = new SilentPaymentAddress(0, scanKey.PubKey, spendKey.PubKey);

				// Creates a lookup table Dic<SilentPaymentAddress, (ECPrivKey labelSecret, ECPubKey labelPubKey)>
				var addressesTable = given.Labels
					.Select(label => SilentPayment.CreateLabel(scanKey, (uint) label))
					.Select(labelSecret => new LabelInfo.Full(labelSecret, labelSecret.PubKey))
					.Select(labelInfo => (LabelInfo: (LabelInfo)labelInfo, Address: baseAddress.DeriveAddressForLabel(labelInfo.PubKey))) // each label has a different address
					.Prepend((LabelInfo: new LabelInfo.None(), baseAddress))
					.ToDictionary(x => x.Address, x => x.LabelInfo);
				var addresses = addressesTable.Keys.ToArray();
				var expectedAddresses = expected.Addresses.Select(x => SilentPaymentAddress.Parse(x, Network.Main));
				Assert.Equal(expectedAddresses, addresses);

				var sharedSecret = SilentPayment.ComputeSharedSecretReceiver(prevOuts, pubKeys, scanKey);

				// Outputs
				var givenOutputPubKeys = given.Outputs.Select(ParseXOnlyPubKey).ToArray();
				var detectedOutputPubKeys = SilentPayment.GetPubKeys(addresses.ToArray(), sharedSecret, givenOutputPubKeys);
				var detectedOutputs = detectedOutputPubKeys.Select(x => Encoders.Hex.EncodeData(x.PubKey.ToBytes())).ToArray();
				var expectedOutputs = expected.Outputs.Select(x => x.pub_key).ToArray();

				Assert.Equal(detectedOutputs.ToHashSet(), expectedOutputs.ToHashSet());

				// Tweak Key

				// Enrich the detected output xonlypubkeys with corresponding label info (secret and public key)
				var detectedXonlyWithLabelInfo = detectedOutputPubKeys
					.Select(x => (x.Address, x.PubKey, LabelInfo: addressesTable[x.Address]))
					.ToArray();

				// Compute tweakKey for each detected output
				var tweakKeys = detectedXonlyWithLabelInfo
					.Select((pk, k) => {
						var tk = SilentPayment.TweakKey(sharedSecret, (uint) k);
						return pk.LabelInfo switch
						{
							LabelInfo.None => (pk.PubKey, TweakKey: tk),
							LabelInfo.Full info => (pk.PubKey, TweakKey: new Key(tk._ECKey.TweakAdd(info.Secret.ToBytes()).sec.ToBytes())),
							_ => throw new ArgumentException("Unknown label type")
						};
					})
					.ToArray();

				var detectedTweakKeys = tweakKeys.Select(x => Encoders.Hex.EncodeData(x.TweakKey.ToBytes()));
				var expectedTweakKeys = expected.Outputs.Select(o => o.priv_key_tweak);

				Assert.Equal(expectedTweakKeys.ToHashSet(), detectedTweakKeys.ToHashSet());

				// Signature
				var expectedSignature = expected.Outputs.Select(o => o.signature).ToHashSet();
				var tweakKeyMap = tweakKeys.ToDictionary(x => x.PubKey, x => x.TweakKey);
				var computedSignatures = detectedOutputPubKeys
					.Select(x => (x.Address, x.PubKey, TweakKey: tweakKeyMap[x.PubKey]))
					.Select(x => SilentPayment.ComputePrivKey(spendKey, x.TweakKey))
					.Select(x => x._ECKey.SignBIP340(msg, aux))
					.Select(x => Encoders.Hex.EncodeData(x.ToBytes()))
					.ToArray();

				Assert.Equal(expectedSignature.ToHashSet(), computedSignatures.ToHashSet());
			}
			catch (InvalidOperationException e) when(e.Message.Contains("infinite") && test.Comment.Contains("point at infinity"))
			{
				// ignore because it is expected to fail;
			}
		}

		PubKey ParseXOnlyPubKey(string pk) =>
			new (Encoders.Hex.DecodeData(pk));

		Key ParsePrivKey(string pk) =>
			new (Encoders.Hex.DecodeData(pk));
	}


	private PubKey? ExtractPubKey(ReceivingVin vin)
	{
		var spk = Script.FromHex(vin.PrevOut.ScriptPubKey.Hex);
		var scriptSig = Script.FromHex(vin.ScriptSig);
		var txInWitness = string.IsNullOrEmpty(vin.TxInWitness) ? null :  new WitScript (Encoders.Hex.DecodeData(vin.TxInWitness));
		return SilentPayment.ExtractPubKey(scriptSig, txInWitness, spk);
	}
}

public class ScriptPubKey
{
	public ScriptPubKey(string Hex)
	{
		this.Hex = Hex;
	}

	public string Hex { get;  }
}

public class Output
{
	public Output(ScriptPubKey ScriptPubKey)
	{
		this.ScriptPubKey = ScriptPubKey;
	}

	public ScriptPubKey ScriptPubKey { get;  }
}

public class ReceivingExpectedOutput
{
	public ReceivingExpectedOutput(string PrivKeyTweak, string PubKey, string Signature)
	{
		priv_key_tweak = PrivKeyTweak;
		pub_key = PubKey;
		signature = Signature;
	}

	public string priv_key_tweak { get;  }
	public string pub_key { get;  }
	public string signature { get;  }
}

public class ReceivingVin
{
	public ReceivingVin(string TxId, int Vout, Output PrevOut, string? ScriptSig, string? TxInWitness)
	{
		this.TxId = TxId;
		this.Vout = Vout;
		this.PrevOut = PrevOut;
		this.ScriptSig = ScriptSig;
		this.TxInWitness = TxInWitness;
	}

	public string TxId { get;  }
	public int Vout { get;  }
	public Output PrevOut { get;  }
	public string ScriptSig { get;  }
	public string TxInWitness { get;  }
}

public class SendingVin
{
	public SendingVin(string TxId, int Vout, string PrivateKey, Output PrevOut)
	{
		this.TxId = TxId;
		this.Vout = Vout;
		Private_Key = PrivateKey;
		this.PrevOut = PrevOut;
	}

	public string TxId { get;  }
	public int Vout { get;  }
	public string Private_Key { get;  }
	public Output PrevOut { get;  }
}

public class SendingGiven
{
	public SendingGiven(SendingVin[] Vin, string[] Recipients)
	{
		this.Vin = Vin;
		this.Recipients = Recipients;
	}

	public SendingVin[] Vin { get;  }
	public string[] Recipients { get;  }
}

public class KeyMaterial
{
	public KeyMaterial(string SpendPrivKey, string ScanPrivKey)
	{
		spend_priv_key = SpendPrivKey;
		scan_priv_key = ScanPrivKey;
	}

	public string spend_priv_key { get;  }
	public string scan_priv_key { get;  }
}

public class ReceivingGiven
{
	public ReceivingGiven(ReceivingVin[] Vin, string[] Outputs, KeyMaterial KeyMaterial, int[] Labels)
	{
		this.Vin = Vin;
		this.Outputs = Outputs;
		Key_Material = KeyMaterial;
		this.Labels = Labels;
	}

	public ReceivingVin[] Vin { get;  }
	public string[] Outputs { get;  }
	public KeyMaterial Key_Material { get;  }
	public int[] Labels { get;  }
}

public class SendingExpected
{
	public SendingExpected(string[][] Outputs)
	{
		this.Outputs = Outputs;
	}

	public string[][] Outputs { get;  }
}

public class ReceivingExpected
{
	public ReceivingExpected(string[] Addresses, ReceivingExpectedOutput[] Outputs)
	{
		this.Addresses = Addresses;
		this.Outputs = Outputs;
	}

	public string[] Addresses { get;  }
	public ReceivingExpectedOutput[] Outputs { get;  }
}

public class Sending
{
	public Sending(SendingGiven Given, SendingExpected Expected)
	{
		this.Given = Given;
		this.Expected = Expected;
	}

	public SendingGiven Given { get;  }
	public SendingExpected Expected { get;  }
}

public class Receiving
{
	public Receiving(ReceivingGiven Given, ReceivingExpected Expected)
	{
		this.Given = Given;
		this.Expected = Expected;
	}

	public ReceivingGiven Given { get;  }
	public ReceivingExpected Expected { get;  }
}

public class SilentPaymentTestVector
{
	public string Comment { get; }
	public Sending[] Sending { get; }
	public Receiving[] Receiving { get; }

	public  SilentPaymentTestVector(string comment, Sending[] Sending, Receiving[] Receiving)
	{
		Comment = comment;
		this.Sending = Sending;
		this.Receiving = Receiving;
	}

	private static SilentPaymentTestVector[] VectorsData() =>
		JsonConvert.DeserializeObject<SilentPaymentTestVector[]>(
			File.ReadAllText("./data/SilentPaymentTestVectors.json"))!;

	private static readonly SilentPaymentTestVector[] TestCases = VectorsData().ToArray();

	public static object[][] TestCasesData =>
		TestCases.Select(testCase => new object[] { testCase }).ToArray();

	public override string ToString() => Comment;
}

public abstract class LabelInfo
{
	public class Full : LabelInfo
	{
		public Key Secret { get; }
		public PubKey PubKey { get; }

		public Full(Key Secret, PubKey PubKey)
		{
			this.Secret = Secret;
			this.PubKey = PubKey;
		}
	}

	public class None : LabelInfo;
}
#endif
