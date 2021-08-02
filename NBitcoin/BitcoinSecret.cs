using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinSecret : Base58Data, ISecret, IDestination
	{
		public BitcoinSecret(Key key, Network network)
			: base(ToBytes(key), network)
		{
		}

		private static byte[] ToBytes(Key key)
		{
			var keyBytes = key.ToBytes();
			if (!key.IsCompressed)
				return keyBytes;
			else
				return keyBytes.Concat(new byte[] { 0x01 }).ToArray();
		}
		public BitcoinSecret(string base58, Network expectedNetwork)
		{
			Init<BitcoinSecret>(base58, expectedNetwork);
		}

		public BitcoinAddress GetAddress(ScriptPubKeyType type)
		{
			return PrivateKey.PubKey.GetAddress(type, Network);
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
				if (vchData.Length != 33 && vchData.Length != 32)
					return false;

				if (vchData.Length == 33 && IsCompressed)
					return true;
				if (vchData.Length == 32 && !IsCompressed)
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
			if (compressed == null)
				compressed = IsCompressed;

			if (compressed.Value && IsCompressed)
			{
				return new BitcoinSecret(wifData, Network);
			}
			else
			{
				var enc = Network.NetworkStringParser.GetBase58CheckEncoder();
				byte[] result = enc.DecodeData(wifData);
				var resultList = result.ToList();

				if (compressed.Value)
				{
					resultList.Insert(resultList.Count, 0x1);
				}
				else
				{
					resultList.RemoveAt(resultList.Count - 1);
				}
				return new BitcoinSecret(enc.EncodeData(resultList.ToArray()), Network);
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

		Script IDestination.ScriptPubKey => ((IDestination)PrivateKey).ScriptPubKey;
	}
}
