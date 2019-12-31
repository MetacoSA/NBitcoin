using NBitcoin.Crypto;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Linq;
using System.Text;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	public class Key : IBitcoinSerializable, IDestination
	{
		private const int KEY_SIZE = 32;
		private readonly static uint256 N = uint256.Parse("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141");

		public static Key Parse(string wif, Network network = null)
		{
			return Network.Parse<BitcoinSecret>(wif, network).PrivateKey;
		}

		public static Key Parse(string wif, string password, Network network = null)
		{
			return Network.Parse<BitcoinEncryptedSecret>(wif, network).GetKey(password);
		}

		byte[] vch = new byte[0];
		internal ECKey _ECKey;
		public bool IsCompressed
		{
			get;
			internal set;
		}

		public Key()
			: this(true)
		{

		}

		public Key(bool fCompressedIn)
		{
			var data = new byte[KEY_SIZE];
			do
			{
				RandomUtils.GetBytes(data);
			} while (!Check(data));

			SetBytes(data, data.Length, fCompressedIn);
		}
		public Key(byte[] data, int count = -1, bool fCompressedIn = true)
		{
			if (count == -1)
				count = data.Length;
			if (count != KEY_SIZE)
			{
				throw new ArgumentException(paramName: "data", message: $"The size of an EC key should be {KEY_SIZE}");
			}
			if (Check(data))
			{
				SetBytes(data, count, fCompressedIn);
			}
			else
				throw new ArgumentException(paramName: "data", message: "Invalid EC key");
		}

		private void SetBytes(byte[] data, int count, bool fCompressedIn)
		{
			vch = data.SafeSubarray(0, count);
			IsCompressed = fCompressedIn;
			_ECKey = new ECKey(vch, true);
		}

		private static bool Check(byte[] vch)
		{
			var candidateKey = new uint256(vch.SafeSubarray(0, KEY_SIZE), false);
			return candidateKey > 0 && candidateKey < N;
		}

		PubKey _PubKey;

		public PubKey PubKey
		{
			get
			{
				if (_PubKey == null)
				{
					ECKey key = new ECKey(vch, true);
					_PubKey = key.GetPubKey(IsCompressed);
				}
				return _PubKey;
			}
		}

		public ECDSASignature Sign(uint256 hash, bool useLowR)
		{
			return _ECKey.Sign(hash, useLowR);
		}

		public ECDSASignature Sign(uint256 hash)
		{
			return _ECKey.Sign(hash, true);
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

			Array.Copy(Utils.BigIntegerToBytes(sig.R, 32), 0, sigData, 1, 32);
			Array.Copy(Utils.BigIntegerToBytes(sig.S, 32), 0, sigData, 33, 32);
			return sigData;
		}



		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref vch);
			if (!stream.Serializing)
			{
				_ECKey = new ECKey(vch, true);
			}
		}

		#endregion

		public string Decrypt(string encryptedText)
		{
			if (string.IsNullOrEmpty(encryptedText))
				throw new ArgumentNullException(nameof(encryptedText));
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

			var magic = encrypted.SafeSubarray(0, 4);
			var ephemeralPubkeyBytes = encrypted.SafeSubarray(4, 33);
			var cipherText = encrypted.SafeSubarray(37, encrypted.Length - 32 - 37);
			var mac = encrypted.SafeSubarray(encrypted.Length - 32);
			if (!Utils.ArrayEqual(magic, Encoders.ASCII.DecodeData("BIE1")))
				throw new ArgumentException("Encrypted text is invalid, Invalid magic number.");

			var ephemeralPubkey = new PubKey(ephemeralPubkeyBytes);
			var ecpoint = ephemeralPubkey.ECKey.GetPublicKeyParameters().Q;
			if (ecpoint.IsInfinity || !ecpoint.IsValid())
				throw new ArgumentException("Encrypted text is invalid, Invalid ephemeral public key.");

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
			byte[] l = null;
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
		}

		public Key Uncover(Key scan, PubKey ephem)
		{
			var curve = ECKey.Secp256k1;
			var priv = new BigInteger(1, PubKey.GetStealthSharedSecret(scan, ephem))
							.Add(new BigInteger(1, this.ToBytes()))
							.Mod(curve.N)
							.ToByteArrayUnsigned();

			if (priv.Length < 32)
				priv = new byte[32 - priv.Length].Concat(priv).ToArray();

			var key = new Key(priv, fCompressedIn: this.IsCompressed);
			return key;
		}

		public BitcoinSecret GetBitcoinSecret(Network network)
		{
			return new BitcoinSecret(this, network);
		}

		/// <summary>
		/// Same than GetBitcoinSecret
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public BitcoinSecret GetWif(Network network)
		{
			return new BitcoinSecret(this, network);
		}

		public BitcoinEncryptedSecretNoEC GetEncryptedBitcoinSecret(string password, Network network)
		{
			return new BitcoinEncryptedSecretNoEC(this, password, network);
		}

		public string ToString(Network network)
		{
			return new BitcoinSecret(this, network).ToString();
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

		public TransactionSignature Sign(uint256 hash, SigHash sigHash, bool useLowR = true)
		{
			return new TransactionSignature(Sign(hash, useLowR), sigHash);
		}


		public override bool Equals(object obj)
		{
			Key item = obj as Key;
			if (item == null)
				return false;
			return PubKey.Equals(item.PubKey);
		}
		public static bool operator ==(Key a, Key b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;
			if (((object)a == null) || ((object)b == null))
				return false;
			return a.PubKey == b.PubKey;
		}

		public static bool operator !=(Key a, Key b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return PubKey.GetHashCode();
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(vch);
		}
	}
}
