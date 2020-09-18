using System;
using System.Linq;
using NBitcoin.Altcoins.HashX11;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/ColossusCoinXT/ColossusCoinXT/blob/master/src/chainparams.cpp
	public class Colossus : NetworkSetBase
	{
		public static Colossus Instance { get; } = new Colossus();

		public override string CryptoCode => "COLX";

		private Colossus()
		{
		}

		public class ColossusConsensusFactory : ConsensusFactory
		{
			private ColossusConsensusFactory()
			{
			}

			public static ColossusConsensusFactory Instance { get; } = new ColossusConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new ColossusBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new ColossusBlock(new ColossusBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class ColossusBlockHeader : BlockHeader
		{
			// https://github.com/ColossusCoinXT/ColossusCoinXT/blob/master/src/primitives/block.cpp#L19
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

		public class ColossusBlock : Block
		{
			public ColossusBlock(ColossusBlockHeader h) : base(h)
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
			RegisterDefaultCookiePath("COLX");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 210000,
					MajorityEnforceBlockUpgrade = 750,
					MajorityRejectBlockOutdated = 950,
					MajorityWindow = 1000,
					BIP34Hash = new uint256("00000f4fb42644a07735beea3647155995ab01cf49d05fdc082c08eb673433f9"),
					PowLimit = new Target(0 >> 1),
					MinimumChainWork = new uint256("000000000000000000000000000000000000000000000000010b219afffe4a8b"),
					PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
					PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
					PowAllowMinDifficultyBlocks = false,
					CoinbaseMaturity = 30,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 1916,
					MinerConfirmationWindow = 2016,
					ConsensusFactory = ColossusConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 222
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 30 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 13 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 212 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("Colossus"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("Colossus"))
				.SetMagic(0xEAFEC591)
				.SetPort(51572)
				.SetRPCPort(51473)
				.SetMaxP2PVersion(70910)
				.SetName("colossus-main")
				.AddAlias("colossus-mainnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("colx1", "seed.colossuscoinxt.org"),
					new DNSSeedData("colx2", "seed.colossusxt.org"),
					new DNSSeedData("colx3", "seed.colxt.net")
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
					BIP34Hash = new uint256("0x0000047d24635e347be3aaaeb66c26be94901a2f962feccd4f95090191f208c1"),
					PowLimit = new Target(0 >> 1),
					MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000924e924a21715"),
					PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
					PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
					PowAllowMinDifficultyBlocks = true,
					CoinbaseMaturity = 30,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 1512,
					MinerConfirmationWindow = 2016,
					ConsensusFactory = ColossusConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 1
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 139 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tColossus"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tColossus"))
				.SetMagic(0xbb667746)
				.SetPort(51374)
				.SetRPCPort(51375)
				.SetMaxP2PVersion(70910)
				.SetName("colossus-test")
				.AddAlias("colossus-testnet")
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000014e427b75837280517873799a954e87b8b0484f3f1df927888a0ff4fd3a0c9f74e19a55af0ff0f1e316a25000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff8604ffff001d01044c7d323031372d30392d32312032323a30313a3034203a20426974636f696e20426c6f636b204861736820666f722048656967687420343836333832203a2030303030303030303030303030303030303039326431356535623365366538323639333938613834613630616535613264626434653766343331313939643033ffffffff0100ba1dd205000000434104c10e83b2703ccf322f7dbd62dd5855ac7c10bd055814ce121ba32607d573b8810c02c0582aed05b4deb9c4b77b26d92428c61256cd42774babea0a073b2ed0c9ac00000000");

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
					ConsensusFactory = ColossusConsensusFactory.Instance,
					SupportSegwit = false
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 30 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 13 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 212 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tColossus"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tColossus"))
				.SetMagic(0xAC7ECFA1)
				.SetPort(51476)
				.SetRPCPort(51478)
				.SetMaxP2PVersion(70910)
				.SetName("colossus-reg")
				.AddAlias("colossus-regtest")
				.AddDNSSeeds(new DNSSeedData[0])
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000014e427b75837280517873799a954e87b8b0484f3f1df927888a0ff4fd3a0c9f7bb2eac56ffff7f20393000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff8604ffff001d01044c7d323031372d30392d32312032323a30313a3034203a20426974636f696e20426c6f636b204861736820666f722048656967687420343836333832203a2030303030303030303030303030303030303039326431356535623365366538323639333938613834613630616535613264626434653766343331313939643033ffffffff0100ba1dd205000000434104c10e83b2703ccf322f7dbd62dd5855ac7c10bd055814ce121ba32607d573b8810c02c0582aed05b4deb9c4b77b26d92428c61256cd42774babea0a073b2ed0c9ac00000000");

			return builder;
		}
	}
}
