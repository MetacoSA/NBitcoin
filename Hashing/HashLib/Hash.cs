using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HashLib
{
    internal abstract class Hash : IHash
    {
        private readonly int m_block_size;
        private readonly int m_hash_size;

        public static int BUFFER_SIZE = 64 * 1024;

        public Hash(int a_hash_size, int a_block_size)
        {
            Debug.Assert((a_block_size > 0) || (a_block_size == -1));
            Debug.Assert(a_hash_size > 0);

            m_block_size = a_block_size;
            m_hash_size = a_hash_size;
        }

        public virtual string Name
        {
            get
            {
                return GetType().Name;
            }
        }

        public virtual int BlockSize
        {
            get
            {
                return m_block_size;
            }
        }

        public virtual int HashSize
        {
            get
            {
                return m_hash_size;
            }
        }

        public virtual HashResult ComputeObject(object a_data)
        {
            if (a_data is byte)
                return ComputeByte((byte)a_data);
            else if (a_data is short)
                return ComputeShort((short)a_data);
            else if (a_data is ushort)
                return ComputeUShort((ushort)a_data);
            else if (a_data is char)
                return ComputeChar((char)a_data);
            else if (a_data is int)
                return ComputeInt((int)a_data);
            else if (a_data is uint)
                return ComputeUInt((uint)a_data);
            else if (a_data is long)
                return ComputeLong((long)a_data);
            else if (a_data is ulong)
                return ComputeULong((ulong)a_data);
            else if (a_data is float)
                return ComputeFloat((float)a_data);
            else if (a_data is double)
                return ComputeDouble((double)a_data);
            else if (a_data is string)
                return ComputeString((string)a_data);
            else if (a_data is byte[])
                return ComputeBytes((byte[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(short))
                return ComputeShorts((short[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(ushort))
                return ComputeUShorts((ushort[])a_data);
            else if (a_data is char[])
                return ComputeChars((char[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(int))
                return ComputeInts((int[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(uint))
                return ComputeUInts((uint[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(long))
                return ComputeLongs((long[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(ulong))
                return ComputeULongs((ulong[])a_data);
            else if (a_data is float[])
                return ComputeFloats((float[])a_data);
            else if (a_data is double[])
                return ComputeDoubles((double[])a_data);
            else
                throw new ArgumentException();
        }

        public virtual HashResult ComputeByte(byte a_data)
        {
            return ComputeBytes(new byte[] { a_data });
        }

        public virtual HashResult ComputeChar(char a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeShort(short a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeUShort(ushort a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeInt(int a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeUInt(uint a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeLong(long a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeULong(ulong a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeFloat(float a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeDouble(double a_data)
        {
            return ComputeBytes(BitConverter.GetBytes(a_data));
        }

        public virtual HashResult ComputeString(string a_data)
        {
            return ComputeBytes(Converters.ConvertStringToBytes(a_data));
        }

        public virtual HashResult ComputeString(string a_data, Encoding a_encoding)
        {
            return ComputeBytes(Converters.ConvertStringToBytes(a_data, a_encoding));
        }

        public virtual HashResult ComputeChars(char[] a_data)
        {
            return ComputeBytes(Converters.ConvertCharsToBytes(a_data));
        }

        public virtual HashResult ComputeShorts(short[] a_data)
        {
            return ComputeBytes(Converters.ConvertShortsToBytes(a_data));
        }

        public virtual HashResult ComputeUShorts(ushort[] a_data)
        {
            return ComputeBytes(Converters.ConvertUShortsToBytes(a_data));
        }

        public virtual HashResult ComputeInts(int[] a_data)
        {
            return ComputeBytes(Converters.ConvertIntsToBytes(a_data));
        }

        public virtual HashResult ComputeUInts(uint[] a_data)
        {
            return ComputeBytes(Converters.ConvertUIntsToBytes(a_data));
        }

        public virtual HashResult ComputeLongs(long[] a_data)
        {
            return ComputeBytes(Converters.ConvertLongsToBytes(a_data));
        }

        public virtual HashResult ComputeULongs(ulong[] a_data)
        {
            return ComputeBytes(Converters.ConvertULongsToBytes(a_data));
        }

        public virtual HashResult ComputeDoubles(double[] a_data)
        {
            return ComputeBytes(Converters.ConvertDoublesToBytes(a_data));
        }

        public virtual HashResult ComputeFloats(float[] a_data)
        {
            return ComputeBytes(Converters.ConvertFloatsToBytes(a_data));
        }

        public virtual HashResult ComputeBytes(byte[] a_data)
        {
            Initialize();
            TransformBytes(a_data);
            HashResult result = TransformFinal();
            Initialize();
            return result;
        }

        public void TransformObject(object a_data)
        {
            if (a_data is byte)
                TransformByte((byte)a_data);
            else if (a_data is short)
                TransformShort((short)a_data);
            else if (a_data is ushort)
                TransformUShort((ushort)a_data);
            else if (a_data is char)
                TransformChar((char)a_data);
            else if (a_data is int)
                TransformInt((int)a_data);
            else if (a_data is uint)
                TransformUInt((uint)a_data);
            else if (a_data is long)
                TransformLong((long)a_data);
            else if (a_data is ulong)
                TransformULong((ulong)a_data);
            else if (a_data is float)
                TransformFloat((float)a_data);
            else if (a_data is double)
                TransformDouble((double)a_data);
            else if (a_data is string)
                TransformString((string)a_data);
            else if (a_data is byte[])
                TransformBytes((byte[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(short))
                TransformShorts((short[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(ushort))
                TransformUShorts((ushort[])a_data);
            else if (a_data is char[])
                TransformChars((char[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(int))
                TransformInts((int[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(uint))
                TransformUInts((uint[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(long))
                TransformLongs((long[])a_data);
            else if (a_data.GetType().IsArray && a_data.GetType().GetElementType() == typeof(ulong))
                TransformULongs((ulong[])a_data);
            else if (a_data is float[])
                TransformFloats((float[])a_data);
            else if (a_data is double[])
                TransformDoubles((double[])a_data);
            else
                throw new ArgumentException();
        }

        public void TransformByte(byte a_data)
        {
            TransformBytes(new byte[] { a_data });
        }

        public void TransformChar(char a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformShort(short a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformUShort(ushort a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformInt(int a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformUInt(uint a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformLong(long a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformULong(ulong a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformFloat(float a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformDouble(double a_data)
        {
            TransformBytes(BitConverter.GetBytes(a_data));
        }

        public void TransformChars(char[] a_data)
        {
            TransformBytes(Converters.ConvertCharsToBytes(a_data));
        }

        public void TransformString(string a_data)
        {
            TransformBytes(Converters.ConvertStringToBytes(a_data));
        }

        public void TransformString(string a_data, Encoding a_encoding)
        {
            TransformBytes(Converters.ConvertStringToBytes(a_data, a_encoding));
        }

        public void TransformShorts(short[] a_data)
        {
            TransformBytes(Converters.ConvertShortsToBytes(a_data));
        }

        public void TransformUShorts(ushort[] a_data)
        {
            TransformBytes(Converters.ConvertUShortsToBytes(a_data));
        }

        public void TransformInts(int[] a_data)
        {
            TransformBytes(Converters.ConvertIntsToBytes(a_data));
        }

        public void TransformUInts(uint[] a_data)
        {
            TransformBytes(Converters.ConvertUIntsToBytes(a_data));
        }

        public void TransformLongs(long[] a_data)
        {
            TransformBytes(Converters.ConvertLongsToBytes(a_data));
        }

        public void TransformULongs(ulong[] a_data)
        {
            TransformBytes(Converters.ConvertULongsToBytes(a_data));
        }

        public void TransformDoubles(double[] a_data)
        {
            TransformBytes(Converters.ConvertDoublesToBytes(a_data));
        }

        public void TransformFloats(float[] a_data)
        {
            TransformBytes(Converters.ConvertFloatsToBytes(a_data));
        }

        public void TransformStream(Stream a_stream, long a_length = -1)
        {
            Debug.Assert((a_length == -1 || a_length > 0));

            if (a_stream.CanSeek)
            {
                if (a_length > -1)
                {
                    if (a_stream.Position + a_length > a_stream.Length)
                        throw new IndexOutOfRangeException();
                }

                if (a_stream.Position >= a_stream.Length)
                    return;
            }

            System.Collections.Concurrent.ConcurrentQueue<byte[]> queue =
                new System.Collections.Concurrent.ConcurrentQueue<byte[]>();
            System.Threading.AutoResetEvent data_ready = new System.Threading.AutoResetEvent(false);
            System.Threading.AutoResetEvent prepare_data = new System.Threading.AutoResetEvent(false);

            Task reader = Task.Factory.StartNew(() =>
            {
                long total = 0;

                for (; ; )
                {
                    byte[] data = new byte[BUFFER_SIZE];
                    int readed = a_stream.Read(data, 0, data.Length);

                    if ((a_length == -1) && (readed != BUFFER_SIZE))
                        data = data.SubArray(0, readed);
                    else if ((a_length != -1) && (total + readed >= a_length))
                        data = data.SubArray(0, (int)(a_length - total));

                    total += data.Length;

                    queue.Enqueue(data);
                    data_ready.Set();

                    if (a_length == -1)
                    {
                        if (readed != BUFFER_SIZE)
                            break;
                    }
                    else if (a_length == total)
                        break;
                    else if (readed != BUFFER_SIZE)
                        throw new EndOfStreamException();

                    prepare_data.WaitOne();
                }
            });

            Task hasher = Task.Factory.StartNew((obj) =>
            {
                IHash h = (IHash)obj;
                long total = 0;

                for (; ; )
                {
                    data_ready.WaitOne();

                    byte[] data;
                    queue.TryDequeue(out data);

                    prepare_data.Set();

                    total += data.Length;

                    if ((a_length == -1) || (total < a_length))
                    {
                        h.TransformBytes(data, 0, data.Length);
                    }
                    else
                    {
                        int readed = data.Length;
                        readed = readed - (int)(total - a_length);
                        h.TransformBytes(data, 0, data.Length);
                    }

                    if (a_length == -1)
                    {
                        if (data.Length != BUFFER_SIZE)
                            break;
                    }
                    else if (a_length == total)
                        break;
                    else if (data.Length != BUFFER_SIZE)
                        throw new EndOfStreamException();
                }
            }, this);

            reader.Wait();
            hasher.Wait();
        }

        public HashResult ComputeStream(Stream a_stream, long a_length = -1)
        {
            Initialize();
            TransformStream(a_stream, a_length);
            HashResult result = TransformFinal();
            Initialize();
            return result;
        }

        public void TransformFile(string a_file_name, long a_from = 0, long a_length = -1)
        {
#if !NOFILEIO
			Debug.Assert(new FileInfo(a_file_name).Exists);
            Debug.Assert(a_from >= 0);
            Debug.Assert((a_length == -1) || (a_length > 0));

            using (FileStream stream = new FileStream(a_file_name, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(a_from, SeekOrigin.Begin);
                TransformStream(stream, a_length);
            }

#else
			throw new NotSupportedException();
#endif
		}

		public void TransformBytes(byte[] a_data)
        {
            TransformBytes(a_data, 0, a_data.Length);
        }

        public void TransformBytes(byte[] a_data, int a_index)
        {
            Debug.Assert(a_index >= 0);

            int length = a_data.Length - a_index;

            Debug.Assert(length >= 0);

            TransformBytes(a_data, a_index, length);
        }

        public HashResult ComputeFile(string a_file_name, long a_from = 0, long a_length = -1)
        {
            Initialize();
            TransformFile(a_file_name, a_from, a_length);
            HashResult result = TransformFinal();
            Initialize();
            return result;
        }

        public abstract void Initialize();
        public abstract void TransformBytes(byte[] a_data, int a_index, int a_length);
        public abstract HashResult TransformFinal();
    }
}
