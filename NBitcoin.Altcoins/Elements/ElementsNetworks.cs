using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Altcoins.Elements
{
	public class ElementsNetworks
	{
		public static Network Regtest
		{
			get
			{
				return ElementsRegtest.Instance.Regtest;
			}
		}
	}
}
