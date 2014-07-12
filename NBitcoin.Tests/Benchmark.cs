using NBitcoin.Crypto;
using NBitcoin.Scanning;
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
		[Trait("Benchmark", "Benchmark")]
		public void BlockDirectoryScanSpeed()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			var count = store.Enumerate(false).Take(150000).Count();
			watch.Stop();
			var spentByBlock = TimeSpan.FromTicks(watch.ElapsedTicks / count);
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BlockDirectoryScanScriptSpeed()
		{
			List<TimeSpan> times = new List<TimeSpan>();
			times.Add(BenchmarkTemplate((txout) => new PayToMultiSigTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => new PayToPubkeyHashTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => new PayToScriptHashTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => new PayToPubkeyTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => new TxNullDataTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BlockDirectoryScanScriptSpeedParallel()
		{
			List<Task<TimeSpan>> times = new List<Task<TimeSpan>>();
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => new PayToMultiSigTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => new PayToPubkeyHashTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => new PayToScriptHashTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => new PayToPubkeyTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => new TxNullDataTemplate().ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));

			Task.WaitAll(times.ToArray());

			var result = times.Select(o => o.Result).ToArray();
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BenchmarkBlockIndexing()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			IndexedBlockStore indexed = new IndexedBlockStore(new SQLiteNoSqlRepository("indexbench", true), store);
			indexed.ReIndex();
			watch.Stop();
			var time = watch.Elapsed;
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BenchmarkCreateChainFromBlocks()
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			var chain = store.BuildChain();
			watch.Stop();
		}
		private TimeSpan BenchmarkTemplate(Action<TxOut> act)
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			foreach(var txout in store.EnumerateFolder().Take(150000).SelectMany(o => o.Item.Transactions.SelectMany(t => t.Outputs)))
			{
				act(txout);
			}
			watch.Stop();
			return watch.Elapsed;
		}
	}
}
