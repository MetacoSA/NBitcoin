using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.OpenAsset
{
	public static class Extensions
	{
		public static ColoredTransaction GetColoredTransaction(this Transaction tx, IColoredTransactionRepository repo)
		{
			return ColoredTransaction.FetchColors(tx, repo);
		}
		public static ColorMarker GetColoredMarker(this Transaction tx)
		{
			return ColorMarker.Get(tx);
		}
		public static bool HasValidColoredMarker(this Transaction tx)
		{
			return ColorMarker.HasValidColorMarker(tx);
		}
		public static AssetId ToAssetId(this ScriptId id)
		{
			return new AssetId(id);
		}
	}
}
