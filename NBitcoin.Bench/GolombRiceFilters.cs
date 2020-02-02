using System;
using NBitcoin;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Bench
{
	[RPlotExporter, RankColumn]
	public class GolombRiceFilters
	{
		BlockSample Sample;
		GolombRiceFilter BlockFilter;

		[GlobalSetup]
		public void Setup()
		{
			Sample = new BlockSample();
			Sample.Download();
			BlockFilter = GolombRiceFilterBuilder.BuildBasicFilter(Sample.BigBlock);
		}


		[Benchmark]
		public void Build()
		{
			GolombRiceFilterBuilder.BuildBasicFilter(Sample.BigBlock);
		}
	}
}