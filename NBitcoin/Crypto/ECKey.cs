using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Crypto.Generators;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Crypto.Prng;
using NBitcoin.BouncyCastle.Crypto.Signers;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;
using NBitcoin.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Crypto
{
	public class ECKey
	{
		public ECPrivateKeyParameters PrivateKey
		{
			get
			{
				return _Key as ECPrivateKeyParameters;
			}
		}
		ECKeyParameters _Key;


		public static BigInteger HALF_CURVE_ORDER = null;
		public static ECDomainParameters CURVE = null;
		static ECKey()
		{
			X9ECParameters @params = CreateCurve();
			CURVE = new ECDomainParameters(@params.Curve, @params.G, @params.N, @params.H);
			HALF_CURVE_ORDER = @params.N.ShiftRight(1);
		}

		public ECKey(byte[] vch, bool isPrivate)
		{
			if(isPrivate)
				_Key = new ECPrivateKeyParameters(new NBitcoin.BouncyCastle.Math.BigInteger(1, vch), DomainParameter);
			else
			{
				var q = Secp256k1.Curve.DecodePoint(vch);
				_Key = new ECPublicKeyParameters("EC", q, DomainParameter);
			}
		}


		X9ECParameters _Secp256k1;
		public X9ECParameters Secp256k1
		{
			get
			{
				if(_Secp256k1 == null)
					_Secp256k1 = CreateCurve();
				return _Secp256k1;
			}
		}

		public static X9ECParameters CreateCurve()
		{
			return NBitcoin.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
		}
		ECDomainParameters _DomainParameter;
		public ECDomainParameters DomainParameter
		{
			get
			{
				if(_DomainParameter == null)
					_DomainParameter = new ECDomainParameters(Secp256k1.Curve, Secp256k1.G, Secp256k1.N, Secp256k1.H);
				return _DomainParameter;
			}
		}


		public ECDSASignature Sign(uint256 hash)
		{
			AssertPrivateKey();
			DeterministicECDSA signer = new DeterministicECDSA();
			signer.setPrivateKey(PrivateKey);
			var sig = ECDSASignature.FromDER(signer.signHash(hash.ToBytes()));
			return sig.MakeCanonical();
		}

		private void AssertPrivateKey()
		{
			if(PrivateKey == null)
				throw new InvalidOperationException("This key should be a private key for such operation");
		}



		internal bool Verify(uint256 hash, ECDSASignature sig)
		{
			var signer = new ECDsaSigner();
			signer.Init(false, GetPublicKeyParameters());
			return signer.VerifySignature(hash.ToBytes(), sig.R, sig.S);
		}


		public PubKey GetPubKey(bool isCompressed)
		{
			var q = GetPublicKeyParameters().Q;
			//Pub key (q) is composed into X and Y, the compressed form only include X, which can derive Y along with 02 or 03 prepent depending on whether Y in even or odd.
			var result = Secp256k1.Curve.CreatePoint(q.X.ToBigInteger(), q.Y.ToBigInteger(), isCompressed).GetEncoded();
			return new PubKey(result);
		}



		public ECPublicKeyParameters GetPublicKeyParameters()
		{
			if(_Key is ECPublicKeyParameters)
				return (ECPublicKeyParameters)_Key;
			else
			{
				ECPoint q = Secp256k1.G.Multiply(PrivateKey.D);
				return new ECPublicKeyParameters("EC", q, DomainParameter);
			}
		}


		public static ECKey RecoverFromSignature(int recId, ECDSASignature sig, uint256 message, bool compressed)
		{
			if(recId < 0)
				throw new ArgumentException("recId should be positive");
			if(sig.R.SignValue < 0)
				throw new ArgumentException("r should be positive");
			if(sig.S.SignValue < 0)
				throw new ArgumentException("s should be positive");
			if(message == null)
				throw new ArgumentNullException("message");


			var curve = ECKey.CreateCurve();

			// 1.0 For j from 0 to h   (h == recId here and the loop is outside this function)
			//   1.1 Let x = r + jn

			var n = curve.N;
			var i = NBitcoin.BouncyCastle.Math.BigInteger.ValueOf((long)recId / 2);
			var x = sig.R.Add(i.Multiply(n));

			//   1.2. Convert the integer x to an octet string X of length mlen using the conversion routine
			//        specified in Section 2.3.7, where mlen = ⌈(log2 p)/8⌉ or mlen = ⌈m/8⌉.
			//   1.3. Convert the octet string (16 set binary digits)||X to an elliptic curve point R using the
			//        conversion routine specified in Section 2.3.4. If this conversion routine outputs “invalid”, then
			//        do another iteration of Step 1.
			//
			// More concisely, what these points mean is to use X as a compressed public key.
			var prime = ((FpCurve)curve.Curve).Q;
			if(x.CompareTo(prime) >= 0)
			{
				return null;
			}

			// Compressed keys require you to know an extra bit of data about the y-coord as there are two possibilities.
			// So it's encoded in the recId.
			ECPoint R = DecompressKey(x, (recId & 1) == 1);
			//   1.4. If nR != point at infinity, then do another iteration of Step 1 (callers responsibility).

			if(!R.Multiply(n).IsInfinity)
				return null;

			//   1.5. Compute e from M using Steps 2 and 3 of ECDSA signature verification.
			var e = new NBitcoin.BouncyCastle.Math.BigInteger(1, message.ToBytes());
			//   1.6. For k from 1 to 2 do the following.   (loop is outside this function via iterating recId)
			//   1.6.1. Compute a candidate public key as:
			//               Q = mi(r) * (sR - eG)
			//
			// Where mi(x) is the modular multiplicative inverse. We transform this into the following:
			//               Q = (mi(r) * s ** R) + (mi(r) * -e ** G)
			// Where -e is the modular additive inverse of e, that is z such that z + e = 0 (mod n). In the above equation
			// ** is point multiplication and + is point addition (the EC group operator).
			//
			// We can find the additive inverse by subtracting e from zero then taking the mod. For example the additive
			// inverse of 3 modulo 11 is 8 because 3 + 8 mod 11 = 0, and -3 mod 11 = 8.

			var eInv = NBitcoin.BouncyCastle.Math.BigInteger.Zero.Subtract(e).Mod(n);
			var rInv = sig.R.ModInverse(n);
			var srInv = rInv.Multiply(sig.S).Mod(n);
			var eInvrInv = rInv.Multiply(eInv).Mod(n);
			var q = (FpPoint)ECAlgorithms.SumOfTwoMultiplies(curve.G, eInvrInv, R, srInv);
			if(compressed)
			{
				q = new FpPoint(curve.Curve, q.X, q.Y, true);
			}
			return new ECKey(q.GetEncoded(), false);
		}

		private static ECPoint DecompressKey(NBitcoin.BouncyCastle.Math.BigInteger xBN, bool yBit)
		{
			var curve = ECKey.CreateCurve().Curve;
			byte[] compEnc = X9IntegerConverter.IntegerToBytes(xBN, 1 + X9IntegerConverter.GetByteLength(curve));
			compEnc[0] = (byte)(yBit ? 0x03 : 0x02);
			return curve.DecodePoint(compEnc);
		}



		public static ECKey FromDER(byte[] der)
		{

			// To understand this code, see the definition of the ASN.1 format for EC private keys in the OpenSSL source
			// code in ec_asn1.c:
			//
			// ASN1_SEQUENCE(EC_PRIVATEKEY) = {
			//   ASN1_SIMPLE(EC_PRIVATEKEY, version, LONG),
			//   ASN1_SIMPLE(EC_PRIVATEKEY, privateKey, ASN1_OCTET_STRING),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, parameters, ECPKPARAMETERS, 0),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, publicKey, ASN1_BIT_STRING, 1)
			// } ASN1_SEQUENCE_END(EC_PRIVATEKEY)
			//

			Asn1InputStream decoder = new Asn1InputStream(der);
			DerSequence seq = (DerSequence)decoder.ReadObject();
			CheckArgument(seq.Count == 4, "Input does not appear to be an ASN.1 OpenSSL EC private key");
			CheckArgument(((DerInteger)seq[0]).Value.Equals(BigInteger.One),
					"Input is of wrong version");
			byte[] bits = ((DerOctetString)seq[1]).GetOctets();
			decoder.Close();
			return new ECKey(bits, true);
		}

		public static string DumpDer(byte[] der)
		{
			StringBuilder builder = new StringBuilder();
			Asn1InputStream decoder = new Asn1InputStream(der);
			DerSequence seq = (DerSequence)decoder.ReadObject();
			builder.AppendLine("Version : " + Encoders.Hex.EncodeData(seq[0].GetDerEncoded()));
			builder.AppendLine("Private : " + Encoders.Hex.EncodeData(seq[1].GetDerEncoded()));
			builder.AppendLine("Params : " + Encoders.Hex.EncodeData(((DerTaggedObject)seq[2]).GetObject().GetDerEncoded()));
			builder.AppendLine("Public : " + Encoders.Hex.EncodeData(seq[3].GetDerEncoded()));
			decoder.Close();
			return builder.ToString();
		}

		static void CheckArgument(bool predicate, string msg)
		{
			if(!predicate)
			{
				throw new FormatException(msg);
			}
		}

		public byte[] ToDER(bool compressed)
		{
			AssertPrivateKey();
			MemoryStream baos = new MemoryStream();

			// ASN1_SEQUENCE(EC_PRIVATEKEY) = {
			//   ASN1_SIMPLE(EC_PRIVATEKEY, version, LONG),
			//   ASN1_SIMPLE(EC_PRIVATEKEY, privateKey, ASN1_OCTET_STRING),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, parameters, ECPKPARAMETERS, 0),
			//   ASN1_EXP_OPT(EC_PRIVATEKEY, publicKey, ASN1_BIT_STRING, 1)
			// } ASN1_SEQUENCE_END(EC_PRIVATEKEY)
			DerSequenceGenerator seq = new DerSequenceGenerator(baos);
			seq.AddObject(new DerInteger(1)); // version
			seq.AddObject(new DerOctetString(PrivateKey.D.ToByteArrayUnsigned()));


			//Did not managed to generate the same der as brainwallet by using this
			//seq.AddObject(new DerTaggedObject(0, Secp256k1.ToAsn1Object()));
			Asn1Object secp256k1Der = null;
			if(compressed)
			{
				secp256k1Der = DerSequence.FromByteArray(DataEncoders.Encoders.Hex.DecodeData("308182020101302c06072a8648ce3d0101022100fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f300604010004010704210279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141020101"));
			}
			else
			{
				secp256k1Der = DerSequence.FromByteArray(DataEncoders.Encoders.Hex.DecodeData("3081a2020101302c06072a8648ce3d0101022100fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f300604010004010704410479be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8022100fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141020101"));
			}
			seq.AddObject(new DerTaggedObject(0, secp256k1Der));
			seq.AddObject(new DerTaggedObject(1, new DerBitString(GetPubKey(compressed).ToBytes())));
			seq.Close();
			return baos.ToArray();
		}


	}
}
