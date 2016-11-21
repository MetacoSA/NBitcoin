using System;
using Xunit;

namespace NStratis.Tests
{
	public class AssetMoneyTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyToStringTest()
		{
			NBitcoin.OpenAsset.AssetId assetId = new NBitcoin.OpenAsset.AssetId("8f316d9a09");
			NBitcoin.OpenAsset.AssetMoney assetMoney = new NBitcoin.OpenAsset.AssetMoney(assetId, 1);

			String actual = assetMoney.ToString();
			Assert.Equal("1-8f316d9a09", actual);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyMultiply()
		{
			NBitcoin.OpenAsset.AssetId assetId = new NBitcoin.OpenAsset.AssetId("8f316d9a09");
			NBitcoin.OpenAsset.AssetMoney assetMoney = new NBitcoin.OpenAsset.AssetMoney(assetId, 2);

			NBitcoin.OpenAsset.AssetMoney actual = assetMoney * 2;

			Assert.Equal(4, actual.Quantity);

			actual = 2 * assetMoney;
			Assert.Equal(4, actual.Quantity);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyGreaterThan()
		{
			NBitcoin.OpenAsset.AssetId assetId = new NBitcoin.OpenAsset.AssetId("8f316d9a09");
			NBitcoin.OpenAsset.AssetMoney smallAssetMoney = new NBitcoin.OpenAsset.AssetMoney(assetId, 2);
			NBitcoin.OpenAsset.AssetMoney largeAssetMoney = new NBitcoin.OpenAsset.AssetMoney(assetId, 5);

			Assert.True(largeAssetMoney > smallAssetMoney);
			Assert.False(smallAssetMoney > largeAssetMoney);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyLessThan()
		{
			NBitcoin.OpenAsset.AssetId assetId = new NBitcoin.OpenAsset.AssetId("8f316d9a09");
			NBitcoin.OpenAsset.AssetMoney smallAssetMoney = new NBitcoin.OpenAsset.AssetMoney(assetId, 2);
			NBitcoin.OpenAsset.AssetMoney largeAssetMoney = new NBitcoin.OpenAsset.AssetMoney(assetId, 5);

			Assert.True(smallAssetMoney < largeAssetMoney);
			Assert.False(largeAssetMoney < smallAssetMoney);
		}
	}
}
