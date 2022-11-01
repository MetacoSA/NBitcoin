#if HAS_SPAN
#nullable enable
using NBitcoin.Secp256k1;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class DLEQProof
	{
		internal static void secp256k1_dleq_serialize_point(Span<byte> buf33, in GE p)
		{
			var y = p.y.Normalize();
			buf33[0] = y.IsOdd ? GE.SECP256K1_TAG_PUBKEY_ODD : GE.SECP256K1_TAG_PUBKEY_EVEN;
			var x = p.x.Normalize();
			x.WriteToSpan(buf33.Slice(1));
		}

		internal static bool secp256k1_dleq_deserialize_point(ReadOnlySpan<byte> buf33, out GE p)
		{
			if (!FE.TryCreate(buf33.Slice(1), out var x))
			{
				p = default;
				return false;
			}
			if (buf33[0] != GE.SECP256K1_TAG_PUBKEY_ODD && buf33[0] != GE.SECP256K1_TAG_PUBKEY_EVEN)
			{
				p = default;
				return false;
			}
			if (!GE.TryCreateXOVariable(x, buf33[0] == GE.SECP256K1_TAG_PUBKEY_ODD, out p))
				return false;
			return true;
		}


		public readonly Scalar b, c;
		public DLEQProof(in Scalar b, in Scalar c)
		{
			this.b = b;
			this.c = c;
		}

		public static bool TryCreate(ReadOnlySpan<byte> in64, [MaybeNullWhen(false)] out DLEQProof proof)
		{
			var s = new Scalar(in64, out var overflow);
			if (overflow != 0)
			{
				proof = default;
				return false;
			}
			var e = new Scalar(in64.Slice(32), out overflow);
			if (overflow != 0)
			{
				proof = default;
				return false;
			}
			proof = new DLEQProof(s, e);
			return true;
		}

		public void WriteToSpan(Span<byte> out64)
		{
			b.WriteToSpan(out64);
			c.WriteToSpan(out64.Slice(32));
		}

		public byte[] ToBytes()
		{
			byte[] buf = new byte[64];
			WriteToSpan(buf);
			return buf;
		}
	}
}
#endif
