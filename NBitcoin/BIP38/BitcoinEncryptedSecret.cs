using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System.Linq;
using System.Security;
using System.Text;
using System.Diagnostics.CodeAnalysis;
#if !NO_BC
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Crypto.Paddings;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Crypto.Engines;
#endif
#if !WINDOWS_UWP && !USEBC
using System.Security.Cryptography;
#endif

namespace NBitcoin
{
	public class BitcoinEncryptedSecretNoEC : BitcoinEncryptedSecret
	{

		public BitcoinEncryptedSecretNoEC(string wif, Network expectedNetwork = null)
			: base(wif, expectedNetwork)
		{
		}

		public BitcoinEncryptedSecretNoEC(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		public BitcoinEncryptedSecretNoEC(Key key, string password, Network network)
			: base(GenerateWif(key, password, network), network)
		{

		}

		private static string GenerateWif(Key key, string password, Network network)
		{
			var vch = key.ToBytes();
			//Compute the Bitcoin address (ASCII),
			var addressBytes = Encoders.ASCII.DecodeData(key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString());
			// and take the first four bytes of SHA256(SHA256()) of it. Let's call this "addresshash".
			var addresshash = Hashes.DoubleSHA256(addressBytes).ToBytes().SafeSubarray(0, 4);

			var derived = SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), addresshash);

			var encrypted = EncryptKey(vch, derived);



			var version = network.GetVersionBytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, true);
			byte flagByte = 0;
			flagByte |= 0x0C0;
			flagByte |= (key.IsCompressed ? (byte)0x20 : (byte)0x00);

			var bytes = version
							.Concat(new[] { flagByte })
							.Concat(addresshash)
							.Concat(encrypted).ToArray();
			return network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(bytes);
		}

		byte[] _FirstHalf;
		public byte[] EncryptedHalf1
		{
			get
			{
				return _FirstHalf ?? (_FirstHalf = vchData.SafeSubarray(ValidLength - 32, 16));
			}
		}

		private byte[] _Encrypted;
		public byte[] Encrypted
		{
			get
			{
				return _Encrypted ?? (_Encrypted = EncryptedHalf1.Concat(EncryptedHalf2).ToArray());
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.ENCRYPTED_SECRET_KEY_NO_EC;
			}
		}

		public override bool TryGetKey(string password, [MaybeNullWhen(false)] out Key key)
		{
			var derived = SCrypt.BitcoinComputeDerivedKey(password, AddressHash);
			var bitcoinprivkey = DecryptKey(Encrypted, derived);

			key = new Key(bitcoinprivkey, fCompressedIn: IsCompressed);

			var addressBytes = Encoders.ASCII.DecodeData(key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network).ToString());
			var salt = Hashes.DoubleSHA256(addressBytes).ToBytes().SafeSubarray(0, 4);
			if (!Utils.ArrayEqual(salt, AddressHash))
				key = null;
			return key is Key;
		}


	}

	public class DecryptionResult
	{
		public Key Key
		{
			get;
			set;
		}
		public LotSequence LotSequence
		{
			get;
			set;
		}
	}
	public class BitcoinEncryptedSecretEC : BitcoinEncryptedSecret
	{
		public BitcoinEncryptedSecretEC(string wif, Network expectedNetwork = null)
			: base(wif, expectedNetwork)
		{
		}

		public BitcoinEncryptedSecretEC(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		byte[] _OwnerEntropy;
		public byte[] OwnerEntropy
		{
			get
			{
				return _OwnerEntropy ?? (_OwnerEntropy = vchData.SafeSubarray(ValidLength - 32, 8));
			}
		}
		LotSequence _LotSequence;
		public LotSequence LotSequence
		{
			get
			{
				var hasLotSequence = (vchData[0] & 0x04) != 0;
				if (!hasLotSequence)
					return null;
				return _LotSequence ?? (_LotSequence = new LotSequence(OwnerEntropy.SafeSubarray(4, 4)));
			}
		}

		byte[] _EncryptedHalfHalf1;
		public byte[] EncryptedHalfHalf1
		{
			get
			{
				return _EncryptedHalfHalf1 ?? (_EncryptedHalfHalf1 = vchData.SafeSubarray(ValidLength - 32 + 8, 8));
			}
		}

		byte[] _PartialEncrypted;
		public byte[] PartialEncrypted
		{
			get
			{
				return _PartialEncrypted ?? (_PartialEncrypted = EncryptedHalfHalf1.Concat(new byte[8]).Concat(EncryptedHalf2).ToArray());
			}
		}



		public override Base58Type Type
		{
			get
			{
				return Base58Type.ENCRYPTED_SECRET_KEY_EC;
			}
		}

		public override bool TryGetKey(string password, [MaybeNullWhen(false)] out Key key)
		{
			var encrypted = PartialEncrypted.ToArray();
			//Derive passfactor using scrypt with ownerentropy and the user's passphrase and use it to recompute passpoint
			byte[] passfactor = CalculatePassFactor(password, LotSequence, OwnerEntropy);
			var passpoint = CalculatePassPoint(passfactor);

			var derived = SCrypt.BitcoinComputeDerivedKey2(passpoint, this.AddressHash.Concat(this.OwnerEntropy).ToArray());

			//Decrypt encryptedpart1 to yield the remainder of seedb.
			var seedb = DecryptSeed(encrypted, derived);
			var factorb = Hashes.DoubleSHA256(seedb).ToBytes();
#if HAS_SPAN
			var eckey = NBitcoinContext.Instance.CreateECPrivKey(passfactor).TweakMul(factorb);
			key = new Key(eckey, IsCompressed);
#else
			var curve = ECKey.Secp256k1;

			//Multiply passfactor by factorb mod N to yield the private key associated with generatedaddress.
			var keyNum = new BigInteger(1, passfactor).Multiply(new BigInteger(1, factorb)).Mod(curve.N);
			var keyBytes = keyNum.ToByteArrayUnsigned();
			if (keyBytes.Length < 32)
				keyBytes = new byte[32 - keyBytes.Length].Concat(keyBytes).ToArray();

			key = new Key(keyBytes, fCompressedIn: IsCompressed);
#endif
			var generatedaddress = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network);
			var addresshash = HashAddress(generatedaddress);

			if (!Utils.ArrayEqual(addresshash, AddressHash))
				key = null;
			return key is Key;
		}

		/// <summary>
		/// Take the first four bytes of SHA256(SHA256(generatedaddress)) and call it addresshash.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		internal static byte[] HashAddress(BitcoinAddress address)
		{
			return Hashes.DoubleSHA256(Encoders.ASCII.DecodeData(address.ToString())).ToBytes().Take(4).ToArray();
		}

		internal static byte[] CalculatePassPoint(byte[] passfactor)
		{
			return new Key(passfactor, fCompressedIn: true).PubKey.ToBytes();
		}

		internal static byte[] CalculatePassFactor(string password, LotSequence lotSequence, byte[] ownerEntropy)
		{
			byte[] passfactor;
			if (lotSequence == null)
			{
				passfactor = SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), ownerEntropy, 32);
			}
			else
			{
				var ownersalt = ownerEntropy.SafeSubarray(0, 4);
				var prefactor = SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), ownersalt, 32);
				passfactor = Hashes.DoubleSHA256(prefactor.Concat(ownerEntropy).ToArray()).ToBytes();
			}
			return passfactor;
		}

		internal static byte[] CalculateDecryptionKey(byte[] Passpoint, byte[] addresshash, byte[] ownerEntropy)
		{
			return SCrypt.BitcoinComputeDerivedKey2(Passpoint, addresshash.Concat(ownerEntropy).ToArray());
		}

	}

	public abstract class BitcoinEncryptedSecret : Base58Data
	{
		public static BitcoinEncryptedSecret Create(string wif, Network expectedNetwork = null)
		{
			return Network.Parse<BitcoinEncryptedSecret>(wif, expectedNetwork);
		}

		public static BitcoinEncryptedSecretNoEC Generate(Key key, string password, Network network)
		{
			return new BitcoinEncryptedSecretNoEC(key, password, network);
		}


		protected BitcoinEncryptedSecret(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		protected BitcoinEncryptedSecret(string wif, Network expectedNetwork)
		{
			Init<BitcoinEncryptedSecret>(wif, expectedNetwork);
		}


		public bool EcMultiply
		{
			get
			{
				return this is BitcoinEncryptedSecretEC;
			}
		}



		byte[] _AddressHash;
		public byte[] AddressHash
		{
			get
			{
				return _AddressHash ?? (_AddressHash = vchData.SafeSubarray(1, 4));
			}
		}
		public bool IsCompressed
		{
			get
			{
				return (vchData[0] & 0x20) != 0;
			}
		}

		byte[] _LastHalf;
		public byte[] EncryptedHalf2
		{
			get
			{
				return _LastHalf ?? (_LastHalf = vchData.Skip(ValidLength - 16).ToArray());
			}
		}
		protected int ValidLength = (1 + 4 + 16 + 16);


		protected override bool IsValid
		{
			get
			{
				var lenOk = vchData.Length == ValidLength;
				if (!lenOk)
					return false;
				var reserved = (vchData[0] & 0x10) == 0 && (vchData[0] & 0x08) == 0;
				return reserved;
			}
		}

		/// <summary>
		/// Get the decrypted private key
		/// </summary>
		/// <param name="password">The password</param>
		/// <returns>The decrypted key</returns>
		/// <exception cref="SecurityException">Invalid password</exception>
		public Key GetKey(string password)
		{
			if (TryGetKey(password, out var k))
				return k;
			throw new SecurityException("Invalid password (or invalid Network)");
		}
		/// <summary>
		/// Get the decrypted private key
		/// </summary>
		/// <param name="password">The password</param>
		/// <param name="key">The decrypted key if successfull</param>
		/// <returns>True if successfull</returns>
		public abstract bool TryGetKey(string password, [MaybeNullWhen(false)] out Key key);
		public BitcoinSecret GetSecret(string password)
		{
			return new BitcoinSecret(GetKey(password), Network);
		}

#if USEBC || WINDOWS_UWP
		internal static PaddedBufferedBlockCipher CreateAES256(bool encryption, byte[] key)
		{
			var aes = new PaddedBufferedBlockCipher(new AesFastEngine(), new Pkcs7Padding());
			aes.Init(encryption, new KeyParameter(key));
			aes.ProcessBytes(new byte[16], 0, 16, new byte[16], 0);
			return aes;
		}
#else
		internal static Aes CreateAES256()
		{
			var aes = Aes.Create();
			aes.KeySize = 256;
			aes.Mode = CipherMode.ECB;
			aes.IV = new byte[16];
			return aes;
		}
#endif
		internal static byte[] EncryptKey(byte[] key, byte[] derived)
		{
			var keyhalf1 = key.SafeSubarray(0, 16);
			var keyhalf2 = key.SafeSubarray(16, 16);
			return EncryptKey(keyhalf1, keyhalf2, derived);
		}

		private static byte[] EncryptKey(byte[] keyhalf1, byte[] keyhalf2, byte[] derived)
		{
			var derivedhalf1 = derived.SafeSubarray(0, 32);
			var derivedhalf2 = derived.SafeSubarray(32, 32);

			var encryptedhalf1 = new byte[16];
			var encryptedhalf2 = new byte[16];
#if USEBC || WINDOWS_UWP
			var aes = BitcoinEncryptedSecret.CreateAES256(true, derivedhalf2);
#else
			var aes = CreateAES256();
			aes.Key = derivedhalf2;
			var encrypt = aes.CreateEncryptor();
#endif

			for (int i = 0; i < 16; i++)
			{
				derivedhalf1[i] = (byte)(keyhalf1[i] ^ derivedhalf1[i]);
			}
#if USEBC || WINDOWS_UWP
			aes.ProcessBytes(derivedhalf1, 0, 16, encryptedhalf1, 0);
			aes.ProcessBytes(derivedhalf1, 0, 16, encryptedhalf1, 0);
#else
			encrypt.TransformBlock(derivedhalf1, 0, 16, encryptedhalf1, 0);
#endif
			for (int i = 0; i < 16; i++)
			{
				derivedhalf1[16 + i] = (byte)(keyhalf2[i] ^ derivedhalf1[16 + i]);
			}
#if USEBC || WINDOWS_UWP
			aes.ProcessBytes(derivedhalf1, 16, 16, encryptedhalf2, 0);
			aes.ProcessBytes(derivedhalf1, 16, 16, encryptedhalf2, 0);
#else
			encrypt.TransformBlock(derivedhalf1, 16, 16, encryptedhalf2, 0);
#endif
			return encryptedhalf1.Concat(encryptedhalf2).ToArray();
		}

		internal static byte[] DecryptKey(byte[] encrypted, byte[] derived)
		{
			var derivedhalf1 = derived.SafeSubarray(0, 32);
			var derivedhalf2 = derived.SafeSubarray(32, 32);

			var encryptedHalf1 = encrypted.SafeSubarray(0, 16);
			var encryptedHalf2 = encrypted.SafeSubarray(16, 16);

			byte[] bitcoinprivkey1 = new byte[16];
			byte[] bitcoinprivkey2 = new byte[16];

#if USEBC || WINDOWS_UWP
			var aes = CreateAES256(false, derivedhalf2);
			aes.ProcessBytes(encryptedHalf1, 0, 16, bitcoinprivkey1, 0);
			aes.ProcessBytes(encryptedHalf1, 0, 16, bitcoinprivkey1, 0);
#else
			var aes = CreateAES256();
			aes.Key = derivedhalf2;
			var decrypt = aes.CreateDecryptor();
			//Need to call that two time, seems AES bug
			decrypt.TransformBlock(encryptedHalf1, 0, 16, bitcoinprivkey1, 0);
			decrypt.TransformBlock(encryptedHalf1, 0, 16, bitcoinprivkey1, 0);
#endif



			for (int i = 0; i < 16; i++)
			{
				bitcoinprivkey1[i] ^= derivedhalf1[i];
			}
#if USEBC || WINDOWS_UWP
			aes.ProcessBytes(encryptedHalf2, 0, 16, bitcoinprivkey2, 0);
			aes.ProcessBytes(encryptedHalf2, 0, 16, bitcoinprivkey2, 0);
#else
			//Need to call that two time, seems AES bug
			decrypt.TransformBlock(encryptedHalf2, 0, 16, bitcoinprivkey2, 0);
			decrypt.TransformBlock(encryptedHalf2, 0, 16, bitcoinprivkey2, 0);
#endif
			for (int i = 0; i < 16; i++)
			{
				bitcoinprivkey2[i] ^= derivedhalf1[16 + i];
			}

			return bitcoinprivkey1.Concat(bitcoinprivkey2).ToArray();
		}


		internal static byte[] EncryptSeed(byte[] seedb, byte[] derived)
		{
			var derivedhalf1 = derived.SafeSubarray(0, 32);
			var derivedhalf2 = derived.SafeSubarray(32, 32);

			var encryptedhalf1 = new byte[16];
			var encryptedhalf2 = new byte[16];

#if USEBC || WINDOWS_UWP
			var aes = CreateAES256(true, derivedhalf2);
#else
			var aes = CreateAES256();
			aes.Key = derivedhalf2;
			var encrypt = aes.CreateEncryptor();
#endif
			//AES256Encrypt(seedb[0...15] xor derivedhalf1[0...15], derivedhalf2), call the 16-byte result encryptedpart1
			for (int i = 0; i < 16; i++)
			{
				derivedhalf1[i] = (byte)(seedb[i] ^ derivedhalf1[i]);
			}
#if USEBC || WINDOWS_UWP
			aes.ProcessBytes(derivedhalf1, 0, 16, encryptedhalf1, 0);
			aes.ProcessBytes(derivedhalf1, 0, 16, encryptedhalf1, 0);
#else
			encrypt.TransformBlock(derivedhalf1, 0, 16, encryptedhalf1, 0);
#endif

			//AES256Encrypt((encryptedpart1[8...15] + seedb[16...23]) xor derivedhalf1[16...31], derivedhalf2), call the 16-byte result encryptedpart2. The "+" operator is concatenation.
			var half = encryptedhalf1.SafeSubarray(8, 8).Concat(seedb.SafeSubarray(16, 8)).ToArray();
			for (int i = 0; i < 16; i++)
			{
				derivedhalf1[16 + i] = (byte)(half[i] ^ derivedhalf1[16 + i]);
			}
#if USEBC || WINDOWS_UWP
			aes.ProcessBytes(derivedhalf1, 16, 16, encryptedhalf2, 0);
			aes.ProcessBytes(derivedhalf1, 16, 16, encryptedhalf2, 0);
#else
			encrypt.TransformBlock(derivedhalf1, 16, 16, encryptedhalf2, 0);
#endif
			return encryptedhalf1.Concat(encryptedhalf2).ToArray();
		}

		internal static byte[] DecryptSeed(byte[] encrypted, byte[] derived)
		{
			byte[] seedb = new byte[24];
			var derivedhalf1 = derived.SafeSubarray(0, 32);
			var derivedhalf2 = derived.SafeSubarray(32, 32);

			var encryptedhalf2 = encrypted.SafeSubarray(16, 16);
#if USEBC || WINDOWS_UWP
			var aes = CreateAES256(false, derivedhalf2);
#else
			var aes = CreateAES256();
			aes.Key = derivedhalf2;
			var decrypt = aes.CreateDecryptor();
#endif
			byte[] half = new byte[16];
			//Decrypt encryptedpart2 using AES256Decrypt to yield the last 8 bytes of seedb and the last 8 bytes of encryptedpart1.
#if USEBC || WINDOWS_UWP
			aes.ProcessBytes(encryptedhalf2, 0, 16, half, 0);
			aes.ProcessBytes(encryptedhalf2, 0, 16, half, 0);
#else
			decrypt.TransformBlock(encryptedhalf2, 0, 16, half, 0);
			decrypt.TransformBlock(encryptedhalf2, 0, 16, half, 0);

#endif
			//half = (encryptedpart1[8...15] + seedb[16...23]) xor derivedhalf1[16...31])
			for (int i = 0; i < 16; i++)
			{
				half[i] = (byte)(half[i] ^ derivedhalf1[16 + i]);
			}

			//half =  (encryptedpart1[8...15] + seedb[16...23])
			for (int i = 0; i < 8; i++)
			{
				seedb[seedb.Length - i - 1] = half[half.Length - i - 1];
			}
			//Restore missing encrypted part
			for (int i = 0; i < 8; i++)
			{
				encrypted[i + 8] = half[i];
			}
			var encryptedhalf1 = encrypted.SafeSubarray(0, 16);
#if USEBC || WINDOWS_UWP
			aes.ProcessBytes(encryptedhalf1, 0, 16, seedb, 0);
			aes.ProcessBytes(encryptedhalf1, 0, 16, seedb, 0);
#else
			decrypt.TransformBlock(encryptedhalf1, 0, 16, seedb, 0);
			decrypt.TransformBlock(encryptedhalf1, 0, 16, seedb, 0);
#endif
			//seedb = seedb[0...15] xor derivedhalf1[0...15]
			for (int i = 0; i < 16; i++)
			{
				seedb[i] = (byte)(seedb[i] ^ derivedhalf1[i]);
			}
			return seedb;
		}
	}
}
