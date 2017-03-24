using System.Linq;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	public class BitcoinSecret : Base58Data, IDestination, ISecret
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

		private BitcoinPubKeyAddress _address;

		public BitcoinPubKeyAddress GetAddress()
		{
			return _address ?? (_address = PrivateKey.PubKey.GetAddress(Network));
		}

		public virtual KeyId PubKeyHash
		{
			get
			{
				return PrivateKey.PubKey.Hash;
			}
		}

		public PubKey PubKey
		{
			get
			{
				return PrivateKey.PubKey;
			}
		}

		#region ISecret Members
		Key _Key;
		public Key PrivateKey
		{
			get
			{
				return _Key ?? (_Key = new Key(vchData, 32, IsCompressed));
			}
		}
		#endregion

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
			return PrivateKey.GetEncryptedBitcoinSecret(password, Network);
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

		#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				return GetAddress().ScriptPubKey;
			}
		}

		#endregion


	}
}
