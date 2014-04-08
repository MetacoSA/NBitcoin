using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin.DataEncoders
{
	public class Base64Encoder : DataEncoder
	{
		public override byte[] DecodeData(string encoded)
		{
			return Convert.FromBase64String(encoded);
		}
		public override string EncodeData(byte[] data, int length)
		{
			return Convert.ToBase64String(data, 0, length);
		}
	}
}
