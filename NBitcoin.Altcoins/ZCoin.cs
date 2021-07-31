using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace NBitcoin.Altcoins
{
	public class ZCoin : NetworkSetBase
	{
		public static ZCoin Instance { get; } = new ZCoin();

		public override string CryptoCode => "XZC";

		private ZCoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0xcd,0x05}, 8168),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x20,0xe8,0xc3}, 8168),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xee,0xac,0xa4}, 8168),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x3f,0x5b,0x97}, 8168),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0xb8,0xbb}, 8168),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0x33,0x62}, 8168),
		};
		static Tuple<byte[], int>[] pnSeed6_test = {
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x34,0xaf,0xf4,0x16}, 18168)
		};

		public class ZCoinConsensusFactory : ConsensusFactory
		{
			private ZCoinConsensusFactory()
			{
			}

			public static ZCoinConsensusFactory Instance { get; } = new ZCoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new ZCoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new ZCoinBlock(new ZCoinBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class ZCoinBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				//ZCoin algos to be implemented here
                                throw new NotSupportedException("PoW for Zcoin XZC is not supported");
			}
		}

		public class ZCoinBlock : Block
		{
			public ZCoinBlock(ZCoinBlockHeader header) : base(header)
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return ZCoinConsensusFactory.Instance;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("ZCoin");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 420000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
				PowLimit = new Target(new uint256("00ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				ConsensusFactory = ZCoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 82 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 07 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 210 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("xzc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("xzc"))
			.SetMagic(0xF1FED9E3) //defined in inverted direction, 0xF9BEB4D9
			.SetPort(8168)
			.SetRPCPort(8888)
			.SetMaxP2PVersion(80000)
			.SetName("xzc-main")
			.AddAlias("xzc-mainnet")
			.AddAlias("zcoin-mainnet")
			.AddAlias("zcoin-main")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("amsterdam.zcoin.io", "amsterdam.zcoin.io"),
				new DNSSeedData("australia.zcoin.io", "australia.zcoin.io"),
				new DNSSeedData("chicago.zcoin.io", "chicago.zcoin.io"),
				new DNSSeedData("london.zcoin.io", "london.zcoin.io"),
				new DNSSeedData("frankfurt.zcoin.io", "frankfurt.zcoin.io")
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("0200000000000000000000000000000000000000000000000000000000000000000000008327a4aae5254fd54eafc4b74b3b1e6b718539acabfdaec97013065da72a5d36dec55354f0ff0f1e382c02000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5a04f0ff0f1e01044c4c54696d657320323031342f31302f3331204d61696e65204a756467652053617973204e75727365204d75737420466f6c6c6f772045626f6c612051756172616e74696e6520666f72204e6f7704823f0000ffffffff0100000000000000000000000000");
			return builder;
		}

   		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 420000,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0000000023b3a96d3484e5abb3755c413e7d41500f8e2a5c3f0dd01299cd8ef8"),
				PowLimit = new Target(new uint256("00ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(5 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1,
				MinerConfirmationWindow = 2,
				CoinbaseMaturity = 100,
				ConsensusFactory = ZCoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 178 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 185 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("txzc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("txzc"))
			.SetMagic(0x00000008) //defined in inverted direction, 0xFDD2C8F1
			.SetPort(18168)
			.SetRPCPort(18888)
			.SetMaxP2PVersion(80000)
			.SetName("xzc-test")
			.AddAlias("xzc-testnet")
			.AddAlias("zcoin-test")
			.AddAlias("zcoin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("SIGMA1", "sigma1.zcoin.io"),
				new DNSSeedData("SIGMA2", "sigma2.zcoin.io")
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("020000000000000000000000000000000000000000000000000000000000000000000000b8b7f5efce3af62564cb2b1035c711d9add9f59b38721e316ba6c70bd661b325dec55354f0ff0f1eed6436000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5a04f0ff0f1e01044c4c54696d657320323031342f31302f3331204d61696e65204a756467652053617973204e75727365204d75737420466f6c6c6f772045626f6c612051756172616e74696e6520666f72204e6f770408000000ffffffff0100000000000000000000000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 420000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60 * 1000),
				PowTargetSpacing = TimeSpan.FromSeconds(1),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 100,
				ConsensusFactory = ZCoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 178 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("txzc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("txzc"))
			.SetMagic(0xDAB5BFFA) //defined in inverted direction, 0xFABFB5DA
			.SetPort(18444)
			.SetRPCPort(28888)
			.SetMaxP2PVersion(80000)
			.SetName("xzc-reg")
			.AddAlias("xzc-regtest")
			.AddAlias("zcoin-reg")
			.AddAlias("zcoin-regtest")
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000b8b7f5efce3af62564cb2b1035c711d9add9f59b38721e316ba6c70bd661b325dec55354ffff7f201ba4ae180101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5a04f0ff0f1e01044c4c54696d657320323031342f31302f3331204d61696e65204a756467652053617973204e75727365204d75737420466f6c6c6f772045626f6c612051756172616e74696e6520666f72204e6f770408000000ffffffff0100000000000000000000000000");
			return builder;
		}
	}
}
