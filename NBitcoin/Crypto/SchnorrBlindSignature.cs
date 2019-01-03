
using System.Security;
using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Crypto.Signers;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;

namespace NBitcoin.Crypto
{
    public class UnblindedSignature
    {
        public BigInteger C { get; }
        public BigInteger S { get; }

        public UnblindedSignature(BigInteger c, BigInteger s)
        {
            C = c;
            S = s;
        }
    }

    public class SchnorrBlinding
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

            public uint256 BlindMessage(uint256 message, PubKey rpubkey, PubKey signerPubKey)
            {
                var P = signerPubKey.ECKey.GetPublicKeyParameters().Q;
                var R = rpubkey.ECKey.GetPublicKeyParameters().Q;

                var t = BigInteger.Zero;
                while (t.SignValue == 0)
                {
                    _v = _k.NextK();
                    _w = _k.NextK();

                    var A1 = Secp256k1.G.Multiply(_v);
                    var A2 = P.Multiply(_w);
                    var A = R.Add(A1.Add(A2)).Normalize();
                    t = A.AffineXCoord.ToBigInteger().Mod(Secp256k1.N);
                }
                _c = new BigInteger(1, Hashes.SHA256(message.ToBytes(false).Concat(Utils.BigIntegerToBytes(t, 32))));
                var cp = _c.Subtract(_w).Mod(Secp256k1.N); // this is sent to the signer (blinded message)
                return new uint256(Utils.BigIntegerToBytes(cp, 32));
            }

            public UnblindedSignature UnblindSignature(uint256 blindSignature)
            {
                var sp = new BigInteger(1, blindSignature.ToBytes());
                var s = sp.Add(_v).Mod(Secp256k1.N);
                return new UnblindedSignature(_c, s);
            }

            public uint256 BlindMessage(byte[] message, PubKey rpubKey, PubKey signerPubKey)
            {
                var msg = new uint256(Hashes.SHA256(message));
                return BlindMessage(msg, rpubKey, signerPubKey);
            }
        }

        public class Signer
        {
            // The random generated r value. It is used to derivate an R point where
            // R = r*G that has to be sent to the requester in order to allow him to
            // blind the message to be signed.
            public Key R { get; }

            // The signer key used for signing
            public Key Key { get; }

            public Signer(Key key)
                : this(key, new Key())
            { }

            public Signer(Key key, Key r)
            {
                R = r;
                Key = key;
            }

            public uint256 Sign(uint256 blindedMessage)
            {
				// blind signature s = r - bs * d
				if(blindedMessage == uint256.Zero)
					throw new System.ArgumentException("Invalid blinded message.", nameof(blindedMessage));
                var r = R._ECKey.PrivateKey.D;
                var d = Key._ECKey.PrivateKey.D;
                var cp = new BigInteger(1, blindedMessage.ToBytes());
                var sp = r.Subtract(cp.Multiply(d)).Mod(ECKey.Secp256k1.N);
                return new uint256(Utils.BigIntegerToBytes(sp, 32));
            }

            public bool VerifyUnblindedSignature(UnblindedSignature signature, uint256 dataHash)
            {
                return SchnorrBlinding.VerifySignature(dataHash, signature, Key.PubKey);
            }

            public bool VerifyUnblindedSignature(UnblindedSignature signature, byte[] data)
            {
                var hash = new uint256(Hashes.SHA256(data));
                return SchnorrBlinding.VerifySignature(hash, signature, Key.PubKey);
            }
        }

        public static bool VerifySignature(uint256 message, UnblindedSignature signature, PubKey signerPubKey)
        {
            var P = signerPubKey.ECKey.GetPublicKeyParameters().Q;

            var sG = Secp256k1.G.Multiply(signature.S);
            var cP = P.Multiply(signature.C);
            var R = cP.Add(sG).Normalize();
            var t = R.AffineXCoord.ToBigInteger().Mod(Secp256k1.N);
            var c = new BigInteger(1, Hashes.SHA256(message.ToBytes(false).Concat(Utils.BigIntegerToBytes(t, 32))));
            return c.Equals(signature.C);
        }
    }
}
