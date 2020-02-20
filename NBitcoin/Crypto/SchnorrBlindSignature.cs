
using System;
using System.Security;
using NBitcoin.BouncyCastle.Asn1.X9;
#if !HAS_SPAN
using NBitcoin.BouncyCastle.Crypto.Signers;
#endif
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Security;

namespace NBitcoin.Crypto
{
#if HAS_SPAN
	public class UnblindedSignature
	{
		internal Secp256k1.Scalar C { get; }
		internal Secp256k1.Scalar S { get; }

		internal UnblindedSignature(in Secp256k1.Scalar c, in Secp256k1.Scalar s)
		{
			C = c;
			S = s;
		}
	}
#else
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
#endif
	public class SchnorrBlinding
	{
#if !HAS_SPAN
		private static X9ECParameters Secp256k1 = ECKey.Secp256k1;
#endif

		public class Requester
		{
#if HAS_SPAN
			Secp256k1.Scalar _v = Secp256k1.Scalar.Zero;
			Secp256k1.Scalar _c = Secp256k1.Scalar.Zero;
			Secp256k1.Scalar _w = Secp256k1.Scalar.Zero;
#else
			private BigInteger _v;
			private BigInteger _w;
			private BigInteger _c;
			private IDsaKCalculator _k;
#endif
			public Requester()
			{
#if !HAS_SPAN
				_k = new RandomDsaKCalculator();
				_k.Init(BigInteger.Arbitrary(256), new SecureRandom());
#endif
			}

			public uint256 BlindMessage(uint256 message, PubKey rpubkey, PubKey signerPubKey)
			{
#if HAS_SPAN
				var ctx = NBitcoinContext.Instance.EcMultGenContext;
				int overflow;
				Span<byte> tmp = stackalloc byte[32];
				var P = signerPubKey.ECKey.Q;
				var R = rpubkey.ECKey.Q.ToGroupElementJacobian();
				var t = Secp256k1.FE.Zero;
			retry:

				RandomUtils.GetBytes(tmp);
				_v = new Secp256k1.Scalar(tmp, out overflow);
				if (overflow != 0 || _v.IsZero)
					goto retry;
				RandomUtils.GetBytes(tmp);
				_w = new Secp256k1.Scalar(tmp, out overflow);
				if (overflow != 0 || _v.IsZero)
					goto retry;
				var A1 = ctx.MultGen(_v);
				var A2 = _w * P;
				var A = R.AddVariable(A1, out _).AddVariable(A2, out _).ToGroupElement();
				t = A.x.Normalize();
				if (t.IsZero)
					goto retry;
				using (var sha = new Secp256k1.SHA256())
				{
					message.ToBytes(tmp);
					sha.Write(tmp);
					t.WriteToSpan(tmp);
					sha.Write(tmp);
					sha.GetHash(tmp);
				}
				_c = new Secp256k1.Scalar(tmp, out overflow);
				if (overflow != 0 || _c.IsZero)
					goto retry;
				var cp = _c.Add(_w.Negate(), out overflow); // this is sent to the signer (blinded message)
				if (cp.IsZero || overflow != 0)
					goto retry;
				cp.WriteToSpan(tmp);
				return new uint256(tmp);
#else
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
				_c = new BigInteger(1, Hashes.SHA256(message.ToBytes().Concat(Utils.BigIntegerToBytes(t, 32))));
				var cp = _c.Subtract(_w).Mod(Secp256k1.N); // this is sent to the signer (blinded message)
				return new uint256(Utils.BigIntegerToBytes(cp, 32));
#endif
			}

#if HAS_SPAN
			public UnblindedSignature UnblindSignature(uint256 blindSignature)
			{
				int overflow;
				Span<byte> tmp = stackalloc byte[32];
				blindSignature.ToBytes(tmp);
				var sp = new Secp256k1.Scalar(tmp, out overflow);
				if (sp.IsZero || overflow != 0)
					throw new ArgumentException("Invalid blindSignature", nameof(blindSignature));
				var s = sp + _v;
				if (s.IsZero || s.IsOverflow)
					throw new ArgumentException("Invalid blindSignature", nameof(blindSignature));
				return new UnblindedSignature(_c, s);
			}
#else
			public UnblindedSignature UnblindSignature(uint256 blindSignature)
			{
				var sp = new BigInteger(1, blindSignature.ToBytes());
				var s = sp.Add(_v).Mod(Secp256k1.N);
				return new UnblindedSignature(_c, s);
			}
#endif

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
#if HAS_SPAN
				Span<byte> tmp = stackalloc byte[32];
				blindedMessage.ToBytes(tmp);
				var cp = new Secp256k1.Scalar(tmp, out int overflow);
				if (cp.IsZero || overflow != 0)
					throw new System.ArgumentException("Invalid blinded message.", nameof(blindedMessage));
				var r = R._ECKey.sec;
				var d = Key._ECKey.sec;
				var sp = r + (cp * d).Negate();
				sp.WriteToSpan(tmp);
				return new uint256(tmp);
#else
				// blind signature s = r - bs * d
				if (blindedMessage == uint256.Zero)
					throw new System.ArgumentException("Invalid blinded message.", nameof(blindedMessage));
				var r = R._ECKey.PrivateKey.D;
				var d = Key._ECKey.PrivateKey.D;
				var cp = new BigInteger(1, blindedMessage.ToBytes());
				var sp = r.Subtract(cp.Multiply(d)).Mod(ECKey.Secp256k1.N);
				return new uint256(Utils.BigIntegerToBytes(sp, 32));
#endif
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
#if HAS_SPAN
			var P = signerPubKey.ECKey.Q;

			var sG = (signature.S * Secp256k1.EC.G).ToGroupElement();
			var cP = P * signature.C;
			var R = cP + sG;
			var t = R.ToGroupElement().x.Normalize();
			using var sha = new Secp256k1.SHA256();
			Span<byte> tmp = stackalloc byte[32];
			message.ToBytes(tmp);
			sha.Write(tmp);
			t.WriteToSpan(tmp);
			sha.Write(tmp);
			sha.GetHash(tmp);
			return new Secp256k1.Scalar(tmp) == signature.C;
#else
			var P = signerPubKey.ECKey.GetPublicKeyParameters().Q;

			var sG = Secp256k1.G.Multiply(signature.S);
			var cP = P.Multiply(signature.C);
			var R = cP.Add(sG).Normalize();
			var t = R.AffineXCoord.ToBigInteger().Mod(Secp256k1.N);
			var c = new BigInteger(1, Hashes.SHA256(message.ToBytes().Concat(Utils.BigIntegerToBytes(t, 32))));
			return c.Equals(signature.C);
#endif
		}
	}
}
