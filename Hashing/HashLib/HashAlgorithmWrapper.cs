using System;
using System.Diagnostics;

namespace HashLib
{
    internal class HashAlgorithmWrapper : System.Security.Cryptography.HashAlgorithm
    {
        private IHash m_hash;

        public HashAlgorithmWrapper(IHash a_hash)
        {
            m_hash = a_hash;
            HashSizeValue = a_hash.HashSize * 8;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            Debug.Assert(cbSize >= 0);
            Debug.Assert(ibStart >= 0);
            Debug.Assert(ibStart + cbSize <= array.Length);

            m_hash.TransformBytes(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            HashValue = m_hash.TransformFinal().GetBytes();
            return HashValue;
        }

        public override void Initialize()
        {
            m_hash.Initialize();
        }
    }
}
