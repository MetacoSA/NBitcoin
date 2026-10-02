using System;
using NBitcoin.DataEncoders;

namespace NBitcoin.Altcoins
{
	public class LitecoinCash : NetworkSetBase
	{
		public static LitecoinCash Instance { get; } = new();

		public override string CryptoCode => "LCC";

		private LitecoinCash()
		{
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();

			builder.SetName("lcc-main")
				.AddAlias("lcc-mainnet")
				.AddAlias("litecoincash-main")
				.AddAlias("litecoincash-mainnet")
				.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 840000,
					PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
					PowTargetTimespan = TimeSpan.FromSeconds(302400),
					PowTargetSpacing = TimeSpan.FromSeconds(150),
					PowAllowMinDifficultyBlocks = false,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 6048,
					MinerConfirmationWindow = 8064,
					SupportSegwit = true,
					LitecoinWorkCalculation = true,
					CoinbaseMaturity = 100,
					BIP9Deployments =
				{
					[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 1199145601, 1230767999),
					[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 1485561600, 1517356801),
					[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 1485561600, 1517356801)
				},
					BIP34Hash = new uint256("fa09d204a83a768ed5a7c8d441fa62f2043abf420cff1226c7b4329aeb9d51cf"),
					BuriedDeployments =
				{
					[BuriedDeployments.BIP34] = 710000,
					[BuriedDeployments.BIP65] = 918684,
					[BuriedDeployments.BIP66] = 811879
				}
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [28])
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [50])
				.SetBase58Bytes(Base58Type.SECRET_KEY, [176])
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY,
					[0x04, 0x88, 0xB2, 0x1E])
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY,
					[0x04, 0x88, 0xAD, 0xE4])
				.SetBech32(
					Bech32Type.WITNESS_PUBKEY_ADDRESS,
					Encoders.Bech32("lcc"))
				.SetBech32(
					Bech32Type.WITNESS_SCRIPT_ADDRESS,
					Encoders.Bech32("lcc"))
				.SetMagic(0xF8BAE4C7)
				.SetPort(62458)
				.SetRPCPort(62457)
				.SetUriScheme("litecoincash")
				.AddDNSSeeds([
					new DNSSeedData(
						"seeds.litecoinca.sh",
						"seeds.litecoinca.sh")
				])
				.SetGenesis(
					"010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd97b9aa8e4ef0ff0f1ecd513f7c0101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();

			builder.SetName("lcc-test")
				.AddAlias("lcc-testnet")
				.AddAlias("litecoincash-test")
				.AddAlias("litecoincash-testnet")
				.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 840000,
					PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
					PowTargetTimespan = TimeSpan.FromSeconds(302400),
					PowTargetSpacing = TimeSpan.FromSeconds(150),
					PowAllowMinDifficultyBlocks = true,
					PowNoRetargeting = false,
					RuleChangeActivationThreshold = 15,
					MinerConfirmationWindow = 20,
					SupportSegwit = true,
					LitecoinWorkCalculation = true,
					CoinbaseMaturity = 100,
					BIP9Deployments =
				{
					[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 1199145601, 1230767999),
					[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 1707828286, 1739364286),
					[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 1707828286, 1739364286)
				},
					BIP34Hash = new uint256("00000025140b1236292bc21b2afa9f3bd5c3d4a8cc1d0e3d1ba0ba7fdefc92eb"),
					BuriedDeployments =
				{
					[BuriedDeployments.BIP34] = 48,
					[BuriedDeployments.BIP65] = 48,
					[BuriedDeployments.BIP66] = 48
				}
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [127])
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [58])
				.SetBase58Bytes(Base58Type.SECRET_KEY, [239])
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY,
					[0x04, 0x35, 0x87, 0xCF])
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY,
					[0x04, 0x35, 0x83, 0x94])
				.SetBech32(
					Bech32Type.WITNESS_PUBKEY_ADDRESS,
					Encoders.Bech32("tlcc"))
				.SetBech32(
					Bech32Type.WITNESS_SCRIPT_ADDRESS,
					Encoders.Bech32("tlcc"))
				.SetMagic(0xCFD3F5B6)
				.SetPort(62456)
				.SetRPCPort(62455)
				.SetUriScheme("litecoincash")
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd97f60ba158f0ff0f1ee17904000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000")
				.AddDNSSeeds([
					new DNSSeedData(
						"testseeds.litecoinca.sh",
						"testseeds.litecoinca.sh")
				]);

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();

			builder.SetName("lcc-reg")
				.AddAlias("lcc-regtest")
				.AddAlias("litecoincash-reg")
				.AddAlias("litecoincash-regtest")
				.SetConsensus(new Consensus
				{
					SubsidyHalvingInterval = 150,
					PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
					PowTargetTimespan = TimeSpan.FromSeconds(302400),
					PowTargetSpacing = TimeSpan.FromSeconds(150),
					PowAllowMinDifficultyBlocks = true,
					PowNoRetargeting = true,
					RuleChangeActivationThreshold = 108,
					MinerConfirmationWindow = 144,
					SupportSegwit = true,
					LitecoinWorkCalculation = true,
					CoinbaseMaturity = 100,
					BIP9Deployments =
				{
					[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 0, 999999999),
					[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 0, 999999999),
					[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, BIP9DeploymentsParameters.AlwaysActive, 999999999)
				},
					BIP34Hash = uint256.Zero,
					BuriedDeployments =
				{
					[BuriedDeployments.BIP34] = 100000000,
					[BuriedDeployments.BIP65] = 1351,
					[BuriedDeployments.BIP66] = 1251
				}
				})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [111])
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [58])
				.SetBase58Bytes(Base58Type.SECRET_KEY, [239])
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY,
					[0x04, 0x35, 0x87, 0xCF])
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY,
					[0x04, 0x35, 0x83, 0x94])
				.SetBech32(
					Bech32Type.WITNESS_PUBKEY_ADDRESS,
					Encoders.Bech32("rlcc"))
				.SetBech32(
					Bech32Type.WITNESS_SCRIPT_ADDRESS,
					Encoders.Bech32("rlcc"))
				.SetMagic(0xDAB5BFFA)
				.SetPort(19444)
				.SetRPCPort(19443)
				.SetUriScheme("litecoincash")
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd97dae5494dffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");

			return builder;
		}
	}
}
