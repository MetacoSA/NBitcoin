using System;
using System.Linq;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	public partial class Bitcoin
	{
		public Network Regtest => _Networks[ChainName.Regtest];

		private Network CreateRegtest()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4adae5494dffff7f20020000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b73ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			builder.SetMagic(0xDAB5BFFA);
			builder.SetMaxP2PVersion(Network.BITCOIN_MAX_P2P_VERSION);
			builder.SetUriScheme(null);
			builder.SetChainName(ChainName.Regtest);
			builder.SetName("RegTest");
			builder.AddAlias("regtest");
			builder.AddAlias("reg");
			builder.AddAlias("regnet");
			builder.AddAlias("btc-regtest");
			builder.SetNetworkSet(this);
			builder.SetPort(18444);
			builder.SetRPCPort(18443);

			for (var index = 0; index < Testnet.base58Prefixes.Length; index++)
			{
				var val = Testnet.base58Prefixes[index];
				builder.SetBase58Bytes((Base58Type)index, val);
			}



			builder.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] {(111)});
			builder.SetBase58Bytes( Base58Type.SCRIPT_ADDRESS,new byte[] {(196)});
			builder.SetBase58Bytes( Base58Type.SECRET_KEY,new byte[] {(239)});
			builder.SetBase58Bytes( Base58Type.EXT_PUBLIC_KEY, new byte[] {(0x04), (0x35), (0x87), (0xCF)});
			builder.SetBase58Bytes( Base58Type.EXT_SECRET_KEY, new byte[] {(0x04), (0x35), (0x83), (0x94)});
			builder.SetBase58Bytes( Base58Type.COLORED_ADDRESS, new byte[] {0x13});



			var encoder = new Bech32Encoder("bcrt");

			builder.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, encoder);
			builder.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, encoder);
			builder.SetBech32(Bech32Type.TAPROOT_ADDRESS, encoder);



			var consensus = new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256(),
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = uint256.Zero,
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				SupportTaproot = true,
				SupportSegwit = true,
				CoinType = 1,
			};

			// Modify the testnet genesis block so the timestamp is valid for a later start.
			consensus.SetBlock(builder._Genesis);

			consensus.BuriedDeployments[BuriedDeployments.BIP34] = 100000000;
			consensus.BuriedDeployments[BuriedDeployments.BIP65] = 100000000;
			consensus.BuriedDeployments[BuriedDeployments.BIP66] = 100000000;

			consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 0, 999999999);
			consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 0, 999999999);
			consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, BIP9DeploymentsParameters.AlwaysActive, 999999999);


			builder.SetConsensus(consensus);
			var result =  builder.BuildAndRegister();

			_Networks.TryAdd(ChainName.Regtest, result);

			assert(Regtest.Consensus.HashGenesisBlock ==
			       uint256.Parse("0x0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206"));
			return result;
		}



	}
}
