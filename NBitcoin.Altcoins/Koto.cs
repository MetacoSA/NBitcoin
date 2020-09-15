using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/KotoDevelopers/koto/blob/master/src/chainparams.cpp
	public class Koto : NetworkSetBase
	{
		public static Koto Instance { get; } = new Koto();
		
		public override string CryptoCode => "KOTO";
		
		private Koto()
		{
		
		}
		public class KotoConsensusFactory : ConsensusFactory
		{
			private KotoConsensusFactory()
			{
			}
			public static KotoConsensusFactory Instance { get; } = new KotoConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new KotoBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new KotoBlock(new KotoBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete

		public class KotoBlock : Block
		{
			public KotoBlock(KotoBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
			return KotoConsensusFactory.Instance;
			}
        }

		public class KotoBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				throw new NotImplementedException();
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
			}
		}

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Koto");
			// Alternatively,
			/*
			RegisterDefaultCookiePath(Mainnet, ".cookie");
			RegisterDefaultCookiePath(Testnet, "testnet3", ".cookie");
			RegisterDefaultCookiePath(Regtest, "regtest", ".cookie");
			*/
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 1051200,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 4000,
				BIP34Hash = new uint256("6d424c350729ae633275d51dc3496e16cd1b1d195c164da00f39c499a2e9959e"), // (Genesis)

				PowLimit = new Target(new uint256("0007ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(1 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000923084b2fcff"),
				ConsensusFactory = KotoConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x18, 0x36 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x18, 0x3B })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0x80 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetMagic(0x4b6f746f)
			.SetPort(8433)
			.SetRPCPort(8432)
			.SetMaxP2PVersion(170007)
			.SetName("koto-main")
			.AddAlias("koto-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("ko-to.org", "dnsseed.ko-to.org")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("04000000000000000000000000000000000000000000000000000000000000000000000072bb817c4c07ab244baca568f5f465db3a87520fa165e9a9e68adaa820eb8de1ceb32c5affff071fcc0a00000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2504ffff071f01041d4b6f746f3a4a6170616e6573652063727970746f2d63757272656e6379ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		//For TestNet v3

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 1051200,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 400,
				BIP34Hash = new uint256("bf84afbde20c2d213b68b231ddb585ab616ef7567226820f00d9b397d774d2f0"),
				PowLimit = new Target(new uint256("07ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(1 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000072c3a6f1"),
				ConsensusFactory = KotoConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x18, 0xA4 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x18, 0x39 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0xEF })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetMagic(0x546f6b6f)
			.SetPort(18433)
			.SetRPCPort(18432)
			.SetMaxP2PVersion(170007)
			.SetName("koto-test")
			.AddAlias("koto-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("ko-to.org", "testnet.ko-to.org")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("04000000000000000000000000000000000000000000000000000000000000000000000072bb817c4c07ab244baca568f5f465db3a87520fa165e9a9e68adaa820eb8de1cfb32c5affff07201c0000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2504ffff071f01041d4b6f746f3a4a6170616e6573652063727970746f2d63757272656e6379ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 200,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256(),
				PowLimit = new Target(new uint256("0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f")),
				PowTargetTimespan = TimeSpan.FromSeconds(1 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 100,
				ConsensusFactory = KotoConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x18, 0xA4 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x18, 0x39 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0xEF })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetMagic(0x52656b6f)
			.SetPort(18433)
			.SetRPCPort(18432)
			.SetMaxP2PVersion(170006)
			.SetName("koto-reg")
			.AddAlias("koto-regtest")
			.SetGenesis("04000000000000000000000000000000000000000000000000000000000000000000000072bb817c4c07ab244baca568f5f465db3a87520fa165e9a9e68adaa820eb8de1cfb32c5affff07201c0000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2504ffff071f01041d4b6f746f3a4a6170616e6573652063727970746f2d63757272656e6379ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}
	}
}
