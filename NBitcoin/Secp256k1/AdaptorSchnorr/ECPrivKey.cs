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
	partial class ECPrivKey
	{
#if SECP256K1_LIB
	public
#else
	internal
#endif
		SchnorrEncryptedSignature SignEncryptedBIP340(ReadOnlySpan<byte> msg32, ECPubKey encryptionKey)
		{
			return SignEncryptedBIP340(msg32, encryptionKey, null);
		}

#if SECP256K1_LIB
	public
#else
		internal
#endif
		SecpSchnorrSignature DecryptBIP340Signature(SchnorrEncryptedSignature encryptedSignature)
		{
			if (encryptedSignature is null)
				throw new ArgumentNullException(nameof(encryptedSignature));
			var y = this.sec;
			if (encryptedSignature.need_negation)
				y = y.Negate();
			var s = encryptedSignature.s_hat + y;
			return new SecpSchnorrSignature(encryptedSignature.R.x, s);
		}

#if SECP256K1_LIB
	public
#else
		internal
#endif
		SchnorrEncryptedSignature SignEncryptedBIP340(ReadOnlySpan<byte> msg32, ECPubKey adaptor, BIP340NonceFunction? nonceFunction)
		{
			if (adaptor is null)
				throw new ArgumentNullException(nameof(adaptor));
			if (msg32.Length != 32)
				throw new ArgumentException(nameof(msg32), "msg32 should be 32 bytes");

			Span<byte> sec_key = stackalloc byte[32];
			Span<byte> pk_buf = stackalloc byte[32];
			var pk = CreateXOnlyPubKey(out var parity).Q;
			var sk = this.sec;
			/* Because we are signing for a x-only pubkey, the secret key is negated
	* before signing if the point corresponding to the secret key does not
	* have an even Y. */
			if (parity)
			{
				sk = sk.Negate();
			}
			sk.WriteToSpan(sec_key);
			pk.x.WriteToSpan(pk_buf);

			Span<byte> nonce32 = stackalloc byte[32];
			nonceFunction ??= new BIP340NonceFunction(true);
			Span<byte> msgNonce = stackalloc byte[33 + 32];
			adaptor.WriteToSpan(true, msgNonce, out _);
			msg32.CopyTo(msgNonce.Slice(33));
			if (!nonceFunction.TryGetNonce(nonce32, msgNonce, sec_key, pk_buf, BIP340NonceFunction.TAG_BIP340))
				throw new InvalidOperationException("This should never happen ERROR29483, contact NBitcoin developers");

			var r = new Scalar(nonce32, out _);
			// let R = g!(r * G + Y)
			var R = this.ctx.EcMultContext.Mult(adaptor.Q.ToGroupElementJacobian(), Scalar.One, r);
			var RWithEvenY = new ECPubKey(R.ToGroupElement(), ctx).ToXOnlyPubKey(out parity);
			// We correct r here but we can't correct the decryption key (y) so we
			// store in "needs_negation" whether the decryptor needs to negate their
			// key before decrypting it
			if (parity)
			{
				r = r.Negate();
			}

			Span<byte> buff = stackalloc byte[32];
			RWithEvenY.WriteToSpan(buff);

			var c = GetBIP340Challenge(msg32, buff, pk_buf);
			var s_hat = r + c * sk;
			return new SchnorrEncryptedSignature(RWithEvenY.Q, s_hat, parity);
		}
	}
}
#endif
