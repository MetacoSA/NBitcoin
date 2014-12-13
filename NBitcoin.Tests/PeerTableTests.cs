#if !NOSQLITE
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class PeerTableTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanStorePeers()
		{
			PeerTable table = new PeerTable();
			table.Randomize = false;
			AssertPeer(table, "1.0.0.0", p => Assert.Null(p));
			AddPeer(table, "1.0.0.0", PeerOrigin.Addr, DateTimeOffset.Now);
			AssertPeer(table, "1.0.0.0", p => Assert.NotNull(p));
			Assert.True(table.GetActivePeers(10).Length == 1);

			AssertPeer(table, "1.0.0.1", p => Assert.Null(p));
			AddPeer(table, "1.0.0.1", PeerOrigin.Addr, DateTimeOffset.Now - TimeSpan.FromHours(4.0));
			AssertPeer(table, "1.0.0.1", p => Assert.Null(p));

			//Second peer should be expired
			Assert.True(table.GetActivePeers(10).Length == 1);

			AddPeer(table, "1.0.0.1", PeerOrigin.Addr, DateTimeOffset.Now - TimeSpan.FromHours(2.0));
			Assert.True(table.GetActivePeers(10).Length == 2);

			Assert.True(table.GetActivePeers(1).Length == 1);

			AddPeer(table, "1.0.0.2", PeerOrigin.DNSSeed, DateTimeOffset.Now);

			var peers = table.GetActivePeers(10);
			Assert.Equal(CreateEndpoint("1.0.0.0"), peers[0].NetworkAddress.Endpoint); //Most recent
			Assert.Equal(CreateEndpoint("1.0.0.1"), peers[1].NetworkAddress.Endpoint);
			Assert.Equal(CreateEndpoint("1.0.0.2"), peers[2].NetworkAddress.Endpoint); //Seeds
			Assert.Equal(2, table.CountUsed());

			//Can add two time a seed
			AddPeer(table, "1.0.0.2", PeerOrigin.DNSSeed, DateTimeOffset.Now);

			AddPeer(table, "1.0.0.1", PeerOrigin.Addr, DateTimeOffset.Now - TimeSpan.FromHours(4.0));

			peers = table.GetActivePeers(10);
			Assert.Equal(CreateEndpoint("1.0.0.0"), peers[0].NetworkAddress.Endpoint); //Most recent
			//Assert.Equal(CreateEndpoint("1.0.0.1"), peers[1].NetworkAddress.Endpoint); Expired
			Assert.Equal(CreateEndpoint("1.0.0.2"), peers[1].NetworkAddress.Endpoint); //Seeds

			Assert.Equal(1, table.CountUsed()); //Seed does not count
			Assert.Equal(2, table.CountUsed(false)); //Seed count


			var p1 = AddPeer(table, "1.0.0.4", PeerOrigin.DNSSeed, DateTimeOffset.Now - TimeSpan.FromHours(4.0));

			Assert.Equal(3, table.CountUsed(false)); //Expired seeds should still appear
			table.RemovePeer(p1);
			Assert.Equal(2, table.CountUsed(false)); //Removed
		}

		private IPEndPoint CreateEndpoint(string ip)
		{
			return new IPEndPoint(IPAddress.Parse(ip).MapToIPv6(), 100);
		}

		private void AssertPeer(PeerTable table, string peer, Action<Peer> test)
		{
			test(table.GetPeer(new IPEndPoint(IPAddress.Parse(peer), 100)));
		}

		private Peer AddPeer(PeerTable table, string addr, PeerOrigin peerOrigin, DateTimeOffset now)
		{
			var peer = new Peer(peerOrigin, new NetworkAddress()
			{
				Endpoint = new IPEndPoint(IPAddress.Parse(addr), 100),
				Time = now
			});
			table.WritePeer(peer);
			return peer;
		}
	}
}
#endif