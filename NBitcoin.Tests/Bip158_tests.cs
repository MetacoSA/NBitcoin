﻿using System;
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
			var tests = TestCase.read_json("data/bip158_vectors.json");

			foreach(var test in tests.Skip(1))
			{
				var i= 0;
				var testBlockHeight = test[i++]; 
				var testBlockHash = uint256.Parse((string)test[i++]);
				var testBlock = Block.Parse((string)test[i++]);
				var testPreviousBasicHeader = uint256.Parse((string)test[i++]);
				var testBasicFilter = (string)test[i++];
				var testBasicHeader = (string)test[i++];
				var message = (string)test[i++];

				var basicFilter = GolombRiceFilterBuilder.BuildBasicFilter(testBlock);
			 	Assert.Equal(testBasicFilter, basicFilter.ToString());
				Assert.Equal(testBasicHeader, basicFilter.GetHeader(testPreviousBasicHeader).ToString());

				var deserializedBasicFilter = GolombRiceFilter.Parse(testBasicFilter);
				Assert.Equal(testBasicFilter, deserializedBasicFilter.ToString());
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

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanHandleCustomPandMValuesTest()
		{
			var byteArray0 = new byte[] { 1, 2, 3, 4 };
			var byteArray1 = new byte[] { 2, 3, 4 };
			var byteArray2 = new byte[] { 3, 4 };

			var key = Hashes.Hash256(new byte[]{ 99, 99, 99, 99 });
			var testKey = key.ToBytes().SafeSubarray(0, 16);

			var filter = new GolombRiceFilterBuilder()
				.SetKey(key)
				.SetP(10)
				.SetM(1U<<10)
				.AddEntries(new [] { byteArray0, byteArray1, byteArray2 })
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray0) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray1) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray2) )
				.Build();
			var filterSize10_10 = filter.ToBytes().Length;

			Assert.Equal(3, filter.N);
			Assert.Equal(10, filter.P);
			Assert.Equal(1U<<10, filter.M);
			Assert.True(filter.Match(byteArray0, testKey));
			Assert.True(filter.Match(byteArray1, testKey));
			Assert.True(filter.Match(byteArray2, testKey));
			Assert.False(filter.Match(new byte[]{ 6, 7, 8}, testKey));

			filter = new GolombRiceFilterBuilder()
				.SetKey(key)
				.SetP(10)
				.SetM(1U<<4)
				.AddEntries(new [] { byteArray0, byteArray1, byteArray2 })
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray0) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray1) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray2) )
				.Build();
			var filterSize10_4 = filter.ToBytes().Length;

			Assert.Equal(3, filter.N);
			Assert.Equal(10, filter.P);
			Assert.Equal(1U<<4, filter.M);
			Assert.True(filter.Match(byteArray0, testKey));
			Assert.True(filter.Match(byteArray1, testKey));
			Assert.True(filter.Match(byteArray2, testKey));
			Assert.False(filter.Match(new byte[]{ 6, 7, 8}, testKey));
			Assert.Equal(filterSize10_4, filterSize10_10);

			filter = new GolombRiceFilterBuilder()
				.SetKey(key)
				.SetP(8)
				.SetM(1U<<4)
				.AddEntries(new [] { byteArray0, byteArray1, byteArray2 })
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray0) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray1) )
				.AddScriptPubkey( Script.FromBytesUnsafe(byteArray2) )
				.Build();
			var filterSize8_4 = filter.ToBytes().Length;

			Assert.Equal(3, filter.N);
			Assert.Equal(8, filter.P);
			Assert.Equal(1U<<4, filter.M);
			Assert.True(filter.Match(byteArray0, testKey));
			Assert.True(filter.Match(byteArray1, testKey));
			Assert.True(filter.Match(byteArray2, testKey));
			Assert.False(filter.Match(new byte[]{ 6, 7, 8}, testKey));
			Assert.True(filterSize8_4 < filterSize10_10); // filter size depends only on P parameter
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSupportCustomeFiltersTest()
		{
			var blockHex = File.ReadAllText("./data/block-testnet-828575.txt");
			var block = Block.Parse(blockHex, Network.TestNet);
			var scripts = new HashSet<Script>();

			foreach (var tx in block.Transactions)
			{
				for (int i = 0; i < tx.Outputs.Count; i++)
				{
					var output = tx.Outputs[i];
					if (!output.ScriptPubKey.IsPayToScriptHash && output.ScriptPubKey.IsWitness)
					{
						var outpoint = new OutPoint(tx.GetHash(), i);
						scripts.Add(output.ScriptPubKey);
					}
				}
			}

			var key = block.GetHash();
			var testkey = key.ToBytes().SafeSubarray(0, 16);
			var filter = new GolombRiceFilterBuilder()
				.SetP(20)
				.SetM(1U << 20)
				.SetKey(key)
				.AddEntries(scripts.Select(x => x.ToCompressedBytes()))
				.Build();

			Assert.Equal("017821b8", filter.ToString());
			foreach (var tx in block.Transactions)
			{
				for (int i = 0; i < tx.Outputs.Count; i++)
				{
					var output = tx.Outputs[i];
					if (!output.ScriptPubKey.IsPayToScriptHash && output.ScriptPubKey.IsWitness)
					{
						Assert.True(filter.Match(output.ScriptPubKey.ToCompressedBytes(), testkey));
					}
				}
			}
		}
	}
}
