using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.BouncyCastle.Asn1.X9;
using System.Security.Cryptography;
using NBitcoin.BouncyCastle.Math.EC.Custom.Sec;
using NBitcoin.DataEncoders;

namespace NBitcoin.Crypto
{

	class Utilsx
	{
		public static byte[] ToBytes(BigInteger me)
		{
			var buff = me.ToByteArrayUnsigned();
			var zeros = 32 - buff.Length;
			if(zeros==0)
				return buff;
			else if(zeros > 0)
				return ByteArrayExtensions.Concat(new byte[zeros], buff);
			else
				return buff.SafeSubarray(0, 32);
		}
	}

	public class SchnorrSignature
	{
		public BigInteger R { get; }
		public BigInteger S { get; }

		public static SchnorrSignature Parse(string hex)
		{
			var bytes = Encoders.Hex.DecodeData(hex);
			return new SchnorrSignature(bytes);
		}

		public SchnorrSignature(byte[] bytes)
		{
			if(bytes.Length != 64)
				throw new ArgumentException("Invalid schnorr signature lenght.");

			R = new BigInteger(bytes.SafeSubarray(0,32));
			S = new BigInteger(bytes.SafeSubarray(32,32));
		}

		public SchnorrSignature(BigInteger r, BigInteger s)
		{
			R = r;
			S = s;
		}

		public byte[] ToBytes()
		{
			return Utilsx.ToBytes(R).Concat( Utilsx.ToBytes(S) );
		}
	}

	public class SchnorrSigner
	{
		private static X9ECParameters Secp256k1 =  NBitcoin.BouncyCastle.Crypto.EC.CustomNamedCurves.Secp256k1;
		private static BigInteger PP = ((SecP256K1Curve)Secp256k1.Curve).QQ;

		public SchnorrSignature Sign(uint256 m, BigInteger secret)
		{
			var k = new BigInteger(1, Hashes.SHA256(Utilsx.ToBytes(secret).Concat(m.ToBytes(false))));
			var R = Secp256k1.G.Multiply(k).Normalize();
			var (Xr, Yr) = (R.XCoord.ToBigInteger(), R.YCoord.ToBigInteger());

			if( BigInteger.Jacobi(Yr, PP) != 1)
				k = Secp256k1.N.Subtract(k);

			var P = Secp256k1.G.Multiply(secret); 
			var keyPrefixedM = Utilsx.ToBytes(Xr).Concat( P.GetEncoded(true), m.ToBytes(false) );
			var e = new BigInteger(1, Hashes.SHA256(keyPrefixedM));

			var s = k.Add(e.Multiply(secret)).Mod(Secp256k1.N);
			return new SchnorrSignature(Xr, s);
		} 

		public bool Verify(uint256 m, byte[] pubkey, SchnorrSignature sig)
		{
			if( sig.R.CompareTo(PP)>=0 || sig.S.CompareTo(Secp256k1.N)>=0)
				return false;

			var e = new BigInteger(1, Hashes.SHA256( Utilsx.ToBytes( sig.R ).Concat( pubkey, m.ToBytes(false))));

			var eckey = new ECKey(pubkey, false);
			var q = eckey.GetPublicKeyParameters().Q.Normalize();
			var P = Secp256k1.Curve.CreatePoint(q.XCoord.ToBigInteger(), q.YCoord.ToBigInteger());

			var R = Secp256k1.G.Multiply(sig.S).Add(P.Multiply(Secp256k1.N.Subtract(e))).Normalize();

			if(R.XCoord.ToBigInteger().CompareTo(sig.R) != 0 || BigInteger.Jacobi(R.YCoord.ToBigInteger(), PP) != 1 )
				return false;

			return true;
		}
	}
}