#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NBitcoin.Secp256k1
{
	/// <summary>
	/// This nonce function is described in BIP-schnorr (https://github.com/sipa/bips/blob/bip-schnorr/bip-schnorr.mediawiki)
	/// </summary>
#if SECP256K1_LIB
	public
#else
	internal
#endif
	class SchnorrNonceFunction : INonceFunction
	{
		byte[]? data = null;
		public SchnorrNonceFunction(byte[]? nonceData = null)
		{
			this.data = nonceData;
		}
		public static SchnorrNonceFunction Instance { get; } = new SchnorrNonceFunction();
		public bool TryGetNonce(Span<byte> nonce32, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> algo16, uint counter)
		{
			using var sha = new Secp256k1.SHA256();
			/* Hash x||msg as per the spec */
			sha.Write(key32.Slice(0, 32));
			sha.Write(msg32.Slice(0, 32));
			/* Hash in algorithm, which is not in the spec, but may be critical to
			 * users depending on it to avoid nonce reuse across algorithms. */
			if (algo16.Length == 16)
			{
				sha.Write(algo16.Slice(0, 16));
			}
			if (data != null)
			{
				sha.Write(data.AsSpan().Slice(0, 32));
			}
			sha.GetHash(nonce32);
			return true;
		}
	}
#if SECP256K1_LIB
	public
#else
	internal
#endif
	partial class ECPrivKey
	{
		public SecpSchnorrSignature SignSchnorr(ReadOnlySpan<byte> msg32)
		{
			if (TrySignSchnorr(msg32, null, out _, out var sig) && sig is SecpSchnorrSignature)
				return sig;
			throw new InvalidOperationException("Schnorr signature failed, this should never happen");
		}
		public bool TrySignSchnorr(ReadOnlySpan<byte> msg32, INonceFunction? nonceFunction, out bool nonceIsNegated, out SecpSchnorrSignature? signature)
		{
			signature = null;
			nonceIsNegated = false;
			var ctx = this.ctx.EcMultGenContext;
			ref readonly Scalar x = ref sec;
			Scalar e;
			Scalar k;
			GEJ pkj;
			GEJ rj;
			GE pk;
			GE r;
			using var sha = new Secp256k1.SHA256();
			Span<byte> buf = stackalloc byte[33];
			Span<byte> sig = stackalloc byte[64];
			int buflen = 33;

			if (nonceFunction == null)
			{
				nonceFunction = SchnorrNonceFunction.Instance;
			}
			//secp256k1_scalar_set_b32(&x, seckey, &overflow);
			//* Fail if the secret key is invalid. */
			//if (overflow || secp256k1_scalar_is_zero(&x))
			//{
			//	memset(sig, 0, sizeof(*sig));
			//	return 0;
			//}

			pkj = ctx.MultGen(x);
			pk = pkj.ToGroupElement();
			Span<byte> seckeyb = stackalloc byte[32];
			sec.WriteToSpan(seckeyb);
			if (!nonceFunction.TryGetNonce(buf, msg32, seckeyb, new ReadOnlySpan<byte>(), 0))
			{
				seckeyb.Clear();
				return false;
			}
			seckeyb.Clear();
			k = new Scalar(buf, out _);
			if (k.IsZero)
			{
				return false;
			}
			rj = ctx.MultGen(k);
			r = rj.ToGroupElement();
			nonceIsNegated = false;
			if (!r.y.IsQuadVariable)
			{
				k = k.Negate();
				nonceIsNegated = true;
			}
			r.x.Normalize().WriteToSpan(sig.Slice(0, 32));

			sha.Write(sig.Slice(0, 32));

			ECPubKey.Serialize(pk, true, buf, out buflen);
			sha.Write(buf.Slice(0, buflen));
			sha.Write(msg32.Slice(0, 32));
			sha.GetHash(buf);
			e = new Scalar(buf, out _);
			e *= x;
			e += k;

			e.WriteToSpan(sig.Slice(32, 32));
			Scalar.Clear(ref k);
			return SecpSchnorrSignature.TryCreate(sig, out signature);
		}
	}
}
#endif
