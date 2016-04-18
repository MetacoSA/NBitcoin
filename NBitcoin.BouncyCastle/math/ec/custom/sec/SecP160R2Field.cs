﻿using System;
using System.Diagnostics;

using NBitcoin.BouncyCastle.Math.Raw;

namespace NBitcoin.BouncyCastle.Math.EC.Custom.Sec
{
    internal class SecP160R2Field
    {
        // 2^160 - 2^32 - 2^14 - 2^12 - 2^9 - 2^8 - 2^7 - 2^3 - 2^2 - 1
        internal static readonly uint[] P = new uint[]{ 0xFFFFAC73, 0xFFFFFFFE, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        internal static readonly uint[] PExt = new uint[]{ 0x1B44BBA9, 0x0000A71A, 0x00000001, 0x00000000, 0x00000000,
            0xFFFF58E6, 0xFFFFFFFD, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
        private static readonly uint[] PExtInv = new uint[]{ 0xE4BB4457, 0xFFFF58E5, 0xFFFFFFFE, 0xFFFFFFFF, 0xFFFFFFFF,
            0x0000A719, 0x00000002 };
        private const uint P4 = 0xFFFFFFFF;
        private const uint PExt9 = 0xFFFFFFFF;
        private const uint PInv33 = 0x538D;

        public static void Add(uint[] x, uint[] y, uint[] z)
        {
            uint c = Nat160.Add(x, y, z);
            if (c != 0 || (z[4] == P4 && Nat160.Gte(z, P)))
            {
                Nat.Add33To(5, PInv33, z);
            }
        }

        public static void AddExt(uint[] xx, uint[] yy, uint[] zz)
        {
            uint c = Nat.Add(10, xx, yy, zz);
            if (c != 0 || (zz[9] == PExt9 && Nat.Gte(10, zz, PExt)))
            {
                if (Nat.AddTo(PExtInv.Length, PExtInv, zz) != 0)
                {
                    Nat.IncAt(10, zz, PExtInv.Length);
                }
            }
        }

        public static void AddOne(uint[] x, uint[] z)
        {
            uint c = Nat.Inc(5, x, z);
            if (c != 0 || (z[4] == P4 && Nat160.Gte(z, P)))
            {
                Nat.Add33To(5, PInv33, z);
            }
        }

        public static uint[] FromBigInteger(BigInteger x)
        {
            uint[] z = Nat160.FromBigInteger(x);
            if (z[4] == P4 && Nat160.Gte(z, P))
            {
                Nat160.SubFrom(P, z);
            }
            return z;
        }

        public static void Half(uint[] x, uint[] z)
        {
            if ((x[0] & 1) == 0)
            {
                Nat.ShiftDownBit(5, x, 0, z);
            }
            else
            {
                uint c = Nat160.Add(x, P, z);
                Nat.ShiftDownBit(5, z, c);
            }
        }

        public static void Multiply(uint[] x, uint[] y, uint[] z)
        {
            uint[] tt = Nat160.CreateExt();
            Nat160.Mul(x, y, tt);
            Reduce(tt, z);
        }

        public static void MultiplyAddToExt(uint[] x, uint[] y, uint[] zz)
        {
            uint c = Nat160.MulAddTo(x, y, zz);
            if (c != 0 || (zz[9] == PExt9 && Nat.Gte(10, zz, PExt)))
            {
                if (Nat.AddTo(PExtInv.Length, PExtInv, zz) != 0)
                {
                    Nat.IncAt(10, zz, PExtInv.Length);
                }
            }
        }

        public static void Negate(uint[] x, uint[] z)
        {
            if (Nat160.IsZero(x))
            {
                Nat160.Zero(z);
            }
            else
            {
                Nat160.Sub(P, x, z);
            }
        }

        public static void Reduce(uint[] xx, uint[] z)
        {
            ulong cc = Nat160.Mul33Add(PInv33, xx, 5, xx, 0, z, 0);
            uint c = Nat160.Mul33DWordAdd(PInv33, cc, z, 0);

            Debug.Assert(c == 0 || c == 1);

            if (c != 0 || (z[4] == P4 && Nat160.Gte(z, P)))
            {
                Nat.Add33To(5, PInv33, z);
            }
        }

        public static void Reduce32(uint x, uint[] z)
        {
            if ((x != 0 && Nat160.Mul33WordAdd(PInv33, x, z, 0) != 0)
                || (z[4] == P4 && Nat160.Gte(z, P)))
            {
                Nat.Add33To(5, PInv33, z);
            }
        }

        public static void Square(uint[] x, uint[] z)
        {
            uint[] tt = Nat160.CreateExt();
            Nat160.Square(x, tt);
            Reduce(tt, z);
        }

        public static void SquareN(uint[] x, int n, uint[] z)
        {
            Debug.Assert(n > 0);

            uint[] tt = Nat160.CreateExt();
            Nat160.Square(x, tt);
            Reduce(tt, z);

            while (--n > 0)
            {
                Nat160.Square(z, tt);
                Reduce(tt, z);
            }
        }

        public static void Subtract(uint[] x, uint[] y, uint[] z)
        {
            int c = Nat160.Sub(x, y, z);
            if (c != 0)
            {
                Nat.Sub33From(5, PInv33, z);
            }
        }

        public static void SubtractExt(uint[] xx, uint[] yy, uint[] zz)
        {
            int c = Nat.Sub(10, xx, yy, zz);
            if (c != 0)
            {
                if (Nat.SubFrom(PExtInv.Length, PExtInv, zz) != 0)
                {
                    Nat.DecAt(10, zz, PExtInv.Length);
                }
            }
        }

        public static void Twice(uint[] x, uint[] z)
        {
            uint c = Nat.ShiftUpBit(5, x, 0, z);
            if (c != 0 || (z[4] == P4 && Nat160.Gte(z, P)))
            {
                Nat.Add33To(5, PInv33, z);
            }
        }
    }
}
