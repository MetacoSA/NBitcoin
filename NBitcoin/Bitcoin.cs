using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using NBitcoin.Protocol;

namespace NBitcoin
{
	public partial class Bitcoin : INetworkSet
	{
		static Bitcoin()
		{
 			Instance.Init();
		}

		internal void Init()
		{
			CreateMainnet();
			CreateTestnet();
			CreateRegtest();
			CreateSignet();
			CreateMutinyNet();

		}

		public static Bitcoin Instance { get; } = new();

		public string CryptoCode => "BTC";

		private ConcurrentDictionary<ChainName, Network> _Networks = new();

		public Network GetNetwork(ChainName chainName)
		{
			if (chainName == null)
				throw new ArgumentNullException(nameof(chainName));
			return _Networks.TryGetValue(chainName, out var network) ? network : null;
		}


#if !NOSOCKET
		private static IEnumerable<NetworkAddress> LoadNetworkAddresses(byte[] payload, NetworkBuilder builder)
		{
			// Convert the pnSeeds array into usable address objects.
			Random rand = new Random();
			TimeSpan nOneWeek = TimeSpan.FromDays(7);


			var stream = new BitcoinStream(payload, consensusFactory: builder._Consensus.ConsensusFactory);
			stream.Type = SerializationType.Network;

			using (stream.ProtocolVersionScope(NetworkAddress.AddrV2Format))
			{
				stream.ProtocolCapabilities.SupportTimeAddress = false;
				while (true)
				{
					// It'll only connect to one or two seed nodes because once it connects,
					// it'll get a pile of addresses with newer timestamps.
					var addr = new NetworkAddress();
					try
					{
						addr.ReadWrite(stream);
						// weeks ago.
						addr.Time = DateTime.UtcNow -
						            (TimeSpan.FromSeconds(rand.NextDouble() * nOneWeek.TotalSeconds)) - nOneWeek;
					}
					catch (EndOfStreamException)
					{
						break;
					}

					yield return addr;
				}
			}
		}
#endif
	}
}
