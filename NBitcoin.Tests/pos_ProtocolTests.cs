#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Protocol.Payloads;
using Xunit;

namespace NBitcoin.Tests
{
	public class pos_ProtocolTests
	{
		public static bool noClient = !Process.GetProcesses().Any(p => p.ProcessName.Contains("stratis"));

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//Copied from https://en.bitcoin.it/wiki/Protocol_specification (19/04/2014)
		public void CanParseMessages()
		{
			if (pos_RPCClientTests.noClient) return;

			var EST = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			var tests = new[]
				{
					new
					{
						Version = ProtocolVersion.INIT_PROTO_VERSION,
						Message = "7035220576657273696f6e0000000000550000009c7c00000100000000000000e615104d00000000010000000000000000000000000000000000ffff0a000001208d010000000000000000000000000000000000ffff0a000002208ddd9d202c3ab457130055810100",
						Test = new Action<object>(o=>
						{
							var version = (VersionPayload)o;
							Assert.Equal((ulong)0x1357B43A2C209DDD, version.Nonce);
							Assert.Equal("", version.UserAgent);
							Assert.Equal("::ffff:10.0.0.2", version.AddressFrom.Address.ToString());
							Assert.Equal(8333, version.AddressFrom.Port);
							Assert.Equal(0x00018155, version.StartHeight);
							Assert.Equal((ProtocolVersion)31900, version.Version);
						})
					},
					new
					{
						Version = ProtocolVersion.MEMPOOL_GD_VERSION,
						Message = "7035220576657273696f6e000000000064000000358d493262ea0000010000000000000011b2d05000000000010000000000000000000000000000000000ffff000000000000000000000000000000000000000000000000ffff0000000000003b2eb35d8ce617650f2f5361746f7368693a302e372e322fc03e0300",
						Test = new Action<object>(o=>
						{
							var version = (VersionPayload)o;
							Assert.Equal("/Satoshi:0.7.2/", version.UserAgent);
							Assert.Equal(0x00033EC0, version.StartHeight);
						})
					},
					new
					{
						Version = ProtocolVersion.PROTOCOL_VERSION,
						Message = "7035220576657261636b000000000000000000005df6e0e2",
						Test = new Action<object>(o=>
							{
								var verack = (VerAckPayload)o;
							})
					},
					new
					{
						Version = ProtocolVersion.MEMPOOL_GD_VERSION,
						Message = "703522056164647200000000000000001f000000ed52399b01e215104d010000000000000000000000000000000000ffff0a000001208d",
						Test = new Action<object>(o=>
							{
								var addr = (AddrPayload)o;
								Assert.Equal(1, addr.Addresses.Length);
								//"Mon Dec 20 21:50:10 EST 2010"
								var date = TimeZoneInfo.ConvertTime(addr.Addresses[0].Time,EST);
								Assert.Equal(20,date.Day);
								Assert.Equal(12, date.Month);
								Assert.Equal(2010, date.Year);
								Assert.Equal(21, date.Hour);
							})
					},

				};

			foreach (var test in tests)
			{
				var message = Network.StratisMain.ParseMessage(TestUtils.ParseHex(test.Message), test.Version);
				test.Test(message.Payload);
				var bytes = message.ToBytes(test.Version);
				var old = message;
				message = new Message();
				message.FromBytes(bytes, test.Version);
				test.Test(message.Payload);
				Assert.Equal(test.Message, Encoders.Hex.EncodeData(message.ToBytes(test.Version)));
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanHandshake()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var seed = builder.CreateNode(false).CreateNodeClient();
				Assert.True(seed.State == NodeState.Connected);
				seed.VersionHandshake();
				Assert.True(seed.State == NodeState.HandShaked);
				seed.Disconnect();
				Assert.True(seed.State == NodeState.Offline);
			}
		}

		//[Fact]
		//[Trait("Protocol", "Protocol")]
		public void CanGetMerkleRoot()
		{
			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false).CreateNodeClient();
				//builder.Nodes[0].Generate(101);
				var rpc = builder.Nodes[0].CreateRPCClient();
				builder.Nodes[0].Split(Money.Coins(50m), 50);
				builder.Nodes[0].SelectMempoolTransactions();
				builder.Nodes[0].Generate(1);
				for (int i = 0; i < 20; i++)
				{
					rpc.SendToAddress(new Key().PubKey.GetAddress(rpc.Network), Money.Coins(0.5m));
				}
				builder.Nodes[0].SelectMempoolTransactions();
				builder.Nodes[0].Generate(1);
				var block = builder.Nodes[0].CreateRPCClient().GetBlock(103);
				var knownTx = block.Transactions[0].GetHash();
				var knownAddress = block.Transactions[0].Outputs[0].ScriptPubKey.GetDestination();
				node.VersionHandshake();
				using (var list = node.CreateListener()
										.Where(m => m.Message.Payload is MerkleBlockPayload || m.Message.Payload is TxPayload))
				{
					BloomFilter filter = new BloomFilter(1, 0.005, 50, BloomFlags.UPDATE_NONE);
					filter.Insert(knownAddress.ToBytes());
					node.SendMessageAsync(new FilterLoadPayload(filter));
					node.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, block.GetHash())));
					var merkle = list.ReceivePayload<MerkleBlockPayload>();
					var tree = merkle.Object.PartialMerkleTree;
					Assert.True(tree.Check(block.Header.HashMerkleRoot));
					Assert.True(tree.GetMatchedTransactions().Count() > 1);
					Assert.True(tree.GetMatchedTransactions().Contains(knownTx));

					List<Transaction> matched = new List<Transaction>();
					for (int i = 0; i < tree.GetMatchedTransactions().Count(); i++)
					{
						matched.Add(list.ReceivePayload<TxPayload>().Object);
					}
					Assert.True(matched.Count > 1);
					tree = tree.Trim(knownTx);
					Assert.True(tree.GetMatchedTransactions().Count() == 1);
					Assert.True(tree.GetMatchedTransactions().Contains(knownTx));

					Action act = () =>
					{
						foreach (var match in matched)
						{
							Assert.True(filter.IsRelevantAndUpdate(match));
						}
					};
					act();
					filter = filter.Clone();
					act();

					var unknownBlock = uint256.Parse("00000000ad262227291eaf90cafdc56a8f8451e2d7653843122c5bb0bf2dfcdd");
					node.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, Network.RegTest.GetGenesis().GetHash())));

					merkle = list.ReceivePayload<MerkleBlockPayload>();
					tree = merkle.Object.PartialMerkleTree;
					Assert.True(tree.Check(merkle.Object.Header.HashMerkleRoot));
					Assert.True(!tree.GetMatchedTransactions().Contains(knownTx));
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanMaintainChainWithChainBehavior()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false).CreateNodeClient();
				//builder.Nodes[0].Generate(600);
				var rpc = builder.Nodes[0].CreateRPCClient();
				var chain = node.GetChain(rpc.GetBlockHash(500));
				Assert.True(chain.Height == 500);
				using (var tester = new NodeServerTester(Network.StratisMain))
				{
					var n1 = tester.Node1;
					n1.Behaviors.Add(new ChainBehavior(chain));
					n1.VersionHandshake();
					Assert.True(n1.MyVersion.StartHeight == 500);
					var n2 = tester.Node2;
					Assert.True(n2.MyVersion.StartHeight == 0);
					Assert.True(n2.PeerVersion.StartHeight == 500);
					Assert.True(n1.State == NodeState.HandShaked);
					Assert.True(n2.State == NodeState.HandShaked);
					var behavior = new ChainBehavior(new ConcurrentChain(Network.StratisMain));
					n2.Behaviors.Add(behavior);
					TestUtils.Eventually(() => behavior.Chain.Height == 500);
					var chain2 = n2.GetChain(rpc.GetBlockHash(500));
					Assert.True(chain2.Height == 500);
					var chain1 = n1.GetChain(rpc.GetBlockHash(500));
					Assert.True(chain1.Height == 500);
					chain1 = n1.GetChain(rpc.GetBlockHash(499));
					Assert.True(chain1.Height == 499);
					Thread.Sleep(5000);
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanCancelConnection()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false);
				CancellationTokenSource cts = new CancellationTokenSource();
				cts.Cancel();
				try
				{
					var client = Node.Connect(Network.StratisMain, "127.0.0.1:" + node.ProtocolPort.ToString(), new NodeConnectionParameters()
					{
						ConnectCancellation = cts.Token
					});
					Assert.False(true, "Should have thrown");
				}
				catch (OperationCanceledException)
				{
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanGetTransactionsFromMemPool()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode();
				node.ConfigParameters.Add("whitelist", "127.0.0.1");
				//node.Start();
				//node.Generate(101);
				//node.CreateRPCClient().SendToAddress(new Key().PubKey.GetAddress(Network.StratisMain), Money.Coins(1.0m));
				var client = node.CreateNodeClient();
				client.VersionHandshake();
				var transactions = client.GetMempoolTransactions();
				Assert.NotNull(transactions);//.True(transactions.Length == 1);
			}
		}

#if !NOFILEIO
		[Fact]
		public void CanConnectToRandomNode()
		{
			Stopwatch watch = new Stopwatch();
			NodeConnectionParameters parameters = new NodeConnectionParameters();
			var addrman = GetCachedAddrMan("addrmancache.dat");
			parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addrman));
			watch.Start();
			using (var node = Node.Connect(Network.StratisMain, parameters))
			{
				var timeToFind = watch.Elapsed;
				node.VersionHandshake();
				node.Dispose();
				watch.Restart();
				using (var node2 = Node.Connect(Network.StratisMain, parameters))
				{
					var timeToFind2 = watch.Elapsed;
				}
			}
			addrman.SavePeerFile("addrmancache.dat", Network.StratisMain);
		}

		public static AddressManager GetCachedAddrMan(string file)
		{
			if (File.Exists(file))
			{
				return AddressManager.LoadPeerFile(file);
			}
			return new AddressManager();
		}
#endif

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanGetBlocksWithProtocol()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false);
				//node.Generate(50);
				var client = node.CreateNodeClient();
				var chain = client.GetChain();
				var blocks = client.GetBlocks(chain.GetBlock(20).HashBlock).ToArray();
				Assert.Equal(20, blocks.Length);
				Assert.Equal(chain.GetBlock(20).HashBlock, blocks.Last().Header.GetHash());

				blocks = client.GetBlocksFromFork(chain.GetBlock(45), chain.GetBlock(50).HashBlock).ToArray();
				Assert.Equal(5, blocks.Length);
				Assert.Equal(chain.GetBlock(50).HashBlock, blocks.Last().Header.GetHash());
				Assert.Equal(chain.GetBlock(46).HashBlock, blocks.First().Header.GetHash());
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanGetMemPool()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode();
				node.ConfigParameters.Add("whitelist", "127.0.0.1");
				//node.Start();
				//node.Generate(102);
				//for (int i = 0; i < 2; i++)
				//	node.CreateRPCClient().SendToAddress(new Key().PubKey.GetAddress(Network.RegTest), Money.Coins(1.0m));
				var client = node.CreateNodeClient();
				var txIds = client.GetMempool();
				Assert.NotNull(txIds); //.True(txIds.Length == 2);
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanGetChainsConcurrently()
		{
			if (pos_RPCClientTests.noClient) return;

			var stop = uint256.Parse("c9920baf967a314bd123efa184d54d4b9e7460301e3f2e059bafc77c45d03017");

			using (var builder = NodeBuilderStratis.Create())
			{
				//bool generating = true;
				builder.CreateNode(false);
				//Task.Run(() =>
				//{
				//	builder.Nodes[0].Generate(600);
				//	generating = false;
				//});
				var node = builder.Nodes[0].CreateNodeClient();
				node.PollHeaderDelay = TimeSpan.FromSeconds(2);
				node.VersionHandshake();
				Random rand = new Random();
				Thread.Sleep(1000);
				var chains =
					Enumerable.Range(0, 5)
					.Select(_ => Task.Factory.StartNew(() =>
					{
						Thread.Sleep(rand.Next(0, 1000));
						return node.GetChain(stop);
					}))
					.Select(t => t.Result)
					.ToArray();
				//while (generating)
				//{
				//	SyncAll(node, rand, chains);
				//}
				SyncAll(node, rand, chains);
				foreach (var c in chains)
				{
					Assert.Equal(500, c.Height);
				}
			}
		}

		private static void SyncAll(Node node, Random rand, ConcurrentChain[] chains)
		{
			var stop = uint256.Parse("c9920baf967a314bd123efa184d54d4b9e7460301e3f2e059bafc77c45d03017");

			Task.WaitAll(Enumerable.Range(0, 5)
								.Select(_ => Task.Factory.StartNew(() =>
								{
									Thread.Sleep(rand.Next(0, 1000));
									node.SynchronizeChain(chains[_], stop);
								})).ToArray());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ServerDisconnectCorrectlyFromDroppingClient()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var tester = new NodeServerTester())
			{
				var to2 = tester.Node1;
				to2.VersionHandshake();
				Assert.True(tester.Server1.IsConnectedTo(tester.Server2.ExternalEndpoint));
				Thread.Sleep(100);
				Assert.True(tester.Server2.IsConnectedTo(tester.Server1.ExternalEndpoint));
				to2.Disconnect();
				Thread.Sleep(100);
				Assert.False(tester.Server1.IsConnectedTo(tester.Server2.ExternalEndpoint));
				Thread.Sleep(100);
				Assert.False(tester.Server2.IsConnectedTo(tester.Server1.ExternalEndpoint));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReceiveHandshake()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var tester = new NodeServerTester())
			{
				var toS2 = tester.Node1;
				toS2.VersionHandshake();
				Assert.Equal(NodeState.HandShaked, toS2.State);
				Thread.Sleep(100); //Let the time to Server2 to add the new node, else the test was failing sometimes.
				Assert.Equal(NodeState.HandShaked, tester.Node2.State);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRespondToPong()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var tester = new NodeServerTester())
			{
				var toS2 = tester.Node1;
				toS2.VersionHandshake();
				var ping = new PingPayload();
				CancellationTokenSource cancel = new CancellationTokenSource();
				cancel.CancelAfter(10000);
				using (var list = toS2.CreateListener())
				{
					toS2.SendMessageAsync(ping);
					while (true)
					{
						var pong = list.ReceivePayload<PongPayload>(cancel.Token);
						if (ping.Nonce == pong.Nonce)
							break;
					}
				}

			}

		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CantConnectToYourself()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var tester = new NodeServerTester())
			{
				tester.Server2.Nonce = tester.Server1.Nonce;
				Assert.Throws(typeof(InvalidOperationException), () =>
				{
					tester.Node1.VersionHandshake();
				});
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanExchangeFastPingPong()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var tester = new NodeServerTester())
			{
				var n1 = tester.Node1;
				n1.Behaviors.Add(new PingPongBehavior()
				{
					PingInterval = TimeSpan.FromSeconds(0.1),
					TimeoutInterval = TimeSpan.FromSeconds(0.8)
				});

				n1.VersionHandshake();
				Assert.Equal(NodeState.HandShaked, n1.State);
				Assert.True(!n1.Inbound);

				var n2 = tester.Node2;
				n2.Behaviors.Add(new PingPongBehavior()
				{
					PingInterval = TimeSpan.FromSeconds(0.1),
					TimeoutInterval = TimeSpan.FromSeconds(0.5)
				});
				Assert.Equal(NodeState.HandShaked, n2.State);
				Assert.True(n2.Inbound);
				Thread.Sleep(2000);
				Assert.Equal(NodeState.HandShaked, n2.State);
				n1.Behaviors.Clear();
				Thread.Sleep(1200);
				Assert.True(n2.State == NodeState.Disconnecting || n2.State == NodeState.Offline);
				Assert.True(n2.DisconnectReason.Reason.StartsWith("Pong timeout", StringComparison.Ordinal));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConnectMultipleTimeToServer()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var tester = new NodeServerTester())
			{
				int nodeCount = 0;
				tester.Server1.NodeAdded += (s, a) => nodeCount++;
				tester.Server1.NodeRemoved += (s, a) => nodeCount--;

				var n1 = Node.Connect(tester.Server1.Network, tester.Server1.ExternalEndpoint);
				n1.VersionHandshake();
				Thread.Sleep(100);
				Assert.Equal(1, nodeCount);
				n1.PingPong();
				var n2 = Node.Connect(tester.Server1.Network, tester.Server1.ExternalEndpoint);
				n2.VersionHandshake();
				Thread.Sleep(100);
				Assert.Equal(2, nodeCount);
				n2.PingPong();
				n1.PingPong();
				Assert.Throws<ProtocolException>(() => n2.VersionHandshake());
				Thread.Sleep(100);
				n2.PingPong();
				Assert.Equal(2, nodeCount);
				n2.Disconnect();
				Thread.Sleep(100);
				Assert.Equal(1, nodeCount);
			}
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseReject()
		{
			var hex = "7035220572656a6563740000000000003a000000db7f7e7802747812156261642d74786e732d696e707574732d7370656e74577a9694da4ff41ae999f6591cff3749ad6a7db19435f3d8af5fecbcff824196";
			Message message = new Message();
			message.ReadWrite(Encoders.Hex.DecodeData(hex));
			var reject = (RejectPayload)message.Payload;
			Assert.True(reject.Message == "tx");
			Assert.True(reject.Code == RejectCode.DUPLICATE);
			Assert.True(reject.CodeType == RejectCodeType.Transaction);
			Assert.True(reject.Reason == "bad-txns-inputs-spent");
			Assert.True(reject.Hash == uint256.Parse("964182ffbcec5fafd8f33594b17d6aad4937ff1c59f699e91af44fda94967a57"));
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanDownloadBlock()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false).CreateNodeClient();
				node.VersionHandshake();
				node.SendMessageAsync(new GetDataPayload(new InventoryVector()
				{
					Hash = Network.StratisMain.GenesisHash,
					Type = InventoryType.MSG_BLOCK
				}));

				var block = node.ReceiveMessage<BlockPayload>();
				Assert.True(block.Object.CheckMerkleRoot());
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanDownloadHeaders()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false).CreateNodeClient();
				//builder.Nodes[0].Generate(50);
				node.VersionHandshake();
				var begin = node.Counter.Snapshot();
				var result = node.GetChain();
				var end = node.Counter.Snapshot();
				var diff = end - begin;
				Assert.True(node.PeerVersion.StartHeight <= result.Height);
				var subChain = node.GetChain(result.GetBlock(10).HashBlock);
				Assert.Equal(10, subChain.Height);
			}
		}


		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanDownloadBlocks()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false).CreateNodeClient();
				//builder.Nodes[0].Generate(50);
				var chain = node.GetChain();
				chain.SetTip(chain.GetBlock(9));
				var blocks = node.GetBlocks(chain.ToEnumerable(true).Select(c => c.HashBlock)).ToList();
				foreach (var block in blocks)
				{
					Assert.True(block.CheckMerkleRoot());
				}
				Assert.Equal(10, blocks.Count);
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanDownloadLastBlocks()
		{
			if (pos_RPCClientTests.noClient) return;

			using (var builder = NodeBuilderStratis.Create())
			{
				var node = builder.CreateNode(false).CreateNodeClient();
				//builder.Nodes[0].Generate(150);
				var chain = node.GetChain();

				Assert.True(node.PeerVersion.StartHeight <= chain.Height);

				var subChain = chain.ToEnumerable(true).Take(100).Select(s => s.HashBlock).ToArray();

				var begin = node.Counter.Snapshot();
				var blocks = node.GetBlocks(subChain).Select(_ => 1).ToList();
				var end = node.Counter.Snapshot();
				var diff = end - begin;
				Assert.True(diff.Start == begin.Taken);
				Assert.True(diff.Taken == end.Taken);
				Assert.True(diff.TotalReadenBytes == end.TotalReadenBytes - begin.TotalReadenBytes);
				Assert.True(diff.TotalWrittenBytes == end.TotalWrittenBytes - begin.TotalWrittenBytes);

				Assert.True(blocks.Count == 100);
			}
		}
	}
}
#endif