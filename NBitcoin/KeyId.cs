using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class KeyId : uint160
	{
		public KeyId()
			: base(0)
		{

		}

		public KeyId(byte[] value)
			: base(value, false)
		{

		}
		public KeyId(uint160 value)
			: base(value.ToBytes(false))
		{

		}

		public KeyId(string value)
			: base(value)
		{
		}

		public byte[] ToBytes()
		{
			return base.ToBytes(false);
		}
	}
}
