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
		public void ListDeserializationDoesNotPreallocateDeclaredCount()
		{
			List<TxIn> list = null;
			var stream = new BitcoinStream(TruncatedMaxSizeList);

			Assert.Throws<EndOfStreamException>(() => stream.ReadWrite(ref list));

			Assert.NotNull(list);
			Assert.Equal(0, list.Capacity);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TransactionListDeserializationDoesNotPreallocateDeclaredCount()
		{
			TxInList inputs = null;
			var inputStream = new BitcoinStream(TruncatedMaxSizeList);

			Assert.Throws<EndOfStreamException>(() => inputStream.ReadWrite(ref inputs));

			Assert.NotNull(inputs);
			Assert.Equal(0, inputs.Capacity);

			TxOutList outputs = null;
			var outputStream = new BitcoinStream(TruncatedMaxSizeList);

			Assert.Throws<EndOfStreamException>(() => outputStream.ReadWrite(ref outputs));

			Assert.NotNull(outputs);
			Assert.Equal(0, outputs.Capacity);
		}
	}
}
