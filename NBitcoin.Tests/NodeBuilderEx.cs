using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	public class NodeBuilderEx
	{
		public static NodeBuilder Create([CallerMemberName] string caller = null)
		{
			//Altcoins.Litecoin.EnsureRegistered();
			//return NodeBuilder.Create(NodeDownloadData.Litecoin.v0_15_1, Altcoins.Litecoin.Regtest, caller);

			//Altcoins.BCash.EnsureRegistered();
			//return NodeBuilder.Create(NodeDownloadData.BCash.v0_16_2, Altcoins.BCash.Regtest, caller);

			return NodeBuilder.Create(NodeDownloadData.Bitcoin.v0_16_0, Network.RegTest, caller);
		}
	}
}
