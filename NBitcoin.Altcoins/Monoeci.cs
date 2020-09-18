using NBitcoin;
using NBitcoin.Crypto;
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
	// Reference: https://github.com/monacocoin-net/monoeci-core/blob/master/src/chainparams.cpp
	public class Monoeci : NetworkSetBase
	{
		public static Monoeci Instance { get; } = new Monoeci();

		public override string CryptoCode => "XMCC";

		private Monoeci()
		{

		}
		public class MonoeciConsensusFactory : ConsensusFactory
		{
			private MonoeciConsensusFactory()
			{
			}

			public static MonoeciConsensusFactory Instance { get; } = new MonoeciConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new MonoeciBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new MonoeciBlock(new MonoeciBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class MonoeciBlockHeader : BlockHeader
		{

			static byte[] CalculateHash(byte[] data, int offset, int count)
			{
				return new HashX11.X11().ComputeBytes(data.Skip(offset).Take(count).ToArray());
			}

			// https://github.com/monacocoin-net/monoeci-core/blob/master/src/primitives/block.cpp#L15
			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash, 80);
			}
		}

		public class MonoeciBlock : Block
		{
			public MonoeciBlock(MonoeciBlockHeader h) : base(h)
			{
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return MonoeciConsensusFactory.Instance;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("MonoeciCore");
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
				BIP34Hash = new uint256("0x0000000319f64f5a0d068d82efb8ce0843f20b3a56fee1bf04707ff92fc484d7\t"),
				PowLimit = new Target(new uint256("00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000072eb013e4e8b5407f"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = MonoeciConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 55 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 56 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 60 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x03, 0xE2, 0x5D, 0x7E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x03, 0xE2, 0x59, 0x45 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("xmcc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("xmcc"))
			.SetMagic(0xBD6B0CBF)
			.SetPort(24157)
			.SetRPCPort(24156)
			.SetMaxP2PVersion(70208)
			.SetName("monoeci-main")
			.AddAlias("monoeci-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("ariga.monoeci.io", "ariga.monoeci.io"),
				new DNSSeedData("dorado.monoeci.io", "dorado.monoeci.io"),
				new DNSSeedData("block.monoeci.io", "block.monoeci.io")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000aa991b3e07b573c8c71273100a0bfebd19a7bdc37e8a13a07c7022eacad69c3607d72259f0ff0f1eeb4109000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5704ffff001d01044c4e4d6f6e61636f206f627469656e74206f6666696369656c6c656d656e7420736f6e20696e64c3a970656e64616e6365206475205361696e742d456d7069726520726f6d61696e20656e2031353234ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210240,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x0000000070b3a96d3484e5abb3755c413e7d41500f8e2a5c3f0dd01299cd8ef8"),
				PowLimit = new Target(new uint256("00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = MonoeciConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 139 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("txmcc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("txmcc"))
			.SetMagic(0xFFCAE2CE)
			.SetPort(34157)
			.SetRPCPort(34156)
			.SetMaxP2PVersion(70208)
		   	.SetName("monoeci-test")
		   	.AddAlias("monoeci-testnet")
		   	.AddSeeds(new NetworkAddress[0])
		   	.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000aa991b3e07b573c8c71273100a0bfebd19a7bdc37e8a13a07c7022eacad69c36e1c6d858f0ff0f1ea0cd07000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5704ffff001d01044c4e4d6f6e61636f206f627469656e74206f6666696369656c6c656d656e7420736f6e20696e64c3a970656e64616e6365206475205361696e742d456d7069726520726f6d61696e20656e2031353234ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
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
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = MonoeciConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 140 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("txmcc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("txmcc"))
			.SetMagic(0xDCB7C1FC)
			.SetPort(44157)
			.SetRPCPort(18332)
			.SetMaxP2PVersion(70208)
			.SetName("monoeci-reg")
			.AddAlias("monoeci-regtest")
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000aa991b3e07b573c8c71273100a0bfebd19a7bdc37e8a13a07c7022eacad69c3661304058f0ff0f1e21970d000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5704ffff001d01044c4e4d6f6e61636f206f627469656e74206f6666696369656c6c656d656e7420736f6e20696e64c3a970656e64616e6365206475205361696e742d456d7069726520726f6d61696e20656e2031353234ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

	}
}
