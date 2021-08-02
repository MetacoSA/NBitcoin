using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace NBitcoin.Tests
{
	public class TaprootAddressTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseTaprootAddress()
		{
			var a = BitcoinAddress.Create("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", Network.Main);
			var address = Assert.IsType<TaprootAddress>(a);
			Assert.Equal("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", address.ToString());
			var addressOriginal = (TaprootAddress)address;
			a = address.ScriptPubKey.GetDestinationAddress(Network.Main);
			address = Assert.IsType<TaprootAddress>(a);
			Assert.Equal("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", address.ToString());
			Assert.Equal(address, addressOriginal);
			Assert.Equal(address.PubKey, addressOriginal.PubKey);
			Assert.True(address.PubKey == addressOriginal.PubKey);
			Assert.True(address.PubKey.GetHashCode() == addressOriginal.PubKey.GetHashCode());

			var address2 = new TaprootPubKey(new byte[32] {
		0x58, 0x84, 0xb3, 0xa2, 0x4b, 0x97, 0x37, 0x88, 0x92, 0x38, 0xa6, 0x26, 0x62, 0x52, 0x35, 0x11,
		0xd0, 0x9a, 0xa1, 0x1b, 0x80, 0x0b, 0x5e, 0x93, 0x80, 0x26, 0x11, 0xef, 0x67, 0x4b, 0xd9, 0x23
	}).GetAddress(Network.Main);
			Assert.NotEqual(address2, address);
			Assert.NotEqual(address2.PubKey, address.PubKey);
			Assert.False(address2.PubKey == address.PubKey);
			Assert.True(address2.PubKey != address.PubKey);
			Assert.False(address2.PubKey.GetHashCode() == address.PubKey.GetHashCode());
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseTaprootSignature()
		{
			void AssertRoundtrip(string sig)
			{
				var parsed = TaprootSignature.Parse(sig);
				Assert.Equal(sig, parsed.ToString());
#if HAS_SPAN
				parsed = TaprootSignature.Parse(Encoders.Hex.DecodeData(sig).AsSpan());
				Assert.Equal(sig, parsed.ToString());
#endif
			}
			var schnorrsig = "23152e7c682ef0a805574d8a9d91d9f9bdbbdbdfea9de2496ac87652d367432784f266ec23cb6eacbc5b890bf5f95941dd0ba1e537a8ffa42d05316acfeef4f3";
			AssertRoundtrip(schnorrsig + "01");
			AssertRoundtrip(schnorrsig);
			Assert.Throws<FormatException>(() => AssertRoundtrip(schnorrsig + "00"));
			Assert.Throws<FormatException>(() => AssertRoundtrip(schnorrsig.Substring(0, 63)));
			Assert.Throws<FormatException>(() => AssertRoundtrip(schnorrsig.Substring(0, 62)));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanExtractTaprootScript()
		{
			var a = BitcoinAddress.Create("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", Network.Main);
			var res = PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(a.ScriptPubKey);
			Assert.Equal(OpcodeType.OP_1, res.Version);
			Assert.Equal(a, TaprootAddress.Create("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", Network.Main));
			Assert.Throws<FormatException>(() => TaprootAddress.Create("bc1qaf7uc0w48q5mqfp3qxy7azjc6dhr6f03tflwcx", Network.Main));
		}


#if HAS_SPAN
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSignUsingTaproot()
		{
			using (var nodeBuilder = NodeBuilderEx.Create())
			{
				var rpc = nodeBuilder.CreateNode().CreateRPCClient();
				nodeBuilder.StartAll();
				rpc.Generate(102);

				var key = new Key();
				var addr = key.PubKey.GetTaprootFullPubKey().GetAddress(nodeBuilder.Network);

				foreach (var anyoneCanPay in new[] { false, true })
				{
					rpc.Generate(1);
					foreach (var hashType in new[] { TaprootSigHash.All, TaprootSigHash.Default, TaprootSigHash.None, TaprootSigHash.Single })
					{
						if (hashType == TaprootSigHash.Default && anyoneCanPay)
							continue; // Not supported by btc
						var txid = rpc.SendToAddress(addr, Money.Coins(1.0m));

						var tx = rpc.GetRawTransaction(txid);
						var spentOutput = tx.Outputs.AsIndexedOutputs().First(o => o.TxOut.ScriptPubKey == addr.ScriptPubKey);

						var spender = nodeBuilder.Network.CreateTransaction();
						spender.Inputs.Add(new OutPoint(tx, spentOutput.N));

						var dest = rpc.GetNewAddress();
						spender.Outputs.Add(Money.Coins(0.7m), dest);
						spender.Outputs.Add(Money.Coins(0.2999000m), addr);

						var sighash = hashType | (anyoneCanPay ? TaprootSigHash.AnyoneCanPay : 0);
						var hash = spender.GetSignatureHashTaproot(new[] { spentOutput.TxOut },
																 new TaprootExecutionData(0) { SigHash = sighash });
						var sig = key.SignTaprootKeySpend(hash, sighash);

						Assert.True(addr.PubKey.VerifySignature(hash, sig.SchnorrSignature));
						spender.Inputs[0].WitScript = new WitScript(Op.GetPushOp(sig.ToBytes()));
						rpc.SendRawTransaction(spender);
					}
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGenerateTaprootPubKey()
		{
			var mnemo = new Mnemonic("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about");
			var root = mnemo.DeriveExtKey();

			// From BIP86
			foreach (var v in new[]
			{
				("bc1p5cyxnuxmeuwuvkwfem96lqzszd02n6xdcjrs20cac6yqjjwudpxqkedrcr", "m/86'/0'/0'/0/0") ,
				("bc1p4qhjn9zdvkux4e44uhx8tc55attvtyu358kutcqkudyccelu0was9fqzwh", "m/86'/0'/0'/0/1"),
				("bc1p3qkhfews2uk44qtvauqyr2ttdsw7svhkl9nkm9s9c3x4ax5h60wqwruhk7", "m/86'/0'/0'/1/0")
			})
			{
				var privKey = root.Derive(KeyPath.Parse(v.Item2));
				var address = privKey.GetPublicKey().GetAddress(ScriptPubKeyType.TaprootBIP86, Network.Main);
				Assert.Equal(v.Item1, address.ToString());
			}
		}
#endif
	}
}
