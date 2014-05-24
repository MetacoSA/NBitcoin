using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class ChainTests
	{
		[Fact]
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
			Assert.Null(chain.Get(last.HashBlock));
			Assert.NotNull(chain.Get(fork.HashBlock));

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
			Assert.Null(chain.Get(last.HashBlock));
			Assert.Null(chain.Get(b1.HashBlock));
			Assert.Null(chain.Get(b2.HashBlock));

			chain.SetTip(last);
			Assert.Equal(6, chain.Height);
			Assert.Equal(6, last.Height);
			Assert.Equal(last.HashBlock, chain.Tip.HashBlock);
		}

		[Fact]
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
			Assert.NotNull(side.Get(mainb1.HashBlock));
			Assert.NotNull(side.Get(mainb2.HashBlock));
			Assert.NotNull(side.Get(mainb3.HashBlock));
			Assert.NotNull(side.Get(common.HashBlock));
			Assert.Null(side.Get(sideb.HashBlock));

			Assert.Equal(common.HashBlock, side.SetTip(sideb).HashBlock);
			Assert.Null(side.Get(mainb1.HashBlock));
			Assert.Null(side.Get(mainb2.HashBlock));
			Assert.Null(side.Get(mainb3.HashBlock));
			Assert.NotNull(side.Get(sideb.HashBlock));
		}

		private BlockIndex AppendBlock(params Chain[] chains)
		{
			BlockIndex last = null;
			var nonce = RandomUtils.GetUInt32();
			foreach(var chain in chains)
			{
				var block = TestUtils.CreateFakeBlock(new Transaction());
				block.Header.HashPrevBlock = chain.Tip.HashBlock;
				block.Header.Nonce = nonce;
				last = chain.GetOrAdd(block.Header);
			}
			return last;
		}
	}
}
