using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class ChainTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSaveForkedBlockInChain()
		{
			var chain = new Chain(Network.Main);
			var common = AppendBlock(chain);
			var fork = AppendBlock(chain);
			var fork2 = AppendBlock(common, chain);

			Assert.True(chain.Tip == fork);

			var chain2 = chain.Clone();
			var newTip = AppendBlock(fork2, chain);
			Assert.True(chain.Tip == newTip);

			Assert.True(chain2.Tip == fork);
			newTip = AppendBlock(fork2, chain2);
			Assert.True(chain2.Tip == newTip);
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSaveChain()
		{
			var stream = new StreamObjectStream<ChainChange>();
			Chain chain = new Chain(Network.Main, stream);
			AppendBlock(chain);
			AppendBlock(chain);
			var fork = AppendBlock(chain);
			AppendBlock(chain);


			stream.Rewind();

			var chain2 = new Chain(stream);
			Assert.True(chain.SameTip(chain2));

			stream.WriteNext(new ChainChange()
			{
				ChangeType = ChainChangeType.BackStep,
				HeightOrBackstep = 1
			});
			stream.Rewind();

			var chain3 = new Chain(stream);
			AssertHeight(stream, 3);
			var actualFork = chain3.FindFork(chain);
			Assert.Equal(fork.HashBlock, actualFork.HashBlock);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void IncompleteScriptDoesNotHang()
		{
			new Script(new byte[] { 0x4d }).ToString();
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseRandomScripts()
		{
			for(int i = 0 ; i < 600 ; i++)
			{
				var bytes = RandomUtils.GetBytes(120);
				new Script(bytes).ToString();
			}
		}
#if !PORTABLE
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildConcurrentChain()
		{
			ConcurrentChain cchain = new ConcurrentChain();
			Chain chain = new Chain(Network.Main);
			Assert.Null(cchain.SetTip(chain.Tip));
			var b0 = cchain.Tip;
			Assert.Equal(cchain.Tip, chain.Tip);

			var b1 = AddBlock(chain);
			var b2 = AddBlock(chain);
			AddBlock(chain);
			AddBlock(chain);
			var b5 = AddBlock(chain);

			Assert.Equal(cchain.SetTip(chain.Tip), b0);
			Assert.Equal(cchain.Tip, chain.Tip);

			Assert.Equal(cchain.GetBlock(5), chain.Tip);
			Assert.Equal(cchain.GetBlock(b5.HashBlock), chain.Tip);

			Assert.Equal(cchain.SetTip(b1), b1);
			Assert.Equal(cchain.GetBlock(b5.HashBlock), null);
			Assert.Equal(cchain.GetBlock(b2.HashBlock), null);

			Assert.Equal(cchain.SetTip(b5), b1);
			Assert.Equal(cchain.GetBlock(b5.HashBlock), chain.Tip);

			chain.SetTip(b2);
			AddBlock(chain);
			AddBlock(chain);
			var b5b = AddBlock(chain);
			var b6b = AddBlock(chain);

			Assert.Equal(cchain.SetTip(b6b), b2);

			Assert.Equal(cchain.GetBlock(b5.HashBlock), null);
			Assert.Equal(cchain.GetBlock(b2.HashBlock), b2);
			Assert.Equal(cchain.GetBlock(6), b6b);
			Assert.Equal(cchain.GetBlock(5), b5b);
		}
#endif
		private ChainedBlock AddBlock(Chain chain)
		{
			BlockHeader header = new BlockHeader();
			header.Nonce = RandomUtils.GetUInt32();
			header.HashPrevBlock = chain.Tip.HashBlock;
			chain.SetTip(header);
			return chain.GetBlock(header.GetHash());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildChain()
		{
			Chain chain = new Chain(Network.Main);
			AppendBlock(chain);
			AppendBlock(chain);
			AppendBlock(chain);
			var b = AppendBlock(chain);
			Assert.Equal(4, chain.Height);
			Assert.Equal(4, b.Height);
			Assert.Equal(b.HashBlock, chain.Tip.HashBlock);
		}


		List<ChainChange> EnumerateFromBeginning(Chain chain)
		{
			chain.Changes.Rewind();
			return chain.Changes.Enumerate().ToList();
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildChainIncrementally()
		{

			StreamObjectStream<ChainChange> changes = new StreamObjectStream<ChainChange>();
			Chain main = new Chain(Network.Main, changes);
			AppendBlock(main);
			AppendBlock(main);
			var forkPoint = AppendBlock(main);

			AssertHeight(changes, 3);
			AssertLength(changes, 4);

			var current = AppendBlock(forkPoint, main);
			var oldFork = AppendBlock(current, main);

			AssertHeight(changes, 5);
			AssertLength(changes, 6);

			current = AppendBlock(forkPoint, main);
			AssertHeight(changes, 5);
			AssertLength(changes, 7);

			current = AppendBlock(current, main);
			AssertHeight(changes, 5);
			AssertLength(changes, 8);

			current = AppendBlock(current, main);
			AssertHeight(changes, 6);

			//1 back track (retour à 3) + 1 set tip
			AssertLength(changes, 10);

			current = AppendBlock(oldFork, main);
			AssertHeight(changes, 6);
			AssertLength(changes, 11);

			//1 back track (retour à 3) + 1 set tip à 7
			current = AppendBlock(current, main);
			AssertHeight(changes, 7);
			AssertLength(changes, 13);
		}

		private void AssertLength(StreamObjectStream<ChainChange> changes, int count)
		{
			changes.Rewind();
			Assert.Equal(count, changes.Enumerate().Count());
		}

		public void AssertHeight(StreamObjectStream<ChainChange> changes, int height)
		{
			changes.Rewind();
			Assert.Equal(height, new Chain(changes).Height);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateDifficulty()
		{
			var o = Network.Main.ProofOfWorkLimit;
			var main = new Chain(LoadMainChain());
			var histories = File.ReadAllText("data/targethistory.csv").Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach(var history in histories)
			{
				var height = int.Parse(history.Split(',')[0]);
				var expectedTarget = new Target(BigInteger.Parse(history.Split(',')[1]));

				var block = main.GetBlock(height).Header;

				Assert.Equal(expectedTarget, block.Bits);
				var target = main.GetWorkRequired(Network.Main, height);
				Assert.Equal(expectedTarget, target);
			}
		}

		private ObjectStream<ChainChange> LoadMainChain()
		{
			if(!File.Exists("MainChain.dat"))
			{
				WebClient client = new WebClient();
				client.DownloadFile("https://aois.blob.core.windows.net/public/MainChain.dat", "MainChain.dat");
			}
			return new StreamObjectStream<ChainChange>(File.Open("MainChain.dat", FileMode.Open));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildPartialChain()
		{
			Chain chain = new Chain(TestUtils.CreateFakeBlock().Header, 10, new StreamObjectStream<ChainChange>());
			AppendBlock(chain);
			AppendBlock(chain);
			AppendBlock(chain);
			var b = AppendBlock(chain);
			Assert.Equal(14, chain.Height);
			Assert.Equal(14, b.Height);
			Assert.Equal(b.HashBlock, chain.Tip.HashBlock);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanForkBackward()
		{
			Chain chain = new Chain(Network.Main);
			AppendBlock(chain);
			AppendBlock(chain);
			var fork = AppendBlock(chain);

			//Test single block back fork
			var last = AppendBlock(chain);
			Assert.Equal(4, chain.Height);
			Assert.Equal(4, last.Height);
			Assert.Equal(last.HashBlock, chain.Tip.HashBlock);
			Assert.Equal(fork.HashBlock, chain.SetTip(fork).HashBlock);
			Assert.Equal(3, chain.Height);
			Assert.Equal(3, fork.Height);
			Assert.Equal(fork.HashBlock, chain.Tip.HashBlock);
			Assert.Null(chain.GetBlock(last.HashBlock));
			Assert.NotNull(chain.GetBlock(fork.HashBlock));

			//Test 3 blocks back fork
			var b1 = AppendBlock(chain);
			var b2 = AppendBlock(chain);
			last = AppendBlock(chain);
			Assert.Equal(6, chain.Height);
			Assert.Equal(6, last.Height);
			Assert.Equal(last.HashBlock, chain.Tip.HashBlock);

			Assert.Equal(fork.HashBlock, chain.SetTip(fork).HashBlock);
			Assert.Equal(3, chain.Height);
			Assert.Equal(3, fork.Height);
			Assert.Equal(fork.HashBlock, chain.Tip.HashBlock);
			Assert.Null(chain.GetBlock(last.HashBlock));
			Assert.Null(chain.GetBlock(b1.HashBlock));
			Assert.Null(chain.GetBlock(b2.HashBlock));

			chain.SetTip(last);
			Assert.Equal(6, chain.Height);
			Assert.Equal(6, last.Height);
			Assert.Equal(last.HashBlock, chain.Tip.HashBlock);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanForkBackwardPartialChain()
		{
			Chain chain = new Chain(TestUtils.CreateFakeBlock().Header, 10, new StreamObjectStream<ChainChange>());
			AppendBlock(chain);
			AppendBlock(chain);
			var fork = AppendBlock(chain);

			//Test single block back fork
			var last = AppendBlock(chain);
			Assert.Equal(14, chain.Height);
			Assert.Equal(14, last.Height);
			Assert.Equal(last.HashBlock, chain.Tip.HashBlock);
			Assert.Equal(fork.HashBlock, chain.SetTip(fork).HashBlock);
			Assert.Equal(13, chain.Height);
			Assert.Equal(13, fork.Height);
			Assert.Equal(fork.HashBlock, chain.Tip.HashBlock);
			Assert.Null(chain.GetBlock(last.HashBlock));
			Assert.NotNull(chain.GetBlock(fork.HashBlock));

			//Test 3 blocks back fork
			var b1 = AppendBlock(chain);
			var b2 = AppendBlock(chain);
			last = AppendBlock(chain);
			Assert.Equal(16, chain.Height);
			Assert.Equal(16, last.Height);
			Assert.Equal(last.HashBlock, chain.Tip.HashBlock);

			Assert.Equal(fork.HashBlock, chain.SetTip(fork).HashBlock);
			Assert.Equal(13, chain.Height);
			Assert.Equal(13, fork.Height);
			Assert.Equal(fork.HashBlock, chain.Tip.HashBlock);
			Assert.Null(chain.GetBlock(last.HashBlock));
			Assert.Null(chain.GetBlock(b1.HashBlock));
			Assert.Null(chain.GetBlock(b2.HashBlock));

			chain.SetTip(last);
			Assert.Equal(16, chain.Height);
			Assert.Equal(16, last.Height);
			Assert.Equal(last.HashBlock, chain.Tip.HashBlock);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanForkSide()
		{
			Chain side = new Chain(Network.Main);
			Chain main = new Chain(Network.Main);
			AppendBlock(side, main);
			AppendBlock(side, main);
			var common = AppendBlock(side, main);
			var sideb = AppendBlock(side);
			var mainb1 = AppendBlock(main);
			var mainb2 = AppendBlock(main);
			var mainb3 = AppendBlock(main);
			Assert.Equal(common.HashBlock, side.SetTip(main.Tip).HashBlock);
			Assert.NotNull(side.GetBlock(mainb1.HashBlock));
			Assert.NotNull(side.GetBlock(mainb2.HashBlock));
			Assert.NotNull(side.GetBlock(mainb3.HashBlock));
			Assert.NotNull(side.GetBlock(common.HashBlock));
			Assert.Null(side.GetBlock(sideb.HashBlock));

			Assert.Equal(common.HashBlock, side.SetTip(sideb).HashBlock);
			Assert.Null(side.GetBlock(mainb1.HashBlock));
			Assert.Null(side.GetBlock(mainb2.HashBlock));
			Assert.Null(side.GetBlock(mainb3.HashBlock));
			Assert.NotNull(side.GetBlock(sideb.HashBlock));
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanForkSidePartialChain()
		{
			var block = TestUtils.CreateFakeBlock();
			Chain side = new Chain(block.Header, 10, new StreamObjectStream<ChainChange>());
			Chain main = new Chain(block.Header, 10, new StreamObjectStream<ChainChange>());
			AppendBlock(side, main);
			AppendBlock(side, main);
			var common = AppendBlock(side, main);
			var sideb = AppendBlock(side);
			var mainb1 = AppendBlock(main);
			var mainb2 = AppendBlock(main);
			var mainb3 = AppendBlock(main);
			Assert.Equal(common.HashBlock, side.SetTip(main.Tip).HashBlock);
			Assert.NotNull(side.GetBlock(mainb1.HashBlock));
			Assert.NotNull(side.GetBlock(mainb2.HashBlock));
			Assert.NotNull(side.GetBlock(mainb3.HashBlock));
			Assert.NotNull(side.GetBlock(common.HashBlock));
			Assert.Null(side.GetBlock(sideb.HashBlock));

			Assert.Equal(common.HashBlock, side.SetTip(sideb).HashBlock);
			Assert.Null(side.GetBlock(mainb1.HashBlock));
			Assert.Null(side.GetBlock(mainb2.HashBlock));
			Assert.Null(side.GetBlock(mainb3.HashBlock));
			Assert.NotNull(side.GetBlock(sideb.HashBlock));
		}


		public ChainedBlock AppendBlock(ChainedBlock previous, params Chain[] chains)
		{
			ChainedBlock last = null;
			var nonce = RandomUtils.GetUInt32();
			foreach(var chain in chains)
			{
				var block = TestUtils.CreateFakeBlock(new Transaction());
				block.Header.HashPrevBlock = previous == null ? chain.Tip.HashBlock : previous.HashBlock;
				block.Header.Nonce = nonce;
				if(!chain.TrySetTip(block.Header, out last))
					throw new InvalidOperationException("Previous not existing");
			}
			return last;
		}
		private ChainedBlock AppendBlock(params Chain[] chains)
		{
			ChainedBlock index = null;
			return AppendBlock(index, chains);
		}
	}
}
