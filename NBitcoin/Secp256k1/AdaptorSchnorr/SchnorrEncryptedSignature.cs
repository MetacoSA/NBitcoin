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
	class SchnorrEncryptedSignature
	{
		public SchnorrEncryptedSignature(GE R, Scalar s_hat, bool need_negation)
		{
			this.R = R;
			this.s_hat = s_hat;
			this.need_negation = need_negation;
		}
		public readonly GE R;
		public readonly Scalar s_hat;
		public readonly bool need_negation;

		public bool TryRecoverDecryptionKey(SecpSchnorrSignature signature, ECPubKey encryptionKey, [MaybeNullWhen(false)] out ECPrivKey? decryptionKey)
		{
			if (encryptionKey is null)
				throw new ArgumentNullException(nameof(encryptionKey));
			return encryptionKey.TryRecoverDecryptionKey(signature, this, out decryptionKey);
		}
	}
}
#endif
