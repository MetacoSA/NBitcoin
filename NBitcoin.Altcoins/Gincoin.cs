using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.Linq;
using NBitcoin.Altcoins.GincoinInternals;

namespace NBitcoin.Altcoins
{
	public class Gincoin : NetworkSetBase
	{
		public static Gincoin Instance { get; } = new Gincoin();

		public override string CryptoCode => "GIN";

		public Gincoin()
		{
		}

		public class GincoinConsensusFactory : ConsensusFactory
		{
			private GincoinConsensusFactory()
			{
			}

			public static GincoinConsensusFactory Instance { get; } = new GincoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new GincoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new GincoinBlock(new GincoinBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class GincoinBlockHeader : BlockHeader
		{
			byte[] CalculateHash(byte[] data, int offset, int count)
			{
				byte[] thash;
				if (this.nTime <= 1525651200) //7 May 2018 @ midnight UTC
				{
					var output = new byte[32];
					// reference https://github.com/mogwaicoin/NeoScryptCSharp for correct neoscrypt native c# hashing
					// haven't added source because it will need to have unsafe code checked for the project.
					//NeoScrypt.NeoScrypt.neoscrypt(data.Skip(offset).Take(count).ToArray(), ref output, 0x0);
					thash = output;
				}
				else
				{
					var lyra2z = new Lyra2Z();
					thash = lyra2z.ComputeHash(data.Skip(offset).Take(count).ToArray());
				}

				return thash;
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash);
			}
		}

		public class GincoinBlock : Block
		{
			public GincoinBlock(GincoinBlockHeader h) : base(h)
			{
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return Gincoin.Instance.Mainnet.Consensus.ConsensusFactory;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("GincoinCore");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 262800,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x00000cd6bde619b2c3b23ad2e384328a450a37fa28731debf748c3b17f91f97d"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000047a222baa2d1fe"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = GincoinConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 38 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 10 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 198 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("gin"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("gin"))
			.SetMagic(0xBD6B0CBF)
			.SetPort(10111)
			.SetRPCPort(10211)
			.SetMaxP2PVersion(70208)
			.SetName("gincoin-main")
			.AddAlias("gincoin-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("gincoin.io", "seed1.gincoin.io"),
				new DNSSeedData("seed2.gincoin.io", "seed2.gincoin.io"),
				new DNSSeedData("seed3.gincoin.io", "seed3.gincoin.io"),
				new DNSSeedData("seed4.gincoin.io", "seed4.gincoin.io"),
				new DNSSeedData("seed5.gincoin.io", "seed5.gincoin.io")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000005e0a079fb1b3af615b69a59944f734c6d2e491d98024fe8c3067e175cb8318158c368f5af0ff0f1e543300000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4904ffff001d01044154686520477561726469616e2032322d4665622d32303138204e5241206865616420627265616b732073696c656e636520746f2061747461636b2067756e2e2e2effffffff0100943577000000004341044f1443283e594f9a087e02aeceef6c2cac0d651f65c82cea13b929240a73f380e708f5922225ae4551749a7db28c742758f44877fd4ba527789481f3733d61b1ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 262800,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x0000070936337da4fa971d46112401d17a7288b57bde0e45fba010b94b2577a9"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = GincoinConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 140 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tgin"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tgin"))
			.SetMagic(0xFFCAE2CE)
			.SetPort(12111)
			.SetRPCPort(12211)
			.SetMaxP2PVersion(70208)
			.SetName("gin-test")
			.AddAlias("gin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("test.gincoin.io", "test-s1.gincoin.io"),
				new DNSSeedData("test.gincoin.io", "test-s2.gincoin.io")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000005e0a079fb1b3af615b69a59944f734c6d2e491d98024fe8c3067e175cb831815ca3b8f5af0ff0f1e6fde10000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4904ffff001d01044154686520477561726469616e2032322d4665622d32303138204e5241206865616420627265616b732073696c656e636520746f2061747461636b2067756e2e2e2effffffff0100943577000000004341044f1443283e594f9a087e02aeceef6c2cac0d651f65c82cea13b929240a73f380e708f5922225ae4551749a7db28c742758f44877fd4ba527789481f3733d61b1ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256(),
				PowLimit = new Target(new uint256("0x7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = GincoinConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 140 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("regtgin"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("regtgin"))
			.SetMagic(0xDCB7C1FC)
			.SetPort(19111)
			.SetRPCPort(18332)
			.SetMaxP2PVersion(70208)
			.SetName("gin-regtest")
			.AddAlias("gin-regtestnet")
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000005e0a079fb1b3af615b69a59944f734c6d2e491d98024fe8c3067e175cb831815193c8f5affff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4904ffff001d01044154686520477561726469616e2032322d4665622d32303138204e5241206865616420627265616b732073696c656e636520746f2061747461636b2067756e2e2e2effffffff0100943577000000004341044f1443283e594f9a087e02aeceef6c2cac0d651f65c82cea13b929240a73f380e708f5922225ae4551749a7db28c742758f44877fd4ba527789481f3733d61b1ac00000000");
			return builder;
		}
	}
}
