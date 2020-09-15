using System;
using System.Diagnostics;
using System.Linq;

namespace NBitcoin.Altcoins.HashX11
{
    [DebuggerDisplay("HashResult, Size: {m_hash.Length}, Hash: {ToString()}")]
    internal class HashResult
    {
        private byte[] m_hash;

        internal HashResult(uint a_hash)
        {
            m_hash = BitConverter.GetBytes(a_hash);
        }

        internal HashResult(int a_hash)
        {
            m_hash = BitConverter.GetBytes(a_hash);
        }

        internal HashResult(ulong a_hash)
        {
            m_hash = BitConverter.GetBytes(a_hash);
        }
        internal HashResult(byte[] a_hash)
        {
            m_hash = a_hash;
        }

        internal byte[] GetBytes()
        {
            return m_hash.ToArray();
        }

        internal uint GetUInt()
        {
            if (m_hash.Length != 4)
                throw new InvalidOperationException();

            return BitConverter.ToUInt32(m_hash, 0);
        }

        internal int GetInt()
        {
            if (m_hash.Length != 4)
                throw new InvalidOperationException();

            return BitConverter.ToInt32(m_hash, 0);
        }

        internal ulong GetULong()
        {
            if (m_hash.Length != 8)
                throw new InvalidOperationException();

            return BitConverter.ToUInt64(m_hash, 0);
        }

        public override string ToString()
        {
            return Converters.ConvertBytesToHexString(m_hash);
        }

        public override bool Equals(Object a_obj)
        {
            HashResult hash_result = a_obj as HashResult;
            if (hash_result == null)
                return false;

            return Equals(hash_result);
        }

        internal bool Equals(HashResult a_hashResult)
        {
            return HashResult.SameArrays(a_hashResult.GetBytes(), m_hash);
        }

		public override int GetHashCode()
        {
            return Convert.ToBase64String(m_hash).GetHashCode();
        }

        private static bool SameArrays(byte[] a_ar1, byte[] a_ar2)
        {
            if (Object.ReferenceEquals(a_ar1, a_ar2))
                return true;

            if (a_ar1.Length != a_ar2.Length)
                return false;

            for (int i = 0; i < a_ar1.Length; i++)
                if (a_ar1[i] != a_ar2[i])
                    return false;

            return true;
        }
    }
}
