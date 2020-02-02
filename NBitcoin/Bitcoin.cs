using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Bitcoin : INetworkSet
	{
		private Bitcoin()
		{

		}
		public static Bitcoin Instance { get; } = new Bitcoin();

		public Network Mainnet => Network.Main;

		public Network Testnet => Network.TestNet;

		public Network Regtest => Network.RegTest;

		public string CryptoCode => "BTC";

		public Network GetNetwork(NetworkType networkType)
		{
			switch (networkType)
			{
				case NetworkType.Mainnet:
					return Mainnet;
				case NetworkType.Testnet:
					return Testnet;
				case NetworkType.Regtest:
					return Regtest;
			}
			throw new NotSupportedException(networkType.ToString());
		}
	}
}
