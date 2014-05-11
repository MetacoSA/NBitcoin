using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public void CanReadStoredBlockFile()
		{
			int count = 0;

			foreach(var stored in StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat"))
			{
				Assert.True(stored.Block.Header.CheckProofOfWork());
				Assert.True(stored.Block.CheckMerkleRoot());
				count++;
			}
			Assert.Equal(300, count);
			count = 0;
			foreach(var stored in StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat", new DiskBlockPosRange(new DiskBlockPos(0, 298))))
			{
				count++;
			}
			Assert.Equal(2, count);
		}

		[Fact]
		public void CanIndexBlock()
		{
			var index = CreateIndexedStore();
			foreach(var block in StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").Take(50))
			{
				index.Put(block.Block);
			}
			var genesis = index.Get(new uint256("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"));
			Assert.NotNull(genesis);
			var invalidBlock = index.Get(new uint256("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26e"));
			Assert.Null(invalidBlock);
		}

		[Fact]
		public void CanStoreBlocks()
		{
			var store = CreateBlockStore();
			var allBlocks = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").Take(50).ToList();

			foreach(var s in allBlocks)
			{
				store.Append(s.Block);
			}
			var storedBlocks = store.Enumerate(true).ToList();
			Assert.Equal(allBlocks.Count, storedBlocks.Count);

			foreach(var s in allBlocks)
			{
				var retrieved = store.Enumerate(true).First(b => b.BlockPosition == s.BlockPosition);
				Assert.True(retrieved.Block.HeaderOnly);
			}
		}

		[Fact]
		public void CanReIndex()
		{
			var source = new BlockStore(@"data\blocks", Network.Main);
			var store = CreateBlockStore("CanReIndexFolder");
			store.AppendAll(source.Enumerate(false).Take(100).Select(b => b.Block));


			var test = new IndexedBlockStore(new SQLiteNoSqlRepository("CanReIndex", true), store);
			var reIndexed = test.ReIndex();
			Assert.Equal(100, reIndexed);
			int i = 0;
			foreach(var b in store.Enumerate(true))
			{
				var result = test.Get(b.Block.GetHash());
				Assert.Equal(result.GetHash(), b.Block.GetHash());
				i++;
			}
			Assert.Equal(100, i);

			var last = source.Enumerate(false).Skip(100).FirstOrDefault();
			store.Append(last.Block);

			reIndexed = test.ReIndex();
			Assert.Equal(1, reIndexed);

			reIndexed = test.ReIndex();
			Assert.Equal(0, reIndexed);
		}

		[Fact]
		public static void Play()
		{
			//var test = new IndexedBlockStore(new SQLiteNoSqlRepository("Play", true), new BlockStore(@"E:\Bitcoin\blocks", Network.Main));
			//test.ReIndex();
			//var i = 0;
			//Stopwatch watch = new Stopwatch();
			//watch.Start();
			//foreach(var b in test.Store.Enumerate(false, new DiskBlockPosRange(new DiskBlockPos(137,0))).Take(144))
			//{
			//	i++;
			//}
			//watch.Stop();
		}

		[Fact]
		public void CanStoreInBlockRepository()
		{
			var blockRepository = CreateBlockRepository();
			var firstblk1 = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").First();
			blockRepository.WriteBlockHeader(firstblk1.Block.Header);
			var result = blockRepository.GetBlock(firstblk1.Block.GetHash());
			Assert.True(result.HeaderOnly);

			blockRepository.WriteBlock(firstblk1.Block);
			result = blockRepository.GetBlock(firstblk1.Block.GetHash());
			Assert.False(result.HeaderOnly);
		}


		[Fact]
		public void CanReadStoredBlockFolder()
		{
			var firstblk1 = StoredBlock.EnumerateFile(@"data\blocks\blk00000.dat").First();
			var firstblk2 = StoredBlock.EnumerateFile(@"data\blocks\blk00001.dat").First();

			int count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks"))
			{
				if(count == 0)
					Assert.Equal(firstblk1.Block.GetHash(), stored.Block.GetHash());
				if(count == 300)
					Assert.Equal(firstblk2.Block.GetHash(), stored.Block.GetHash());
				Assert.True(stored.Block.Header.CheckProofOfWork());
				Assert.True(stored.Block.CheckMerkleRoot());
				count++;
			}
			Assert.Equal(600, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks", new DiskBlockPosRange(new DiskBlockPos(1, 298))))
			{
				count++;
			}
			Assert.Equal(2, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks", new DiskBlockPosRange(new DiskBlockPos(0, 298))))
			{
				count++;
			}
			Assert.Equal(302, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks", new DiskBlockPosRange(new DiskBlockPos(0, 298), new DiskBlockPos(1, 2))))
			{
				count++;
			}
			Assert.Equal(4, count);

			count = 0;
			foreach(var stored in StoredBlock.EnumerateFolder(@"data\blocks", new DiskBlockPosRange(new DiskBlockPos(0, 30), new DiskBlockPos(0, 34))))
			{
				count++;
			}
			Assert.Equal(4, count);
		}

		[Fact]
		public void CanStoreInSQLiteNoSql()
		{
			SQLiteNoSqlRepository repository = CreateNoSqlRepository();
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

		private SQLiteNoSqlRepository CreateNoSqlRepository([CallerMemberName]string filename = null)
		{
			if(File.Exists(filename))
				File.Delete(filename);
			return new SQLiteNoSqlRepository(filename);
		}

		[Fact]
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
			if(Directory.Exists(folderName))
				Directory.Delete(folderName, true);
			Thread.Sleep(50);
			Directory.CreateDirectory(folderName);
			Thread.Sleep(50);
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

		private BlockRepository CreateBlockRepository([CallerMemberName]string folderName = null)
		{
			return new BlockRepository(CreateIndexedStore(folderName + "-Blocks"), CreateIndexedStore(folderName + "-Headers"));
		}
	}
}
