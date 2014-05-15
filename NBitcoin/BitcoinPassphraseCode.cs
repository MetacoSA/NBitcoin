using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class EncryptedKeyResult
	{
		public EncryptedKeyResult(BitcoinEncryptedSecretEC key, BitcoinAddress address, byte[] seed)
		{
			_EncryptedKey = key;
			_GeneratedAddress = address;
			_Seed = seed;
		}
		private readonly BitcoinEncryptedSecretEC _EncryptedKey;
		public BitcoinEncryptedSecretEC EncryptedKey
		{
			get
			{
				return _EncryptedKey;
			}
		}
		private readonly BitcoinAddress _GeneratedAddress;
		public BitcoinAddress GeneratedAddress
		{
			get
			{
				return _GeneratedAddress;
			}
		}

		private readonly byte[] _Seed;
		public byte[] Seed
		{
			get
			{
				return _Seed;
			}
		}
	}
	public class BitcoinPassphraseCode : Base58Data
	{
		public static BitcoinPassphraseCode Generate(string passphrase, Network network,
														byte[] ownersalt = null)
		{
			//ownersalt is 8 random bytes
			ownersalt = ownersalt ?? RandomUtils.GetBytes(8);
			//ownerentropy becomes an alias for ownersalt
			var ownerEntropy = ownersalt;
			var passfactor = SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(passphrase), ownersalt,32);

			var passpoint = new Key(passfactor, fCompressedIn: true).PubKey.ToBytes();

			var bytes =
				network.GetVersionBytes(Base58Type.PASSPHRASE_CODE)
				.Concat(ownersalt)
				.Concat(passpoint)
				.ToArray();
			return new BitcoinPassphraseCode(Encoders.Base58Check.EncodeData(bytes), Network.Main);
		}
		public BitcoinPassphraseCode(byte[] passpoint, byte[] ownerentropy, Network network)
			: base(GenerateWif(passpoint, ownerentropy, network), network)
		{

		}

		public BitcoinPassphraseCode(string wif, Network network)
			: base(wif, network)
		{
		}

		private static string GenerateWif(byte[] passpoint, byte[] ownerentropy, Network network)
		{
			var bytes =
				network.GetVersionBytes(Base58Type.PASSPHRASE_CODE)
				.Concat(ownerentropy)
				.Concat(passpoint).ToArray();
			return Encoders.Base58Check.EncodeData(bytes);
		}

		public EncryptedKeyResult GenerateEncryptedSecret(bool isCompressed = true, byte[] seedb = null)
		{
			//Set flagbyte.
			byte flagBytes = 0;
			//Turn on bit 0x20 if the Bitcoin address will be formed by hashing the compressed public key
			flagBytes |= isCompressed ? (byte)0x20 : (byte)0x00;
			//TODO : Turn on bit 0x04 if ownerentropy contains a value for lotsequence.

			//Generate 24 random bytes, call this seedb. Take SHA256(SHA256(seedb)) to yield 32 bytes, call this factorb.
			seedb = seedb ?? RandomUtils.GetBytes(24);
			
			var factorb = Hashes.Hash256(seedb).ToBytes();

			//ECMultiply passpoint by factorb.
			var curve = ECKey.CreateCurve();
			var point = curve.Curve.DecodePoint(Passpoint);

			//and hash it into a Bitcoin address using either compressed or uncompressed public key methodology (specify which methodology is used inside flagbyte). This is the generated Bitcoin address, call it generatedaddress.
			var pubPoint = point.Multiply(new BigInteger(1, factorb));

			//Use the resulting EC point as a public key
			var pubKey = new PubKey(pubPoint.GetEncoded());

			//and hash it into a Bitcoin address using either compressed or uncompressed public key
			pubKey = isCompressed ? pubKey.Compress() : pubKey.Decompress();

			//call it generatedaddress.
			var generatedaddress = pubKey.GetAddress(Network);

			//Take the first four bytes of SHA256(SHA256(generatedaddress)) and call it addresshash.
			var addresshash = Hashes.Hash256(Encoders.ASCII.DecodeData(generatedaddress.ToString())).ToBytes()
							.Take(4).ToArray();

			//Derive a second key from passpoint using scrypt
			//salt is addresshash + ownerentropy
			var derived = SCrypt.BitcoinComputeDerivedKey2(Passpoint, addresshash.Concat(this.OwnerEntropy).ToArray());

			//Now we will encrypt seedb.

			var encrypted = BitcoinEncryptedSecret.EncryptSeed
							(seedb,
							derived);

			//0x01 0x43 + flagbyte + addresshash + ownerentropy + encryptedpart1[0...7] + encryptedpart2 which totals 39 bytes
			var bytes =
				Network.GetVersionBytes(Base58Type.ENCRYPTED_SECRET_KEY_EC)
				.Concat(new byte[] { flagBytes })
				.Concat(addresshash)
				.Concat(this.OwnerEntropy)
				.Concat(encrypted.Take(8).ToArray())
				.Concat(encrypted.Skip(16).ToArray())
				.ToArray();

			var encryptedSecret = new BitcoinEncryptedSecretEC(Encoders.Base58Check.EncodeData(bytes), Network);
			return new EncryptedKeyResult(encryptedSecret, generatedaddress, seedb);
		}

		byte[] _OwnerEntropy;
		public byte[] OwnerEntropy
		{
			get
			{
				if(_OwnerEntropy == null)
				{
					_OwnerEntropy = vchData.Take(8).ToArray();
				}
				return _OwnerEntropy;
			}
		}
		byte[] _Passpoint;
		public byte[] Passpoint
		{
			get
			{
				if(_Passpoint == null)
				{
					_Passpoint = vchData.Skip(8).ToArray();
				}
				return _Passpoint;
			}
		}

		protected override bool IsValid
		{
			get
			{
				return 8 + 33 == vchData.Length;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.PASSPHRASE_CODE;
			}
		}
	}
}
