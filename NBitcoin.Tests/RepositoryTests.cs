#if !NOFILEIO
using NBitcoin.BitcoinCore;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using NBitcoin.Stealth;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
		public void CanValidateBlocks()
		{
			foreach(var block in StoredBlock.EnumerateFolder(@"data\blocks"))
			{
				ValidationState validation = Network.Main.CreateValidationState();
				validation.Now = block.Item.Header.BlockTime;
				Assert.True(validation.CheckBlock(block.Item));
			}
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
			var chain = store.BuildChain();
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
			var genesis = index.Get(new uint256("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"));
			Assert.NotNull(genesis);
			var invalidBlock = index.Get(new uint256("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26e"));
			Assert.Null(invalidBlock);
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


			var test = new IndexedBlockStore(new SQLiteNoSqlRepository("CanReIndex", true), store);
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
		public static void CanRequestBlockr()
		{
			var repo = new BlockrTransactionRepository(Network.Main);
			var result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de");
			Assert.NotNull(result);
			Assert.Equal("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de", result.GetHash().ToString());

			result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927df");
			Assert.Null(result);

			repo = new BlockrTransactionRepository(Network.TestNet);
			result = repo.Get("7d4c5d69a85c70ff70daff789114b9b76fb6d2613ac18764bd96f0a2b9358782");
			Assert.NotNull(result);
		}
		[Fact]
		public static void CanRequestTransactionOnRapidBase()
		{
			var repo = new RapidBaseTransactionRepository("http://rapidbase-test.azurewebsites.net/");
			var result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de");
			Assert.NotNull(result);
			Assert.Equal("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de", result.GetHash().ToString());

			result = repo.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927df");
			Assert.Null(result);
		}

		//[Fact]
		public static void Play()
		{
			//var node = Node.ConnectToLocal(Network.Main);
			//node.VersionHandshake();
			//var chain = node.GetChain();
			//var v3 = chain.Tip
			//	.EnumerateToGenesis()
			//	.Take(1000)
			//	.Aggregate(0, (a, b) => b.Header.Version == 3 ? a+1 : a);

			//var r = (double)v3 / (double)1000;

			Stopwatch watch = new Stopwatch();
			watch.Start();
			System.Net.ServicePointManager.DefaultConnectionLimit = 100;
			System.Net.ServicePointManager.Expect100Continue = false;
			var repo = new RapidBaseTransactionRepository("http://rapidbase-test.azurewebsites.net/");
			var colored = new OpenAsset.NoSqlColoredTransactionRepository(repo);

			
			var result = repo
				.Get("c3462373f1a722c66cbb1b93712df94aa7b3731f4142cd8413f10c9e872927de")
				.GetColoredTransaction(colored);
			watch.Stop();
			//for(int i = 0 ; i < 100 ; i++)
			//{
			//	using(var node = Node.ConnectToLocal(Network.Main))
			//	{
			//		node.VersionHandshake();
			//		var chain = new ConcurrentChain(Network.Main);
			//		node.SynchronizeChain(chain);
			//	}
			//}
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
			var cached = new CachedNoSqlRepository(CreateNoSqlRepository());
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
				CreateNoSqlRepository(),
				new InMemoryNoSqlRepository(),
				new CachedNoSqlRepository(CreateNoSqlRepository("CanStoreInNoSqlCached"))
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

		private SQLiteNoSqlRepository CreateNoSqlRepository([CallerMemberName]string filename = null)
		{
			if(File.Exists(filename))
				File.Delete(filename);
			return new SQLiteNoSqlRepository(filename);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanStorePeers()
		{
			SqLitePeerTableRepository repository = CreateTableRepository();
			CanStorePeer(repository);
			CanStorePeer(new InMemoryPeerTableRepository());
		}


		private static void CanStorePeer(PeerTableRepository repository)
		{
			var peer = new Peer(PeerOrigin.Addr, new NetworkAddress()
			{
				Endpoint = new IPEndPoint(IPAddress.Parse("0.0.1.0").MapToIPv6(), 110),
				Time = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5)
			});
			repository.WritePeer(peer);
			var result = repository.GetPeers().ToArray();
			Assert.Equal(1, result.Length);
			Assert.Equal(PeerOrigin.Addr, result[0].Origin);
			Assert.Equal(IPAddress.Parse("0.0.1.0").MapToIPv6(), result[0].NetworkAddress.Endpoint.Address);
			Assert.Equal(110, result[0].NetworkAddress.Endpoint.Port);
			Assert.Equal(peer.NetworkAddress.Time, result[0].NetworkAddress.Time);

			peer.NetworkAddress.Time = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5);
			repository.WritePeer(peer);
			result = repository.GetPeers().ToArray();
			Assert.Equal(1, result.Length);
			Assert.Equal(peer.NetworkAddress.Time, result[0].NetworkAddress.Time);

			var peer2 = new Peer(PeerOrigin.Addr, new NetworkAddress()
			{
				Endpoint = new IPEndPoint(IPAddress.Parse("0.0.2.0").MapToIPv6(), 110),
				Time = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5)
			});

			repository.WritePeers(new Peer[] { peer, peer2 });
			Assert.Equal(2, repository.GetPeers().ToArray().Length);

			peer.NetworkAddress.Time = Utils.UnixTimeToDateTime(0);
			repository.WritePeer(peer);
			result = repository.GetPeers().ToArray();
			Assert.Equal(1, result.Length);
			Assert.Equal(IPAddress.Parse("0.0.2.0").MapToIPv6(), result[0].NetworkAddress.Endpoint.Address);
		}

		private SqLitePeerTableRepository CreateTableRepository([CallerMemberName]string filename = null)
		{
			if(File.Exists(filename))
				File.Delete(filename);
			return new SqLitePeerTableRepository(filename);
		}


		public static IndexedBlockStore CreateIndexedStore([CallerMemberName]string folderName = null)
		{
			TestUtils.EnsureNew(folderName);
			return new IndexedBlockStore(new SQLiteNoSqlRepository(Path.Combine(folderName, "Index")), new BlockStore(folderName, Network.Main));
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