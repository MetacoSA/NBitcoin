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
		public static OpenAssetPayload GetColoredPayload(this Transaction tx)
		{
			return OpenAssetPayload.Get(tx);
		}
		public static bool HasWellFormedColoredMarker(this Transaction tx)
		{
			return OpenAssetPayload.HasWellFormedPayload(tx);
		}
	}
}
