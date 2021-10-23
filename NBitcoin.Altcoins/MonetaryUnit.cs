using System;
using System.Linq;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/muecoin/MUE/blob/master/src/chainparams.cpp
	public class MonetaryUnit : NetworkSetBase
	{
		public static MonetaryUnit Instance { get; } = new MonetaryUnit();

		public override string CryptoCode => "MUE";

		private MonetaryUnit()
		{
		}

		public class MonetaryUnitConsensusFactory : ConsensusFactory
		{
			private MonetaryUnitConsensusFactory()
			{
			}

			public static MonetaryUnitConsensusFactory Instance { get; } = new MonetaryUnitConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new MonetaryUnitBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new MonetaryUnitBlock(new MonetaryUnitBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class MonetaryUnitBlockHeader : BlockHeader
		{

			public override uint256 GetPoWHash()
			{
				throw new NotSupportedException("PoW for MUE is not supported");
			}
		}

		public class MonetaryUnitBlock : Block
		{
			public MonetaryUnitBlock(MonetaryUnitBlockHeader h) : base(h)
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
			RegisterDefaultCookiePath("MUE");
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
				MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(10 * 40),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 40),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 50,
				PowNoRetargeting = false,
				ConsensusFactory = MonetaryUnitConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 31
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 16 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 76 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 126 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x02, 0x2D, 0x25, 0x33 })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x02, 0x21, 0x31, 0x2B })
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("mue"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("mue"))
				.SetMagic(0xeafdc491)
				.SetPort(19687)
				.SetRPCPort(19688)
				.SetMaxP2PVersion(70800)
				.SetName("monetaryunit-main")
				.AddAlias("monetaryunit-mainnet")
				.SetUriScheme("monetaryunit")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("dns1.monetaryunit.org", "dns1.monetaryunit.org"),
					new DNSSeedData("dns2.monetaryunit.org", "dns2.monetaryunit.org"),
					new DNSSeedData("dns3.monetaryunit.org", "dns3.monetaryunit.org"),
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000004e9144c4f6527245c4bdd6ee4e832e45d4ce035d5da260e261194a48f2adae72958f915bffff7f20010000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6004ffff001d01044c57426974636f696e20426c6f636b20233534343338313a2030303030303030303030303030303030303030643564313634376364353137343032306430333439373761376131333362316165643438646636363731383138ffffffff0100000000000000000000000000");

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
				MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(1 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 10),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 15,
				PowNoRetargeting = false,
				ConsensusFactory = MonetaryUnitConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 1
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 139 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x3A, 0x80, 0x61, 0xA0 })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x3A, 0x80, 0x58, 0x37 })
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tmue"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tmue"))
				.SetMagic(0xbd657647)
				.SetPort(19685)
				.SetRPCPort(19686)
				.SetMaxP2PVersion(70800)
				.SetName("monetaryunit-test")
				.AddAlias("monetaryunit-testnet")
				.SetUriScheme("monetaryunit")
				.AddSeeds(new NetworkAddress[0])
				//testnet down for now
				.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000004e9144c4f6527245c4bdd6ee4e832e45d4ce035d5da260e261194a48f2adae7260e8b759ffff7f20010000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6004ffff001d01044c57426974636f696e20426c6f636b20233534343338313a2030303030303030303030303030303030303030643564313634376364353137343032306430333439373761376131333362316165643438646636363731383138ffffffff0100000000000000000000000000");

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
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 0,
				PowNoRetargeting = true,
				ConsensusFactory = MonetaryUnitConsensusFactory.Instance,
				SupportSegwit = false
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 139 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x3A, 0x80, 0x61, 0xA0 })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x3A, 0x80, 0x58, 0x37 })
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("rtmue"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("rtmue"))
				.SetMagic(0xad7ecfa2)
				.SetPort(19685)
				.SetRPCPort(19686)
				.SetMaxP2PVersion(70800)
				.SetName("monetaryunit-reg")
				.AddAlias("monetaryunit-regtest")
				.SetUriScheme("monetaryunit")
				.AddDNSSeeds(new DNSSeedData[0])
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000004e9144c4f6527245c4bdd6ee4e832e45d4ce035d5da260e261194a48f2adae7260e8b759ffff7f203b3000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6004ffff001d01044c57426974636f696e20426c6f636b20233534343338313a2030303030303030303030303030303030303030643564313634376364353137343032306430333439373761376131333362316165643438646636363731383138ffffffff0100000000000000000000000000");

			return builder;
		}
	}
}
