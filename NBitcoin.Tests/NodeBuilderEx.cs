using System.Net;
using System.Runtime.CompilerServices;

namespace NBitcoin.Tests
{
	public class NodeBuilderEx
	{
		public static NodeDownloadData GetNodeDownloadData()
		{
			//return NodeDownloadData.Litecoin.v0_17_1;
			//return NodeDownloadData.Viacoin.v0_15_1;
			//return NodeDownloadData.BCash.v0_16_2;
			//return NodeDownloadData.Dogecoin.v1_10_0;
			//return NodeDownloadData.Dash.v0_13_0;
			//return NodeDownloadData.BGold.v0_15_0;
			//return NodeDownloadData.Polis.v1_4_3;
			//return NodeDownloadData.Monoeci.v0_12_2_3;
			//return NodeDownloadData.Colossus.v1_1_1;
			//return NodeDownloadData.GoByte.v0_12_2_4;
			//return NodeDownloadData.Monacoin.v0_15_1;
			//return NodeDownloadData.Feathercoin.v0_16_0;
			//return NodeDownloadData.Ufo.v0_16_0;
			//return NodeDownloadData.Groestlcoin.v2_16_3;
			//return NodeDownloadData.Mogwai.v0_12_2;
			//return NodeDownloadData.Dystem.v1_0_9_9;
			//return NodeDownloadData.Bitcoinplus.v2_7_0;
			//return NodeDownloadData.Liquid.v3_14_1_21;
			//return NodeDownloadData.Bitcore.v0_15_2;
			//return NodeDownloadData.Gincoin.v1_1_0_0;
			//return NodeDownloadData.Koto.v2_0_0;
			//return NodeDownloadData.Chaincoin.v0_16_4;
			//return NodeDownloadData.Stratis.v3_0_0;
			//return NodeDownloadData.ZCoin.v0_13_8_3;
			//return NodeDownloadData.DogeCash.v5_1_1;

			//return NodeDownloadData.Elements.v0_18_1_1;
			return NodeDownloadData.Bitcoin.v0_19_0_1;
		}

		public static Network GetNetwork()
		{
			//return Altcoins.Litecoin.Instance.Regtest;
			//return Altcoins.Viacoin.Instance.Regtest;
			//return Altcoins.BCash.Instance.Regtest;
			//return Altcoins.Dogecoin.Instance.Regtest;
			//return Altcoins.Dash.Instance.Regtest;
			//return Altcoins.BGold.Instance.Regtest;
			//return Altcoins.Polis.Instance.Regtest;
			//return Altcoins.Monoeci.Instance.Regtest;
			//return Altcoins.Colossus.Instance.Regtest;
			//return Altcoins.GoByte.Instance.Regtest;
			//return Altcoins.Monacoin.Regtest;
			//return Altcoins.AltNetworkSets.Feathercoin.Regtest;
			//return Altcoins.AltNetworkSets.Ufo.Regtest;
			//return Altcoins.AltNetworkSets.Groestlcoin.Regtest;
			//return Altcoins.AltNetworkSets.Mogwai.Regtest;
			//return Altcoins.Dystem.Instance.Regtest;
			//return Altcoins.AltNetworkSets.Bitcoinplus.Regtest;
			//return Altcoins.AltNetworkSets.Liquid.Regtest;
			//return Altcoins.Bitcore.Instance.Regtest;
			//return Altcoins.Gincoin.Instance.Regtest;
			//return Altcoins.Koto.Regtest;
			//return Altcoins.AltNetworkSets.Chaincoin.Regtest;
			//return Altcoins.AltNetworkSets.Stratis.Regtest;
			//return Altcoins.ZCoin.Instance.Regtest;
			//return Altcoins.DogeCash.Instance.Regtest;

			//return Altcoins.AltNetworkSets.Liquid.Regtest;
			return Altcoins.AltNetworkSets.Bitcoin.Regtest;
		}

		public static NodeBuilder Create([CallerMemberName] string caller = null)
		{
			var builder = NodeBuilder.Create(GetNodeDownloadData(), GetNetwork(), caller);
			return builder;
		}

		public static NodeBuilder Create(NodeDownloadData nodeDownloadData, [CallerMemberName] string caller = null)
		{
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			var builder = NodeBuilder.Create(nodeDownloadData, Altcoins.AltNetworkSets.Bitcoin.Regtest, caller);
			return builder;
		}
	}
}
