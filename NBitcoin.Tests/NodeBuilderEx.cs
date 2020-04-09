﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace NBitcoin.Tests
{
	public class NodeBuilderEx
	{
		public static NodeBuilder Create([CallerMemberName] string caller = null)
		{
			//var builder = NodeBuilder.Create(NodeDownloadData.Litecoin.v0_17_1, Altcoins.Litecoin.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Viacoin.v0_15_1, Altcoins.Viacoin.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.BCash.v0_16_2, Altcoins.BCash.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Dogecoin.v1_10_0, Altcoins.Dogecoin.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Verge.v6_0_2, Altcoins.Verge.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Dash.v0_13_0, Altcoins.Dash.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Terracoin.v0_12_2, Altcoins.Terracoin.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.BGold.v0_15_0, Altcoins.BGold.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Polis.v1_4_3, Altcoins.Polis.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Monoeci.v0_12_2_3, Altcoins.Monoeci.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Colossus.v1_1_1, Altcoins.Colossus.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.GoByte.v0_12_2_4, Altcoins.GoByte.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Monacoin.v0_15_1, Altcoins.Monacoin.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Feathercoin.v0_16_0, Altcoins.AltNetworkSets.Feathercoin.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Ufo.v0_16_0, Altcoins.AltNetworkSets.Ufo.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Groestlcoin.v2_18_2, Altcoins.AltNetworkSets.Groestlcoin.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Mogwai.v0_12_2, Altcoins.AltNetworkSets.Mogwai.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Dystem.v1_0_9_9, Altcoins.Dystem.Instance.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.Bitcoinplus.v2_7_0, Altcoins.AltNetworkSets.Bitcoinplus.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Liquid.v3_14_1_21, Altcoins.AltNetworkSets.Liquid.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.Bitcore.v0_15_2, Altcoins.Bitcore.Instance.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.Gincoin.v1_1_0_0, Altcoins.Gincoin.Instance.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.Koto.v2_0_0, Altcoins.Koto.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.Chaincoin.v0_16_4 , Altcoins.AltNetworkSets.Chaincoin.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.Stratis.v3_0_0, Altcoins.AltNetworkSets.Stratis.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.ZCoin.v0_13_8_3, Altcoins.ZCoin.Instance.Regtest, caller);
			//var builder = NodeBuilder.Create(NodeDownloadData.DogeCash.v5_1_1, Altcoins.DogeCash.Instance.Regtest, caller);

			//var builder = NodeBuilder.Create(NodeDownloadData.Elements.v0_18_1_1, Altcoins.AltNetworkSets.Liquid.Regtest, caller);
			var builder = Create(NodeDownloadData.Bitcoin.v0_19_0_1, caller);
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
