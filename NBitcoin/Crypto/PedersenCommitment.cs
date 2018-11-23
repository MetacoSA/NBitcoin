using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Math.EC;

namespace NBitcoin.Crypto
{
	/// <summary>
	/// A data structure for holding and manipulating Pedersen commitments,
	/// as used in Confidential Transactions etc.
	/// </summary>
	public class PedersenCommitment
	{
		private static X9ECParameters Secp256k1 = NBitcoin.BouncyCastle.Crypto.EC.CustomNamedCurves.Secp256k1;

		// Secret blinding factor.
		private readonly BigInteger x;

		// Amount being committed to.
		private readonly BigInteger a;

		// Generator point of the group.
		private static readonly ECPoint G = Secp256k1.G;

		// X coordinate of 'H'.
		private static readonly BigInteger H_x = new BigInteger(1,
			new byte[]
			{
				0x50, 0x92, 0x9b, 0x74, 0xc1, 0xa0, 0x49, 0x54, 0xb7, 0x8b, 0x4b, 0x60, 0x35, 0xe9, 0x7a, 0x5e,
				0x07, 0x8a, 0x5a, 0x0f, 0x28, 0xec, 0x96, 0xd5, 0x47, 0xbf, 0xee, 0x9a, 0xce, 0x80, 0x3a, 0xc0
			});

		// Y coordinate of 'H'.
		private static readonly BigInteger H_y = new BigInteger(1,
			new byte[]
			{
				0x31, 0xd3, 0xc6, 0x86, 0x39, 0x73, 0x92, 0x6e, 0x04, 0x9e, 0x63, 0x7c, 0xb1, 0xb5, 0xf4, 0x0a,
				0x36, 0xda, 0xc2, 0x8a, 0xf1, 0x76, 0x69, 0x68, 0xc3, 0x0c, 0x23, 0x13, 0xf3, 0xa3, 0x89, 0x04
			});

		// H is an additional generator point for the group.
		// This is unrelated to the cofactor 'H' of the secp256k1 curve.
		private static readonly ECPoint H = Secp256k1.Curve.CreatePoint(H_x, H_y);

		// The actual commitment, which is simply a point on the secp256k1 curve.
		private readonly ECPoint Commitment;

		public PedersenCommitment(BigInteger x, BigInteger a)
		{
			this.x = x;
			this.a = a;

			// Pedersen commitment C = xG + aH
			this.Commitment = G.Multiply(x).Add(H.Multiply(a));
		}

		internal PedersenCommitment(ECPoint commitment)
		{
			// Only the commitment is known, this is how the commitment will be seen by most users.
			this.Commitment = commitment;
		}

		public static PedersenCommitment operator +(PedersenCommitment c1, PedersenCommitment c2)
		{
			// Pedersen commitments are additively homomorphic.
			// So C1(x1, a1) + C2(x2, a2) = C3((x1 + x2), (a1 + a2))

			// Preserve secrets in the result if they are known.
			if ((c1.x != null) && (c1.a != null) && (c2.x != null) && (c2.a != null))
				return new PedersenCommitment(c1.x.Add(c2.x), c1.a.Add(c2.a));

			// The secret values were not available, just give back the summed commitment.
			return new PedersenCommitment(c1.Commitment.Add(c2.Commitment));
		}

		public static bool operator ==(PedersenCommitment c1, PedersenCommitment c2)
		{
			return c1.Commitment.Equals(c2.Commitment);
		}

		public static bool operator !=(PedersenCommitment c1, PedersenCommitment c2)
		{
			return !c1.Commitment.Equals(c2.Commitment);
		}
	}
}
