using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.DataEncoders
{
	public class HexEncoder : DataEncoder
	{
		public bool Space
		{
			get;
			set;
		}

		private static readonly string[] HexTbl = Enumerable.Range(0, 256).Select(v => v.ToString("x2")).ToArray();

		public override string EncodeData(byte[] data, int offset, int count)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var spaces = (Space ? Math.Max((count - 1), 0) : 0);

#if !HAS_SPAN
			int pos = 0;
			var s = new char[2 * count + spaces];
			for (var i = offset; i < offset + count; i++)
			{
				if (Space && i != 0)
					s[pos++] = ' ';
				var c = HexTbl[data[i]];
				s[pos++] = c[0];
				s[pos++] = c[1];
			}
			return new string(s);
#else
			return string.Create(2 * count + spaces, (offset, count, data), CreateHexString);
#endif
		}

#if HAS_SPAN
		void CreateHexString(Span<char> s, (int offset, int count, byte[] data) state)
		{
			int pos = 0;
			for (var i = state.offset; i < state.offset + state.count; i++)
			{
				if (Space && i != 0)
					s[pos++] = ' ';
				var c = HexTbl[state.data[i]];
				s[pos++] = c[0];
				s[pos++] = c[1];
			}
		}
#endif

		public override byte[] DecodeData(string encoded)
		{
			if (encoded == null)
				throw new ArgumentNullException(nameof(encoded));
			if (encoded.Length % 2 == 1)
				throw new FormatException("Invalid Hex String");

			var result = new byte[encoded.Length / 2];
			for (int i = 0, j = 0; i < encoded.Length; i += 2, j++)
			{
				var a = IsDigit(encoded[i]);
				var b = IsDigit(encoded[i + 1]);
				if (a == -1 || b == -1)
					throw new FormatException("Invalid Hex String");
				result[j] = (byte)(((uint)a << 4) | (uint)b);
			}
			return result;
		}
#if HAS_SPAN
		public void DecodeData(string encoded, Span<byte> output)
		{
			if (encoded == null)
				throw new ArgumentNullException(nameof(encoded));
			if (encoded.Length % 2 == 1)
				throw new FormatException("Invalid Hex String");
			if (output.Length < (encoded.Length >> 1))
				throw new ArgumentException("output should be bigger", nameof(output));
			try
			{
				for (int i = 0, j = 0; i < encoded.Length; i += 2, j++)
				{
					var a = IsDigit(encoded[i]);
					var b = IsDigit(encoded[i + 1]);
					if (a == -1 || b == -1)
						throw new FormatException("Invalid Hex String");
					output[j] = (byte)(((uint)a << 4) | (uint)b);
				}
			}
			catch(IndexOutOfRangeException) { throw new FormatException("Invalid Hex String"); }
		}
#endif

		static HexEncoder()
		{
			var hexDigits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f',
																				  'A', 'B', 'C', 'D', 'E', 'F' };
			var hexValues = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
																   10, 11, 12, 13, 14, 15};

			var max = hexDigits.Max();
			hexValueArray = new int[max + 1];
			for (int i = 0; i < hexValueArray.Length; i++)
			{
				var idx = Array.IndexOf(hexDigits, (char)i);
				var value = -1;
				if (idx != -1)
					value = hexValues[idx];
				hexValueArray[i] = value;
			}
		}

		public bool IsValid(string str)
		{
			if (str.Length % 2 != 0)
				return false;
			for (int i = 0; i < str.Length; i++)
			{
				if (IsDigit(str[i]) == -1)
					return false;
			}
			return true;
		}

		static readonly int[] hexValueArray;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IsDigit(char c)
		{
			if ('0' <= c && c <= '9')
			{
				return c - '0';
			}
			else if ('a' <= c && c <= 'f')
			{
				return c - 'a' + 10;
			}
			else if ('A' <= c && c <= 'F')
			{
				return c - 'A' + 10;
			}
			else
			{
				return -1;
			}
		}

		public static bool IsWellFormed(string str)
		{
			return ((HexEncoder)Encoders.Hex).IsValid(str);
		}
	}
}
