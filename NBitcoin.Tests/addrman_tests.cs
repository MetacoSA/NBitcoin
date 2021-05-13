#if !NOSOCKET
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class addrman_tests
	{
#if !NOFILEIO
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeDeserializePeerTable()
		{
			AddressManager addrman = new AddressManager();
			addrman.SavePeerFile("CanSerializeDeserializePeerTable.dat", Network.Main);
			AddressManager.LoadPeerFile("CanSerializeDeserializePeerTable.dat", Network.Main);

			addrman = AddressManager.LoadPeerFile("../../../data/peers.dat", Network.Main);
			addrman.DebugMode = true;
			addrman.Check();
			addrman.SavePeerFile("serializerPeer.dat", Network.Main);

			AddressManager addrman2 = AddressManager.LoadPeerFile("serializerPeer.dat", Network.Main);
			addrman2.DebugMode = true;
			addrman2.Check();
			addrman2.SavePeerFile("serializerPeer2.dat", Network.Main);

			var original = File.ReadAllBytes("serializerPeer2.dat");
			var after = File.ReadAllBytes("serializerPeer.dat");
			Assert.True(original.SequenceEqual(after));
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeDeserializePeerTableAfterBIP155()
		{
			AddressManager addrman = new AddressManager();
			var netaddr = new NetworkAddress(new DnsEndPoint("wasabiukrxmkdgve5kynjztuovbg43uxcbcxn6y2okcrsg7gb6jdmbad.onion", 8333));
			Assert.True(netaddr.AddressType == NetworkAddressType.Onion);
			addrman.Add(netaddr);
			addrman.SavePeerFile("serializerPeerBIP155.dat", Network.Main);
			var loaddedAddrMgr = AddressManager.LoadPeerFile("serializerPeerBIP155.dat", Network.Main);

			var addr = addrman.Select();
			Assert.True(addr.Endpoint is DnsEndPoint);
			var dns = addr.Endpoint as DnsEndPoint;
			Assert.Equal("wasabiukrxmkdgve5kynjztuovbg43uxcbcxn6y2okcrsg7gb6jdmbad.onion", dns.Host);
			Assert.Equal(8333, dns.Port);
		}

#endif

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUseAddrManager()
		{
			AddressManager addrman = new AddressManager();
			addrman.DebugMode = true;
			var localhost = new NetworkAddress(IPAddress.Parse("127.0.0.1"), 8333);
			addrman.Add(localhost, IPAddress.Loopback);
			Assert.NotNull(addrman.nKey);
			Assert.True(addrman.nKey != new uint256(0));
			Assert.True(addrman.nNew == 1);
			addrman.Good(localhost);
			Assert.True(addrman.nNew == 0);
			Assert.True(addrman.nTried == 1);
			addrman.Attempt(localhost);

			var addr = addrman.Select();
			Assert.False(addr.Ago < TimeSpan.FromSeconds(10.0));

			addrman.Connected(localhost);

			addr = addrman.Select();
			Assert.True(addr.Ago < TimeSpan.FromSeconds(1.0));
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanStressAddrManager()
		{
			Exception exception = null;
			var addrmanager = new AddressManager();
			Random randl = new Random();
			for (int i = 0; i < 30; i++)
			{
				NetworkAddress address = RandomNetworkAddress(randl);
				IPAddress addressSource = RandomAddress(randl);
				address.Ago = TimeSpan.FromMinutes(5.0);
				addrmanager.Add(address, addressSource);
			}

			addrmanager.DebugMode = true;
			var threads =
				Enumerable
				.Range(0, 20)
				.Select(t => new Thread(() =>
				{
					try
					{
						Random rand = new Random(t);
						for (int i = 0; i < 50; i++)
						{
							NetworkAddress address = RandomNetworkAddress(rand);
							IPAddress addressSource = RandomAddress(rand);
							var operation = rand.Next(0, 7);
							switch (operation)
							{
								case 0:
									addrmanager.Attempt(address);
									break;
								case 1:
									addrmanager.Add(address, addressSource);
									break;
								case 2:
									addrmanager.Select();
									break;
								case 3:
									addrmanager.GetAddr();
									break;
								case 4:
									{
										var several = addrmanager.GetAddr();
										addrmanager.Good(several.Length == 0 ? address : several[0]);
									}
									break;

								case 5:
									addrmanager.Connected(address);
									break;
								case 6:
									addrmanager.ToBytes();
									break;
								default:
									throw new NotSupportedException();
							}
						}
					}
					catch (Exception ex)
					{
						exception = ex;
						throw;
					}
				})).ToArray();
			foreach (var t in threads)
				t.Start();
			foreach (var t in threads)
				t.Join();

			Assert.True(addrmanager.nNew != 0);
			Assert.True(addrmanager.nTried != 0);
			Assert.True(addrmanager.GetAddr().Length != 0);
			Assert.Null(exception);
		}

		private IPAddress RandomAddress(Random rand)
		{
			if (rand.Next(0, 100) == 0)
				return IPAddress.Parse("1.2.3.4"); //Simulate collision
			var count = rand.Next(0, 2) % 2 == 0 ? 4 : 16;
			return new IPAddress(RandomUtils.GetBytes(count));
		}

		private NetworkAddress RandomNetworkAddress(Random rand)
		{
			var addr = RandomAddress(rand);
			var p = rand.Next(0, ushort.MaxValue);
			return new NetworkAddress(new IPEndPoint(addr, p));
		}
	}
}
#endif
