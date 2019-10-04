using System.Collections.Generic;
using NBitcoin.Altcoins.Elements;

namespace NBitcoin.Altcoins
{
	public class LiquidTether : LiquidAsset<LiquidTether, LiquidTether.LiquidTetherRegtest>
	{
		public override string CryptoCode => "USDT";
		public static LiquidTether Instance { get; } = new LiquidTether();
		public class LiquidTetherRegtest { }
		static LiquidTether()
		{
			ElementsParams<Liquid>.PeggedAssetId = new uint256("ce091c998b83c78bb71a632313ba3760f1763d9cfcffae02258ffa9865a37bd2");
			ElementsParams<LiquidTetherRegtest>.PeggedAssetId = null;
		}

		public override Dictionary<NetworkType, string[]> NetworkNames
		{
			get
			{
				return 	new Dictionary<NetworkType, string[]>()
				{
					{NetworkType.Mainnet, new[]{"liquid-tether", "liquid-tether-mainnet", "liquid-tetehr-main"}},
					{NetworkType.Regtest, new[]{"liq-tether-reg", "liq-tether-regtest", "liquid-tether-reg", "liquid-tether-regtest"}}
				};
			}
		}
	}
}
