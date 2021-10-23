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
	public class Monacoin : NetworkSetBase
	{
		public static Monacoin Instance { get; } = new Monacoin();

		public override string CryptoCode => "MONA";

		private Monacoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x31,0xd4,0xa6,0xb5}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0x9c,0xee,0xcb}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xe9,0x7a,0xa9}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x71,0x92,0x44,0xfb}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x7c,0x27,0x04,0x93}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x80,0xc7,0xd6,0xa8}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x80,0xc7,0xfe,0xd8}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x90,0x4c,0x03,0x88}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x99,0x78,0x27,0x59}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc7,0x7f,0x6c,0xa1}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xca,0xb5,0x65,0xcd}, 9401),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xdb,0x75,0xf8,0x37}, 9401),
};
		static Tuple<byte[], int>[] pnSeed6_test = {
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6f,0x67,0x3b,0x7d}, 19403),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x99,0x78,0x27,0x59}, 19403)
};

#pragma warning disable CS0618 // Type or member is obsolete
		public class MonacoinConsensusFactory : ConsensusFactory
		{
			private MonacoinConsensusFactory()
			{
			}

			public static MonacoinConsensusFactory Instance { get; } = new MonacoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new MonacoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new MonacoinBlock(new MonacoinBlockHeader());
			}
		}

		public class MonacoinBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				//TODO: Implement here.
				throw new NotSupportedException();
			}
		}

		public class MonacoinBlock : Block
		{
			public MonacoinBlock(MonacoinBlockHeader header) : base(header)
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return MonacoinConsensusFactory.Instance;
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Monacoin");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 1051200,
				MajorityEnforceBlockUpgrade = 750,//TODO
				MajorityRejectBlockOutdated = 950,//TODO
				MajorityWindow = 10080,
				BIP34Hash = new uint256("ff9f1c0116d19de7c9963845e129f9ed1bfc0b376eb54fd7afa42e0d418c8bb6"),
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(1.0 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1.5 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 7560,
				MinerConfirmationWindow = 10080,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,//TODO
				ConsensusFactory = MonacoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 50 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 55 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 176 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("mona"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("mona"))
			.SetMagic(0xdbb6c0fb)
			.SetPort(9401)
			.SetRPCPort(9402)
			.SetName("mona-main")
			.AddAlias("mona-mainnet")
			.AddAlias("monacoin-mainnet")
			.AddAlias("monacoin-main")
			.SetUriScheme("monacoin")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("monacoin.org", "dnsseed.monacoin.org"),
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000a64bac07fe31877f31d03252953b3c32398933af7a724119bc4d6fa4a805e435f083c252f0ff0f1e66d612000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5f04ffff001d01044c564465632e20333174682032303133204a6170616e2c205468652077696e6e696e67206e756d62657273206f6620746865203230313320596561722d456e64204a756d626f204c6f74746572793a32332d313330393136ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 1052100,
				MajorityEnforceBlockUpgrade = 51,//TODO:
				MajorityRejectBlockOutdated = 75,//TODO
				MajorityWindow = 1000,//TODO
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(1.1 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 74,
				MinerConfirmationWindow = 100,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,//TODO
				ConsensusFactory = MonacoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tmona"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tmona"))
			.SetMagic(0xf1c8d2fd)
			.SetPort(19403)
			.SetRPCPort(19402)
			.SetName("mona-test")
			.AddAlias("mona-testnet")
			.AddAlias("monacoin-test")
			.AddAlias("monacoin-testnet")
			.SetUriScheme("monacoin")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("monacoin.org", "testnet-dnsseed.monacoin.org"),
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000a64bac07fe31877f31d03252953b3c32398933af7a724119bc4d6fa4a805e435ec2dbf58f0ff0f1e6c6420000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5f04ffff001d01044c564465632e20333174682032303133204a6170616e2c205468652077696e6e696e67206e756d62657273206f6620746865203230313320596561722d456e64204a756d626f204c6f74746572793a32332d313330393136ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 51,//TODO
				MajorityRejectBlockOutdated = 75,//TODO
				MajorityWindow = 144,//TODO
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(1.1 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,//TODO
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,
				ConsensusFactory = MonacoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("rmona"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("rmona"))
			.SetMagic(0xdab5bffa)
			.SetPort(20444)
			.SetRPCPort(19402)
			.SetName("mona-reg")
			.AddAlias("mona-regtest")
			.AddAlias("monacoin-reg")
			.AddAlias("monacoin-regtest")
			.SetUriScheme("monacoin")
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000a64bac07fe31877f31d03252953b3c32398933af7a724119bc4d6fa4a805e435dae5494dffff7f20010000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5f04ffff001d01044c564465632e20333174682032303133204a6170616e2c205468652077696e6e696e67206e756d62657273206f6620746865203230313320596561722d456e64204a756d626f204c6f74746572793a32332d313330393136ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}
	}
}
