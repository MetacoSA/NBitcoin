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
			//var completeScan = Bench(() =>
			//{
			//	BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			//	var count = store.Enumerate(false).Take(150000).Count();
			//});

			var headersOnlyScan = Bench(() =>
			{
				BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
				var count = store.Enumerate(true).Count();
			});
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

		TimeSpan Bench(Action act)
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			act();
			watch.Stop();
			return watch.Elapsed;
		}
		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BenchmarkCreateChainFromBlocks()
		{
			BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			Chain chain = null;
			var fullBuild = Bench(() =>
			{

				chain = store.BuildChain();
			});

			chain.Changes.Rewind();
			var rebuildFromMemory = Bench(() =>
			{
				var chain2 = new Chain(chain.Changes);
			});

			chain.Changes.Rewind();
			var halfChain = new StreamObjectStream<ChainChange>();
			for(int i = 0 ; i < 300000 ; i++)
			{
				halfChain.WriteNext(chain.Changes.ReadNext());
			}

			var halfBuild = Bench(() =>
			{
				var fullChain = store.BuildChain(halfChain);
			});
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
