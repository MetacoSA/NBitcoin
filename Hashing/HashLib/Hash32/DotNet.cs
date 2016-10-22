using System;
using System.Text;

namespace HashLib.Hash32
{
    internal class DotNet : MultipleTransformNonBlock, IHash32, IFastHash32
    {
        public DotNet()
            : base(4, 8)
        {
        }

        protected override HashResult ComputeAggregatedBytes(byte[] a_data)
        {
            return new HashResult(ComputeBytesFast(a_data));
        }

        public int ComputeBytesFast(byte[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeByteFast(byte a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeCharFast(char a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeShortFast(short a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeUShortFast(ushort a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeIntFast(int a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeUIntFast(uint a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeLongFast(long a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeULongFast(ulong a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeFloatFast(float a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeDoubleFast(double a_data)
        {
            return a_data.GetHashCode();
        }


        public int ComputeStringFast(string a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeCharsFast(char[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeShortsFast(short[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeUShortsFast(ushort[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeIntsFast(int[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeUIntsFast(uint[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeLongsFast(long[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeULongsFast(ulong[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeDoublesFast(double[] a_data)
        {
            return a_data.GetHashCode();
        }

        public int ComputeFloatsFast(float[] a_data)
        {
            return a_data.GetHashCode();
        }
    }
}
