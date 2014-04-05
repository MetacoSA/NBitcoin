using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
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
				_Key = new ECPrivateKeyParameters(new Org.BouncyCastle.Math.BigInteger(1, vch), DomainParameter);
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
			return Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
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
			ECDsaSigner signer = new ECDsaSigner();
			signer.Init(true, PrivateKey);
			BigInteger[] components = signer.GenerateSignature(hash.ToBytes());
			ECDSASignature signature = new ECDSASignature(components[0], components[1]);
			signature.EnsureCanonical();
			return signature;
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



		private ECPublicKeyParameters GetPublicKeyParameters()
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
			var i = Org.BouncyCastle.Math.BigInteger.ValueOf((long)recId / 2);
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
			var e = new Org.BouncyCastle.Math.BigInteger(1, message.ToBytes());
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

			var eInv = Org.BouncyCastle.Math.BigInteger.Zero.Subtract(e).Mod(n);
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

		private static ECPoint DecompressKey(Org.BouncyCastle.Math.BigInteger xBN, bool yBit)
		{
			var curve = ECKey.CreateCurve().Curve;
			byte[] compEnc = X9IntegerConverter.IntegerToBytes(xBN, 1 + X9IntegerConverter.GetByteLength(curve));
			compEnc[0] = (byte)(yBit ? 0x03 : 0x02);
			return curve.DecodePoint(compEnc);
		}



	}
}
