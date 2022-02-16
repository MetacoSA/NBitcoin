#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class SecpRecoverableECDSASignature
	{
		private readonly Scalar r;
		private readonly Scalar s;
		private readonly int recid;
		public SecpRecoverableECDSASignature(SecpECDSASignature sig, int recid)
		{
			if (sig == null)
				throw new ArgumentNullException(nameof(sig));
			this.r = sig.r;
			this.s = sig.s;
			this.recid = recid;
		}

		public static bool TryCreateFromCompact(ReadOnlySpan<byte> in64, int recid, [MaybeNullWhen(false)] out SecpRecoverableECDSASignature sig)
		{
			sig = null;
			if (SecpECDSASignature.TryCreateFromCompact(in64, out var compact) && compact is SecpECDSASignature)
			{
				sig = new SecpRecoverableECDSASignature(compact, recid);
				return true;
			}
			return false;
		}

		public void Deconstruct(out Scalar r, out Scalar s, out int recid)
		{
			r = this.r;
			s = this.s;
			recid = this.recid;
		}

		public void WriteToSpanCompact(Span<byte> out64, out int recid)
		{
			if (out64.Length != 64)
				throw new ArgumentException(paramName: nameof(out64), message: "out64 should be 64 bytes");
			recid = this.recid;
			r.WriteToSpan(out64);
			s.WriteToSpan(out64.Slice(32));
		}

		public SecpECDSASignature ToSignature()
		{
			return new SecpECDSASignature(r, s, false);
		}
	}
}
#nullable disable
#endif
