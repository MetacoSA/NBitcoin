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
	public class TDCoin : NetworkSetBase
	{
		public static TDCoin Instance { get; } = new TDCoin();

		public override string CryptoCode => "TDC";

		private TDCoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc0,0xab,0x12,0xc5}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0xd1,0x9a,0x59}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2c,0xee,0x75,0x1b}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x49,0x7d,0x58,0x03}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x23,0x9e,0x92,0x65}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0xea,0x65,0x15}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x36,0x96,0x57,0x9e}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6c,0x80,0x79,0xdf}, 9901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0xec,0x59,0x23}, 9901)
};


		static Tuple<byte[], int>[] pnSeed6_test = {
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc0,0xab,0x12,0xc5}, 19901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2c,0xee,0x75,0x1b}, 19901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x49,0x7d,0x58,0x03}, 19901),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x0d,0xd1,0x9a,0x59}, 19901)
};

#pragma warning disable CS0618 // Type or member is obsolete
		public class TDCoinConsensusFactory : ConsensusFactory
		{
			private TDCoinConsensusFactory()
			{
			}

			public static TDCoinConsensusFactory Instance { get; } = new TDCoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new TDCoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new TDCoinBlock(new TDCoinBlockHeader());
			}
		}

		public class TDCoinBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				var headerBytes = this.ToBytes();
				var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
				return new uint256(h);
			}
		}

		public class TDCoinBlock : Block
		{
			public TDCoinBlock(TDCoinBlockHeader header) : base(header)
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return TDCoinConsensusFactory.Instance;
			}
		}
/*
		public class TDCoinMainnetAddressStringParser : NetworkStringParser
		{
			public override bool TryParse<T>(string str, Network network, out T result)
			{
				if(str.StartsWith("Ltpv", StringComparison.OrdinalIgnoreCase) && typeof(T) == typeof(BitcoinExtKey))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x88;
						decoded[2] = 0xAD;
						decoded[3] = 0xE4;
						result = (T)(object)new BitcoinExtKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				if(str.StartsWith("Ltub", StringComparison.OrdinalIgnoreCase) && typeof(T) == typeof(BitcoinExtPubKey))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x88;
						decoded[2] = 0xB2;
						decoded[3] = 0x1E;
						result = (T)(object)new BitcoinExtPubKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				return base.TryParse(str, network, out result);
			}
		}
*/
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("TDCoin", new FolderName() { TestnetFolder = "testnet3" });
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 5039370,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("000000006bccd4925d01cfdca84c90bffe266cca24629040fa084fb0d1f8dd19"),
				PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(7 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,
				ConsensusFactory = TDCoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 82 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 107 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tc"))
			.SetMagic(0x12a6f558)
			.SetPort(9901)
			.SetRPCPort(9902)
			.SetName("tdc-main")
			.AddAlias("tdc-mainnet")
			.AddAlias("TDCoin-mainnet")
			.AddAlias("TDCoin-main")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("tokyo.nigez.com", "seeds.nigez.com"),
				new DNSSeedData("usns.nigez.com", "seeds.nigez.com"),
				new DNSSeedData("irns.nigez.com", "seeds4.nigez.com"),
				new DNSSeedData("euns.nigez.com", "seeds2.nigez.com"),
				new DNSSeedData("krns.nigez.com", "seeds3.nigez.com"),
				new DNSSeedData("india.nigez.com", "seeds7.nigez.com"),
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000004ade933b9e1c7ad4d879f9f56b30ba19ee2fea9a55e184c4ad1008810962ba6c67f6cc5cffff001de1a696090101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440342d31322d31392041797261742056205a616b69722c2063726561746f72206f662054415444696720456e67696e6520616e642054616e676b7961206e65742effffffff0100d6117e03000000434104486940951aad21ee9646dd1fc43ec8bf01723cdd95661741034888ffa1f21b837c0a59b9c3b625d66379d35055a389ff3f3d849105ee9da67eb82d13d7cdde67ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 20000000,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(7 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,
				ConsensusFactory = TDCoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 68 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tt"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tt"))
			.SetMagic(0x597b4712)
			.SetPort(19901)
			.SetRPCPort(19902)
			.SetName("tdc-test")
			.AddAlias("tdc-testnet")
			.AddAlias("TDCoin-test")
			.AddAlias("TDCoin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("tokyo.nigez.com", "seeds.nigez.com"),
				new DNSSeedData("usns.nigez.com", "seeds.nigez.com"),
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000004ade933b9e1c7ad4d879f9f56b30ba19ee2fea9a55e184c4ad1008810962ba6c217ab15cffff001d7b4b0c0a0101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440342d31322d31392041797261742056205a616b69722c2063726561746f72206f662054415444696720456e67696e6520616e642054616e676b7961206e65742effffffff0100d6117e03000000434104486940951aad21ee9646dd1fc43ec8bf01723cdd95661741034888ffa1f21b837c0a59b9c3b625d66379d35055a389ff3f3d849105ee9da67eb82d13d7cdde67ac00000000");
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
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,
				ConsensusFactory = TDCoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 90 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tdrt"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tdrt"))
			.SetMagic(0xdab5bffa)
			.SetPort(19444)
			.SetRPCPort(19443)
			.SetName("tdc-reg")
			.AddAlias("tdc-regtest")
			.AddAlias("TDCoin-reg")
			.AddAlias("TDCoin-regtest")
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000004ade933b9e1c7ad4d879f9f56b30ba19ee2fea9a55e184c4ad1008810962ba6c217ab15cffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440342d31322d31392041797261742056205a616b69722c2063726561746f72206f662054415444696720456e67696e6520616e642054616e676b7961206e65742effffffff0100d6117e03000000434104486940951aad21ee9646dd1fc43ec8bf01723cdd95661741034888ffa1f21b837c0a59b9c3b625d66379d35055a389ff3f3d849105ee9da67eb82d13d7cdde67ac00000000");
			return builder;
		}
	}
}
