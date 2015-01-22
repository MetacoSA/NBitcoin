using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System.Linq;

namespace NBitcoin
{
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
		public byte[] CalculateChildFingerprint()
		{
			return pubkey.Hash.ToBytes().Take(vchFingerprint.Length).ToArray();
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
			    nDepth = (byte) (nDepth + 1), 
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
