using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface INetworkSet
	{
		Network GetNetwork(ChainName chainName);
		Network Mainnet
		{
			get;
		}
		Network Testnet
		{
			get;
		}
		Network Regtest
		{
			get;
		}
		string CryptoCode
		{
			get;
		}
	}
}
