using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Bench
{
	public class OutPointBench
	{
		private const int Count = 10000;
		private OutPoint[] _outPoints;

		[GlobalSetup]
		public void Setup()
		{
			Random random = new Random(Seed: 53454);

			_outPoints = new OutPoint[Count];

			for (int i = 0; i < Count; i++)
			{
				uint256 txid = new uint256(RandomUtils.GetBytes(32));
				int n = random.Next(1_000_000) % 2;

				_outPoints[i] = new OutPoint(txid, n);
			}
		}

		[Benchmark]
		public void Old()
		{
			_ = DoComparisons(newImplementation: false);
		}

		[Benchmark]
		public void New()
		{
			_ = DoComparisons(newImplementation: true);
		}

		private int DoComparisons(bool newImplementation)
		{
			int count = 0;

			foreach (OutPoint o1 in _outPoints)
			{
				foreach (OutPoint o2 in _outPoints)
				{
					if (OutPoint.OperatorEqualsEquals(newImplementation, o1, o2))
					{
						count++;
					}
				}
			}

			return count;
		}
	}
}
