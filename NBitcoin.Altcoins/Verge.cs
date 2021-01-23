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
	public class Verge : NetworkSetBase
	{
		public static Verge Instance { get; } = new Verge();

		public override string CryptoCode => "XVG";

		private Verge()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x05,0x65,0x6a,0x35}, 21102),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x52,0xc4,0x0b,0xc9}, 21102),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6b,0xaa,0xad,0x9d}, 21102),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x8c,0x14,0xde,0x2c,0x7d,0x6b,0xc0,0xa3,0xdc,0xf7}, 21102),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x9b,0x65,0xda,0xa4,0x3d,0xf5,0x2f,0x21,0x89,0xd5}, 21102),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x0d,0xfd,0x43,0xe6,0xeb,0xbc,0xc3,0xe4,0xf4,0x02}, 21102)
};
		static Tuple<byte[], int>[] pnSeed6_test = {
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x8c,0x95,0x7f,0x9b,0x49,0x18,0xd4,0xca,0x06,0x11}, 21104),
};

#pragma warning disable CS0618 // Type or member is obsolete
		public class VergeConsensusFactory : ConsensusFactory
		{
			private VergeConsensusFactory()
			{
			}
			
			public static VergeConsensusFactory Instance { get; } = new VergeConsensusFactory();
			
			public override BlockHeader CreateBlockHeader()
			{
				return new VergeBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new VergeBlock(new VergeBlockHeader());
			}
		}
		
		public class VergeBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				var headerBytes = this.ToBytes();
				var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
				return new uint256(h);
			}
		}

		public class VergeBlock : Block
		{
			public VergeBlock(VergeBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return VergeConsensusFactory.Instance;
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Verge");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 500000,
				MajorityEnforceBlockUpgrade = 1500,
				MajorityRejectBlockOutdated = 1900,
				MajorityWindow = 200,
				PowLimit = new Target(new uint256("00000fffff000000000000000000000000000000000000000000000000000000")),
				PowTargetTimespan = TimeSpan.FromSeconds(30),
				PowTargetSpacing = TimeSpan.FromSeconds(30),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 120,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				ConsensusFactory = VergeConsensusFactory.Instance,
				LitecoinWorkCalculation = true,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 30 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 33 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 158 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x02, 0x2D, 0x25, 0x33 })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x02, 0x21, 0x31, 0x2B })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("vg"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("vg"))
			.SetMagic(0xf7a77eff)
			.SetPort(21102)
			.SetRPCPort(20102)
			.SetName("verge-main")
			.AddAlias("verge-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("marpmedev.xyz", "seed.marpmedev.xyz"),
				new DNSSeedData("verge.dev", "seed.verge.dev"),
				new DNSSeedData("159.89.46.252", "159.89.46.252"),
				new DNSSeedData("139.59.34.170", "139.59.34.170")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000696ad20e2dd4365c7459b4a4a5af743d5e92c6da3229e6532cd605f6533f2a5b24a6a152f0ff0f1e678601000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1004ffff001d0104084e696e746f6e646fffffffff010058850c020000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 501,
				MajorityRejectBlockOutdated = 750,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000fffff000000000000000000000000000000000000000000000000000000")),
				// pre-post-digishield https://github.com/dogecoin/dogecoin/blob/10a5e93a055ab5f239c5447a5fe05283af09e293/src/chainparams.cpp#L45
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(45),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 240,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				LitecoinWorkCalculation = true,
				ConsensusFactory = VergeConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 115 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 198 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 243 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("vt"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("vt"))
			.SetMagic(0xcdf2c0ef)
			.SetPort(21104)
			.SetRPCPort(21102)
		   .SetName("verge-test")
		   .AddAlias("verge-testnet")
		   .AddDNSSeeds(new[]
		   {
				new DNSSeedData("jrn.me.uk", "testseed.jrn.me.uk")
		   })
		   .AddSeeds(new NetworkAddress[0])
		   .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000696ad20e2dd4365c7459b4a4a5af743d5e92c6da3229e6532cd605f6533f2a5bb9a7f052f0ff0f1ef7390f000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1004ffff001d0104084e696e746f6e646fffffffff010058850c020000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
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
				MajorityWindow = 144,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(4 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 60,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				LitecoinWorkCalculation = true,
				ConsensusFactory = VergeConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("vgrt"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("vgrt "))
			.SetMagic(0xfabfb5da)
			.SetPort(31102)
			.SetRPCPort(44555) // by default this is assigned dynamically, adding port I got for testing
			.SetName("verge-reg")
			.AddAlias("verge-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000696ad20e2dd4365c7459b4a4a5af743d5e92c6da3229e6532cd605f6533f2a5bdae5494dffff7f20020000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1004ffff001d0104084e696e746f6e646fffffffff010058850c020000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}
	}
}
