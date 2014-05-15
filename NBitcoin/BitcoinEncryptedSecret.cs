using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
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

		public BitcoinEncryptedSecretNoEC(string wif, Network network)
			: base(wif, network)
		{
		}

		public BitcoinEncryptedSecretNoEC(byte[] raw, Network network)
			: base(raw, network)
		{
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

		public override Base58Type Type
		{
			get
			{
				return Base58Type.ENCRYPTED_SECRET_KEY_NO_EC;
			}
		}

		public override Key GetKey(string password)
		{
			var derived = NBitcoin.Crypto.SCrypt.BitcoinComputeDerivedKey(password, AddressSalt);
			var derivedhalf1 = derived.Take(32).ToArray();
			var derivedhalf2 = derived.Skip(32).Take(32).ToArray();


			byte[] bitcoinprivkey1 = new byte[16];
			byte[] bitcoinprivkey2 = new byte[16];

			var aes = CreateAES256();
			aes.Key = derivedhalf2;

			var decrypt = aes.CreateDecryptor();

			//Need to call that two time, seems AES bug
			decrypt.TransformBlock(EncryptedHalf1, 0, 16, bitcoinprivkey1, 0);
			decrypt.TransformBlock(EncryptedHalf1, 0, 16, bitcoinprivkey1, 0);

			for(int i = 0 ; i < 16 ; i++)
			{
				bitcoinprivkey1[i] ^= derivedhalf1[i];
			}

			//Need to call that two time, seems AES bug
			decrypt.TransformBlock(EncryptedHalf2, 0, 16, bitcoinprivkey2, 0);
			decrypt.TransformBlock(EncryptedHalf2, 0, 16, bitcoinprivkey2, 0);
			for(int i = 0 ; i < 16 ; i++)
			{
				bitcoinprivkey2[i] ^= derivedhalf1[16 + i];
			}

			var key = new Key(bitcoinprivkey1.Concat(bitcoinprivkey2).ToArray(), fCompressedIn: IsCompressed);

			var addressBytes = Encoding.ASCII.GetBytes(key.PubKey.GetAddress(Network).ToString());
			var salt = Hashes.Hash256(addressBytes).ToBytes().Take(4).ToArray();

			if(Utils.ArrayEqual(salt, AddressSalt))
				return key;

			throw new SecurityException("Invalid password");
		}
	}
	public class BitcoinEncryptedSecretEC : BitcoinEncryptedSecret
	{

		public BitcoinEncryptedSecretEC(string wif, Network network)
			: base(wif, network)
		{
		}

		public BitcoinEncryptedSecretEC(byte[] raw, Network network)
			: base(raw, network)
		{
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
			throw new NotImplementedException();
		}
	}

	public abstract class BitcoinEncryptedSecret : Base58Data
	{
		public static BitcoinEncryptedSecret Create(string wif, Network network)
		{
			var raw = Encoders.Base58Check.DecodeData(wif);
			var version = raw.Take(2).ToArray();
			if(Utils.ArrayEqual(version, network.GetVersionBytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC)))
				return new BitcoinEncryptedSecretNoEC(wif, network);
			if(Utils.ArrayEqual(version, network.GetVersionBytes(Base58Type.ENCRYPTED_SECRET_KEY_EC)))
				return new BitcoinEncryptedSecretEC(wif, network);
			throw new FormatException("Invalid encrypted secret");
		}

		public BitcoinEncryptedSecret(byte[] raw, Network network)
			: base(raw, network)
		{
		}

		public BitcoinEncryptedSecret(string wif, Network network)
			: base(wif, network)
		{
			//42 : non EC, 43 : EC
		}


		public bool EcMultiply
		{
			get
			{
				return this is BitcoinEncryptedSecretEC;
			}
		}

		public bool IsCompressed
		{
			get
			{
				return (vchData[0] & 0x20) != 0;
			}
		}

		byte[] _AddressSalt;
		public byte[] AddressSalt
		{
			get
			{
				if(_AddressSalt == null)
				{
					_AddressSalt = vchData.Skip(1).Take(4).ToArray();
				}
				return _AddressSalt;
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




		byte[] content; //(16B) content

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

		internal static Aes CreateAES256()
		{
			var aes = Aes.Create();
			aes.KeySize = 256;
			aes.Mode = CipherMode.ECB;
			aes.IV = new byte[16];
			return aes;
		}
	}
}
