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

namespace NBitcoin.Crypto
{
	public class SchnorrSigner
	{

        public byte[] Sign(uint256 m, byte[] secret)
        {
            var qq = ((NBitcoin.BouncyCastle.Math.EC.Custom.Sec.SecP256K1Curve)ECKey.Secp256k1.Curve).QQ;
            var key = new uint256(secret);
            var keyPrefixedMessage =  key.ToBytes(true).Concat(m.ToBytes(false)); 
            var k = new BigInteger(1, Hashes.SHA256(keyPrefixedMessage));
            var R = ECKey.Secp256k1.G.Multiply(k);
            var Xr = R.X.ToBigInteger();
            var Yr = R.Y.ToBigInteger();
            if( BigInteger.Jacobi(Yr, qq) != 1)
                k = ECKey.Secp256k1.N.Subtract(k);

            var P = ECKey.Secp256k1.G.Multiply(new BigInteger(1, secret)); 
            var r = new uint256 (Xr.SignValue > 0 ? Xr.ToByteArray() : Xr.ToByteArrayUnsigned()).ToBytes(true);
            var encP = P.GetEncoded(true);
            var keyPrefixedM = ByteArrayExtensions.Concat(r, encP, m.ToBytes(false));
            var e = new BigInteger(1, Hashes.SHA256(keyPrefixedM));
            var ss= new BigInteger(1, secret);

            var t = k.Add(e.Multiply(ss)).Mod(ECKey.Secp256k1.N);
            var sig = ByteArrayExtensions.Concat(r,  new uint256(t.SignValue>0 ? t.ToByteArray() : t.ToByteArrayUnsigned()).ToBytes(true));
            return sig;
        } 

        /*
        def schnorr_verify(msg, pubkey, sig):
            if (not on_curve(pubkey)):
                return False
            r = int.from_bytes(sig[0:32], byteorder="big")
            s = int.from_bytes(sig[32:64], byteorder="big")
            if r >= p or s >= n:
                return False
            e = sha256(sig[0:32] + bytes_point(pubkey) + msg)
            R = point_add(point_mul(G, s), point_mul(pubkey, n - e))
            if R is None or jacobi(R[1]) != 1 or R[0] != r:
                return False
            return True
        */

        public bool Verify(uint256 m, byte[] pubkey, byte[] sig)
        {
            var r = sig.SafeSubarray(0, 32);
            var s = sig.SafeSubarray(32, 32);
            var ri = new BigInteger(1, r);
            var si = new BigInteger(1, s);

            var secP256K1_P = ((NBitcoin.BouncyCastle.Math.EC.Custom.Sec.SecP256K1Curve)ECKey.Secp256k1.Curve).QQ;

            if( ri.CompareTo(secP256K1_P)>=0 || si.CompareTo(ECKey.Secp256k1.N)>=0)
            {
                return false;
            }
            var e = new BigInteger(1, Hashes.SHA256( ByteArrayExtensions.Concat(r, pubkey, m.ToBytes(false))));

            var eckey = new ECKey(pubkey, false);
			var q = eckey.GetPublicKeyParameters().Q;
			q = q.Normalize();
			var P = ECKey.Secp256k1.Curve.CreatePoint(q.XCoord.ToBigInteger(), q.YCoord.ToBigInteger());

            var Rt1 = ECKey.Secp256k1.G.Multiply(si);
            var Rt2 = P.Multiply(ECKey.Secp256k1.N.Subtract(e));
            var R = Rt1.Add(Rt2);

            if(R.X.ToBigInteger().CompareTo(ri) != 0 || BigInteger.Jacobi(R.Y.ToBigInteger(), secP256K1_P) != 1 )
                return false;

            return true;
        }
    }
}