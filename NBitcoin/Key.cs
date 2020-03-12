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
	public class Key : IDestination, IDisposable, IBitcoinSerializable
	{
		private const int KEY_SIZE = 32;
		private readonly static uint256 N = uint256.Parse("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141");

		public static Key Parse(string wif, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return Network.Parse<BitcoinSecret>(wif, network).PrivateKey;
		}

		public static Key Parse(string wif, string password, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			return Network.Parse<BitcoinEncryptedSecret>(wif, network).GetKey(password);
		}

#if HAS_SPAN
		internal Secp256k1.ECPrivKey _ECKey;
#else
		byte[] vch = new byte[0];
		internal ECKey _ECKey;
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
#if HAS_SPAN
		internal Key(Secp256k1.ECPrivKey ecKey, bool compressed)
		{
			if (ecKey == null)
				throw new ArgumentNullException(nameof(ecKey));
			this.IsCompressed = compressed;
			_ECKey = ecKey;
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
				AssertNotDiposed();
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
			AssertNotDiposed();
			return _ECKey.Sign(hash, useLowR);
		}

		public ECDSASignature Sign(uint256 hash)
		{
			AssertNotDiposed();
			return _ECKey.Sign(hash, true);
		}

		public SchnorrSignature SignSchnorr(uint256 hash)
		{
			AssertNotDiposed();
#if HAS_SPAN
			Span<byte> h = stackalloc byte[32];
			hash.ToBytes(h);
			return new SchnorrSignature(_ECKey.SignSchnorr(h));
#else
			var signer = new SchnorrSigner();
			return signer.Sign(hash, this);
#endif
		}

		public string SignMessage(String message)
		{
			return SignMessage(Encoding.UTF8.GetBytes(message));
		}

		public string SignMessage(byte[] messageBytes)
		{
			return SignMessage(messageBytes, true);
		}
		public string SignMessage(byte[] messageBytes, bool forceLowR)
		{
			if (messageBytes is null)
				throw new ArgumentNullException(nameof(messageBytes));
			AssertNotDiposed();
			byte[] data = Utils.FormatMessageForSigning(messageBytes);
			var hash = Hashes.Hash256(data);
			return Convert.ToBase64String(SignCompact(hash, forceLowR));
		}


		public byte[] SignCompact(uint256 hash)
		{
			return SignCompact(hash, true);
		}

		public byte[] SignCompact(uint256 hash, bool forceLowR)
		{
			if (hash is null)
				throw new ArgumentNullException(nameof(hash));
			AssertNotDiposed();
#if HAS_SPAN
			Span<byte> vchSig = stackalloc byte[65];
			int rec = -1;
			var sig = new Secp256k1.SecpRecoverableECDSASignature(_ECKey.Sign(hash, forceLowR, out rec), rec);
			sig.WriteToSpanCompact(vchSig.Slice(1), out int recid);
			vchSig[0] = (byte)(27 + rec + (IsCompressed ? 4 : 0));
			return vchSig.ToArray();
#else
			var sig = _ECKey.Sign(hash, forceLowR);
			// Now we have to work backwards to figure out the recId needed to recover the signature.
			int recId = -1;
			for (int i = 0; i < 4; i++)
			{
				ECKey k = ECKey.RecoverFromSignature(i, sig, hash, IsCompressed);
				if (k != null && k.GetPubKey(IsCompressed).ToHex() == PubKey.ToHex())
				{
					recId = i;
					break;
				}
			}

			if (recId == -1)
				throw new InvalidOperationException("Could not construct a recoverable key. This should never happen.");

			int headerByte = recId + 27 + (IsCompressed ? 4 : 0);

			byte[] sigData = new byte[65];  // 1 header + 32 bytes for R + 32 bytes for S

			sigData[0] = (byte)headerByte;
#pragma warning disable 618
			Array.Copy(Utils.BigIntegerToBytes(sig.R, 32), 0, sigData, 1, 32);
			Array.Copy(Utils.BigIntegerToBytes(sig.S, 32), 0, sigData, 33, 32);
#pragma warning restore 618
			return sigData;
#endif
		}



		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			AssertNotDiposed();
#if HAS_SPAN
			Span<byte> tmp = stackalloc byte[KEY_SIZE];
			if (!stream.Serializing)
			{
				stream.ReadWrite(ref tmp);
				if (NBitcoinContext.Instance.TryCreateECPrivKey(tmp, out var k) && k is Secp256k1.ECPrivKey)
				{
					_ECKey = k;
				}
				else
				{
					throw new FormatException("Unvalid private key");
				}
			}
			else
			{
				_ECKey.WriteToSpan(tmp);
				stream.ReadWrite(ref tmp);
			}
#else
			stream.ReadWrite(ref vch);
			if (!stream.Serializing)
			{
				_ECKey = new ECKey(vch, true);
			}
#endif
		}

#endregion

		public string Decrypt(string encryptedText)
		{
			if (string.IsNullOrEmpty(encryptedText))
				throw new ArgumentNullException(nameof(encryptedText));
			AssertNotDiposed();
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
			AssertNotDiposed();
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
			AssertNotDiposed();
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

		public Key Uncover(Key scan, PubKey ephem)
		{
			AssertNotDiposed();
#if HAS_SPAN
			Span<byte> tmp = stackalloc byte[33];
			ephem.ECKey.GetSharedPubkey(scan._ECKey).WriteToSpan(true, tmp, out _);
			var c = NBitcoinContext.Instance.CreateECPrivKey(Hashes.SHA256(tmp));
			var priv = c.sec + this._ECKey.sec;
			return new Key(this._ECKey.ctx.CreateECPrivKey(priv), this.IsCompressed);
#else
			var curve = ECKey.Secp256k1;
			var priv = new BigInteger(1, PubKey.GetStealthSharedSecret(scan, ephem))
							.Add(new BigInteger(1, this.ToBytes()))
							.Mod(curve.N)
							.ToByteArrayUnsigned();

			if (priv.Length < 32)
				priv = new byte[32 - priv.Length].Concat(priv).ToArray();

			var key = new Key(priv, fCompressedIn: this.IsCompressed);
			return key;
#endif
		}

		public BitcoinSecret GetBitcoinSecret(Network network)
		{
			AssertNotDiposed();
			return new BitcoinSecret(this, network);
		}

		/// <summary>
		/// Same than GetBitcoinSecret
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public BitcoinSecret GetWif(Network network)
		{
			AssertNotDiposed();
			return new BitcoinSecret(this, network);
		}

		public BitcoinEncryptedSecretNoEC GetEncryptedBitcoinSecret(string password, Network network)
		{
			AssertNotDiposed();
			return new BitcoinEncryptedSecretNoEC(this, password, network);
		}

		public string ToString(Network network)
		{
			AssertNotDiposed();
			return new BitcoinSecret(this, network).ToString();
		}

#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				AssertNotDiposed();
				return PubKey.Hash.ScriptPubKey;
			}
		}

#endregion

		public TransactionSignature Sign(uint256 hash, SigHash sigHash, bool useLowR = true)
		{
			AssertNotDiposed();
			return new TransactionSignature(Sign(hash, useLowR), sigHash);
		}


		public override bool Equals(object obj)
		{
			if (obj is Key item)
				return PubKey.Equals(item.PubKey);
			return false;
		}
		public static bool operator ==(Key? a, Key? b)
		{
			if (a?.PubKey is PubKey apk && b?.PubKey is PubKey bpk)
			{
				return apk == bpk;
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

		public string ToHex()
		{
			AssertNotDiposed();
#if HAS_SPAN
			Span<byte> tmp = stackalloc byte[KEY_SIZE];
			_ECKey.WriteToSpan(tmp);
			return Encoders.Hex.EncodeData(tmp);
#else
			return Encoders.Hex.EncodeData(vch);
#endif
		}


#if HAS_SPAN
		void AssertNotDiposed()
		{
			if (_ECKey.cleared)
				throw new ObjectDisposedException(nameof(NBitcoin.Key));
		}
		public void Dispose()
		{
			_ECKey.Clear();
		}
#else
		bool disposed = false;
		void AssertNotDiposed()
		{
			if (disposed)
				throw new ObjectDisposedException(nameof(NBitcoin.Key));
		}
		public void Dispose()
		{
			disposed = true;
		}
#endif
	}
}
#nullable disable
