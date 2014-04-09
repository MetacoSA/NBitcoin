using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.DataEncoders
{
	public class HexEncoder : DataEncoder
	{
		public bool Space
		{
			get;
			set;
		}
		public override string EncodeData(byte[] data, int length)
		{
			if(data == null)
				throw new ArgumentNullException("data");
			if(length < 0)
				length = data.Length;

			StringBuilder rv = new StringBuilder();
			for(int i = 0 ; i < length ; i++)
			{
				var val = data[i];
				if(Space && i != 0)
					rv.Append(' ');
				rv.Append(hexDigits[val >> 4]);
				rv.Append(hexDigits[val & 15]);
			}

			return rv.ToString();
		}

		public override byte[] DecodeData(string encoded)
		{
			if(encoded == null)
				throw new ArgumentNullException("encoded");

			// convert hex dump to vector
			Queue<byte> vch = new Queue<byte>();

			int i = 0;

			while(true)
			{
				if(i >= encoded.Length)
					break;
				char psz = encoded[i];
				while(IsSpace(psz))
				{
					i++;
					if(i >= encoded.Length)
						break;
					psz = encoded[i];
				}
				if(i >= encoded.Length)
					break;
				psz = encoded[i];

				int c = IsDigit(psz);
				i++;
				if(i >= encoded.Length)
					break;
				psz = encoded[i];
				if(c == -1)
					break;
				int n = (c << 4);
				c = IsDigit(psz);
				i++;
				if(c == -1)
					break;
				n |= c;
				vch.Enqueue((byte)n);
			}
			return vch.ToArray();
		}

		static readonly char[] hexDigits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };
		static readonly byte[] hexValues = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 10, 11, 12, 13, 14, 15 };
		public static int IsDigit(char c)
		{
			var i = Array.IndexOf(hexDigits, c);
			if(i == -1)
				return -1;
			return hexValues[i];
		}

		public static bool IsWellFormed(string str)
		{
			foreach(var c in str)
			{
				if(IsDigit(c) < 0)
					return false;
			}
			return (str.Length > 0) && (str.Length % 2 == 0);
		}
	}
}
