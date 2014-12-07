using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.DataEncoders
{
	public class ASCIIEncoder : DataEncoder
	{
		public override byte[] DecodeData(string encoded)
		{
			if(String.IsNullOrEmpty(encoded))
				return new byte[0];
			return encoded.ToCharArray().Select(o => (byte)o).ToArray();
		}
		public override string EncodeData(byte[] data, int length)
		{
			return new String(data.Take(length).Select(o => (char)o).ToArray()).Replace("\0", "");
		}
	}
}
