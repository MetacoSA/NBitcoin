using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;
using NBitcoin.DataEncoders;
using NBitcoin.Crypto;

namespace NBitcoin.Tests
{
	public class GolombRiceFilterTest
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void GenerateTestVectorsTest()
		{
			var testLines = File.ReadAllLines("data/bip158_vectors.csv");
			foreach(var testLine in testLines.Skip(1))
			{
				var i= 0;
				var test = testLine.Split(',');
				var testBlockHeight = int.Parse(test[i++]); 
				var testBlockHash = uint256.Parse(test[i++]);
				var testBlock = Block.Parse(test[i++]);
				var testPreviousBasicHeader = uint256.Parse(test[i++]);
				var testPreviousExtHeader = uint256.Parse(test[i++]);
				var testBasicFilter = test[i++];
				var testExtFilter = test[i++] ;
				var testBasicHeader = test[i++];
				var testExtHeader = test[i++];

				var basicFilter = GolombRiceFilterBuilder.BuildBasicFilter(testBlock);
			 	Assert.Equal(testBasicFilter, basicFilter.ToString());
				Assert.Equal(testBasicHeader, basicFilter.GetHeader(testPreviousBasicHeader).ToString());

				testExtFilter = !string.IsNullOrEmpty(testExtFilter) ? testExtFilter : "00";
				var extFilter = GolombRiceFilterBuilder.BuildExtendedFilter(testBlock);
			 	Assert.Equal(testExtFilter, extFilter.ToString());
				Assert.Equal(testExtHeader, extFilter.GetHeader(testPreviousExtHeader).ToString());

				var deserializedBasicFilter = GolombRiceFilter.Parse(testBasicFilter);
				Assert.Equal(testBasicFilter, deserializedBasicFilter.ToString());

				var deserializedExtFilter = GolombRiceFilter.Parse(testExtFilter);
				Assert.Equal(testExtFilter, deserializedExtFilter.ToString());

			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanHandleDuplicatedValuesTest()
		{
			var byteArray0 = new byte[] { 1, 2, 3, 4 };
			var byteArray1 = new byte[] { 1, 2, 3, 4 };
			var byteArray2 = new byte[] { 1, 2, 3, 4 };

			var filter = new GolombRiceFilterBuilder()
				.SetKey(Hashes.Hash256(new byte[]{ 99, 99, 99, 99 }))
				.AddEntries(new [] { byteArray0, byteArray1, byteArray2 })
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray0) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray1) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray2) )
				.Build();
			Assert.Equal(1, filter.N);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BuildFilterAndMatchValues()
		{
			var names = from name in new[] { "New York", "Amsterdam", "Paris", "Buenos Aires", "La Habana" }
				select Encoding.ASCII.GetBytes(name);

			var key = Hashes.Hash256(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
			var filter = new GolombRiceFilterBuilder()
				.SetKey( key ) 
				.AddEntries(names)
				.SetP(0x10)
				.Build();

			var testKey = key.ToBytes().SafeSubarray(0, 16);
			// The filter should match all ther values that were added.
			foreach (var name in names)
			{
				Assert.True(filter.Match(name, testKey));
			}

			// The filter should NOT match any extra value.
			Assert.False(filter.Match(Encoding.ASCII.GetBytes("Porto Alegre"), testKey));
			Assert.False(filter.Match(Encoding.ASCII.GetBytes("Madrid"), testKey));

			// The filter should match because it has one element indexed: Buenos Aires.
			var otherCities = new[] { "La Paz", "Barcelona", "El Cairo", "Buenos Aires", "Asunción" };
			var otherNames = from name in otherCities select Encoding.ASCII.GetBytes(name);
			Assert.True(filter.MatchAny(otherNames, testKey));

			// The filter should NOT match because it doesn't have any element indexed.
			var otherCities2 = new[] { "La Paz", "Barcelona", "El Cairo", "Córdoba", "Asunción" };
			var otherNames2 = from name in otherCities2 select Encoding.ASCII.GetBytes(name);
			Assert.False(filter.MatchAny(otherNames2, testKey));
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
			const int maxBlockSize = 4_000_000;
			const int avgTxSize = 250;                  // Currently the average is around 1kb.
			const int txoutCountPerBlock = maxBlockSize / avgTxSize;
			const int avgTxoutPushDataSize = 20;        // P2PKH scripts has 20 bytes.
			const int walletAddressCount = 1_000;       // We estimate that our user will have 1000 addresses.

			var key = Hashes.Hash256(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });
			var testKey = key.ToBytes().SafeSubarray(0, 16);

			// Generation of data to be added into the filter
			var random = new Random();
			var sw = new Stopwatch();
				
			var blocks = new List<BlockFilter>(blockCount);
			for (var i = 0; i < blockCount; i++)
			{
				var builder = new GolombRiceFilterBuilder()
					.SetKey(key)
					.SetP(P);

				var txouts = new List<byte[]>(txoutCountPerBlock);
				for (var j = 0; j < txoutCountPerBlock; j++)
				{
					var pushDataBuffer = new byte[avgTxoutPushDataSize];
					random.NextBytes(pushDataBuffer);
					txouts.Add(pushDataBuffer);
				}

				builder.AddEntries(txouts);

				sw.Start();
				var filter = builder.Build();
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
				if (block.Filter.MatchAny(walletAddresses, testKey))
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
				if (!block.Filter.MatchAny(block.Data, testKey))
					falseNegativeCount++;
			}

			sw.Stop();

			Assert.Equal(0, falseNegativeCount);
		}
	}
}
