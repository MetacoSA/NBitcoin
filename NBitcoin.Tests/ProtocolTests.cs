#if !NOSOCKET
using System;
using System.Collections.Generic;
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
using System.Diagnostics;

namespace NBitcoin.Tests
{
	public class NodeServerTester : IDisposable
	{
		static Random _Rand = new Random();
		public NodeServerTester(Network network = null)
		{
			network = network ?? Network.TestNet;
			var a = _Rand.Next(4000, 60000);
			var b = _Rand.Next(4000, 60000);
			_Server1 = new NodeServer(network, internalPort: a);
			_Server1.AllowLocalPeers = true;
			_Server1.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6(), a);
			_Server1.NATRuleName = NATRuleName;
			_Server1.Listen();
			_Server2 = new NodeServer(network, internalPort: b);
			_Server2.AllowLocalPeers = true;
			_Server2.NATRuleName = NATRuleName;
			_Server2.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6(), b);
			_Server2.Listen();
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
				return _Node1;
			}
		}

		Node _Node2;
		public Node Node2
		{
			get
			{
				_Node2 = _Node2 ?? Server1.FindOrConnect(Server2.ExternalEndpoint);
				return _Node2;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			_Server1.Dispose();
			_Server2.Dispose();
		}

		#endregion

		public static string NATRuleName = "NBitcoin Tests";
	}
	public class ProtocolTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//Copied from https://en.bitcoin.it/wiki/Protocol_specification (19/04/2014)
		public void CanParseMessages()
		{
			var EST = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			var tests = new[]
				{
					new
					{
						Version = ProtocolVersion.INIT_PROTO_VERSION,
						Message = "f9beb4d976657273696f6e0000000000550000009c7c00000100000000000000e615104d00000000010000000000000000000000000000000000ffff0a000001208d010000000000000000000000000000000000ffff0a000002208ddd9d202c3ab457130055810100",
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
						Version = ProtocolVersion.PROTOCOL_VERSION,
						Message = "f9beb4d976657261636b000000000000000000005df6e0e2",
						Test = new Action<object>(o=>
							{
								var verack = (VerAckPayload)o;
							})
					},
					new
					{
						Version = ProtocolVersion.MEMPOOL_GD_VERSION,
						Message = "f9beb4d96164647200000000000000001f000000ed52399b01e215104d010000000000000000000000000000000000ffff0a000001208d",
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

			foreach(var test in tests)
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
		[Trait("Network", "Network")]
		public void CanGetMyIp()
		{
			var client = new NodeServer(Network.Main, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(client.GetMyExternalIP() != null);
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanHandshake()
		{
			using(var seed = Node.ConnectToLocal(Network.TestNet))
			{
				Assert.True(seed.State == NodeState.Connected);
				seed.VersionHandshake();
				Assert.True(seed.State == NodeState.HandShaked);
				seed.Disconnect();
				Assert.True(seed.State == NodeState.Offline);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanGetMerkleRoot()
		{
			using(var node = Node.ConnectToLocal(Network.TestNet, isRelay: false))
			{
				var knownBlock = new uint256("00000000db9a24016f87f98ddaf08d32383319431d27c37dee2c91898ef57066");
				var knownTx = new uint256("dabf4960a5c6d9affec746734cbd8ba68287126b8c4514de846a9702a813a449");
				node.VersionHandshake();
				using(var list = node.CreateListener()
										.Where(m => m.Message.Payload is MerkleBlockPayload || m.Message.Payload is TxPayload))
				{
					BloomFilter filter = new BloomFilter(1, 0.005, 50, BloomFlags.UPDATE_NONE);
					filter.Insert(BitcoinAddress.Create("mwdJkHRNJi1fEwHBx6ikWFFuo2rLBdri2h", Network.TestNet).Hash.ToBytes());
					node.SendMessageAsync(new FilterLoadPayload(filter));
					node.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, knownBlock)));
					var merkle = list.ReceivePayload<MerkleBlockPayload>();
					var tree = merkle.Object.PartialMerkleTree;
					Assert.True(tree.Check(new uint256("89b905cdf2ab70c1acd9b538cf6738937ae28fca86c1514ebbf130962312e478")));
					Assert.True(tree.GetMatchedTransactions().Count() > 1);
					Assert.True(tree.GetMatchedTransactions().Contains(knownTx));

					List<Transaction> matched = new List<Transaction>();
					for(int i = 0 ; i < tree.GetMatchedTransactions().Count() ; i++)
					{
						matched.Add(list.ReceivePayload<TxPayload>().Object);
					}
					Assert.True(matched.Count > 1);
					tree = tree.Trim(knownTx);
					Assert.True(tree.GetMatchedTransactions().Count() == 1);
					Assert.True(tree.GetMatchedTransactions().Contains(knownTx));

					Action act = () =>
					{
						foreach(var match in matched)
						{
							Assert.True(filter.IsRelevantAndUpdate(match));
						}
					};
					act();
					filter = filter.Clone();
					act();

					var unknownBlock = new uint256("00000000ad262227291eaf90cafdc56a8f8451e2d7653843122c5bb0bf2dfcdd");
					node.SendMessageAsync(new GetDataPayload(new InventoryVector(InventoryType.MSG_FILTERED_BLOCK, Network.TestNet.GetGenesis().GetHash())));

					merkle = list.ReceivePayload<MerkleBlockPayload>();
					tree = merkle.Object.PartialMerkleTree;
					Assert.True(tree.Check(merkle.Object.Header.HashMerkleRoot));
					Assert.True(!tree.GetMatchedTransactions().Contains(knownTx));
				}
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanGetMemPool()
		{
			using(var node = Node.ConnectToLocal(Network.TestNet))
			{
				var txIds = node.GetMempool();
				Assert.True(txIds.Length > 0);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanMaintainChainWithChainBehavior()
		{
			using(var node = Node.ConnectToLocal(Network.TestNet))
			{
				var chain = node.GetChain(new uint256("00000000a2424460c992803ed44cfe0c0333e91af04fde9a6a97b468bf1b5f70"));
				Assert.True(chain.Height == 500);
				using(var tester = new NodeServerTester(Network.TestNet))
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
					var behavior = new ChainBehavior(new ConcurrentChain(Network.TestNet));
					n2.Behaviors.Add(behavior);
					TestUtils.Eventually(() => behavior.Chain.Height == 500);
					var chain2 = n2.GetChain(new uint256("00000000a2424460c992803ed44cfe0c0333e91af04fde9a6a97b468bf1b5f70"));
					Assert.True(chain2.Height == 500);
					var chain1 = n1.GetChain(new uint256("00000000a2424460c992803ed44cfe0c0333e91af04fde9a6a97b468bf1b5f70"));
					Assert.True(chain1.Height == 500);
					chain1 = n1.GetChain(new uint256("000000008cd4b1bdaa1278e3f1708258f862da16858324e939dc650627cd2e27"));
					Assert.True(chain1.Height == 499);
					Thread.Sleep(5000);
				}
			}
		}

		[Fact]
		[Trait("MainNet", "MainNet")]
		public void CanGetTransactionsFromMemPool()
		{
			using(var node = Node.ConnectToLocal(Network.Main))
			{
				var transactions = node.GetMempoolTransactions();
				Assert.True(transactions.Length > 0);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ServerDisconnectCorrectlyFromDroppingClient()
		{
			using(var tester = new NodeServerTester())
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
		[Trait("Network", "Network")]
		public void CanConnectToRandomNode()
		{
			Stopwatch watch = new Stopwatch();
			NodeConnectionParameters parameters = new NodeConnectionParameters();
			var addrman = GetCachedAddrMan("addrmancache.dat");
			parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addrman));
			watch.Start();
			using(var node = Node.Connect(Network.Main, parameters))
			{
				var timeToFind = watch.Elapsed;
				node.VersionHandshake();
				node.Dispose();
				watch.Restart();
				using(var node2 = Node.Connect(Network.Main, parameters))
				{
					var timeToFind2 = watch.Elapsed;
				}
			}
			addrman.SavePeerFile("addrmancache.dat", Network.Main);
		}

		private AddressManager GetCachedAddrMan(string file)
		{
			if(File.Exists(file))
			{
				return AddressManager.LoadPeerFile(file);
			}
			return new AddressManager();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReceiveHandshake()
		{
			using(var tester = new NodeServerTester())
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

			using(var tester = new NodeServerTester())
			{
				var toS2 = tester.Node1;
				toS2.VersionHandshake();
				var ping = new PingPayload();
				CancellationTokenSource cancel = new CancellationTokenSource();
				cancel.CancelAfter(10000);
				using(var list = toS2.CreateListener())
				{
					toS2.SendMessageAsync(ping);
					while(true)
					{
						var pong = list.ReceivePayload<PongPayload>(cancel.Token);
						if(ping.Nonce == pong.Nonce)
							break;
					}
				}

			}

		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CantConnectToYourself()
		{
			using(var tester = new NodeServerTester())
			{
				tester.Server2.Nonce = tester.Server1.Nonce;
				Assert.Throws(typeof(InvalidOperationException), () =>
				{
					tester.Node1.VersionHandshake();
				});
			}
		}

		[Fact]
		[Trait("UnitTest", "Buggy")]
		public void CanExchangeFastPingPong()
		{
			using(var tester = new NodeServerTester())
			{
				var n1 = tester.Node1;
				n1.Behaviors.Clear();
				n1.Behaviors.Add(new PingPongBehavior()
				{
					PingInterval = TimeSpan.FromSeconds(0.1),
					TimeoutInterval = TimeSpan.FromSeconds(1.0)
				});

				n1.VersionHandshake();
				Assert.Equal(NodeState.HandShaked, n1.State);
				Assert.True(!n1.Inbound);

				var n2 = tester.Node2;
				n2.Behaviors.Clear();
				n2.Behaviors.Add(new PingPongBehavior()
				{
					PingInterval = TimeSpan.FromSeconds(0.1),
					TimeoutInterval = TimeSpan.FromSeconds(1.0)
				});
				Assert.Equal(NodeState.HandShaked, n2.State);
				Assert.True(n2.Inbound);
				Thread.Sleep(2000);
				Assert.Equal(NodeState.HandShaked, n2.State);
				n1.Behaviors.Clear();
				Thread.Sleep(2000);
				Assert.True(n2.State == NodeState.Disconnecting || n2.State == NodeState.Offline);
				Assert.True(n2.DisconnectReason.Reason == "Pong timeout");
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConnectMultipleTimeToServer()
		{
			using(var tester = new NodeServerTester())
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
			var hex = "f9beb4d972656a6563740000000000003a000000db7f7e7802747812156261642d74786e732d696e707574732d7370656e74577a9694da4ff41ae999f6591cff3749ad6a7db19435f3d8af5fecbcff824196";
			Message message = new Message();
			message.ReadWrite(Encoders.Hex.DecodeData(hex));
			var reject = (RejectPayload)message.Payload;
			Assert.True(reject.Message == "tx");
			Assert.True(reject.Code == RejectCode.DUPLICATE);
			Assert.True(reject.CodeType == RejectCodeType.Transaction);
			Assert.True(reject.Reason == "bad-txns-inputs-spent");
			Assert.True(reject.Hash == new uint256("964182ffbcec5fafd8f33594b17d6aad4937ff1c59f699e91af44fda94967a57"));
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanDownloadBlock()
		{
			using(var node = Node.ConnectToLocal(Network.TestNet))
			{
				node.VersionHandshake();
				node.SendMessageAsync(new GetDataPayload(new InventoryVector()
						{
							Hash = new uint256("00000000278d16a190be56f541b3fda44c3168b43dcc05d9c664e6f27ffe2c78"),
							Type = InventoryType.MSG_BLOCK
						}));

				var block = node.ReceiveMessage<BlockPayload>();
				Assert.True(block.Object.CheckMerkleRoot());
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanDownloadHeaders()
		{
			using(var node = Node.ConnectToLocal(Network.TestNet))
			{
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
		[Trait("NodeServer", "NodeServer")]
		public void CanDownloadBlocks()
		{
			using(var node = Node.ConnectToLocal(Network.TestNet))
			{
				var chain = node.GetChain();
				chain.SetTip(chain.GetBlock(9));
				var blocks = node.GetBlocks(chain.ToEnumerable(true).Select(c => c.HashBlock)).ToList();
				foreach(var block in blocks)
				{
					Assert.True(block.CheckMerkleRoot());
				}
				Assert.Equal(10, blocks.Count);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanDownloadLastBlocks()
		{
			using(var node = Node.ConnectToLocal(Network.TestNet))
			{
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

		[Fact]
		public void CanUseUPNP()
		{
			UPnPLease lease = null;
			UPnPLease.ReleaseAll(NodeServerTester.NATRuleName); //Clean the gateway of previous tests attempt
			using(var server = new NodeServer(Network.Main))
			{
				server.NATRuleName = NodeServerTester.NATRuleName;
				Assert.False(server.ExternalEndpoint.Address.IsRoutable(false));
				lease = server.DetectExternalEndpoint();
				Assert.True(server.ExternalEndpoint.Address.IsRoutable(false));
				Assert.NotNull(lease);
				Assert.True(lease.IsOpen());
				lease.Dispose();
				Assert.False(lease.IsOpen());
				lease = server.DetectExternalEndpoint();
				Assert.NotNull(lease);
				Assert.True(lease.IsOpen());
			}
			Assert.False(lease.IsOpen());
		}
	}
}
#endif