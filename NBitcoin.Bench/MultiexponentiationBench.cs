using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IO;
using NBitcoin.Bench;
using NBitcoin.Secp256k1;

namespace NBitcoin.Bench
{
	[RPlotExporter, RankColumn]
	public class Multiexponentiation
	{
		[Params(2, 10, 100)]
		public int count;

		public Scalar[] ns;
		public GEJ[] gs;
		public GE me;
		public GE em;

		[GlobalSetup]
		public void Setup()
		{
			ns = Enumerable.Range(1, count).Select(x => new Scalar((uint)x)).ToArray();
			gs = Enumerable.Repeat(EC.G.ToGroupElementJacobian(), count).ToArray();
		}

		[Benchmark]
		public void MultiExp()
		{
			me = ECMultContext.Instance.Mult(gs, ns).ToGroupElement();
		}

		[Benchmark]
		public void ExpMulti()
		{
			em =Enumerable.Aggregate(
				Enumerable.Zip(ns, gs, (n, g) => n * g.ToGroupElement()), (acc, elem) => acc + elem.ToGroupElement()).ToGroupElement();
		}

		[GlobalCleanup]
		public void CleanUp()
		{
			if (!me.Equals(em))
				throw new Exception("No equals");
		}
 	}
}