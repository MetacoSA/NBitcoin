using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Altcoins.Elements
{
	public class ElementsStringParser : NetworkStringParser
	{
		public override bool TryParse<T>(string str, Network network, out T result)
		{
			if (typeof(BitcoinAddress).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
			{
				try
				{
					result = (T)(object)new BitcoinBlindedAddress(str, network);
					return true;
				}
				catch
				{
				}
			}
			return base.TryParse(str, network, out result);
		}
	}
}
