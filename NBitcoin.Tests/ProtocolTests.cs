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
		public NodeServerTester()
		{
			_Server1 = new NodeServer(Network.Main, internalPort: 3390);
			_Server1.AllowLocalPeers = true;
			_Server1.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6(), 3390);
			_Server1.NATRuleName = NATRuleName;
			_Server1.Listen();
			_Server2 = new NodeServer(Network.Main, internalPort: 3391);
			_Server2.AllowLocalPeers = true;
			_Server2.NATRuleName = NATRuleName;
			_Server2.ExternalEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1").MapToIPv6(), 3391);
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
		[Trait("Network", "Network")]
		public void CanHandshake()
		{

			using(var server = new NodeServer(Network.Main, ProtocolVersion.PROTOCOL_VERSION))
			{
				var seed = server.GetNodeByHostName("seed.bitcoin.sipa.be");
				Assert.True(seed.State == NodeState.Connected);
				seed.VersionHandshake();
				Assert.True(seed.State == NodeState.HandShaked);
				seed.Disconnect();
				Assert.True(seed.State == NodeState.Offline);
			}
		}

		[Fact]
		[Trait("Network", "Network")]
		public void CanDiscoverNodes()
		{
			using(var server = new NodeServer(Network.Main, ProtocolVersion.PROTOCOL_VERSION))
			{
				Assert.True(server.CountPeerRequired() > 500);
				server.DiscoverPeers();
				Assert.True(server.CountPeerRequired() < 10);
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
				Assert.Equal(NodeState.HandShaked, tester.Server2.GetNodeByEndpoint(toS2.ExternalEndpoint).State);
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
				var pong = toS2.RecieveMessage<PongPayload>(TimeSpan.FromSeconds(10.0));
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
		[Trait("NodeServer", "NodeServer")]
		public void CanConnectToSeveralNodes()
		{
			UPnPLease.ReleaseAll(NodeServerTester.NATRuleName); //Clean the gateway of previous tests attempt
			using(var server = new NodeServer(Network.Main))
			{
				server.NATRuleName = NodeServerTester.NATRuleName;
				//var nodes = server.CreateNodeSet(10);
			}
		}
	}
}
