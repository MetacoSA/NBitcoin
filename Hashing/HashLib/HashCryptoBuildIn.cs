using System;
using System.Diagnostics;

namespace HashLib
{
    internal class HashCryptoBuildIn : Hash, ICryptoBuildIn
    {
        protected static readonly byte[] EMPTY = new byte[0];

        protected System.Security.Cryptography.HashAlgorithm m_hash_algorithm;

        public HashCryptoBuildIn(System.Security.Cryptography.HashAlgorithm a_hash_algorithm, int a_block_size)
                : base(a_hash_algorithm.HashSize / 8, a_block_size)
        {
            if (a_hash_algorithm.CanReuseTransform == false)
                throw new NotImplementedException();
            if (a_hash_algorithm.CanTransformMultipleBlocks == false)
                throw new NotImplementedException();

            m_hash_algorithm = a_hash_algorithm;
        }

        public override void Initialize()
        {
            m_hash_algorithm.Initialize();
        }

        public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);

            m_hash_algorithm.TransformBlock(a_data, a_index, a_length, null, 0);
        }

        public override HashResult TransformFinal()
        {
            m_hash_algorithm.TransformFinalBlock(EMPTY, 0, 0);
            byte[] result = m_hash_algorithm.Hash;

            Debug.Assert(result.Length == HashSize);

            Initialize();
            return new HashResult(result);
        }
    }
}
