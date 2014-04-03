using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class Key
	{
		bool fValid = false;
		byte[] vch = new byte[0];
		public bool IsCompressed
		{
			get;
			private set;
		}

		public void Set(byte[] data, int count, bool fCompressedIn)
		{
			if(count != 32)
			{
				fValid = false;
				return;
			}
			if(Check(data))
			{
				vch = new byte[32];
				Array.Copy(data, 0, vch, 0, count);
				IsCompressed = fCompressedIn;
			}
			else
				fValid = false;
		}

		private bool Check(byte[] vch)
		{
			// Do not convert to OpenSSL's data structures for range-checking keys,
			// it's easy enough to do directly.
			byte[] vchMax = new byte[32]{
        0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
        0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFE,
        0xBA,0xAE,0xDC,0xE6,0xAF,0x48,0xA0,0x3B,
        0xBF,0xD2,0x5E,0x8C,0xD0,0x36,0x41,0x40
    };
			bool fIsZero = true;
			for(int i = 0 ; i < 32 && fIsZero ; i++)
				if(vch[i] != 0)
					fIsZero = false;
			if(fIsZero)
				return false;
			for(int i = 0 ; i < 32 ; i++)
			{
				if(vch[i] < vchMax[i])
					return true;
				if(vch[i] > vchMax[i])
					return false;
			}
			return true;
		}

		public PubKey GetPubKey()
		{
			ECKey key = new ECKey();
			key.SetSecretBytes(vch);
			PubKey pubkey = new PubKey();
			key.GetPubKey(pubkey, IsCompressed);
			return pubkey;
		}
	}
}
