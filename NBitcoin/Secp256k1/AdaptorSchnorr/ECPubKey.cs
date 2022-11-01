#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	partial class ECPubKey
	{
		public bool TryRecoverDecryptionKey(SecpSchnorrSignature signature, SchnorrEncryptedSignature encryptedSignature, [MaybeNullWhen(false)] out ECPrivKey decryptionKey)
		{
			decryptionKey = null;
			if (encryptedSignature is null)
				throw new ArgumentNullException(nameof(encryptedSignature));
			if (signature is null)
				throw new ArgumentNullException(nameof(signature));
			var y = signature.s.Add(encryptedSignature.s_hat.Negate());
			if (encryptedSignature.need_negation)
				y = y.Negate();
			if (y.IsOverflow || y.IsZero)
				return false;
			var privKey = new ECPrivKey(y, ctx, true);
			if (privKey.CreatePubKey() == this)
			{
				decryptionKey = privKey;
				return true;
			}
			return false;
		}
	}
}
#endif
