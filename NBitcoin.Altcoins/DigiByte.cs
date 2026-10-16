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
		        .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000ad0f7d7518fc1e90ed28bd0e444ccd8e24d94688355705ed2142006b49d9dd726a62d052f0ff0f1e245925000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d01044555534120546f6461793a2031302f4a616e2f323031342c205461726765743a20446174612073746f6c656e2066726f6d20757020746f203131304d20637573746f6d657273ffffffff01401f0000000000000200ac00000000")
		        .AddDNSSeeds([
			        new DNSSeedData("seed.digibyte.io", "seed.digibyte.io"),
			        new DNSSeedData("seed.diginode.tools", "seed.diginode.tools"),
			        new DNSSeedData("seed.digibyte.link", "seed.digibyte.link"),
			        new DNSSeedData("seed.aroundtheblock.app", "seed.aroundtheblock.app"),
			        new DNSSeedData("seed.tuyul.cc", "seed.tuyul.cc")
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
		        .SetMagic(0xE7B9C6FE)
		        .SetPort(12033)
		        .SetRPCPort(14026)
		        .SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000097c108c537584d093dec35631a8dc487fe01a98226b702c60672476a9fa5ec7680091b6af0ff0f1e899613000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4304ffff001d01043b44696769446f6c6c617220546573746e657432363a205243343420372d6f662d3335204f7261636c65205265736574206f6e204469676942797465ffffffff010040b743ba0000000200ac00000000")
		        .AddDNSSeeds([
			        new DNSSeedData("testnetseed.digibyte.io", "testnetseed.digibyte.io"),
			        new DNSSeedData("testnetseed.digibyte.link", "testnetseed.digibyte.link"),
			        new DNSSeedData("testnetseed.digibyte.services", "testnetseed.digibyte.services")
		        ]);

	        return builder;
        }

        protected override NetworkBuilder CreateRegtest()
        {
            var builder = new NetworkBuilder()
	            .SetName("dgb-regtest")
	            .AddAlias("digibyte-regtest")
	            .AddAlias("digibyte-reg")
	            .SetConsensus(new Consensus
	            {
		            SubsidyHalvingInterval = 150,
		            MajorityEnforceBlockUpgrade = 750,
		            MajorityRejectBlockOutdated = 950,
		            MajorityWindow = 1000
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
	            .SetRPCPort(18443)
	            .SetGenesis(
		            "010000000000000000000000000000000000000000000000000000000000000000000000ad0f7d7518fc1e90ed28bd0e444ccd8e24d94688355705ed2142006b49d9dd723a22915affff7f20040000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d01044555534120546f6461793a2031302f4a616e2f323031342c205461726765743a20446174612073746f6c656e2066726f6d20757020746f203131304d20637573746f6d657273ffffffff01401f0000000000000200ac00000000");

            return builder;
        }
    }
}
