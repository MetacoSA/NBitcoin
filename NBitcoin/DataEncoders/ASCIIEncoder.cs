using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.DataEncoders
{
	public class ASCIIEncoder : DataEncoder
	{
		//Do not using Encoding.ASCII (not portable)
		public override byte[] DecodeData(string encoded)
		{
			if(String.IsNullOrEmpty(encoded))
				return new byte[0];
			return encoded.ToCharArray().Select(o => (byte)o).ToArray();
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			return new String(data.Skip(offset).Take(count).Select(o => (char)o).ToArray()).Replace("\0", "");
		}
	}
}
