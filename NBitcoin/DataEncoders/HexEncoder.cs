using System;
using System.Collections.Generic;
using System.Linq;
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
			if(data == null)
				throw new ArgumentNullException("data");

			int pos = 0;
			var spaces = (Space ? Math.Max((count - 1), 0) : 0);
			var s = new char[2 * count + spaces];
			for(var i = offset ; i < offset + count ; i++)
			{
				if(Space && i != 0)
					s[pos++] = ' ';
				var c = HexTbl[data[i]];
				s[pos++] = c[0];
				s[pos++] = c[1];
			}
			return new string(s);
		}

		public override byte[] DecodeData(string encoded)
		{
			if(encoded == null)
				throw new ArgumentNullException("encoded");

			var result = new List<byte>(encoded.Length);
			var i = 0;
			while (i < encoded.Length)
			{
				if(IsSpace(encoded[i]))
				{
					i++;
					continue;
				}
				var a = IsDigit(encoded[i++]);
				if(i >= encoded.Length)
					throw new FormatException("Invalid Hex String");

				var b = IsDigit(encoded[i++]);
				if(a == -1 || b == -1)
					throw new FormatException("Invalid Hex String");
				result.Add(((byte)(((uint)a << 4) | (uint)b)));
			}
			return result.ToArray();
		}

		static HexEncoder()
		{
			var hexDigits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 
																				  'A', 'B', 'C', 'D', 'E', 'F' };
			var hexValues = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 
																   10, 11, 12, 13, 14, 15};

			var max = hexDigits.Max();
			hexValueArray = new int[max + 1];
			for(int i = 0 ; i < hexValueArray.Length ; i++)
			{
				var idx = Array.IndexOf(hexDigits, (char)i);
				var value = -1;
				if(idx != -1)
					value = hexValues[idx];
				hexValueArray[i] = value;
			}
		}

		static readonly int[] hexValueArray;

		public static int IsDigit(char c)
		{
			if((int)c + 1 > hexValueArray.Length)
				return -1;
			return hexValueArray[(int)c];
		}

		public static bool IsWellFormed(string str)
		{
			try
			{
				Encoders.Hex.DecodeData(str);
				return true;
			}
			catch(FormatException)
			{
				return false;
			}
		}
	}
}
