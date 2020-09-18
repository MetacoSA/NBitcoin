using System;
using System.Linq;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/lbryio/lbrycrd/blob/master/src/chainparams.cpp
	public class LBRYCredits : NetworkSetBase
	{
		public static LBRYCredits Instance { get; } = new LBRYCredits();

		public override string CryptoCode => "LBC";

		private LBRYCredits()
		{
		}

		public class LBRYCreditsConsensusFactory : ConsensusFactory
		{
			private LBRYCreditsConsensusFactory()
			{
			}

			public static LBRYCreditsConsensusFactory Instance { get; } = new LBRYCreditsConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new LBRYCreditsBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new LBRYCreditsBlock(new LBRYCreditsBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class LBRYCreditsBlockHeader : BlockHeader
		{

			public override uint256 GetPoWHash()
			{
				throw new NotSupportedException("PoW for LBC is not supported");
			}
		}

		public class LBRYCreditsBlock : Block
		{
			public LBRYCreditsBlock(LBRYCreditsBlockHeader h) : base(h)
			{
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return Instance.Mainnet.Consensus.ConsensusFactory;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("LBC");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
			{
				SubsidyHalvingInterval = unchecked((int)4000000000),
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000a0c3931735170"),
				PowTargetTimespan = TimeSpan.FromSeconds(3 * 50),
				PowTargetSpacing = TimeSpan.FromSeconds(3 * 50),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 50,
				PowNoRetargeting = false,
				ConsensusFactory = LBRYCreditsConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 31
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 85 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 122 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 28 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("LBC"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("LBC"))
				.SetMagic(0xfae4aaf1)
				.SetPort(9246)
				.SetRPCPort(9245)
				.SetMaxP2PVersion(70800)
				.SetName("LBRYCredits-main")
				.AddAlias("LBRYCredits-mainnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("dnsseed1.lbry.io", "dnsseed1.lbry.io"),
					new DNSSeedData("dnsseed2.lbry.io", "dnsseed2.lbry.io"),
					new DNSSeedData("dnsseed3.lbry.io", "dnsseed3.lbry.io"),
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000cc59e59ff97ac092b55e423aa5495151ed6fb80570a5bb78cd5bd1c3821c21b8010000000000000000000000000000000000000000000000000000000000000033193156ffff001f070500000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1f04ffff001d010417696e736572742074696d657374616d7020737472696e67ffffffff01000004bfc91b8e001976a914345991dbf57bfb014b87006acdfafbfc5fe8292f88ac00000000fae4aaf1d30000000000002063f4346a4db34fdfce29a70f5e8d11f065f6b91602b7036c7f22f3a03b28899cba888e2f9c037f831046f8ad");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
			{
				SubsidyHalvingInterval = unchecked(1000000000),
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000a0c3931735170"),
				PowTargetTimespan = TimeSpan.FromSeconds(3 * 50),
				PowTargetSpacing = TimeSpan.FromSeconds(3 * 50),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 15,
				PowNoRetargeting = false,
				ConsensusFactory = LBRYCreditsConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 1
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tLBC"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tLBC"))
				.SetMagic(0xfae4aae1)
				.SetPort(29246)
				.SetRPCPort(19245)
				.SetMaxP2PVersion(70800)
				.SetName("LBRYCredits-test")
				.AddAlias("LBRYCredits-testnet")
				.AddSeeds(new NetworkAddress[0])
				//testnet down for now
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000cc59e59ff97ac092b55e423aa5495151ed6fb80570a5bb78cd5bd1c3821c21b8010000000000000000000000000000000000000000000000000000000000000033193156ffff001f070500000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1f04ffff001d010417696e736572742074696d657374616d7020737472696e67ffffffff01000004bfc91b8e001976a914345991dbf57bfb014b87006acdfafbfc5fe8292f88ac0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			var res = builder.SetConsensus(new Consensus
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(1),
				PowTargetSpacing = TimeSpan.FromSeconds(1),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 0,
				PowNoRetargeting = true,
				ConsensusFactory = LBRYCreditsConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("rtLBC"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("rtLBC"))
			.SetMagic(0xfae4aad1)
			.SetPort(29246)
			.SetRPCPort(29245)
			.SetMaxP2PVersion(70800)
			.SetName("LBRYCredits-reg")
			.AddAlias("LBRYCredits-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000cc59e59ff97ac092b55e423aa5495151ed6fb80570a5bb78cd5bd1c3821c21b8010000000000000000000000000000000000000000000000000000000000000033193156ffff7f20010000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1f04ffff001d010417696e736572742074696d657374616d7020737472696e67ffffffff01000004bfc91b8e001976a914345991dbf57bfb014b87006acdfafbfc5fe8292f88ac0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

			return builder;
		}
	}
}
