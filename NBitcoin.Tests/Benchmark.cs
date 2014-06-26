using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class Benchmark
	{
		[Fact]
		[Trait("Benchmark","Benchmark")]
		public void BlockDirectoryScanSpeed()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			var count = store.EnumerateFile(@"E:\Bitcoin\blocks\blk00150.dat").Count();
			watch.Stop();
			var spentByBlock = TimeSpan.FromTicks(watch.ElapsedTicks / count);
		}
	}
}
