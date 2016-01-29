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

			var unspent = repo.GetUnspentAsync("15sYbVpRh6dyWycZMwPdxJWD4xbfxReeHe").Result;
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

		class ConflictPart
		{
			public Network Network;
			public Base58Type Type;
			public byte[] Value;
			public override string ToString()
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(Network + " ");
				builder.Append(Enum.GetName(typeof(Base58Type), Type) + " ");
				builder.Append(String.Join(",", Value));
				return builder.ToString();
			}
		}
		class Conflict
		{
			public Conflict()
			{
				A = new ConflictPart();
				B = new ConflictPart();
			}
			public ConflictPart A;
			public ConflictPart B;
			public override string ToString()
			{
				return A + " <=> " + B;
			}
		}
		[Fact]
		public static void Play()
		{
			Transaction txxxxx = new Transaction("0100000000010213206299feb17742091c3cb2ab45faa3aa87922d3c030cafb3f798850a2722bf0000000000feffffffa12f2424b9599898a1d30f06e1ce55eba7fabfeee82ae9356f07375806632ff3010000006b483045022100fcc8cf3014248e1a0d6dcddf03e80f7e591605ad0dbace27d2c0d87274f8cd66022053fcfff64f35f22a14deb657ac57f110084fb07bb917c3b42e7d033c54c7717b012102b9e4dcc33c9cc9cb5f42b96dddb3b475b067f3e21125f79e10c853e5ca8fba31feffffff02206f9800000000001976a9144841b9874d913c430048c78a7b18baebdbea440588ac8096980000000000160014e4873ef43eac347471dd94bc899c51b395a509a502483045022100dd8250f8b5c2035d8feefae530b10862a63030590a851183cb61b3672eb4f26e022057fe7bc8593f05416c185d829b574290fb8706423451ebd0a0ae50c276b87b43012102179862f40b85fa43487500f1d6b13c864b5eb0a83999738db0f7a6b91b2ec64f00db080000");

			Script scriptCode = new Script("OP_DUP OP_HASH160 e4873ef43eac347471dd94bc899c51b395a509a5 OP_EQUALVERIFY OP_CHECKSIG");
			var rrrr = Script.SignatureHash(scriptCode, txxxxx, 0, SigHash.All, Money.Satoshis(10000000), HashVersion.Witness);

			//Console.WriteLine(total);
			new Key().PubKey.WitHash.GetAddress(Network.SegNet).ToString();

			var node = Node.Connect(Network.SegNet, "qbitninja-server.cloudapp.net");
			node.VersionHandshake();

			uint256 p2wsh = null;
			uint256 p2pwkh = null;
			uint256 p2wshp2sh = null;
			uint256 p2wpkhp2sh = null;
			foreach(var block in node.GetBlocks())
			{
				foreach(var tx in block.Transactions)
				{
					if(p2wsh != null && p2pwkh != null && p2wshp2sh != null && p2wpkhp2sh != null)
						break;
					if(!tx.IsCoinBase && tx.HasWitness)
					{
						foreach(var input in tx.Inputs.AsIndexedInputs())
						{
							if(input.WitScript == WitScript.Empty)
								continue;
							if(input.ScriptSig == Script.Empty)
							{
								if(PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(input.WitScript) != null)
									p2pwkh = tx.GetHash();
								else
									p2wsh = tx.GetHash();
							}
							else
							{
								if(PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(input.WitScript) != null)
									p2wpkhp2sh = tx.GetHash();
								else
									p2wshp2sh = tx.GetHash();
							}
							break;
						}
					}
				}
			}

			var secret = new BitcoinSecret("QTrKVpsVwNUD9GayzdbUNz2NNDqiPgjd9RCprwSa4gmBFg3V2oik", Network.SegNet);

			var oo = secret.PubKey.WitHash.GetAddress(Network.SegNet);

			var key = new Key().GetBitcoinSecret(Network.SegNet);
			var aa = key.GetAddress();
			foreach(var n in new[] { Network.Main, Network.SegNet })
			{
				for(int i = 0 ; i < 2 ; i++)
				{
					BitcoinAddress addr = i == 0 ? (BitcoinAddress)new Key().PubKey.GetSegwitAddress(n) : new Key().ScriptPubKey.GetWitScriptAddress(n);
					var c = addr.ScriptPubKey.ToString();
				}
			}


			var networks = Network.GetNetworks().ToArray();
			List<Conflict> conflicts = new List<Conflict>();
			for(int n = 0 ; n < networks.Length ; n++)
			{
				for(int n2 = n + 1 ; n2 < networks.Length ; n2++)
				{
					var a = networks[n];
					var b = networks[n2];
					if(a == Network.RegTest || b == Network.RegTest)
						continue;
					if(a == b)
						throw new Exception();
					for(int i = 0 ; i < a.base58Prefixes.Length ; i++)
					{
						for(int y = i + 1 ; y < a.base58Prefixes.Length ; y++)
						{
							if(a.base58Prefixes[i] != null && b.base58Prefixes[y] != null)
							{
								var ae = Encoders.Hex.EncodeData(a.base58Prefixes[i]);
								var be = Encoders.Hex.EncodeData(b.base58Prefixes[y]);
								if(ae == be)
									continue;
								if(ae.StartsWith(be) || be.StartsWith(ae))
								{
									ConflictPart ca = new ConflictPart();
									ca.Network = a;
									ca.Type = (Base58Type)i;
									ca.Value = a.base58Prefixes[i];

									ConflictPart cb = new ConflictPart();
									cb.Network = b;
									cb.Type = (Base58Type)y;
									cb.Value = b.base58Prefixes[y];

									Conflict cc = new Conflict();
									cc.A = ca;
									cc.B = cb;
									conflicts.Add(cc);
								}
							}
						}
					}
				}
			}

			var rr = String.Join("\r\n", conflicts.OfType<object>().ToArray());
			Console.WriteLine();

			//ConcurrentChain chain = new ConcurrentChain(Network.Main);
			//ChainBehavior chainBehavior = new ChainBehavior(chain);
			//NodeConnectionParameters para = new NodeConnectionParameters();
			//para.TemplateBehaviors.Add(chainBehavior);

			//NodesGroup group = new NodesGroup(Network.Main, para);
			//group.Connect();
			//while(true)
			//{
			//	Thread.Sleep(1000);
			//}



			//Parallel.ForEach(Enumerable.Range(0, 10), _ =>
			//{
			//	ConcurrentChain chain = new ConcurrentChain(Network.Main);
			//	node.SynchronizeChain(chain);
			//});

			//Wallet wallet = new Wallet(new ExtKey(), Network.Main);
			//wallet.Connect(addrman: AddressManager.LoadPeerFile(@"E:\Program Files\Bitcoin\peers.dat", Network.Main));
			//while(true)
			//{
			//	Thread.Sleep(1000);
			//	Console.WriteLine(wallet.ConnectedNodes + " " + wallet.State);

			//}

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
			var repo = new QBitNinjaTransactionRepository("http://rapidbase-test.azurewebsites.net/");
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

		private static Coin RandomCoin(Key bob, Money amount, bool p2pkh = false)
		{
			return new Coin(new uint256(Enumerable.Range(0, 32).Select(i => (byte)0xaa).ToArray()), 0, amount, p2pkh ? bob.PubKey.Hash.ScriptPubKey : bob.PubKey.WitHash.ScriptPubKey);
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