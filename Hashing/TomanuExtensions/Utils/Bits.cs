using System.Diagnostics;

namespace TomanuExtensions.Utils
{
    public static class Bits
    {
        public static bool IsSet(byte a_byte, int a_bit_index)
        {
            Debug.Assert(a_bit_index >= 0);
            Debug.Assert(a_bit_index <= 7);

            return (a_byte & (1 << a_bit_index)) != 0;
        }

        public static void SetBit(ref byte a_byte, int a_bit_index, bool a_bit_value)
        {
            Debug.Assert(a_bit_index >= 0);
            Debug.Assert(a_bit_index <= 7);

            if (a_bit_value)
                a_byte = (byte)(a_byte | (1 << a_bit_index));
            else
                a_byte = (byte)(a_byte & ~(1 << a_bit_index));
        }

        public static bool IsSet(ushort a_ushort, int a_bitIndex)
        {
            Debug.Assert(a_bitIndex >= 0);
            Debug.Assert(a_bitIndex <= 15);

            return (a_ushort & (1 << a_bitIndex)) != 0;
        }

        public static void SetBit(ref ushort a_ushort, int a_bit_index, bool a_bit_value)
        {
            Debug.Assert(a_bit_index >= 0);
            Debug.Assert(a_bit_index <= 15);

            if (a_bit_value)
                a_ushort = (ushort)(a_ushort | (1 << a_bit_index));
            else
                a_ushort = (ushort)(a_ushort & ~(1 << a_bit_index));
        }

        public static bool IsSet(uint a_uint, int a_bit_index)
        {
            Debug.Assert(a_bit_index >= 0);
            Debug.Assert(a_bit_index <= 31);

            return (a_uint & (1 << a_bit_index)) != 0;
        }

        public static void SetBit(ref uint a_uint, int a_bit_index, bool a_bit_value)
        {
            Debug.Assert(a_bit_index >= 0);
            Debug.Assert(a_bit_index <= 31);

            if (a_bit_value)
                a_uint = a_uint | (1U << a_bit_index);
            else
                a_uint = a_uint & ~(1U << a_bit_index);
        }

        static public uint RotateLeft(uint a_uint, int a_n)
        {
            Debug.Assert(a_n >= 0);

            return (uint)((a_uint << a_n) | (a_uint >> (32 - a_n)));
        }

        static public ulong RotateLeft(ulong a_ulong, int a_n)
        {
            Debug.Assert(a_n >= 0);

            return (ulong)((a_ulong << a_n) | (a_ulong >> (64 - a_n)));
        }

        static public uint RotateRight(uint a_uint, int a_n)
        {
            Debug.Assert(a_n >= 0);

            return (uint)((a_uint >> a_n) | (a_uint << (32 - a_n)));
        }

        static public ulong RotateRight(ulong a_ulong, int a_n)
        {
            Debug.Assert(a_n >= 0);

            return (ulong)((a_ulong >> a_n) | (a_ulong << (64 - a_n)));
        }
    }
}