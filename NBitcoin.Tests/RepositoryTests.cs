#if !NOFILEIO
using NBitcoin.BitcoinCore;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.RPC;
using NBitcoin.SPV;
using NBitcoin.Stealth;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class RepositoryTests
	{
		public class RawData : IBitcoinSerializable
		{
			public RawData()
			{

			}
			public RawData(byte[] data)
			{
				_Data = data;
			}
			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWriteAsVarString(ref _Data);
			}

			private byte[] _Data = new byte[0];
			public byte[] Data
			{
				get
				{
					return _Data;
				}
			}

			#endregion
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanEnumerateBlockCountRange()
		{
			var store = new BlockStore(@"data/blocks", Network.Main);
			var expectedBlock = store.Enumerate(false).Skip(4).First();
			var actualBlocks = store.Enumerate(false, 4, 2).ToArray();
			Assert.Equal(2, actualBlocks.Length);
			Assert.Equal(expectedBlock.Item.Header.GetHash(), actualBlocks[0].Item.Header.GetHash());
			Assert.True(actualBlocks[0].Item.CheckMerkleRoot());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//The last block is off by 1 byte + lots of padding zero at the end
		public void CanEnumerateIncompleteBlk()
		{
			Assert.Equal(301, StoredBlock.EnumerateFile(@"data/blocks/incompleteblk.dat").Count());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildChainFromBlocks()
		{
			var store = new BlockStore(@"data/blocks", Network.Main);
			var chain = store.GetChain();
			Assert.True(chain.Height == 599);

		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIndexBlock()
		{
			var index = CreateIndexedStore();
			foreach(var block in StoredBlock.EnumerateFile(@"data/blocks/blk00000.dat").Take(50))
			{
				index.Put(block.Item);
			}
			var genesis = index.Get(uint256.Parse("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"));
			Assert.NotNull(genesis);
			var invalidBlock = index.Get(uint256.Parse("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26e"));
			Assert.Null(invalidBlock);
		}


		public static IndexedBlockStore CreateIndexedStore([CallerMemberName]string folderName = null)
		{
			TestUtils.EnsureNew(folderName);
			return new IndexedBlockStore(new InMemoryNoSqlRepository(), new BlockStore(folderName, Network.Main));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void CanRequestTransactionOnQBit()
		{
			var repo = new QBitNinjaTransactionRepository(Network.Main);
			var result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de");
			Assert.NotNull(result);
			Assert.Equal("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de", result.GetHash().ToString());

			result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927df");
			Assert.Null(result);

			repo = new QBitNinjaTransactionRepository(Network.TestNet);
			result = repo.Get("7d4c5d69a85c70ff70daff789114b9b76fb6d2613ac18764bd96f0a2b9358782");
			Assert.NotNull(result);
		}

		private static Coin RandomCoin(Key bob, Money amount, bool p2pkh = false)
		{
			return new Coin(new uint256(Enumerable.Range(0, 32).Select(i => (byte)0xaa).ToArray()), 0, amount, p2pkh ? bob.PubKey.Hash.ScriptPubKey : bob.PubKey.WitHash.ScriptPubKey);
		}
		private static Coin RandomCoin2(Key bob, Money amount, bool p2pkh = false)
		{
			return new Coin(new uint256(RandomUtils.GetBytes(32)), 0, amount, p2pkh ? bob.PubKey.Hash.ScriptPubKey : bob.PubKey.WitHash.ScriptPubKey);
		}

		[Fact]
		public void Play()
		{
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildTransactionWithSubstractFeeAndSendEstimatedFees()
		{
			var signer = new Key();
			var builder = new TransactionBuilder();
			builder.AddKeys(signer);
			builder.AddCoins(RandomCoin(signer, Money.Coins(1)));
			builder.Send(new Key().ScriptPubKey, Money.Coins(1));
			builder.SubtractFees();
			builder.SendEstimatedFees(new FeeRate(Money.Satoshis(100), 1));
			var result = builder.BuildTransaction(true);
			Assert.Equal(Money.Coins(0.00011300m), result.GetFee(builder.FindSpentCoins(result)));
		}



		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TwoGroupsCanSendToSameDestination()
		{
			var alice = new Key();
			var carol = new Key();
			var bob = new Key();

			var builder = new TransactionBuilder();
			builder.StandardTransactionPolicy.CheckFee = false;
			Transaction tx = builder
				.AddCoins(RandomCoin2(alice, Money.Coins(1.0m)))
				.AddKeys(alice)
				.Send(bob, Money.Coins(0.3m))
				.SetChange(alice)
				.Then()
				.AddCoins(RandomCoin2(carol, Money.Coins(1.1m)))
				.AddKeys(carol)
				.Send(bob, Money.Coins(0.1m))
				.SetChange(carol)
				.BuildTransaction(sign: true);

			Assert.Equal(2, tx.Inputs.Count);
			Assert.Equal(3, tx.Outputs.Count);
			Assert.Equal(1, tx.Outputs
								.Where(o => o.ScriptPubKey == bob.ScriptPubKey)
								.Where(o => o.Value == Money.Coins(0.3m) + Money.Coins(0.1m))
								.Count());
			Assert.Equal(1, tx.Outputs
							  .Where(o => o.ScriptPubKey == alice.ScriptPubKey)
							  .Where(o => o.Value == Money.Coins(0.7m))
							  .Count());
			Assert.Equal(1, tx.Outputs
								.Where(o => o.ScriptPubKey == carol.ScriptPubKey)
								.Where(o => o.Value == Money.Coins(1.0m))
								.Count());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCacheNoSqlRepository()
		{
			var cached = new CachedNoSqlRepository(new InMemoryNoSqlRepository());
			byte[] data1 = new byte[] { 1, 2, 3, 4, 5, 6 };
			byte[] data2 = new byte[] { 11, 22, 33, 4, 5, 66 };
			cached.InnerRepository.Put("data1", new RawData(data1));
			Assert.NotNull(cached.Get<RawData>("data1"));
			cached.InnerRepository.Put("data1", new RawData(data2));
			cached.Flush();
			var data1Actual = cached.InnerRepository.Get<RawData>("data1");
			AssertEx.CollectionEquals(data1Actual.Data, data2);
			cached.Put("data1", new RawData(data1));

			data1Actual = cached.InnerRepository.Get<RawData>("data1");
			AssertEx.CollectionEquals(data1Actual.Data, data2);

			cached.Flush();

			data1Actual = cached.InnerRepository.Get<RawData>("data1");
			AssertEx.CollectionEquals(data1Actual.Data, data1);

			cached.Put("data1", null);
			cached.Flush();
			Assert.Null(cached.InnerRepository.Get<RawData>("data1"));

			cached.Put("data1", new RawData(data1));
			cached.Put("data1", null);
			cached.Flush();
			Assert.Null(cached.InnerRepository.Get<RawData>("data1"));

			cached.Put("data1", null);
			cached.Put("data1", new RawData(data1));
			cached.Flush();
			Assert.NotNull(cached.InnerRepository.Get<RawData>("data1"));
		}
	}
}
#endif