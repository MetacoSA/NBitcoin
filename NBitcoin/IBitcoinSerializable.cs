using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IBitcoinSerializable
	{
		void ReadWrite(BitcoinStream stream);
	}

	public static class BitcoinSerializableExtensions
	{
		public static void ReadWrite(this IBitcoinSerializable serializable, Stream stream, bool serializing)
		{
			BitcoinStream s = new BitcoinStream(stream, serializing);
			serializable.ReadWrite(s);
		}
		public static int GetSerializedSize(this IBitcoinSerializable serializable)
		{
			return serializable.ToBytes().Length;
		}
		public static void ReadWrite(this IBitcoinSerializable serializable, byte[] bytes)
		{
			ReadWrite(serializable, new MemoryStream(bytes), false);
		}
		public static void FromBytes(this IBitcoinSerializable serializable, byte[] bytes)
		{
			serializable.ReadWrite(new BitcoinStream(bytes));
		}
		public static byte[] ToBytes(this IBitcoinSerializable serializable)
		{
			MemoryStream ms = new MemoryStream();
			serializable.ReadWrite(new BitcoinStream(ms, true));
			return ms.ToArray();
		}
	}
}
