using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
		public void CanCloneConcurrentChain()
		{
			var chain = new ConcurrentChain(Network.Main);
			var common = AppendBlock(chain);
			var fork = AppendBlock(chain);
			var fork2 = AppendBlock(chain);

			Assert.True(chain.Tip == fork2);
			var clone = chain.Clone();
			Assert.True(clone.Tip == fork2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildSlimChain()
		{
			var b0 = RandomUInt256();
			SlimChain chain = new SlimChain(b0);
			var b1 = RandomUInt256();
			Assert.Throws<ArgumentException>(() => chain.TrySetTip(b0, b0));
			Assert.True(chain.TrySetTip(b1, b0));
			var b2 = RandomUInt256();
			Assert.True(chain.TrySetTip(b2, b1));
			Assert.True(chain.TrySetTip(b2, b1));
			Assert.Equal(b0, chain.Genesis);
			Assert.Equal(b2, chain.Tip);
			Assert.True(chain.Contains(b2));
			Assert.Equal(2, chain.Height);
			Assert.False(chain.TrySetTip(b1, b0, true));
			Assert.True(chain.TrySetTip(b1, b0, false));
			Assert.Equal(b1, chain.Tip);
			Assert.False(chain.TryGetHeight(b2, out int height));
			Assert.False(chain.Contains(b2));
			Assert.True(chain.TryGetHeight(b1, out height));
			Assert.Equal(1, height);

			Assert.True(chain.TrySetTip(b2, b1));
			Assert.Throws<ArgumentException>(() => chain.TrySetTip(b1, b2)); // Incoherent
			Assert.Throws<ArgumentException>(() => chain.TrySetTip(b0, b1, true)); // Genesis block should not have previosu
			Assert.Throws<ArgumentException>(() => chain.TrySetTip(b0, b1, false));
			Assert.True(chain.TrySetTip(b0, null));
			Assert.Equal(0, chain.Height);
			Assert.True(chain.TrySetTip(b1, b0, true));
			Assert.True(chain.TrySetTip(b2, b1));

			var b3 = RandomUInt256();
			var block = chain.GetBlock(b2);
			Assert.Equal(b2, block.Hash);
			Assert.Equal(b1, block.Previous);
			Assert.Equal(2, block.Height);
			Assert.Null(chain.GetBlock(b3));

			block = chain.GetBlock(2);
			Assert.Equal(b2, block.Hash);
			Assert.Equal(b1, block.Previous);
			Assert.Equal(2, block.Height);
			Assert.Null(chain.GetBlock(3));
			Assert.Null(chain.GetBlock(-1));

			block = chain.GetBlock(0);
			Assert.Equal(b0, block.Hash);
			Assert.Null(block.Previous);
			Assert.Equal(0, block.Height);

			var chain2 = new SlimChain(RandomUInt256());
			var ms = new MemoryStream();
			chain.Save(ms);
			ms.Position = 0;
			// Not good genesis
			Assert.Throws<InvalidOperationException>(() => chain2.Load(ms));

			chain2 = new SlimChain(b0);
			ms.Position = 0;
			chain2.Load(ms);
			Assert.Equal(chain.Tip, chain2.Tip);
			Assert.Equal(2, chain2.Height);
		}

		private uint256 RandomUInt256()
		{
			return RandomUtils.GetUInt256();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSaveChain()
		{
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
			AppendBlock(chain);
			AppendBlock(chain);
			var fork = AppendBlock(chain);
			AppendBlock(chain);



			var chain2 = new ConcurrentChain(chain.ToBytes(), Network.Main);
			Assert.True(chain.SameTip(chain2));
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
			for (int i = 0; i < 600; i++)
			{
				var bytes = RandomUtils.GetBytes(120);
				new Script(bytes).ToString();
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanLoadAndSaveConcurrentChain()
		{
			ConcurrentChain cchain = new ConcurrentChain();
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
			AddBlock(chain);
			AddBlock(chain);
			AddBlock(chain);

			cchain.SetTip(chain);

			var bytes = cchain.ToBytes();
			cchain = new ConcurrentChain();
			cchain.Load(bytes, Network.TestNet);

			Assert.Equal(cchain.Tip, chain.Tip);
			Assert.NotNull(cchain.GetBlock(0));

			cchain = new ConcurrentChain(Network.TestNet);
			cchain.Load(cchain.ToBytes(), Network.TestNet);
			Assert.NotNull(cchain.GetBlock(0));
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildConcurrentChain()
		{
			ConcurrentChain cchain = new ConcurrentChain();
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
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
			Assert.Null(cchain.GetBlock(b5.HashBlock));
			Assert.Null(cchain.GetBlock(b2.HashBlock));

			Assert.Equal(cchain.SetTip(b5), b1);
			Assert.Equal(cchain.GetBlock(b5.HashBlock), chain.Tip);

			chain.SetTip(b2);
			AddBlock(chain);
			AddBlock(chain);
			var b5b = AddBlock(chain);
			var b6b = AddBlock(chain);

			Assert.Equal(cchain.SetTip(b6b), b2);

			Assert.Null(cchain.GetBlock(b5.HashBlock));
			Assert.Equal(cchain.GetBlock(b2.HashBlock), b2);
			Assert.Equal(cchain.GetBlock(6), b6b);
			Assert.Equal(cchain.GetBlock(5), b5b);
		}

		private ChainedBlock AddBlock(ConcurrentChain chain)
		{
			BlockHeader header = Network.Main.Consensus.ConsensusFactory.CreateBlockHeader();
			header.Nonce = RandomUtils.GetUInt32();
			header.HashPrevBlock = chain.Tip.HashBlock;
			chain.SetTip(header);
			return chain.GetBlock(header.GetHash());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIterateConcurrentChain()
		{
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
			AppendBlock(chain);
			AppendBlock(chain);
			AppendBlock(chain);
			foreach (var b in chain.EnumerateAfter(chain.Genesis))
			{
				chain.GetBlock(0);
			}

			foreach (var b in chain.ToEnumerable(false))
			{
				chain.GetBlock(0);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildChain()
		{
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
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
		public void CanFindFork()
		{
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
			ConcurrentChain chain2 = new ConcurrentChain(Network.Main);
			AppendBlock(chain);
			var fork = AppendBlock(chain);
			var tip = AppendBlock(chain);

			AssertFork(chain, chain2, chain.Genesis);
			chain2 = new ConcurrentChain(Network.TestNet);
			AssertFork(chain, chain2, null);
			chain2 = new ConcurrentChain(Network.Main);
			chain2.SetTip(fork);
			AssertFork(chain, chain2, fork);
			chain2.SetTip(tip);
			AssertFork(chain, chain2, tip);
		}

		private void AssertFork(ConcurrentChain chain, ConcurrentChain chain2, ChainedBlock expectedFork)
		{
			var fork = chain.FindFork(chain2);
			Assert.Equal(expectedFork, fork);
			fork = chain.Tip.FindFork(chain2.Tip);
			Assert.Equal(expectedFork, fork);

			var temp = chain;
			chain = chain2;
			chain2 = temp;

			fork = chain.FindFork(chain2);
			Assert.Equal(expectedFork, fork);
			fork = chain.Tip.FindFork(chain2.Tip);
			Assert.Equal(expectedFork, fork);
		}

#if !NOFILEIO
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateDifficulty()
		{
			var main = new ConcurrentChain(LoadMainChain(), Network.Main);
			var histories = File.ReadAllText("data/targethistory.csv").Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var history in histories)
			{
				var height = int.Parse(history.Split(',')[0]);
#if NO_NATIVE_BIGNUM
				var expectedTarget = new Target(new BouncyCastle.Math.BigInteger(history.Split(',')[1], 10));
#else
				var expectedTarget = new Target(System.Numerics.BigInteger.Parse(history.Split(',')[1]));
#endif

				var block = main.GetBlock(height).Header;

				Assert.Equal(expectedTarget, block.Bits);
				var target = main.GetWorkRequired(Network.Main, height);
				Assert.Equal(expectedTarget, target);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanValidateChain()
		{
			var main = new ConcurrentChain(LoadMainChain(), Network.Main);
			foreach (var h in main.ToEnumerable(false))
			{
				Assert.True(h.Validate(Network.Main));
			}
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreateBigSlimChain()
		{
			var main = new ConcurrentChain(LoadMainChain(), Network.Main);
			var c = new SlimChain(main.GetBlock(0).HashBlock);
			foreach (var item in main.EnumerateToTip(main.GetBlock(0).HashBlock))
			{
				c.TrySetTip(item.HashBlock, item.Previous?.HashBlock);
			}
			Assert.Equal(main.Height, c.Height);
			Assert.Equal(main.Tip.HashBlock, c.Tip);
			// Can up the capacity without errors
			c.SetCapacity(main.Height + 3000);
			Assert.Equal(main.Height, c.Height);
			Assert.Equal(main.Tip.HashBlock, c.Tip);
			Assert.Equal(main.GetBlock(main.Tip.HashBlock).HashBlock, c.GetBlock(c.Tip).Hash);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanPersistMainchain()
		{
			var main = new ConcurrentChain(LoadMainChain(), Network.Main);
			MemoryStream ms = new MemoryStream();
			main.WriteTo(ms);
			ms.Position = 0;
			main.SetTip(main.Genesis);
			main.Load(ms);
			Assert.True(main.Tip.HasHeader);

			var original = main;

			foreach (var options in new[]{
				new ConcurrentChain.ChainSerializationFormat()
				{
					SerializeBlockHeader = true,
					SerializePrecomputedBlockHash = true,
				},
				new ConcurrentChain.ChainSerializationFormat()
				{
					SerializeBlockHeader = true,
					SerializePrecomputedBlockHash = false,
				},
				new ConcurrentChain.ChainSerializationFormat()
				{
					SerializeBlockHeader = false,
					SerializePrecomputedBlockHash = true,
				}
			})
			{
				main = new ConcurrentChain();
				main.SetTip(original.Tip);
				ms = new MemoryStream();
				main.WriteTo(ms, options);
				ms.Position = 0;
				main.SetTip(main.Genesis);
				main.Load(ms, Network.Main, options);
				Assert.Equal(options.SerializeBlockHeader, main.Tip.HasHeader);
				if (main.Tip.HasHeader)
				{
					Assert.True(main.Tip.TryGetHeader(out var unused));
				}
				else
				{
					Assert.False(main.Tip.TryGetHeader(out var unused));
					Assert.Throws<InvalidOperationException>(() => main.Tip.Header);
				}
				Assert.Equal(original.Tip.HashBlock, main.Tip.HashBlock);
			}

			Assert.Throws<InvalidOperationException>(() =>
			{
				main.WriteTo(new MemoryStream(), new ConcurrentChain.ChainSerializationFormat()
				{
					SerializeBlockHeader = false,
					SerializePrecomputedBlockHash = false,
				});
			});
		}

		private byte[] LoadMainChain()
		{
			if (!File.Exists("MainChain1.dat"))
			{
				HttpClient client = new HttpClient();
				var bytes = client.GetByteArrayAsync("https://aois.blob.core.windows.net/public/MainChain1.dat").GetAwaiter().GetResult();
				File.WriteAllBytes("MainChain1.dat", bytes);
			}
			return File.ReadAllBytes("MainChain1.dat");
		}
#endif

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanEnumerateAfterChainedBlock()
		{
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
			AppendBlock(chain);
			var a = AppendBlock(chain);
			var b = AppendBlock(chain);
			var c = AppendBlock(chain);

			Assert.True(chain.EnumerateAfter(a).SequenceEqual(new[] { b, c }));

			var d = AppendBlock(chain);

			var enumerator = chain.EnumerateAfter(b).GetEnumerator();
			enumerator.MoveNext();
			Assert.True(enumerator.Current == c);

			chain.SetTip(b);
			var cc = AppendBlock(chain);
			var dd = AppendBlock(chain);

			Assert.False(enumerator.MoveNext());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildChain2()
		{
			ConcurrentChain chain = CreateChain(10);
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
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
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
			ConcurrentChain chain = CreateChain(10);
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
			ConcurrentChain side = new ConcurrentChain(Network.Main);
			ConcurrentChain main = new ConcurrentChain(Network.Main);
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
			var genesis = TestUtils.CreateFakeBlock();
			ConcurrentChain side = new ConcurrentChain(genesis.Header);
			ConcurrentChain main = new ConcurrentChain(genesis.Header);
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

		private ConcurrentChain CreateChain(int height)
		{
			return CreateChain(TestUtils.CreateFakeBlock().Header, height);
		}

		private ConcurrentChain CreateChain(BlockHeader genesis, int height)
		{
			var chain = new ConcurrentChain(genesis);
			for (int i = 0; i < height; i++)
			{
				var b = TestUtils.CreateFakeBlock();
				b.Header.HashPrevBlock = chain.Tip.HashBlock;
				chain.SetTip(b.Header);
			}
			return chain;
		}


		public ChainedBlock AppendBlock(ChainedBlock previous, params ConcurrentChain[] chains)
		{
			ChainedBlock last = null;
			var nonce = RandomUtils.GetUInt32();
			foreach (var chain in chains)
			{
				var block = TestUtils.CreateFakeBlock(Network.Main.CreateTransaction());
				block.Header.HashPrevBlock = previous == null ? chain.Tip.HashBlock : previous.HashBlock;
				block.Header.Nonce = nonce;
				if (!chain.TrySetTip(block.Header, out last))
					throw new InvalidOperationException("Previous not existing");
			}
			return last;
		}
		private ChainedBlock AppendBlock(params ConcurrentChain[] chains)
		{
			ChainedBlock index = null;
			return AppendBlock(index, chains);
		}
	}
}
