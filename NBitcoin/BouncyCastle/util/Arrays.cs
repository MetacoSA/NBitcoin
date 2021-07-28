using System;
using System.Text;

namespace NBitcoin.BouncyCastle.Utilities
{
	/// <summary> General array utilities.</summary>
	internal abstract class Arrays
	{
		/// <summary>
		/// Are two arrays equal.
		/// </summary>
		/// <param name="a">Left side.</param>
		/// <param name="b">Right side.</param>
		/// <returns>True if equal.</returns>
		public static bool AreEqual(
			byte[] a,
			byte[] b)
		{
			if (a == b)
				return true;

			if (a == null || b == null)
				return false;

			return HaveSameContents(a, b);
		}

		public static bool AreEqual(
			int[] a,
			int[] b)
		{
			if (a == b)
				return true;

			if (a == null || b == null)
				return false;

			return HaveSameContents(a, b);
		}

		private static bool HaveSameContents(
			byte[] a,
			byte[] b)
		{
			int i = a.Length;
			if (i != b.Length)
				return false;
			while (i != 0)
			{
				--i;
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		private static bool HaveSameContents(
			int[] a,
			int[] b)
		{
			int i = a.Length;
			if (i != b.Length)
				return false;
			while (i != 0)
			{
				--i;
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		public static int GetHashCode(byte[] data)
		{
			if (data == null)
			{
				return 0;
			}

			int i = data.Length;
			int hc = i + 1;

			while (--i >= 0)
			{
				hc *= 257;
				hc ^= data[i];
			}

			return hc;
		}

		public static int GetHashCode(int[] data)
		{
			if (data == null)
				return 0;

			int i = data.Length;
			int hc = i + 1;

			while (--i >= 0)
			{
				hc *= 257;
				hc ^= data[i];
			}

			return hc;
		}

		public static int GetHashCode(uint[] data, int off, int len)
		{
			if (data == null)
				return 0;

			int i = len;
			int hc = i + 1;

			while (--i >= 0)
			{
				hc *= 257;
				hc ^= (int)data[off + i];
			}

			return hc;
		}

		public static byte[] Clone(
			byte[] data)
		{
			return data == null ? null : (byte[])data.Clone();
		}

		public static int[] Clone(
			int[] data)
		{
			return data == null ? null : (int[])data.Clone();
		}

		internal static uint[] Clone(uint[] data)
		{
			return data == null ? null : (uint[])data.Clone();
		}

		public static long[] Clone(long[] data)
		{
			return data == null ? null : (long[])data.Clone();
		}

		public static void Fill(
			byte[] buf,
			byte b)
		{
			int i = buf.Length;
			while (i > 0)
			{
				buf[--i] = b;
			}
		}
	}
}
