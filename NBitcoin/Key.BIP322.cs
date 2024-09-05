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
		internal static uint256 CreateBIP322MessageHash(string message, bool legacy = false)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			if (legacy)
			{
				var ms = new MemoryStream();
				ms.WriteByte((byte)BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);
				ms.Write(BITCOIN_SIGNED_MESSAGE_HEADER_BYTES, 0, BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);

				var size = new VarInt((ulong)message.Length).ToBytes();
				ms.Write(size, 0, size.Length);
				ms.Write(bytes, 0, bytes.Length);
				return Hashes.DoubleSHA256(ms.ToArray());
			}
			else
			{
				using Secp256k1.SHA256 sha = new Secp256k1.SHA256();
				sha.InitializeTagged(TAG);
				sha.Write(bytes);
				return new uint256(sha.GetHash(), false);
			}
		}

		public BIP322.BIP322Signature SignBIP322(BitcoinAddress address, string message, SignatureType type)
		{
			switch (type)
			{
				case SignatureType.Legacy when !address.ScriptPubKey.IsScriptType(ScriptType.P2PKH):
					throw new InvalidOperationException("Legacy signing is only supported for P2PKH scripts.");
				case SignatureType.Legacy:
					{
						var messageHash = CreateBIP322MessageHash(message, true);
						var sig = SignCompact(messageHash);
						var recovered = PubKey.RecoverCompact(messageHash, sig);
						if (recovered != PubKey)
						{
							throw new InvalidOperationException("Invalid signature.");
						}
						return new BIP322.BIP322Signature.Legacy(IsCompressed, sig, address.Network);
					}
			}

			var toSignPSBT = address.CreateBIP322PSBT(message);
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
