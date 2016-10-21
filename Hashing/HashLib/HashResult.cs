using System;
using System.Diagnostics;
using System.Linq;

namespace HashLib
{
    [DebuggerDisplay("HashResult, Size: {m_hash.Length}, Hash: {ToString()}")]
    public class HashResult
    {
        private byte[] m_hash;

        public HashResult(uint a_hash)
        {
            m_hash = BitConverter.GetBytes(a_hash);
        }

        internal HashResult(int a_hash)
        {
            m_hash = BitConverter.GetBytes(a_hash);
        }

        public HashResult(ulong a_hash)
        {
            m_hash = BitConverter.GetBytes(a_hash);
        }
        public HashResult(byte[] a_hash)
        {
            m_hash = a_hash;
        }

        public byte[] GetBytes()
        {
            return m_hash.ToArray();
        }

        public uint GetUInt()
        {
            if (m_hash.Length != 4)
                throw new InvalidOperationException();

            return BitConverter.ToUInt32(m_hash, 0);
        }

        public int GetInt()
        {
            if (m_hash.Length != 4)
                throw new InvalidOperationException();

            return BitConverter.ToInt32(m_hash, 0);
        }

        public ulong GetULong()
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
            if ((HashResult)hash_result == null)
                return false;

            return Equals(hash_result);
        }

        public bool Equals(HashResult a_hashResult)
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
