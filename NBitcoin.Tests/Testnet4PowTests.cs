using Xunit;

namespace NBitcoin.Tests
{
	/// <summary>
	/// Tests for BIP 94 (Testnet4) difficulty adjustment rules.
	/// BIP 94 uses the FIRST block of the previous period as the difficulty anchor, not the LAST.
	/// </summary>
	public class Testnet4PowTests
	{
		// Real testnet4 headers at difficulty boundary where BIP 94 matters:
		// Block 28224 (first of period): nBits=0x1a0082a5, Block 30239 (last): nBits=0x1d00ffff (min diff!)
		// Block 30240's difficulty 0x1973b070 was calculated from 28224, NOT from 30239's min-difficulty.
		const string Header28224 = "0060992d545f75503d65d04310b1904921b09b6f98c0674c20a84f2d6e00000000000000fe7f1811f7694b3cff4ed7a8ef0447fb59139d763e1a58c6ffbd6198eae38e83c1cf5c66a582001a2c76f01d";
		const string Header30239 = "0000002092e737868048420bb08385b9937cec6b2f77784509a0995507000000000000002e4de945b5edd021ad3420e46a7ebe5dbd5d9bc5d15b319dec8f9afe3fde9334de276d66ffff001db5b35b90";
		const string Header30240 = "00805222bbde84820b42d69d70d6b4c45d5ec570ed2a891436833d25eae0f8ea0000000026a7ae7a6e174ca8bb25e9d3d3063b2c54dba4fb16012108d7c5ffff27957e43752a6d6670b073194c48554f";

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCalculateTestnet4DifficultyWithBIP94()
		{
			var network = Network.TestNet4;
			Assert.True(network.Consensus.EnforceBIP94);

			var header28224 = BlockHeader.Parse(Header28224, network);
			var header30239 = BlockHeader.Parse(Header30239, network);
			var header30240 = BlockHeader.Parse(Header30240, network);

			Assert.Equal(0x1a0082a5u, header28224.Bits.ToCompact());
			Assert.Equal(0x1d00ffffu, header30239.Bits.ToCompact());
			Assert.Equal(0x1973b070u, header30240.Bits.ToCompact());

			var block30240 = BuildTestChain(header28224, header30239, header30240, network.Consensus);
			Assert.Equal(header30240.Bits, block30240.GetWorkRequired(network.Consensus));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BIP94PreventsMinDifficultyFromDistortingRetarget()
		{
			var network = Network.TestNet4;
			var header28224 = BlockHeader.Parse(Header28224, network);
			var header30239 = BlockHeader.Parse(Header30239, network);
			var header30240 = BlockHeader.Parse(Header30240, network);

			var block30240 = BuildTestChain(header28224, header30239, header30240, network.Consensus);

			var targetWithBIP94 = block30240.GetWorkRequired(network.Consensus);

			var consensusWithoutBIP94 = network.Consensus.Clone();
			consensusWithoutBIP94.EnforceBIP94 = false;
			var targetWithoutBIP94 = block30240.GetWorkRequired(consensusWithoutBIP94);

			Assert.Equal(new Target(0x1973b070), targetWithBIP94);
			Assert.True(targetWithoutBIP94 > targetWithBIP94);
		}

		private static ChainedBlock BuildTestChain(BlockHeader first, BlockHeader last, BlockHeader next, Consensus consensus)
		{
			const int firstHeight = 28224;
			const int lastHeight = 30239;

			var current = new ChainedBlock(first, firstHeight);
			for (int height = firstHeight + 1; height <= lastHeight; height++)
			{
				var header = consensus.ConsensusFactory.CreateBlockHeader();
				header.HashPrevBlock = current.HashBlock;
				header.Bits = (height == lastHeight) ? last.Bits : first.Bits;
				header.BlockTime = (height == lastHeight) ? last.BlockTime : first.BlockTime;
				current = new ChainedBlock(header, header.GetHash(), current);
			}

			var nextHeader = consensus.ConsensusFactory.CreateBlockHeader();
			nextHeader.HashPrevBlock = current.HashBlock;
			nextHeader.BlockTime = next.BlockTime;
			nextHeader.Bits = next.Bits;
			return new ChainedBlock(nextHeader, nextHeader.GetHash(), current);
		}
	}
}