using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace NBitcoin.Tests
{
	public class GolombRiceFilterTest
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BuildFilterAndMatchValues()
		{
			var names = from name in new[] { "New York", "Amsterdam", "Paris", "Buenos Aires", "La Habana" }
				select Encoding.ASCII.GetBytes(name);

			var key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
			var filter = GolombRiceFilter.Build(key, names, 0x10);

			// The filter should match all ther values that were added.
			foreach (var name in names)
			{
				Assert.True(filter.Match(name, key));
			}

			// The filter should NOT match any extra value.
			Assert.False(filter.Match(Encoding.ASCII.GetBytes("Porto Alegre"), key));
			Assert.False(filter.Match(Encoding.ASCII.GetBytes("Madrid"), key));

			// The filter should match because it has one element indexed: Buenos Aires.
			var otherCities = new[] { "La Paz", "Barcelona", "El Cairo", "Buenos Aires", "Asunción" };
			var otherNames = from name in otherCities select Encoding.ASCII.GetBytes(name);
			Assert.True(filter.MatchAny(otherNames, key));

			// The filter should NOT match because it doesn't have any element indexed.
			var otherCities2 = new[] { "La Paz", "Barcelona", "El Cairo", "Córdoba", "Asunción" };
			var otherNames2 = from name in otherCities2 select Encoding.ASCII.GetBytes(name);
			Assert.False(filter.MatchAny(otherNames2, key));
		}

		class BlockFilter
		{
			public GolombRiceFilter Filter { get; }
			public List<byte[]> Data { get; }

			public BlockFilter(GolombRiceFilter filter, List<byte[]> data)
			{
				Filter = filter;
				Data = data;
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void FalsePositivesTest()
		{
			// Given this library can be used for building and query filters for each block of 
			// the bitcoin's blockchain, we must be sure it performs well, specially in the queries.

			// Considering a 4MB block (overestimated) with an average transaction size of 250 bytes (underestimated)
			// gives us 16000 transactions (this is about 27 tx/sec). Assuming 2.5 txouts per tx we have 83885 txouts 
			// per block.
			const byte P = 20;
			const int blockCount = 100;
			const int maxBlockSize = 4 * 1000 * 1000;
			const int avgTxSize = 250;                  // Currently the average is around 1kb.
			const int txoutCountPerBlock = maxBlockSize / avgTxSize;
			const int avgTxoutPushDataSize = 20;        // P2PKH scripts has 20 bytes.
			const int walletAddressCount = 1000;        // We estimate that our user will have 1000 addresses.

			var key = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

			// Generation of data to be added into the filter
			var random = new Random();
			var sw = new Stopwatch();

			var blocks = new List<BlockFilter>(blockCount);
			for (var i = 0; i < blockCount; i++)
			{
				var txouts = new List<byte[]>(txoutCountPerBlock);
				for (var j = 0; j < txoutCountPerBlock; j++)
				{
					var pushDataBuffer = new byte[avgTxoutPushDataSize];
					random.NextBytes(pushDataBuffer);
					txouts.Add(pushDataBuffer);
				}

				sw.Start();
				var filter = GolombRiceFilter.Build(key, txouts, P);
				sw.Stop();

				blocks.Add(new BlockFilter(filter, txouts));
			}
			sw.Reset();


			var walletAddresses = new List<byte[]>(walletAddressCount);
			var falsePositiveCount = 0;
			for (var i = 0; i < walletAddressCount; i++)
			{
				var walletAddress = new byte[avgTxoutPushDataSize];
				random.NextBytes(walletAddress);
				walletAddresses.Add(walletAddress);
			}

			sw.Start();
			// Check that the filter can match every single txout in every block.
			foreach (var block in blocks)
			{
				if (block.Filter.MatchAny(walletAddresses, key))
					falsePositiveCount++;
			}

			sw.Stop();
			Assert.True(falsePositiveCount < 5);

			// Filter has to mat existing values
			sw.Start();
			var falseNegativeCount = 0;
			// Check that the filter can match every single txout in every block.
			foreach (var block in blocks)
			{
				if (!block.Filter.MatchAny(block.Data, key))
					falseNegativeCount++;
			}

			sw.Stop();

			Assert.Equal(0, falseNegativeCount);
		}
	}

	public class FastBitArrayTest
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void GetBitsTest()
		{
			// 1 1 1 0 1 0 1 1 - 1 0 1 0 1 0 1 1 - 1 0 1 0 1 1 1 0 - 1 0 1 0 1 1 1 0 
			// 1 0 1 1 1 0 1 0 
			var barr = new FastBitArray();
			barr.Length = 50;
			for (var i = 0; i < 40; i++)
			{
				if (i % 7 == 0)
				{
					barr[i] = true;
					i++;
					barr[i] = true;
				}
				else
				{
					barr[i] = i % 2 == 0;
				}
			}

			// Get bits in the same int.
			Assert.Equal((ulong)0b111, barr.GetBits(0, 3));
			Assert.Equal((ulong)0b10111, barr.GetBits(0, 5));
			Assert.Equal((ulong)0b01010111010, barr.GetBits(3, 11));

			// Get bits in cross int
			Assert.Equal((ulong)0b101110101110101, barr.GetBits(24, 16));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SetRandomBitsTest()
		{
			var barr = new FastBitArray(new byte[0]);
			barr.Length = 150;
			var values = new List<int>();
			var lengths = new List<int>();
			var rnd = new Random();
			var pos = 0;

			for (int i = 0; i < 10; i++)
			{
				var val = rnd.Next();
				var len = rnd.Next(1, 20);
				barr.SetBits(pos, (ulong)val, len);

				values.Add(val);
				lengths.Add(len);
				pos += len;
			}

			pos = 0;
			for (int i = 0; i < 10; i++)
			{
				var len = lengths[i];
				var expectedValue = values[i];
				var value = barr.GetBits(pos, len);
				Assert.Equal(((ulong)expectedValue & value), value);
				pos += len;
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SetBitAndGetBitsTest()
		{
			var barr = new FastBitArray(new byte[0]);
			barr.Length = 150;

			var j = true;
			for (int i = 0; i < 64; i += 2)
			{
				if (j)
				{
					barr.SetBit(i, true);
					barr.SetBit(i + 1, true);
				}
				else
				{
					barr.SetBit(i, false);
					barr.SetBit(i + 1, false);
				}

				j = !j;
			}

			for (var i = 0; i < 16; i++)
			{
				Assert.Equal(0b11UL, barr.GetBits(i * 4, 2));
				Assert.Equal(0b00UL, barr.GetBits((i * 4) + 2, 2));
			}

			for (var i = 0; i < 8; i++)
			{
				Assert.Equal(0b0011UL, barr.GetBits(i * 8, 4));
				Assert.Equal(0b0011UL, barr.GetBits((i * 8) + 4, 2));
			}

			Assert.Equal(0b11001UL, barr.GetBits(29, 5));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void SetBitsBigEndianTest()
		{
			var barr = new FastBitArray(new byte[0]);
			barr.Length = 5;
			barr.SetBits(0, 14, 4);
			var val = barr.GetBits(0, 4);

			barr = new FastBitArray(new byte[0]);
			barr.Length = 5;
			barr.SetBit(0, false);
			barr.SetBit(1, true);
			barr.SetBit(2, true);
			barr.SetBit(3, true);
			val = barr.GetBits(0, 4);
		}
	}
}
