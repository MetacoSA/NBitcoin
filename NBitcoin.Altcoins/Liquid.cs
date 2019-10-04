using System.Collections.Generic;
using NBitcoin.Altcoins.Elements;

namespace NBitcoin.Altcoins
{
	public class Liquid : LiquidAsset<Liquid, Liquid.LiquidRegtest>
	{
		public override string CryptoCode => "LBTC";
		public static Liquid Instance { get; } = new Liquid();
		public class LiquidRegtest { }
		static Liquid()
		{
			ElementsParams<Liquid>.PeggedAssetId = new uint256("6f0279e9ed041c3d710a9f57d0c02928416460c4b722ae3457a11eec381c526d");
			ElementsParams<LiquidRegtest>.PeggedAssetId = new uint256("b2e15d0d7a0c94e4e2ce0fe6e8691b9e451377f6e46e8045a86f7c4b5d4f0f23");
		}

		public override Dictionary<NetworkType, string[]> NetworkNames
		{
			get
			{
				return 	new Dictionary<NetworkType, string[]>()
				{
					{NetworkType.Mainnet, new[]{"liquid", "liquid-mainnet", "liquid-main"}},
					{NetworkType.Regtest, new[]{"liq-reg", "liq-regtest", "liquid-reg", "liquid-regtest"}}
				};
			}
		}
	}
}
