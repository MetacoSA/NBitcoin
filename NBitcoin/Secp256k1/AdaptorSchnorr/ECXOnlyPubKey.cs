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
	partial class ECXOnlyPubKey
	{
#if SECP256K1_LIB
	public
#else
		internal
#endif
		bool SigVerifyEncryptedBIP340(SchnorrEncryptedSignature encryptedSignature, ReadOnlySpan<byte> msg32, ECPubKey encryptionKey)
		{
			if (encryptedSignature is null)
				throw new ArgumentNullException(nameof(encryptedSignature));
			if (msg32.Length != 32)
				throw new ArgumentException(nameof(msg32), "msg32 should be 32 bytes");
			if (encryptionKey is null)
				throw new ArgumentNullException(nameof(encryptionKey));

			var R = encryptedSignature.R;
			var s_hat = encryptedSignature.s_hat;
			var needs_negation = encryptedSignature.need_negation;
			var X = this.Q;
			var Y = encryptionKey.Q;

			//  needs_negation => R_hat = R + Y
			// !needs_negation => R_hat = R - Y
			if (!needs_negation)
				Y = Y.Negate();
			var R_hat = R.ToGroupElementJacobian().Add(Y).ToGroupElement();

			Span<byte> RBytes = stackalloc byte[32];
			R.x.WriteToSpan(RBytes);
			Span<byte> XBytes = stackalloc byte[32];
			X.x.WriteToSpan(XBytes);
			var c = ECPrivKey.GetBIP340Challenge(msg32, RBytes, XBytes);

			// R_hat == g!(s_hat * G - c * X)
			var res = ctx.EcMultContext.Mult(X.ToGroupElementJacobian(), c.Negate(), s_hat).Add(R_hat.Negate());
			return res.IsInfinity;
		}
	}
}
#endif
