#if HAS_SPAN
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.BIP322;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin
{
	public static class BIP322o
	{
		public enum SignatureType
		{
			Legacy,
			Simple,
			Full
		}

		public enum MessageType
		{
			Legacy,
			BIP322
		}

		private static byte[] TAG = Encoding.UTF8.GetBytes("BIP0322-signed-message");

		private static byte[] BITCOIN_SIGNED_MESSAGE_HEADER_BYTES =>
			Encoding.UTF8.GetBytes("Bitcoin Signed Message:\n");

		public static uint256 CreateMessageHash(string message, MessageType type = MessageType.BIP322) =>
			CreateMessageHash(Encoding.UTF8.GetBytes(message), type);

		public static uint256 CreateMessageHash(byte[] message, MessageType type)
		{
			if (type == MessageType.Legacy)
			{
				var ms = new MemoryStream();

				ms.WriteByte((byte) BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);
				ms.Write(BITCOIN_SIGNED_MESSAGE_HEADER_BYTES, 0, BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);

				var size = new VarInt((ulong) message.Length).ToBytes();
				ms.Write(size, 0, size.Length);
				ms.Write(message, 0, message.Length);
				return Hashes.DoubleSHA256(ms.ToArray());
			}

			var tagHash = Hashes.SHA256(TAG);
			var tagBytes = tagHash.Concat(tagHash).ToArray();
			return new uint256(Hashes.SHA256(tagBytes.Concat(message).ToArray()), false);
		}

		internal static Transaction CreateToSpendTransaction(Network network, uint256 messageHash, Script scriptPubKey)
		{
			var tx = network.CreateTransaction();
			tx.Version = 0;
			tx.LockTime = 0;
			tx.Inputs.Add(new TxIn( new OutPoint(uint256.Zero, 0xFFFFFFFF), new Script(OpcodeType.OP_0, Op.GetPushOp(messageHash.ToBytes(false))))
			{
				Sequence = 0,
				WitScript = WitScript.Empty,
			});
			tx.Outputs.Add(new TxOut(Money.Zero, scriptPubKey));
			return tx;
		}

		internal static Transaction CreateToSignTransaction(Network network, uint256 toSpendTxId,
			WitScript? messageSignature = null,
			uint version = 0, uint lockTime = 0, uint sequence = 0, ScriptCoin[]? additionalInputs = null)
		{
			var tx = network.CreateTransaction();
			tx.Version = version;
			tx.LockTime = lockTime;
			tx.Inputs.Add(new TxIn(new OutPoint(toSpendTxId, 0))
			{
				Sequence = sequence,
				WitScript = messageSignature?? WitScript.Empty,
			});
			if (additionalInputs is not null)
			{
				foreach (var input in additionalInputs)
				{
					tx.Inputs.Add(new TxIn(input.Outpoint, input.Redeem)
					{
						Sequence = sequence,
					});
				}
			}

			tx.Outputs.Add(new TxOut(Money.Zero, new Script(OpcodeType.OP_RETURN)));
			return tx;
		}
		public static async Task<BIP322.BIP322Signature> SignEncoded(BitcoinAddress address, string message, SignatureType type,
			Script? redeemScript = null, ICoin[]? additionalCoins = null, params Key[] keys) => await Sign(address, message, type, keys, redeemScript, additionalCoins);
		public static async Task<BIP322.BIP322Signature> SignEncoded(BitcoinAddress address, string message, SignatureType type,
			params Key[] keys) => await Sign(address, message, type, keys);
		public static async Task<BIP322.BIP322Signature> Sign(BitcoinAddress address, string message, SignatureType type,
			Key[] keys, Script? redeemScript = null, ICoin[]? additionalCoins = null)
		{
			// if(address.ScriptPubKey.IsScriptType(ScriptType.P2SH) && type == SignatureType.Simple)
			// 	throw new InvalidOperationException("Simple signatures are not supported for P2SH scripts.");

			if(additionalCoins?.Length > 0 && type != SignatureType.Full)
				throw new InvalidOperationException("Additional coins are only supported for full signatures.");
			if(keys.Length == 0)
				throw new ArgumentException("At least one key is required.", nameof(keys));
			var messageHash = CreateMessageHash(message,
				type == SignatureType.Legacy ? MessageType.Legacy : MessageType.BIP322);
			var network = address.Network;
			switch (type)
			{
				case SignatureType.Legacy when !address.ScriptPubKey.IsScriptType(ScriptType.P2PKH):
					throw new InvalidOperationException("Legacy signing is only supported for P2PKH scripts.");
				case SignatureType.Legacy when keys.Length != 1:
					throw new InvalidOperationException("Legacy signing only supports one key.");
				case SignatureType.Legacy:
				{
					var key = keys[0];
					var sig = key.SignCompact(messageHash);
					var recovered = PubKey.RecoverCompact(messageHash, sig);
					if (recovered != key.PubKey)
					{
						throw new InvalidOperationException("Invalid signature.");
					}
					return new BIP322.BIP322Signature.Legacy(key.IsCompressed, sig, address.Network);
				}
			}

			var toSpendTx = CreateToSpendTransaction(network, new uint256(messageHash), address.ScriptPubKey);
			var toSignTx = CreateToSignTransaction(network, toSpendTx.GetHash(), WitScript.Empty);

			Func<OutPoint[], Task<TxOut[]>>? proofOfFundsLookup = null;
			if (additionalCoins is not null)
			{
				foreach (var coin in additionalCoins)
				{
					toSignTx.Inputs.Add(new TxIn(coin.Outpoint));
				}
				proofOfFundsLookup = async (outpoints) => additionalCoins.Select(coin => coin.TxOut).ToArray();
			}
			var coins = additionalCoins is null ? toSpendTx.Outputs.AsCoins().Select(coin => redeemScript is null ? coin: new ScriptCoin(coin, redeemScript)).ToArray() : toSpendTx.Outputs.AsCoins().Concat(additionalCoins).ToArray();

			toSignTx.Sign(keys.Select(key=> key.GetBitcoinSecret(address.Network)), coins);

			BIP322.BIP322Signature result =
				type == SignatureType.Simple ? new BIP322.BIP322Signature.Simple(toSignTx.Inputs[0].WitScript, address.Network)
											 : new BIP322.BIP322Signature.Full(toSignTx, address.Network);
			if (result is BIP322.BIP322Signature.Simple { WitnessScript : { PushCount: 0 } } ||
				result is BIP322.BIP322Signature.Full full && full.SignedTransaction.Inputs[0].WitScript.PushCount == 0 && full.SignedTransaction.Inputs[0].ScriptSig.Length == 0)
			{
				throw new InvalidOperationException("Failed to sign the message. Did you forget to provide a redeem script?");
			}


			if (!await Verify(message, address, result!, proofOfFundsLookup))
			{
				throw new InvalidOperationException("Could not produce a valid signature.");
			}

			return result;
		}
		public static Task<bool> Verify(string message, BitcoinAddress address, string signature,
			Func<OutPoint[], Task<TxOut[]>>? proofOfFundsLookup = null)
		{
			var sig = BIP322Signature.Parse(signature, address.Network);
			return Verify(message, address, sig, proofOfFundsLookup);
		}

		public static async Task<bool> Verify(string message, BitcoinAddress address, BIP322.BIP322Signature signature,
			Func<OutPoint[], Task<TxOut[]>>? proofOfFundsLookup = null)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);
			if (signature is BIP322.BIP322Signature.Simple { WitnessScript: var script })
			{
				if (script.PushCount < 2 && !address.ScriptPubKey.IsScriptType(ScriptType.Taproot))
				{
					return false;
				}

				var toSpend = CreateToSpendTransaction(address.Network, CreateMessageHash(message, MessageType.BIP322),
					address.ScriptPubKey);
				var toSign = CreateToSignTransaction(address.Network, toSpend.GetHash(), script);
				ScriptEvaluationContext evalContext = new ScriptEvaluationContext()
				{
					ScriptVerify = ScriptVerify.Const_ScriptCode
								   | ScriptVerify.LowS
								   | ScriptVerify.StrictEnc
								   | ScriptVerify.NullFail
								   | ScriptVerify.MinimalData
								   | ScriptVerify.CleanStack
								   | ScriptVerify.P2SH
								   | ScriptVerify.Witness
								   | ScriptVerify.Taproot
								   | ScriptVerify.MinimalIf
				};

				if (address.ScriptPubKey.IsScriptType(ScriptType.P2SH))
				{

					var withScriptParams = PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(script);
					toSign.Inputs[0].ScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(new Op[0], withScriptParams.Hash.ScriptPubKey);
				}
				// Create a checker for the signature
				TransactionChecker checker = address.ScriptPubKey.IsScriptType(ScriptType.Taproot) ? new TransactionChecker(toSign, 0, toSpend.Outputs[0], new TaprootReadyPrecomputedTransactionData(toSign, toSpend.Outputs.ToArray())) : new TransactionChecker(toSign, 0, toSpend.Outputs[0]);
				return evalContext.VerifyScript(toSign.Inputs[0].ScriptSig, script, address.ScriptPubKey, checker);
			}
			else if (signature is BIP322.BIP322Signature.Legacy { CompactSignature : var sig})
			{
				try
				{
					if (!address.ScriptPubKey.IsScriptType(ScriptType.P2PKH))
					{
						return false;
					}
					var hash = CreateMessageHash(message, MessageType.Legacy);
					var k = sig.RecoverPubKey(hash);
					if (k.GetAddress(ScriptPubKeyType.Legacy, address.Network) != address)
					{
						return false;
					}
					return ECDSASignature.TryParseFromCompact(sig.Signature, out var ecSig) && k.Verify(hash, ecSig);
				}
				catch
				{
					return false;
				}
			}
			else if (signature is BIP322.BIP322Signature.Full { SignedTransaction: var toSign })
			{
				var toSpend = CreateToSpendTransaction(address.Network, CreateMessageHash(message, MessageType.BIP322),
					address.ScriptPubKey);
				if (toSign!.Inputs[0].PrevOut.Hash != toSpend.GetHash())
				{
					return false;
				}

				if (toSign.Inputs.Count > 1)
				{
					if (proofOfFundsLookup is null)
					{
						return false;
					}

					var utxosToLookup = toSign.Inputs.Skip(1).Select(x => x.PrevOut).ToArray();
					var lookup = await proofOfFundsLookup(utxosToLookup);
					if (lookup.Length != utxosToLookup.Length)
					{
						return false;
					}

					return toSign.CreateValidator(toSpend.Outputs.Concat(lookup).ToArray()).ValidateInputs()
						.All(x => x.Error is null);
				}

				return toSign.CreateValidator(toSpend.Outputs.ToArray()).ValidateInputs().All(x => x.Error is null);
			}
			return false;
		}
	}
}
#endif
