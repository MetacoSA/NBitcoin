﻿using System;
using System.Diagnostics;

using NBitcoin.BouncyCastle.Math.Raw;

namespace NBitcoin.BouncyCastle.Math.EC.Custom.Sec
{
    internal class SecT239Field
    {
        private const ulong M47 = ulong.MaxValue >> 17;
        private const ulong M60 = ulong.MaxValue >> 4;

        public static void Add(ulong[] x, ulong[] y, ulong[] z)
        {
            z[0] = x[0] ^ y[0];
            z[1] = x[1] ^ y[1];
            z[2] = x[2] ^ y[2];
            z[3] = x[3] ^ y[3];
        }

        public static void AddExt(ulong[] xx, ulong[] yy, ulong[] zz)
        {
            zz[0] = xx[0] ^ yy[0];
            zz[1] = xx[1] ^ yy[1];
            zz[2] = xx[2] ^ yy[2];
            zz[3] = xx[3] ^ yy[3];
            zz[4] = xx[4] ^ yy[4];
            zz[5] = xx[5] ^ yy[5];
            zz[6] = xx[6] ^ yy[6];
            zz[7] = xx[7] ^ yy[7];
        }

        public static void AddOne(ulong[] x, ulong[] z)
        {
            z[0] = x[0] ^ 1UL;
            z[1] = x[1];
            z[2] = x[2];
            z[3] = x[3];
        }

        public static ulong[] FromBigInteger(BigInteger x)
        {
            ulong[] z = Nat256.FromBigInteger64(x);
            Reduce17(z, 0);
            return z;
        }

        public static void Invert(ulong[] x, ulong[] z)
        {
            if (Nat256.IsZero64(x))
                throw new InvalidOperationException();

            // Itoh-Tsujii inversion

            ulong[] t0 = Nat256.Create64();
            ulong[] t1 = Nat256.Create64();

            Square(x, t0);
            Multiply(t0, x, t0);
            Square(t0, t0);
            Multiply(t0, x, t0);
            SquareN(t0, 3, t1);
            Multiply(t1, t0, t1);
            Square(t1, t1);
            Multiply(t1, x, t1);
            SquareN(t1, 7, t0);
            Multiply(t0, t1, t0);
            SquareN(t0, 14, t1);
            Multiply(t1, t0, t1);
            Square(t1, t1);
            Multiply(t1, x, t1);
            SquareN(t1, 29, t0);
            Multiply(t0, t1, t0);
            Square(t0, t0);
            Multiply(t0, x, t0);
            SquareN(t0, 59, t1);
            Multiply(t1, t0, t1);
            Square(t1, t1);
            Multiply(t1, x, t1);
            SquareN(t1, 119, t0);
            Multiply(t0, t1, t0);
            Square(t0, z);
        }

        public static void Multiply(ulong[] x, ulong[] y, ulong[] z)
        {
            ulong[] tt = Nat256.CreateExt64();
            ImplMultiply(x, y, tt);
            Reduce(tt, z);
        }

        public static void MultiplyAddToExt(ulong[] x, ulong[] y, ulong[] zz)
        {
            ulong[] tt = Nat256.CreateExt64();
            ImplMultiply(x, y, tt);
            AddExt(zz, tt, zz);
        }

        public static void Reduce(ulong[] xx, ulong[] z)
        {
            ulong x0 = xx[0], x1 = xx[1], x2 = xx[2], x3 = xx[3];
            ulong x4 = xx[4], x5 = xx[5], x6 = xx[6], x7 = xx[7];

            x3 ^= (x7 << 17);
            x4 ^= (x7 >> 47);
            x5 ^= (x7 << 47);
            x6 ^= (x7 >> 17);

            x2 ^= (x6 << 17);
            x3 ^= (x6 >> 47);
            x4 ^= (x6 << 47);
            x5 ^= (x6 >> 17);

            x1 ^= (x5 << 17);
            x2 ^= (x5 >> 47);
            x3 ^= (x5 << 47);
            x4 ^= (x5 >> 17);

            x0 ^= (x4 << 17);
            x1 ^= (x4 >> 47);
            x2 ^= (x4 << 47);
            x3 ^= (x4 >> 17);

            ulong t = x3 >> 47;
            z[0]    = x0 ^ t;
            z[1]    = x1;
            z[2]    = x2 ^ (t << 30);
            z[3]    = x3 & M47;
        }

        public static void Reduce17(ulong[] z, int zOff)
        {
            ulong z3     = z[zOff + 3], t = z3 >> 47;
            z[zOff    ] ^= t;
            z[zOff + 2] ^= (t << 30);
            z[zOff + 3]  = z3 & M47;
        }

        public static void Sqrt(ulong[] x, ulong[] z)
        {
            ulong u0, u1;
            u0 = Interleave.Unshuffle(x[0]); u1 = Interleave.Unshuffle(x[1]);
            ulong e0 = (u0 & 0x00000000FFFFFFFFUL) | (u1 << 32);
            ulong c0 = (u0 >> 32) | (u1 & 0xFFFFFFFF00000000UL);

            u0 = Interleave.Unshuffle(x[2]); u1 = Interleave.Unshuffle(x[3]);
            ulong e1 = (u0 & 0x00000000FFFFFFFFUL) | (u1 << 32);
            ulong c1 = (u0 >> 32) | (u1 & 0xFFFFFFFF00000000UL);

            ulong c2, c3;
            c3  = (c1 >> 49);
            c2  = (c0 >> 49) | (c1 << 15);
            c1 ^=              (c0 << 15);

            ulong[] tt = Nat256.CreateExt64();

            int[] shifts = { 39, 120 };
            for (int i = 0; i < shifts.Length; ++i)
            {
                int w = shifts[i] >> 6, s = shifts[i] & 63;
                Debug.Assert(s != 0);
                tt[w    ] ^= (c0 << s);
                tt[w + 1] ^= (c1 << s) | (c0 >> -s);
                tt[w + 2] ^= (c2 << s) | (c1 >> -s);
                tt[w + 3] ^= (c3 << s) | (c2 >> -s);
                tt[w + 4] ^=             (c3 >> -s);
            }

            Reduce(tt, z);

            z[0] ^= e0;
            z[1] ^= e1;
        }

        public static void Square(ulong[] x, ulong[] z)
        {
            ulong[] tt = Nat256.CreateExt64();
            ImplSquare(x, tt);
            Reduce(tt, z);
        }

        public static void SquareAddToExt(ulong[] x, ulong[] zz)
        {
            ulong[] tt = Nat256.CreateExt64();
            ImplSquare(x, tt);
            AddExt(zz, tt, zz);
        }

        public static void SquareN(ulong[] x, int n, ulong[] z)
        {
            Debug.Assert(n > 0);

            ulong[] tt = Nat256.CreateExt64();
            ImplSquare(x, tt);
            Reduce(tt, z);

            while (--n > 0)
            {
                ImplSquare(z, tt);
                Reduce(tt, z);
            }
        }

        public static uint Trace(ulong[] x)
        {
            // Non-zero-trace bits: 0, 81, 162
            return (uint)(x[0] ^ (x[1] >> 17) ^ (x[2] >> 34)) & 1U;
        }

        protected static void ImplCompactExt(ulong[] zz)
        {
            ulong z0 = zz[0], z1 = zz[1], z2 = zz[2], z3 = zz[3], z4 = zz[4], z5 = zz[5], z6 = zz[6], z7 = zz[7];
            zz[0] =  z0        ^ (z1 << 60);
            zz[1] = (z1 >>  4) ^ (z2 << 56);
            zz[2] = (z2 >>  8) ^ (z3 << 52);
            zz[3] = (z3 >> 12) ^ (z4 << 48);
            zz[4] = (z4 >> 16) ^ (z5 << 44);
            zz[5] = (z5 >> 20) ^ (z6 << 40);
            zz[6] = (z6 >> 24) ^ (z7 << 36);
            zz[7] = (z7 >> 28);
        }

        protected static void ImplExpand(ulong[] x, ulong[] z)
        {
            ulong x0 = x[0], x1 = x[1], x2 = x[2], x3 = x[3];
            z[0] = x0 & M60;
            z[1] = ((x0 >> 60) ^ (x1 <<  4)) & M60;
            z[2] = ((x1 >> 56) ^ (x2 <<  8)) & M60;
            z[3] = ((x2 >> 52) ^ (x3 << 12));
        }

        protected static void ImplMultiply(ulong[] x, ulong[] y, ulong[] zz)
        {
            /*
             * "Two-level seven-way recursion" as described in "Batch binary Edwards", Daniel J. Bernstein.
             */

            ulong[] f = new ulong[4], g = new ulong[4];
            ImplExpand(x, f);
            ImplExpand(y, g);

            ImplMulwAcc(f[0], g[0], zz, 0);
            ImplMulwAcc(f[1], g[1], zz, 1);
            ImplMulwAcc(f[2], g[2], zz, 2);
            ImplMulwAcc(f[3], g[3], zz, 3);

            // U *= (1 - t^n)
            for (int i = 5; i > 0; --i)
            {
                zz[i] ^= zz[i - 1];
            }

            ImplMulwAcc(f[0] ^ f[1], g[0] ^ g[1], zz, 1);
            ImplMulwAcc(f[2] ^ f[3], g[2] ^ g[3], zz, 3);

            // V *= (1 - t^2n)
            for (int i = 7; i > 1; --i)
            {
                zz[i] ^= zz[i - 2];
            }

            // Double-length recursion
            {
                ulong c0 = f[0] ^ f[2], c1 = f[1] ^ f[3];
                ulong d0 = g[0] ^ g[2], d1 = g[1] ^ g[3];
                ImplMulwAcc(c0 ^ c1, d0 ^ d1, zz, 3);
                ulong[] t = new ulong[3];
                ImplMulwAcc(c0, d0, t, 0);
                ImplMulwAcc(c1, d1, t, 1);
                ulong t0 = t[0], t1 = t[1], t2 = t[2];
                zz[2] ^= t0;
                zz[3] ^= t0 ^ t1;
                zz[4] ^= t2 ^ t1;
                zz[5] ^= t2;
            }

            ImplCompactExt(zz);
        }

        protected static void ImplMulwAcc(ulong x, ulong y, ulong[] z, int zOff)
        {
            Debug.Assert(x >> 60 == 0);
            Debug.Assert(y >> 60 == 0);

            ulong[] u = new ulong[8];
            //u[0] = 0;
            u[1] = y;
            u[2] = u[1] << 1;
            u[3] = u[2] ^  y;
            u[4] = u[2] << 1;
            u[5] = u[4] ^  y;
            u[6] = u[3] << 1;
            u[7] = u[6] ^  y;

            uint j = (uint)x;
            ulong g, h = 0, l = u[j & 7]
                              ^ (u[(j >> 3) & 7] << 3);
            int k = 54;
            do
            {
                j  = (uint)(x >> k);
                g  = u[j & 7]
                   ^ u[(j >> 3) & 7] << 3;
                l ^= (g <<  k);
                h ^= (g >> -k);
            }
            while ((k -= 6) > 0);

            h ^= ((x & 0x0820820820820820L) & (ulong)(((long)y << 4) >> 63)) >> 5;

            Debug.Assert(h >> 55 == 0);

            z[zOff    ] ^= l & M60;
            z[zOff + 1] ^= (l >> 60) ^ (h << 4);
        }

        protected static void ImplSquare(ulong[] x, ulong[] zz)
        {
            Interleave.Expand64To128(x[0], zz, 0);
            Interleave.Expand64To128(x[1], zz, 2);
            Interleave.Expand64To128(x[2], zz, 4);

            ulong x3 = x[3];
            zz[6] = Interleave.Expand32to64((uint)x3);
            zz[7] = Interleave.Expand16to32((uint)(x3 >> 32));
        }
    }
}
