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

		static readonly byte[] ValidPubKey = Encoders.Hex.DecodeData("0374ef3990e387b5a2992797f14c031a64efd80e5cb843d7c1d4a0274a9bc75e55");
		internal byte NDepth;
		internal byte[] VchFingerprint = new byte[4];
		internal uint NChild;

		//
		internal PubKey Pubkey = new PubKey(ValidPubKey);
		internal byte[] VchChainCode = new byte[32];

		public byte Depth
		{
			get
			{
				return NDepth;
			}
		}

		public uint Child
		{
			get
			{
				return NChild;
			}
		}

		public bool IsHardened
		{
			get
			{
				return (NChild & 0x80000000u) != 0;
			}
		}
		public PubKey PubKey
		{
			get
			{
				return Pubkey;
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
			return Pubkey.Hash.ToBytes().Take(VchFingerprint.Length).ToArray();
		}

		public byte[] Fingerprint
		{
			get
			{
				return VchFingerprint;
			}
		}

		public ExtPubKey Derive(uint index)
		{
			var result = new ExtPubKey
			{
			    NDepth = (byte) (NDepth + 1), 
                VchFingerprint = CalculateChildFingerprint(), 
                NChild = index
			};
		    result.Pubkey = Pubkey.Derivate(VchChainCode, index, out result.VchChainCode);
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
				stream.ReadWrite(ref NDepth);
				stream.ReadWrite(ref VchFingerprint);
				stream.ReadWrite(ref NChild);
				stream.ReadWrite(ref VchChainCode);
				stream.ReadWrite(ref Pubkey);
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
			return item != null && Hash.Equals(item.Hash);
		}
		public static bool operator ==(ExtPubKey a, ExtPubKey b)
		{
			if(ReferenceEquals(a, b))
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
