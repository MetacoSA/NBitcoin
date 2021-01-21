using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
#if !NO_BC
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
#endif
using System;
using System.Linq;

namespace NBitcoin
{
	public class BitcoinConfirmationCode : Base58Data
	{

		public BitcoinConfirmationCode(string wif, Network expectedNetwork = null)
		{
			Init<BitcoinConfirmationCode>(wif, expectedNetwork);
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
		byte[] _OwnerEntropy;
		public byte[] OwnerEntropy
		{
			get
			{
				return _OwnerEntropy ?? (_OwnerEntropy = vchData.SafeSubarray(5, 8));
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
				if (_LotSequence == null)
				{
					_LotSequence = new LotSequence(OwnerEntropy.SafeSubarray(4, 4));
				}
				return _LotSequence;
			}
		}

		byte[] _EncryptedPointB;
		byte[] EncryptedPointB
		{
			get
			{
				return _EncryptedPointB ?? (_EncryptedPointB = vchData.SafeSubarray(13));
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
			if (pointbprefix != 0x02 && pointbprefix != 0x03)
				return false;
			var pointb = BitcoinEncryptedSecret.DecryptKey(EncryptedPointB.Skip(1).ToArray(), derived);
			pointb = new byte[] { pointbprefix }.Concat(pointb).ToArray();

			//4.ECMultiply pointb by passfactor. Use the resulting EC point as a public key

#if HAS_SPAN
			if (!NBitcoinContext.Instance.TryCreatePubKey(pointb, out var pk) || pk is null)
				return false;
			PubKey pubkey = new PubKey(pk.TweakMul(passfactor), true);
#else
			var curve = ECKey.Secp256k1;
			ECPoint pointbec;
			try
			{
				pointbec = curve.Curve.DecodePoint(pointb);
			}
			catch (ArgumentException)
			{
				return false;
			}
			catch (ArithmeticException)
			{
				return false;
			}
			PubKey pubkey = new PubKey(pointbec.Multiply(new BigInteger(1, passfactor)).GetEncoded());
#endif
			//and hash it into address using either compressed or uncompressed public key methodology as specifid in flagbyte.
			pubkey = IsCompressed ? pubkey.Compress() : pubkey.Decompress();

			var actualhash = BitcoinEncryptedSecretEC.HashAddress(pubkey.GetAddress(ScriptPubKeyType.Legacy, Network));
			var expectedhash = BitcoinEncryptedSecretEC.HashAddress(expectedAddress);

			return Utils.ArrayEqual(actualhash, expectedhash);
		}
	}
}
