using System.IO;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	public class checkblock_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateMerkleRoot()
		{
			Block block = new Block();
			block.ReadWrite(Encoders.Hex.DecodeData(File.ReadAllText(TestDataLocations.DataFolder("block1125.txt"))));
			Assert.Equal(block.Header.HashMerkleRoot, block.GetMerkleRoot().Hash);
		}
	}
}
