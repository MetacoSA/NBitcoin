using NBitcoin.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class ChainTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSaveChain()
		{
			var stream = new StreamObjectStream<ChainChange>(new MemoryStream());
			Chain chain = Chain.LoadOrInitialize(stream, Network.Main);
			AppendBlock(stream, chain);
			AppendBlock(stream, chain);
			var fork = AppendBlock(stream, chain);
			AppendBlock(stream, chain);


			stream.Rewind();

			var chain2 = Chain.Load(stream);
			Assert.True(chain.SameTip(chain2));

			stream.WriteNext(new ChainChange()
			{
				Add = false,
				HeightOrBackstep = 1
			});
			stream.Rewind();

			var chain3 = Chain.Load(stream);
			AssertHeight(stream, 3);
			var actualFork = chain3.FindFork(chain);
			Assert.Equal(fork.HashBlock, actualFork.HashBlock);
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

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildChainIncrementally()
		{

			StreamObjectStream<ChainChange> changes = new StreamObjectStream<ChainChange>();
			Chain main = Chain.LoadOrInitialize(changes, Network.Main);
			AppendBlock(changes, main);
			AppendBlock(changes, main);
			var forkPoint = AppendBlock(changes, main);

			AssertHeight(changes, 3);
			AssertLength(changes, 4);

			var current = AppendBlock(changes, forkPoint, main);
			var oldFork = AppendBlock(changes, current, main);

			AssertHeight(changes, 5);
			AssertLength(changes, 6);

			current = AppendBlock(changes, forkPoint, main);
			AssertHeight(changes, 5);
			AssertLength(changes, 6);

			current = AppendBlock(changes, current, main);
			AssertHeight(changes, 5);
			AssertLength(changes, 6);

			current = AppendBlock(changes, current, main);
			AssertHeight(changes, 6);

			//1 back track (retour à 3) + height4 + height5 + height6
			AssertLength(changes, 10);

			current = AppendBlock(changes, oldFork, main);
			AssertHeight(changes, 6);
			AssertLength(changes, 10);

			//1 back track (retour à 3) + height4 + height5 + height6 + height7
			current = AppendBlock(changes, current, main);
			AssertHeight(changes, 7);
			AssertLength(changes, 15);
		}

		private void AssertLength(StreamObjectStream<ChainChange> changes, int count)
		{
			changes.Rewind();
			Assert.Equal(count, changes.Enumerate().Count());
		}

		public void AssertHeight(StreamObjectStream<ChainChange> changes, int height)
		{
			changes.Rewind();
			Assert.Equal(height, Chain.Load(changes).Height);
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildPartialChain()
		{
			Chain chain = new Chain(TestUtils.CreateFakeBlock().Header, 10);
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
			Chain chain = new Chain(TestUtils.CreateFakeBlock().Header, 10);
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
			Chain side = new Chain(block.Header, 10);
			Chain main = new Chain(block.Header, 10);
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


		private BlockIndex AppendBlock(ObjectStream<ChainChange> changes, BlockIndex previous, params Chain[] chains)
		{
			BlockIndex last = null;
			var nonce = RandomUtils.GetUInt32();
			foreach(var chain in chains)
			{
				var block = TestUtils.CreateFakeBlock(new Transaction());
				block.Header.HashPrevBlock = previous == null ? chain.Tip.HashBlock : previous.HashBlock;
				block.Header.Nonce = nonce;
				last = chain.GetOrAdd(block.Header, changes);
			}
			return last;
		}

		private BlockIndex AppendBlock(params Chain[] chains)
		{
			return AppendBlock(null, chains);
		}
		private BlockIndex AppendBlock(ObjectStream<ChainChange> changes, params Chain[] chains)
		{
			BlockIndex index = null;
			return AppendBlock(changes, index, chains);
		}
	}
}
