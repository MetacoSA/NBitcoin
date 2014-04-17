using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class ExtPubKey : IBitcoinSerializable
	{
		static byte[] validPubKey = Encoders.Hex.DecodeData("0374ef3990e387b5a2992797f14c031a64efd80e5cb843d7c1d4a0274a9bc75e55");
		public byte nDepth;
		public byte[] vchFingerprint = new byte[4];
		public uint nChild;

		//
		public PubKey pubkey = new PubKey(validPubKey);
		public byte[] vchChainCode = new byte[32];

		public ExtPubKey()
		{
		}
		public ExtPubKey Derive(uint nChild)
		{
			var result = new ExtPubKey();
			result.nDepth = (byte)(nDepth + 1);
			result.vchFingerprint = pubkey.ID.ToBytes().Take(result.vchFingerprint.Length).ToArray();
			result.nChild = nChild;
			result.pubkey = pubkey.Derivate(this.vchChainCode, nChild, out result.vchChainCode);
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
				stream.ReadWrite(ref pubkey);
			}
		}


		private uint256 Hash
		{
			get
			{
				return Hashes.Hash256(this.ToBytes());
			}
		}

		public override bool Equals(object obj)
		{
			ExtPubKey item = obj as ExtPubKey;
			if(item == null)
				return false;
			return Hash.Equals(item.Hash);
		}
		public static bool operator ==(ExtPubKey a, ExtPubKey b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.Hash == b.Hash;
		}

		public static bool operator !=(ExtPubKey a, ExtPubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Hash.GetHashCode();
		}
		#endregion
	}
}
