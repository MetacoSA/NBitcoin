using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin.Tests
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
	}
}
