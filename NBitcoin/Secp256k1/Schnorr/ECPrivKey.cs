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
		static RandomNumberGenerator rand = RandomNumberGenerator.Create();
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

		/* Precomputed TaggedHash("BIP0340/aux", 0x0000...00); */
		static byte[] ZERO_MASK = new byte[]{
			  84, 241, 105, 207, 201, 226, 229, 114,
			 116, 128,  68,  31, 144, 186,  37, 196,
			 136, 244,  97, 199,  11,  94, 165, 220,
			 170, 247, 175, 105, 39,  10, 165,  20
		};

		static byte[] TAG_BIP0340AUX = ASCIIEncoding.ASCII.GetBytes("BIP0340/aux");
		public readonly static byte[] TAG_BIP340 = ASCIIEncoding.ASCII.GetBytes("BIP0340/nonce");
		public bool TryGetNonce(Span<byte> nonce32, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> xonly_pk32, ReadOnlySpan<byte> algo)
		{
			int i = 0;
			Span<byte> masked_key = stackalloc byte[32];
			using SHA256 sha = new SHA256();

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
			else
			{
			
				for (i = 0; i < 32; i++)
				{
					masked_key[i] = (byte)(key32[i] ^ ZERO_MASK[i]);
				}
			}

			sha.InitializeTagged(algo);
			//* Hash masked-key||pk||msg using the tagged hash as per the spec */
			sha.Write(masked_key);
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
	partial class ECPrivKey
	{
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
			if (msg32.Length != 32)
				throw new ArgumentException("msg32 should be 32 bytes", nameof(msg32));
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
			var ret = nonceFunction.TryGetNonce(buf, msg32, sec_key, pk_buf, BIP340NonceFunction.TAG_BIP340);
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
			
			/* Set scalar e to the challenge hash modulo the curve order as per
     * BIP340. */
			Scalar e = GetBIP340Challenge(msg32, sig64.Slice(0, 32), pk_buf);
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
			return signature is SecpSchnorrSignature;
		}

		internal static Scalar GetBIP340Challenge(ReadOnlySpan<byte> msg32, Span<byte> R, Span<byte> pk)
		{
			Span<byte> buf = stackalloc byte[32];
			/* tagged hash(r.x, pk.x, msg32) */
			using SHA256 sha = new SHA256();
			sha.InitializeTagged(ECXOnlyPubKey.TAG_BIP0340Challenge);
			sha.Write(R);
			sha.Write(pk);
			sha.Write(msg32);
			sha.GetHash(buf);
			var e = new Scalar(buf, out _);
			return e;
		}

		public ECXOnlyPubKey CreateXOnlyPubKey()
		{
			return CreateXOnlyPubKey(out _);
		}
		public ECXOnlyPubKey CreateXOnlyPubKey(out bool parity)
		{
			return CreatePubKey().ToXOnlyPubKey(out parity);
		}
	}
}
#endif
