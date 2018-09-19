using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Math.EC.Custom.Sec;
using NBitcoin.DataEncoders;
using NBitcoin.BouncyCastle.Math.EC;

namespace NBitcoin.Crypto
{
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
				throw new ArgumentException("Invalid schnorr signature length.");

			R = new BigInteger(1, bytes, 0, 32);
			S = new BigInteger(1, bytes, 32, 32);
		}

		public SchnorrSignature(BigInteger r, BigInteger s)
		{
			R = r;
			S = s;
		}

		public byte[] ToBytes()
		{
			return Utils.BigIntegerToBytes(R, 32).Concat( Utils.BigIntegerToBytes(S, 32) );
		}
	}

	public class SchnorrSigner
	{
		private static X9ECParameters Secp256k1 =  NBitcoin.BouncyCastle.Crypto.EC.CustomNamedCurves.Secp256k1;
		private static BigInteger PP = ((SecP256K1Curve)Secp256k1.Curve).QQ;

		public SchnorrSignature Sign(uint256 m, Key secret)
		{
			return Sign(m, new BigInteger(1, secret.ToBytes()));
		}

		public SchnorrSignature Sign(uint256 m, BigInteger secret)
		{
			var k = new BigInteger(1, Hashes.SHA256(Utils.BigIntegerToBytes(secret, 32).Concat(m.ToBytes(false))));
			var R = Secp256k1.G.Multiply(k).Normalize();
			var Xr = R.XCoord.ToBigInteger();
			var Yr = R.YCoord.ToBigInteger();
			if( BigInteger.Jacobi(Yr, PP) != 1)
				k = Secp256k1.N.Subtract(k);

			var P = Secp256k1.G.Multiply(secret); 
			var keyPrefixedM = Utils.BigIntegerToBytes(Xr, 32).Concat( P.GetEncoded(true), m.ToBytes(false) ); 
			var e = new BigInteger(1, Hashes.SHA256(keyPrefixedM));

			var s = k.Add(e.Multiply(secret)).Mod(Secp256k1.N);
			return new SchnorrSignature(Xr, s);
		} 

		public bool Verify(uint256 m, PubKey pubkey, SchnorrSignature sig)
		{
			if( sig.R.CompareTo(PP)>=0 || sig.S.CompareTo(Secp256k1.N)>=0)
				return false;

			var e = new BigInteger(1, Hashes.SHA256( Utils.BigIntegerToBytes( sig.R, 32 ).Concat( pubkey.ToBytes(), m.ToBytes(false)))).Mod(Secp256k1.N);
			var q = pubkey.ECKey.GetPublicKeyParameters().Q.Normalize();
			var P = Secp256k1.Curve.CreatePoint(q.XCoord.ToBigInteger(), q.YCoord.ToBigInteger());

			var R = Secp256k1.G.Multiply(sig.S).Add(P.Multiply(Secp256k1.N.Subtract(e))).Normalize();

			if(R.IsInfinity 
				|| R.XCoord.ToBigInteger().CompareTo(sig.R) != 0 
				|| BigInteger.Jacobi(R.YCoord.ToBigInteger(), PP) != 1 )
				return false;

			return true;
		}

		public static bool BatchVerify(uint256[] m, PubKey[] pubkeys, SchnorrSignature[] sigs, BigInteger[] rnds)
		{
			if(m.Length != pubkeys.Length || pubkeys.Length != sigs.Length || sigs.Length != rnds.Length +1)
				throw new ArgumentException("Invalid array lengths");
			if(rnds.Any(r=> r.CompareTo(BigInteger.Zero) <=0 || r.CompareTo(Secp256k1.N)>=0))
				throw new ArgumentException("Random numbers are out of range");

			var s = BigInteger.Zero;
			var r1 = Secp256k1.Curve.Infinity;
			var r2 = Secp256k1.Curve.Infinity;
			for(var i=0; i< sigs.Count(); i++)
			{
				var sig = sigs[i];
				if( sig.R.CompareTo(PP)>=0 || sig.S.CompareTo(Secp256k1.N)>=0)
					return false;

				var e = new BigInteger(1, Hashes.SHA256( Utils.BigIntegerToBytes( sig.R, 32 ).Concat( pubkeys[i].ToBytes(), m[i].ToBytes(false)))).Mod(Secp256k1.N);
				var c = sig.R.Pow(3).Add(BigInteger.ValueOf(7)).Mod(PP);
				var y = c.ModPow(PP.Add(BigInteger.One).Divide(BigInteger.ValueOf(4)), PP);
				if (!y.ModPow(BigInteger.Two, PP).Equals(c))
					return false;
				
				var a = i == 0 ? BigInteger.One : rnds[i-1];
				s = s.Add( sig.S.Multiply(a) ).Mod(Secp256k1.N);

				var R = Secp256k1.Curve.CreatePoint(sig.R, y);
				r1 = r1.Add( R.Multiply(a) );

				var P = pubkeys[i].ECKey.GetPublicKeyParameters().Q.Normalize();
				r2 = r2.Add( P.Multiply(e.Multiply(a)) );
			}
			return Secp256k1.G.Multiply(s).Equals(r1.Add(r2));
		}
	}
}