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

        //[Fact]
        //[Trait("UnitTest", "UnitTest")]
        public void EnumerateRawStratisBlockcahinAndWriteResultsToFile()
        {
            var inserts = this.ManuallyEnumerateTheBlockchainFile();

            List<string> fileInserts = new List<string>();
            foreach (var blockHex in inserts.Where(s => !string.IsNullOrEmpty(s)))
            {
                fileInserts.Add(blockHex);
                var rem = blockHex.Substring(8);// the magic bytes
                var bt = Encoders.Hex.DecodeData(rem); // pars to bytes
                var size = BitConverter.ToUInt32(bt, 0); // first 4 bytes are a uint size 
                var b = new Block(bt.Skip(4).ToArray()); // create the block
                var str = $"hash={b.GetHash()} ver={b.Header.Version} size={size} nonc={b.Header.Nonce} bits={b.Header.Bits} prv={b.Header.HashPrevBlock} mrk={b.Header.HashMerkleRoot}";
                fileInserts.Add(str);
            }
            var pathFile = @"C:\StratisData\compare-blocks.txt";
            File.WriteAllLines(pathFile, fileInserts);
        }

        private IEnumerable<string> ManuallyEnumerateTheBlockchainFile()
        {
            // read all bytes form the first block file
            // to get a copy of the blockchain fiel without downloading follow this link
            // https://1drv.ms/f/s!AE-fao7cBVQ-aQ
            var byts = File.ReadAllBytes(@"C:\StratisData\blk0001.dat");
            // the magic byte separator of blocks
            var m = new byte[4] { 0x70, 0x35, 0x22, 0x05 };
            // first bytes must be magic
            Assert.True(m[0] == byts[0] && m[1] == byts[1] && m[2] == byts[2] && m[3] == byts[3]);
            // enumerate over all the bytes and separate the blocks to hex representations
            var current = new List<byte>();
            for (int i = 1; i < byts.Length; i++) // start from 1 to skip first check
            {
                // check for the magic byte
                if ((m[0] == byts[i] &&
                    m[1] == byts[i + 1] &&
                    m[2] == byts[i + 2] &&
                    m[3] == byts[i + 3]))
                {
                    // if we reached the magic byte we got to the end of the block
                    yield return Encoders.Hex.EncodeData(current.ToArray());
                    current.Clear();
                }
                current.Add(byts[i]);
            }
            // read the last block
            yield return Encoders.Hex.EncodeData(current.ToArray());
        }

        //[Fact]
        //[Trait("UnitTest", "UnitTest")]
        public void EnumerateRawStratisBlockcahinAndCompareWithRpcServer()
        {
            // this is a long running tests and take a few hours to complete.
            // the tests requires a stratis full node rpc server to be running
            // validate all blocks with the full node rpc server
            // at the time of writing the test the stratis blockchain was just beyond the 90k mark
            // I will cap this test to 90k for that reason

            var inserts = this.ManuallyEnumerateTheBlockchainFile();

            // now we try 
            List<Block> blocks = new List<Block>();
            foreach (var blockHex in inserts.Where(s => !string.IsNullOrEmpty(s)))
            {
                var rem = blockHex.Substring(8);// the magic bytes
                var bt = Encoders.Hex.DecodeData(rem); // pars to bytes
                var size = BitConverter.ToUInt32(bt, 0); // first 4 bytes are a uint size 
                var b = new Block(bt.Skip(4).ToArray()); // create the block
                blocks.Add(b);
            }

            var client = new RPCClient(new NetworkCredential("rpcuser", "rpcpassword"), new Uri("http://127.0.0.1:5000"), Network.StratisMain);
            Dictionary<int, string> blocksNotFound = new Dictionary<int, string>();
            var index = 0;
            while (index <= 9000)
            {
                var blk = client.GetBlockHash(index);
                var res = blocks.FirstOrDefault(f => f.GetHash() == blk);

                if (res == null)
                {
                    blocksNotFound.Add(index, blk.ToString());
                }
                index++;
            }

            // check that all the blocks where found
            Assert.Empty(blocksNotFound);
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void EnumerateStratisBlockcahinAndValidateAllBlocks()
        {
            var listAll = new Dictionary<string, StoredBlock>();
            var store = new BlockStore(@"C:\StratisData", Network.StratisMain);
            foreach (var block in store.EnumerateFolder())
            {
                var hash = block.Item.GetHash();
                listAll.Add(hash.ToString(), block);
                Assert.True(block.Item.CheckMerkleRoot());
                Assert.True(block.Item.CheckProofOfWork());
                Assert.True(block.Item.CheckProofOfStake());
            }

            // walk the chain and check that all block are loaded correctly 
            var block100K = uint256.Parse("af380a53467b70bc5d1ee61441586398a0a5907bb4fad7855442575483effa54");
            var genesis = Network.StratisMain.GetGenesis().GetHash();
            uint256 current = block100K;
            while (true)
            {
                StoredBlock foundBlock;
                var found = listAll.TryGetValue(current.ToString(), out foundBlock);
                Assert.True(found);
                if (current == genesis) break;
                current = foundBlock.Item.Header.HashPrevBlock;
            }            
        }

        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void EnumerateStratisBlockcahinCheckTipBlock()
        {
            var store = new BlockStore(@"C:\StratisData", Network.StratisMain);

            // use the synchronize chain method to load all blocks and look for the tip (currently block 100k)
            var block100K = uint256.Parse("af380a53467b70bc5d1ee61441586398a0a5907bb4fad7855442575483effa54");
            var chain = store.GetChain();
            var lastblk = chain.GetBlock(block100K);
            Assert.Equal(block100K, lastblk.Header.GetHash());
            Assert.Equal(100000, lastblk.Height);
        }
    }
}
#endif