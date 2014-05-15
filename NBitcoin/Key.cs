using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Key : IBitcoinSerializable
	{
		byte[] vch = new byte[0];
		ECKey _ECKey;
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
			byte[] data = new byte[32];

			do
			{
				RandomUtils.GetBytes(data);
			} while(!Check(data));

			SetBytes(data, data.Length, fCompressedIn);
		}
		public Key(byte[] data, int count = -1, bool fCompressedIn = true)
		{
			if(count == -1)
				count = data.Length;
			if(count != 32)
			{
				throw new FormatException("The size of an EC key should be 32");
			}
			if(Check(data))
			{
				SetBytes(data, count, fCompressedIn);
			}
			else
				throw new FormatException("Invalid EC key");
		}

		private void SetBytes(byte[] data, int count, bool fCompressedIn)
		{
			vch = new byte[32];
			Array.Copy(data, 0, vch, 0, count);
			IsCompressed = fCompressedIn;
			_ECKey = new ECKey(vch, true);
		}

		private bool Check(byte[] vch)
		{
			// Do not convert to OpenSSL's data structures for range-checking keys,
			// it's easy enough to do directly.
			byte[] vchMax = new byte[32]{
        0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
        0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFE,
        0xBA,0xAE,0xDC,0xE6,0xAF,0x48,0xA0,0x3B,
        0xBF,0xD2,0x5E,0x8C,0xD0,0x36,0x41,0x40
    };
			bool fIsZero = true;
			for(int i = 0 ; i < 32 && fIsZero ; i++)
				if(vch[i] != 0)
					fIsZero = false;
			if(fIsZero)
				return false;
			for(int i = 0 ; i < 32 ; i++)
			{
				if(vch[i] < vchMax[i])
					return true;
				if(vch[i] > vchMax[i])
					return false;
			}
			return true;
		}

		PubKey _PubKey;
		public PubKey PubKey
		{
			get
			{
				if(_PubKey == null)
				{
					ECKey key = new ECKey(vch, true);
					_PubKey = key.GetPubKey(IsCompressed);
				}
				return _PubKey;
			}
		}

		public byte[] Sign(uint256 hash)
		{
			var signature = _ECKey.Sign(hash);
			signature = signature.MakeCanonical();
			return signature.ToDER();
		}


		public string SignMessage(String message)
		{
			byte[] data = Utils.FormatMessageForSigning(message);
			var hash = Hashes.Hash256(data);
			return Convert.ToBase64String(SignCompact(hash));
		}


		public byte[] SignCompact(uint256 hash)
		{
			var sig = _ECKey.Sign(hash);
			// Now we have to work backwards to figure out the recId needed to recover the signature.
			int recId = -1;
			for(int i = 0 ; i < 4 ; i++)
			{
				ECKey k = ECKey.RecoverFromSignature(i, sig, hash, IsCompressed);
				if(k != null && k.GetPubKey(IsCompressed).ToHex() == PubKey.ToHex())
				{
					recId = i;
					break;
				}
			}

			if(recId == -1)
				throw new InvalidOperationException("Could not construct a recoverable key. This should never happen.");

			int headerByte = recId + 27 + (IsCompressed ? 4 : 0);

			byte[] sigData = new byte[65];  // 1 header + 32 bytes for R + 32 bytes for S

			sigData[0] = (byte)headerByte;

			Array.Copy(Utils.BigIntegerToBytes(sig.R, 32), 0, sigData, 1, 32);
			Array.Copy(Utils.BigIntegerToBytes(sig.S, 32), 0, sigData, 33, 32);
			return sigData;
		}

		public byte[] ToDER()
		{
			return _ECKey.ToDER(IsCompressed);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref vch);
			if(!stream.Serializing)
			{
				_ECKey = new ECKey(vch, true);
			}
		}

		#endregion


		public Key Derivate(byte[] cc, uint nChild, out byte[] ccChild)
		{
			byte[] l = null;
			byte[] ll = new byte[32];
			byte[] lr = new byte[32];
			if((nChild >> 31) == 0)
			{
				var pubKey = PubKey.ToBytes();
				l = Hashes.BIP32Hash(cc, nChild, pubKey[0], pubKey.Skip(1).ToArray());
			}
			else
			{
				l = Hashes.BIP32Hash(cc, nChild, 0, this.ToBytes());
			}
			Array.Copy(l, ll, 32);
			Array.Copy(l, 32, lr, 0, 32);
			ccChild = lr;


			BigInteger parse256LL = new BigInteger(1, ll);
			BigInteger kPar = new BigInteger(1, vch);
			BigInteger N = ECKey.CURVE.N;

			if(parse256LL.CompareTo(N) >= 0)
				throw new InvalidOperationException("You won a prize ! this should happen very rarely. Take a screenshot, and roll the dice again.");
			var key = parse256LL.Add(kPar).Mod(N);
			if(key == BigInteger.Zero)
				throw new InvalidOperationException("You won the big prize ! this would happen only 1 in 2^127. Take a screenshot, and roll the dice again.");
			return new Key(key.ToByteArrayUnsigned());
		}

		public BitcoinSecret GetBitcoinSecret(Network network)
		{
			return new BitcoinSecret(this, network);
		}

		public BitcoinEncryptedSecretNoEC GetEncryptedBitcoinSecret(string password, Network network)
		{
			//Compute the Bitcoin address (ASCII),
			var addressBytes = Encoders.ASCII.DecodeData(PubKey.GetAddress(network).ToWif());
			// and take the first four bytes of SHA256(SHA256()) of it. Let's call this "addresshash".
			var addresshash = Hashes.Hash256(addressBytes).ToBytes().Take(4).ToArray();

			var derived = NBitcoin.Crypto.SCrypt.BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), addresshash);

			var encryptedhalf1 = new byte[16];
			var encryptedhalf2 = new byte[16];

			var aes = BitcoinEncryptedSecret.CreateAES256();
			aes.Key = derived.Skip(32).Take(32).ToArray();
			var encrypt = aes.CreateEncryptor();
			for(int i = 0 ; i < 16 ; i++)
			{
				derived[i] = (byte)(vch[i] ^ derived[i]);
			}
			encrypt.TransformBlock(derived, 0, 16, encryptedhalf1, 0);
			for(int i = 0 ; i < 16 ; i++)
			{
				derived[16 + i] = (byte)(vch[16 + i] ^ derived[16 + i]);
			}
			encrypt.TransformBlock(derived,16, 16, encryptedhalf2, 0);

			var version = network.GetVersionBytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC);
			byte flagByte = 0;
			flagByte |= 0x0C0;
			flagByte |= (IsCompressed ? (byte)0x20 : (byte)0x00);

			var bytes = version
							.Concat(new byte[] { flagByte })
							.Concat(addresshash)
							.Concat(encryptedhalf1)
							.Concat(encryptedhalf2).ToArray();
			return new BitcoinEncryptedSecretNoEC(Encoders.Base58Check.EncodeData(bytes), network);
		}
	}
}
