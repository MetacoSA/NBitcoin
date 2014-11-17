using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinSecret : Base58Data
	{
		public BitcoinSecret(Key key, Network network)
			: base(ToBytes(key), network)
		{
		}

		private static byte[] ToBytes(Key key)
		{
			var keyBytes = key.ToBytes();
			if(!key.IsCompressed)
				return keyBytes;
			else
				return keyBytes.Concat(new byte[] { 0x01 }).ToArray();
		}
		public BitcoinSecret(string base58, Network expectedAddress = null)
			: base(base58, expectedAddress)
		{
		}

        private BitcoinAddress _address; 

		public BitcoinAddress GetAddress()
		{
            if (_address == null)
                _address = Key.PubKey.GetAddress(Network);

            return _address;
		}

		public KeyId ID
		{
			get
			{
				return Key.PubKey.ID;
			}
		}

		public Key Key
		{
			get
			{
				Key ret = new Key(vchData, 32, IsCompressed);
				return ret;
			}
		}

		protected override bool IsValid
		{
			get
			{
				if(vchData.Length != 33 && vchData.Length != 32)
					return false;

				if(vchData.Length == 33 && IsCompressed)
					return true;
				if(vchData.Length == 32 && !IsCompressed)
					return true;
				return false;
			}
		}

		public BitcoinEncryptedSecret Encrypt(string password)
		{
			return Key.GetEncryptedBitcoinSecret(password, Network);
		}


		public BitcoinSecret Copy(bool? compressed)
		{
			if(compressed == null)
				compressed = IsCompressed;

			if(compressed.Value && IsCompressed)
			{
				return new BitcoinSecret(wifData, Network);
			}
			else
			{
				byte[] result = Encoders.Base58Check.DecodeData(wifData);
				var resultList = result.ToList();

				if(compressed.Value)
				{
					resultList.Insert(resultList.Count, 0x1);
				}
				else
				{
					resultList.RemoveAt(resultList.Count - 1);
				}
				return new BitcoinSecret(Encoders.Base58Check.EncodeData(resultList.ToArray()), Network);
			}
		}

		public bool IsCompressed
		{
			get
			{
				return vchData.Length > 32 && vchData[32] == 1;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.SECRET_KEY;
			}
		}
	}
}
