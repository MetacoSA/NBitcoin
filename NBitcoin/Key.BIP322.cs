#if HAS_SPAN
#nullable enable
using NBitcoin.BIP322;
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
		public BIP322.BIP322Signature SignBIP322(BitcoinAddress address, string message, SignatureType type)
		{
			switch (type)
			{
				case SignatureType.Legacy when !address.ScriptPubKey.IsScriptType(ScriptType.P2PKH):
					throw new InvalidOperationException("Legacy signing is only supported for P2PKH scripts.");
				case SignatureType.Legacy:
					{
						var messageHash = BIP322Signature.CreateMessageHash(message, true);
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
