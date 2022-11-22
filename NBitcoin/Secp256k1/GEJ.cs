#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#else
	internal
#endif
	readonly struct GEJ
	{
#if SECP256K1_LIB
		public
#else
		internal
# endif
		readonly FE x, y, z;
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly bool infinity; /* whether this represents the point at infinity */
		static readonly GEJ _Infinity = new GEJ(FE.Zero, FE.Zero, FE.Zero, true);
		public static ref readonly GEJ Infinity => ref _Infinity;

		public static GEJ CONST(uint a, uint b, uint c, uint d, uint e, uint f, uint g, uint h, uint i, uint j, uint k, uint l, uint m, uint n, uint o, uint p)
		{
			return new GEJ(
				FE.CONST(a, b, c, d, e, f, g, h),
				FE.CONST(i, j, k, l, m, n, o, p),
				FE.CONST(0, 0, 0, 0, 0, 0, 0, 1),
				false
				);
		}

		public GEJ(in FE x, in FE y, in FE z, bool infinity)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.infinity = infinity;
		}
		public GEJ(in FE x, in FE y, in FE z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.infinity = false;
		}

		public readonly bool IsInfinity
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return infinity;
			}
		}

		public readonly bool HasQuadYVariable
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (infinity)
				{
					return false;
				}
				/* We rely on the fact that the Jacobi symbol of 1 / a->z^3 is the same as
				 * that of a->z. Thus a->y / a->z^3 is a quadratic residue iff a->y * a->z
				   is */
				return (y * z).IsQuadVariable;
			}
		}
		public readonly GEJ AddVariable(in GE b)
		{
			return AddVariable(b, out _);
		}
		public readonly GEJ AddVariable(in GE b, out FE rzr)
		{
			ref readonly GEJ a = ref this;
			/* 8 mul, 3 sqr, 4 normalize, 12 mul_int/add/negate */
			FE z12, u1, u2, s1, s2, h, i, i2, h2, h3, t;
			if (a.infinity)
			{
				rzr = default;
				return b.ToGroupElementJacobian();
			}
			if (b.infinity)
			{
				rzr = new FE(1U);
				return a;
			}
			var (rx, ry, rz, rinfinity) = default(GEJ);
			rinfinity = false;

			z12 = a.z.Sqr();
			u1 = a.x;
			u1 = u1.NormalizeWeak();
			u2 = b.x * z12;
			s1 = a.y;
			s1 = s1.NormalizeWeak();
			s2 = b.y * z12;
			s2 = s2 * a.z;
			h = u1.Negate(1);
			h += u2;
			i = s1.Negate(1);
			i += s2;
			if (h.NormalizesToZeroVariable())
			{
				if (i.NormalizesToZeroVariable())
				{
					return a.DoubleVariable(out rzr);
				}
				else
				{
					rzr = new FE(0);
					return GEJ.Infinity;
				}
			}
			i2 = i.Sqr();
			h2 = h.Sqr();
			h3 = h * h2;
			rzr = h;
			rz = a.z * h;
			t = u1 * h2;
			rx = t;
			rx *= 2U;
			rx += h3;
			rx = rx.Negate(3);
			rx += i2;
			ry = rx.Negate(5);
			ry += t;
			ry = ry * i;
			h3 = h3 * s1;
			h3 = h3.Negate(1);
			ry += h3;
			return new GEJ(rx, ry, rz, rinfinity);
		}

		public readonly bool IsValidVariable
		{
			get
			{
				FE y2, x3, z2, z6;
				if (infinity)
				{
					return false;
				}
				/* y^2 = x^3 + 7
				 *  (Y/Z^3)^2 = (X/Z^2)^3 + 7
				 *  Y^2 / Z^6 = X^3 / Z^6 + 7
				 *  Y^2 = X^3 + 7*Z^6
				 */
				y2 = y.Sqr();
				x3 = x.Sqr();
				x3 = x3 * x;
				z2 = z.Sqr();
				z6 = z2.Sqr();
				z6 = z6 * z2;
				z6 *= EC.CURVE_B;
				x3 += z6;
				x3 = x3.NormalizeWeak();
				return y2.EqualsVariable(x3);
			}
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GE ToGroupElementZInv(in FE zi)
		{
			ref readonly GEJ a = ref this;
			FE zi2 = zi.Sqr();
			FE zi3 = zi2 * zi;
			FE rx = a.x * zi2;
			FE ry = a.y * zi3;
			return new GE(rx, ry, a.infinity);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GEJ Negate()
		{
			return new GEJ(x, y.NormalizeWeak().Negate(1), z, infinity);
		}
		public readonly GEJ AddVariable(in GEJ b)
		{
			return AddVariable(b, out _);
		}
		public readonly GEJ AddVariable(in GEJ b, out FE rzr)
		{
			ref readonly GEJ a = ref this;
			/* Operations: 12 mul, 4 sqr, 2 normalize, 12 mul_int/add/negate */
			FE z22, z12, u1, u2, s1, s2, h, i, i2, h2, h3, t;
			if (a.infinity)
			{
				rzr = default;
				return b;
			}

			if (b.infinity)
			{
				rzr = new FE(1);
				return a;
			}

			FE rx, ry, rz;
			bool rinfinity;
			rinfinity = false;
			z22 = b.z.Sqr();
			z12 = a.z.Sqr();
			u1 = a.x * z22;
			u2 = b.x * z12;
			s1 = a.y * z22;
			s1 = s1 * b.z;
			s2 = b.y * z12;
			s2 = s2 * a.z;
			h = u1.Negate(1);
			h += u2;
			i = s1.Negate(1);
			i += s2;
			if (h.NormalizesToZeroVariable())
			{
				if (i.NormalizesToZeroVariable())
				{
					return a.DoubleVariable(out rzr);
				}
				else
				{
					rzr = new FE(0);
					return GEJ.Infinity;
				}
			}
			i2 = i.Sqr();
			h2 = h.Sqr();
			h3 = h * h2;
			h = h * b.z;
			rzr = h;
			rz = a.z * h;
			t = u1 * h2;
			rx = t;
			rx *= 2U;
			rx += h3;
			rx = rx.Negate(3);
			rx += i2;
			ry = rx.Negate(5);
			ry += t;
			ry = ry * i;
			h3 = h3 * s1;
			h3 = h3.Negate(1);
			ry += h3;
			return new GEJ(rx, ry, rz, rinfinity);
		}

		public readonly GEJ AddZInvVariable(in GE b, in FE bzinv)
		{
			ref readonly GEJ a = ref this;
			/* 9 mul, 3 sqr, 4 normalize, 12 mul_int/add/negate */
			FE az, z12, u1, u2, s1, s2, h, i, i2, h2, h3, t;
			FE rx, ry, rz;
			bool rinfinity;
			if (b.infinity)
			{
				return a;
			}
			if (a.infinity)
			{
				FE bzinv2, bzinv3;
				rinfinity = b.infinity;
				bzinv2 = bzinv.Sqr();
				bzinv3 = bzinv2 * bzinv;
				rx = b.x * bzinv2;
				ry = b.y * bzinv3;
				rz = new FE(1);
				return new GEJ(rx, ry, rz, rinfinity);
			}
			rinfinity = false;

			/* We need to calculate (rx,ry,rz) = (ax,ay,az) + (bx,by,1/bzinv). Due to
			 *  secp256k1's isomorphism we can multiply the Z coordinates on both sides
			 *  by bzinv, and get: (rx,ry,rz*bzinv) = (ax,ay,az*bzinv) + (bx,by,1).
			 *  This means that (rx,ry,rz) can be calculated as
			 *  (ax,ay,az*bzinv) + (bx,by,1), when not applying the bzinv factor to rz.
			 *  The variable az below holds the modified Z coordinate for a, which is used
			 *  for the computation of rx and ry, but not for rz.
			 */
			az = a.z * bzinv;

			z12 = az.Sqr();
			u1 = a.x;
			u1 = u1.NormalizeWeak();
			u2 = b.x * z12;
			s1 = a.y;
			s1 = s1.NormalizeWeak();
			s2 = b.y * z12;
			s2 = s2 * az;
			h = u1.Negate(1);
			h += u2;
			i = s1.Negate(1);
			i += s2;
			if (h.NormalizesToZeroVariable())
			{
				if (i.NormalizesToZeroVariable())
				{
					return a.DoubleVariable();
				}
				else
				{
					return GEJ.Infinity;
				}
			}
			i2 = i.Sqr();
			h2 = h.Sqr();
			h3 = h * h2;
			rz = a.z;
			rz = rz * h;
			t = u1 * h2;
			rx = t;
			rx *= 2U;
			rx += h3;
			rx = rx.Negate(3);
			rx += i2;
			ry = rx.Negate(5);
			ry += t;
			ry = ry * i;
			h3 = h3 * s1;
			h3 = h3.Negate(1);
			ry += h3;
			return new GEJ(rx, ry, rz, rinfinity);
		}

		public static void Clear(ref GEJ groupElementJacobian)
		{
			groupElementJacobian = new GEJ();
		}

		public readonly GEJ DoubleVariable()
		{
			if (infinity)
			{
				return GEJ.Infinity;
			}
			return this.Double();
		}
		public readonly GEJ DoubleVariable(out FE rzr)
		{
			ref readonly GEJ a = ref this;

			/* For secp256k1, 2Q is infinity if and only if Q is infinity. This is because if 2Q = infinity,
   *  Q must equal -Q, or that Q.y == -(Q.y), or Q.y is 0. For a point on y^2 = x^3 + 7 to have
   *  y=0, x^3 must be -7 mod p. However, -7 has no cube root mod p.
   *
   *  Having said this, if this function receives a point on a sextic twist, e.g. by
   *  a fault attack, it is possible for y to be 0. This happens for y^2 = x^3 + 6,
   *  since -6 does have a cube root mod p. For this point, this function will not set
   *  the infinity flag even though the point doubles to infinity, and the result
   *  point will be gibberish (z = 0 but infinity = 0).
   */
			if (a.infinity)
			{
				rzr = new FE(1);
				return GEJ.Infinity;
			}


			rzr = a.y;
			rzr = rzr.NormalizeWeak();
			rzr = rzr.Multiply(2U);

			return a.Double();
		}
		[Conditional("SECP256K1_VERIFY")]
		private static void VERIFY_CHECK(bool value)
		{
			if (!value)
				throw new InvalidOperationException("VERIFY_CHECK failed (bug in C# secp256k1)");
		}
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GEJ Double()
		{
			ref readonly GEJ a = ref this;
			/* Operations: 3 mul, 4 sqr, 0 normalize, 12 mul_int/add/negate.
     *
     * Note that there is an implementation described at
     *     https://hyperelliptic.org/EFD/g1p/auto-shortw-jacobian-0.html#doubling-dbl-2009-l
     * which trades a multiply for a square, but in practice this is actually slower,
     * mainly because it requires more normalizations.
     */
			FE rx, ry, rz;
			bool rinfinity;
			FE t1, t2, t3, t4;

			rinfinity = a.infinity;

			rz = a.z * a.y;
			rz *= 2U;      /* Z' = 2*Y*Z (2) */
			t1 = a.x.Sqr();
			t1 *= 3U;         /* T1 = 3*X^2 (3) */
			t2 = t1.Sqr();           /* T2 = 9*X^4 (1) */
			t3 = a.y.Sqr();
			t3 *= 2U;         /* T3 = 2*Y^2 (2) */
			t4 = t3.Sqr();
			t4 *= 2U;         /* T4 = 8*Y^4 (2) */
			t3 = t3 * a.x;    /* T3 = 2*X*Y^2 (1) */
			rx = t3;
			rx *= 4U;       /* X' = 8*X*Y^2 (4) */
			rx = rx.Negate(4); /* X' = -8*X*Y^2 (5) */
			rx += t2;         /* X' = 9*X^4 - 8*X*Y^2 (6) */
			t2 = t2.Negate(1);     /* T2 = -9*X^4 (2) */
			t3 *= 6U;         /* T3 = 12*X*Y^2 (6) */
			t3 += t2;           /* T3 = 12*X*Y^2 - 9*X^4 (8) */
			ry = t1 * t3;    /* Y' = 36*X^3*Y^2 - 27*X^6 (1) */
			t2 = t4.Negate(2);     /* T2 = -8*Y^4 (3) */
			ry += t2;         /* Y' = 36*X^3*Y^2 - 27*X^6 - 8*Y^4 (4) */
			return new GEJ(rx, ry, rz, rinfinity);
		}

		public readonly GE ToGroupElementVariable()
		{
			FE x, y;
			bool infinity;
			var (ax, ay, az, ainfinity) = this;
			FE z2, z3;
			infinity = ainfinity;
			if (ainfinity)
			{
				return GE.Infinity;
			}
			az = az.InverseVariable();
			z2 = az.Sqr();
			z3 = az * z2;
			ax = ax * z2;
			ay = ay * z3;
			az = new FE(1);
			x = ax;
			y = ay;
			return new GE(x, y, infinity);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GE ToGroupElement()
		{
			ref readonly GEJ a = ref this;
			FE z2, z3;
			FE rx, ry;
			bool rinfinity;
			var (ax, ay, az, ainfinity) = this;
			rinfinity = a.infinity;
			az = az.Inverse();
			z2 = az.Sqr();
			z3 = az * z2;
			ax = ax * z2;
			ay = ay * z3;
			az = new FE(1);
			rx = ax;
			ry = ay;
			return new GE(rx, ry, rinfinity);
		}

		static readonly FE fe_1 = FE.CONST(0, 0, 0, 0, 0, 0, 0, 1);
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GEJ Add(in GE b)
		{
			ref readonly GEJ a = ref this;
			FE rx, ry, rz;
			bool rinfinity;
			/* Operations: 7 mul, 5 sqr, 4 normalize, 21 mul_int/add/negate/cmov */
			FE zz, u1, u2, s1, s2, t, tt, m, n, q, rr;
			FE m_alt, rr_alt;
			int infinity, degenerate;
			VERIFY_CHECK(!b.infinity);

			/* In:
			 *    Eric Brier and Marc Joye, Weierstrass Elliptic Curves and Side-Channel Attacks.
			 *    In D. Naccache and P. Paillier, Eds., Public Key Cryptography, vol. 2274 of Lecture Notes in Computer Science, pages 335-345. Springer-Verlag, 2002.
			 *  we find as solution for a unified addition/doubling formula:
			 *    lambda = ((x1 + x2)^2 - x1 * x2 + a) / (y1 + y2), with a = 0 for secp256k1's curve equation.
			 *    x3 = lambda^2 - (x1 + x2)
			 *    2*y3 = lambda * (x1 + x2 - 2 * x3) - (y1 + y2).
			 *
			 *  Substituting x_i = Xi / Zi^2 and yi = Yi / Zi^3, for i=1,2,3, gives:
			 *    U1 = X1*Z2^2, U2 = X2*Z1^2
			 *    S1 = Y1*Z2^3, S2 = Y2*Z1^3
			 *    Z = Z1*Z2
			 *    T = U1+U2
			 *    M = S1+S2
			 *    Q = T*M^2
			 *    R = T^2-U1*U2
			 *    X3 = 4*(R^2-Q)
			 *    Y3 = 4*(R*(3*Q-2*R^2)-M^4)
			 *    Z3 = 2*M*Z
			 *  (Note that the paper uses xi = Xi / Zi and yi = Yi / Zi instead.)
			 *
			 *  This formula has the benefit of being the same for both addition
			 *  of distinct points and doubling. However, it breaks down in the
			 *  case that either point is infinity, or that y1 = -y2. We handle
			 *  these cases in the following ways:
			 *
			 *    - If b is infinity we simply bail by means of a VERIFY_CHECK.
			 *
			 *    - If a is infinity, we detect this, and at the end of the
			 *      computation replace the result (which will be meaningless,
			 *      but we compute to be constant-time) with b.x : b.y : 1.
			 *
			 *    - If a = -b, we have y1 = -y2, which is a degenerate case.
			 *      But here the answer is infinity, so we simply set the
			 *      infinity flag of the result, overriding the computed values
			 *      without even needing to cmov.
			 *
			 *    - If y1 = -y2 but x1 != x2, which does occur thanks to certain
			 *      properties of our curve (specifically, 1 has nontrivial cube
			 *      roots in our field, and the curve equation has no x coefficient)
			 *      then the answer is not infinity but also not given by the above
			 *      equation. In this case, we cmov in place an alternate expression
			 *      for lambda. Specifically (y1 - y2)/(x1 - x2). Where both these
			 *      expressions for lambda are defined, they are equal, and can be
			 *      obtained from each other by multiplication by (y1 + y2)/(y1 + y2)
			 *      then substitution of x^3 + 7 for y^2 (using the curve equation).
			 *      For all pairs of nonzero points (a, b) at least one is defined,
			 *      so this covers everything.
			 */

			zz = a.z.Sqr();                       /* z = Z1^2 */
			u1 = a.x;
			u1 = u1.NormalizeWeak();        /* u1 = U1 = X1*Z2^2 (1) */
			u2 = b.x * zz;                  /* u2 = U2 = X2*Z1^2 (1) */
			s1 = a.y;
			s1 = s1.NormalizeWeak();        /* s1 = S1 = Y1*Z2^3 (1) */
			s2 = b.y * zz;                  /* s2 = Y2*Z1^2 (1) */
			s2 = s2 * a.z;                  /* s2 = S2 = Y2*Z1^3 (1) */
			t = u1; t += u2;                  /* t = T = U1+U2 (2) */
			m = s1; m += s2;                  /* m = M = S1+S2 (2) */
			rr = t.Sqr();                          /* rr = T^2 (1) */
			m_alt = u2.Negate(1);                /* Malt = -X2*Z1^2 */
			tt = u1 * m_alt;                 /* tt = -U1*U2 (2) */
			rr += tt;                         /* rr = R = T^2-U1*U2 (3) */
			/* If lambda = R/M = 0/0 we have a problem (except in the "trivial"
			 *  case that Z = z1z2 = 0, and this is special-cased later on). */
			degenerate = (m.NormalizesToZero() ? 1 : 0) & (rr.NormalizesToZero() ? 1 : 0);
			/* This only occurs when y1 == -y2 and x1^3 == x2^3, but x1 != x2.
			 * This means either x1 == beta*x2 or beta*x1 == x2, where beta is
			 * a nontrivial cube root of one. In either case, an alternate
			 * non-indeterminate expression for lambda is (y1 - y2)/(x1 - x2),
			 * so we set R/M equal to this. */
			rr_alt = s1;
			rr_alt *= 2U;       /* rr = Y1*Z2^3 - Y2*Z1^3 (2) */
			m_alt += u1;          /* Malt = X1*Z2^2 - X2*Z1^2 */

			FE.CMov(ref rr_alt, rr, degenerate != 0 ? 0 : 1);
			FE.CMov(ref m_alt, m, degenerate != 0 ? 0 : 1);
			/* Now Ralt / Malt = lambda and is guaranteed not to be 0/0.
			 * From here on out Ralt and Malt represent the numerator
			 * and denominator of lambda; R and M represent the explicit
			 * expressions x1^2 + x2^2 + x1x2 and y1 + y2. */
			n = m_alt.Sqr();                       /* n = Malt^2 (1) */
			q = n * t;                       /* q = Q = T*Malt^2 (1) */
			/* These two lines use the observation that either M == Malt or M == 0,
			 * so M^3 * Malt is either Malt^4 (which is computed by squaring), or
			 * zero (which is "computed" by cmov). So the cost is one squaring
			 * versus two multiplications. */
			n = n.Sqr();
			FE.CMov(ref n, m, degenerate);              /* n = M^3 * Malt (2) */
			t = rr_alt.Sqr();                      /* t = Ralt^2 (1) */
			rz = a.z * m_alt;             /* rz = Malt*Z (1) */
			infinity = (rz.NormalizesToZero() ? 1 : 0) * (1 - (a.infinity ? 1 : 0));
			rz *= 2U;                     /* rz = Z3 = 2*Malt*Z (2) */
			q = q.Negate(1);                     /* q = -Q (2) */
			t += q;                           /* t = Ralt^2-Q (3) */
			t = t.NormalizeWeak();
			rx = t;                                           /* rx = Ralt^2-Q (1) */
			t *= 2U;                        /* t = 2*x3 (2) */
			t += q;                           /* t = 2*x3 - Q: (4) */
			t = t * rr_alt;                  /* t = Ralt*(2*x3 - Q) (1) */
			t += n;                           /* t = Ralt*(2*x3 - Q) + M^3*Malt (3) */
			ry = t.Negate(3);                  /* ry = Ralt*(Q - 2x3) - M^3*Malt (4) */
			ry = ry.NormalizeWeak();
			rx *= 4U;                     /* rx = X3 = 4*(Ralt^2-Q) */
			ry *= 4U;                     /* ry = Y3 = 4*Ralt*(Q - 2x3) - 4*M^3*Malt (4) */

			/* In case a.infinity == 1, replace r with (b.x, b.y, 1). */
			FE.CMov(ref rx, b.x, a.infinity ? 1 : 0);
			FE.CMov(ref ry, b.y, a.infinity ? 1 : 0);
			FE.CMov(ref rz, fe_1, a.infinity ? 1 : 0);
			rinfinity = infinity == 1;
			return new GEJ(rx, ry, rz, rinfinity);
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public readonly GEJ Rescale(in FE s)
		{
			var (rx, ry, rz, rinfinity) = this;
			/* Operations: 4 mul, 1 sqr */
			VERIFY_CHECK(!s.IsZero);
			FE zz = s.Sqr();
			rx *= zz; /* r->x *= s^2 */
			ry *= zz;
			ry *= s; /* r->y *= s^3 */
			rz *= s; /* r->z *= s   */
			return new GEJ(rx, ry, rz, rinfinity);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static GEJ operator +(in GEJ a, in GE b)
		{
			return a.Add(b);
		}

		public readonly void Deconstruct(out FE x, out FE y, out FE z, out bool infinity)
		{
			x = this.x;
			y = this.y;
			z = this.z;
			infinity = this.infinity;
		}

		public readonly string ToC(string varName)
		{
			StringBuilder b = new StringBuilder();
			b.AppendLine(x.ToC($"{varName}x"));
			b.AppendLine(y.ToC($"{varName}y"));
			b.AppendLine(z.ToC($"{varName}z"));
			var infinitystr = infinity ? 1 : 0;
			b.AppendLine($"int {varName}infinity = {infinitystr};");
			b.AppendLine($"secp256k1_gej {varName} = {{ {varName}x, {varName}y, {varName}z, {varName}infinity }};");
			return b.ToString();
		}
	}
}
#nullable restore
#endif
