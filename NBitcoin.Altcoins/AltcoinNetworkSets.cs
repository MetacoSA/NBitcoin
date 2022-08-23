using System;
using System.ComponentModel.Composition.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace NBitcoin.Altcoins
{
	public class AltNetworkSets
	{
		[ImportMany(typeof(INetworkSet))]
		private IEnumerable<INetworkSet> networkSets;

		public AltNetworkSets()
		{
			this.networkSets = new List<INetworkSet>();
			var aggregate = new AggregateCatalog();
			aggregate.Catalogs.Add(new AssemblyCatalog(typeof(AltNetworkSets).Assembly));
			var container = new CompositionContainer(aggregate);
			container.ComposeParts(this);
		}

		public IEnumerable<INetworkSet> NetworkSets => networkSets;
	}
}
