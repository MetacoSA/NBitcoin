using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Linq;

namespace NBitcoin
{
	/// <summary>
	/// A private Hierarchical Deterministic key
	/// </summary>
	public class ExtKey : IBitcoinSerializable, IDestination, ISecret
	{
		/// <summary>
		/// Parses the Base58 data (checking the network if specified), checks it represents the
		/// correct type of item, and then returns the corresponding ExtKey.
		/// </summary>
		public static ExtKey Parse(string wif, Network expectedNetwork = null)
		{
			return Network.Parse<BitcoinExtKey>(wif, expectedNetwork).ExtKey;
		}

		private const int FingerprintLength = 4;
		private const int ChainCodeLength = 32;

		Key key;
		byte[] vchChainCode = new byte[ChainCodeLength];
		uint nChild;
		byte nDepth;
		byte[] vchFingerprint = new byte[FingerprintLength];

		static readonly byte[] hashkey = Encoders.ASCII.DecodeData("Bitcoin seed");

		/// <summary>
		/// Gets the depth of this extended key from the root key.
		/// </summary>
		public byte Depth
		{
			get
			{
				return nDepth;
			}
		}

		/// <summary>
		/// Gets the child number of this key (in reference to the parent).
		/// </summary>
		public uint Child
		{
			get
			{
				return nChild;
			}
		}

		public byte[] ChainCode
		{
			get
			{
				byte[] chainCodeCopy = new byte[ChainCodeLength];
				Buffer.BlockCopy(vchChainCode, 0, chainCodeCopy, 0, ChainCodeLength);

				return chainCodeCopy;
			}
		}

		/// <summary>
		/// Constructor. Reconstructs an extended key from the Base58 representations of 
		/// the public key and corresponding private key.  
		/// </summary>
		public ExtKey(BitcoinExtPubKey extPubKey, BitcoinSecret key)
			: this(extPubKey.ExtPubKey, key.PrivateKey)
		{
		}

		/// <summary>
		/// Constructor. Creates an extended key from the public key and corresponding private key.  
		/// </summary>
		/// <remarks>
		/// <para>
		/// The ExtPubKey has the relevant values for child number, depth, chain code, and fingerprint.
		/// </para>
		/// </remarks>
		public ExtKey(ExtPubKey extPubKey, Key privateKey)
		{
			if(extPubKey == null)
				throw new ArgumentNullException(nameof(extPubKey));
			if(privateKey == null)
				throw new ArgumentNullException(nameof(privateKey));
			this.nChild = extPubKey.nChild;
			this.nDepth = extPubKey.nDepth;
			this.vchChainCode = extPubKey.vchChainCode;
			this.vchFingerprint = extPubKey.vchFingerprint;
			this.key = privateKey;
		}

		/// <summary>
		/// Constructor. Creates an extended key from the private key, and specified values for
		/// chain code, depth, fingerprint, and child number.
		/// </summary>
		public ExtKey(Key key, byte[] chainCode, byte depth, byte[] fingerprint, uint child)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));
			if(chainCode == null)
				throw new ArgumentNullException(nameof(chainCode));
			if(fingerprint == null)
				throw new ArgumentNullException(nameof(fingerprint));
			if(fingerprint.Length != FingerprintLength)
				throw new ArgumentException(string.Format("The fingerprint must be {0} bytes.", FingerprintLength), "fingerprint");
			if(chainCode.Length != ChainCodeLength)
				throw new ArgumentException(string.Format("The chain code must be {0} bytes.", ChainCodeLength), "chainCode");
			this.key = key;
			this.nDepth = depth;
			this.nChild = child;
			Buffer.BlockCopy(fingerprint, 0, vchFingerprint, 0, FingerprintLength);
			Buffer.BlockCopy(chainCode, 0, vchChainCode, 0, ChainCodeLength);
		}

		/// <summary>
		/// Constructor. Creates an extended key from the private key, with the specified value
		/// for chain code. Depth, fingerprint, and child number, will have their default values.
		/// </summary>
		public ExtKey(Key masterKey, byte[] chainCode)
		{
			if(masterKey == null)
				throw new ArgumentNullException(nameof(masterKey));
			if(chainCode == null)
				throw new ArgumentNullException(nameof(chainCode));
			if(chainCode.Length != ChainCodeLength)
				throw new ArgumentException(string.Format("The chain code must be {0} bytes.", ChainCodeLength), "chainCode");
			this.key = masterKey;
			Buffer.BlockCopy(chainCode, 0, vchChainCode, 0, ChainCodeLength);
		}

		/// <summary>
		/// Constructor. Creates a new extended key with a random 64 byte seed.
		/// </summary>
		public ExtKey()
		{
			byte[] seed = RandomUtils.GetBytes(64);
			SetMaster(seed);
		}

		/// <summary>
		/// Constructor. Creates a new extended key from the specified seed bytes, from the given hex string.
		/// </summary>
		public ExtKey(string seedHex)
		{
			SetMaster(Encoders.Hex.DecodeData(seedHex));
		}

		/// <summary>
		/// Constructor. Creates a new extended key from the specified seed bytes.
		/// </summary>
		public ExtKey(byte[] seed)
		{
			SetMaster(seed.ToArray());
		}

		private void SetMaster(byte[] seed)
		{
			var hashMAC = Hashes.HMACSHA512(hashkey, seed);
			key = new Key(hashMAC.SafeSubarray(0, 32));

			Buffer.BlockCopy(hashMAC, 32, vchChainCode, 0, ChainCodeLength);
		}

		/// <summary>
		/// Get the private key of this extended key.
		/// </summary>
		public Key PrivateKey
		{
			get
			{
				return key;
			}
		}

		/// <summary>
		/// Create the public key from this key.
		/// </summary>
		public ExtPubKey Neuter()
		{
			ExtPubKey ret = new ExtPubKey
			{
				nDepth = nDepth,
				vchFingerprint = vchFingerprint.ToArray(),
				nChild = nChild,
				pubkey = key.PubKey,
				vchChainCode = vchChainCode.ToArray()
			};
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
			return key.PubKey.Hash.ToBytes().SafeSubarray(0, FingerprintLength);
		}

		public byte[] Fingerprint
		{
			get
			{
				return vchFingerprint;
			}
		}

		/// <summary>
		/// Derives a new extended key in the hierarchy as the given child number.
		/// </summary>
		public ExtKey Derive(uint index)
		{
			var result = new ExtKey
			{
				nDepth = (byte)(nDepth + 1),
				vchFingerprint = CalculateChildFingerprint(),
				nChild = index
			};
			result.key = key.Derivate(this.vchChainCode, index, out result.vchChainCode);
			return result;
		}

		/// <summary>
		/// Derives a new extended key in the hierarchy as the given child number, 
		/// setting the high bit if hardened is specified.
		/// </summary>
		public ExtKey Derive(int index, bool hardened)
		{
			if(index < 0)
				throw new ArgumentOutOfRangeException("index", "the index can't be negative");
			uint realIndex = (uint)index;
			realIndex = hardened ? realIndex | 0x80000000u : realIndex;
			return Derive(realIndex);
		}

		/// <summary>
		/// Derives a new extended key in the hierarchy at the given path below the current key,
		/// by deriving the specified child at each step.
		/// </summary>
		public ExtKey Derive(KeyPath derivation)
		{
			ExtKey result = this;
			return derivation.Indexes.Aggregate(result, (current, index) => current.Derive(index));
		}

		/// <summary>
		/// Converts the extended key to the base58 representation, within the specified network.
		/// </summary>
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

		/// <summary>
		/// Converts the extended key to the base58 representation, as a string, within the specified network.
		/// </summary>
		public string ToString(Network network)
		{
			return new BitcoinExtKey(this, network).ToString();
		}

		#region IDestination Members

		/// <summary>
		/// Gets the script of the hash of the public key corresponding to the private key.
		/// </summary>
		public Script ScriptPubKey
		{
			get
			{
				return PrivateKey.PubKey.Hash.ScriptPubKey;
			}
		}

		#endregion

		/// <summary>
		/// Gets whether or not this extended key is a hardened child.
		/// </summary>
		public bool IsHardened
		{
			get
			{
				return (nChild & 0x80000000u) != 0;
			}
		}

		/// <summary>
		/// Recreates the private key of the parent from the private key of the child 
		/// combinated with the public key of the parent (hardened children cannot be
		/// used to recreate the parent).
		/// </summary>
		public ExtKey GetParentExtKey(ExtPubKey parent)
		{
			if(parent == null)
				throw new ArgumentNullException(nameof(parent));
			if(Depth == 0)
				throw new InvalidOperationException("This ExtKey is the root key of the HD tree");
			if(IsHardened)
				throw new InvalidOperationException("This private key is hardened, so you can't get its parent");
			var expectedFingerPrint = parent.CalculateChildFingerprint();
			if(parent.Depth != this.Depth - 1 || !expectedFingerPrint.SequenceEqual(vchFingerprint))
				throw new ArgumentException("The parent ExtPubKey is not the immediate parent of this ExtKey", "parent");

			byte[] l = null;
			byte[] ll = new byte[32];
			byte[] lr = new byte[32];

			var pubKey = parent.PubKey.ToBytes();
			l = Hashes.BIP32Hash(parent.vchChainCode, nChild, pubKey[0], pubKey.SafeSubarray(1));
			Array.Copy(l, ll, 32);
			Array.Copy(l, 32, lr, 0, 32);
			var ccChild = lr;

			BigInteger parse256LL = new BigInteger(1, ll);
			BigInteger N = ECKey.CURVE.N;

			if(!ccChild.SequenceEqual(vchChainCode))
				throw new InvalidOperationException("The derived chain code of the parent is not equal to this child chain code");

			var keyBytes = PrivateKey.ToBytes();
			var key = new BigInteger(1, keyBytes);

			BigInteger kPar = key.Add(parse256LL.Negate()).Mod(N);
			var keyParentBytes = kPar.ToByteArrayUnsigned();
			if(keyParentBytes.Length < 32)
				keyParentBytes = new byte[32 - keyParentBytes.Length].Concat(keyParentBytes).ToArray();

			var parentExtKey = new ExtKey
			{
				vchChainCode = parent.vchChainCode,
				nDepth = parent.Depth,
				vchFingerprint = parent.Fingerprint,
				nChild = parent.nChild,
				key = new Key(keyParentBytes)
			};
			return parentExtKey;
		}

	}
}
