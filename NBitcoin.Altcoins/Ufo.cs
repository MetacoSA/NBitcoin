using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace NBitcoin.Altcoins
{

    public class Ufo : NetworkSetBase
	{

        public static Ufo Instance { get; } = new Ufo();

        public override string CryptoCode => "UFO";

        private Ufo()
        {

        }

        //Format visual studio
        //{({.*?}), (.*?)}
        //Tuple.Create(new byte[]$1, $2)
        static Tuple<byte[], int>[] pnSeed6_main = {
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x05,0x09,0x0e,0xc7}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x49,0x4a,0x39,0x57}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x59,0xb3,0xf0,0x68}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0x83,0x52,0xc0}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x8a,0xc5,0xce,0x84}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x95,0xca,0x66,0x49}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb2,0x3f,0x65,0x1c}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb2,0x4f,0xb2,0x8f}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbc,0x02,0x25,0xe1}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbc,0xa5,0xc0,0x47}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc1,0x1d,0xbb,0x39}, 9887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc7,0xf7,0x07,0xfd}, 6970),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xd4,0x2f,0xe5,0x7f}, 9887),
};
        static Tuple<byte[], int>[] pnSeed6_test = {
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x05,0x09,0x0e,0xc7}, 19887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x18,0xea,0x44,0x91}, 19887),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x25,0xbb,0x7f,0x5c}, 19887),
};

#pragma warning disable CS0618 // Type or member is obsolete
        public class UfoConsensusFactory : ConsensusFactory
        {
            private UfoConsensusFactory()
            {
            }

            public static UfoConsensusFactory Instance { get; } = new UfoConsensusFactory();

            public override BlockHeader CreateBlockHeader()
            {
                return new UfoBlockHeader();
            }
            public override Block CreateBlock()
            {
                return new UfoBlock(new UfoBlockHeader());
            }
        }

        public class UfoBlockHeader : BlockHeader
		{
            public override uint256 GetPoWHash()
            {
                //TODO: Implement here
                throw new NotSupportedException();
            }
        }

        public class UfoBlock : Block
        {
            public UfoBlock(UfoBlockHeader header) : base(header)
            {

            }
            public override ConsensusFactory GetConsensusFactory()
            {
                return UfoConsensusFactory.Instance;
            }
        }

#pragma warning restore CS0618 // Type or member is obsolete


        protected override NetworkBuilder CreateMainnet()
        {
            var builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 400000,
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                BIP34Hash = new uint256("966e30dd04d09232f6f690a04664cd3258abe43eeda2f2291d93706aa494aa54"),
                PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(90),
                PowAllowMinDifficultyBlocks = false,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 10080,
                MinerConfirmationWindow = 13440,
                CoinbaseMaturity = 100,
                LitecoinWorkCalculation = true,
                ConsensusFactory = UfoConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 27 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 68 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 155 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("uf"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("uf"))
            .SetMagic(0xddb7d9fc)
            .SetPort(9887)
            .SetRPCPort(9888)
            .SetName("ufo-main")
            .AddAlias("Ufo-mainnet")
			.SetUriScheme("ufo")
			.AddDNSSeeds(new[]
            {
                new DNSSeedData("Ufocoin.net", "seed1.ufocoin.net"),
                new DNSSeedData("ufocoin.net", "seed2.ufocoin.net"),
                new DNSSeedData("ufocoin.net", "seed3.ufocoin.net"),
                new DNSSeedData("ufocoin.net", "seed4.ufocoin.net"),
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
                SubsidyHalvingInterval = 400000,
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 100,
                PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(90),
                PowAllowMinDifficultyBlocks = true,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 375,
                MinerConfirmationWindow = 500,
                CoinbaseMaturity = 100,
                LitecoinWorkCalculation = true,
				ConsensusFactory = UfoConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 130 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tuf"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tuf"))
            .SetMagic(0xdbb8c0fb)
            .SetPort(19887)
            .SetRPCPort(19888)
            .SetName("ufo-test")
            .AddAlias("Ufo-testnet")
			.SetUriScheme("ufo")
			.AddDNSSeeds(new[]
            {
                new DNSSeedData("ufocoin.net", "testnet-seed.ufocoin.net"),
            })
            .AddSeeds(ToSeed(pnSeed6_test))
            .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd97f60ba158f0ff0f1ee17904000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
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
                PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(90),
                PowAllowMinDifficultyBlocks = true,
                MinimumChainWork = uint256.Zero,
                PowNoRetargeting = true,
                RuleChangeActivationThreshold = 108,
                MinerConfirmationWindow = 144,
                CoinbaseMaturity = 100,
                ConsensusFactory = UfoConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 130 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tuf"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tuf"))
            .SetMagic(0x1c55211b)
            .SetPort(18444)
            .SetRPCPort(18445)
            .SetName("ufo-reg")
            .AddAlias("Ufo-regtest")
			.SetUriScheme("ufo")
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000006570e7f569717849280945b767d0a8ae3a1240e510c8a0abdcbfa5283adf0782dae5494dffff7f20030000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1604ffff001d01040e32206a616e756172792032303134ffffffff0100000000000000000200ac00000000");
            return builder;

        }

        protected override void PostInit()
        {
            RegisterDefaultCookiePath("Ufo");
        }

    }
}
