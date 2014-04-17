using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	class TestUtils
	{
		public static byte[] ToBytes(string str)
		{
			byte[] result = new byte[str.Length];
			for(int i = 0 ; i < str.Length ; i++)
			{
				result[i] = (byte)str[i];
			}
			return result;
		}

		internal static bool TupleEquals<T1, T2>(Tuple<T1, T2> a, Tuple<T1, T2> b)
		{
			return a.Item1.Equals(b.Item1) && a.Item2.Equals(b.Item2);
		}

		internal static byte[] ParseHex(string data)
		{
			return Encoders.Hex.DecodeData(data);
		}
	}
}
