using System;
using System.Diagnostics;

/* C# Version Copyright (C) 2001-2004 Akihilo Kramot (Takel).  */
/* C# porting from a C-program for MT19937, originaly coded by */
/* Takuji Nishimura, considering the suggestions by            */
/* Topher Cooper and Marc Rieffel in July-Aug. 1997.           */
/* This library is free software under the Artistic license:   */
/*                                                             */
/* You can find the original C-program at                      */
/*     http://www.math.keio.ac.jp/~matumoto/mt.html            */
/*                                                             */

namespace TomanuExtensions.Utils
{
    public class MersenneTwister : System.Random
    {
        /* Period parameters */
        private const int N = 624;
        private const int M = 397;
        private const uint MATRIX_A = 0x9908b0df; /* constant vector a */
        private const uint UPPER_MASK = 0x80000000; /* most significant w-r bits */
        private const uint LOWER_MASK = 0x7fffffff; /* least significant r bits */

        /* Tempering parameters */
        private const uint TEMPERING_MASK_B = 0x9d2c5680;
        private const uint TEMPERING_MASK_C = 0xefc60000;

        private static uint TEMPERING_SHIFT_U(uint y) { return (y >> 11); }

        private static uint TEMPERING_SHIFT_S(uint y) { return (y << 7); }

        private static uint TEMPERING_SHIFT_T(uint y) { return (y << 15); }

        private static uint TEMPERING_SHIFT_L(uint y) { return (y >> 18); }

        private uint[] mt = new uint[N]; /* the array for the state vector  */

        private short mti;

        private static uint[] mag01 = { 0x0, MATRIX_A };

        /* initializing the array with a NONZERO seed */

        public MersenneTwister(uint seed)
        {
            /* setting initial seeds to mt[N] using         */
            /* the generator Line 25 of Table 1 in          */
            /* [KNUTH 1981, The Art of Computer Programming */
            /*    Vol. 2 (2nd Ed.), pp102]                  */
            mt[0] = seed & 0xffffffffU;
            for (mti = 1; mti < N; ++mti)
            {
                mt[mti] = (69069 * mt[mti - 1]) & 0xffffffffU;
            }
        }

        public MersenneTwister()
            : this((uint)System.Environment.TickCount)
        {
        }

        protected uint GenerateUInt()
        {
            uint y;

            /* mag01[x] = x * MATRIX_A  for x=0,1 */
            if (mti >= N) /* generate N words at one time */
            {
                short kk = 0;

                for (; kk < N - M; ++kk)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1];
                }

                for (; kk < N - 1; ++kk)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1];
                }

                y = (mt[N - 1] & UPPER_MASK) | (mt[0] & LOWER_MASK);
                mt[N - 1] = mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1];

                mti = 0;
            }

            y = mt[mti++];
            y ^= TEMPERING_SHIFT_U(y);
            y ^= TEMPERING_SHIFT_S(y) & TEMPERING_MASK_B;
            y ^= TEMPERING_SHIFT_T(y) & TEMPERING_MASK_C;
            y ^= TEMPERING_SHIFT_L(y);

            return y;
        }

        public virtual uint NextUInt()
        {
            return this.GenerateUInt();
        }

        public virtual uint NextUInt(uint maxValue)
        {
            return (uint)(this.GenerateUInt() / ((double)uint.MaxValue / maxValue));
        }

        public virtual ushort NextUShort(ushort maxValue)
        {
            return (ushort)(this.GenerateUInt() / ((double)ushort.MaxValue / maxValue));
        }

        public virtual ushort NextUShort(int maxValue)
        {
            if (maxValue > ushort.MaxValue)
                throw new ArgumentOutOfRangeException();
            if (maxValue <= 0)
                throw new ArgumentOutOfRangeException();

            return (ushort)(this.GenerateUInt() / ((double)ushort.MaxValue / maxValue));
        }

        public virtual uint NextUInt(uint minValue, uint maxValue) /* throws ArgumentOutOfRangeException */
        {
            Debug.Assert(minValue < maxValue);

            return (uint)(this.GenerateUInt() / ((double)uint.MaxValue / (maxValue - minValue)) + minValue);
        }

        public override int Next()
        {
            return this.Next(int.MaxValue);
        }

        public override int Next(int maxValue) /* throws ArgumentOutOfRangeException */
        {
            Debug.Assert(maxValue > 0);

            return (int)(this.NextDouble() * maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            Debug.Assert(maxValue >= minValue);

            if (maxValue == minValue)
            {
                return minValue;
            }
            else
            {
                return this.Next(maxValue - minValue) + minValue;
            }
        }

        public override void NextBytes(byte[] buffer) /* throws ArgumentNullException*/
        {
            int bufLen = buffer.Length;

            for (int idx = 0; idx < bufLen; ++idx)
                buffer[idx] = (byte)this.Next(256);
        }

        public override double NextDouble()
        {
            return (double)this.GenerateUInt() / ((ulong)uint.MaxValue + 1);
        }

        public float NextFloat()
        {
            return (float)this.GenerateUInt() / ((ulong)uint.MaxValue + 1);
        }

        public byte NextByte()
        {
            return (byte)NextUInt(byte.MaxValue);
        }

        public char NextChar()
        {
            return (char)NextUInt(char.MaxValue);
        }

        public short NextShort()
        {
            return (short)Next(short.MinValue, short.MaxValue);
        }

        public ushort NextUShort()
        {
            return (ushort)NextUInt(ushort.MaxValue);
        }

        public int NextInt()
        {
            return (int)GenerateUInt();
        }

        public long NextLong()
        {
            return ((long)NextUInt() << 32) | NextUInt();
        }

        public ulong NextULong()
        {
            return ((ulong)NextUInt() << 32) | NextUInt();
        }

        public byte[] NextBytes(int a_length)
        {
            byte[] result = new byte[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextByte();
            return result;
        }

        public char[] NextChars(int a_length)
        {
            char[] result = new char[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextChar();
            return result;
        }

        public short[] NextShorts(int a_length)
        {
            short[] result = new short[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextShort();
            return result;
        }

        public ushort[] NextUShorts(int a_length)
        {
            ushort[] result = new ushort[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextUShort();
            return result;
        }

        public int[] NextInts(int a_length)
        {
            int[] result = new int[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = Next();
            return result;
        }

        public uint[] NextUInts(int a_length)
        {
            uint[] result = new uint[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextUInt();
            return result;
        }

        public long[] NextLongs(int a_length)
        {
            long[] result = new long[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextLong();
            return result;
        }

        public ulong[] NextULongs(int a_length)
        {
            ulong[] result = new ulong[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextULong();
            return result;
        }

        public string NextString(int a_length)
        {
            return new string(NextChars(a_length));
        }

        public double NextDoubleFull()
        {
            return BitConverter.Int64BitsToDouble(NextLong());
        }

        public float NextFloatFull()
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(NextUInt()), 0);
        }

        public double[] NextDoublesFull(int a_length)
        {
            double[] result = new double[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextDoubleFull();
            return result;
        }

        public double[] NextDoublesFullSafe(int a_length)
        {
            double[] result = new double[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextDoubleFullSafe();
            return result;
        }

        public double[] NextDoubles(int a_length)
        {
            double[] result = new double[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextDouble();
            return result;
        }

        public float[] NextFloatsFull(int a_length)
        {
            float[] result = new float[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextFloatFull();
            return result;
        }

        public float[] NextFloatsFullSafe(int a_length)
        {
            float[] result = new float[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextFloatFullSafe();
            return result;
        }

        public float[] NextFloats(int a_length)
        {
            float[] result = new float[a_length];
            for (int i = 0; i < a_length; i++)
                result[i] = NextFloat();
            return result;
        }

        public double NextDoubleFullSafe()
        {
            for (; ; )
            {
                double d = NextDoubleFull();

                if (Double.IsNaN(d))
                    continue;

                return d;
            }
        }

        public float NextFloatFullSafe()
        {
            for (; ; )
            {
                float f = NextFloatFull();

                if (!Single.IsNaN(f))
                    return f;
            }
        }
    }
}