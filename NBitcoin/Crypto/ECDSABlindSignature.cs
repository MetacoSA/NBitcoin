
using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Crypto.Signers;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;

namespace NBitcoin.Crypto
{
	public class BlindSignature
	{
		public BigInteger C { get; }
		public BigInteger S { get; }

		public BlindSignature(BigInteger c, BigInteger s)
		{
			C = c;
			S = s;
		}
	}

    public class ECdsaBlinding
    {
        private static X9ECParameters Secp256k1 = ECKey.Secp256k1;

        public class Requester
        {
            private BigInteger _v;
            private BigInteger _w;
            private BigInteger _c;
            private IDsaKCalculator _k;

            public Requester()
            {
                _k = new RandomDsaKCalculator();
                _k.Init(BigInteger.Arbitrary(256), new SecureRandom());
            }

            public uint256 BlindMessage(uint256 message, PubKey signerPubKey, PubKey verificationPubKey)
            {
                var P = new ECKey(verificationPubKey.ToBytes(), false).GetPublicKeyParameters().Q;
                var R = new ECKey(signerPubKey.ToBytes(), false).GetPublicKeyParameters().Q;

                var t = BigInteger.Zero;
                while(t.SignValue == 0)
                {
                    _v = _k.NextK();
                    _w = _k.NextK();

                    var A1 = Secp256k1.G.Multiply(_v); 
                    var A2 = P.Multiply(_w); 
                    var A  = R.Add( A1.Add(A2) ).Normalize();
                    t = A.AffineXCoord.ToBigInteger().Mod(Secp256k1.N);
                }
                _c = new BigInteger(1, Hashes.SHA256(message.ToBytes(false).Concat(Utils.BigIntegerToBytes(t, 32))));
                var cp= _c.Subtract(_w).Mod(Secp256k1.N); // this is sent to the signer (blinded message)
                return new uint256(Utils.BigIntegerToBytes(cp, 32)); 
            }

            public BlindSignature UnblindSignature(BigInteger sp)
            {
                var s = sp.Add(_v).Mod(Secp256k1.N);
                return new BlindSignature(_c, s);
            }
        }

        public class Signer
        {
            public Key SignerKey { get; }
            public Key VerificationKey { get; }

            public Signer(Key signerKey)
                : this(signerKey, new Key())
            {}

            public Signer(Key signerKey, Key verificationKey)
            {
                SignerKey = signerKey;
                VerificationKey = verificationKey;
            }

            public BigInteger Sign(uint256 message)
            {
                var r = new BigInteger(1, SignerKey.ToBytes());
                var d = new BigInteger(1, VerificationKey.ToBytes());
                var cp = new BigInteger(1, message.ToBytes());
                var sp = r.Subtract(cp.Multiply(d)).Mod(ECKey.Secp256k1.N);
                return sp;
            }
        }

        internal static bool VerifySignature(uint256 message, BlindSignature signature, PubKey verificationPubKey)
        {
            var P = new ECKey(verificationPubKey.ToBytes(), false).GetPublicKeyParameters().Q;

            var sG = Secp256k1.G.Multiply(signature.S);
            var cP = P.Multiply(signature.C);
            var R  = cP.Add(sG).Normalize();
            var t = R.AffineXCoord.ToBigInteger().Mod(Secp256k1.N);
            var c = new BigInteger(1, Hashes.SHA256(message.ToBytes(false).Concat(Utils.BigIntegerToBytes(t, 32))));
            return c.Equals(signature.C);
        }
	}
}