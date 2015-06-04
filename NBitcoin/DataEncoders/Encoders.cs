using System;
using System.Linq;

namespace NBitcoin.DataEncoders
{
	public abstract class DataEncoder
	{
		public static readonly char[] SpaceCharacters = new[] { ' ', '\t', '\n', '\v', '\f', '\r' };
		public static bool IsSpace(char c)
		{
			return SpaceCharacters.Contains(c);
		}

		public string EncodeData(byte[] data)
		{
			return EncodeData(data, 0, data.Length);
		}

		public abstract string EncodeData(byte[] data, int offset, int count);

		public abstract byte[] DecodeData(string encoded);
	}

	public class Encoders
	{
		static readonly ASCIIEncoder _ASCII = new ASCIIEncoder();
		public static DataEncoder ASCII
		{
			get
			{
				return _ASCII;
			}
		}

		static readonly HexEncoder _Hex = new HexEncoder();
		public static DataEncoder Hex
		{
			get
			{
				return _Hex;
			}
		}

		static readonly Base58Encoder _Base58 = new Base58Encoder();
		public static DataEncoder Base58
		{
			get
			{
				return _Base58;
			}
		}

		static readonly Base58Encoder _Base58Check = new Base58Encoder()
				{
					Check = true
				};
		public static DataEncoder Base58Check
		{
			get
			{
				return _Base58Check;
			}
		}

		static readonly Base64Encoder _Base64 = new Base64Encoder();
		public static DataEncoder Base64
		{
			get
			{
				return _Base64;
			}
		}

		//public static DataEncoder Bin
		//{
		//	get
		//	{
		//		return null;
		//	}
		//}
		//public static DataEncoder Dec
		//{
		//	get
		//	{
		//		return null;
		//	}
		//}
		//public static DataEncoder RFC1751
		//{
		//	get
		//	{
		//		return null;
		//	}
		//}
		//public static DataEncoder Poetry
		//{
		//	get
		//	{
		//		return null;
		//	}
		//}
		//public static DataEncoder Rot13
		//{
		//	get
		//	{
		//		return null;
		//	}
		//}
		//public static DataEncoder Easy16
		//{
		//	get
		//	{
		//		return null;
		//	}
		//}
	}
}
