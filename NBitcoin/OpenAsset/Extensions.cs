using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public static class Extensions
	{
		public static ColoredTransaction FetchColors(this Transaction tx, IColoredTransactionRepository repo)
		{
			return ColoredTransaction.FetchColors(tx, repo);
		}
	}
}
