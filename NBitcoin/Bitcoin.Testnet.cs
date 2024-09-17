using System;
using System.Linq;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	public partial class Bitcoin
	{
		public Network Testnet => _Networks[ChainName.Testnet];

		private Network CreateTestnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4adae5494dffff001d1aa4ae180101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b73ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			builder.SetMagic(0x0709110B);
			builder.SetMaxP2PVersion(Network.BITCOIN_MAX_P2P_VERSION);
			builder.SetUriScheme(null);
			builder.SetChainName(ChainName.Testnet);
			builder.SetName("TestNet");
			builder.AddAlias("testnet");
			builder.AddAlias("test");
			builder.AddAlias("testnet3");
			builder.AddAlias("btc-testnet");

			builder.SetNetworkSet(this);
			builder.SetPort(18333);
			builder.SetRPCPort(18332);

			for (var index = 0; index < Mainnet.base58Prefixes.Length; index++)
			{
				var val = Mainnet.base58Prefixes[index];
				builder.SetBase58Bytes((Base58Type)index, val);
			}

			builder.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (111) });
			builder.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (196) });
			builder.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (239) });
			builder.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x35), (0x87), (0xCF) });
			builder.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x35), (0x83), (0x94) });
			builder.SetBase58Bytes(Base58Type.ASSET_ID, new byte[] { 115 });
			builder.SetBase58Bytes(Base58Type.COLORED_ADDRESS, new byte[] {0x13});




			var encoder = new Bech32Encoder("tb");

			builder.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, encoder);
			builder.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, encoder);
			builder.SetBech32(Bech32Type.TAPROOT_ADDRESS, encoder);



			var consensus = new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x0000000023b3a96d3484e5abb3755c413e7d41500f8e2a5c3f0dd01299cd8ef8"),
				PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000198b4def2baa9338d6"),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512, // 75% for testchains
				MinerConfirmationWindow = 2016, // nPowTargetTimespan / nPowTargetSpacing
				SupportTaproot = true,
				SupportSegwit = true,
				CoinType = 1,
			};

			// Modify the testnet genesis block so the timestamp is valid for a later start.
			consensus.SetBlock(builder._Genesis);
			consensus.BuriedDeployments[BuriedDeployments.BIP34] = 21111;
			consensus.BuriedDeployments[BuriedDeployments.BIP65] = 581885;
			consensus.BuriedDeployments[BuriedDeployments.BIP66] = 330776;

			consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 1199145601, 1230767999);
			consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 1456790400, 1493596800);
			consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 1462060800, 1493596800);


			builder.SetConsensus(consensus);


#if !NOSOCKET
			builder.AddDNSSeeds(new[]
			{
				new DNSSeedData("bitcoin.jonasschnelli.ch", "testnet-seed.bitcoin.jonasschnelli.ch"),
				new DNSSeedData("tbtc.petertodd.org", "seed.tbtc.petertodd.org"),
				new DNSSeedData("bitcoin.sprovoost.nl", "seed.testnet.bitcoin.sprovoost.nl"),
				new DNSSeedData("bluematt.me", "testnet-seed.bluematt.me")
			});

			// https://github.com/bitcoin/bitcoin/blob/master/src/chainparamsseeds.h
			// All these entries were prefixed with 0x00 because NBitcoin expects the service flags.
			byte[] pnSeed6_test = new byte[] {
				0x00,0x03,0x0a,0x99,0xcb,0x26,0x31,0xba,0x48,0x51,0x31,0x39,0x0d,0x47,0x9d,
				0x00,0x03,0x0a,0x44,0xf4,0xf4,0xf0,0xbf,0xf7,0x7e,0x6d,0xc4,0xe8,0x47,0x9d,
				0x00,0x03,0x0a,0x6a,0x8b,0xd2,0x78,0x3f,0x7a,0xf8,0x92,0x8f,0x80,0x47,0x9d,
				0x00,0x03,0x0a,0xe6,0x4e,0xa4,0x47,0x4e,0x2a,0xfe,0xe8,0x95,0xcc,0x47,0x9d,
				0x00,0x03,0x0a,0x9f,0xae,0x9f,0x59,0x0b,0x3f,0x31,0x3a,0x8a,0x5f,0x47,0x9d,
				0x00,0x03,0x0a,0x47,0xb1,0xe4,0x55,0xd1,0xb0,0x14,0x3f,0xb6,0xdb,0x47,0x9d,
				0x00,0x03,0x0a,0xa0,0x60,0x9e,0x46,0x54,0xdb,0x61,0x3b,0xb2,0x6f,0x47,0x9d,
			};

			builder.AddSeeds(LoadNetworkAddresses(pnSeed6_test,  builder));
#endif

			var result =    builder.BuildAndRegister();

			_Networks.TryAdd(ChainName.Testnet, result);

			assert(Testnet.Consensus.HashGenesisBlock ==
			       uint256.Parse("0x000000000933ea01ad0ee984209779baaec3ced90fa3f408719526f8d77f4943"));
			return result;
		}


	}
}
