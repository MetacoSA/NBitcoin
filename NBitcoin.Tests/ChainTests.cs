﻿using NBitcoin.Protocol;
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
		public void CanSaveChain()
		{
			ConcurrentChain chain = new ConcurrentChain(Network.Main);
			AppendBlock(chain);
			AppendBlock(chain);
			var fork = AppendBlock(chain);
			AppendBlock(chain);



			var chain2 = new ConcurrentChain(chain.ToBytes());
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
			for(int i = 0; i < 600; i++)
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
			cchain.Load(bytes);

			Assert.Equal(cchain.Tip, chain.Tip);
			Assert.NotNull(cchain.GetBlock(0));

			cchain = new ConcurrentChain(Network.TestNet);
			cchain.Load(cchain.ToBytes());
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

		private ChainedBlock AddBlock(ConcurrentChain chain)
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
		public void CanCalculateDifficulty()
		{
			var main = new ConcurrentChain(LoadMainChain());
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

		private byte[] LoadMainChain()
		{
			if(!File.Exists("MainChain1.dat"))
			{
				WebClient client = new WebClient();
				client.DownloadFile("https://aois.blob.core.windows.net/public/MainChain1.dat", "MainChain1.dat");
			}
			return File.ReadAllBytes("MainChain1.dat");
		}
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
			for(int i = 0; i < height; i++)
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
		private ChainedBlock AppendBlock(params ConcurrentChain[] chains)
		{
			ChainedBlock index = null;
			return AppendBlock(index, chains);
		}
	}
}
