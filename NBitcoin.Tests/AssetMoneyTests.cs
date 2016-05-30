using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class AssetMoneyTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyToStringTest()
		{
			OpenAsset.AssetId assetId = new OpenAsset.AssetId("8f316d9a09");
			OpenAsset.AssetMoney assetMoney = new OpenAsset.AssetMoney(assetId, 1);

			String actual = assetMoney.ToString();
			Assert.Equal("1-8f316d9a09", actual);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyMultiply()
		{
			OpenAsset.AssetId assetId = new OpenAsset.AssetId("8f316d9a09");
			OpenAsset.AssetMoney assetMoney = new OpenAsset.AssetMoney(assetId, 2);

			OpenAsset.AssetMoney actual = assetMoney * 2;

			Assert.Equal(4, actual.Quantity);

			actual = 2 * assetMoney;
			Assert.Equal(4, actual.Quantity);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyGreaterThan()
		{
			OpenAsset.AssetId assetId = new OpenAsset.AssetId("8f316d9a09");
			OpenAsset.AssetMoney smallAssetMoney = new OpenAsset.AssetMoney(assetId, 2);
			OpenAsset.AssetMoney largeAssetMoney = new OpenAsset.AssetMoney(assetId, 5);

			Assert.True(largeAssetMoney > smallAssetMoney);
			Assert.False(smallAssetMoney > largeAssetMoney);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void AssetMoneyLessThan()
		{
			OpenAsset.AssetId assetId = new OpenAsset.AssetId("8f316d9a09");
			OpenAsset.AssetMoney smallAssetMoney = new OpenAsset.AssetMoney(assetId, 2);
			OpenAsset.AssetMoney largeAssetMoney = new OpenAsset.AssetMoney(assetId, 5);

			Assert.True(smallAssetMoney < largeAssetMoney);
			Assert.False(largeAssetMoney < smallAssetMoney);
		}
	}
}
