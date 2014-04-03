using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class PubKey
	{
		byte[] vch = new byte[0];
		public KeyId GetID()
		{
			return new KeyId(Utils.Hash160(vch,vch.Length));
		}

		public void Set(byte[] data)
		{
			vch = data.ToArray();
		}
	}
}
