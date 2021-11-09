#nullable enable
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
#if !NO_BC
using NBitcoin.BouncyCastle.Math;
#endif
using System;
using System.Collections;
using System.Linq;

namespace NBitcoin
{
	public class HDKeyScriptPubKey : IHDScriptPubKey
	{
		private readonly IHDKey hdKey;
		private readonly ScriptPubKeyType type;

		public IHDKey HDKey
		{
			get
			{
				return hdKey;
			}
		}

		public HDKeyScriptPubKey(IHDKey hdKey, ScriptPubKeyType type)
		{
			if (hdKey == null)
				throw new ArgumentNullException(nameof(hdKey));
			this.hdKey = hdKey;
			this.type = type;
		}
		Script? _ScriptPubKey;
		public Script ScriptPubKey => _ScriptPubKey = _ScriptPubKey ?? hdKey.GetPublicKey().GetScriptPubKey(type);

		public IHDScriptPubKey Derive(KeyPath keyPath)
		{
			return new HDKeyScriptPubKey(this.hdKey.Derive(keyPath), type);
		}

		public bool CanDeriveHardenedPath()
		{
			return this.hdKey.CanDeriveHardenedPath();
		}
	}

	/// <summary>
	/// A private Hierarchical Deterministic key
	/// </summary>
	public class ExtKey : IDestination, ISecret, IEquatable<ExtKey>, IHDKey
	{
		/// <summary>
		/// Parses the Base58 data (checking the network if specified), checks it represents the
		/// correct type of item, and then returns the corresponding ExtKey.
		/// </summary>
		public static ExtKey Parse(string wif, Network expectedNetwork)
		{
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));
			if (wif == null)
				throw new ArgumentNullException(nameof(wif));
			return expectedNetwork.Parse<BitcoinExtKey>(wif).ExtKey;
		}

		private const int ChainCodeLength = 32;

		readonly Key key;
		readonly byte[] vchChainCode;
		readonly uint nChild;
		readonly byte nDepth;
		readonly HDFingerprint parentFingerprint = default;

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
				return vchChainCode;
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
			if (extPubKey is null)
				throw new ArgumentNullException(nameof(extPubKey));
			if (privateKey is null)
				throw new ArgumentNullException(nameof(privateKey));
			this.nChild = extPubKey.nChild;
			this.nDepth = extPubKey.nDepth;
			this.vchChainCode = extPubKey.vchChainCode;
			this.parentFingerprint = extPubKey.parentFingerprint;
			this.key = privateKey;
		}

		/// <summary>
		/// Constructor. Creates an extended key from the private key, and specified values for
		/// chain code, depth, fingerprint, and child number.
		/// </summary>
		public ExtKey(Key key, byte[] chainCode, byte depth, HDFingerprint fingerprint, uint child)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (chainCode == null)
				throw new ArgumentNullException(nameof(chainCode));
			if (chainCode.Length != ChainCodeLength)
				throw new ArgumentException(string.Format("The chain code must be {0} bytes.", ChainCodeLength), "chainCode");
			this.key = key;
			this.nDepth = depth;
			this.nChild = child;
			parentFingerprint = fingerprint;
			vchChainCode = new byte[32];
			Buffer.BlockCopy(chainCode, 0, vchChainCode, 0, ChainCodeLength);
		}

		/// <summary>
		/// Constructor. Creates an extended key from the private key, with the specified value
		/// for chain code. Depth, fingerprint, and child number, will have their default values.
		/// </summary>
		public ExtKey(Key masterKey, byte[] chainCode)
		{
			if (masterKey == null)
				throw new ArgumentNullException(nameof(masterKey));
			if (chainCode == null)
				throw new ArgumentNullException(nameof(chainCode));
			if (chainCode.Length != ChainCodeLength)
				throw new ArgumentException(string.Format("The chain code must be {0} bytes.", ChainCodeLength), "chainCode");
			this.key = masterKey;
			vchChainCode = new byte[32];
			Buffer.BlockCopy(chainCode, 0, vchChainCode, 0, ChainCodeLength);
		}

		/// <summary>
		/// Constructor. Creates a new extended key with a random 64 byte seed.
		/// </summary>
		public ExtKey()
		{
#if HAS_SPAN
			Span<byte> seed = stackalloc byte[64];
			RandomUtils.GetBytes(seed);
			key = CalculateKey(seed, out var cc);
			this.vchChainCode = cc;
#else
			byte[] seed = RandomUtils.GetBytes(64);
			key = CalculateKey(seed, out var cc);
			this.vchChainCode = cc;
#endif
		}

		public ExtKey Derive(RootedKeyPath rootedKeyPath)
		{
			if (rootedKeyPath == null)
				throw new ArgumentNullException(nameof(rootedKeyPath));
			if (rootedKeyPath.MasterFingerprint != GetPublicKey().GetHDFingerPrint())
				throw new ArgumentException(paramName: nameof(rootedKeyPath), message: "The rootedKeyPath's fingerprint does not match this ExtKey");
			return Derive(rootedKeyPath.KeyPath);
		}

		/// <summary>
		/// Constructor. Creates a new extended key from the specified seed bytes, from the given hex string.
		/// </summary>
		public ExtKey(string seedHex)
		{
			key = CalculateKey(Encoders.Hex.DecodeData(seedHex), out var cc);
			this.vchChainCode = cc;
		}


		public static ExtKey CreateFromSeed(byte[] seed)
		{
			if (seed == null)
				throw new ArgumentNullException(nameof(seed));
			return new ExtKey(seed, true);
		}
		public static ExtKey CreateFromBytes(byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			return new ExtKey(bytes, false);
		}
		/// <summary>
		/// Constructor. Creates a new extended key from the specified seed bytes.
		/// </summary>
		private ExtKey(byte[] bytes, bool isSeed)
		{
			if (isSeed)
			{
				key = CalculateKey(bytes, out var cc);
				this.vchChainCode = cc;
			}
			else
			{
				if (bytes == null)
					throw new ArgumentNullException(nameof(bytes));
				if (bytes.Length != Length)
					throw new FormatException($"An extpubkey should be {Length} bytes");
				int i = 0;
				nDepth = bytes[i];
				i++;
				parentFingerprint = new HDFingerprint(bytes, i);
				i += 4;
				nChild = Utils.ToUInt32(bytes, i, false);
				i += 4;
				vchChainCode = new byte[32];
				Array.Copy(bytes, i, vchChainCode, 0, 32);
				i += 32;
				if (bytes[i++] != 0)
					throw new FormatException($"Invalid ExtKey");
				var pk = new byte[32];
				Array.Copy(bytes, i, pk, 0, 32);
				key = new Key(pk);
			}
		}

#if HAS_SPAN

		public static ExtKey CreateFromSeed(ReadOnlySpan<byte> seed)
		{
			return new ExtKey(seed, true);
		}
		public static ExtKey CreateFromBytes(ReadOnlySpan<byte> bytes)
		{
			return new ExtKey(bytes, false);
		}

		private ExtKey(ReadOnlySpan<byte> bytes, bool isSeed)
		{
			if (isSeed)
			{
				key = CalculateKey(bytes, out var cc);
				this.vchChainCode = cc;
			}
			else
			{
				if (bytes.Length != Length)
					throw new FormatException($"An extpubkey should be {Length} bytes");
				int i = 0;
				nDepth = bytes[i];
				i++;
				parentFingerprint = new HDFingerprint(bytes.Slice(1,4));
				i += 4;
				nChild = Utils.ToUInt32(bytes.Slice(i, 4), false);
				i += 4;
				vchChainCode = new byte[32];
				bytes.Slice(i, 32).CopyTo(vchChainCode);
				i += 32;
				if (bytes[i++] != 0)
					throw new FormatException($"Invalid ExtKey");
				Span<byte> pk = stackalloc byte[32];
				bytes.Slice(i, 32).CopyTo(pk);
				key = new Key(pk);
			}
		}
		private static Key CalculateKey(ReadOnlySpan<byte> seed, out byte[] chainCode)
		{
			Span<byte> hashMAC = stackalloc byte[64];
			if (Hashes.HMACSHA512(hashkey, seed, hashMAC, out int len) &&
				len == 64 &&
				NBitcoinContext.Instance.TryCreateECPrivKey(hashMAC.Slice(0, 32), out var k) && k is Secp256k1.ECPrivKey)
			{
				var key = new Key(k, true);
				chainCode = new byte[32];
				hashMAC.Slice(32, ChainCodeLength).CopyTo(chainCode);
				hashMAC.Clear();
				return key;
			}
			else
			{
				throw new InvalidOperationException("Invalid ExtKey (this should never happen)");
			}
		}
#else
		private static Key CalculateKey(byte[] seed, out byte[] chainCode)
		{
			var hashMAC = Hashes.HMACSHA512(hashkey, seed);
			var key = new Key(hashMAC.SafeSubarray(0, 32));
			chainCode = new byte[32];
			Buffer.BlockCopy(hashMAC, 32, chainCode, 0, ChainCodeLength);
			return key;
		}
#endif
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
			return new ExtPubKey(key.PubKey, vchChainCode, nDepth, parentFingerprint, nChild);
		}

		public bool IsChildOf(ExtKey parentKey)
		{
			if (Depth != parentKey.Depth + 1)
				return false;
			return parentKey.PrivateKey.PubKey.GetHDFingerPrint() == ParentFingerprint;
		}
		public bool IsParentOf(ExtKey childKey)
		{
			return childKey.IsChildOf(this);
		}

		public HDFingerprint ParentFingerprint
		{
			get
			{
				return parentFingerprint;
			}
		}

		/// <summary>
		/// Derives a new extended key in the hierarchy as the given child number.
		/// </summary>
		public ExtKey Derive(uint index)
		{
			var childkey = key.Derivate(this.vchChainCode, index, out var childcc);
			return new ExtKey(childkey, childcc, (byte)(nDepth + 1), this.key.PubKey.GetHDFingerPrint(), index);
		}

		/// <summary>
		/// Derives a new extended key in the hierarchy as the given child number, 
		/// setting the high bit if hardened is specified.
		/// </summary>
		public ExtKey Derive(int index, bool hardened)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", "the index can't be negative");
			uint realIndex = (uint)index;
			realIndex = hardened ? realIndex | 0x80000000u : realIndex;
			return Derive(realIndex);
		}

		/// <summary>
		/// Derives a new extended key in the hierarchy at the given path below the current key,
		/// by deriving the specified child at each step.
		/// </summary>
		public ExtKey Derive(KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			ExtKey result = this;
			return keyPath.Indexes.Aggregate(result, (current, index) => current.Derive(index));
		}

		/// <summary>
		/// Converts the extended key to the base58 representation, within the specified network.
		/// </summary>
		public BitcoinExtKey GetWif(Network network)
		{
			return new BitcoinExtKey(this, network);
		}

		public const int Length = 1 + 4 + 4 + 32 + 33;

		public byte[] ToBytes()
		{
			var b = new byte[Length];
			int i = 0;
			b[i++] = nDepth;
			Array.Copy(parentFingerprint.ToBytes(), 0, b, i, 4);
			i += 4;
			Array.Copy(Utils.ToBytes(nChild, false), 0, b, i, 4);
			i += 4;
			Array.Copy(vchChainCode, 0, b, i, 32);
			i += 32;
			b[i++] = 0;
			Array.Copy(key.ToBytes(), 0, b, i, 32);
			return b;
		}

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
			if (parent is null)
				throw new ArgumentNullException(nameof(parent));
			if (Depth == 0)
				throw new InvalidOperationException("This ExtKey is the root key of the HD tree");
			if (IsHardened)
				throw new InvalidOperationException("This private key is hardened, so you can't get its parent");
			var expectedFingerPrint = parent.PubKey.GetHDFingerPrint();
			if (parent.Depth != this.Depth - 1 || expectedFingerPrint != parentFingerprint)
				throw new ArgumentException("The parent ExtPubKey is not the immediate parent of this ExtKey", "parent");
#if HAS_SPAN
			Span<byte> pubkey = stackalloc byte[33];
			Span<byte> l = stackalloc byte[64];
			parent.PubKey.ToBytes(pubkey, out _);
			Hashes.BIP32Hash(parent.vchChainCode, nChild, pubkey[0], pubkey.Slice(1), l);
			var parse256LL = new Secp256k1.Scalar(l.Slice(0, 32), out int overflow);
			if (overflow != 0 || parse256LL.IsZero)
				throw new InvalidOperationException("Invalid extkey (this should never happen)");
			if (!l.Slice(32, 32).SequenceEqual(vchChainCode))
				throw new InvalidOperationException("The derived chain code of the parent is not equal to this child chain code");
			var kPar = this.PrivateKey._ECKey.sec + parse256LL.Negate();
			return new ExtKey(new Key(new Secp256k1.ECPrivKey(kPar, this.PrivateKey._ECKey.ctx, true), true),
				parent.vchChainCode,
				parent.Depth,
				parent.ParentFingerprint,
				parent.nChild);
#else
			byte[] ll = new byte[32];
			byte[] lr = new byte[32];

			var pubKey = parent.PubKey.ToBytes();
			byte[] l = Hashes.BIP32Hash(parent.vchChainCode, nChild, pubKey[0], pubKey.SafeSubarray(1));
			Array.Copy(l, ll, 32);
			Array.Copy(l, 32, lr, 0, 32);
			var ccChild = lr;

			BigInteger parse256LL = new BigInteger(1, ll);
			BigInteger N = ECKey.CURVE.N;

			if (!ccChild.SequenceEqual(vchChainCode))
				throw new InvalidOperationException("The derived chain code of the parent is not equal to this child chain code");

			var keyBytes = PrivateKey.ToBytes();
			var key = new BigInteger(1, keyBytes);

			BigInteger kPar = key.Add(parse256LL.Negate()).Mod(N);
			var keyParentBytes = kPar.ToByteArrayUnsigned();
			if (keyParentBytes.Length < 32)
				keyParentBytes = new byte[32 - keyParentBytes.Length].Concat(keyParentBytes).ToArray();

			var parentExtKey = new ExtKey(new Key(keyParentBytes),
				parent.vchChainCode,
				parent.Depth,
				parent.ParentFingerprint,
				parent.nChild);
			return parentExtKey;
#endif
		}

		public bool Equals(ExtKey? other)
		{
			if (other is null)
				return false;
			return Depth == other.Depth &&
					   ParentFingerprint == other.ParentFingerprint &&
					   key == other.key &&
					   Child == other.Child &&
					   StructuralComparisons.StructuralEqualityComparer.Equals(vchChainCode, other.vchChainCode);
		}

		IHDKey IHDKey.Derive(KeyPath keyPath)
		{
			return this.Derive(keyPath);
		}

		public PubKey GetPublicKey()
		{
			return PrivateKey.PubKey;
		}

		bool IHDKey.CanDeriveHardenedPath()
		{
			return true;
		}

		public override bool Equals(object? obj)
		{
			if (obj is ExtKey other)
			{
				return Depth == other.Depth &&
					   ParentFingerprint == other.ParentFingerprint &&
					   key == other.key &&
					   Child == other.Child &&
					   StructuralComparisons.StructuralEqualityComparer.Equals(vchChainCode, other.vchChainCode);
			}
			return false;
		}
		public static bool operator ==(ExtKey? a, ExtKey? b)
		{
			if (a is ExtKey && b is ExtKey)
				return a.Equals(b);
			return a is null && b is null;
		}

		public static bool operator !=(ExtKey? a, ExtKey? b)
		{
			return !(a == b);
		}

		int? hashcode;
		public override int GetHashCode()
		{
			if (hashcode is int h)
				return h;
			h = Encoders.Hex.EncodeData(ToBytes()).GetHashCode();
			hashcode = h;
			return h;
		}
	}
}
