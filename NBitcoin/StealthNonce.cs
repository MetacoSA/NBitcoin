using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class StealthNonce
	{
		private readonly byte[] _Nonce;
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		private readonly PubKey _StealthKey;
		public PubKey StealthKey
		{
			get
			{
				return _StealthKey;
			}
		}



		public StealthNonce(Key priv, PubKey pub, PubKey stealthKey, Network network)
		{
			_Network = network;
			var curve = ECKey.CreateCurve();
			var pubec = curve.Curve.DecodePoint(pub.ToBytes());
			var p = pubec.Multiply(new BigInteger(1, priv.ToBytes()));
			_StealthKey = stealthKey;
			_Nonce = Hashes.SHA256(p.GetEncoded());

			//Q' = Q + cG
			var qprim = curve.G.Multiply(new BigInteger(1, _Nonce)).Add(curve.Curve.DecodePoint(stealthKey.ToBytes()));
			_DestinationKey = new PubKey(qprim.GetEncoded());
			if(stealthKey.IsCompressed)
				_DestinationKey = _DestinationKey.Compress();
			else
				_DestinationKey = _DestinationKey.Decompress();
		}



		readonly PubKey _DestinationKey;
		public PubKey DestinationKey
		{
			get
			{
				return _DestinationKey;
			}
		}

		public BitcoinAddress DestinationAddress
		{
			get
			{
				return _DestinationKey.GetAddress(Network);
			}
		}

		Key _Key;
		public Key Key
		{
			get
			{
				return _Key;
			}
		}

		public bool DeriveKey(Key receiverKey)
		{
			var curve = ECKey.CreateCurve();
			var priv = new BigInteger(1, _Nonce)
							.Add(new BigInteger(1, receiverKey.ToBytes()))
							.Mod(curve.N)
							.ToByteArrayUnsigned();

			if(priv.Length < 32)
				priv = new byte[32 - priv.Length].Concat(priv).ToArray();

			var key = new Key(priv, fCompressedIn: StealthKey.IsCompressed);

			if(key.PubKey.GetAddress(Network).ToString() != DestinationAddress.ToString())
				return false;
			_Key = key;
			return true;
		}

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(_Nonce);
		}

	}
}
