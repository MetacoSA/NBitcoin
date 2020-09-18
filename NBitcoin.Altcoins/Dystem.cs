using System;
using System.Linq;
using NBitcoin.Altcoins.HashX11;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/Dystem/Dystem/blob/master/src/chainparams.cpp
	public class Dystem : NetworkSetBase
	{
		public static Dystem Instance { get; } = new Dystem();

		public override string CryptoCode => "DTEM";

		private Dystem()
		{
		}

		public class DystemConsensusFactory : ConsensusFactory
		{
			private DystemConsensusFactory()
			{
			}

			public static DystemConsensusFactory Instance { get; } = new DystemConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new DystemBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new DystemBlock(new DystemBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class DystemBlockHeader : BlockHeader
		{
			// https://github.com/Dystemp/Dystem/blob/e596762ca22d703a79c6880a9d3edb1c7c972fd3/src/primitives/block.cpp#L13
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

		public class DystemBlock : Block
		{
			public DystemBlock(DystemBlockHeader h) : base(h)
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
			RegisterDefaultCookiePath("Dystem");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 210240,
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
					ConsensusFactory = DystemConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 222
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] {30})
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] {68})
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] {58})
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("Dystem"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("Dystem"))
				.SetMagic(0x3595a329)
				.SetPort(65443)
				.SetRPCPort(17200)
				.SetMaxP2PVersion(70912)
				.SetName("Dystem-main")
				.AddAlias("Dystem-mainnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("seed.dystem.io", "seed.dystem.io"),
					new DNSSeedData("seed2.dystem.io", "seed2.dystem.io"),
					new DNSSeedData("seed3.dystem.io", "seed3.dystem.io"),
					new DNSSeedData("seed.hashbeat.io", "seed.hashbeat.io")
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000027cc0d8f6a20e41f445b1045d1c73ba4b068ee60b5fd4aa34027cbbe5c2e161e1546db5af0ff0f1e18cb3f010101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6704ffff001d01044c5e4120736c69702d75702062792073757065726d61726b6574204173646120696e20616e206f6e6c696e65206f7264657220736177206120776f6d616e206368617267656420c2a339333020666f7220612073696e676c652062616e616e61ffffffff010000000000000000434104575f641084f76b9e94aae509ce78f6213ee4855d5c245b76d931fa190a1b453edf3ecf2b28288a338ac186d07eedc6d99256838cb57322406edc697f239a0a6eac00000000");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 210240,
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
					ConsensusFactory = DystemConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 1
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] {30})
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] {68})
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] {58})
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tDystem"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tDystem"))
				.SetMagic(0x1d322dc2)
				.SetPort(65444)
				.SetRPCPort(17200)
				.SetMaxP2PVersion(70912)
				.SetName("dystem-test")
				.AddAlias("dystem-testnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("seed.dystem.io", "seed.dystem.io"),
					new DNSSeedData("seed2.dystem.io", "seed2.dystem.io"),
					new DNSSeedData("seed3.dystem.io", "seed3.dystem.io"),
					new DNSSeedData("seed.hashbeat.io", "seed.hashbeat.io")
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000027cc0d8f6a20e41f445b1045d1c73ba4b068ee60b5fd4aa34027cbbe5c2e161e1546db5af0ff0f1e18cb3f010101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6704ffff001d01044c5e4120736c69702d75702062792073757065726d61726b6574204173646120696e20616e206f6e6c696e65206f7264657220736177206120776f6d616e206368617267656420c2a339333020666f7220612073696e676c652062616e616e61ffffffff010000000000000000434104575f641084f76b9e94aae509ce78f6213ee4855d5c245b76d931fa190a1b453edf3ecf2b28288a338ac186d07eedc6d99256838cb57322406edc697f239a0a6eac00000000");

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			var res = builder.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 210240,
					MajorityEnforceBlockUpgrade = 750,
					MajorityRejectBlockOutdated = 950,
					MajorityWindow = 1000,
					BIP34Hash = new uint256("0x000007d91d1254d60e2dd1ae580383070a4ddffa4c64c2eeb4a2f9ecc0414343"),
					PowLimit = new Target(0 >> 1),
					MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000100a308553b4863b755"),
					PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
					PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
					PowAllowMinDifficultyBlocks = false,
					CoinbaseMaturity = 10,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 1916,
					MinerConfirmationWindow = 2016,
					ConsensusFactory = DystemConsensusFactory.Instance,
					SupportSegwit = false
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] {30})
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] {68})
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] {58})
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tDystem"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tDystem"))
				.SetMagic(0x7d322ba2)
				.SetPort(65445)
				.SetRPCPort(17200)
				.SetMaxP2PVersion(70912)
				.SetName("dystem-reg")
				.AddAlias("dystem-regtest")
				.AddDNSSeeds(new DNSSeedData[0])
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000027cc0d8f6a20e41f445b1045d1c73ba4b068ee60b5fd4aa34027cbbe5c2e161e434fdb5af0ff0f1e4a6c51010101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6704ffff001d01044c5e4120736c69702d75702062792073757065726d61726b6574204173646120696e20616e206f6e6c696e65206f7264657220736177206120776f6d616e206368617267656420c2a339333020666f7220612073696e676c652062616e616e61ffffffff010000000000000000434104575f641084f76b9e94aae509ce78f6213ee4855d5c245b76d931fa190a1b453edf3ecf2b28288a338ac186d07eedc6d99256838cb57322406edc697f239a0a6eac00000000");

			return builder;
		}
	}
}
