using System;

namespace NBitcoin.DataEncoders
{
	public abstract class DataEncoder
	{
		// char.IsWhiteSpace fits well but it match other whitespaces
		// characters too and also works for unicode characters.
		public static bool IsSpace(char c)
		{
			switch (c)
			{
				case ' ':
				case '\t':
				case '\n':
				case '\v':
				case '\f':
				case '\r':
					return true;
			}
			return false;
		}

		internal DataEncoder()
		{
		}

		public string EncodeData(byte[] data)
		{
			return EncodeData(data, 0, data.Length);
		}

		public abstract string EncodeData(byte[] data, int offset, int count);

		public abstract byte[] DecodeData(string encoded);
	}

	public static class Encoders
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

		static readonly Base32Encoder _Base32 = new Base32Encoder();
		public static DataEncoder Base32
		{
			get
			{
				return _Base32;
			}
		}

		private static readonly Base58CheckEncoder _Base58Check = new Base58CheckEncoder();
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

		public static Bech32Encoder Bech32(string hrp)
		{
			return new Bech32Encoder(hrp);
		}
		public static Bech32Encoder Bech32(byte[] hrp)
		{
			return new Bech32Encoder(hrp);
		}

	}
}
