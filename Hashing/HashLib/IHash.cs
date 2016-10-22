using System;
using System.Text;
using System.IO;

namespace HashLib
{
    public interface IHash
    {
        string Name { get; }
        int BlockSize { get; }
        int HashSize { get; }

        HashResult ComputeObject(object a_data);
        HashResult ComputeByte(byte a_data);
        HashResult ComputeChar(char a_data);
        HashResult ComputeShort(short a_data);
        HashResult ComputeUShort(ushort a_data);
        HashResult ComputeInt(int a_data);
        HashResult ComputeUInt(uint a_data);
        HashResult ComputeLong(long a_data);
        HashResult ComputeULong(ulong a_data);
        HashResult ComputeFloat(float a_data);
        HashResult ComputeDouble(double a_data);
        HashResult ComputeString(string a_data);
        HashResult ComputeString(string a_data, Encoding a_encoding);
        HashResult ComputeBytes(byte[] a_data);
        HashResult ComputeChars(char[] a_data);
        HashResult ComputeShorts(short[] a_data);
        HashResult ComputeUShorts(ushort[] a_data);
        HashResult ComputeInts(int[] a_data);
        HashResult ComputeUInts(uint[] a_data);
        HashResult ComputeLongs(long[] a_data);
        HashResult ComputeULongs(ulong[] a_data);
        HashResult ComputeDoubles(double[] a_data);
        HashResult ComputeFloats(float[] a_data);
        HashResult ComputeStream(Stream a_stream, long a_length = -1);
        HashResult ComputeFile(string a_file_name, long a_from = 0, long a_length = -1);

        void Initialize();

        void TransformBytes(byte[] a_data);
        void TransformBytes(byte[] a_data, int a_index);
        void TransformBytes(byte[] a_data, int a_index, int a_length);

        HashResult TransformFinal();

        void TransformObject(object a_data);
        void TransformByte(byte a_data);
        void TransformChar(char a_data);
        void TransformShort(short a_data);
        void TransformUShort(ushort a_data);
        void TransformInt(int a_data);
        void TransformUInt(uint a_data);
        void TransformLong(long a_data);
        void TransformULong(ulong a_data);
        void TransformFloat(float a_data);
        void TransformDouble(double a_data);
        void TransformString(string a_data);
        void TransformString(string a_data, Encoding a_encoding);
        void TransformChars(char[] a_data);
        void TransformShorts(short[] a_data);
        void TransformUShorts(ushort[] a_data);
        void TransformInts(int[] a_data);
        void TransformUInts(uint[] a_data);
        void TransformLongs(long[] a_data);
        void TransformULongs(ulong[] a_data);
        void TransformDoubles(double[] a_data);
        void TransformFloats(float[] a_data);
        void TransformStream(Stream a_stream, long a_length = -1);
        void TransformFile(string a_file_name, long a_from = 0, long a_length = -1);
    }
}
