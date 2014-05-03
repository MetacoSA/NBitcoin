using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class RepositoryTests
	{
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
	}
}
