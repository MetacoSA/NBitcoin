using System;
using System.Diagnostics;
using System.Text;

namespace NBitcoin.Altcoins.HashX11
{
    internal static class Converters
    {
        public static byte[] ConvertToBytes(object a_in)
        {
            if (a_in is byte)
                return new byte[] { (byte)a_in };
            else if (a_in is short)
                return BitConverter.GetBytes((short)a_in);
            else if (a_in is ushort)
                return BitConverter.GetBytes((ushort)a_in);
            else if (a_in is char)
                return BitConverter.GetBytes((char)a_in);
            else if (a_in is int)
                return BitConverter.GetBytes((int)a_in);
            else if (a_in is uint)
                return BitConverter.GetBytes((uint)a_in);
            else if (a_in is long)
                return BitConverter.GetBytes((long)a_in);
            else if (a_in is ulong)
                return BitConverter.GetBytes((ulong)a_in);
            else if (a_in is float)
                return BitConverter.GetBytes((float)a_in);
            else if (a_in is double)
                return BitConverter.GetBytes((double)a_in);
            else if (a_in is string)
                return ConvertStringToBytes((string)a_in);
            else if (a_in is byte[])
                return (byte[])((byte[])a_in).Clone();
            else if (a_in.GetType().IsArray && a_in.GetType().GetElementType() == typeof(short))
                return ConvertShortsToBytes((short[])a_in);
            else if (a_in.GetType().IsArray && a_in.GetType().GetElementType() == typeof(ushort))
                return ConvertUShortsToBytes((ushort[])a_in);
            else if (a_in is char[])
                return ConvertCharsToBytes((char[])a_in);
            else if (a_in.GetType().IsArray && a_in.GetType().GetElementType() == typeof(int))
                return ConvertIntsToBytes((int[])a_in);
            else if (a_in.GetType().IsArray && a_in.GetType().GetElementType() == typeof(uint))
                return ConvertUIntsToBytes((uint[])a_in);
            else if (a_in.GetType().IsArray && a_in.GetType().GetElementType() == typeof(long))
                return ConvertLongsToBytes((long[])a_in);
            else if (a_in.GetType().IsArray && a_in.GetType().GetElementType() == typeof(ulong))
                return ConvertULongsToBytes((ulong[])a_in);
            else if (a_in is float[])
                return ConvertFloatsToBytes((float[])a_in);
            else if (a_in is double[])
                return ConvertDoublesToBytes((double[])a_in);
            else
                throw new ArgumentException();
        }

        public static uint[] ConvertBytesToUInts(byte[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 1, 4, a_index, a_length);

            uint[] result = new uint[a_length / 4];
            ConvertBytesToUInts(a_in, a_index, a_length, result);
            return result;
        }

        public static void ConvertBytesToUInts(byte[] a_in, int a_index, int a_length, uint[] a_out)
        {
            Check(a_in, 1, 4, a_index, a_length);

            Buffer.BlockCopy(a_in, a_index, a_out, 0, a_length);
        }

        public static ulong[] ConvertBytesToULongs(byte[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 1, 8, a_index, a_length);

            ulong[] result = new ulong[a_length / 8];
            ConvertBytesToULongs(a_in, a_index, a_length, result, 0);
            return result;
        }

        public static void ConvertBytesToULongs(byte[] a_in, int a_index_in, int a_length, ulong[] a_out, int a_index_out)
        {
            Check(a_in, 1, a_out, 8, a_index_in, a_length, a_index_out);

            Buffer.BlockCopy(a_in, a_index_in, a_out, a_index_out * 8, a_length);
        }

        public static uint[] ConvertBytesToUIntsSwapOrder(byte[] a_in, int a_index, int a_length)
        {
            Check(a_in, 1, 4, a_index, a_length);

            uint[] result = new uint[a_length / 4];
            ConvertBytesToUIntsSwapOrder(a_in, a_index, a_length, result, 0);
            return result;
        }

        public static void ConvertBytesToUIntsSwapOrder(byte[] a_in, int a_index, int a_length, uint[] a_result, int a_index_out)
        {
            Check(a_in, 1, a_result, 4, a_index, a_length, a_index_out);

            for (int i = a_index_out; a_length > 0; a_length -= 4)
            {
                a_result[i++] =
                    ((uint)a_in[a_index++] << 24) |
                    ((uint)a_in[a_index++] << 16) |
                    ((uint)a_in[a_index++] << 8) |
                    a_in[a_index++];
            }
        }

        public static ulong ConvertBytesToULongSwapOrder(byte[] a_in, int a_index)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_index + 8 <= a_in.Length);

            return ((ulong)a_in[a_index++] << 56) |
                   ((ulong)a_in[a_index++] << 48) |
                   ((ulong)a_in[a_index++] << 40) |
                   ((ulong)a_in[a_index++] << 32) |
                   ((ulong)a_in[a_index++] << 24) |
                   ((ulong)a_in[a_index++] << 16) |
                   ((ulong)a_in[a_index++] << 8) |
                   a_in[a_index];
        }

        public static ulong ConvertBytesToULong(byte[] a_in, int a_index)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_index + 8 <= a_in.Length);

            return BitConverter.ToUInt64(a_in, a_index);
        }

        public static uint ConvertBytesToUIntSwapOrder(byte[] a_in, int a_index)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_index + 4 <= a_in.Length);

            return ((uint)a_in[a_index++] << 24) |
                   ((uint)a_in[a_index++] << 16) |
                   ((uint)a_in[a_index++] << 8) |
                   a_in[a_index];
        }

        public static uint ConvertBytesToUInt(byte[] a_in, int a_index = 0)
        {
            Debug.Assert(a_index >= 0);
            Debug.Assert(a_index + 4 <= a_in.Length);

            return a_in[a_index++] |
                   ((uint)a_in[a_index++] << 8) |
                   ((uint)a_in[a_index++] << 16) |
                   ((uint)a_in[a_index] << 24);
        }

        public static ulong[] ConvertBytesToULongsSwapOrder(byte[] a_in, int a_index, int a_length)
        {
            Check(a_in, 1, 8, a_index, a_length);

            ulong[] result = new ulong[a_length / 8];
            ConvertBytesToULongsSwapOrder(a_in, a_index, a_length, result);
            return result;
        }

        public static void ConvertBytesToULongsSwapOrder(byte[] a_in, int a_index, int a_length, ulong[] a_out)
        {
            Check(a_in, 1, 8, a_index, a_length);

            for (int i = 0; a_length > 0; a_length -= 8)
            {
                a_out[i++] =
                    (((ulong)a_in[a_index++] << 56) |
                    ((ulong)a_in[a_index++] << 48) |
                    ((ulong)a_in[a_index++] << 40) |
                    ((ulong)a_in[a_index++] << 32) |
                    ((ulong)a_in[a_index++] << 24) |
                    ((ulong)a_in[a_index++] << 16) |
                    ((ulong)a_in[a_index++] << 8) |
					a_in[a_index++]);
            }
        }

        public static byte[] ConvertUIntsToBytesSwapOrder(uint[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 4, 1, a_index, a_length);

            byte[] result = new byte[a_length * 4];

            for (int j = 0; a_length > 0; a_length--, a_index++)
            {
                result[j++] = (byte)(a_in[a_index] >> 24);
                result[j++] = (byte)(a_in[a_index] >> 16);
                result[j++] = (byte)(a_in[a_index] >> 8);
                result[j++] = (byte)a_in[a_index];
            }

            return result;
        }

        public static byte[] ConvertUIntsToBytes(uint[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 4, 1, a_index, a_length);

            byte[] result = new byte[a_length * 4];
            Buffer.BlockCopy(a_in, a_index * 4, result, 0, a_length * 4);
            return result;
        }


        public static byte[] ConvertULongsToBytes(ulong[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 8, 1, a_index, a_length);

            byte[] result = new byte[a_length * 8];
            Buffer.BlockCopy(a_in, a_index * 8, result, 0, a_length * 8);
            return result;
        }

        public static byte[] ConvertULongsToBytesSwapOrder(ulong[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 8, 1, a_index, a_length);

            byte[] result = new byte[a_length * 8];

            for (int j = 0; a_length > 0; a_length--, a_index++)
            {
                result[j++] = (byte)(a_in[a_index] >> 56);
                result[j++] = (byte)(a_in[a_index] >> 48);
                result[j++] = (byte)(a_in[a_index] >> 40);
                result[j++] = (byte)(a_in[a_index] >> 32);
                result[j++] = (byte)(a_in[a_index] >> 24);
                result[j++] = (byte)(a_in[a_index] >> 16);
                result[j++] = (byte)(a_in[a_index] >> 8);
                result[j++] = (byte)a_in[a_index];
            }

            return result;
        }

        public static byte[] ConvertUIntToBytes(uint a_in)
        {
            byte[] result = new byte[4];
            ConvertUIntToBytes(a_in, result, 0);
            return result;
        }

        public static void ConvertUIntToBytes(ulong a_in, byte[] a_out, int a_index)
        {
            Debug.Assert(a_index + 4 <= a_out.Length);

            Array.Copy(BitConverter.GetBytes(a_in), 0, a_out, a_index, 4);
        }

        public static byte[] ConvertULongToBytes(ulong a_in)
        {
            byte[] result = new byte[8];
            ConvertULongToBytes(a_in, result, 0);
            return result;
        }

        public static void ConvertULongToBytes(ulong a_in, byte[] a_out, int a_index)
        {
            Debug.Assert(a_index + 8 <= a_out.Length);

            Array.Copy(BitConverter.GetBytes(a_in), 0, a_out, a_index, 8);
        }

        public static void ConvertULongToBytesSwapOrder(ulong a_in, byte[] a_out, int a_index)
        {
            Debug.Assert(a_index + 8 <= a_out.Length);

            a_out[a_index++] = (byte)(a_in >> 56);
            a_out[a_index++] = (byte)(a_in >> 48);
            a_out[a_index++] = (byte)(a_in >> 40);
            a_out[a_index++] = (byte)(a_in >> 32);
            a_out[a_index++] = (byte)(a_in >> 24);
            a_out[a_index++] = (byte)(a_in >> 16);
            a_out[a_index++] = (byte)(a_in >> 8);
            a_out[a_index++] = (byte)a_in;
        }

        public static byte[] ConvertStringToBytes(string a_in)
        {
            byte[] bytes = new byte[a_in.Length * sizeof(char)];
            System.Buffer.BlockCopy(a_in.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static byte[] ConvertStringToBytes(string a_in, Encoding a_encoding)
        {
            return a_encoding.GetBytes(a_in);
        }

        public static byte[] ConvertCharsToBytes(char[] a_in)
        {
            Check(a_in, 2, 1);

            byte[] bytes = new byte[a_in.Length * sizeof(char)];
            System.Buffer.BlockCopy(a_in, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static byte[] ConvertShortsToBytes(short[] a_in)
        {
            Check(a_in, 2, 1);

            byte[] result = new byte[a_in.Length * 2];
            Buffer.BlockCopy(a_in, 0, result, 0, result.Length);
            return result;
        }

        public static byte[] ConvertUShortsToBytes(ushort[] a_in)
        {
            Check(a_in, 2, 1);

            byte[] result = new byte[a_in.Length * 2];
            Buffer.BlockCopy(a_in, 0, result, 0, result.Length);
            return result;
        }

        public static byte[] ConvertIntsToBytes(int[] a_in)
        {
            Check(a_in, 4, 1);

            byte[] result = new byte[a_in.Length * 4];
            Buffer.BlockCopy(a_in, 0, result, 0, a_in.Length * 4);
            return result;
        }

        public static byte[] ConvertLongsToBytes(long[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 8, 1, a_index, a_length);

            byte[] result = new byte[a_length * 8];
            Buffer.BlockCopy(a_in, a_index * 8, result, 0, a_length * 8);
            return result;
        }

        public static byte[] ConvertDoublesToBytes(double[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 8, 1, a_index, a_length);

            byte[] result = new byte[a_length * 8];
            Buffer.BlockCopy(a_in, a_index * 8, result, 0, a_length * 8);
            return result;
        }

        public static byte[] ConvertFloatsToBytes(float[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, 4, 1, a_index, a_length);

            byte[] result = new byte[a_length * 4];
            Buffer.BlockCopy(a_in, a_index * 4, result, 0, a_length * 4);
            return result;
        }

        public static ulong[] ConvertFloatsToULongs(float[] a_in, int a_index = 0, int a_length = -1)
        {
            if (a_length == -1)
                a_length = a_in.Length;

            Check(a_in, sizeof(float), sizeof(ulong), a_index, a_length);

            ulong[] result = new ulong[a_length / (sizeof(ulong) / sizeof(float))];
            ConvertFloatsToULongs(a_in, a_index, a_length, result, 0);
            return result;
        }

        public static void ConvertFloatsToULongs(float[] a_in, int a_index_in, int a_length, ulong[] a_out, int a_index_out)
        {
            Check(a_in, sizeof(float), a_out, sizeof(ulong), a_index_in, a_length, a_index_out);

            Buffer.BlockCopy(a_in, a_index_in, a_out, a_index_out * sizeof(ulong), a_length * sizeof(float));
        }

        public static byte[] ConvertHexStringToBytes(string a_in)
        {
            a_in = a_in.Replace("-", "");

            Debug.Assert(a_in.Length % 2 == 0);

            byte[] result = new byte[a_in.Length / 2];

            for (int i = 0; i < result.Length; i++)
                result[i] = Byte.Parse(a_in.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);

            return result;
        }

        public static string ConvertBytesToHexString(byte[] a_in, bool a_group = true)
        {
            string hex = BitConverter.ToString(a_in).ToUpper();

            if (a_group)
            {
                Check(a_in, 1, 4);

                string[] ar = BitConverter.ToString(a_in).ToUpper().Split(new char[] { '-' });

                hex = "";

                for (int i = 0; i < ar.Length / 4; i++)
                {
                    if (i != 0)
                        hex += "-";
                    hex += ar[i * 4] + ar[i * 4 + 1] + ar[i * 4 + 2] + ar[i * 4 + 3];
                }
            }
            else
                hex = hex.Replace("-", "");

            return hex;
        }

        [Conditional("DEBUG")]
        private static void Check<I>(I[] a_in, int a_in_size, int a_out_size)
        {
            Debug.Assert((a_in.Length * a_in_size % a_out_size) == 0);
        }

        [Conditional("DEBUG")]
        private static void Check<I>(I[] a_in, int a_in_size, int a_out_size, int a_index, int a_length)
        {
            Debug.Assert((a_length * a_in_size % a_out_size) == 0);

            if (a_out_size > a_in_size)
                Debug.Assert((a_length % (a_out_size / a_in_size)) == 0);
            else
                Debug.Assert(a_in_size % a_out_size == 0);

            Debug.Assert(a_index >= 0);

            if (a_length > 0)
                Debug.Assert(a_index < a_in.Length);

            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index + a_length <= a_in.Length);
            Debug.Assert(a_index + a_length <= a_in.Length);
        }

        [Conditional("DEBUG")]
        private static void Check<I, O>(I[] a_in, int a_in_size, O[] a_result, int a_out_size, int a_index_in, int a_length, 
            int a_index_out)
        {
            Debug.Assert((a_length * a_in_size % a_out_size) == 0);

            if (a_out_size > a_in_size)
                Debug.Assert((a_length % (a_out_size / a_in_size)) == 0);

            Debug.Assert(a_index_in >= 0);

            if (a_length > 0)
                Debug.Assert(a_index_in < a_in.Length);

            Debug.Assert(a_length >= 0);
            Debug.Assert(a_index_in + a_length <= a_in.Length);
            Debug.Assert(a_index_in + a_length <= a_in.Length);

            Debug.Assert(a_index_out + a_result.Length >= (a_length / a_out_size));
        }
    }
}