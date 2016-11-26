#if !USEBC

using System;
using System.Diagnostics;

namespace HashLib
{
    internal class HMACBuildInAdapter : Hash, IHMACBuildIn
    {
        protected static readonly byte[] EMPTY = new byte[0];
        protected System.Security.Cryptography.HMAC m_hmac;
        private byte[] m_key;

        public byte[] Key
        {
            get
            {
                return (byte[])m_key.Clone();
            }
            set
            {
                if (m_key == null)
                {
                    m_key = new byte[0];
                }
                else
                {
                    m_key = (byte[])value.Clone();
                }
            }
        }

        public int? KeyLength
        {
            get
            {
                return null;
            }
        }

        public HMACBuildInAdapter(System.Security.Cryptography.HMAC a_hmac, int a_block_size)
            : base(a_hmac.HashSize / 8, a_block_size)
        {
            m_hmac = a_hmac;
            m_key = new byte[0];
        }

        public override void Initialize()
        {
            m_hmac.Initialize();
            m_hmac.Key = Key;
        }

        public override HashResult TransformFinal()
        {
#if !NETCORE
            m_hmac.TransformFinalBlock(EMPTY, 0, 0);
            byte[] result = m_hmac.Hash;


            Debug.Assert(result.Length == HashSize);

            Initialize();
            return new HashResult(result);
#else
			return null;
#endif
		}

		public override void TransformBytes(byte[] a_data, int a_index, int a_length)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_data.Length);
#if !NETCORE
            m_hmac.TransformBlock(a_data, a_index, a_length, null, 0);
#endif
        }

        public override string Name
        {
            get
            {
                return String.Format("{0}({1})", GetType().Name, m_hmac.GetType().Name);
            }
        }
    }
}
#endif