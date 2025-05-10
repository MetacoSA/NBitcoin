using NBitcoin.DataEncoders;

namespace NBitcoin.Altcoins
{
    public class DigiByte : NetworkSetBase
    {
        public static DigiByte Instance { get; } = new();
        public override string CryptoCode => "DGB";

        private DigiByte() { }

        protected override NetworkBuilder CreateMainnet()
        {
	        var builder = new NetworkBuilder();

	        builder.SetName("dgb-main")
		        .AddAlias("dgb-mainnet")
		        .AddAlias("digibyte-main")
		        .SetConsensus(new Consensus
		        {
			        SubsidyHalvingInterval = 210000,
			        MajorityEnforceBlockUpgrade = 750,
			        MajorityRejectBlockOutdated = 950,
			        MajorityWindow = 1000,
		        })
		        .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [30])
		        .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [63])
		        .SetBase58Bytes(Base58Type.SECRET_KEY, [128])
		        .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x88, 0xB2, 0x1E])
		        .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x88, 0xAD, 0xE4])
		        .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("dgb"))
		        .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("dgb"))
		        .SetMagic(0xDAB6C3FA)
		        .SetPort(12024)
		        .SetRPCPort(14022)
		        .SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4a29ab5f49ffff001d1dac2b7c")
		        .AddDNSSeeds([
			        new DNSSeedData("seed.digibyte.org", "seed.digibyte.org"),
			        new DNSSeedData("seed.digibyte.co", "seed.digibyte.co"),
			        new DNSSeedData("digiexplorer.info", "digiexplorer.info")
		        ]);

	        return builder;
        }

        protected override NetworkBuilder CreateTestnet()
        {
	        var builder = new NetworkBuilder()
		        .SetName("dgb-test")
		        .AddAlias("digibyte-testnet")
		        .AddAlias("digibyte-test")
		        .SetConsensus(new Consensus
		        {
			        SubsidyHalvingInterval = 210000,
			        MajorityEnforceBlockUpgrade = 750,
			        MajorityRejectBlockOutdated = 950,
			        MajorityWindow = 1000
		        })
		        .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [0x7E])  // tD
		        .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [0x8C]) // tS
		        .SetBase58Bytes(Base58Type.SECRET_KEY, [0xFE])      // tWIF
		        .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x35, 0x87, 0xCF])
		        .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x35, 0x83, 0x94])
		        .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("dgbt"))
		        .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("dgbt"))
		        .SetMagic(0xDDB6C3FA)
		        .SetPort(12026)
		        .SetRPCPort(14023)
		        .SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4a29ab5f49ffff001d1dac2b7c")
		        .AddDNSSeeds([
			        new DNSSeedData("seed.testnet-1.us.digibyteservers.io", "seed.testnet-1.us.digibyteservers.io"),
			        new DNSSeedData("seed.testnetexplorer.digibyteservers.io", "seed.testnetexplorer.digibyteservers.io")
		        ]);

	        return builder;
        }

        protected override NetworkBuilder CreateRegtest()
        {
            var builder = new NetworkBuilder();

            builder.SetName("dgb-regtest")
	            .AddAlias("digibyte-regtest")
	            .SetConsensus(new Consensus
	            {
		            SubsidyHalvingInterval = 150,
		            MajorityEnforceBlockUpgrade = 750,
		            MajorityRejectBlockOutdated = 950,
		            MajorityWindow = 1000,
	            })
	            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [126])
	            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [140])
	            .SetBase58Bytes(Base58Type.SECRET_KEY, [254])
	            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x35, 0x87, 0xCF])
	            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x35, 0x83, 0x94])
	            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("dgbrt"))
	            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("dgbrt"))
	            .SetMagic(0xDAB5BFFA)
	            .SetPort(18444)
	            .SetRPCPort(18443);

            return builder;
        }
    }
}
