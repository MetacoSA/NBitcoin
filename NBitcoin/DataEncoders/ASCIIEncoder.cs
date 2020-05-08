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
			if (String.IsNullOrEmpty(encoded))
				return new byte[0];
#if HAS_SPAN
			Span<byte> r = encoded.Length is int v && v > 256 ? new byte[v] : stackalloc byte[v];
#else
			var r = new byte[encoded.Length];
#endif
			for (int i = 0; i < r.Length; i++)
			{
				r[i] = (byte)encoded[i];
			}
#if HA_SPAN
			return r;
#else
			return r.ToArray();
#endif
		}

		public override string EncodeData(byte[] data, int offset, int count)
		{
			return new String(data.Skip(offset).Take(count).Select(o => (char)o).ToArray()).Replace("\0", "");
		}
	}
}
