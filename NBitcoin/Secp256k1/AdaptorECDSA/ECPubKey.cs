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
		public bool TryRecoverDecryptionKey(SecpECDSASignature signature, ECDSAEncryptedSignature encryptedSignature, [MaybeNullWhen(false)] out ECPrivKey? decryptionKey)
		{
			if (signature is null)
				throw new ArgumentNullException(nameof(signature));
			if (encryptedSignature is null)
				throw new ArgumentNullException(nameof(encryptedSignature));
			decryptionKey = null;
			if (encryptedSignature.GetRAsScalar() != signature.r)
				return false;
			var adaptor_secret = signature.s.Inverse();
			adaptor_secret = adaptor_secret * encryptedSignature.sp;

			/* Deal with ECDSA malleability */
			var adaptor_expected_gej = ctx.EcMultGenContext.MultGen(adaptor_secret);
			var adaptor_expected_ge = adaptor_expected_gej.ToGroupElement();
			var adaptor_expected = new ECPubKey(adaptor_expected_ge, ctx);
			if (this != adaptor_expected)
			{
				adaptor_secret = adaptor_secret.Negate();
			}
			decryptionKey = ctx.CreateECPrivKey(adaptor_secret);
			adaptor_expected = decryptionKey.CreatePubKey();
			if (this != adaptor_expected)
			{
				decryptionKey = null;
				return false;
			}
			return true;
		}

		public bool SigVerifyEncryptedECDSA(ECDSAEncryptedSignature encryptedSignature, ReadOnlySpan<byte> msg32, ECPubKey encryptionKey)
		{
			if (encryptedSignature is null)
				throw new ArgumentNullException(nameof(encryptedSignature));
			if (msg32.Length != 32)
				throw new ArgumentException(nameof(msg32), "msg32 should be 32 bytes");
			if (encryptionKey is null)
				throw new ArgumentNullException(nameof(encryptionKey));
			if (encryptedSignature.rp.IsInfinity || encryptedSignature.r.IsInfinity)
				return false;
			if (!new ECPubKey(encryptedSignature.rp, ctx).VerifyDLEQ(encryptedSignature.Proof, encryptionKey, new ECPubKey(encryptedSignature.r, ctx)))
				return false;
			var msg = new Scalar(msg32, out _);
			/* return R' == s'⁻¹(m * G + R.x * X) */
			var sn = encryptedSignature.sp.InverseVariable();
			var u1 = sn * msg;
			var u2 = sn * encryptedSignature.GetRAsScalar();
			var pubkeyj = this.Q.ToGroupElementJacobian();
			var derived_rp = ctx.EcMultContext.Mult(pubkeyj, u2, u1);
			if (derived_rp.IsInfinity)
				return false;
			derived_rp = derived_rp.Negate();
			derived_rp = derived_rp.AddVariable(encryptedSignature.rp, out _);
			return derived_rp.IsInfinity;
		}

		public bool VerifyDLEQ(DLEQProof proof, ECPubKey Y, ECPubKey Z)
		{
			if (proof is null)
				throw new ArgumentNullException(nameof(proof));
			if (Y is null)
				throw new ArgumentNullException(nameof(Y));
			if (Z is null)
				throw new ArgumentNullException(nameof(Z));

			var ecmult_ctx = ctx.EcMultContext;
			var p1j = Q.ToGroupElementJacobian();
			var p2j = Z.Q.ToGroupElementJacobian();
			var e_neg = proof.b.Negate();
			/* R1 = s*G  - e*P1 */
			var r1j = ecmult_ctx.Mult(p1j, e_neg, proof.c);
			/* R2 = s*gen2 - e*P2 */
			var tmpj = ecmult_ctx.Mult(p2j, e_neg, Scalar.Zero);
			var gen2j = Y.Q.ToGroupElementJacobian();
			var r2j = ecmult_ctx.Mult(gen2j, proof.c, Scalar.Zero);
			r2j = r2j.AddVariable(tmpj, out _);
			var r1 = r1j.ToGroupElement();
			var r2 = r2j.ToGroupElement();
			ECPrivKey.secp256k1_dleq_challenge(Y.Q, r1, r2, Q, Z.Q, out var e_expected);
			e_expected = e_expected.Add(e_neg);
			return e_expected.IsZero;
		}
	}
}
#endif
