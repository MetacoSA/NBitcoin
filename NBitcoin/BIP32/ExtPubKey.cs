﻿using System;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System.Linq;

namespace NBitcoin
{
	/// <summary>
	/// A public HD key
	/// </summary>
	public class ExtPubKey : IBitcoinSerializable, IDestination
	{
		public static ExtPubKey Parse(string wif, Network expectedNetwork = null)
		{
			return Network.CreateFromBase58Data<BitcoinExtPubKey>(wif, expectedNetwork).ExtPubKey;
		}

		static readonly byte[] validPubKey = Encoders.Hex.DecodeData("0374ef3990e387b5a2992797f14c031a64efd80e5cb843d7c1d4a0274a9bc75e55");
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

		public bool IsHardened
		{
			get
			{
				return (nChild & 0x80000000u) != 0;
			}
		}
		public PubKey PubKey
		{
			get
			{
				return pubkey;
			}
		}
		public byte[] ChainCode
		{
			get
			{
				byte[] chainCodeCopy = new byte[vchChainCode.Length];
				Buffer.BlockCopy(vchChainCode, 0, chainCodeCopy, 0, vchChainCode.Length);

				return chainCodeCopy;
			}
		}

		internal ExtPubKey()
		{
		}

		public ExtPubKey(byte[] bytes)
		{
			if(bytes == null)
				throw new ArgumentNullException("bytes");
			this.ReadWrite(bytes);
		}

		public ExtPubKey(PubKey pubkey, byte[] chainCode, byte depth, byte[] fingerprint, uint child)
		{
			if(pubkey == null)
				throw new ArgumentNullException("pubkey");
			if(chainCode == null)
				throw new ArgumentNullException("chainCode");
			if(fingerprint == null)
				throw new ArgumentNullException("fingerprint");
			if(fingerprint.Length != fingerprint.Length)
				throw new ArgumentException(string.Format("The fingerprint must be {0} bytes.", fingerprint.Length), "fingerprint");
			if(chainCode.Length != vchChainCode.Length)
				throw new ArgumentException(string.Format("The chain code must be {0} bytes.", vchChainCode.Length), "chainCode");
			this.pubkey = pubkey;
			this.nDepth = depth;
			this.nChild = child;
			Buffer.BlockCopy(fingerprint, 0, vchFingerprint, 0, vchFingerprint.Length);
			Buffer.BlockCopy(chainCode, 0, vchChainCode, 0, vchChainCode.Length);
		}

		public ExtPubKey(PubKey masterKey, byte[] chainCode)
		{
			if(masterKey == null)
				throw new ArgumentNullException("masterKey");
			if(chainCode == null)
				throw new ArgumentNullException("chainCode");
			if(chainCode.Length != vchChainCode.Length)
				throw new ArgumentException(string.Format("The chain code must be {0} bytes.", vchChainCode.Length), "chainCode");
			this.pubkey = masterKey;
			Buffer.BlockCopy(chainCode, 0, vchChainCode, 0, vchChainCode.Length);
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
		public byte[] CalculateChildFingerprint()
		{
			return pubkey.Hash.ToBytes().SafeSubarray(0, vchFingerprint.Length);
		}

		public byte[] Fingerprint
		{
			get
			{
				return vchFingerprint;
			}
		}

		public ExtPubKey Derive(uint index)
		{
			var result = new ExtPubKey
			{
				nDepth = (byte)(nDepth + 1),
				vchFingerprint = CalculateChildFingerprint(),
				nChild = index
			};
			result.pubkey = pubkey.Derivate(this.vchChainCode, index, out result.vchChainCode);
			return result;
		}

		public ExtPubKey Derive(KeyPath derivation)
		{
			ExtPubKey result = this;
			return derivation.Indexes.Aggregate(result, (current, index) => current.Derive(index));
		}

		public ExtPubKey Derive(int index, bool hardened)
		{
			if(index < 0)
				throw new ArgumentOutOfRangeException("index", "the index can't be negative");
			uint realIndex = (uint)index;
			realIndex = hardened ? realIndex | 0x80000000u : realIndex;
			return Derive(realIndex);
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

		#region IDestination Members

		/// <summary>
		/// The P2PKH payment script
		/// </summary>
		public Script ScriptPubKey
		{
			get
			{
				return PubKey.Hash.ScriptPubKey;
			}
		}

		#endregion
	}
}
