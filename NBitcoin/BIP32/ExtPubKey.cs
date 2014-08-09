﻿using NBitcoin.Crypto;
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
		public static ExtPubKey Parse(string wif, Network expectedNetwork = null)
		{
			return Network.CreateFromBase58Data<BitcoinExtPubKey>(wif, expectedNetwork).ExtPubKey;
		}

		static byte[] validPubKey = Encoders.Hex.DecodeData("0374ef3990e387b5a2992797f14c031a64efd80e5cb843d7c1d4a0274a9bc75e55");
		internal byte nDepth;
		internal byte[] vchFingerprint = new byte[4];
		internal uint nChild;

		//
		internal PubKey pubkey = new PubKey(validPubKey);
		internal byte[] vchChainCode = new byte[32];

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
		public PubKey PubKey
		{
			get
			{
				return pubkey;
			}
		}

		internal ExtPubKey()
		{
		}

		public bool IsChildOf(ExtPubKey parentKey)
		{
			if(Depth != parentKey.Depth + 1)
				return false;
			return parentKey.CalculateChildFingerprint().SequenceEqual(Fingerprint);
		}
		public bool IsParentOf(ExtPubKey childKey)
		{
			return childKey.IsChildOf(this);
		}
		private byte[] CalculateChildFingerprint()
		{
			return pubkey.ID.ToBytes().Take(vchFingerprint.Length).ToArray();
		}

		public byte[] Fingerprint
		{
			get
			{
				return vchFingerprint;
			}
		}
		public ExtPubKey Derive(uint nChild)
		{
			var result = new ExtPubKey();
			result.nDepth = (byte)(nDepth + 1);
			result.vchFingerprint = CalculateChildFingerprint();
			result.nChild = nChild;
			result.pubkey = pubkey.Derivate(this.vchChainCode, nChild, out result.vchChainCode);
			return result;
		}

		public ExtPubKey Derive(KeyPath derivation)
		{
			ExtPubKey result = this;
			foreach(var index in derivation.Indexes)
			{
				result = result.Derive(index);
			}
			return result;
		}

		

		public BitcoinExtPubKey GetWif(Network network)
		{
			return new BitcoinExtPubKey(this, network);
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

		public string ToString(Network network)
		{
			return new BitcoinExtPubKey(this, network).ToString();
		}
	}
}
