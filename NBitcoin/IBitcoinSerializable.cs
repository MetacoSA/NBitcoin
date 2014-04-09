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
		public static void FromBytes(this IBitcoinSerializable serializable,byte[] bytes)
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
