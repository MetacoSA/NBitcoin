using Xunit;
using NBitcoin;
using NBitcoin.Tests.Helpers;

namespace NBitcoin.Tests
{
	public class ECDSATests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ECDSASerializationTests()
		{
			var rBytes = new byte[] { 255, 243, 22, 18, 30, 119, 145, 145, 181, 246, 229, 69,
			127, 129, 220, 20, 146, 135, 213, 55, 116, 193, 77,
			112, 75, 86, 154, 129, 192, 171, 159, 3};
			var sBytes = new byte[] { 89, 73, 211,
			209, 8, 245, 164, 77, 242, 61, 237, 65, 36, 219, 246,
			197, 201, 239, 46, 70, 143, 17, 255, 34, 215, 16, 183,
			164, 144, 161, 29, 194};
			var r = new BouncyCastle.Math.BigInteger(rBytes);
			var s = new BouncyCastle.Math.BigInteger(sBytes);
			var rBytes2 = Utils.BigIntegerToBytes(r, 32);
			Assert.Equal(rBytes.ToHexString(), rBytes2.ToHexString());
		}
	}
}