using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	public class NodeBuilderEx
	{
		public static NodeBuilder Create([CallerMemberName] string caller = null)
		{
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


			//var builder = NodeBuilder.Create(NodeDownloadData.Litecoin.v0_15_1, Altcoins.Litecoin.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.BCash.v0_16_2, Altcoins.BCash.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Dogecoin.v1_10_0, Altcoins.Dogecoin.Instance.Regtest, caller);
			//builder.SupportCookieFile = false;

			//var builder = NodeBuilder.Create(NodeDownloadData.Dash.v0_12_2, Altcoins.Dash.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.BitcoinGold.v0_15_0, Altcoins.BitcoinGold.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Polis.v1_3_0, Altcoins.Polis.Instance.Regtest, caller);

			var builder = NodeBuilder.Create(NodeDownloadData.Bitcoin.v0_16_0, Altcoins.AltNetworkSets.Bitcoin.Regtest, caller);
			return builder;
		}
	}
}
