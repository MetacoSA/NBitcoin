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
	public class Viacoin : NetworkSetBase
	{
		public static Viacoin Instance { get; } = new Viacoin();

		public override string CryptoCode => "VIA";

		private Viacoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x05,0x65,0x6a,0x35}, 5223),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x52,0xc4,0x0b,0xc9}, 5223),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6b,0xaa,0xad,0x9d}, 5223),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x8c,0x14,0xde,0x2c,0x7d,0x6b,0xc0,0xa3,0xdc,0xf7}, 5223),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x9b,0x65,0xda,0xa4,0x3d,0xf5,0x2f,0x21,0x89,0xd5}, 5223),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x0d,0xfd,0x43,0xe6,0xeb,0xbc,0xc3,0xe4,0xf4,0x02}, 5223)
};
		static Tuple<byte[], int>[] pnSeed6_test = {
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x8c,0x95,0x7f,0x9b,0x49,0x18,0xd4,0xca,0x06,0x11}, 25223),
};

#pragma warning disable CS0618 // Type or member is obsolete
		public class ViacoinConsensusFactory : ConsensusFactory
		{
			private ViacoinConsensusFactory()
			{
			}

			public static ViacoinConsensusFactory Instance { get; } = new ViacoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new ViacoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new ViacoinBlock(new ViacoinBlockHeader());
			}
		}

		public class ViacoinBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				var headerBytes = this.ToBytes();
				var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
				return new uint256(h);
			}
		}

		public class ViacoinBlock : Block
		{
			public ViacoinBlock(ViacoinBlockHeader header) : base(header)
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return ViacoinConsensusFactory.Instance;
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Viacoin");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 657000,
				MajorityEnforceBlockUpgrade = 15000,
				MajorityRejectBlockOutdated = 19000,
				MajorityWindow = 20000,
				BIP34Hash = new uint256("4e9b54001f9976049830128ec0331515eaabe35a70970d79971da1539a400ba1"),
				PowLimit = new Target(new uint256("000001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 24),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 8100,
				MinerConfirmationWindow = 10800,
				CoinbaseMaturity = 30,
				LitecoinWorkCalculation = true,
				ConsensusFactory = ViacoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 71 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 33 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 176 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			//.SetNetworkStringParser(new ViacoinMainnetAddressStringParser())
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("via"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("via"))
			.SetMagic(0xcbc6680f)
			.SetPort(5223)
			.SetRPCPort(5222)
			.SetName("via-main")
			.AddAlias("via-mainnet")
			.AddAlias("viacoin-mainnet")
			.AddAlias("viacoin-main")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("viacoin.net", "seed.viacoin.net"),
				new DNSSeedData("barbatos.fr", "viaseeder.barbatos.fr"),
				new DNSSeedData("viacoin.net", "mainnet.viacoin.net"),
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd97b9aa8e4ef0ff0f1ecd513f7c0101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 657000,
				MajorityEnforceBlockUpgrade = 510,
				MajorityRejectBlockOutdated = 750,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00001fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60), // 3.5 days
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 24),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 30,
				LitecoinWorkCalculation = true,
				ConsensusFactory = ViacoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 127 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 255 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tvia"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tvia"))
			.SetMagic(0x92efc5a9)
			.SetPort(25223)
			.SetRPCPort(25222)
			.SetName("via-test")
			.AddAlias("via-testnet")
			.AddAlias("viacoin-test")
			.AddAlias("viacoin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("viacoin.net",  "testnet.viacoin.net"),
				new DNSSeedData("viacoin.net", "seed-testnet.viacoin.net")
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000c762a6567f3cc092f0684bb62b7e00a84890b990f07cc71a6bb58d64b98e02e0dee1e352f0ff0f1ec3c927e60101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6204ffff001d01044c5957697265642030392f4a616e2f3230313420546865204772616e64204578706572696d656e7420476f6573204c6976653a204f76657273746f636b2e636f6d204973204e6f7720416363657074696e6720426974636f696e73ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
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
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 2700,
				MinerConfirmationWindow = 3600,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,
				ConsensusFactory = ViacoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tvia"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tvia"))
			.SetMagic(0xda5bbffa)
			.SetPort(15224)
			.SetRPCPort(15223)
			.SetName("via-reg")
			.AddAlias("via-regtest")
			.AddAlias("viacoin-reg")
			.AddAlias("viacoin-regtest")
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd97f6028c4ef0ff0f1e38c3f6160101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}
	}
}
