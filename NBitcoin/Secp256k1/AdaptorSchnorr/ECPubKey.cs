#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
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
		public ECPrivKey RecoverDecryptionKey(SchnorrEncryptedSignature encryptedSignature, SecpSchnorrSignature signature)
		{
			if (encryptedSignature is null)
				throw new ArgumentNullException(nameof(encryptedSignature));
			if (signature is null)
				throw new ArgumentNullException(nameof(signature));
			var y = signature.s.Add(encryptedSignature.s_hat.Negate());
			if (encryptedSignature.need_negation)
				y = y.Negate();
			if (y.IsOverflow || y.IsZero)
				throw new InvalidOperationException("Impossible to recover the decryption key");
			var privKey = new ECPrivKey(y, ctx, true);
			if (privKey.CreatePubKey() == this)
				return privKey;
			throw new InvalidOperationException("Impossible to recover the decryption key");
		}
	}
}
#endif
