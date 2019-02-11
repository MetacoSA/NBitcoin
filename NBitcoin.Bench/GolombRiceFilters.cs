using System;
using NBitcoin;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Benck
{
	[ClrJob(baseline: true), CoreJob]
	[RPlotExporter, RankColumn]
	public class GolombRiceFilterBuilding
	{
		private GolombRiceFilterBuilder _builder;
		private List<byte[]> _itemsInFilter;

		// We estimate a block will never have more than 100,000 txouts
		// OTOH 20,000 outputs could be a rasonable number of txouts 
		// for a block considering the current block sizes.
		// Also, currently P2WPKH tx are 5% of total scripts and that
		// gives us 1,000 txouts.
		[Params(1_000, 20_000, 100_000)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			var random = new Random(Seed:145);
			var keyBuffer = new byte[32];
			random.NextBytes(keyBuffer);
			var key = new uint256(keyBuffer);

			_builder = new GolombRiceFilterBuilder()
				.SetKey(key)
				.SetP(20);

			_itemsInFilter = new List<byte[]>();
			for (var j = 0; j < N; j++)
			{
				var data = new byte[random.Next(20, 30)];
				random.NextBytes(data);
				_itemsInFilter.Add(data);
			}
			_builder.AddEntries(_itemsInFilter);
		}

		[Benchmark]
		public GolombRiceFilter Build() => _builder.Build();
	}

	[ClrJob(baseline: true), CoreJob]
	[RPlotExporter, RankColumn]
	public class GolombRiceFilterQuering
	{
		private GolombRiceFilter _filter;
		private byte[][] _sample;
		private Random _random = new Random(Seed:145);
		private byte[] _testKey;

		[Params(1_000, 10_000, 100_000)]
		public int N;

		[GlobalSetup]
		public void Setup()
		{
			var keyBuffer = new byte[32];
			_random.NextBytes(keyBuffer);
			var key = new uint256(keyBuffer);
			_testKey = key.ToBytes().Take(16).ToArray();

			var builder = new GolombRiceFilterBuilder()
				.SetKey(key)
				.SetP(20);

			var itemsInFilter = new List<byte[]>();
			for (var j = 0; j < N; j++)
			{
				var data = new byte[_random.Next(20, 30)];
				_random.NextBytes(data);
				itemsInFilter.Add(data);
			}
			builder.AddEntries(itemsInFilter);
			_sample = itemsInFilter.OrderBy(x=>_random.Next()).Take(N / 200).ToArray();
			_filter = builder.Build();
		}

		[Benchmark]
		public bool Match() => _filter.Match(_sample[_sample.Count() %2 ], _testKey);

		[Benchmark]
		public bool MatchAny() => _filter.MatchAny(_sample, _testKey);
	}
}