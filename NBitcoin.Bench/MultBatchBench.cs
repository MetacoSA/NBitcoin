#if HAS_SPAN
using BenchmarkDotNet.Attributes;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Secp256k1;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Bench
{
	[MemoryDiagnoser]
	public class MultBatchBench
	{
		Scalar[] Scalars;
		GE[] Points;

		[Params(300, 500)]
		public int KeyCount { get; set; }

		[Params(ECMultiImplementation.Pippenger, ECMultiImplementation.Simple, ECMultiImplementation.Strauss, ECMultiImplementation.Auto)]
		public ECMultiImplementation Implementation { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			Scalars = new Scalar[KeyCount];
			Points = new GE[KeyCount];
			for (int i = 0; i < KeyCount; i++)
			{
				Scalars[i] = new Scalar(RandomUtils.GetBytes(32));
				Points[i] = new GE(new FE(RandomUtils.GetBytes(32)), new FE(RandomUtils.GetBytes(32)));
			}
		}
		[Benchmark]
		public void BatchMult()
		{
			var batch = ECMultContext.Instance.MultBatch(null, Scalars, Points, new MultBatchOptions(Implementation));
		}
	}
}
#endif
