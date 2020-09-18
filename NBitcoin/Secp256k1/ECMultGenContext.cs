#if HAS_SPAN
#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class ECMultGenContext
	{
		static readonly Lazy<ECMultGenContext> _Instance = new Lazy<ECMultGenContext>(CreateInstance, true);
		public static ECMultGenContext Instance => _Instance.Value;
		static ECMultGenContext CreateInstance()
		{
			return new ECMultGenContext();
		}

		/* For accelerating the computation of a*G:
 * To harden against timing attacks, use the following mechanism:
 * * Break up the multiplicand into groups of 4 bits, called n_0, n_1, n_2, ..., n_63.
 * * Compute sum(n_i * 16^i * G + U_i, i=0..63), where:
 *   * U_i = U * 2^i (for i=0..62)
 *   * U_i = U * (1-2^63) (for i=63)
 *   where U is a point with no known corresponding scalar. Note that sum(U_i, i=0..63) = 0.
 * For each i, and each of the 16 possible values of n_i, (n_i * 16^i * G + U_i) is
 * precomputed (call it prec(i, n_i)). The formula now becomes sum(prec(i, n_i), i=0..63).
 * None of the resulting prec group elements have a known scalar, and neither do any of
 * the intermediate sums while computing a*G.
 */
		internal GEStorage[,] prec; /* prec[j][i] = 16^j * i * G + U_i */
		internal Scalar blind;
		internal GEJ initial;

		public ECMultGenContext()
		{
			Span<GE> prec = stackalloc GE[1024];
			GEJ gj;
			GEJ nums_gej;
			int i, j;

			this.prec = new GEStorage[64, 16];
			gj = EC.G.ToGroupElementJacobian();
			/* Construct a group element with no known corresponding scalar (nothing up my sleeve). */
			{
				var nums_b32 = "The scalar for this x is unknown".ToCharArray().Select(b => (byte)b).ToArray();
				FE nums_x;
				GE nums_ge;
				var r = FE.TryCreate(nums_b32, out nums_x);
				VERIFY_CHECK(r);
				r = GE.TryCreateXOVariable(nums_x, false, out nums_ge);
				VERIFY_CHECK(r);
				nums_gej = nums_ge.ToGroupElementJacobian();
				/* Add G to make the bits in x uniformly distributed. */
				nums_gej = nums_gej.AddVariable(EC.G, out _);
			}
			/* compute prec. */
			{
				Span<GEJ> precj = stackalloc GEJ[1024]; /* Jacobian versions of prec. */
				GEJ gbase;
				GEJ numsbase;
				gbase = gj; /* 16^j * G */
				numsbase = nums_gej; /* 2^j * nums. */
				for (j = 0; j < 64; j++)
				{
					/* Set precj[j*16 .. j*16+15] to (numsbase, numsbase + gbase, ..., numsbase + 15*gbase). */
					precj[j * 16] = numsbase;
					for (i = 1; i < 16; i++)
					{
						precj[j * 16 + i] = precj[j * 16 + i - 1].AddVariable(gbase, out _);
					}
					/* Multiply gbase by 16. */
					for (i = 0; i < 4; i++)
					{
						gbase = gbase.DoubleVariable();
					}
					/* Multiply numbase by 2. */
					numsbase = numsbase.DoubleVariable();
					if (j == 62)
					{
						/* In the last iteration, numsbase is (1 - 2^j) * nums instead. */
						numsbase = numsbase.Negate();
						numsbase = numsbase.AddVariable(nums_gej, out _);
					}
				}
				GE.SetAllGroupElementJacobianVariable(prec, precj, 1024);
			}
			for (j = 0; j < 64; j++)
			{
				for (i = 0; i < 16; i++)
				{
					this.prec[j, i] = prec[j * 16 + i].ToStorage();
				}
			}
			Blind();
		}

		public void Blind(byte[]? seed32 = null)
		{
			Scalar b;
			GEJ gb;
			FE s;
			Span<byte> nonce32 = stackalloc byte[32];
			using RFC6979HMACSHA256 rng = new RFC6979HMACSHA256();
			bool retry;
			Span<byte> keydata = stackalloc byte[64];
			if (seed32 == null)
			{
				/* When seed is NULL, reset the initial point and blinding value. */
				initial = EC.G.ToGroupElementJacobian();
				initial = initial.Negate();
				blind = new Scalar(1);
			}
			/* The prior blinding value (if not reset) is chained forward by including it in the hash. */
			blind.WriteToSpan(nonce32);
			/* Using a CSPRNG allows a failure free interface, avoids needing large amounts of random data,
			 *   and guards against weak or adversarial seeds.  This is a simpler and safer interface than
			 *   asking the caller for blinding values directly and expecting them to retry on failure.
			 */
			nonce32.CopyTo(keydata);
			if (seed32 != null)
			{
				seed32.AsSpan().CopyTo(keydata.Slice(32, 32));
			}
			else
			{
				keydata = keydata.Slice(0, 32);
			}
			rng.Initialize(keydata);
			keydata.Clear();
			/* Retry for out of range results to achieve uniformity. */
			do
			{
				rng.Generate(nonce32);
				retry = !FE.TryCreate(nonce32, out s);
				retry |= s.IsZero;
			} while (retry); /* This branch true is cryptographically unreachable. Requires sha256_hmac output > Fp. */
			/* Randomize the projection to defend against multiplier sidechannels. */
			initial = initial.Rescale(s);
			FE.Clear(ref s);
			do
			{
				rng.Generate(nonce32);
				b = new Scalar(nonce32, out int overflow);
				retry = overflow == 1;
				/* A blinding value of 0 works, but would undermine the projection hardening. */
				retry |= b.IsZero;
			} while (retry); /* This branch true is cryptographically unreachable. Requires sha256_hmac output > order. */
			rng.Dispose();
			nonce32.Fill(0);

			gb = MultGen(b);
			b = b.Negate();
			blind = b;
			initial = gb;
			Scalar.Clear(ref b);
			GEJ.Clear(ref gb);
		}


		/// <summary>
		/// Multiply with the generator: R = a*G
		/// (secp256k1_ecmult_gen)
		/// </summary>
		/// <param name="a">A scalar to multiply to G</param>
		/// <returns>The result of a*G</returns>
		public GEJ MultGen(in Scalar a)
		{
			GEJ r;
			GE add = GE.Zero;
			GEStorage adds = default;
			Scalar gnb;
			uint bits;
			int i, j;
			r = initial;
			/* Blind scalar/point multiplication by computing (n-b)G + bG instead of nG. */
			gnb = a + blind;
			for (j = 0; j < 64; j++)
			{
				bits = gnb.GetBits(j * 4, 4);
				for (i = 0; i < 16; i++)
				{
					/* This uses a conditional move to avoid any secret data in array indexes.
					 *   _Any_ use of secret indexes has been demonstrated to result in timing
					 *   sidechannels, even when the cache-line access patterns are uniform.
					 *  See also:
					 *   "A word of warning", CHES 2013 Rump Session, by Daniel J. Bernstein and Peter Schwabe
					 *    (https://cryptojedi.org/peter/data/chesrump-20130822.pdf) and
					 *   "Cache Attacks and Countermeasures: the Case of AES", RSA 2006,
					 *    by Dag Arne Osvik, Adi Shamir, and Eran Tromer
					 *    (http://www.tau.ac.il/~tromer/papers/cache.pdf)
					 */
					GEStorage.CMov(ref adds, prec[j, i], i == bits ? 1 : 0);
				}
				add = adds.ToGroupElement();
				r += add;
			}

			GE.Clear(ref add);
			Scalar.Clear(ref gnb);
			return r;
		}

		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool v)
		{
			if (!v)
				throw new InvalidOperationException("Bug in secp256k1 for C#");
		}
	}
}
#nullable restore
#endif
