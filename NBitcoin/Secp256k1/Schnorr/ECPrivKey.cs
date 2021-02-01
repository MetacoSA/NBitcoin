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
	class BIP340NonceFunction : INonceFunctionHardened
	{
		static RandomNumberGenerator rand = new RNGCryptoServiceProvider();
		ReadOnlyMemory<byte> data32;
		public BIP340NonceFunction(ReadOnlyMemory<byte> auxData32)
		{
			if (auxData32.Length != 0 && auxData32.Length != 32)
				throw new ArgumentException("auxData32 should be 0 or 32 bytes", nameof(auxData32));
			this.data32 = auxData32;
		}
		public BIP340NonceFunction(bool random)
		{
			if (random)
			{
				var a = new byte[32];
				rand.GetBytes(a);
				this.data32 = a;
			}
		}

		static byte[] TAG_BIP0340AUX = ASCIIEncoding.ASCII.GetBytes("BIP0340/aux");
		public readonly static byte[] ALGO_BIP340 = ASCIIEncoding.ASCII.GetBytes("BIP0340/nonce\0\0\0");
		public readonly static byte[] TAG_BIP340 = ASCIIEncoding.ASCII.GetBytes("BIP0340/nonce");
		public bool TryGetNonce(Span<byte> nonce32, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> xonly_pk32, ReadOnlySpan<byte> algo16)
		{
			int i = 0;
			Span<byte> masked_key = stackalloc byte[32];
			using SHA256 sha = new SHA256();
			if (algo16.Length != 16)
				return false;

			if (data32.Length == 32)
			{
				sha.InitializeTagged(TAG_BIP0340AUX);
				sha.Write(data32.Span);
				sha.GetHash(masked_key);
				for (i = 0; i < 32; i++)
				{
					masked_key[i] ^= key32[i];
				}
			}

			// * Tag the hash with algo16 which is important to avoid nonce reuse across
			// * algorithms. If this nonce function is used in BIP-340 signing as defined
			// * in the spec, an optimized tagging implementation is used. */

			if (algo16.SequenceCompareTo(ALGO_BIP340) == 0)
			{
				sha.InitializeTagged(TAG_BIP340);
			}
			else
			{
				int algo16_len = 16;
				/* Remove terminating null bytes */
				while (algo16_len > 0 && algo16[algo16_len - 1] == 0)
				{
					algo16_len--;
				}
				sha.InitializeTagged(algo16.Slice(0, algo16_len));
			}

			//* Hash (masked-)key||pk||msg using the tagged hash as per the spec */
			if (data32.Length == 32)
			{
				sha.Write(masked_key);
			}
			else
			{
				sha.Write(key32);
			}
			sha.Write(xonly_pk32);
			sha.Write(msg32);
			sha.GetHash(nonce32);
			return true;
		}
	}



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
			if (TrySignSchnorr(msg32, null, out _, out var sig))
				return sig;
			throw new InvalidOperationException("Schnorr signature failed, this should never happen");
		}
		/// <summary>
		/// Create a non deterministic BIP340 schnorr signature. With Auxiliary random data taken from secure RNG.
		/// </summary>
		/// <param name="msg32">32 bytes message to sign</param>
		/// <returns>A schnorr signature</returns>
		public SecpSchnorrSignature SignBIP340(ReadOnlySpan<byte> msg32)
		{
			return SignBIP340(msg32, new BIP340NonceFunction(true));
		}
		/// <summary>
		/// Create a deterministic BIP340 schnorr signature. With auxiliary random data passed in parameter.
		/// </summary>
		/// <param name="msg32">32 bytes message to sign</param>
		/// <param name="auxData32">Auxiliary random data</param>
		/// <returns>A schnorr signature</returns>
		public SecpSchnorrSignature SignBIP340(ReadOnlySpan<byte> msg32, ReadOnlyMemory<byte> auxData32)
		{
			return SignBIP340(msg32, new BIP340NonceFunction(auxData32));
		}
		public SecpSchnorrSignature SignBIP340(ReadOnlySpan<byte> msg32, INonceFunctionHardened? nonceFunction)
		{
			if (TrySignBIP340(msg32, nonceFunction, out var sig))
				return sig;
			throw new InvalidOperationException("Schnorr signature failed, this should never happen");
		}
		
		public bool TrySignBIP340(ReadOnlySpan<byte> msg32, INonceFunctionHardened? nonceFunction, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out SecpSchnorrSignature signature)
		{
			return TrySignBIP340(msg32, null, nonceFunction, out signature);
		}

		public bool TrySignBIP340(ReadOnlySpan<byte> msg32, ECPubKey? pubkey, INonceFunctionHardened? nonceFunction, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out SecpSchnorrSignature signature)
		{
			signature = null;
			if (msg32.Length != 32)
				return false;
			using var sha = new Secp256k1.SHA256();
			Span<byte> buf = stackalloc byte[32];
			Span<byte> sig64 = stackalloc byte[64];
			Span<byte> pk_buf = stackalloc byte[32];
			Span<byte> sec_key = stackalloc byte[32];

			if (nonceFunction == null)
			{
				nonceFunction = new BIP340NonceFunction(true);
			}

			var pk = (pubkey ?? CreatePubKey()).Q;
			var sk = this.sec;
			/* Because we are signing for a x-only pubkey, the secret key is negated
	* before signing if the point corresponding to the secret key does not
	* have an even Y. */
			if (pk.y.IsOdd)
			{
				sk = sk.Negate();
			}
			sk.WriteToSpan(sec_key);
			pk.x.WriteToSpan(pk_buf);
			var ret = nonceFunction.TryGetNonce(buf, msg32, sec_key, pk_buf, BIP340NonceFunction.ALGO_BIP340);
			var k = new Scalar(buf, out _);
			ret &= !k.IsZero;
			Scalar.CMov(ref k, Scalar.One, ret ? 0 : 1);
			var rj = ctx.EcMultGenContext.MultGen(k);
			var r = rj.ToGroupElement();
			var ry = r.y.NormalizeVariable();
			if (ry.IsOdd)
				k = k.Negate();
			var rx = r.x.NormalizeVariable();
			rx.WriteToSpan(sig64);
			/* tagged hash(r.x, pk.x, msg32) */
			sha.InitializeTagged(ECXOnlyPubKey.TAG_BIP0340Challenge);
			sha.Write(sig64.Slice(0, 32));
			sha.Write(pk_buf);
			sha.Write(msg32);
			sha.GetHash(buf);

			/* Set scalar e to the challenge hash modulo the curve order as per
     * BIP340. */
			var e = new Scalar(buf, out _);
			e = e * sk;
			e = e + k;
			e.WriteToSpan(sig64.Slice(32));

			ret &= SecpSchnorrSignature.TryCreate(sig64, out signature);

			k = default;
			sk = default;
			sec_key.Fill(0);
			sig64.Fill(0);
			if (!ret)
				signature = null;
			return ret;
		}
		public bool TrySignSchnorr(ReadOnlySpan<byte> msg32, INonceFunction? nonceFunction, out bool nonceIsNegated, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out SecpSchnorrSignature signature)
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

		public ECXOnlyPubKey CreateXOnlyPubKey()
		{
			return CreatePubKey().ToXOnlyPubKey(out _);
		}
	}
}
#endif
