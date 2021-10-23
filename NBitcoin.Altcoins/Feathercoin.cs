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

	public class Feathercoin : NetworkSetBase
	{

		public static Feathercoin Instance { get; } = new Feathercoin();

		public override string CryptoCode => "FTC";

		private Feathercoin()
		{

		}

        //Format visual studio
        //{({.*?}), (.*?)}
        //Tuple.Create(new byte[]$1, $2)
        static Tuple<byte[], int>[] pnSeed6_main = {
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x18,0x77,0xbe,0xf6}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x25,0x3b,0x2c,0x21}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2e,0xbc,0x2c,0x14}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x32,0x2e,0x74,0x70}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x44,0x2e,0x28,0x8b}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x45,0x76,0x9b,0xac}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x45,0xa2,0x43,0x03}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x52,0x2e,0x7f,0x1c}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x52,0xc8,0xcd,0x1e}, 9329),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x54,0xea,0x34,0xbe}, 57702),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x57,0xed,0xd2,0x8f}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x5e,0x17,0xd3,0xd2}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x5e,0x82,0xdc,0x02}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x5e,0xf2,0xde,0x1b}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xec,0xaa,0x39}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xed,0x03,0xc3}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6b,0x96,0x34,0x7a}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x89,0xba,0x40,0x79}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xac,0x68,0x16,0x98}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xac,0x68,0x1e,0xd9}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb2,0x53,0x18,0xd7}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb2,0xee,0xec,0x82}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0x19,0x3c,0xc7}, 9329),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbc,0xbf,0xa5,0x67}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc0,0x4d,0xbc,0x68}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2e,0x65,0x3d,0x5b}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2e,0x04,0x00,0x65}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x65,0x64,0xae,0x8a}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xe1,0xd9,0x0c}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x4e,0x6b,0x3a,0xc9}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc0,0xf3,0x64,0x1a}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xe1,0xdc,0x13}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x8b,0xa2,0xd8,0x43}, 9336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xa5,0xe3,0x4d,0x69}, 9336),
    
};
        static Tuple<byte[], int>[] pnSeed6_test = {
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xa7,0x63,0x4f,0x5a}, 19336),
    Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2e,0x04,0x00,0x65}, 19336),
};

#pragma warning disable CS0618 // Type or member is obsolete
        public class FeathercoinConsensusFactory : ConsensusFactory
		{
			private FeathercoinConsensusFactory()
			{
			}

			public static FeathercoinConsensusFactory Instance { get; } = new FeathercoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new FeathercoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new FeathercoinBlock(new FeathercoinBlockHeader());
			}
		}

		public class FeathercoinBlockHeader : BlockHeader
		{
            public override uint256 GetPoWHash()
            {
                //TODO: Implement here
                throw new NotSupportedException();
            }
        }

        public class FeathercoinBlock : Block
        {
            public FeathercoinBlock(FeathercoinBlockHeader header) : base(header)
            {

            }
            public override ConsensusFactory GetConsensusFactory()
            {
                return FeathercoinConsensusFactory.Instance;
            }
        }


#pragma warning restore CS0618 // Type or member is obsolete

        protected override void PostInit()
        {
            RegisterDefaultCookiePath("Feathercoin", new FolderName() { TestnetFolder = "testnet4" });
        }

        protected override NetworkBuilder CreateMainnet()
        {
            var builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 2100000,
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                BIP34Hash = new uint256("966e30dd04d09232f6f690a04664cd3258abe43eeda2f2291d93706aa494aa54"),
                PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(60),
                PowTargetSpacing = TimeSpan.FromSeconds(60),
                PowAllowMinDifficultyBlocks = false,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 15120,
                MinerConfirmationWindow = 20160,
                CoinbaseMaturity = 120,
                LitecoinWorkCalculation = true,
                ConsensusFactory = FeathercoinConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 14 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 5 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 142 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xBC, 0x26 })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xDA, 0xEE })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("fc"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("fc"))
            .SetMagic(0x211a1541)
            .SetPort(9336)
            .SetRPCPort(9337)
            .SetName("ftc-main")
            .AddAlias("ftc-mainnet")
            .AddAlias("feathercoin-mainnet")
            .AddAlias("feathercoin-main")
			.SetUriScheme("feathercoin")
			.AddDNSSeeds(new[]
            {
                new DNSSeedData("feathercoin.com", "dnsseed.feathercoin.com"),
                new DNSSeedData("bushstar.co.uk", "dnsseed.bushstar.co.uk"),
                new DNSSeedData("feathercoin.ch", "dnsseed-static.feathercoin.ch"),
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
                SubsidyHalvingInterval = 2100000,
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 600,
                PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(60),
                PowTargetSpacing = TimeSpan.FromSeconds(60),
                PowAllowMinDifficultyBlocks = true,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 375,
                MinerConfirmationWindow = 500,
                CoinbaseMaturity = 120,
                LitecoinWorkCalculation = true,
                ConsensusFactory = FeathercoinConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tfc"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tfc"))
            .SetMagic(0x716a6591)
            .SetPort(19336)
            .SetRPCPort(19337)
            .SetName("ftc-test")
            .AddAlias("ftc-testnet")
            .AddAlias("feathercoin-test")
            .AddAlias("feathercoin-testnet")
			.SetUriScheme("feathercoin")
			.AddDNSSeeds(new[]
            {
                new DNSSeedData("testnet-explorer2.feathercoin.com","feathercoin.com"),
                new DNSSeedData("testnet-dnsseed.feathercoin.com","feathercoin.com"),
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
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 1000,
                PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(60),
                PowAllowMinDifficultyBlocks = true,
                MinimumChainWork = uint256.Zero,
                PowNoRetargeting = true,
                RuleChangeActivationThreshold = 108,
                MinerConfirmationWindow = 2016,
                CoinbaseMaturity = 120,
                LitecoinWorkCalculation = true,
                ConsensusFactory = FeathercoinConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tfc"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tfc"))
            .SetMagic(0xb1aaa5d1)
            .SetPort(18446)
            .SetRPCPort(18447)
            .SetName("ftc-reg")
            .AddAlias("ftc-regtest")
            .AddAlias("feathercoin-reg")
            .AddAlias("feathercoin-regtest")
			.SetUriScheme("feathercoin")
			.AddDNSSeeds(new DNSSeedData[0])
            .AddSeeds(new NetworkAddress[0])
            .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd977ae4595affff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
            return builder;
        }
    }
}
