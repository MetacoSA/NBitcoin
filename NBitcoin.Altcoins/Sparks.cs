using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.Linq;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/SparksReborn/sparkspay/blob/master/src/chainparams.cpp
	public class Sparks : NetworkSetBase
	{
		public static Sparks Instance { get; } = new Sparks();

		public override string CryptoCode => "SPK";

		private Sparks()
		{

		}
		public class SparksConsensusFactory : ConsensusFactory
		{
			private SparksConsensusFactory()
			{
			}

			public static SparksConsensusFactory Instance { get; } = new SparksConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new SparksBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new SparksBlock(new SparksBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class SparksBlockHeader : BlockHeader
		{

			byte[] CalculateHash(byte[] data, int offset, int count)
			{
				byte[] thash;
				var output = new byte[32];
				// reference https://github.com/mogwaicoin/NeoScryptCSharp for correct neoscrypt native c# hashing
				NeoScrypt.NeoScrypt.neoscrypt(data.Skip(offset).Take(count).ToArray(), ref output, 0x0);
				thash = output;
				return thash;
			}

			// https://github.com/SparksReborn/sparkspay/blob/master/src/primitives/block.cpp#L18
			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash);
			}
		}

		public class SparksBlock : Block
		{
#pragma warning disable CS0612 // Type or member is obsolete
			public SparksBlock(SparksBlockHeader h) : base(h)
#pragma warning restore CS0612 // Type or member is obsolete
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return SparksConsensusFactory.Instance;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete


		protected override void PostInit()
		{
			RegisterDefaultCookiePath("SparksCore");
		}


		static uint256 GetPoWHash(BlockHeader header)
		{
			var headerBytes = header.ToBytes();
			var h = SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
			return new uint256(h);
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
				BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
				PowLimit = new Target(new uint256("00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x00000000000000000000000000000000000000000000000000dde3dfa3ad67fd"),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = SparksConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 38 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 10 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 198 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			//.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("sparks"))
			//.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("sparks"))
			.SetMagic(0xD4C3B21A)
			.SetPort(8890)
			.SetRPCPort(8892)
			.SetMaxP2PVersion(70212)
			.SetName("sparks-main")
			.AddAlias("sparks-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("seed1.sparkspay.io", "seed1.sparkspay.io"),
				new DNSSeedData("seed2.sparkspay.io", "seed2.sparkspay.io"),
				new DNSSeedData("seed3.sparkspay.io", "seed3.sparkspay.io"),
				new DNSSeedData("seed4.sparkspay.io", "seed4.sparkspay.io"),

			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000e421c96fd8c114ec8903e8a4307092a20ff22db62b37026f4c80dfb9ba52391be2513c5af0ff0f1ee36b0a000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3f04ffff001d01043754686520537061726b732047656e657369732032322e31322e323031373a204f6b2c206c65747320676f20746f20746865206d6f6f6e21ffffffff0100f2052a010000004341048ec1d4d14133344c77703e4c5c722d6b1cb0b840f9531f6b73e93943cddbd2e01e07bd40057a66e0fe19790ffc6d7427cd9088cf6e44c41c4a82b2b92c3b3909ac00000000");
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
				BIP34Hash = new uint256("0x0000000023b3a96d3484e5abb3755c413e7d41500f8e2a5c3f0dd01299cd8ef8"),
				PowLimit = new Target(new uint256("00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000000000ce0010a"),
				PowTargetTimespan = TimeSpan.FromSeconds(5 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(0.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = SparksConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 112 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 20 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 240 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			//.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tsparks"))
			//.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tsparks"))
			.SetMagic(0x7AB32BD1)
			.SetPort(8891)
			.SetRPCPort(8893)
			.SetMaxP2PVersion(70212)
		   	.SetName("sparks-test")
		   	.AddAlias("sparks-testnet")
		   	.AddSeeds(new NetworkAddress[0])
		   	.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000fc4b8cb903aed54e11e1ae8a5b7ad097ade34988a84500ad2d80e4d1f5bcc95d678f875af0ff0f1e94ba01000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2404ffff001d01041c506f6c69732c2066726f6d2070656f706c6520746f2070656f706c65ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
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
				MinimumChainWork = new uint256(),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = SparksConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 112 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 20 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 240 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			//.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tsparks"))
			//.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tsparks"))
			.SetMagic(0x7BD5B3A1)
			.SetPort(18891)
			.SetRPCPort(18893)
			.SetMaxP2PVersion(70212)
			.SetName("sparks-reg")
			.AddAlias("sparks-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000fc4b8cb903aed54e11e1ae8a5b7ad097ade34988a84500ad2d80e4d1f5bcc95d9a3b3b5af0ff0f1e3c8b0d000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2404ffff001d01041c506f6c69732c2066726f6d2070656f706c6520746f2070656f706c65ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}
	}
}
