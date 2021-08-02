namespace NBitcoin.BouncyCastle.Crypto.Utilities
{
	internal static class Pack
	{

		internal static void UInt32_To_BE(uint n, byte[] bs, int off)
		{
			bs[off] = (byte)(n >> 24);
			bs[off + 1] = (byte)(n >> 16);
			bs[off + 2] = (byte)(n >> 8);
			bs[off + 3] = (byte)(n);
		}

		internal static uint BE_To_UInt32(byte[] bs, int off)
		{
			return (uint)bs[off] << 24
				| (uint)bs[off + 1] << 16
				| (uint)bs[off + 2] << 8
				| (uint)bs[off + 3];
		}

		internal static void BE_To_UInt32(byte[] bs, int off, uint[] ns)
		{
			for (int i = 0; i < ns.Length; ++i)
			{
				ns[i] = BE_To_UInt32(bs, off);
				off += 4;
			}
		}

		internal static void UInt64_To_BE(ulong n, byte[] bs, int off)
		{
			UInt32_To_BE((uint)(n >> 32), bs, off);
			UInt32_To_BE((uint)(n), bs, off + 4);
		}

		internal static ulong BE_To_UInt64(byte[] bs, int off)
		{
			uint hi = BE_To_UInt32(bs, off);
			uint lo = BE_To_UInt32(bs, off + 4);
			return ((ulong)hi << 32) | (ulong)lo;
		}

		internal static void UInt16_To_LE(ushort n, byte[] bs, int off)
		{
			bs[off] = (byte)(n);
			bs[off + 1] = (byte)(n >> 8);
		}

		internal static ushort LE_To_UInt16(byte[] bs)
		{
			uint n = (uint)bs[0]
				| (uint)bs[1] << 8;
			return (ushort)n;
		}

		internal static ushort LE_To_UInt16(byte[] bs, int off)
		{
			uint n = (uint)bs[off]
				| (uint)bs[off + 1] << 8;
			return (ushort)n;
		}

		internal static byte[] UInt32_To_LE(uint n)
		{
			byte[] bs = new byte[4];
			UInt32_To_LE(n, bs, 0);
			return bs;
		}

		internal static void UInt32_To_LE(uint n, byte[] bs)
		{
			bs[0] = (byte)(n);
			bs[1] = (byte)(n >> 8);
			bs[2] = (byte)(n >> 16);
			bs[3] = (byte)(n >> 24);
		}

		internal static void UInt32_To_LE(uint n, byte[] bs, int off)
		{
			bs[off] = (byte)(n);
			bs[off + 1] = (byte)(n >> 8);
			bs[off + 2] = (byte)(n >> 16);
			bs[off + 3] = (byte)(n >> 24);
		}

		internal static byte[] UInt32_To_LE(uint[] ns)
		{
			byte[] bs = new byte[4 * ns.Length];
			UInt32_To_LE(ns, bs, 0);
			return bs;
		}

		internal static void UInt32_To_LE(uint[] ns, byte[] bs, int off)
		{
			for (int i = 0; i < ns.Length; ++i)
			{
				UInt32_To_LE(ns[i], bs, off);
				off += 4;
			}
		}

		internal static uint LE_To_UInt32(byte[] bs, int off)
		{
			return (uint)bs[off]
				| (uint)bs[off + 1] << 8
				| (uint)bs[off + 2] << 16
				| (uint)bs[off + 3] << 24;
		}

		internal static void UInt64_To_LE(ulong n, byte[] bs, int off)
		{
			UInt32_To_LE((uint)(n), bs, off);
			UInt32_To_LE((uint)(n >> 32), bs, off + 4);
		}

		internal static void UInt64_To_LE(ulong[] ns, int nsOff, int nsLen, byte[] bs, int bsOff)
		{
			for (int i = 0; i < nsLen; ++i)
			{
				UInt64_To_LE(ns[nsOff + i], bs, bsOff);
				bsOff += sizeof(ulong);
			}
		}

		internal static ulong LE_To_UInt64(byte[] bs, int off)
		{
			uint lo = LE_To_UInt32(bs, off);
			uint hi = LE_To_UInt32(bs, off + 4);
			return ((ulong)hi << 32) | (ulong)lo;
		}
	}
}
