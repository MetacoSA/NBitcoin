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
    }
}
