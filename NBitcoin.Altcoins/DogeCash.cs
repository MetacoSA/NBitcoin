using System;
using System.Linq;
using NBitcoin.Altcoins.HashX11;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/dogecash/dogecash/blob/master/src/chainparams.cpp
	public class DogeCash : NetworkSetBase
	{
		public static DogeCash Instance { get; } = new DogeCash();

		public override string CryptoCode => "DOGEC";

		private DogeCash()
		{
		}

		public class DogeCashConsensusFactory : ConsensusFactory
		{
			private DogeCashConsensusFactory()
			{
			}

			public static DogeCashConsensusFactory Instance { get; } = new DogeCashConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new DogeCashBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new DogeCashBlock(new DogeCashBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class DogeCashBlockHeader : BlockHeader
		{
			// https://github.com/dogecash/dogecas/blob/master/src/primitives/block.cpp#L19
			private static byte[] CalculateHash(byte[] data, int offset, int count)
			{
				var h = new Quark().ComputeBytes(data.Skip(offset).Take(count).ToArray());

				return h;
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash);
			}
		}

		public class DogeCashBlock : Block
		{
			public DogeCashBlock(DogeCashBlockHeader h) : base(h)
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
			RegisterDefaultCookiePath("DOGEC");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 210000,
					MajorityEnforceBlockUpgrade = 8100,
					MajorityRejectBlockOutdated = 10260,
					MajorityWindow = 10800,
					BIP34Hash = new uint256("000000cb303fa18385fca0610ce525db98f32a5f55f5cd9bf5ff83d88b537915"),
					PowLimit = new Target(0 >> 1),
					MinimumChainWork = new uint256("000000000000000000000000000000000000000000000000c2ba8ca4fb1f06cb"),
					PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
					PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
					PowAllowMinDifficultyBlocks = true,
					CoinbaseMaturity = 30,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 1916,
					MinerConfirmationWindow = 2016,
					ConsensusFactory = DogeCashConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 385
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 31 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 122 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x02, 0x2D, 0x25, 0x33})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x02, 0x21, 0x31, 0x2B})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("DogeCash"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("DogeCash"))
				.SetMagic(0x191643a0)
				.SetPort(56740)
				.SetRPCPort(51573)
				.SetMaxP2PVersion(70920)
				.SetName("DogeCash-main")
				.AddAlias("DogeCash-mainnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("dogec1", "seeds.dogec.io"),
					new DNSSeedData("dogec2", "x9.seeds.dogec.io"),
					new DNSSeedData("dogec3", "testseeds.dogec.io")
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000014e427b75837280517873799a954e87b8b0484f3f1df927888a0ff4fd3a0c9f7bb2eac56f0ff0f1edfa624000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff8604ffff001d01044c7d323031372d30392d32312032323a30313a3034203a20426974636f696e20426c6f636b204861736820666f722048656967687420343836333832203a2030303030303030303030303030303030303039326431356535623365366538323639333938613834613630616535613264626434653766343331313939643033ffffffff0100ba1dd205000000434104c10e83b2703ccf322f7dbd62dd5855ac7c10bd055814ce121ba32607d573b8810c02c0582aed05b4deb9c4b77b26d92428c61256cd42774babea0a073b2ed0c9ac00000000");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 210000,
					MajorityEnforceBlockUpgrade = 51,
					MajorityRejectBlockOutdated = 75,
					MajorityWindow = 100,
					BIP34Hash = new uint256("0000000000000000000000000000000000000000000000000000000000000000"),
					PowLimit = new Target(0 >> 1),
					MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000000000000100010"),
					PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
					PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
					PowAllowMinDifficultyBlocks = true,
					CoinbaseMaturity = 30,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 1512,
					MinerConfirmationWindow = 2016,
					ConsensusFactory = DogeCashConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 1
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 31 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 122 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x02, 0x2D, 0x25, 0x33})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x02, 0x21, 0x31, 0x2B})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tDogeCash"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tDogeCash"))
				.SetMagic(0x191643a0)
				.SetPort(56740)
				.SetRPCPort(51375)
				.SetMaxP2PVersion(70920)
				.SetName("DogeCash-test")
				.AddAlias("DogeCash-testnet")
				.AddSeeds(new NetworkAddress[0])
        //testnet down for now
				.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000008c5b00d67050180b3a90addb9cd1aabbb3dd79ce20fc071d428ce374581b3f7cde30df5cf0ff0f1e1a1754000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3b04ffff001d010433446f676543617368205265706f7765726564204c61756e6368202d20616b736861796e65787573202d204c6971756964333639ffffffff0100000000000000004341047a7df379bd5e6b93b164968c10fcbb141ecb3c6dc1a5e181c2a62328405cf82311dd5b40bf45430320a4f30add05c8e3e16dd56c52d65f7abe475189564bf2b1ac00000000");

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
					BIP34Hash = new uint256(),
					PowLimit = new Target(0 >> 1),
					MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000100a308553b4863b755"),
					PowTargetTimespan = TimeSpan.FromSeconds(1 * 60 * 40),
					PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
					PowAllowMinDifficultyBlocks = false,
					CoinbaseMaturity = 10,
					PowNoRetargeting = true,
					RuleChangeActivationThreshold = 1916,
					MinerConfirmationWindow = 2016,
					ConsensusFactory = DogeCashConsensusFactory.Instance,
					SupportSegwit = false
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 31 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 122 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x02, 0x2D, 0x25, 0x33})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x02, 0x21, 0x31, 0x2B})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tDogeCash"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tDogeCash"))
				.SetMagic(0x191643a0)
				.SetPort(56740)
				.SetRPCPort(51478)
				.SetMaxP2PVersion(70920)
				.SetName("DogeCash-reg")
				.AddAlias("DogeCash-regtest")
				.AddDNSSeeds(new DNSSeedData[0])
				.AddSeeds(new NetworkAddress[0])
        //No regtest at the moment
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000014e427b75837280517873799a954e87b8b0484f3f1df927888a0ff4fd3a0c9f7bb2eac56ffff7f20393000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff8604ffff001d01044c7d323031372d30392d32312032323a30313a3034203a20426974636f696e20426c6f636b204861736820666f722048656967687420343836333832203a2030303030303030303030303030303030303039326431356535623365366538323639333938613834613630616535613264626434653766343331313939643033ffffffff0100ba1dd205000000434104c10e83b2703ccf322f7dbd62dd5855ac7c10bd055814ce121ba32607d573b8810c02c0582aed05b4deb9c4b77b26d92428c61256cd42774babea0a073b2ed0c9ac00000000");

			return builder;
		}
	}
}
