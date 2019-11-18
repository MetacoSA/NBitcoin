using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Altcoins.Elements
{
	public class ElementsStringParser : NetworkStringParser
	{
		public override bool TryParse(string str, Network network, Type targetType, out IBitcoinString result)
		{
			if (typeof(BitcoinAddress).GetTypeInfo().IsAssignableFrom(targetType.GetTypeInfo()))
			{
				try
				{
					result = new BitcoinBlindedAddress(str, network);
					return true;
				}
				catch
				{
				}
			}
			return base.TryParse(str, network, targetType, out result);
		}
	}
}
