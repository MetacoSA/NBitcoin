using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Math;
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
		public static ExtKey Parse(string wif, Network expectedNetwork = null)
		{
			return Network.CreateFromBase58Data<BitcoinExtKey>(wif, expectedNetwork).ExtKey;
		}

		Key key = null;
		byte[] vchChainCode = new byte[32];
		uint nChild;
		byte nDepth;
		byte[] vchFingerprint = new byte[4];

		static readonly byte[] hashkey = new[] { 'B', 'i', 't', 'c', 'o', 'i', 'n', ' ', 's', 'e', 'e', 'd' }.Select(o => (byte)o).ToArray();

		public byte Depth
		{
			get
			{
				return nDepth;
			}
		}
		public uint Child
		{
			get
			{
				return nChild;
			}
		}

		public ExtKey()
		{
			byte[] seed = RandomUtils.GetBytes(64);
			SetMaster(seed);
		}

		public Key Key
		{
			get
			{
				return key;
			}
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

		public bool IsChildOf(ExtKey parentKey)
		{
			if(Depth != parentKey.Depth + 1)
				return false;
			return parentKey.CalculateChildFingerprint().SequenceEqual(Fingerprint);
		}
		public bool IsParentOf(ExtKey childKey)
		{
			return childKey.IsChildOf(this);
		}
		private byte[] CalculateChildFingerprint()
		{
			return key.PubKey.ID.ToBytes().Take(vchFingerprint.Length).ToArray();
		}

		public byte[] Fingerprint
		{
			get
			{
				return vchFingerprint;
			}
		}
		public ExtKey Derive(uint nChild)
		{
			var result = new ExtKey();
			result.nDepth = (byte)(nDepth + 1);
			result.vchFingerprint = CalculateChildFingerprint();
			result.nChild = nChild;
			result.key = key.Derivate(this.vchChainCode, nChild, out result.vchChainCode);
			return result;
		}

		public BitcoinExtKey GetWif(Network network)
		{
			return new BitcoinExtKey(this, network);
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

		public ExtKey Derive(KeyPath derivation)
		{
			ExtKey result = this;
			foreach(var index in derivation.Indexes)
			{
				result = result.Derive(index);
			}
			return result;
		}

		public string ToString(Network network)
		{
			return new BitcoinExtKey(this, network).ToString();
		}
	}
}
