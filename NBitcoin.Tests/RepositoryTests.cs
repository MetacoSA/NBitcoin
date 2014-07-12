using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
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
		public static void Play()
		{
			//1,132

			//NodeServer server = new NodeServer(Network.Main);
			//server.RegisterPeerTableRepository(new SqLitePeerTableRepository("PeerCachePlay"));
			//server.BuildChain(new StreamObjectStream<ChainChange>(File.Open("MainChain.dat", FileMode.OpenOrCreate)));

			var from = new DateTimeOffset(new DateTime(2012, 12, 1));
			var to = new DateTimeOffset(new DateTime(2013, 1, 20));

			var chain = new Chain(new StreamObjectStream<ChainChange>(File.Open("MainChain.dat", FileMode.OpenOrCreate)));
			var blocks =
				chain.ToEnumerable(false)
					.SkipWhile(c => c.Header.BlockTime < from)
					.TakeWhile(c => c.Header.BlockTime < to)
					.ToArray();

			//index.ReIndex();
			//Console.WriteLine("");


			BlockStore store = new BlockStore("E:\\Bitcoin\\blocks", Network.Main);
			IndexedBlockStore index = new IndexedBlockStore(new SQLiteNoSqlRepository("PlayIndex"), store);
			var target = Money.Parse("1100");
			var margin = Money.Parse("1");
			var dest = Network.CreateFromBase58Data<BitcoinAddress>("1FrtkNXastDoMAaorowys27AKQERxgmZjY");
			var en = new CultureInfo("en-US");

			FileStream fs = File.Open("logs", FileMode.Create);
			var writer = new StreamWriter(fs);
			writer.WriteLine("time,height,txid,value");

			var lines = from header in blocks
						let block = index.Get(header.HashBlock)
						from tx in block.Transactions
						from txout in tx.Outputs
						//where txout.ScriptPubKey.GetDestination() == dest.ID
						where target - margin < txout.Value && txout.Value < target + margin
						select new
						{
							Block = block,
							Height = header.Height,
							Transaction = tx,
							TxOut = txout
						};
			foreach(var line in lines)
			{
				writer.WriteLine(
					line.Block.Header.BlockTime.ToString(en) + "," +
					line.Height + "," +
					line.Transaction.GetHash() + "," +
					line.TxOut.Value.ToString());
			}
			writer.Flush();
			Process.Start(@"E:\Chocolatey\lib\notepadplusplus.commandline.6.4.5\tools\notepad++.exe", "\"" + new FileInfo(fs.Name).FullName + "\"");

			//foreach(var b in blocks)
			//{
			//	var block = index.Get(b.HashBlock);
			//	foreach(var tx in block.Transactions)
			//	{
			//		foreach(var txout in tx.Outputs)
			//		{
			//			var pa = new PayToPubkeyHashTemplate()
			//					.ExtractScriptPubKeyParameters(txout.ScriptPubKey);
			//			if(pa != null && pa == dest.ID)
			//			{
			//				if(target - margin < txout.Value && txout.Value < target + margin)
			//				{
			//					writer.WriteLine(b.Header.BlockTime.ToString(en) + "," + b.Height + "," + tx.GetHash() + "," + txout.Value.ToString());
			//				}
			//			}
			//		}
			//	}

			//}

			//They were purchased anonymously at a restaurant halfway between Sandwich, IL, and Chicago, in Naperville, IL, about the second week of December, 2012. Taxing my memory, the total was 1,132 + x(single digit).xxx... and immediately put in a freshly created InstaWallet bitcoin wallet. A couple/three days later I split the three accounts up into 1,000, 132, and x.xxx... wallets, always using InstaWallet while in my possession. Hope that helps with your plans.



			//var txt = File.ReadAllText("data/difficultyhistory.csv");
			//var lines = txt.Split(new string[]{"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
			//StringBuilder builder = new StringBuilder();
			//foreach(var line in lines)
			//{
			//	var fields = line.Split(',');
			//	builder.AppendLine(fields[0] + "," + fields[2]);
			//}
			//File.WriteAllText("targethistory.csv",builder.ToString());
			////PlaySplit();
			//var oo = new PubKey(TestUtils.ParseHex("02bbc9fccbe03de928fc66fcd176fbe69d3641677970c6f8d558aa72f72e35e0cb")).GetAddress(Network.TestNet);
			//RPCClient client = RPCClientTests.CreateRPCClient();

			////https://tpfaucet.appspot.com/
			//var scan = new Key(TestUtils.ParseHex("cc411aab02edcd3bccf484a9ba5280d4a774e6f81eac8ebec9cb1c2e8f73020a"));
			//var addr = new BitcoinStealthAddress("waPYjXyrTrvXjZHmMGdqs9YTegpRDpx97H5G3xqLehkgyrrZKsxGCmnwKexpZjXTCskUWwYywdUvrZK7L2vejeVZSYHVns61gm8VfU", Network.TestNet);

			//var sender = new BitcoinSecret("cRjSUV1LqN2F8MsGnLE2JKfCP75kbWGFRroNQeXHC429jqVFgmW3", Network.TestNet);


			//using(var fs = File.Open("Data.txt", FileMode.Create))
			//{
			//	StreamWriter writer = new StreamWriter(fs);
			//	foreach(var c in client.ListUnspent())
			//	{
			//		var ephem = new Key();
			//		writer.WriteLine("---");
			//		writer.WriteLine("Ephem Private key : " + Encoders.Hex.EncodeData(ephem.ToBytes()));
			//		Transaction tx = new Transaction();
			//		tx.Version = 1;
			//		tx.AddInput(new TxIn(c.OutPoint));
			//		var pay = addr.CreatePayment(ephem);

			//		writer.WriteLine("Metadata hash : " + pay.Metadata.Hash);
			//		writer.WriteLine("Metadata script : " + pay.Metadata.Script);
			//		writer.WriteLine("Metadata Nonce : " + pay.Metadata.Nonce);
			//		writer.WriteLine("Metadata Ephem Key : " + pay.Metadata.EphemKey.ToHex());

			//		pay.AddToTransaction(tx, c.Amount);

			//		tx.SignAll(sender);

			//		client.SendRawTransaction(tx);

			//		fs.Flush();
			//	}

			//}

			//var p = new PubKey(Encoders.Hex.DecodeData("03b4e5d3cf889840c75f0dd02ebda946151bf37e56cb888c6002c2ae5288e56de7"));
			//var o = Network.CreateFromBase58Data("mvXf4sF4C1w5KgQyasbEWxqVyqbLNtVdnY");
			//var sender = new BitcoinSecret("cRjSUV1LqN2F8MsGnLE2JKfCP75kbWGFRroNQeXHC429jqVFgmW3", Network.TestNet).Key;
			////var addr = secret.Key.PubKey.GetAddress(Network.TestNet); //mwdJkHRNJi1fEwHBx6ikWFFuo2rLBdri2h
			//
			//var receiver = new BitcoinStealthAddress("waPV5rHToBq3NoR7y5J9UdE7aUbuqJybNpE88Dve7WgWhEfvMrcuaSvF6tSQ3Fbe8dErL6ks8byJPcp3QCK2HHviGCSjg42VgMAPJb", Network.TestNet);

			//Key ephemKey = new Key(Encoders.Hex.DecodeData("9daed68ad37754305e82740a6252cf80765c36d29a55158b1a19ed29914f0cb1"));
			//var ephemKeyStr = Encoders.Hex.EncodeData(ephemKey.ToBytes());
			//var scanStr = Encoders.Hex.EncodeData(receiver.ScanPubKey.ToBytes());
			//var spendStr = Encoders.Hex.EncodeData(receiver.SpendPubKeys[0].ToBytes());

			//var payment = receiver.CreatePayment(ephemKey);
			//var tx = new Transaction();
			//tx.Version = 1;
			//tx.Inputs.Add(new TxIn(new OutPoint(new uint256("d65e2274f6fde9515a35655d54e79243d5a17355f6943d6c16a63083a8769ea3"), 1)));

			//payment.AddToTransaction(tx, Money.Parse("0.51"));

			//tx.Inputs[0].ScriptSig = new PayToPubkeyHashTemplate().GenerateScriptPubKey(sender.PubKey.GetAddress(Network.TestNet));
			//var hash = tx.Inputs[0].ScriptSig.SignatureHash(tx, 0, SigHash.All);
			//var sig = sender.Sign(hash);
			//tx.Inputs[0].ScriptSig = new PayToPubkeyHashTemplate().GenerateScriptSig(new TransactionSignature(sig, SigHash.All), sender.PubKey);

			//var result = Script.VerifyScript(tx.Inputs[0].ScriptSig, new Script("OP_DUP OP_HASH160 b0b594bb2d2ca509b817f27e9280f6471807af26 OP_EQUALVERIFY OP_CHECKSIG"), tx, 0);

			//var bytes = Encoders.Hex.EncodeData(tx.ToBytes());
			//var client = new NodeServer(Network.TestNet);

			//var node = client.GetNodeByEndpoint(new IPEndPoint(IPAddress.Parse("95.85.39.28"), 18333));
			//node.VersionHandshake();
			//node.SendMessage(new TxPayload(tx));
			////var store = new BlockStore(@"E:\Bitcoin\blocks", Network.Main);
			////foreach(var un in store.EnumerateFolder())
			////{
			//	var expectedSize = un.Header.ItemSize;
			//	var actualSize = un.Item.GetSerializedSize();
			//}

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

		private static void PlaySplit()
		{
			var scan = new Key(TestUtils.ParseHex("cc411aab02edcd3bccf484a9ba5280d4a774e6f81eac8ebec9cb1c2e8f73020a"));
			var addr = new BitcoinStealthAddress("waPYjXyrTrvXjZHmMGdqs9YTegpRDpx97H5G3xqLehkgyrrZKsxGCmnwKexpZjXTCskUWwYywdUvrZK7L2vejeVZSYHVns61gm8VfU", Network.TestNet);

			var sender = new BitcoinSecret("cRjSUV1LqN2F8MsGnLE2JKfCP75kbWGFRroNQeXHC429jqVFgmW3", Network.TestNet);
			var tx = new Transaction();
			tx.Version = 1;

			RPCClient client = RPCClientTests.CreateRPCClient();
			var coins = client.ListUnspent();
			foreach(var unspent in coins)
			{
				tx.Inputs.Add(new TxIn(unspent.OutPoint));
			}
			var amount = coins.Select(c => c.Amount).Sum();

			var perOut = (long)(amount.Satoshi / 13);

			while(amount > 0)
			{
				var toSend = Math.Min(perOut, (long)amount);
				amount -= toSend;

				var tout = new TxOut(toSend, sender.GetAddress());
				if(!tout.IsDust)
					tx.Outputs.Add(tout);
			}

			tx.SignAll(sender);
			client.SendRawTransaction(tx);
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
