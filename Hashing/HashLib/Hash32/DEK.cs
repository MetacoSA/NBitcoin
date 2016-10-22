using System;

namespace HashLib.Hash32
{
    internal class DEK : MultipleTransformNonBlock, IHash32
    {
        public DEK()
            : base(4, 1)
        {
        }

        protected override HashResult ComputeAggregatedBytes(byte[] a_data)
        {
            uint hash = (uint)a_data.Length;

            foreach (byte b in a_data)
                hash = ((hash << 5) ^ (hash >> 27)) ^ b;

            return new HashResult(hash);
        }
    }
}
