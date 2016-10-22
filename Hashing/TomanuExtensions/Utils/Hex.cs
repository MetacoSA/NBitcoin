using System;

namespace TomanuExtensions.Utils
{
    public static class Hex
    {
        private static byte[] m_trans_hex_to_bin = new byte[255];
        private static char[] m_trans_bin_to_hex = new[] { '0', '1', '2', '3', '4', '5', '6', '7',
                                                            '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        static Hex()
        {
            for (int i = '0'; i <= '9'; i++)
                m_trans_hex_to_bin[i] = (byte)(i - '0');
            for (int i = 'a'; i <= 'f'; i++)
                m_trans_hex_to_bin[i] = (byte)(i - 'a' + 10);
            for (int i = 'A'; i <= 'F'; i++)
                m_trans_hex_to_bin[i] = (byte)(i - 'A' + 10);
        }

        public static byte HexToByte(string a_str, bool a_added_0x = true)
        {
            int index = 0;
            if (a_added_0x)
                index += 2;

            return (byte)(
                (m_trans_hex_to_bin[a_str[index++]] << 4) |
                m_trans_hex_to_bin[a_str[index++]]);
        }

        public static ushort HexToUShort(string a_str, bool a_added_0x = true)
        {
            int index = 0;
            if (a_added_0x)
                index += 2;

            return (ushort)(
                (m_trans_hex_to_bin[a_str[index++]] << 12) |
                (m_trans_hex_to_bin[a_str[index++]] << 8) |
                (m_trans_hex_to_bin[a_str[index++]] << 4) |
                m_trans_hex_to_bin[a_str[index++]]);
        }

        public static uint HexToUInt(string a_str, bool a_added_0x = true)
        {
            int index = 0;
            if (a_added_0x)
                index += 2;

            return (uint)(
                (m_trans_hex_to_bin[a_str[index++]] << 28) |
                (m_trans_hex_to_bin[a_str[index++]] << 24) |
                (m_trans_hex_to_bin[a_str[index++]] << 20) |
                (m_trans_hex_to_bin[a_str[index++]] << 16) |
                (m_trans_hex_to_bin[a_str[index++]] << 12) |
                (m_trans_hex_to_bin[a_str[index++]] << 8) |
                (m_trans_hex_to_bin[a_str[index++]] << 4) |
                m_trans_hex_to_bin[a_str[index++]]);
        }

        public static string ByteToHex(byte a_value, bool a_add_0x = true)
        {
            if (a_add_0x)
            {
                return new String(new char[]
                {
                    '0', 'x',
                    m_trans_bin_to_hex[a_value >> 4],
                    m_trans_bin_to_hex[a_value & 0x0F]
                });
            }
            else
            {
                return new String(new char[]
                {
                    m_trans_bin_to_hex[a_value >> 4],
                    m_trans_bin_to_hex[a_value & 0x0F]
                });
            }
        }

        public static string UShortToHex(ushort a_value, bool a_add_0x = true)
        {
            if (a_add_0x)
            {
                return new String(new char[]
                {
                    '0', 'x',
                    m_trans_bin_to_hex[a_value >> 12],
                    m_trans_bin_to_hex[(a_value >> 8) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 4) & 0x0F],
                    m_trans_bin_to_hex[a_value & 0x0F]
                });
            }
            else
            {
                return new String(new char[]
                {
                    m_trans_bin_to_hex[a_value >> 12],
                    m_trans_bin_to_hex[(a_value >> 8) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 4) & 0x0F],
                    m_trans_bin_to_hex[a_value & 0x0F]
                });
            }
        }

        public static string UIntToHex(uint a_value, bool a_add_0x = true)
        {
            if (a_add_0x)
            {
                return new String(new char[]
                {
                    '0', 'x',
                    m_trans_bin_to_hex[a_value >> 28],
                    m_trans_bin_to_hex[(a_value >> 24) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 20) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 16) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 12) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 8) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 4) & 0x0F],
                    m_trans_bin_to_hex[a_value & 0x0F]
                });
            }
            else
            {
                return new String(new char[]
                {
                    m_trans_bin_to_hex[a_value >> 28],
                    m_trans_bin_to_hex[(a_value >> 24) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 20) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 16) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 12) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 8) & 0x0F],
                    m_trans_bin_to_hex[(a_value >> 4) & 0x0F],
                    m_trans_bin_to_hex[a_value & 0x0F]
                });
            }
        }
    }
}