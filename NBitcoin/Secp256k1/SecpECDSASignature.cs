#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class SecpECDSASignature
	{
		public readonly Scalar r;
		public readonly Scalar s;

		/// <summary>
		/// Create a signature from r and s
		/// </summary>
		/// <param name="r"></param>
		/// <param name="s"></param>
		/// <param name="enforceCheck">If true, will check that r and s are not zero or overflow. If false, we assume the caller made the checks</param>
		/// <exception cref="System.ArgumentException">Thrown if enforceCheck is true and r or s is not valid</exception>
		public SecpECDSASignature(in Scalar r, in Scalar s, bool enforceCheck)
		{
			if (enforceCheck)
			{
				if (r.IsOverflow)
					throw new ArgumentException(paramName: nameof(r), message: "r should not be overflow");
				if (s.IsOverflow)
					throw new ArgumentException(paramName: nameof(s), message: "s should not be overflow");
			}
			else
			{
				VERIFY_CHECK(!r.IsOverflow && !s.IsOverflow);
			}
			this.r = r;
			this.s = s;
		}

		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}
		public const int MaxLength = 74;
		public byte[] ToDER()
		{
			Span<byte> sig = stackalloc byte[74];
			WriteDerToSpan(sig, out int len);
			return sig.Slice(0, len).ToArray();
		}

		public bool TryNormalize(out SecpECDSASignature normalized)
		{
			Scalar s = this.s;
			var ret = s.IsHigh;
			if (ret)
			{
				s = s.Negate();
			}
			normalized = new SecpECDSASignature(r, s, false);
			return ret;
		}

		static int DerReadLen(ref ReadOnlySpan<byte> sig)
		{
			int lenleft, b1;
			int ret = 0;
			if (sig.Length == 0)
			{
				return -1;
			}
			b1 = sig[0];
			sig = sig.Slice(1);
			if (b1 == 0xFF)
			{
				/* X.690-0207 8.1.3.5.c the value 0xFF shall not be used. */
				return -1;
			}
			if ((b1 & 0x80) == 0)
			{
				/* X.690-0207 8.1.3.4 short form length octets */
				return b1;
			}
			if (b1 == 0x80)
			{
				/* Indefinite length is not allowed in DER. */
				return -1;
			}
			/* X.690-207 8.1.3.5 long form length octets */
			lenleft = b1 & 0x7F;
			if (lenleft > sig.Length)
			{
				return -1;
			}
			if (sig[0] == 0)
			{
				/* Not the shortest possible length encoding. */
				return -1;
			}
			if (lenleft > sizeof(uint))
			{
				/* The resulting length would exceed the range of a size_t, so
				 * certainly longer than the passed array size.
				 */
				return -1;
			}
			while (lenleft > 0)
			{
				ret = (ret << 8) | sig[0];
				if (ret + lenleft > sig.Length)
				{
					/* Result exceeds the length of the passed array. */
					return -1;
				}
				sig = sig.Slice(1);
				lenleft--;
			}
			if (ret < 128)
			{
				/* Not the shortest possible length encoding. */
				return -1;
			}
			return ret;
		}

		static bool DerParseInteger(out Scalar r, ref ReadOnlySpan<byte> sig)
		{
			r = default;
			int overflow = 0;
			Span<byte> ra = stackalloc byte[32];
			int rlen;

			if (sig.Length == 0 || sig[0] != 0x02)
			{
				r = default;
				/* Not a primitive integer (X.690-0207 8.3.1). */
				return false;
			}
			sig = sig.Slice(1);
			rlen = DerReadLen(ref sig);
			if (rlen <= 0 || rlen > sig.Length)
			{
				/* Exceeds bounds or not at least length 1 (X.690-0207 8.3.1).  */
				return false;
			}
			if (sig[0] == 0x00 && rlen > 1 && ((sig[1]) & 0x80) == 0x00)
			{
				/* Excessive 0x00 padding. */
				return false;
			}
			if (sig[0] == 0xFF && rlen > 1 && ((sig[1]) & 0x80) == 0x80)
			{
				/* Excessive 0xFF padding. */
				return false;
			}
			if ((sig[0] & 0x80) == 0x80)
			{
				/* Negative. */
				overflow = 1;
			}
			while (rlen > 0 && sig[0] == 0)
			{
				/* Skip leading zero bytes */
				rlen--;
				sig = sig.Slice(1);
			}
			if (rlen > 32)
			{
				overflow = 1;
			}
			if (overflow == 0)
			{
				sig.Slice(0, rlen).CopyTo(ra.Slice(32 - rlen));
				r = new Scalar(ra, out overflow);
			}

			if (overflow == 1)
			{
				r = new Scalar(0);
			}
			sig = sig.Slice(rlen);
			return true;
		}

		public static bool TryCreateFromDer(ReadOnlySpan<byte> sig, [MaybeNullWhen(false)] out SecpECDSASignature output)
		{
			int rlen;
			Scalar rr, rs;
			if (sig.Length == 0 || sig[0] != 0x30)
			{
				/* The encoding doesn't start with a constructed sequence (X.690-0207 8.9.1). */
				output = null;
				return false;
			}
			sig = sig.Slice(1);
			rlen = DerReadLen(ref sig);
			if (rlen < 0 || rlen > sig.Length)
			{
				/* Tuple exceeds bounds */
				output = null;
				return false;
			}
			if (rlen != sig.Length)
			{
				/* Garbage after tuple. */
				output = null;
				return false;
			}

			if (!DerParseInteger(out rr, ref sig))
			{
				output = null;
				return false;
			}
			if (!DerParseInteger(out rs, ref sig))
			{
				output = null;
				return false;
			}

			if (sig.Length != 0)
			{
				/* Trailing garbage inside tuple. */
				output = null;
				return false;
			}
			output = new SecpECDSASignature(rr, rs, false);
			return true;
		}
		public static bool TryCreateFromCompact(ReadOnlySpan<byte> in64, [MaybeNullWhen(false)] out SecpECDSASignature output)
		{
			output = null;
			if (in64.Length != 64)
				return false;
			Scalar r, s;
			bool ret = true;
			int overflow;
			r = new Scalar(in64, out overflow);
			ret &= overflow == 0;
			s = new Scalar(in64.Slice(32), out overflow);
			ret &= overflow == 0;
			if (ret)
			{
				output = new SecpECDSASignature(r, s, false);
				return true;
			}
			else
			{
				output = null;
				return false;
			}
		}

		public bool WriteDerToSpan(Span<byte> sig, out int size)
		{
			size = sig.Length;
			Span<byte> r = stackalloc byte[33];
			r.Fill(0);
			Span<byte> s = stackalloc byte[33];
			s.Fill(0);
			Span<byte> rp = r;
			Span<byte> sp = s;
			int lenR = 33, lenS = 33;
			this.r.WriteToSpan(r.Slice(1));
			this.s.WriteToSpan(s.Slice(1));
			while (lenR > 1 && rp[0] == 0 && rp[1] < 0x80) { lenR--; rp = rp.Slice(1); }
			while (lenS > 1 && sp[0] == 0 && sp[1] < 0x80) { lenS--; sp = sp.Slice(1); }
			if (size < 6 + lenS + lenR)
			{
				size = 6 + lenS + lenR;
				return false;
			}
			size = 6 + lenS + lenR;
			sig[0] = 0x30;
			sig[1] = (byte)(4 + lenS + lenR);
			sig[2] = 0x02;
			sig[3] = (byte)lenR;
			rp.Slice(0, lenR).CopyTo(sig.Slice(4));
			sig[4 + lenR] = 0x02;
			sig[5 + lenR] = (byte)lenS;
			sp.Slice(0, lenS).CopyTo(sig.Slice(lenR + 6));
			return true;
		}

		public void WriteCompactToSpan(Span<byte> out64)
		{
			if (out64.Length != 64)
				throw new ArgumentException(paramName: nameof(out64), message: "out64 should be 64 bytes");
			this.r.WriteToSpan(out64.Slice(0, 32));
			this.s.WriteToSpan(out64.Slice(32));
		}

		public override bool Equals(object? obj)
		{
			if (obj is SecpECDSASignature item)
				return this == item;
			return false;
		}
		public static bool operator ==(SecpECDSASignature? a, SecpECDSASignature? b)
		{
			if (a is SecpECDSASignature aa && b is SecpECDSASignature bb)
			{
				return aa.r == bb.r & aa.s == bb.s;
			}
			return a is null && b is null;
		}

		public static bool operator !=(SecpECDSASignature? a, SecpECDSASignature? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + r.GetHashCode();
				hash = hash * 23 + s.GetHashCode();
				return hash;
			}
		}

		public void Deconstruct(out Scalar r, out Scalar s)
		{
			r = this.r;
			s = this.s;
		}
	}
}
#nullable restore
#endif
