using NBitcoin.Crypto;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinStealthAddress : Base58Data
	{
		public BitcoinStealthAddress(string base58, Network network)
			: base(base58, network)
		{
		}
		public BitcoinStealthAddress(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		public BitcoinStealthAddress(PubKey pubKey, Network network)
			: base(GenerateBytes(pubKey, network), network)
		{

		}

		private static byte[] GenerateBytes(PubKey pubKey, Network network)
		{
			return
				pubKey.ToBytes()
				.Concat(new byte[] { 0, 0 })
				.ToArray();
		}

		PubKey _PubKey;
		public PubKey PubKey
		{
			get
			{
				if(_PubKey == null)
				{
					var iscompressed = vchData.Length == 33 + 2;
					_PubKey = new PubKey(vchData.Take(iscompressed ? 33 : 65).ToArray());
				}
				return _PubKey;
			}
		}

		byte[] _Unknown;
		public byte[] Unknown
		{
			get
			{
				if(_Unknown == null)
				{
					_Unknown = vchData.Skip(33).ToArray();
				}
				return _Unknown;
			}
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 33 + 2 || vchData.Length == 65 + 2;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.STEALTH_ADDRESS;
			}
		}


		public StealthNonce GetNonce(Key senderKey)
		{
			return new StealthNonce(senderKey, PubKey, PubKey, Network);
		}

		public StealthNonce GetNonce(Key receiverKey, PubKey senderKey)
		{
			var nonce = new StealthNonce(receiverKey, senderKey, PubKey, Network);
			if(!nonce.DeriveKey(receiverKey))
			{
				throw new SecurityException("invalid receiver key for this nonce");
			}
			return nonce;
		}
	}
}
