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

			//var builder = NodeBuilder.Create(NodeDownloadData.Viacoin.v0_15_1, Altcoins.Viacoin.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.BCash.v0_16_2, Altcoins.BCash.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Dogecoin.v1_10_0, Altcoins.Dogecoin.Instance.Regtest, caller);
			//builder.SupportCookieFile = false;

			//var builder = NodeBuilder.Create(NodeDownloadData.Dash.v0_12_2, Altcoins.Dash.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.BGold.v0_15_0, Altcoins.BGold.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Polis.v1_3_0, Altcoins.Polis.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Monacoin.v0_15_1, Altcoins.Monacoin.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Feathercoin.v0_16_0, Altcoins.AltNetworkSets.Feathercoin.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Ufo.v0_16_0, Altcoins.AltNetworkSets.Ufo.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Groestlcoin.v2_16_0, Altcoins.AltNetworkSets.Groestlcoin.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Mogwai.v0_12_2, Altcoins.AltNetworkSets.Mogwai.Regtest, caller);
			var builder = NodeBuilder.Create(NodeDownloadData.Bitcoin.v0_16_2, Altcoins.AltNetworkSets.Bitcoin.Regtest, caller);
			return builder;
		}
	}
}
