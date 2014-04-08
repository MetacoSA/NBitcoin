using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Signers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class Key
	{
		byte[] vch = new byte[0];
		ECKey _ECKey;
		public bool IsCompressed
		{
			get;
			private set;
		}

		public Key(byte[] data, int count, bool fCompressedIn)
		{
			if(count != 32)
			{
				throw new FormatException("The size of an EC key should be 32");
			}
			if(Check(data))
			{
				vch = new byte[32];
				Array.Copy(data, 0, vch, 0, count);
				IsCompressed = fCompressedIn;
				_ECKey = new ECKey(vch, true);
			}
			else
				throw new FormatException("Invalid EC key");
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

		PubKey _PubKey;
		public PubKey PubKey
		{
			get
			{
				if(_PubKey == null)
				{
					ECKey key = new ECKey(vch, true);
					_PubKey = key.GetPubKey(IsCompressed);
				}
				return _PubKey;
			}
		}

		public ECDSASignature Sign(uint256 hash)
		{
			return _ECKey.Sign(hash);
		}


		public string SignMessage(String message)
		{
			byte[] data = Utils.FormatMessageForSigning(message);
			var hash = Utils.Hash(data);
			return Convert.ToBase64String(SignCompact(hash));
		}


		public byte[] SignCompact(uint256 hash)
		{
			var sig = Sign(hash);
			// Now we have to work backwards to figure out the recId needed to recover the signature.
			int recId = -1;
			for(int i = 0 ; i < 4 ; i++)
			{
				ECKey k = ECKey.RecoverFromSignature(i, sig, hash, IsCompressed);
				if(k != null && k.GetPubKey(IsCompressed).ToHex() == PubKey.ToHex())
				{
					recId = i;
					break;
				}
			}

			if(recId == -1)
				throw new InvalidOperationException("Could not construct a recoverable key. This should never happen.");

			int headerByte = recId + 27 + (IsCompressed ? 4 : 0);

			byte[] sigData = new byte[65];  // 1 header + 32 bytes for R + 32 bytes for S

			sigData[0] = (byte)headerByte;

			Array.Copy(Utils.BigIntegerToBytes(sig.R, 32), 0, sigData, 1, 32);
			Array.Copy(Utils.BigIntegerToBytes(sig.S, 32), 0, sigData, 33, 32);
			return sigData;
		}

		public byte[] ToDER()
		{
			return _ECKey.ToDER(IsCompressed);
		}
	}
}
