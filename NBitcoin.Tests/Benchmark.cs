using NBitcoin.Crypto;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
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
			//TestUtils.EnsureNew("BlockDirectoryScanSpeed");
			var completeScan = Bench(() =>
			{
				BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
				//BlockStore other = new BlockStore(@"BlockDirectoryScanSpeed", Network.Main);
				foreach(var block in store.Enumerate(false, new DiskBlockPosRange(new DiskBlockPos(120, 0))))
				{
					if(block.Item.Header.BlockTime < ColoredTransaction.FirstColoredDate)
						continue;
					foreach(var tx in block.Item.Transactions)
					{
						uint index = 0;
						var pay = OpenAsset.ColorMarker.Get(tx, out index);
						if(pay != null && index != 0 && index != tx.Outputs.Count - 1)
						{
							if(pay.Quantities.Length > index)
								Debugger.Break();
						}

					}
				}
			});

			var headersOnlyScan = Bench(() =>
			{
				BlockStore store = new BlockStore(@"E:\Bitcoin\blocks\", Network.Main);
				var count = store.Enumerate(true).Count();
			});
		}

		[Fact]
		[Trait("Benchmark", "Benchmark")]
		public void BlockDownloadFromNetwork()
		{
			using(var server = new NodeServer(Network.Main))
			{
				var originalNode = server.GetLocalNode();
				var original = originalNode.GetChain();
				Assert.True(originalNode.PeerVersion.StartHeight <= original.Height);

				int simultaneous = 3;
				var chaines = Enumerable.Range(0, simultaneous).Select(i => original.Clone()).ToArray();
				var time = Benchmark.Bench(() =>
				{
					Parallel.For(0, simultaneous, new ParallelOptions()
					{
						MaxDegreeOfParallelism = simultaneous
					}, i =>
					{
						var chain = chaines[i];
						var node = new NodeServer(Network.Main).GetLocalNode();
						var localTime = Benchmark.Bench(() =>
						{
							chain.PushChange(new ChainChange()
							{
								ChangeType = ChainChangeType.BackStep,
								HeightOrBackstep = 100
							}, null);
							var blocks = node.GetBlocks(chain.ToEnumerable(true).Select(c => c.HashBlock)).ToList();
							Assert.True(blocks.Count == 100 || blocks.Count == 101);
						});
					});
				});
				Console.WriteLine(time);
			}
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
			IndexedBlockStore indexed = new IndexedBlockStore(new SQLiteNoSqlRepository("indexbench", true), store);
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
