using NBitcoin.DataEncoders;

namespace NBitcoin.Altcoins
{
    // Reference: https://github.com/wamcoin-core-dev/wam-coin/blob/main/src/kernel/chainparams.cpp
    public class WAM : NetworkSetBase
    {
        public static WAM Instance { get; } = new();
        public override string CryptoCode => "WAM";

        private WAM() { }

        protected override NetworkBuilder CreateMainnet()
        {
            var builder = new NetworkBuilder();

            builder.SetName("wam-main")
                .AddAlias("wam-mainnet")
                .AddAlias("wamcoin-main")
                .AddAlias("wamcoin-mainnet")
                .SetConsensus(new Consensus
                {
                    SubsidyHalvingInterval = 200000,
                    MajorityEnforceBlockUpgrade = 750,
                    MajorityRejectBlockOutdated = 950,
                    MajorityWindow = 1000,
                })
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [73])
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [135])
                .SetBase58Bytes(Base58Type.SECRET_KEY, [190])
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x88, 0xB2, 0x1E])
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x88, 0xAD, 0xE4])
                .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("wam"))
                .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("wam"))
                .SetMagic(0x214D4157)
                .SetPort(9555)
                .SetRPCPort(9554)
                .SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000029fffcf77bae3e9022efe41746d7fc608717e39233c408c2ced4badf79c50f23808aa86af0ff0f1e4d4a13000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d01044057414d204e6574776f726b204c61756e6368696e67204e6578742047656e65726174696f6e20446563656e7472616c697a65642045636f6e6f6d792032303236ffffffff050080ca3961240000200400be896cb17576a91455e6e7c170cb8b3afbd0249dcfe59a6dab1fc3d488ac0080ca3961240000200400436c6eb17576a91455e6e7c170cb8b3afbd0249dcfe59a6dab1fc3d488ac0080ca3961240000200480764d70b17576a91455e6e7c170cb8b3afbd0249dcfe59a6dab1fc3d488ac0080ca3961240000200400aa2e72b17576a91455e6e7c170cb8b3afbd0249dcfe59a6dab1fc3d488ac0080ca3961240000200480dd0f74b17576a91455e6e7c170cb8b3afbd0249dcfe59a6dab1fc3d488ac00000000")
                .AddDNSSeeds([
                    new DNSSeedData("seed1.wamcoin.org", "seed1.wamcoin.org"),
                    new DNSSeedData("seed2.wamcoin.org", "seed2.wamcoin.org"),
                    new DNSSeedData("seed3.wamcoin.org", "seed3.wamcoin.org")
                ]);

            return builder;
        }

        protected override NetworkBuilder CreateTestnet()
        {
            var builder = new NetworkBuilder()
                .SetName("wam-test")
                .AddAlias("wam-testnet")
                .AddAlias("wamcoin-test")
                .AddAlias("wamcoin-testnet")
                .SetConsensus(new Consensus
                {
                    SubsidyHalvingInterval = 200000,
                    MajorityEnforceBlockUpgrade = 750,
                    MajorityRejectBlockOutdated = 950,
                    MajorityWindow = 1000,
                })
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [65])
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [128])
                .SetBase58Bytes(Base58Type.SECRET_KEY, [239])
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x35, 0x87, 0xCF])
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x35, 0x83, 0x94])
                .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("twam"))
                .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("twam"))
                .SetMagic(0x216D6177)
                .SetPort(19555)
                .SetRPCPort(19554)
                .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000bd47e10c1fe316576f2cc078ed902759660a99d771231a7c774be07bbdb1041b00376d6af0ff0f1e0d411c000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d01044057414d204e6574776f726b204c61756e6368696e67204e6578742047656e65726174696f6e20446563656e7472616c697a65642045636f6e6f6d792032303236ffffffff050080ca3961240000200400be896cb17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200400436c6eb17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200480764d70b17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200400aa2e72b17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200480dd0f74b17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac00000000")
                .AddDNSSeeds([
                    new DNSSeedData("testnet-seed.wamcoin.org", "testnet-seed.wamcoin.org")
                ]);

            return builder;
        }

        protected override NetworkBuilder CreateRegtest()
        {
            var builder = new NetworkBuilder()
                .SetName("wam-regtest")
                .AddAlias("wam-reg")
                .AddAlias("wamcoin-regtest")
                .SetConsensus(new Consensus
                {
                    SubsidyHalvingInterval = 150,
                    MajorityEnforceBlockUpgrade = 750,
                    MajorityRejectBlockOutdated = 950,
                    MajorityWindow = 1000,
                })
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [65])
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [128])
                .SetBase58Bytes(Base58Type.SECRET_KEY, [239])
                .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x35, 0x87, 0xCF])
                .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x35, 0x83, 0x94])
                .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("wamrt"))
                .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("wamrt"))
                .SetMagic(0x524D4157)
                .SetPort(29555)
                .SetRPCPort(29554)
                .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000bd47e10c1fe316576f2cc078ed902759660a99d771231a7c774be07bbdb1041bdae5494dffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d01044057414d204e6574776f726b204c61756e6368696e67204e6578742047656e65726174696f6e20446563656e7472616c697a65642045636f6e6f6d792032303236ffffffff050080ca3961240000200400be896cb17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200400436c6eb17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200480764d70b17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200400aa2e72b17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac0080ca3961240000200480dd0f74b17576a9146373d5f3f259812f2dcb75cde924df225261ed6488ac00000000");

            return builder;
        }
    }
}
