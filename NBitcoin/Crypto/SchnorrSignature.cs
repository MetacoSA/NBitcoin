using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.BouncyCastle.Asn1.X9;

namespace NBitcoin.Crypto
{
	public class SchnorrSigner
	{

        public byte[] Sign(byte[] secret, uint256 m)
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
    }
}