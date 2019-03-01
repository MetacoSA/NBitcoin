using System;
using System.Linq;
using NBitcoin.Altcoins.HashX11;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/dogecash/dogecash/blob/master/src/chainparams.cpp
	public class Dogecash : NetworkSetBase
	{
		public static Dogecash Instance { get; } = new Dogecash();

		public override string CryptoCode => "DOGEC";

		private Dogecash()
		{
		}

		public class DogecashConsensusFactory : ConsensusFactory
		{
			private DogecashConsensusFactory()
			{
			}

			public static DogecashConsensusFactory Instance { get; } = new DogecashConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new DogecashBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new DogecashBlock(new DogecashBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class DogecashBlockHeader : BlockHeader
		{
			// https://github.com/dogecash/dogecash/blob/master/src/primitives/block.cpp#L19
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

		public class DogecashBlock : Block
		{
#pragma warning disable CS0612 // Type or member is obsolete
			public DogecashBlock(DogecashBlockHeader h) : base(h)
#pragma warning restore CS0612 // Type or member is obsolete
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

		private static uint256 GetPoWHash(BlockHeader header)
		{
			var headerBytes = header.ToBytes();
			var h = SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
			return new uint256(h);
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
					BIP34Hash = new uint256("000009a7bad1966421754adaa60cfaaef30dd065b30e1a93b8c6d71e3cfe1be7"),
					PowLimit = new Target(0 >> 1),
					MinimumChainWork = new uint256("000000000000000000000000000000000000000000000000307539c6aebab49d"),
					PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
					PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
					PowAllowMinDifficultyBlocks = false,
					CoinbaseMaturity = 30,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 1916,
					MinerConfirmationWindow = 2016,
					ConsensusFactory = DogecashConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 222
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 30 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 122 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x02, 0x2D, 0x25, 0x33})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x02, 0x21, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("Dogecash"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("Dogecash"))
				.SetMagic(0x6F1A5C6A)
				.SetPort(6740)
				.SetRPCPort(6783)
				.SetMaxP2PVersion(72019)
				.SetName("dogecash-main")
				.AddAlias("dogecash-mainnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("dogec1", "137.74.196.209"),
					new DNSSeedData("dogec2", "51.68.175.181"),
					new DNSSeedData("dogec3", "46.101.225.244")
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000e0b373411dcad2afa95889ada157c06e59d0aeea0fac7272345576a4d28e23787836b25bf0ff0f1e721d15000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2d04ffff001d010425446f676543617368204d61696e4e6574204c61756e6368202d20317374204f63746f626572ffffffff0100f2052a010000004341047a7df379bd5e6b93b164968c10fcbb141ecb3c6dc1a5e181c2a62328405cf82311dd5b40bf45430320a4f30add05c8e3e16dd56c52d65f7abe475189564bf2b1ac00000000");

			return builder;
		}
//Testnet nd regtest is not active on dogec,so unmodified
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
					ConsensusFactory = DogecashConsensusFactory.Instance,
					SupportSegwit = false,
					CoinType = 1
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 139 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tDogecash"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tDogecash"))
				.SetMagic(0xbb667746)
				.SetPort(51374)
				.SetRPCPort(51375)
				.SetMaxP2PVersion(70910)
				.SetName("dogecash-test")
				.AddAlias("dogecash-testnet")
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
					ConsensusFactory = DogecashConsensusFactory.Instance,
					SupportSegwit = false
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 30 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 13 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 212 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {0x04, 0x88, 0xB2, 0x1E})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {0x04, 0x88, 0xAD, 0xE4})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tDogecash"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tDogecash"))
				.SetMagic(0xAC7ECFA1)
				.SetPort(51476)
				.SetRPCPort(51478)
				.SetMaxP2PVersion(70910)
				.SetName("dogecash-reg")
				.AddAlias("dogecash-regtest")
				.AddDNSSeeds(new DNSSeedData[0])
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000014e427b75837280517873799a954e87b8b0484f3f1df927888a0ff4fd3a0c9f7bb2eac56ffff7f20393000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff8604ffff001d01044c7d323031372d30392d32312032323a30313a3034203a20426974636f696e20426c6f636b204861736820666f722048656967687420343836333832203a2030303030303030303030303030303030303039326431356535623365366538323639333938613834613630616535613264626434653766343331313939643033ffffffff0100ba1dd205000000434104c10e83b2703ccf322f7dbd62dd5855ac7c10bd055814ce121ba32607d573b8810c02c0582aed05b4deb9c4b77b26d92428c61256cd42774babea0a073b2ed0c9ac00000000");

			return builder;
		}
	}
}
