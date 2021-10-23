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
	public class Bitcore : NetworkSetBase
	{
		public static Bitcore Instance { get; } = new Bitcore();

		public override string CryptoCode => "BTX";

		private Bitcore()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x33,0x0f,0xde,0xe0}, 8555),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x25,0x78,0xbe,0x4c}, 8555),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x25,0x78,0xba,0x55}, 8555),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0xca,0x8c,0x3c}, 8555),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbc,0x47,0xdf,0xce}, 8555),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0xc2,0x8e,0x7a}, 8555),
		};
		static Tuple<byte[], int>[] pnSeed6_test = {
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x58,0x44,0x34,0xac}, 8666),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x25,0x78,0xba,0x55}, 8666),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbc,0x47,0xdf,0xce}, 8666),
			Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0xc2,0x8e,0x7a}, 8666),
		};        		     		

#pragma warning disable CS0618 // Type or member is obsolete
		public class BitcoreConsensusFactory : ConsensusFactory
		{
			private BitcoreConsensusFactory()
			{
			}

			public static BitcoreConsensusFactory Instance { get; } = new BitcoreConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new BitcoreBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new BitcoreBlock(new BitcoreBlockHeader());
			}
		}

		public class BitcoreBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				//BTX Timetravel Algo implement here
                                throw new NotSupportedException("PoW for BitCore BTX is not supported");
			}
		}

		public class BitcoreBlock : Block
		{
			public BitcoreBlock(BitcoreBlockHeader header) : base(header)
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return BitcoreConsensusFactory.Instance;
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Bitcore");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000, 
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("604148281e5c4b7f2487e5d03cd60d8e6f69411d613f6448034508cea52e9574"), 
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 250,
				MinerConfirmationWindow = 1000,
				CoinbaseMaturity = 100,
				ConsensusFactory = BitcoreConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 03 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 125 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("btx"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("btx"))
			.SetMagic(0xD9B4BEF9) //defined in inverted direction, 0xF9BEB4D9
			.SetPort(8555) 
			.SetRPCPort(8556)
			.SetMaxP2PVersion(80009)
			.SetName("btx-main")
			.AddAlias("btx-mainnet")
			.AddAlias("bitcore-mainnet")
			.AddAlias("bitcore-main")
			.SetUriScheme("bitcore")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("seed.bitcore.cc", "seed.bitcore.cc"),
				new DNSSeedData("94.16.108.85", "94.16.108.85"),
				new DNSSeedData("45.83.104.212", "45.83.104.212"),
				new DNSSeedData("45.132.245.131", "45.132.245.131"),
				new DNSSeedData("94.16.109.242", "94.16.109.242")
			})
			.AddSeeds(ToSeed(pnSeed6_main)) 
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000c787795041016d5ee652e55e3a6aeff6c8019cf0c525887337e0b4206552691613f7fc58f0ff0f1ea12400000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4004ffff001d010438506f77657264652062792042697473656e642d4575726f7065636f696e2d4469616d6f6e642d4d41432d42332032332f4170722f32303137ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

   		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1,
				MinerConfirmationWindow = 2,
				CoinbaseMaturity = 100,
				ConsensusFactory = BitcoreConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tbtx"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tbtx"))
			.SetMagic(0xF1C8D2FD) //defined in inverted direction, 0xFDD2C8F1
			.SetPort(8666)
			.SetRPCPort(50332)
			.SetMaxP2PVersion(80009)
			.SetName("btx-test")
			.AddAlias("btx-testnet")
			.AddAlias("bitcore-test")
			.AddAlias("bitcore-testnet")
			.SetUriScheme("bitcore")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("51.15.84.165", "51.15.84.165"),
				new DNSSeedData("188.68.52.172", "188.68.52.172"),
				new DNSSeedData("37.120.186.85", "37.120.186.85"),
				new DNSSeedData("188.71.223.206", "188.71.223.206")
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000ff00e3481f61b255420602f7af626924221a41224b0d645bd2f082f82c8bc50a5746ff58f0ff0f1e98611a000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4004ffff001d010438506f77657264652062792042697473656e642d4575726f7065636f696e2d4469616d6f6e642d4d41432d42332032332f4170722f32303137ffffffff01807c814a00000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 144,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 100,
				ConsensusFactory = BitcoreConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tbtx")) 
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tbtx")) 
			.SetMagic(0xDAB5BFFA) //defined in inverted direction, 0xFABFB5DA
			.SetPort(19444)
			.SetRPCPort(19332)
			.SetMaxP2PVersion(80009)
			.SetName("btx-reg")
			.AddAlias("btx-regtest")
			.AddAlias("bitcore-reg")
			.AddAlias("bitcore-regtest")
			.SetUriScheme("bitcore")
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000c787795041016d5ee652e55e3a6aeff6c8019cf0c525887337e0b4206552691613f7fc58f0ff0f1ea12400000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4004ffff001d010438506f77657264652062792042697473656e642d4575726f7065636f696e2d4469616d6f6e642d4d41432d42332032332f4170722f32303137ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}
	}
}
