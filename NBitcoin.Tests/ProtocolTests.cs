#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using NBitcoin;
using NBitcoin.Protocol;
using System.Net;
using System.Threading;
using System.IO;
using NBitcoin.DataEncoders;
using System.Net.Sockets;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.Logging;
using NBitcoin.Tests.Helpers;
using Xunit.Abstractions;
using Xunit.Sdk;
using NBitcoin.Protocol.Connectors;

namespace NBitcoin.Tests
{
	public class NodeServerTester : IDisposable
	{
		static Random _Rand = new Random();
		public NodeServerTester()
		{
			int retry = 0;
			var network = Network.RegTest;
			Network = network;
			while (true)
			{
				try
				{
					var a = _Rand.Next(4000, 60000);
					var b = _Rand.Next(4000, 60000);
					_Server1 = new NodeServer(network, internalPort: a);
					_Server1.AllowLocalPeers = true;
					_Server1.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6(), a);
					_Server1.Listen();
					Assert.True(_Server1.IsListening);
					_Server2 = new NodeServer(network, internalPort: b);
					_Server2.AllowLocalPeers = true;
					_Server2.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6(), b);
					_Server2.Listen();
					Assert.True(_Server2.IsListening);
					break;
				}
				catch (Exception)
				{
					if (_Server1 != null)
						_Server1.Dispose();
					if (_Server2 != null)
						_Server2.Dispose();
					retry++;
					if (retry == 5)
						throw;
				}
			}
		}

		public Network Network
		{
			get; set;
		}

		public IEnumerable<Node> ConnectedNodes
		{
			get
			{
				return Server1.ConnectedNodes.Concat(Server2.ConnectedNodes);
			}
		}
		private readonly NodeServer _Server1;
		public NodeServer Server1
		{
			get
			{
				return _Server1;
			}
		}
		private readonly NodeServer _Server2;
		public NodeServer Server2
		{
			get
			{
				return _Server2;
			}
		}

		Node _Node1;
		public Node Node1
		{
			get
			{
				_Node1 = _Node1 ?? Server2.FindOrConnect(Server1.ExternalEndpoint);
				Thread.Sleep(0); //Don't underestimate thread preemption... without that the tests crash in mono proc... :(
				Thread.Sleep(0);
				return _Node1;
			}
		}

		Node _Node2;
		public Node Node2
		{
			get
			{
				_Node2 = _Node2 ?? Server1.FindOrConnect(Server2.ExternalEndpoint);
				Thread.Sleep(0);  //Don't underestimate thread preemption... without that the tests crash in mono proc... :(
				Thread.Sleep(0);
				return _Node2;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			_Server1.Dispose();
			_Server2.Dispose();
			foreach (var dispo in _Disposables)
				dispo.Dispose();
		}

		#endregion

		public static string NATRuleName = "NBitcoin Tests";

		List<IDisposable> _Disposables = new List<IDisposable>();
		internal void AddDisposable(IDisposable disposable)
		{
			_Disposables.Add(disposable);
		}
	}
	public class ProtocolTests
	{
		private readonly ITestOutputHelper logs;

		public ProtocolTests(ITestOutputHelper testOutputHelper)
		{
			this.logs = testOutputHelper;
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//Copied from https://en.bitcoin.it/wiki/Protocol_specification (19/04/2014)
		public void CanParseMessages()
		{
			TimeZoneInfo EST;
			try
			{
				EST = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			}
			catch (TimeZoneNotFoundException)
			{
				EST = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
			}

			var tests = new[]
				{
					new
					{
						Version = 209U,
						Message = "f9beb4d976657273696f6e0000000000550000009c7c00000100000000000000e615104d00000000010000000000000000000000000000000000ffff0a000001208d010000000000000000000000000000000000ffff0a000002208ddd9d202c3ab457130055810100",
						Test = new Action<object>(o=>
						{
							var version = (VersionPayload)o;
							Assert.Equal((ulong)0x1357B43A2C209DDD, version.Nonce);
							Assert.Equal("", version.UserAgent);
							Assert.Equal("[::ffff:10.0.0.2]:8333", version.AddressFrom.ToString());
							Assert.Equal(0x00018155, version.StartHeight);
							Assert.Equal<uint>(31900, version.Version);
						})
					},
					new
					{
						Version = 60002U,
						Message = "f9beb4d976657273696f6e000000000064000000358d493262ea0000010000000000000011b2d05000000000010000000000000000000000000000000000ffff000000000000000000000000000000000000000000000000ffff0000000000003b2eb35d8ce617650f2f5361746f7368693a302e372e322fc03e0300",
						Test = new Action<object>(o=>
						{
							var version = (VersionPayload)o;
							Assert.Equal("/Satoshi:0.7.2/", version.UserAgent);
							Assert.Equal(0x00033EC0, version.StartHeight);
						})
					},
					new
					{
						Version = 70012U,
						Message = "f9beb4d976657261636b000000000000000000005df6e0e2",
						Test = new Action<object>(o=>
							{
								var verack = (VerAckPayload)o;
							})
					},
					new
					{
						Version = 60002U,
						Message = "f9beb4d96164647200000000000000001f000000ed52399b01e215104d010000000000000000000000000000000000ffff0a000001208d",
						Test = new Action<object>(o=>
							{
								var addr = (AddrPayload)o;
								Assert.Single(addr.Addresses);
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
				var message = Network.Main.ParseMessage(TestUtils.ParseHex(test.Message), test.Version);
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
			using (var builder = NodeBuilderEx.Create())
			{
				var seed = builder.CreateNode(true).CreateNodeClient();
				Assert.True(seed.State == NodeState.Connected);
				seed.VersionHandshake();
				Assert.True(seed.State == NodeState.HandShaked);
				seed.Disconnect();
				Assert.True(seed.State == NodeState.Offline);
				Assert.NotNull(seed.TimeOffset);
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanProcessAddressGossip()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true);
				var rpc = node.CreateRPCClient();
				for (var i = 1; i < 101; i++)
				{
					for (var j = 1; j < 101; j++)
					{
						var ip = IPAddress.Parse($"{i}.{j}.1.1");
						rpc.AddPeerAddress(ip, 8333);
					}
				}

				using (var nodeClient = node.CreateNodeClient())
				{
					nodeClient.VersionHandshake();
					AddrV2Payload addr;
					using (var list = nodeClient.CreateListener()
												.Where(m => m.Message.Payload is AddrV2Payload))
					{
						nodeClient.SendMessage(new GetAddrPayload());

						addr = list.ReceivePayload<AddrV2Payload>();
						Assert.Equal(1000, addr.Addresses.Length);
					}
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanHandshakeRestrictNodes()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true);
				var manager = new AddressManager();
				manager.Add(new NetworkAddress(node.NodeEndpoint), IPAddress.Loopback);

				var nodesRequirement = new NodeRequirement(){ MinStartHeight = 100 };
				var nodeConnectionParameters = new NodeConnectionParameters()
				{
					TemplateBehaviors =
					{
						new AddressManagerBehavior(manager)
						{
							PeersToDiscover = 1,
							Mode = AddressManagerBehaviorMode.None
						}
					}
				};
				var group = new NodesGroup(builder.Network, nodeConnectionParameters, nodesRequirement);
				group.AllowSameGroup = true;
				var connecting = WaitConnected(group);
				try
				{
					group.Connect();
					connecting.GetAwaiter().GetResult();
				}
				catch (TaskCanceledException)
				{
					// It is expected because no node should connect.
					Assert.Empty(group.ConnectedNodes); // but we chack it anyway.
				}
				finally
				{
					group.Disconnect();
				}

				node.Generate(101);
				group = new NodesGroup(builder.Network, nodeConnectionParameters, nodesRequirement);
				group.AllowSameGroup = true;
				connecting = WaitConnected(group);
				try
				{
					group.Connect();
					connecting.GetAwaiter().GetResult();
					Eventually(() =>
					{
						Assert.NotEmpty(group.ConnectedNodes);
						Assert.All(group.ConnectedNodes, connectedNode => 
							Assert.True(connectedNode.RemoteSocketEndpoint.IsEqualTo(node.NodeEndpoint)));
					});
				}
				finally
				{
					group.Disconnect();
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanHandshakeWithSeveralTemplateBehaviors()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true);
				node.Generate(101);
				AddressManager manager = new AddressManager();
				manager.Add(new NetworkAddress(node.NodeEndpoint), IPAddress.Loopback);

				var chain = new SlimChain(builder.Network.GenesisHash);
				NodesGroup group = new NodesGroup(builder.Network, new NodeConnectionParameters()
				{
					Services = NodeServices.Nothing,
					IsRelay = true,
					TemplateBehaviors =
				{
					new AddressManagerBehavior(manager)
					{
						PeersToDiscover = 1,
						Mode = AddressManagerBehaviorMode.None
					},
					new SlimChainBehavior(chain),
					new PingPongBehavior()
				}
				});
				group.AllowSameGroup = true;
				group.MaximumNodeConnection = 1;
				var connecting = WaitConnected(group);
				try
				{

					group.Connect();
					connecting.GetAwaiter().GetResult();
					Eventually(() =>
					{
						Assert.Equal(101, chain.Height);
					});
					var ms = new MemoryStream();
					chain.Save(ms);

					var chain2 = new SlimChain(chain.Genesis);
					ms.Position = 0;
					chain2.Load(ms);
					Assert.Equal(chain.Tip, chain2.Tip);

					using (var fs = new FileStream("test.slim.dat", FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
					{
						chain.Save(fs);
						fs.Flush();
					}

					chain.ResetToGenesis();
					using (var fs = new FileStream("test.slim.dat", FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024))
					{
						chain.Load(fs);
					}
					Assert.Equal(101, chain2.Height);
					chain.ResetToGenesis();
				}
				finally
				{
					group.Disconnect();
				}
			}
		}
		private static async Task WaitConnected(NodesGroup group)
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			EventHandler<NodeEventArgs> waitingConnected = null;
			waitingConnected = (a, b) =>
			{
				tcs.TrySetResult(true);
				group.ConnectedNodes.Added -= waitingConnected;
			};
			group.ConnectedNodes.Added += waitingConnected;
			CancellationTokenSource cts = new CancellationTokenSource(5000);
			using (cts.Token.Register(() => tcs.TrySetCanceled()))
			{
				await tcs.Task;
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanGetMerkleRoot()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true);
				var rpc = node.CreateRPCClient();
				var nodeClient = node.CreateNodeClient();
				rpc.Generate(101);

				List<IAddressableDestination> knownAddresses = new List<IAddressableDestination>();
				var batch = rpc.PrepareBatch();
				for (int i = 0; i < 20; i++)
				{
					var address = (BitcoinPubKeyAddress)new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, rpc.Network);
					knownAddresses.Add(address.Hash);
#pragma warning disable CS4014
					batch.SendToAddressAsync(address, Money.Coins(0.5m));
#pragma warning restore CS4014
				}
				batch.SendBatch();
				knownAddresses = knownAddresses.Take(10).ToList();
				var blockId = rpc.Generate(1)[0];
				var block = rpc.GetBlock(blockId);
				Assert.Equal(21, block.Transactions.Count);
				var knownTx = block.Transactions[1].GetHash();
				nodeClient.VersionHandshake();
				using (var list = nodeClient.CreateListener()
										.Where(m => m.Message.Payload is MerkleBlockPayload || m.Message.Payload is TxPayload))
				{
					BloomFilter filter = new BloomFilter(1, 0.0001, 50, BloomFlags.UPDATE_NONE);
					foreach (var a in knownAddresses)
						filter.Insert(ToBytes(a));
					nodeClient.SendMessageAsync(new FilterLoadPayload(filter));
					nodeClient.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, block.GetHash())));
					var merkle = list.ReceivePayload<MerkleBlockPayload>();
					var tree = merkle.Object.PartialMerkleTree;
					Assert.True(tree.Check(block.Header.HashMerkleRoot));
					Assert.True(tree.GetMatchedTransactions().Count() >= 10);
					Assert.Contains(knownTx, tree.GetMatchedTransactions());

					List<Transaction> matched = new List<Transaction>();
					for (int i = 0; i < tree.GetMatchedTransactions().Count(); i++)
					{
						matched.Add(list.ReceivePayload<TxPayload>().Object);
					}
					Assert.True(matched.Count >= 10);
					tree = tree.Trim(knownTx);
					Assert.True(tree.GetMatchedTransactions().Count() == 1);
					Assert.Contains(knownTx, tree.GetMatchedTransactions());

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
					nodeClient.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, Network.RegTest.GetGenesis().GetHash())));

					merkle = list.ReceivePayload<MerkleBlockPayload>();
					tree = merkle.Object.PartialMerkleTree;
					Assert.True(tree.Check(merkle.Object.Header.HashMerkleRoot));
					Assert.True(!tree.GetMatchedTransactions().Contains(knownTx));
				}
			}
		}

		private byte[] ToBytes(IAddressableDestination a)
		{
			if (a is WitKeyId wk)
				return wk.ToBytes();
			if (a is KeyId ki)
				return ki.ToBytes();
			if (a is WitScriptId wsk)
				return wsk.ToBytes();
			if (a is ScriptId si)
				return si.ToBytes();
			if (a is TaprootPubKey tp)
				return tp.ToBytes();
			throw new NotSupportedException("Error code 3921: It should, contact NBitcoin developers");
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void MaxConnectionLimit()
		{
			using (var tester = new NodeServerTester())
			{
				tester.Server1.MaxConnections = 4;
				Node.Connect(tester.Network, tester.Server1.ExternalEndpoint).VersionHandshake();
				Node.Connect(tester.Network, tester.Server1.ExternalEndpoint).VersionHandshake();
				Node.Connect(tester.Network, tester.Server1.ExternalEndpoint).VersionHandshake();
				Node.Connect(tester.Network, tester.Server1.ExternalEndpoint).VersionHandshake();

				TestUtils.Eventually(() => tester.Server1.ConnectedNodes.Count == 4);

				var connect = Node.Connect(tester.Network, tester.Server1.ExternalEndpoint);
				try
				{
					connect.VersionHandshake();
				}
				catch
				{
				}

				TestUtils.Eventually(() => tester.Server1.ConnectedNodes.Count == 4);
				TestUtils.Eventually(() => connect.IsConnected == false);
			}
		}


		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanMaintainChainWithChainBehavior()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true).CreateNodeClient();
				builder.Nodes[0].Generate(600);
				var rpc = builder.Nodes[0].CreateRPCClient();
				var chain = node.GetChain(rpc.GetBlockHash(500));
				Assert.True(chain.Height == 500);
				using (var tester = new NodeServerTester())
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
					var behavior = new ChainBehavior(new ConcurrentChain(Network.RegTest));
					n2.Behaviors.Add(behavior);
					TestUtils.Eventually(() => behavior.Chain.Height == 500);
					var chain2 = n2.GetChain(rpc.GetBlockHash(500));
					Assert.True(chain2.Height == 500);
					var chain1 = n1.GetChain(rpc.GetBlockHash(500));
					Assert.True(chain1.Height == 500);
					chain1 = n1.GetChain(rpc.GetBlockHash(499));
					Assert.True(chain1.Height == 499);

					//Should not broadcast above HighestValidatorPoW
					n1.Behaviors.Find<ChainBehavior>().SharedState.HighestValidatedPoW = chain1.GetBlock(300);
					chain1 = n2.GetChain(rpc.GetBlockHash(499));
					Assert.True(chain1.Height == 300);
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanMaintainChainWithSlimChainBehavior()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var nodeClients = new []
				{
					builder.CreateNode(true).CreateNodeClient(),
					builder.CreateNode(true).CreateNodeClient()
				};
				logs.WriteLine("Creating node0 with 300 blocks and node1 with 600 blocks");
				builder.Nodes[0].Generate(300);
				builder.Nodes[1].Generate(600);

				var rpcs = new[]
				{
					builder.Nodes[0].CreateRPCClient(),
					builder.Nodes[1].CreateRPCClient(),
				};

				logs.WriteLine("Let's check if we can get the slim chain from node0 up to 200");
				var slimChain = nodeClients[0].GetSlimChain(rpcs[0].GetBlockHash(200));
				Assert.True(slimChain.Height == 200);

				logs.WriteLine("Let's check if we can now synchronize to tip of node1 (reorg of 200 blocks + 600 blocks)");
				nodeClients[1].SynchronizeSlimChain(slimChain);
				Assert.Equal(slimChain.Tip, rpcs[1].GetBestBlockHash());

				logs.WriteLine("Let's now use a SlimChainBehavior to sync back to node0 (300 blocks)");
				nodeClients[0].Behaviors.Add(new SlimChainBehavior(slimChain));
				Eventually(() =>
				{
					try
					{
						Assert.Equal(slimChain.Tip, rpcs[0].GetBestBlockHash());
					}
					catch
					{
						logs.WriteLine("Chain tip is now at " + slimChain.Height);
						throw;
					}
				});
				logs.WriteLine("Let's now reorg node0 to node1 (600 blocks) and see if the SlimChainBehavior can keep up");
				builder.Nodes[1].Sync(builder.Nodes[0]);
				Eventually(() =>
				{
					try
					{
						Assert.Equal(slimChain.Tip, rpcs[1].GetBestBlockHash());
					}
					catch
					{
						logs.WriteLine("Chain tip is now at " + slimChain.Height);
						throw;
					}
				});
			}
		}
		private void Eventually(Action act)
		{
			CancellationTokenSource cts = new CancellationTokenSource(30000);
			while (true)
			{
				try
				{
					act();
					break;
				}
				catch (XunitException) when (!cts.Token.IsCancellationRequested)
				{
					cts.Token.WaitHandle.WaitOne(500);
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanCancelConnection()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true);
				CancellationTokenSource cts = new CancellationTokenSource();
				cts.Cancel();
				try
				{
					var client = Node.Connect(Network.RegTest, "127.0.0.1:" + node.ProtocolPort.ToString(), new NodeConnectionParameters()
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
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				node.ConfigParameters.Add("whitelist", "127.0.0.1");
				node.Start();
				var rpc = node.CreateRPCClient();
				rpc.Generate(101);
				rpc.SendToAddress(new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.RegTest), Money.Coins(1.0m));
				var client = node.CreateNodeClient();
				client.VersionHandshake();
				var transactions = client.GetMempoolTransactions();
				Assert.True(transactions.Length == 1);
			}
		}

#if !NOFILEIO
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConnectToRandomNode()
		{
			Stopwatch watch = new Stopwatch();
			NodeConnectionParameters parameters = new NodeConnectionParameters();
			var addrman = GetCachedAddrMan("addrmancache.dat");
			parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addrman)
			{
				PeersToDiscover = 50
			});
			watch.Start();
			using (var node = Node.Connect(Network.Main, parameters))
			{
				var timeToFind = watch.Elapsed;
				node.VersionHandshake();
				node.Dispose();
				watch.Restart();
				using (var node2 = Node.Connect(Network.Main, parameters))
				{
					var timeToFind2 = watch.Elapsed;
				}
			}
			addrman.SavePeerFile("addrmancache.dat", Network.Main);
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
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true);
				var rpc = node.CreateRPCClient();
				rpc.Generate(50);
				var client = node.CreateNodeClient();
				var chain = client.GetChain();
				var blocks = client.GetBlocks(chain.GetBlock(20).HashBlock).ToArray();
				Assert.Equal(20, blocks.Length);
				Assert.Equal(chain.GetBlock(20).HashBlock, blocks.Last().Header.GetHash());

				blocks = client.GetBlocksFromFork(chain.GetBlock(45)).ToArray();
				Assert.Equal(5, blocks.Length);
				Assert.Equal(chain.GetBlock(50).HashBlock, blocks.Last().Header.GetHash());
				Assert.Equal(chain.GetBlock(46).HashBlock, blocks.First().Header.GetHash());
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanGetMemPool()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				node.ConfigParameters.Add("whitelist", "127.0.0.1");
				node.Start();
				rpc.Generate(102);
				for (int i = 0; i < 2; i++)
					node.CreateRPCClient().SendToAddress(new Key().PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.RegTest), Money.Coins(1.0m));
				var client = node.CreateNodeClient();
				var txIds = client.GetMempool();
				Assert.True(txIds.Length == 2);
			}
		}


		[Fact]
		[Trait("Protocol", "Protocol")]
		public async Task CanMaskExceptionThrownByMessageReceivers()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				var rpc = node.CreateRPCClient();
				node.Start();
				var nodeClient = node.CreateNodeClient();
				TaskCompletionSource<bool> ok = new TaskCompletionSource<bool>();
				nodeClient.VersionHandshake();
				nodeClient.UncaughtException += (s, m) =>
				{
					ok.TrySetResult(m.GetType() == typeof(Exception) && m.Message == "test");
				};
				nodeClient.MessageReceived += (s, m) =>
				{
					throw new Exception("test");
				};
				nodeClient.SendMessage(new PingPayload());
				Assert.True(await ok.Task);
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void SynchronizeChainSurviveReorg()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				ConcurrentChain chain = new ConcurrentChain(Network.RegTest);
				var node1 = builder.CreateNode(true);
				node1.Generate(10);
				node1.CreateNodeClient().SynchronizeChain(chain);
				Assert.Equal(10, chain.Height);


				var node2 = builder.CreateNode(true);
				node2.Generate(12);

				var node2c = node2.CreateNodeClient();
				node2c.PollHeaderDelay = TimeSpan.FromSeconds(2);
				node2c.SynchronizeChain(chain);
				Assert.Equal(12, chain.Height);
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanGetChainsConcurrenty()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				bool generating = true;
				var node = builder.CreateNode(true);
				var rpc = node.CreateRPCClient();
				Task.Run(() =>
				{
					rpc.Generate(600);
					generating = false;
				});
				var nodeClient = node.CreateNodeClient();
				nodeClient.PollHeaderDelay = TimeSpan.FromSeconds(2);
				nodeClient.VersionHandshake();
				Random rand = new Random();
				Thread.Sleep(1000);
				var chains =
					Enumerable.Range(0, 5)
					.Select(_ => Task.Factory.StartNew(() =>
					{
						Thread.Sleep(rand.Next(0, 1000));
						return nodeClient.GetChain();
					}))
					.Select(t => t.Result)
					.ToArray();
				while (generating)
				{
					SyncAll(nodeClient, rand, chains);
				}
				SyncAll(nodeClient, rand, chains);
				foreach (var c in chains)
				{
					Assert.Equal(600, c.Height);
				}

				var chainNoHeader = nodeClient.GetChain(new SynchronizeChainOptions() { SkipPoWCheck = true, StripHeaders = true });
				Assert.False(chainNoHeader.Tip.HasHeader);
			}
		}

		private static void SyncAll(Node node, Random rand, ConcurrentChain[] chains)
		{
			Task.WaitAll(Enumerable.Range(0, 5)
								.Select(_ => Task.Factory.StartNew(() =>
								{
									Thread.Sleep(rand.Next(0, 1000));
									node.SynchronizeChain(chains[_]);
								})).ToArray());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ServerDisconnectCorrectlyFromDroppingClient()
		{
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
			using (var tester = new NodeServerTester())
			{
				var toS2 = tester.Node1;
				toS2.VersionHandshake();
				Assert.Equal(NodeState.HandShaked, toS2.State);
				Thread.Sleep(100); //Let the time to Server2 to add the new node, else the test was failing sometimes.
				Assert.Equal(NodeState.HandShaked, tester.Node2.State);
				Assert.NotNull(toS2.TimeOffset);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRespondToPong()
		{

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
			using (var tester = new NodeServerTester())
			{
				tester.Server2.Nonce = tester.Server1.Nonce;
				Assert.Throws<InvalidOperationException>(() =>
				{
					tester.Node1.VersionHandshake();
				});
			}
		}


		//[Fact]
		// This test is disabled because it relies on hosts that might
		// be up and down. Please, if you want to test it, adapt the links
		// You need to run Tor Browser (the test use socks port 9150, not 9050)

#pragma warning disable xUnit1013 // Public method should be marked as test
		//[Fact]
		public async Task TestDifferentConnectionMethods()
#pragma warning restore xUnit1013 // Public method should be marked as test
		{
			var hosts = new[]
				{
				// Should works with IPv6
				"[2406:da18:f7c:4351:94e0:5b27:78c2:5111]:8333",

				// Should works for onion
				"7xnmrhmkvptbcvpl.onion:8333",

				// Should works for onioncat
				Utils.ParseEndpoint("7xnmrhmkvptbcvpl.onion:8333", 8333).AsOnionCatIPEndpoint().ToEndpointString(),

				// Should works for ipv4
				"38.140.62.62",

				// Should works for ipv4 mapped
				"[::ffff:38.140.62.62]",

				// Should works for DNS names
				"ec2-52-14-64-82.us-east-2.compute.amazonaws.com"
				};
			foreach (var (onlyForOnionHosts, changeIpIdentities) in new[] { (true, true), (true, false), (false, true), (false, false) })
			{
				foreach (var endpoint in hosts.Select(h => Utils.ParseEndpoint(h, Network.Main.DefaultPort)))
				{
					if (endpoint is IPEndPoint ipv6 && !ipv6.IsTor() && onlyForOnionHosts)
						continue; // My network does not support ipv6 without Tor so I disable this test
					using (var cancellationToken = new CancellationTokenSource(20000))
					{
						var node = await Node.ConnectAsync(Network.Main, endpoint, new NodeConnectionParameters()
						{
							TemplateBehaviors =
							{
								new SocksSettingsBehavior(Utils.ParseEndpoint("localhost", 9150), onlyForOnionHosts, null, changeIpIdentities)
							},
							ConnectCancellation = cancellationToken.Token
						});

						node.VersionHandshake();
						node.DisconnectAsync();
					}
				}
			}
		}

		// Disabled because it relies on tor which make tests shaky
		//[Fact]
		//[Trait("UnitTest", "UnitTest")]
#pragma warning disable xUnit1013 // Public method should be marked as test
		public async Task CanResolveTor()
#pragma warning restore xUnit1013 // Public method should be marked as test
		{
			var resolver = new DnsSocksResolver(Utils.ParseEndpoint("localhost", 9050));
			var ex1 = await Assert.ThrowsAsync<SocketException>(async () => await resolver.GetHostAddressesAsync("googlekefwjefjfwqk.com", default));
			var ex2 = await Assert.ThrowsAsync<SocketException>(async () => await DnsResolver.Instance.GetHostAddressesAsync("googlekefwjefjfwqk.com", default));
			Assert.Equal(ex1.ErrorCode, ex2.ErrorCode);
			var ip = await resolver.GetHostAddressesAsync("google.com", default);
			Assert.NotNull(ip);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanExchangeFastPingPong()
		{
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
				Assert.StartsWith("Pong timeout", n2.DisconnectReason.Reason, StringComparison.Ordinal);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConnectMultipleTimeToServer()
		{
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
				Assert.Throws<InvalidOperationException>(() => n2.VersionHandshake());
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
		public void CanRoundtripCmpctBlock()
		{
			Block block = Network.Main.Consensus.ConsensusFactory.CreateBlock();
			block.Transactions.Add(Network.Main.Consensus.ConsensusFactory.CreateTransaction());
			var cmpct = new CmpctBlockPayload(block);
			cmpct.Clone();
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanDownloadBlock()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true).CreateNodeClient();
				node.VersionHandshake();
				using (var listener = node.CreateListener())
				{
					node.SendMessageAsync(new GetDataPayload(new InventoryVector()
					{
						Hash = Network.RegTest.GenesisHash,
						Type = InventoryType.MSG_BLOCK
					}));
					var block = listener.ReceivePayload<BlockPayload>();
					Assert.True(block.Object.CheckMerkleRoot());
				}
			}
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void CanDownloadHeaders()
		{
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true).CreateNodeClient();
				builder.Nodes[0].Generate(50);
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
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true).CreateNodeClient();
				builder.Nodes[0].Generate(50);
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
			using (var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode(true).CreateNodeClient();
				builder.Nodes[0].Generate(150);
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
