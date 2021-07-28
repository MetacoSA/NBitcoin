#if !NO_BC
using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Math;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin.Crypto
{
	public class ECDSASignature
	{
#if HAS_SPAN
		/* This function is taken from the libsecp256k1 distribution and implements
 *  DER parsing for ECDSA signatures, while supporting an arbitrary subset of
 *  format violations.
 *
 *  Supported violations include negative integers, excessive padding, garbage
 *  at the end, and overly long length descriptors. This is safe to use in
 *  Bitcoin because since the activation of BIP66, signatures are verified to be
 *  strict DER before being passed to this module, and we know it supports all
 *  violations present in the blockchain before that point.
 */
		static bool ecdsa_signature_parse_der_lax(ReadOnlySpan<byte> input, out Secp256k1.SecpECDSASignature sig)
		{
			int inputlen = input.Length;
			int rpos, rlen, spos, slen;
			int pos = 0;
			int lenbyte;
			Span<byte> tmpsig = stackalloc byte[64];
			tmpsig.Clear();
			sig = null;
			int overflow = 0;

			/* Sequence tag byte */
			if (pos == inputlen || input[pos] != 0x30)
			{
				return false;
			}
			pos++;

			/* Sequence length bytes */
			if (pos == inputlen)
			{
				return false;
			}
			lenbyte = input[pos++];
			if ((lenbyte & 0x80) != 0)
			{
				lenbyte -= 0x80;
				if (lenbyte > inputlen - pos)
				{
					return false;
				}
				pos += lenbyte;
			}

			/* Integer tag byte for R */
			if (pos == inputlen || input[pos] != 0x02)
			{
				return false;
			}
			pos++;

			/* Integer length for R */
			if (pos == inputlen)
			{
				return false;
			}
			lenbyte = input[pos++];
			if ((lenbyte & 0x80) != 0)
			{
				lenbyte -= 0x80;
				if (lenbyte > inputlen - pos)
				{
					return false;
				}
				while (lenbyte > 0 && input[pos] == 0)
				{
					pos++;
					lenbyte--;
				}
				if (lenbyte >= 4)
				{
					return false;
				}
				rlen = 0;
				while (lenbyte > 0)
				{
					rlen = (rlen << 8) + input[pos];
					pos++;
					lenbyte--;
				}
			}
			else
			{
				rlen = lenbyte;
			}
			if (rlen > inputlen - pos)
			{
				return false;
			}
			rpos = pos;
			pos += rlen;

			/* Integer tag byte for S */
			if (pos == inputlen || input[pos] != 0x02)
			{
				return false;
			}
			pos++;

			/* Integer length for S */
			if (pos == inputlen)
			{
				return false;
			}
			lenbyte = input[pos++];
			if ((lenbyte & 0x80) != 0)
			{
				lenbyte -= 0x80;
				if (lenbyte > inputlen - pos)
				{
					return false;
				}
				while (lenbyte > 0 && input[pos] == 0)
				{
					pos++;
					lenbyte--;
				}
				if (lenbyte >= 4)
				{
					return false;
				}
				slen = 0;
				while (lenbyte > 0)
				{
					slen = (slen << 8) + input[pos];
					pos++;
					lenbyte--;
				}
			}
			else
			{
				slen = lenbyte;
			}
			if (slen > inputlen - pos)
			{
				return false;
			}
			spos = pos;

			/* Ignore leading zeroes in R */
			while (rlen > 0 && input[rpos] == 0)
			{
				rlen--;
				rpos++;
			}
			/* Copy R value */
			if (rlen > 32)
			{
				overflow = 1;
			}
			else
			{
				input.Slice(rpos, rlen).CopyTo(tmpsig.Slice(32 - rlen));
				//memcpy(tmpsig + 32 - rlen, input + rpos, rlen);
			}

			/* Ignore leading zeroes in S */
			while (slen > 0 && input[spos] == 0)
			{
				slen--;
				spos++;
			}
			/* Copy S value */
			if (slen > 32)
			{
				overflow = 1;
			}
			else
			{
				input.Slice(spos, slen).CopyTo(tmpsig.Slice(64 - slen));
				//memcpy(tmpsig + 64 - slen, input + spos, slen);
			}

			if (overflow == 0)
			{
				overflow = Secp256k1.SecpECDSASignature.TryCreateFromCompact(tmpsig, out sig) ? 0 : 1;
			}
			if (overflow != 0)
			{
				/* Overwrite the result again with a correctly-parsed but invalid
				   signature if parsing failed. */
				tmpsig.Clear();
				Secp256k1.SecpECDSASignature.TryCreateFromCompact(tmpsig, out sig);
			}
			return true;
		}
		private readonly Secp256k1.Scalar r, s;
		internal ECDSASignature(in Secp256k1.Scalar r, in Secp256k1.Scalar s)
		{
			this.r = r;
			this.s = s;
		}
#else
		private readonly BigInteger _R;
		internal BigInteger R
		{
			get
			{
				return _R;
			}
		}
		private BigInteger _S;
		internal BigInteger S
		{
			get
			{
				return _S;
			}
		}
		internal ECDSASignature(BigInteger r, BigInteger s)
		{
			if (r == null)
				throw new ArgumentNullException(paramName: nameof(r));
			if (s == null)
				throw new ArgumentNullException(paramName: nameof(s));
			_R = r;
			_S = s;
		}
		internal ECDSASignature(BigInteger[] rs)
		{
			_R = rs[0];
			_S = rs[1];
		}
		public static bool TryParseFromCompact(byte[] compactFormat, out ECDSASignature signature)
		{
			if (compactFormat == null)
				throw new ArgumentNullException(nameof(compactFormat));
			signature = null;
			if (compactFormat.Length != 64)
				return false;
#pragma warning disable 618
			signature = new ECDSASignature(
                new NBitcoin.BouncyCastle.Math.BigInteger(1, compactFormat, 0, 32),
                new NBitcoin.BouncyCastle.Math.BigInteger(1, compactFormat, 32, 32));
#pragma warning restore 618
			return true;
		}

		public byte[] ToCompact()
		{
			var result = new byte[64];
#pragma warning disable 618
			var rBytes = this.R.ToByteArrayUnsigned();
			var sBytes = this.S.ToByteArrayUnsigned();
			rBytes.CopyTo(result, 32 - rBytes.Length);
			sBytes.CopyTo(result, 64 - sBytes.Length);
#pragma warning restore 618
			return result;
		}
#endif

#if HAS_SPAN
		internal ECDSASignature(Secp256k1.SecpECDSASignature sig)
		{
			r = sig.r;
			s = sig.s;
		}
		public static bool TryParseFromCompact(byte[] compactFormat, out ECDSASignature signature)
		{
			if (compactFormat == null)
				throw new ArgumentNullException(nameof(compactFormat));
			signature = null;
			if (compactFormat.Length != 64)
				return false;
			if (Secp256k1.SecpECDSASignature.TryCreateFromCompact(compactFormat, out var s) && s is Secp256k1.SecpECDSASignature)
			{
				signature = new ECDSASignature(s);
				return true;
			}
			return false;
		}
		public static bool TryParseFromCompact(ReadOnlySpan<byte> compactFormat, out ECDSASignature signature)
		{
			signature = null;
			if (compactFormat.Length != 64)
				return false;
			if (Secp256k1.SecpECDSASignature.TryCreateFromCompact(compactFormat, out var s) && s is Secp256k1.SecpECDSASignature)
			{
				signature = new ECDSASignature(s);
				return true;
			}
			return false;
		}
		public ECDSASignature(byte[] derSig) : this(derSig.AsSpan())
		{
		}

		public byte[] ToCompact()
		{
			var result = new byte[64];
			ToSecpECDSASignature().WriteCompactToSpan(result.AsSpan());
			return result;
		}

		public ECDSASignature(ReadOnlySpan<byte> derSig)
		{
			if (ecdsa_signature_parse_der_lax(derSig, out var sig) && sig is Secp256k1.SecpECDSASignature)
			{
				(r, s) = sig;
				return;
			}
			throw new FormatException(InvalidDERSignature);
		}
		/**
		* What we get back from the signer are the two components of a signature, r and s. To get a flat byte stream
		* of the type used by Bitcoin we have to encode them using DER encoding, which is just a way to pack the two
		* components into a structure.
		*/
		public byte[] ToDER()
		{
			Span<byte> tmp = stackalloc byte[75];
			ToSecpECDSASignature().WriteDerToSpan(tmp, out int l);
			tmp = tmp.Slice(0, l);
			return tmp.ToArray();
		}
#else
		public ECDSASignature(byte[] derSig)
		{
			try
			{
				Asn1InputStream decoder = new Asn1InputStream(derSig);
				var seq = decoder.ReadObject() as DerSequence;
				if (seq == null || seq.Count != 2)
					throw new FormatException(InvalidDERSignature);
				_R = ((DerInteger)seq[0]).Value;
				_S = ((DerInteger)seq[1]).Value;
			}
			catch (Exception ex)
			{
				throw new FormatException(InvalidDERSignature, ex);
			}
		}

		public ECDSASignature(Stream derSig)
		{
			try
			{
				Asn1InputStream decoder = new Asn1InputStream(derSig);
				var seq = decoder.ReadObject() as DerSequence;
				if (seq == null || seq.Count != 2)
					throw new FormatException(InvalidDERSignature);
				_R = ((DerInteger)seq[0]).Value;
				_S = ((DerInteger)seq[1]).Value;
			}
			catch (Exception ex)
			{
				throw new FormatException(InvalidDERSignature, ex);
			}
		}
		/**
		* What we get back from the signer are the two components of a signature, r and s. To get a flat byte stream
		* of the type used by Bitcoin we have to encode them using DER encoding, which is just a way to pack the two
		* components into a structure.
		*/
		public byte[] ToDER()
		{
			// Usually 70-72 bytes.
			MemoryStream bos = new MemoryStream(72);
			DerSequenceGenerator seq = new DerSequenceGenerator(bos);
#pragma warning disable 618
			seq.AddObject(new DerInteger(R));
			seq.AddObject(new DerInteger(S));
#pragma warning restore 618
			seq.Close();
			return bos.ToArray();
		}
#endif
#if HAS_SPAN
		public void WriteDerToSpan(Span<byte> sigs, out int length)
		{
			ToSecpECDSASignature().WriteDerToSpan(sigs, out length);
		}
		internal Secp256k1.SecpECDSASignature ToSecpECDSASignature()
		{
			return new Secp256k1.SecpECDSASignature(r, s.IsHigh ? s.Negate() : s, false);
		}
#endif
		const string InvalidDERSignature = "Invalid DER signature";
		public static ECDSASignature FromDER(byte[] sig)
		{
			return new ECDSASignature(sig);
		}

#if HAS_SPAN
		/// <summary>
		/// Enforce LowS on the signature
		/// </summary>
		public ECDSASignature MakeCanonical()
		{
			if (!IsLowS)
			{
				return new ECDSASignature(this.r, this.s.Negate());
			}
			else
				return this;
		}
		public bool IsLowS
		{
			get
			{
				return !s.IsHigh;
			}
		}

		public bool IsLowR
		{
			get
			{
				return !r.IsHigh;
			}
		}
		public static bool IsValidDER(ReadOnlySpan<byte> bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			return ecdsa_signature_parse_der_lax(bytes, out _);
		}
		public static bool IsValidDER(byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			return IsValidDER(bytes.AsSpan());
		}
#else
		/// <summary>
		/// Enforce LowS on the signature
		/// </summary>
		public ECDSASignature MakeCanonical()
		{
			if (!IsLowS)
			{
#pragma warning disable 618
				return new ECDSASignature(this.R, ECKey.CURVE_ORDER.Subtract(this.S));
#pragma warning restore 618
			}
			else
				return this;
		}

		public bool IsLowS
		{
			get
			{
#pragma warning disable 618
				return this.S.CompareTo(ECKey.HALF_CURVE_ORDER) <= 0;
#pragma warning restore 618
			}
		}

		public bool IsLowR
		{
			get
			{
#pragma warning disable 618
				var rBytes = this.R.ToByteArrayUnsigned();
#pragma warning restore 618
				return rBytes.Length < 32 || rBytes[0] < 0x80;
			}
		}
		public static bool IsValidDER(byte[] bytes)
		{
			try
			{
				ECDSASignature.FromDER(bytes);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
			catch (Exception)
			{
				return false;
			}
		}
#endif
	}
}
