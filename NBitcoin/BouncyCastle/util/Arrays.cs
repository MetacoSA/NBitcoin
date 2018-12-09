using System;
using System.Text;

using NBitcoin.BouncyCastle.Math;

namespace NBitcoin.BouncyCastle.Utilities
{
	/// <summary> General array utilities.</summary>
	internal abstract class Arrays
	{
		public static bool AreEqual(
			bool[] a,
			bool[] b)
		{
			if(a == b)
				return true;

			if(a == null || b == null)
				return false;

			return HaveSameContents(a, b);
		}

		public static bool AreEqual(
			char[] a,
			char[] b)
		{
			if(a == b)
				return true;

			if(a == null || b == null)
				return false;

			return HaveSameContents(a, b);
		}

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
			if(a == b)
				return true;

			if(a == null || b == null)
				return false;

			return HaveSameContents(a, b);
		}

		[Obsolete("Use 'AreEqual' method instead")]
		public static bool AreSame(
			byte[] a,
			byte[] b)
		{
			return AreEqual(a, b);
		}

		/// <summary>
		/// A constant time equals comparison - does not terminate early if
		/// test will fail.
		/// </summary>
		/// <param name="a">first array</param>
		/// <param name="b">second array</param>
		/// <returns>true if arrays equal, false otherwise.</returns>
		public static bool ConstantTimeAreEqual(
			byte[] a,
			byte[] b)
		{
			int i = a.Length;
			if(i != b.Length)
				return false;
			int cmp = 0;
			while(i != 0)
			{
				--i;
				cmp |= (a[i] ^ b[i]);
			}
			return cmp == 0;
		}

		public static bool AreEqual(
			int[] a,
			int[] b)
		{
			if(a == b)
				return true;

			if(a == null || b == null)
				return false;

			return HaveSameContents(a, b);
		}

		
		public static bool AreEqual(uint[] a, uint[] b)
		{
			if(a == b)
				return true;

			if(a == null || b == null)
				return false;

			return HaveSameContents(a, b);
		}

		private static bool HaveSameContents(
			bool[] a,
			bool[] b)
		{
			int i = a.Length;
			if(i != b.Length)
				return false;
			while(i != 0)
			{
				--i;
				if(a[i] != b[i])
					return false;
			}
			return true;
		}

		private static bool HaveSameContents(
			char[] a,
			char[] b)
		{
			int i = a.Length;
			if(i != b.Length)
				return false;
			while(i != 0)
			{
				--i;
				if(a[i] != b[i])
					return false;
			}
			return true;
		}

		private static bool HaveSameContents(
			byte[] a,
			byte[] b)
		{
			int i = a.Length;
			if(i != b.Length)
				return false;
			while(i != 0)
			{
				--i;
				if(a[i] != b[i])
					return false;
			}
			return true;
		}

		private static bool HaveSameContents(
			int[] a,
			int[] b)
		{
			int i = a.Length;
			if(i != b.Length)
				return false;
			while(i != 0)
			{
				--i;
				if(a[i] != b[i])
					return false;
			}
			return true;
		}

		private static bool HaveSameContents(uint[] a, uint[] b)
		{
			int i = a.Length;
			if(i != b.Length)
				return false;
			while(i != 0)
			{
				--i;
				if(a[i] != b[i])
					return false;
			}
			return true;
		}

		public static string ToString(
			object[] a)
		{
			StringBuilder sb = new StringBuilder('[');
			if(a.Length > 0)
			{
				sb.Append(a[0]);
				for(int index = 1; index < a.Length; ++index)
				{
					sb.Append(", ").Append(a[index]);
				}
			}
			sb.Append(']');
			return sb.ToString();
		}

		public static int GetHashCode(byte[] data)
		{
			if(data == null)
			{
				return 0;
			}

			int i = data.Length;
			int hc = i + 1;

			while(--i >= 0)
			{
				hc *= 257;
				hc ^= data[i];
			}

			return hc;
		}

		public static int GetHashCode(byte[] data, int off, int len)
		{
			if(data == null)
			{
				return 0;
			}

			int i = len;
			int hc = i + 1;

			while(--i >= 0)
			{
				hc *= 257;
				hc ^= data[off + i];
			}

			return hc;
		}

		public static int GetHashCode(int[] data)
		{
			if(data == null)
				return 0;

			int i = data.Length;
			int hc = i + 1;

			while(--i >= 0)
			{
				hc *= 257;
				hc ^= data[i];
			}

			return hc;
		}

		public static int GetHashCode(int[] data, int off, int len)
		{
			if(data == null)
				return 0;

			int i = len;
			int hc = i + 1;

			while(--i >= 0)
			{
				hc *= 257;
				hc ^= data[off + i];
			}

			return hc;
		}

		
		public static int GetHashCode(uint[] data)
		{
			if(data == null)
				return 0;

			int i = data.Length;
			int hc = i + 1;

			while(--i >= 0)
			{
				hc *= 257;
				hc ^= (int)data[i];
			}

			return hc;
		}

		
		public static int GetHashCode(uint[] data, int off, int len)
		{
			if(data == null)
				return 0;

			int i = len;
			int hc = i + 1;

			while(--i >= 0)
			{
				hc *= 257;
				hc ^= (int)data[off + i];
			}

			return hc;
		}

		
		public static int GetHashCode(ulong[] data)
		{
			if(data == null)
				return 0;

			int i = data.Length;
			int hc = i + 1;

			while(--i >= 0)
			{
				ulong di = data[i];
				hc *= 257;
				hc ^= (int)di;
				hc *= 257;
				hc ^= (int)(di >> 32);
			}

			return hc;
		}

		
		public static int GetHashCode(ulong[] data, int off, int len)
		{
			if(data == null)
				return 0;

			int i = len;
			int hc = i + 1;

			while(--i >= 0)
			{
				ulong di = data[off + i];
				hc *= 257;
				hc ^= (int)di;
				hc *= 257;
				hc ^= (int)(di >> 32);
			}

			return hc;
		}

		public static byte[] Clone(
			byte[] data)
		{
			return data == null ? null : (byte[])data.Clone();
		}

		public static byte[] Clone(
			byte[] data,
			byte[] existing)
		{
			if(data == null)
			{
				return null;
			}
			if((existing == null) || (existing.Length != data.Length))
			{
				return Clone(data);
			}
			Array.Copy(data, 0, existing, 0, existing.Length);
			return existing;
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

		
		public static ulong[] Clone(
			ulong[] data)
		{
			return data == null ? null : (ulong[])data.Clone();
		}

		
		public static ulong[] Clone(
			ulong[] data,
			ulong[] existing)
		{
			if(data == null)
			{
				return null;
			}
			if((existing == null) || (existing.Length != data.Length))
			{
				return Clone(data);
			}
			Array.Copy(data, 0, existing, 0, existing.Length);
			return existing;
		}

		public static bool Contains(byte[] a, byte n)
		{
			for(int i = 0; i < a.Length; ++i)
			{
				if(a[i] == n)
					return true;
			}
			return false;
		}

		public static bool Contains(short[] a, short n)
		{
			for(int i = 0; i < a.Length; ++i)
			{
				if(a[i] == n)
					return true;
			}
			return false;
		}

		public static bool Contains(int[] a, int n)
		{
			for(int i = 0; i < a.Length; ++i)
			{
				if(a[i] == n)
					return true;
			}
			return false;
		}

		public static void Fill(
			byte[] buf,
			byte b)
		{
			int i = buf.Length;
			while(i > 0)
			{
				buf[--i] = b;
			}
		}

		public static byte[] CopyOf(byte[] data, int newLength)
		{
			byte[] tmp = new byte[newLength];
			Array.Copy(data, 0, tmp, 0, System.Math.Min(newLength, data.Length));
			return tmp;
		}

		public static char[] CopyOf(char[] data, int newLength)
		{
			char[] tmp = new char[newLength];
			Array.Copy(data, 0, tmp, 0, System.Math.Min(newLength, data.Length));
			return tmp;
		}

		public static int[] CopyOf(int[] data, int newLength)
		{
			int[] tmp = new int[newLength];
			Array.Copy(data, 0, tmp, 0, System.Math.Min(newLength, data.Length));
			return tmp;
		}

		public static long[] CopyOf(long[] data, int newLength)
		{
			long[] tmp = new long[newLength];
			Array.Copy(data, 0, tmp, 0, System.Math.Min(newLength, data.Length));
			return tmp;
		}

		public static BigInteger[] CopyOf(BigInteger[] data, int newLength)
		{
			BigInteger[] tmp = new BigInteger[newLength];
			Array.Copy(data, 0, tmp, 0, System.Math.Min(newLength, data.Length));
			return tmp;
		}

		/**
         * Make a copy of a range of bytes from the passed in data array. The range can
         * extend beyond the end of the input array, in which case the return array will
         * be padded with zeros.
         *
         * @param data the array from which the data is to be copied.
         * @param from the start index at which the copying should take place.
         * @param to the final index of the range (exclusive).
         *
         * @return a new byte array containing the range given.
         */
		public static byte[] CopyOfRange(byte[] data, int from, int to)
		{
			int newLength = GetLength(from, to);
			byte[] tmp = new byte[newLength];
			Array.Copy(data, from, tmp, 0, System.Math.Min(newLength, data.Length - from));
			return tmp;
		}

		public static int[] CopyOfRange(int[] data, int from, int to)
		{
			int newLength = GetLength(from, to);
			int[] tmp = new int[newLength];
			Array.Copy(data, from, tmp, 0, System.Math.Min(newLength, data.Length - from));
			return tmp;
		}

		public static long[] CopyOfRange(long[] data, int from, int to)
		{
			int newLength = GetLength(from, to);
			long[] tmp = new long[newLength];
			Array.Copy(data, from, tmp, 0, System.Math.Min(newLength, data.Length - from));
			return tmp;
		}

		public static BigInteger[] CopyOfRange(BigInteger[] data, int from, int to)
		{
			int newLength = GetLength(from, to);
			BigInteger[] tmp = new BigInteger[newLength];
			Array.Copy(data, from, tmp, 0, System.Math.Min(newLength, data.Length - from));
			return tmp;
		}

		private static int GetLength(int from, int to)
		{
			int newLength = to - from;
			if(newLength < 0)
				throw new ArgumentException(from + " > " + to);
			return newLength;
		}

		public static byte[] Append(byte[] a, byte b)
		{
			if(a == null)
				return new byte[] { b };

			int length = a.Length;
			byte[] result = new byte[length + 1];
			Array.Copy(a, 0, result, 0, length);
			result[length] = b;
			return result;
		}

		public static short[] Append(short[] a, short b)
		{
			if(a == null)
				return new short[] { b };

			int length = a.Length;
			short[] result = new short[length + 1];
			Array.Copy(a, 0, result, 0, length);
			result[length] = b;
			return result;
		}

		public static int[] Append(int[] a, int b)
		{
			if(a == null)
				return new int[] { b };

			int length = a.Length;
			int[] result = new int[length + 1];
			Array.Copy(a, 0, result, 0, length);
			result[length] = b;
			return result;
		}

		public static byte[] Concatenate(byte[] a, byte[] b)
		{
			if(a == null)
				return Clone(b);
			if(b == null)
				return Clone(a);

			byte[] rv = new byte[a.Length + b.Length];
			Array.Copy(a, 0, rv, 0, a.Length);
			Array.Copy(b, 0, rv, a.Length, b.Length);
			return rv;
		}

		public static byte[] ConcatenateAll(params byte[][] vs)
		{
			byte[][] nonNull = new byte[vs.Length][];
			int count = 0;
			int totalLength = 0;

			for(int i = 0; i < vs.Length; ++i)
			{
				byte[] v = vs[i];
				if(v != null)
				{
					nonNull[count++] = v;
					totalLength += v.Length;
				}
			}

			byte[] result = new byte[totalLength];
			int pos = 0;

			for(int j = 0; j < count; ++j)
			{
				byte[] v = nonNull[j];
				Array.Copy(v, 0, result, pos, v.Length);
				pos += v.Length;
			}

			return result;
		}

		public static int[] Concatenate(int[] a, int[] b)
		{
			if(a == null)
				return Clone(b);
			if(b == null)
				return Clone(a);

			int[] rv = new int[a.Length + b.Length];
			Array.Copy(a, 0, rv, 0, a.Length);
			Array.Copy(b, 0, rv, a.Length, b.Length);
			return rv;
		}

		public static byte[] Prepend(byte[] a, byte b)
		{
			if(a == null)
				return new byte[] { b };

			int length = a.Length;
			byte[] result = new byte[length + 1];
			Array.Copy(a, 0, result, 1, length);
			result[0] = b;
			return result;
		}

		public static short[] Prepend(short[] a, short b)
		{
			if(a == null)
				return new short[] { b };

			int length = a.Length;
			short[] result = new short[length + 1];
			Array.Copy(a, 0, result, 1, length);
			result[0] = b;
			return result;
		}

		public static int[] Prepend(int[] a, int b)
		{
			if(a == null)
				return new int[] { b };

			int length = a.Length;
			int[] result = new int[length + 1];
			Array.Copy(a, 0, result, 1, length);
			result[0] = b;
			return result;
		}

		public static byte[] Reverse(byte[] a)
		{
			if(a == null)
				return null;

			int p1 = 0, p2 = a.Length;
			byte[] result = new byte[p2];

			while(--p2 >= 0)
			{
				result[p2] = a[p1++];
			}

			return result;
		}

		public static int[] Reverse(int[] a)
		{
			if(a == null)
				return null;

			int p1 = 0, p2 = a.Length;
			int[] result = new int[p2];

			while(--p2 >= 0)
			{
				result[p2] = a[p1++];
			}

			return result;
		}
	}
}
