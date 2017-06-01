#if !NOFILEIO
using NBitcoin.BitcoinCore;
using NBitcoin.Crypto;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
			//TestUtils.EnsureNew("BlockDirectoryScanSpeed");
			var completeScan = Bench(() =>
			{
				BlockStore store = new BlockStore(@"Z:\Bitcoin\blocks", Network.Main);
				//BlockStore other = new BlockStore(@"BlockDirectoryScanSpeed", Network.Main);
				foreach(var block in store.Enumerate(false))
				{

				}
			});

			//var headersOnlyScan = Bench(() =>
			//{
			//	BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
			//	var count = store.Enumerate(true).Count();
			//});
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BlockDownloadFromNetwork()
		{
			Task.WaitAll(Enumerable.Range(0, 1)
				.Select(_ => Task.Factory.StartNew(Core))
				.ToArray());
		}

		private static void Core()
		{

			var complete = Bench(() =>
			{
				using(var node = Node.Connect(Network.Main, "192.168.0.7", new NodeConnectionParameters()
				{
					IsRelay = false
				}))
				{
					var originalNode = node;
					node.VersionHandshake();
					var chain = node.GetChain();
					List<ulong> speeds = new List<ulong>();

					Stopwatch watch = new Stopwatch();
					watch.Start();
					PerformanceSnapshot snap = null;
					foreach(var block in originalNode.GetBlocks(chain.Tip.EnumerateToGenesis()))
					{
						if(watch.Elapsed > TimeSpan.FromSeconds(10.0))
						{
							var newSnap = originalNode.Counter.Snapshot();
							if(snap != null)
							{
								var perf = newSnap - snap;
								speeds.Add(perf.ReadenBytesPerSecond / 1024);
							}
							snap = newSnap;
							watch.Restart();
						}

					}
				}
			});
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BlockDirectoryScanScriptSpeed()
		{
			List<TimeSpan> times = new List<TimeSpan>();
			times.Add(BenchmarkTemplate((txout) => PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
			times.Add(BenchmarkTemplate((txout) => TxNullDataTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)));
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BlockDirectoryScanScriptSpeedParallel()
		{
			List<Task<TimeSpan>> times = new List<Task<TimeSpan>>();
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));
			times.Add(Task.Factory.StartNew(() => BenchmarkTemplate((txout) => TxNullDataTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey)), TaskCreationOptions.LongRunning));

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
			IndexedBlockStore indexed = new IndexedBlockStore(new InMemoryNoSqlRepository(), store);
			indexed.ReIndex();
			watch.Stop();
			var time = watch.Elapsed;
		}

		public static TimeSpan Bench(Action act)
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
			ConcurrentChain chain = null;
			var fullBuild = Bench(() =>
			{
				chain = store.GetChain();
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
#endif