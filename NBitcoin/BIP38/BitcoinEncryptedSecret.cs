using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
			var addressBytes = Encoders.ASCII.DecodeData(key.PubKey.GetAddress(network).ToWif());
			// and take the first four bytes of SHA256(SHA256()) of it. Let's call this "addresshash".
			var addresshash = Hashes.Hash256(addressBytes).ToBytes().Take(4).ToArray();

			var derived = NBitcoin.Crypto.SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), addresshash);

			var encrypted = EncryptKey(vch, derived);



			var version = network.GetVersionBytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC);
			byte flagByte = 0;
			flagByte |= 0x0C0;
			flagByte |= (key.IsCompressed ? (byte)0x20 : (byte)0x00);

			var bytes = version
							.Concat(new byte[] { flagByte })
							.Concat(addresshash)
							.Concat(encrypted).ToArray();
			return Encoders.Base58Check.EncodeData(bytes);
		}

		byte[] _FirstHalf;
		public byte[] EncryptedHalf1
		{
			get
			{
				if(_FirstHalf == null)
				{
					_FirstHalf = vchData.Skip(ValidLength - 32).Take(16).ToArray();
				}
				return _FirstHalf;
			}
		}

		public byte[] _Encrypted;
		public byte[] Encrypted
		{
			get
			{
				if(_Encrypted == null)
				{
					_Encrypted = EncryptedHalf1.Concat(EncryptedHalf2).ToArray();
				}
				return _Encrypted;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.ENCRYPTED_SECRET_KEY_NO_EC;
			}
		}

		public override Key GetKey(string password)
		{
			var derived = NBitcoin.Crypto.SCrypt.BitcoinComputeDerivedKey(password, AddressHash);
			var bitcoinprivkey = DecryptKey(Encrypted, derived);

			var key = new Key(bitcoinprivkey, fCompressedIn: IsCompressed);

			var addressBytes = Encoding.ASCII.GetBytes(key.PubKey.GetAddress(Network).ToString());
			var salt = Hashes.Hash256(addressBytes).ToBytes().Take(4).ToArray();

			if(!Utils.ArrayEqual(salt, AddressHash))
				throw new SecurityException("Invalid password");
			return key;
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
				if(_OwnerEntropy == null)
				{
					_OwnerEntropy = vchData.Skip(ValidLength - 32).Take(8).ToArray();
				}
				return _OwnerEntropy;
			}
		}
		LotSequence _LotSequence;
		public LotSequence LotSequence
		{
			get
			{
				var hasLotSequence = (vchData[0] & (byte)0x04) != 0;
				if(!hasLotSequence)
					return null;
				if(_LotSequence == null)
				{
					_LotSequence = new LotSequence(OwnerEntropy.Skip(4).Take(4).ToArray());
				}
				return _LotSequence;
			}
		}

		byte[] _EncryptedHalfHalf1;
		public byte[] EncryptedHalfHalf1
		{
			get
			{
				if(_EncryptedHalfHalf1 == null)
				{
					_EncryptedHalfHalf1 = vchData.Skip(ValidLength - 32 + 8).Take(8).ToArray();
				}
				return _EncryptedHalfHalf1;
			}
		}

		byte[] _PartialEncrypted;
		public byte[] PartialEncrypted
		{
			get
			{
				if(_PartialEncrypted == null)
				{
					_PartialEncrypted = EncryptedHalfHalf1.Concat(new byte[8]).Concat(EncryptedHalf2).ToArray();
				}
				return _PartialEncrypted;
			}
		}



		public override Base58Type Type
		{
			get
			{
				return Base58Type.ENCRYPTED_SECRET_KEY_EC;
			}
		}

		public override Key GetKey(string password)
		{
			var encrypted = PartialEncrypted.ToArray();
			//Derive passfactor using scrypt with ownerentropy and the user's passphrase and use it to recompute passpoint
			byte[] passfactor = CalculatePassFactor(password, LotSequence, OwnerEntropy);
			var passpoint = CalculatePassPoint(passfactor);

			var derived = SCrypt.BitcoinComputeDerivedKey2(passpoint, this.AddressHash.Concat(this.OwnerEntropy).ToArray());

			//Decrypt encryptedpart1 to yield the remainder of seedb.
			var seedb = BitcoinEncryptedSecret.DecryptSeed(encrypted, derived);
			var factorb = Hashes.Hash256(seedb).ToBytes();

			var curve = ECKey.CreateCurve();

			//Multiply passfactor by factorb mod N to yield the private key associated with generatedaddress.
			var keyNum = new BigInteger(1, passfactor).Multiply(new BigInteger(1, factorb)).Mod(curve.N);
			var keyBytes = keyNum.ToByteArrayUnsigned();
			if(keyBytes.Length < 32)
				keyBytes = new byte[32 - keyBytes.Length].Concat(keyBytes).ToArray();

			var key = new Key(keyBytes, fCompressedIn: IsCompressed);

			var generatedaddress = key.PubKey.GetAddress(Network);
			var addresshash = HashAddress(generatedaddress);

			if(!Utils.ArrayEqual(addresshash, AddressHash))
				throw new SecurityException("Invalid password");

			return key;
		}

		/// <summary>
		/// Take the first four bytes of SHA256(SHA256(generatedaddress)) and call it addresshash.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		internal static byte[] HashAddress(BitcoinAddress address)
		{
			return Hashes.Hash256(Encoders.ASCII.DecodeData(address.ToString())).ToBytes().Take(4).ToArray();
		}

		internal static byte[] CalculatePassPoint(byte[] passfactor)
		{
			return new Key(passfactor, fCompressedIn: true).PubKey.ToBytes();
		}

		internal static byte[] CalculatePassFactor(string password, LotSequence lotSequence, byte[] ownerEntropy)
		{
			byte[] passfactor = null;
			if(lotSequence == null)
			{
				passfactor = SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), ownerEntropy, 32);
			}
			else
			{
				var ownersalt = ownerEntropy.Take(4).ToArray();
				var lotsequence = ownerEntropy.Skip(4).Take(4).ToArray();
				var prefactor = SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), ownersalt, 32);
				passfactor = Hashes.Hash256(prefactor.Concat(ownerEntropy).ToArray()).ToBytes();
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
			return Network.CreateFromBase58Data<BitcoinEncryptedSecret>(wif, expectedNetwork);
		}

		public static BitcoinEncryptedSecretNoEC Generate(Key key, string password, Network network)
		{
			return new BitcoinEncryptedSecretNoEC(key, password, network);
		}


		public BitcoinEncryptedSecret(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		public BitcoinEncryptedSecret(string wif, Network network)
			: base(wif, network)
		{
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
				if(_AddressHash == null)
				{
					_AddressHash = vchData.Skip(1).Take(4).ToArray();
				}
				return _AddressHash;
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
				if(_LastHalf == null)
				{
					_LastHalf = vchData.Skip(ValidLength - 16).ToArray();
				}
				return _LastHalf;
			}
		}
		protected int ValidLength = (1 + 4 + 16 + 16);


		protected override bool IsValid
		{
			get
			{
				var lenOk = vchData.Length == ValidLength;
				if(!lenOk)
					return false;
				var reserved = (vchData[0] & 0x10) == 0 && (vchData[0] & 0x08) == 0;
				return reserved;
			}
		}

		protected byte[] GetHalf1(byte[] b)
		{
			return b.Take(b.Length / 2).ToArray();
		}
		protected byte[] GetHalf2(byte[] b)
		{
			return b.Skip(b.Length / 2).Take(b.Length / 2).ToArray();
		}

		public abstract Key GetKey(string password);
		public BitcoinSecret GetSecret(string password)
		{
			return new BitcoinSecret(GetKey(password), Network);
		}

		internal static Aes CreateAES256()
		{
			var aes = Aes.Create();
			aes.KeySize = 256;
			aes.Mode = CipherMode.ECB;
			aes.IV = new byte[16];
			return aes;
		}

		internal static byte[] EncryptKey(byte[] key, byte[] derived)
		{
			var keyhalf1 = key.Take(16).ToArray();
			var keyhalf2 = key.Skip(16).Take(16).ToArray();
			return EncryptKey(keyhalf1, keyhalf2, derived);
		}

		private static byte[] EncryptKey(byte[] keyhalf1, byte[] keyhalf2, byte[] derived)
		{
			var derivedhalf1 = derived.Take(32).ToArray();
			var derivedhalf2 = derived.Skip(32).Take(32).ToArray();

			var encryptedhalf1 = new byte[16];
			var encryptedhalf2 = new byte[16];

			var aes = BitcoinEncryptedSecret.CreateAES256();
			aes.Key = derivedhalf2;
			var encrypt = aes.CreateEncryptor();
			for(int i = 0 ; i < 16 ; i++)
			{
				derivedhalf1[i] = (byte)(keyhalf1[i] ^ derivedhalf1[i]);
			}
			encrypt.TransformBlock(derivedhalf1, 0, 16, encryptedhalf1, 0);
			for(int i = 0 ; i < 16 ; i++)
			{
				derivedhalf1[16 + i] = (byte)(keyhalf2[i] ^ derivedhalf1[16 + i]);
			}
			encrypt.TransformBlock(derivedhalf1, 16, 16, encryptedhalf2, 0);
			return encryptedhalf1.Concat(encryptedhalf2).ToArray();
		}

		internal static byte[] DecryptKey(byte[] encrypted, byte[] derived)
		{
			var derivedhalf1 = derived.Take(32).ToArray();
			var derivedhalf2 = derived.Skip(32).Take(32).ToArray();

			var encryptedHalf1 = encrypted.Take(16).ToArray();
			var encryptedHalf2 = encrypted.Skip(16).Take(16).ToArray();

			byte[] bitcoinprivkey1 = new byte[16];
			byte[] bitcoinprivkey2 = new byte[16];

			var aes = CreateAES256();
			aes.Key = derivedhalf2;

			var decrypt = aes.CreateDecryptor();

			//Need to call that two time, seems AES bug
			decrypt.TransformBlock(encryptedHalf1, 0, 16, bitcoinprivkey1, 0);
			decrypt.TransformBlock(encryptedHalf1, 0, 16, bitcoinprivkey1, 0);

			for(int i = 0 ; i < 16 ; i++)
			{
				bitcoinprivkey1[i] ^= derivedhalf1[i];
			}

			//Need to call that two time, seems AES bug
			decrypt.TransformBlock(encryptedHalf2, 0, 16, bitcoinprivkey2, 0);
			decrypt.TransformBlock(encryptedHalf2, 0, 16, bitcoinprivkey2, 0);
			for(int i = 0 ; i < 16 ; i++)
			{
				bitcoinprivkey2[i] ^= derivedhalf1[16 + i];
			}

			return bitcoinprivkey1.Concat(bitcoinprivkey2).ToArray();
		}


		internal static byte[] EncryptSeed(byte[] seedb, byte[] derived)
		{
			var derivedhalf1 = derived.Take(32).ToArray();
			var derivedhalf2 = derived.Skip(32).Take(32).ToArray();

			var encryptedhalf1 = new byte[16];
			var encryptedhalf2 = new byte[16];

			var aes = CreateAES256();
			aes.Key = derivedhalf2;
			var encrypt = aes.CreateEncryptor();

			//AES256Encrypt(seedb[0...15] xor derivedhalf1[0...15], derivedhalf2), call the 16-byte result encryptedpart1
			for(int i = 0 ; i < 16 ; i++)
			{
				derivedhalf1[i] = (byte)(seedb[i] ^ derivedhalf1[i]);
			}
			encrypt.TransformBlock(derivedhalf1, 0, 16, encryptedhalf1, 0);

			//AES256Encrypt((encryptedpart1[8...15] + seedb[16...23]) xor derivedhalf1[16...31], derivedhalf2), call the 16-byte result encryptedpart2. The "+" operator is concatenation.
			var half = encryptedhalf1.Skip(8).Take(8).Concat(seedb.Skip(16).Take(8)).ToArray();
			for(int i = 0 ; i < 16 ; i++)
			{
				derivedhalf1[16 + i] = (byte)(half[i] ^ derivedhalf1[16 + i]);
			}
			encrypt.TransformBlock(derivedhalf1, 16, 16, encryptedhalf2, 0);
			return encryptedhalf1.Concat(encryptedhalf2).ToArray();
		}

		internal static byte[] DecryptSeed(byte[] encrypted, byte[] derived)
		{
			byte[] seedb = new byte[24];
			var derivedhalf1 = derived.Take(32).ToArray();
			var derivedhalf2 = derived.Skip(32).Take(32).ToArray();

			var encryptedhalf2 = encrypted.Skip(16).Take(16).ToArray();

			var aes = CreateAES256();
			aes.Key = derivedhalf2;
			var decrypt = aes.CreateDecryptor();

			byte[] half = new byte[16];
			//Decrypt encryptedpart2 using AES256Decrypt to yield the last 8 bytes of seedb and the last 8 bytes of encryptedpart1.
			decrypt.TransformBlock(encryptedhalf2, 0, 16, half, 0);
			decrypt.TransformBlock(encryptedhalf2, 0, 16, half, 0);
			//half = (encryptedpart1[8...15] + seedb[16...23]) xor derivedhalf1[16...31])
			for(int i = 0 ; i < 16 ; i++)
			{
				half[i] = (byte)(half[i] ^ derivedhalf1[16 + i]);
			}

			//half =  (encryptedpart1[8...15] + seedb[16...23])
			var encryptedPart1End = half.Take(8).ToArray();
			for(int i = 0 ; i < 8 ; i++)
			{
				seedb[seedb.Length - i - 1] = half[half.Length - i - 1];
			}
			//Restore missing encrypted part
			for(int i = 0 ; i < 8 ; i++)
			{
				encrypted[i + 8] = half[i];
			}
			var encryptedhalf1 = encrypted.Take(16).ToArray();
			decrypt.TransformBlock(encryptedhalf1, 0, 16, seedb, 0);
			decrypt.TransformBlock(encryptedhalf1, 0, 16, seedb, 0);
			//seedb = seedb[0...15] xor derivedhalf1[0...15]
			for(int i = 0 ; i < 16 ; i++)
			{
				seedb[i] = (byte)(seedb[i] ^ derivedhalf1[i]);
			}
			return seedb;
		}
	}
}
