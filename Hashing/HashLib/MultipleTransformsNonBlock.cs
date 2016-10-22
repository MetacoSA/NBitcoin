using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HashLib
{
    internal abstract class MultipleTransformNonBlock : Hash, INonBlockHash
    {
        private List<ArraySegment<byte>> m_list = new List<ArraySegment<byte>>();

        public MultipleTransformNonBlock(int a_hash_size, int a_block_size)
            : base(a_hash_size, a_block_size)
        {
        }

        public override void Initialize()
        {
            m_list.Clear();
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);

            m_list.Add(new ArraySegment<byte>(a_data, a_index, a_length));
        }

        public override HashResult TransformFinal()
        {
            HashResult result = ComputeAggregatedBytes(Aggregate());
            Initialize();
            return result;
        }

        private byte[] Aggregate()
        {
            int sum = 0;
            foreach (ArraySegment<byte> seg in m_list)
                sum += seg.Count;

            byte[] res = new byte[sum];

            int index = 0;

            foreach (ArraySegment<byte> seg in m_list)
            {
                Array.Copy(seg.Array, seg.Offset, res, index, seg.Count);
                index += seg.Count;
            }

            return res;
        }

        public override HashResult ComputeBytes(byte[] a_data)
        {
            Initialize();
            return ComputeAggregatedBytes(a_data);
        }

        protected abstract HashResult ComputeAggregatedBytes(byte[] a_data);
    }
}
