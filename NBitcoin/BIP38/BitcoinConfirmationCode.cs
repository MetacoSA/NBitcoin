using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class BitcoinConfirmationCode : Base58Data
	{

		public BitcoinConfirmationCode(string wif, Network expectedNetwork = null)
			: base(wif, expectedNetwork)
		{
		}
		public BitcoinConfirmationCode(byte[] rawBytes, Network network)
			: base(rawBytes, network)
		{
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
		byte[] _OwnerEntropy;
		public byte[] OwnerEntropy
		{
			get
			{
				if(_OwnerEntropy == null)
				{
					_OwnerEntropy = vchData.Skip(1).Skip(4).Take(8).ToArray();
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

		byte[] _EncryptedPointB;
		byte[] EncryptedPointB
		{
			get
			{
				if(_EncryptedPointB == null)
				{
					_EncryptedPointB = vchData.Skip(1).Skip(4).Skip(8).ToArray();
				}
				return _EncryptedPointB;
			}
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.CONFIRMATION_CODE;
			}
		}

		protected override bool IsValid
		{
			get
			{
				return vchData.Length == 1 + 4 + 8 + 33;
			}
		}


		public bool Check(string passphrase, BitcoinAddress expectedAddress)
		{
			//Derive passfactor using scrypt with ownerentropy and the user's passphrase and use it to recompute passpoint 
			byte[] passfactor = BitcoinEncryptedSecretEC.CalculatePassFactor(passphrase, LotSequence, OwnerEntropy);
			//Derive decryption key for pointb using scrypt with passpoint, addresshash, and ownerentropy
			byte[] passpoint = BitcoinEncryptedSecretEC.CalculatePassPoint(passfactor);
			byte[] derived = BitcoinEncryptedSecretEC.CalculateDecryptionKey(passpoint, AddressHash, OwnerEntropy);

			//Decrypt encryptedpointb to yield pointb
			var pointbprefix = EncryptedPointB[0];
			pointbprefix = (byte)(pointbprefix ^ (byte)(derived[63] & (byte)0x01));

			//Optional since ArithmeticException will catch it, but it saves some times
			if(pointbprefix != 0x02 && pointbprefix != 0x03)
				return false;
			var pointb = BitcoinEncryptedSecret.DecryptKey(EncryptedPointB.Skip(1).ToArray(), derived);
			pointb = new byte[] { pointbprefix }.Concat(pointb).ToArray();

			var param1 = Encoders.Hex.EncodeData(EncryptedPointB.Skip(1).ToArray());
			var param2 = Encoders.Hex.EncodeData(derived);

			//4.ECMultiply pointb by passfactor. Use the resulting EC point as a public key
			var curve = ECKey.CreateCurve();
			ECPoint pointbec = null;
			try
			{
				pointbec = curve.Curve.DecodePoint(pointb);
			}
			catch(ArgumentException)
			{
				return false;
			}
			catch(ArithmeticException)
			{
				return false;
			}
			PubKey pubkey = new PubKey(pointbec.Multiply(new BigInteger(1, passfactor)).GetEncoded());

			//and hash it into address using either compressed or uncompressed public key methodology as specifid in flagbyte.
			pubkey = IsCompressed ? pubkey.Compress() : pubkey.Decompress();

			var actualhash = BitcoinEncryptedSecretEC.HashAddress(pubkey.GetAddress(Network));
			var expectedhash = BitcoinEncryptedSecretEC.HashAddress(expectedAddress);

			return Utils.ArrayEqual(actualhash, expectedhash);
		}
	}
}
