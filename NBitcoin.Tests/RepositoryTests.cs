#if !NOFILEIO
using NBitcoin.BitcoinCore;
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
		public void CanReadStoredBlockFile()
		{
			int count = 0;

			foreach(var stored in StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat"))
			{
				Assert.True(stored.Item.Header.CheckProofOfWork());
				Assert.True(stored.Item.CheckMerkleRoot());
				count++;
			}
			Assert.Equal(300, count);
			count = 0;
			var twoLast = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").Skip(298).ToList();
			foreach(var stored in StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat", range: new DiskBlockPosRange(twoLast[0].BlockPosition)))
			{
				count++;
			}
			Assert.Equal(2, count);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanEnumerateBlockCountRange()
		{
			var store = new BlockStore(@"data\blocks", Network.Main);
			var expectedBlock = store.Enumerate(false).Skip(4).First();
			var actualBlocks = store.Enumerate(false, 4, 2).ToArray();
			Assert.Equal(2, actualBlocks.Length);
			Assert.Equal(expectedBlock.Item.Header.GetHash(), actualBlocks[0].Item.Header.GetHash());
			Assert.True(actualBlocks[0].Item.CheckMerkleRoot());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanEnumerateBlockInAFileRange()
		{
			var store = new BlockStore(@"data\blocks", Network.Main);
			var result = store.Enumerate(new DiskBlockPosRange(new DiskBlockPos(0, 0), new DiskBlockPos(1, 0))).ToList();
			Assert.Equal(300, result.Count);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//The last block is off by 1 byte + lots of padding zero at the end
		public void CanEnumerateIncompleteBlk()
		{
			Assert.Equal(301, StoredBlock.EnumerateFile(@"data\blocks\incompleteblk.dat").Count());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildChainFromBlocks()
		{
			var store = new BlockStore(@"data\blocks", Network.Main);
			var chain = store.GetChain();
			Assert.True(chain.Height == 599);

		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIndexBlock()
		{
			var index = CreateIndexedStore();
			foreach(var block in StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").Take(50))
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
		public void CanStoreBlocks()
		{
			var store = CreateBlockStore();
			var allBlocks = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").Take(50).ToList();

			foreach(var s in allBlocks)
			{
				store.Append(s.Item);
			}
			var storedBlocks = store.Enumerate(true).ToList();
			Assert.Equal(allBlocks.Count, storedBlocks.Count);

			foreach(var s in allBlocks)
			{
				var retrieved = store.Enumerate(true).First(b => b.BlockPosition == s.BlockPosition);
				Assert.True(retrieved.Item.HeaderOnly);
			}
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanStoreBlocksInMultipleFiles()
		{
			var store = CreateBlockStore();
			store.MaxFileSize = 10; //Verify break all block in one respective file with extreme settings
			var allBlocks = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").Take(10).ToList();
			foreach(var s in allBlocks)
			{
				store.Append(s.Item);
			}
			var storedBlocks = store.Enumerate(true).ToList();
			Assert.Equal(allBlocks.Count, storedBlocks.Count);
			Assert.Equal(11, store.Folder.GetFiles().Length); //10 files + lock file
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReIndex()
		{
			var source = new BlockStore(@"data\blocks", Network.Main);
			var store = CreateBlockStore("CanReIndexFolder");
			store.AppendAll(source.Enumerate(false).Take(100).Select(b => b.Item));


			var test = new IndexedBlockStore(new InMemoryNoSqlRepository(), store);
			var reIndexed = test.ReIndex();
			Assert.Equal(100, reIndexed);
			int i = 0;
			foreach(var b in store.Enumerate(true))
			{
				var result = test.Get(b.Item.GetHash());
				Assert.Equal(result.GetHash(), b.Item.GetHash());
				i++;
			}
			Assert.Equal(100, i);

			var last = source.Enumerate(false).Skip(100).FirstOrDefault();
			store.Append(last.Item);

			reIndexed = test.ReIndex();
			Assert.Equal(1, reIndexed);

			reIndexed = test.ReIndex();
			Assert.Equal(0, reIndexed);
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void CanParseRev()
		{
			BlockUndoStore src = new BlockUndoStore(@"data\blocks", Network.Main);
			BlockUndoStore dest = CreateBlockUndoStore();
			int count = 0;
			foreach(var un in src.EnumerateFolder())
			{
				var expectedSize = un.Header.ItemSize;
				var actualSize = (uint)un.Item.GetSerializedSize();
				Assert.Equal(expectedSize, actualSize);
				dest.Append(un.Item);
				count++;
			}
			Assert.Equal(40, count);

			count = 0;
			foreach(var un in dest.EnumerateFolder())
			{
				var expectedSize = un.Header.ItemSize;
				var actualSize = (uint)un.Item.GetSerializedSize();
				Assert.Equal(expectedSize, actualSize);
				count++;
			}
			Assert.Equal(40, count);
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void CanRequestBlockr()
		{
			var repo = new BlockrTransactionRepository(Network.Main);
			var result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de");
			Assert.NotNull(result);
			Assert.Equal("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de", result.GetHash().ToString());

			result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927df");
			Assert.Null(result);

			var unspent = repo.GetUnspentAsync("1KF8kUVHK42XzgcmJF4Lxz4wcL5WDL97PB").Result;
			Assert.True(unspent.Count != 0);

			repo = new BlockrTransactionRepository(Network.TestNet);
			result = repo.Get("7d4c5d69a85c70ff70daff789114b9b76fb6d2613ac18764bd96f0a2b9358782");
			Assert.NotNull(result);

			unspent = repo.GetUnspentAsync("2N66DDrmjDCMM3yMSYtAQyAqRtasSkFhbmX").Result;
			Assert.True(unspent.Count != 0);
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void CanPushTxBlockr()
		{
			var repo = new BlockrTransactionRepository(Network.Main);
			var result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de");
			Assert.NotNull(result);
			Assert.Equal("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de", result.GetHash().ToString());

			var pushPath = BlockrTransactionRepository.BroadcastPath;

			try
			{
				BlockrTransactionRepository.BroadcastPath = "tx/decode";
				repo.BroadcastAsync(result).GetAwaiter().GetResult();
			}
			finally
			{
				BlockrTransactionRepository.BroadcastPath = pushPath;
			}
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

		[Fact]
		public void Play()
		{
			var tx = new Transaction("0100000001c68e0b29cd00bc5557d81844c8b9145b1e67006572f4801872eba1a4c10c792f00000000fdae01004daa0174608763a6142a05e92814a7698d96737968e36068f2e068497588a614ec699defb1dca11d73583b98d5016542b51f158788a61470bdebf38acd970afa88e364b2a80ef7cb6bff4488a614e6c9eabf1b9428f96fa4224d14ca4e2de14ce8db88a614f476d430b98e90ff59de08d7f35fc7313a4d926788a61406b27071338ff1732f7292ee43b4b87cce524d8f88a61426f54b93dfedf83c159bebfb73a6dc6751bf6b4d88a6140c88a93da127c4a756e769b68dd8ecbac76222a888a61452bf85e329e9a74f90abb71d512869715a68ce9788a614b5fdc1662b345d9033879179427b57286317a3a388a614e40e1311303e4681932f4c3af9a87b44176ffefe88a61445b1b74fb6abdce74ee1bb338415b3b056b724f988a614a39f2e05a24216648139a7f1538ef6de962b201488a614d2346f225dded92c187bc30357d15e91e02f04b188a614132edc21acfb3eaac2f46c54824c22fa3a1ac5dc8821030aec375b5d49baaa8966037d639d07d385e2b5c2d09c1ccbcf022ed43ad4a24667036da410b1752103b26d75ce682232e946f3647967e115a544ea05a960ae6fcff6d052d9ec8cad4d68ac0000000001a4bf0406000000001976a9144e932520f9b5ead95a3227944a7325bc0680a90788ac6da41000");

			var prev = new Transaction("01000000017bffaa7dca6d0722ddef74cb2a42d13e45f6e7ea92f7cec844fe35fde5b556d201000000fd0a0100483045022100d0541dc318b1c1c900cf80353de1abd18c4ec2457ad6f940a648c2b95c4b45a602202add27f6e825551b9a052cba74bb27264ab1a18e8865490d87d14247da52e54b01473044022051e0d254c43ddfd229902803bec6010ff6f1beb65fe5ef61688c22a724fbe2f1022077d171c054cca280c4b8331c8a4e744ca7eb2d61ce6e06eb4c961e11d8cb0623014c76745387635221029c9b96dd3bb1f8c79dbc5402cf692e698d1fed8c1ddb42d4e4385a0b8e7e1aae2103b8a6758106d44ee9042f562edf57951ce3fd8f29b8f1d7d9b9eec747f335d0d852ae67036da410b1752103b26d75ce682232e946f3647967e115a544ea05a960ae6fcff6d052d9ec8cad4dac68ffffffff01dedf04060000000017a914e6bd2395b1fbcf2afba03efc46dfa1bd5d2f2f4a8700000000");

			ScriptError err;
			tx.Inputs.AsIndexedInputs().First().VerifyScript(prev.Outputs.AsCoins().ToArray()[0], out err);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanStoreInBlockRepository()
		{
			var blockRepository = CreateBlockRepository();
			var firstblk1 = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").First();
			blockRepository.WriteBlockHeader(firstblk1.Item.Header);
			var result = blockRepository.GetBlock(firstblk1.Item.GetHash());
			Assert.True(result.HeaderOnly);

			blockRepository.WriteBlock(firstblk1.Item);
			result = blockRepository.GetBlock(firstblk1.Item.GetHash());
			Assert.False(result.HeaderOnly);
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadStoredBlockFolder()
		{
			var blk0 = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat", (uint)0).ToList();
			var blk1 = StoredBlock.EnumerateFile(@"data\blocks\blk00001.dat", (uint)1).ToList();

			int count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks"))
			{
				if(count == 0)
					Assert.Equal(blk0[0].Item.GetHash(), stored.Item.GetHash());
				if(count == 300)
					Assert.Equal(blk1[0].Item.GetHash(), stored.Item.GetHash());
				Assert.True(stored.Item.Header.CheckProofOfWork());
				Assert.True(stored.Item.CheckMerkleRoot());
				count++;
			}
			Assert.Equal(600, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks", new DiskBlockPosRange(blk1[298].BlockPosition)))
			{
				count++;
			}
			Assert.Equal(2, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks", new DiskBlockPosRange(blk0[298].BlockPosition)))
			{
				count++;
			}
			Assert.Equal(302, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks",
														new DiskBlockPosRange(blk0[298].BlockPosition, blk1[2].BlockPosition)))
			{
				count++;
			}
			Assert.Equal(4, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks", new DiskBlockPosRange(blk0[30].BlockPosition, blk0[34].BlockPosition)))
			{
				count++;
			}
			Assert.Equal(4, count);
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

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanStoreInNoSql()
		{
			var repositories = new NoSqlRepository[]
			{
				new InMemoryNoSqlRepository(),
				new CachedNoSqlRepository(new InMemoryNoSqlRepository())
			};

			foreach(var repository in repositories)
			{
				byte[] data1 = new byte[] { 1, 2, 3, 4, 5, 6 };
				byte[] data2 = new byte[] { 11, 22, 33, 4, 5, 66 };
				Assert.Null(repository.Get<RawData>("data1"));

				repository.Put("data1", new RawData(data1));
				var actual = repository.Get<RawData>("data1");
				Assert.NotNull(actual);
				AssertEx.CollectionEquals(actual.Data, data1);

				repository.Put("data1", new RawData(data2));
				actual = repository.Get<RawData>("data1");
				Assert.NotNull(actual);
				AssertEx.CollectionEquals(actual.Data, data2);

				repository.Put("data1", null as RawData);
				actual = repository.Get<RawData>("data1");
				Assert.Null(actual);

				repository.Put("data1", null as RawData);
				actual = repository.Get<RawData>("data1");
				Assert.Null(actual);

				//Test batch
				repository.PutBatch(new[] {new Tuple<string,IBitcoinSerializable>("data1",new RawData(data1)),
									   new Tuple<string,IBitcoinSerializable>("data2",new RawData(data2))});

				actual = repository.Get<RawData>("data1");
				Assert.NotNull(actual);
				AssertEx.CollectionEquals(actual.Data, data1);

				actual = repository.Get<RawData>("data2");
				Assert.NotNull(actual);
				AssertEx.CollectionEquals(actual.Data, data2);
			}
		}


		private static BlockStore CreateBlockStore([CallerMemberName]string folderName = null)
		{
			if(Directory.Exists(folderName))
				Directory.Delete(folderName, true);
			Thread.Sleep(50);
			Directory.CreateDirectory(folderName);
			Thread.Sleep(50);
			return new BlockStore(folderName, Network.Main);
		}
		private static BlockUndoStore CreateBlockUndoStore([CallerMemberName]string folderName = null)
		{
			TestUtils.EnsureNew(folderName);
			return new BlockUndoStore(folderName, Network.Main);
		}

		private BlockRepository CreateBlockRepository([CallerMemberName]string folderName = null)
		{
			return new BlockRepository(CreateIndexedStore(folderName + "-Blocks"), CreateIndexedStore(folderName + "-Headers"));
		}

	}
}
#endif