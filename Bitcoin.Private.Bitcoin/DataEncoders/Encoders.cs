using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin.DataEncoders
{
	public class DataEncoder
	{
		public static bool IsSpace(char c)
		{
			return c == ' ' || c == '\t' || c == '\n' || c == '\v' || c == '\f' || c == '\r';
		}

		public string Encode(string value)
		{
			return EncodeData(ToBytes(value));
		}

		public string EncodeData(byte[] data)
		{
			return EncodeData(data, data.Length);
		}
		public virtual string EncodeData(byte[] data, int length)
		{
			throw new NotSupportedException();
		}

		public virtual byte[] DecodeData(string encoded)
		{
			throw new NotSupportedException();
		}

		private string FromBytes(byte[] data)
		{
			return Encoding.UTF8.GetString(data);
		}

		private byte[] ToBytes(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}

		public string Decode(string encoded)
		{
			return FromBytes(DecodeData(encoded));
		}
	}
	public class Encoders
	{
		public static DataEncoder Hex
		{
			get
			{
				return new HexEncoder();
			}
		}
	
		public static DataEncoder Base58
		{
			get
			{
				return new Base58Encoder();
			}
		}
		public static DataEncoder Base58Check
		{
			get
			{
				return new Base58Encoder()
				{
					Check = true
				};
			}
		}
		public static DataEncoder Base64
		{
			get
			{
				return new Base64Encoder();
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
