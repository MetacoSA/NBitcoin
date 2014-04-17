using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	//https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki
	public class ExtKey : IBitcoinSerializable
	{
		public Key key = null;
		public byte[] vchChainCode = new byte[32];
		public uint nChild;
		public byte nDepth;
		public byte[] vchFingerprint = new byte[4];

		static readonly byte[] hashkey = new[] { 'B', 'i', 't', 'c', 'o', 'i', 'n', ' ', 's', 'e', 'e', 'd' }.Select(o => (byte)o).ToArray();

		static Random _Rand = new Random();
		public ExtKey()
		{
			byte[] seed = new byte[64];
			lock(_Rand)
			{
				_Rand.NextBytes(seed);
			}
			SetMaster(seed);
		}
		public ExtKey(string seedHex)
		{
			SetMaster(Encoders.Hex.DecodeData(seedHex));
		}
		public ExtKey(byte[] seed)
		{
			SetMaster(seed.ToArray());
		}
		private void SetMaster(byte[] seed)
		{
			var hashMAC = Hashes.HMACSHA512(hashkey, seed);
			key = new Key(hashMAC.Take(32).ToArray());
			Array.Copy(hashMAC.Skip(32).Take(32).ToArray(), 0, vchChainCode, 0, vchChainCode.Length);
		}

		public ExtPubKey Neuter()
		{
			ExtPubKey ret = new ExtPubKey();
			ret.nDepth = nDepth;
			ret.vchFingerprint = vchFingerprint.ToArray();
			ret.nChild = nChild;
			ret.pubkey = key.PubKey;
			ret.vchChainCode = vchChainCode.ToArray();
			return ret;
		}

		public ExtKey Derive(uint nChild)
		{
			var result = new ExtKey();
			result.nDepth = (byte)(nDepth + 1);
			result.vchFingerprint = key.PubKey.ID.ToBytes().Take(result.vchFingerprint.Length).ToArray();
			result.nChild = nChild;
			result.key = key.Derivate(this.vchChainCode, nChild, out result.vchChainCode);
			return result;
		}



		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			using(stream.BigEndianScope())
			{
				stream.ReadWrite(ref nDepth);
				stream.ReadWrite(ref vchFingerprint);
				stream.ReadWrite(ref nChild);
				stream.ReadWrite(ref vchChainCode);
				byte b = 0;
				stream.ReadWrite(ref b);
				stream.ReadWrite(ref key);
			}
		}

		#endregion
	}
}
