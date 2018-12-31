using System;
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
		// It can be calculated as follow:
		// H = Secp256k1.Curve.DecodePoint(new[] { (byte)0x02 }.Concat(Hashes.SHA256(G.GetEncoded(false))));
		// This is unrelated to the cofactor 'H' of the secp256k1 curve.
		private static readonly ECPoint H = Secp256k1.Curve.CreatePoint(H_x, H_y);

		private bool preserveSecrets;
		// The actual commitment, which is simply a point on the secp256k1 curve.
		private readonly ECPoint commitment;

		public PedersenCommitment(BigInteger blindingFactor, BigInteger value)
		{
			if (blindingFactor == null) 
				throw new ArgumentNullException(nameof(blindingFactor));
			if (value == null) 
				throw new ArgumentNullException(nameof(value));

			this.x = blindingFactor;
			this.a = value;
			this.preserveSecrets = true;

			// Pedersen commitment C = xG + aH
			this.commitment = G.Multiply(x).Add(H.Multiply(a));
		}

		internal PedersenCommitment(ECPoint commitment)
		{
			if (commitment == null) 
				throw new ArgumentNullException(nameof(commitment));

			// Only the commitment is known, this is how the commitment will be seen by most users.
			this.preserveSecrets = false;
			this.commitment = commitment;
		}

		public static PedersenCommitment operator +(PedersenCommitment c1, PedersenCommitment c2)
		{
			if (c1 == null) 
				throw new ArgumentNullException(nameof(c1));
			if (c2 == null) 
				throw new ArgumentNullException(nameof(c2));
			// Pedersen commitments are additively homomorphic.
			// So Commit(x1, a1) + Commit(x2, a2) = Commit((x1 + x2), (a1 + a2))

			// Preserve secrets in the result if they are known.
			if (c1.preserveSecrets && c2.preserveSecrets)
				return new PedersenCommitment(c1.x.Add(c2.x), c1.a.Add(c2.a));

			// The secret values were not available, just give back the summed commitment.
			return new PedersenCommitment(c1.commitment.Add(c2.commitment));
		}

		public static PedersenCommitment operator +(BigInteger n, PedersenCommitment c1)
		{
			if (n == null) 
				throw new ArgumentNullException(nameof(n));
			if (c1 == null) 
				throw new ArgumentNullException(nameof(c1));
			if (!c1.preserveSecrets)
				throw new InvalidOperationException("Pedersen commitment's secret is not available");

			// Pedersen commitments are additively homomorphic.
			// Commit(x,r) + n = Commit(x + n,r)

			// Preserve secrets in the result if they are known.
				return new PedersenCommitment(c1.x.Add(n), c1.a);
		}

		public static PedersenCommitment operator -(PedersenCommitment c1, PedersenCommitment c2)
		{
			if (c1 == null) 
				throw new ArgumentNullException(nameof(c1));
			if (c2 == null) 
				throw new ArgumentNullException(nameof(c2));

			// Preserve secrets in the result if they are known.
			if (c1.preserveSecrets && c2.preserveSecrets)
				return new PedersenCommitment(c1.x.Subtract(c2.x), c1.a.Subtract(c2.a));

			// The secret values were not available, just give back the summed commitment.
			return new PedersenCommitment(c1.commitment.Add(c2.commitment.Negate()));
		}

		public bool Verify(BigInteger blindingFactor, BigInteger value)
		{
			var commitment = new PedersenCommitment(blindingFactor, value);
			return this == commitment;
		}

		public static bool operator ==(PedersenCommitment c1, PedersenCommitment c2)
		{
			if(System.Object.ReferenceEquals(c1, c2))
				return true;
			if(((object)c1 == null) || ((object)c2 == null))
				return false;
			return c1.commitment.Equals(c2.commitment);
		}

		public static bool operator !=(PedersenCommitment c1, PedersenCommitment c2)
		{
			return !(c1.commitment == c2.commitment);
		}
	}
}
