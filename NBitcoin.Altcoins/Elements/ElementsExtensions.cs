using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Altcoins.Elements
{
	public static class ElementsExtensions
	{
		public static BitcoinBlindedAddress AddBlindingKey(this BitcoinAddress address, PubKey blindingKey)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));
			if (blindingKey == null)
				throw new ArgumentNullException(nameof(blindingKey));
			if (address is BitcoinBlindedAddress ba)
				address = ba.UnblindedAddress;
			return new BitcoinBlindedAddress(blindingKey, address);
		}
	}
}
