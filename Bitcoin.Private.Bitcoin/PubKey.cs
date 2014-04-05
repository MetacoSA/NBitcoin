using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class PubKey
	{
		public PubKey(byte[] vch)
		{
			if(vch.Length != 65 && vch.Length != 33)
			{
				throw new ArgumentException("Invalid public key size");
			}
			this.vch = vch.ToArray();
			_Key = new ECKey(vch, false);
		}
		byte[] vch = new byte[0];
		ECKey _Key = null;
		KeyId _ID;
		public KeyId ID
		{
			get
			{
				if(_ID == null)
				{
					_ID = new KeyId(Utils.Hash160(vch, vch.Length));
				}
				return _ID;
			}
		}

		public bool IsCompressed
		{
			get
			{
				if(this.vch.Length == 65)
					return false;
				if(this.vch.Length == 33)
					return true;
				throw new NotSupportedException("Invalid public key size");
			}
		}

		BitcoinAddress _Address;
		public BitcoinAddress Address
		{
			get
			{
				if(_Address == null)
				{
					var vchList = this.ID.ToBytes().ToList();
					vchList.Insert(0, 0);
					_Address = new BitcoinAddress(Utils.EncodeBase58Check(vchList.ToArray()));
				}
				return _Address;
			}
		}

		public bool Verify(uint256 hash, ECDSASignature sig)
		{
			return _Key.Verify(hash, sig);
		}

		public string ToHex()
		{
			return Utils.HexStr(vch, false);
		}

		public override string ToString()
		{
			return ToHex();
		}

		public bool VerifyMessage(string message, string signature)
		{
			var pubKey = this.Address.GetPublicKeyFromMessageSignature(message, signature);
			return pubKey.ID == this.ID;
		}
	}
}
