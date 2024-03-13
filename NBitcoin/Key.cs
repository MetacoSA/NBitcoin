#nullable enable
using NBitcoin.Crypto;
using System;
using System.Linq;
using System.Text;
using NBitcoin.DataEncoders;
#if !HAS_SPAN
using NBitcoin.BouncyCastle.Math;
#endif
namespace NBitcoin
{
	public class Key : IDestination, IDisposable
	{
		private const int KEY_SIZE = 32;
		private readonly static uint256 N = uint256.Parse("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141");

		public static Key Parse(string wif, Network expectedNetwork)
		{
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));
			return Network.Parse<BitcoinSecret>(wif, expectedNetwork).PrivateKey;
		}

		public static Key Parse(string wif, string password, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return Network.Parse<BitcoinEncryptedSecret>(wif, network).GetKey(password);
		}

#if HAS_SPAN
		internal readonly Secp256k1.ECPrivKey _ECKey;
#else
		readonly byte[] vch;
		internal readonly ECKey _ECKey;
#endif
		public bool IsCompressed
		{
			get;
			internal set;
		}

		public Key()
			: this(true)
		{

		}

		public BitcoinAddress GetAddress(ScriptPubKeyType scriptPubKeyType, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return PubKey.GetAddress(scriptPubKeyType, network);
		}
		public IAddressableDestination GetDestination(ScriptPubKeyType scriptPubKeyType)
		{
			return PubKey.GetDestination(scriptPubKeyType);
		}
		public Script GetScriptPubKey(ScriptPubKeyType scriptPubKeyType)
		{
			return PubKey.GetScriptPubKey(scriptPubKeyType);
		}

#if HAS_SPAN
		internal Key(Secp256k1.ECPrivKey ecKey, bool compressed)
		{
			if (ecKey == null)
				throw new ArgumentNullException(nameof(ecKey));
			this.IsCompressed = compressed;
			_ECKey = ecKey;
		}
		internal Key(ReadOnlySpan<byte> bytes, bool compressed = true)
		{
			this.IsCompressed = compressed;
			if (bytes.Length != KEY_SIZE)
			{
				throw new ArgumentException(paramName: "data", message: $"The size of an EC key should be {KEY_SIZE}");
			}
			if (NBitcoinContext.Instance.TryCreateECPrivKey(bytes, out var key) && key is Secp256k1.ECPrivKey)
			{
				_ECKey = key;
			}
			else
				throw new ArgumentException(paramName: "data", message: "Invalid EC key");
		}
#endif

		public Key(bool fCompressedIn)
		{
			IsCompressed = fCompressedIn;
#if HAS_SPAN
			Span<byte> data = stackalloc byte[KEY_SIZE];
			while (true)
			{
				RandomUtils.GetBytes(data);
				if (NBitcoinContext.Instance.TryCreateECPrivKey(data, out var key) && key is Secp256k1.ECPrivKey)
				{
					_ECKey = key;
					return;
				}
			}
#else
			var data = new byte[KEY_SIZE];
			do
			{
				RandomUtils.GetBytes(data);
			} while (!Check(data));

			vch = data.SafeSubarray(0, data.Length);
			IsCompressed = fCompressedIn;
			_ECKey = new ECKey(vch, true);
#endif
		}
		public Key(byte[] data, int count = -1, bool fCompressedIn = true)
		{
			if (count == -1)
				count = data.Length;
			if (count != KEY_SIZE)
			{
				throw new ArgumentException(paramName: "data", message: $"The size of an EC key should be {KEY_SIZE}");
			}
#if HAS_SPAN
			if (NBitcoinContext.Instance.TryCreateECPrivKey(data.AsSpan().Slice(0, KEY_SIZE), out var key) && key is Secp256k1.ECPrivKey)
			{
				IsCompressed = fCompressedIn;
				_ECKey = key;
			}
			else
				throw new ArgumentException(paramName: "data", message: "Invalid EC key");
#else
			if (Check(data))
			{
				vch = data.SafeSubarray(0, count);
				IsCompressed = fCompressedIn;
				_ECKey = new ECKey(vch, true);

			}
			else
				throw new ArgumentException(paramName: "data", message: "Invalid EC key");
#endif
		}

		private static bool Check(byte[] vch)
		{
			var candidateKey = new uint256(vch.SafeSubarray(0, KEY_SIZE), false);
			return candidateKey > 0 && candidateKey < N;
		}

		PubKey? _PubKey;

		public PubKey PubKey
		{
			get
			{
				AssertNotDisposed();
				if (_PubKey is PubKey pubkey)
					return pubkey;
#if HAS_SPAN
				pubkey = new PubKey(_ECKey.CreatePubKey(), IsCompressed);
				_PubKey = pubkey;
				return pubkey;
#else
				ECKey key = new ECKey(vch, true);
				pubkey = key.GetPubKey(IsCompressed);
				_PubKey = pubkey;
				return pubkey;
#endif
			}
		}

		public ECDSASignature Sign(uint256 hash, bool useLowR)
		{
			AssertNotDisposed();
			return _ECKey.Sign(hash, useLowR);
		}

		public ECDSASignature Sign(uint256 hash)
		{
			AssertNotDisposed();
			return _ECKey.Sign(hash, true);
		}
#if HAS_SPAN
		public TaprootSignature SignTaprootKeySpend(uint256 hash, TaprootSigHash sigHash = TaprootSigHash.Default)
		{
			return SignTaprootKeySpend(hash, null, sigHash);
		}
		public TaprootSignature SignTaprootKeySpend(uint256 hash, uint256? merkleRoot, TaprootSigHash sigHash)
		{
			return SignTaprootKeySpend(hash, merkleRoot, null, sigHash);
		}
		public TaprootSignature SignTaprootKeySpend(uint256 hash, uint256? merkleRoot, uint256? aux, TaprootSigHash sigHash)
		{
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
			AssertNotDisposed();
			var eckey = _ECKey;
			if (PubKey.Parity)
			{
				eckey = new Secp256k1.ECPrivKey(_ECKey.sec.Negate(), _ECKey.ctx, true);
			}
			Span<byte> buf = stackalloc byte[32];
			TaprootFullPubKey.ComputeTapTweak(PubKey.TaprootInternalKey, merkleRoot, buf);
			eckey = eckey.TweakAdd(buf);
			hash.ToBytes(buf);
			var sig = aux?.ToBytes() is byte[] auxbytes ? eckey.SignBIP340(buf, auxbytes) : eckey.SignBIP340(buf);
			return new TaprootSignature(new SchnorrSignature(sig), sigHash);
		}
		public TaprootKeyPair CreateTaprootKeyPair()
		{
			return CreateTaprootKeyPair(null);
		}
		public TaprootKeyPair CreateTaprootKeyPair(uint256? merkleRoot)
		{
			return TaprootKeyPair.CreateTaprootPair(this, merkleRoot);
		}
#endif
		public KeyPair CreateKeyPair()
		{
			return new KeyPair(this, this.PubKey);
		}

		public CompactSignature SignCompact(uint256 hash)
		{
			return SignCompact(hash, true);
		}

		public CompactSignature SignCompact(uint256 hash, bool forceLowR)
		{
			if (hash is null)
				throw new ArgumentNullException(nameof(hash));
			if (!IsCompressed)
				throw new InvalidOperationException("This operation is only supported on compressed pubkey");
			AssertNotDisposed();
#if HAS_SPAN
			byte[] sigBytes = new byte[64];
			var sig = new Secp256k1.SecpRecoverableECDSASignature(_ECKey.Sign(hash, forceLowR, out var rec), rec);
			sig.WriteToSpanCompact(sigBytes, out _);
			return new CompactSignature(rec, sigBytes);
#else
			var sig = _ECKey.Sign(hash, forceLowR);
			// Now we have to work backwards to figure out the recId needed to recover the signature.
			int recId = -1;
			for (int i = 0; i < 4; i++)
			{
				ECKey k = ECKey.RecoverFromSignature(i, sig, hash);
				if (k != null && k.GetPubKey(true).ToHex() == PubKey.ToHex())
				{
					recId = i;
					break;
				}
			}

			if (recId == -1)
				throw new InvalidOperationException("Could not construct a recoverable key. This should never happen.");
#pragma warning disable 618
			byte[] sigData = new byte[64];  // 1 header + 32 bytes for R + 32 bytes for S
			Array.Copy(Utils.BigIntegerToBytes(sig.R, 32), 0, sigData, 0, 32);
			Array.Copy(Utils.BigIntegerToBytes(sig.S, 32), 0, sigData, 32, 32);
#pragma warning restore 618
			return new CompactSignature(recId, sigData);
#endif
		}

		public string Decrypt(string encryptedText)
		{
			if (encryptedText is null)
				throw new ArgumentNullException(nameof(encryptedText));
			AssertNotDisposed();
			var bytes = Encoders.Base64.DecodeData(encryptedText);
			var decrypted = Decrypt(bytes);
			return Encoding.UTF8.GetString(decrypted, 0, decrypted.Length);
		}

		public byte[] Decrypt(byte[] encrypted)
		{
			if (encrypted is null)
				throw new ArgumentNullException(nameof(encrypted));
			if (encrypted.Length < 85)
				throw new ArgumentException("Encrypted text is invalid, it should be length >= 85.");
			AssertNotDisposed();
			var magic = encrypted.SafeSubarray(0, 4);
			var ephemeralPubkeyBytes = encrypted.SafeSubarray(4, 33);
			var cipherText = encrypted.SafeSubarray(37, encrypted.Length - 32 - 37);
			var mac = encrypted.SafeSubarray(encrypted.Length - 32);
			if (!Utils.ArrayEqual(magic, Encoders.ASCII.DecodeData("BIE1")))
				throw new ArgumentException("Encrypted text is invalid, Invalid magic number.");

			var ephemeralPubkey = new PubKey(ephemeralPubkeyBytes);

			var sharedKey = Hashes.SHA512(ephemeralPubkey.GetSharedPubkey(this).ToBytes());
			var iv = sharedKey.SafeSubarray(0, 16);
			var encryptionKey = sharedKey.SafeSubarray(16, 16);
			var hashingKey = sharedKey.SafeSubarray(32);

			var hashMAC = Hashes.HMACSHA256(hashingKey, encrypted.SafeSubarray(0, encrypted.Length - 32));
			if (!Utils.ArrayEqual(mac, hashMAC))
				throw new ArgumentException("Encrypted text is invalid, Invalid mac.");

			var aes = new AesBuilder().SetKey(encryptionKey).SetIv(iv).IsUsedForEncryption(false).Build();
			var message = aes.Process(cipherText, 0, cipherText.Length);
			return message;
		}

		public Key Derivate(byte[] cc, uint nChild, out byte[] ccChild)
		{
			AssertNotDisposed();
#if HAS_SPAN
			if (!IsCompressed)
				throw new InvalidOperationException("The key must be compressed");
			Span<byte> vout = stackalloc byte[64];
			vout.Clear();
			if ((nChild >> 31) == 0)
			{
				Span<byte> pubkey = stackalloc byte[33];
				this.PubKey.ToBytes(pubkey, out _);
				Hashes.BIP32Hash(cc, nChild, pubkey[0], pubkey.Slice(1), vout);
			}
			else
			{
				Span<byte> privkey = stackalloc byte[32];
				this._ECKey.WriteToSpan(privkey);
				Hashes.BIP32Hash(cc, nChild, 0, privkey, vout);
				privkey.Fill(0);
			}
			ccChild = new byte[32];
			vout.Slice(32, 32).CopyTo(ccChild);
			Secp256k1.ECPrivKey keyChild = _ECKey.TweakAdd(vout.Slice(0, 32));
			vout.Clear();
			return new Key(keyChild, true);
#else
			byte[]? l = null;
			if ((nChild >> 31) == 0)
			{
				var pubKey = PubKey.ToBytes();
				l = Hashes.BIP32Hash(cc, nChild, pubKey[0], pubKey.SafeSubarray(1));
			}
			else
			{
				l = Hashes.BIP32Hash(cc, nChild, 0, this.ToBytes());
			}
			var ll = l.SafeSubarray(0, 32);
			var lr = l.SafeSubarray(32, 32);

			ccChild = lr;

			var parse256LL = new BigInteger(1, ll);
			var kPar = new BigInteger(1, vch);
			var N = ECKey.CURVE.N;

			if (parse256LL.CompareTo(N) >= 0)
				throw new InvalidOperationException("You won a prize ! this should happen very rarely. Take a screenshot, and roll the dice again.");
			var key = parse256LL.Add(kPar).Mod(N);
			if (key == BigInteger.Zero)
				throw new InvalidOperationException("You won the big prize ! this has probability lower than 1 in 2^127. Take a screenshot, and roll the dice again.");

			var keyBytes = key.ToByteArrayUnsigned();
			if (keyBytes.Length < 32)
				keyBytes = new byte[32 - keyBytes.Length].Concat(keyBytes).ToArray();
			return new Key(keyBytes);
#endif
		}

		public BitcoinSecret GetBitcoinSecret(Network network)
		{
			AssertNotDisposed();
			return new BitcoinSecret(this, network);
		}

		/// <summary>
		/// Same as GetBitcoinSecret
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public BitcoinSecret GetWif(Network network)
		{
			AssertNotDisposed();
			return new BitcoinSecret(this, network);
		}

		public BitcoinEncryptedSecretNoEC GetEncryptedBitcoinSecret(string password, Network network)
		{
			AssertNotDisposed();
			return new BitcoinEncryptedSecretNoEC(this, password, network);
		}

		public string ToString(Network network)
		{
			AssertNotDisposed();
			return new BitcoinSecret(this, network).ToString();
		}

#region IDestination Members

		Script IDestination.ScriptPubKey
		{
			get
			{
				AssertNotDisposed();
				return PubKey.Hash.ScriptPubKey;
			}
		}

#endregion

		public TransactionSignature Sign(uint256 hash, SigningOptions signingOptions)
		{
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
			AssertNotDisposed();
			signingOptions ??= new SigningOptions();
			return new TransactionSignature(Sign(hash, signingOptions.EnforceLowR), signingOptions.SigHash);
		}


		public override bool Equals(object? obj)
		{
			if (obj is Key item)
				return PubKey.Equals(item.PubKey);
			return false;
		}
		public static bool operator ==(Key? a, Key? b)
		{
			if (a?.PubKey is PubKey apk && b?.PubKey is PubKey bpk)
			{
				return apk.Equals(bpk);
			}
			return a is null && b is null;
		}

		public static bool operator !=(Key? a, Key? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return PubKey.GetHashCode();
		}

		public byte[] ToBytes()
		{
			AssertNotDisposed();
#if HAS_SPAN
			var b = new byte[KEY_SIZE];
			_ECKey.WriteToSpan(b);
			return b;
#else
			return vch.ToArray();
#endif
		}

		public string ToHex()
		{
			AssertNotDisposed();
#if HAS_SPAN
			Span<byte> tmp = stackalloc byte[KEY_SIZE];
			_ECKey.WriteToSpan(tmp);
			return Encoders.Hex.EncodeData(tmp);
#else
			return Encoders.Hex.EncodeData(vch);
#endif
		}

		bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void AssertNotDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(NBitcoin.Key));
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;
			
			if (disposing)
			{
				if (_ECKey is IDisposable keyMaterial)
					keyMaterial.Dispose();
			}
			disposed = true;
		}

		~Key()
		{
			Dispose(false);
		}
	}
}
#nullable disable
