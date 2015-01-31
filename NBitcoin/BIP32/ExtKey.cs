using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Linq;

namespace NBitcoin
{
	//https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki
	public class ExtKey : IBitcoinSerializable, IDestination, ISecret
	{
		public static ExtKey Parse(string wif, Network expectedNetwork = null)
		{
			return Network.CreateFromBase58Data<BitcoinExtKey>(wif, expectedNetwork).ExtKey;
		}

		Key key;
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

		public ExtKey(BitcoinExtPubKey extPubKey, BitcoinSecret key)
			: this(extPubKey.ExtPubKey, key.PrivateKey)
		{
		}
		public ExtKey(ExtPubKey extPubKey, Key privateKey)
		{
			if(extPubKey == null)
				throw new ArgumentNullException("extPubKey");
			if(privateKey == null)
				throw new ArgumentNullException("privateKey");
			this.nChild = extPubKey.nChild;
			this.nDepth = extPubKey.nDepth;
			this.vchChainCode = extPubKey.vchChainCode;
			this.vchFingerprint = extPubKey.vchFingerprint;
			this.key = privateKey;
		}
		public ExtKey()
		{
			byte[] seed = RandomUtils.GetBytes(64);
			SetMaster(seed);
		}
		public Key PrivateKey
		{
			get
			{
				return key;
			}
		}
		[Obsolete("Use PrivateKey instead")]
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
			return key.PubKey.Hash.ToBytes().Take(vchFingerprint.Length).ToArray();
		}

		public byte[] Fingerprint
		{
			get
			{
				return vchFingerprint;
			}
		}
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

		public ExtKey Derive(int index, bool hardened)
		{
			if(index < 0)
				throw new ArgumentOutOfRangeException("index", "the index can't be negative");
			uint realIndex = (uint)index;
			realIndex = hardened ? realIndex | 0x80000000u : realIndex;
			return Derive(realIndex);
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
			return derivation.Indexes.Aggregate(result, (current, index) => current.Derive(index));
		}

		public string ToString(Network network)
		{
			return new BitcoinExtKey(this, network).ToString();
		}

		#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				return PrivateKey.PubKey.Hash.ScriptPubKey;
			}
		}

		#endregion

		public bool IsHardened
		{
			get
			{
				return (nChild & 0x80000000u) != 0;
			}
		}

		public ExtKey GetParentExtKey(ExtPubKey parent)
		{
			if(parent == null)
				throw new ArgumentNullException("parent");
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
			l = Hashes.BIP32Hash(parent.vchChainCode, nChild, pubKey[0], pubKey.Skip(1).ToArray());
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
