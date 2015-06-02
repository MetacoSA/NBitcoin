using System;
using System.Text;

namespace NBitcoin.DataEncoders
{
	public class ASCIIEncoder : DataEncoder
	{
		public override byte[] DecodeData(string encoded)
		{
			if (encoded == null)
			{
				throw new ArgumentNullException("encoded");
			}

			return String.IsNullOrEmpty(encoded) 
				? new byte[0] 
				: Encoding.ASCII.GetBytes(encoded);
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			return Encoding.ASCII.GetString(data, offset, count).Replace("\0", "");
		}
	}
}
