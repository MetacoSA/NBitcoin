#if HAS_SPAN
#nullable enable
using NBitcoin.BIP322;
using NBitcoin.Crypto;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public partial class Key
	{
		private static string TAG = "BIP0322-signed-message";

		private static byte[] BITCOIN_SIGNED_MESSAGE_HEADER_BYTES =>
			Encoding.UTF8.GetBytes("Bitcoin Signed Message:\n");

		public static uint256 CreateMessageHash(string message, HashType type = HashType.BIP322) =>
			CreateMessageHash(Encoding.UTF8.GetBytes(message), type);

		public static uint256 CreateMessageHash(byte[] message, HashType type)
		{
			if (type == HashType.Legacy)
			{
				var ms = new MemoryStream();
				ms.WriteByte((byte)BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);
				ms.Write(BITCOIN_SIGNED_MESSAGE_HEADER_BYTES, 0, BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);

				var size = new VarInt((ulong)message.Length).ToBytes();
				ms.Write(size, 0, size.Length);
				ms.Write(message, 0, message.Length);
				return Hashes.DoubleSHA256(ms.ToArray());
			}
			else
			{
				using Secp256k1.SHA256 sha = new Secp256k1.SHA256();
				sha.InitializeTagged(TAG);
				sha.Write(message);
				return new uint256(sha.GetHash(), false);
			}
		}

		internal static PSBT CreateToSignPSBT(
			Network network, uint256 messageHash, Script scriptPubKey,
			uint version = 0, uint lockTime = 0, uint sequence = 0, Coin[]? additionalInputs = null)
		{
			var toSpend = network.CreateTransaction();
			toSpend.Version = 0;
			toSpend.LockTime = 0;
			toSpend.Inputs.Add(new TxIn(new OutPoint(uint256.Zero, 0xFFFFFFFF), new Script(OpcodeType.OP_0, Op.GetPushOp(messageHash.ToBytes(false))))
			{
				Sequence = 0,
				WitScript = WitScript.Empty,
			});
			toSpend.Outputs.Add(new TxOut(Money.Zero, scriptPubKey));
			var toSpendTxId = toSpend.GetHash();
			var toSign = network.CreateTransaction();
			toSign.Version = version;
			toSign.LockTime = lockTime;
			toSign.Inputs.Add(new TxIn(new OutPoint(toSpendTxId, 0))
			{
				Sequence = sequence
			});
			additionalInputs ??= additionalInputs ?? Array.Empty<Coin>();

			foreach (var input in additionalInputs)
			{
				toSign.Inputs.Add(new TxIn(input.Outpoint, Script.Empty)
				{
					Sequence = sequence,
				});
			}
			toSign.Outputs.Add(new TxOut(Money.Zero, new Script(OpcodeType.OP_RETURN)));
			var psbt = PSBT.FromTransaction(toSign, network);
			psbt.AddTransactions(toSpend);
			psbt.AddCoins(additionalInputs);
			return psbt;
		}

		public BIP322.BIP322Signature SignBIP322(BitcoinAddress address, string message, SignatureType type)
		{
			var messageHash = CreateMessageHash(message,
				type == SignatureType.Legacy ? HashType.Legacy : HashType.BIP322);
			var network = address.Network;
			switch (type)
			{
				case SignatureType.Legacy when !address.ScriptPubKey.IsScriptType(ScriptType.P2PKH):
					throw new InvalidOperationException("Legacy signing is only supported for P2PKH scripts.");
				case SignatureType.Legacy:
					{
						var sig = SignCompact(messageHash);
						var recovered = PubKey.RecoverCompact(messageHash, sig);
						if (recovered != PubKey)
						{
							throw new InvalidOperationException("Invalid signature.");
						}
						return new BIP322.BIP322Signature.Legacy(IsCompressed, sig, address.Network);
					}
			}

			var toSignPSBT = CreateToSignPSBT(network, messageHash, address.ScriptPubKey);
			toSignPSBT.AddScripts(this.GetScriptPubKey(ScriptPubKeyType.Segwit));
			toSignPSBT.SignWithKeys(this);

			Transaction toSignTx;
			try
			{
				toSignPSBT.Finalize();
				toSignTx = toSignPSBT.ExtractTransaction();
			}
			catch
			{
				throw new InvalidOperationException("Failed to sign the message. Did you forget to provide a redeem script?");
			}

			BIP322.BIP322Signature result =
				type == SignatureType.Simple ? new BIP322.BIP322Signature.Simple(toSignTx.Inputs[0].WitScript, address.Network)
											 : new BIP322.BIP322Signature.Full(toSignTx, address.Network);

			if (!address.VerifyBIP322(message, result!))
			{
				throw new InvalidOperationException("Could not produce a valid signature.");
			}

			return result;
		}
	}
}
#endif
