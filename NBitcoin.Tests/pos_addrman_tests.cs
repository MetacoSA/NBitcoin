#if !NOSOCKET
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NBitcoin.Protocol;
using Xunit;

namespace NBitcoin.Tests
{
	public class pos_addrman_tests
	{
#if !NOFILEIO
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeDeserializePeerTable()
		{
			AddressManager addrman = new AddressManager();
			addrman.SavePeerFile("strat-CanSerializeDeserializePeerTable.dat", Network.StratisMain);
			AddressManager.LoadPeerFile("strat-CanSerializeDeserializePeerTable.dat", Network.StratisMain);

			addrman = AddressManager.LoadPeerFile(TestDataLocations.DataFolder("peers.dat"), Network.StratisMain);
			addrman.DebugMode = true;
			addrman.Check();
			addrman.SavePeerFile("serializerPeer.dat", Network.StratisMain);

			AddressManager addrman2 = AddressManager.LoadPeerFile("serializerPeer.dat", Network.StratisMain);
			addrman2.DebugMode = true;
			addrman2.Check();
			addrman2.SavePeerFile("serializerPeer2.dat", Network.StratisMain);

			var original = File.ReadAllBytes("serializerPeer2.dat");
			var after = File.ReadAllBytes("serializerPeer.dat");
			Assert.True(original.SequenceEqual(after));
		}
#endif
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUseAddrManager()
		{
			AddressManager addrman = new AddressManager();
			addrman.DebugMode = true;
			var localhost = new NetworkAddress(IPAddress.Parse("127.0.0.1"), 8333);
			addrman.Add(localhost, localhost.Endpoint.Address);
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
			Assert.True(addr.Ago < TimeSpan.FromSeconds(10.0));
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanStressAddrManager()
		{
			Exception exception = null;
			var addrmanager = new AddressManager();
			Random randl = new Random();
			for(int i = 0; i < 30; i++)
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
						for(int i = 0; i < 50; i++)
						{
							NetworkAddress address = RandomNetworkAddress(rand);
							IPAddress addressSource = RandomAddress(rand);
							var operation = rand.Next(0, 7);
							switch(operation)
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
					catch(Exception ex)
					{
						exception = ex;
						throw;
					}
				})).ToArray();
			foreach(var t in threads)
				t.Start();
			foreach(var t in threads)
				t.Join();

			Assert.True(addrmanager.nNew != 0);
			Assert.True(addrmanager.nTried != 0);
			Assert.True(addrmanager.GetAddr().Length != 0);
			Assert.Null(exception);
		}

		private IPAddress RandomAddress(Random rand)
		{
			if(rand.Next(0, 100) == 0)
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