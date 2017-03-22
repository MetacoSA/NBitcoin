using System;
using NBitcoin.OpenAsset;
using Xunit;

namespace NBitcoin.Tests
{
	public class AssetMoneyTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyToStringTest()
		{
			AssetId assetId = new AssetId("8f316d9a09");
			AssetMoney assetMoney = new AssetMoney(assetId, 1);

			String actual = assetMoney.ToString();
			Assert.Equal("1-8f316d9a09", actual);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyMultiply()
		{
			AssetId assetId = new AssetId("8f316d9a09");
			AssetMoney assetMoney = new AssetMoney(assetId, 2);

			AssetMoney actual = assetMoney * 2;

			Assert.Equal(4, actual.Quantity);

			actual = 2 * assetMoney;
			Assert.Equal(4, actual.Quantity);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyGreaterThan()
		{
			AssetId assetId = new AssetId("8f316d9a09");
			AssetMoney smallAssetMoney = new AssetMoney(assetId, 2);
			AssetMoney largeAssetMoney = new AssetMoney(assetId, 5);

			Assert.True(largeAssetMoney > smallAssetMoney);
			Assert.False(smallAssetMoney > largeAssetMoney);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyLessThan()
		{
			AssetId assetId = new AssetId("8f316d9a09");
			AssetMoney smallAssetMoney = new AssetMoney(assetId, 2);
			AssetMoney largeAssetMoney = new AssetMoney(assetId, 5);

			Assert.True(smallAssetMoney < largeAssetMoney);
			Assert.False(largeAssetMoney < smallAssetMoney);
		}
	}
}
