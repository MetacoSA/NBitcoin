using System.Collections.Generic;
using System.IO;
using Xunit;

namespace NBitcoin.Tests
{
	public class BitcoinStreamTests
	{
		private static readonly byte[] TruncatedMaxSizeList = { 0xfe, 0x00, 0x00, 0x40, 0x00 };

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ListDeserializationCapsPreallocatedCapacity()
		{
			List<TxIn> list = null;
			var stream = new BitcoinStream(TruncatedMaxSizeList);

			Assert.Throws<EndOfStreamException>(() => stream.ReadWrite(ref list));

			Assert.NotNull(list);
			Assert.Equal(1024, list.Capacity);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TransactionListDeserializationCapsPreallocatedCapacity()
		{
			TxInList inputs = null;
			var inputStream = new BitcoinStream(TruncatedMaxSizeList);

			Assert.Throws<EndOfStreamException>(() => inputStream.ReadWrite(ref inputs));

			Assert.NotNull(inputs);
			Assert.Equal(1024, inputs.Capacity);

			TxOutList outputs = null;
			var outputStream = new BitcoinStream(TruncatedMaxSizeList);

			Assert.Throws<EndOfStreamException>(() => outputStream.ReadWrite(ref outputs));

			Assert.NotNull(outputs);
			Assert.Equal(1024, outputs.Capacity);
		}
	}
}
