using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Altcoins.Elements
{
	public class ElementsParams<T>
	{
		public static uint256 PeggedAssetId;
		public static bool BlockHeightInHeader { get; set; }
		public static bool SignedBlocks { get; set; }
	}
}
