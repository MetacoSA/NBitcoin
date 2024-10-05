#nullable enable
using System;

namespace NBitcoin.Payment
{
	[Obsolete("Use BitcoinUriBuilder instead")]
	public class BitcoinUrlBuilder : BitcoinUriBuilder
	{
		[Obsolete("Use BitcoinUriBuilder(Network) instead")]
		public BitcoinUrlBuilder() :
			base(Bitcoin.Instance.Mainnet)
		{
		}

		[Obsolete("Use BitcoinUriBuilder(network) instead")]
		public BitcoinUrlBuilder(Network network) :
			base(network)
		{
		}

		[Obsolete("Use BitcoinUriBuilder(uri, network) instead")]
		public BitcoinUrlBuilder(Uri uri, Network network) :
			base(uri, network)
		{
		}

		[Obsolete("Use BitcoinUriBuilder(uri, network) instead")]
		public BitcoinUrlBuilder(string uri, Network network) :
			base(uri, network)
		{
		}
	}
}
