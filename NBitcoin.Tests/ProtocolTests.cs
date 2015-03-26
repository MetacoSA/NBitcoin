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

namespace NBitcoin.Tests
{
	public class NodeServerTester : IDisposable
	{
		static Random _Rand = new Random();
		public NodeServerTester()
		{
			var a = _Rand.Next(4000, 60000);
			var b = _Rand.Next(4000, 60000);
			_Server1 = new NodeServer(Network.Main, internalPort: a);
			_Server1.AllowLocalPeers = true;
			_Server1.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6(), a);
			_Server1.NATRuleName = NATRuleName;
			_Server1.Listen();
			_Server2 = new NodeServer(Network.Main, internalPort: b);
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
			using(var server = new NodeServer(Network.TestNet, ProtocolVersion.PROTOCOL_VERSION))
			{
				var seed = server.GetLocalNode();
				Assert.True(seed.State == NodeState.Connected);
				seed.VersionHandshake();
				Assert.True(seed.State == NodeState.HandShaked);
				seed.Disconnect();
				Assert.True(seed.State == NodeState.Offline);
			}
		}
		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanGetMemPool()
		{
			using(var server = new NodeServer(Network.TestNet, ProtocolVersion.PROTOCOL_VERSION))
			{
				var node = server.GetLocalNode();
				var txIds = node.GetMempool();
				Assert.True(txIds.Length > 0);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanGetTransactionsFromMemPool()
		{
			using(var server = new NodeServer(Network.TestNet, ProtocolVersion.PROTOCOL_VERSION))
			{
				var node = server.GetLocalNode();
				var transactions = node.GetMempoolTransactions();
				Assert.True(transactions.Length > 0);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void ServerDisconnectCorrectlyFromDroppingClient()
		{
			using(var tester = new NodeServerTester())
			{
				var to2 = tester.Server1.GetNodeByEndpoint(tester.Server2.ExternalEndpoint);
				to2.VersionHandshake();
				Assert.True(tester.Server1.IsConnectedTo(tester.Server2.ExternalEndpoint));
				Thread.Sleep(500);
				Assert.True(tester.Server2.IsConnectedTo(tester.Server1.ExternalEndpoint));
				to2.Disconnect();
				Assert.False(tester.Server1.IsConnectedTo(tester.Server2.ExternalEndpoint));
				Thread.Sleep(500);
				Assert.False(tester.Server2.IsConnectedTo(tester.Server1.ExternalEndpoint));
			}
		}

		[Fact]
		[Trait("Network", "Network")]
		public void CanDiscoverPeers()
		{
			using(var server = new NodeServer(Network.Main, ProtocolVersion.PROTOCOL_VERSION))
			{
				Assert.True(server.PeerTable.CountUsed(true) < 50);
				server.DiscoverPeers(100);
				Assert.True(server.PeerTable.CountUsed(true) > 50);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanReceiveHandshake()
		{
			using(var tester = new NodeServerTester())
			{
				var toS2 = tester.Server1.GetNodeByEndpoint(tester.Server2.ExternalEndpoint);
				toS2.VersionHandshake();
				Assert.Equal(NodeState.HandShaked, toS2.State);
				Thread.Sleep(100); //Let the time to Server2 to add the new node, else the test was failing sometimes.
				Assert.Equal(NodeState.HandShaked, tester.Server2.GetNodeByEndpoint(toS2.MyVersion.AddressFrom).State);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CanRespondToPong()
		{
			using(var tester = new NodeServerTester())
			{
				var toS2 = tester.Server1.GetNodeByEndpoint(tester.Server2.ExternalEndpoint);
				toS2.VersionHandshake();
				var ping = new PingPayload();
				toS2.SendMessage(ping);
				var pong = toS2.ReceiveMessage<PongPayload>(TimeSpan.FromSeconds(10.0));
				Assert.Equal(ping.Nonce, pong.Nonce);
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
		public void CantConnectToYourself()
		{
			using(var tester = new NodeServerTester())
			{
				tester.Server2.Nonce = tester.Server1.Nonce;
				Assert.Throws(typeof(InvalidOperationException), () =>
				{
					tester.Server1.GetNodeByEndpoint(tester.Server2.ExternalEndpoint).VersionHandshake();
				});
			}
		}

		[Fact]
		[Trait("NodeServer", "NodeServer")]
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
				node.SendMessage(new GetDataPayload(new InventoryVector()
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

				var subChain = chain.CreateSubChain(chain.ToEnumerable(true).Skip(99).First(), true, chain.Tip, true);

				var begin = node.Counter.Snapshot();
				var blocks = node.GetBlocks(subChain.ToEnumerable(true).Select(c => c.HashBlock)).Select(_ => 1).ToList();
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

		[Fact]
		[Trait("Network", "Network")]
		public void CanConnectToNodeSet()
		{
			using(var server = new NodeServer(Network.Main))
			{
				server.RegisterPeerTableRepository(PeerCache);
				var set = server.CreateNodeSet(5);
				Assert.Equal(5, set.GetNodes().Length);
				foreach(var node in set.GetNodes())
				{
					Assert.Equal(NodeState.HandShaked, node.State);
				}
			}
		}


		PeerTableRepository _PeerCache;
		public PeerTableRepository PeerCache
		{
			get
			{
				if(_PeerCache == null)
					_PeerCache = new SqLitePeerTableRepository("PeerCache");
				return _PeerCache;
			}
		}

	}
}
#endif