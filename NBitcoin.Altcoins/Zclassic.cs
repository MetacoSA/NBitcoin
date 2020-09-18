using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NBitcoin.Altcoins
{
    // Reference: https://github.com/z-classic/zclassic/blob/master/src/chainparams.cpp
    public class Zclassic : NetworkSetBase
    {
        public static Zclassic Instance { get; } = new Zclassic();

        public override string CryptoCode => "ZCL";

        private Zclassic()
        {

        }
        public class ZclassicConsensusFactory : ConsensusFactory
        {
            private ZclassicConsensusFactory()
            {
            }
            public static ZclassicConsensusFactory Instance { get; } = new ZclassicConsensusFactory();

            public override BlockHeader CreateBlockHeader()
            {
                return new ZclassicBlockHeader();
            }
            public override Block CreateBlock()
            {
                return new ZclassicBlock(new ZclassicBlockHeader());
            }
        }

        public class ZclassicBlock : Block
        {
#pragma warning disable CS0618 // Type or member is obsolete
			public ZclassicBlock(ZclassicBlockHeader header) : base(header)
			{
			}

            public override ConsensusFactory GetConsensusFactory()
            {
                return ZclassicConsensusFactory.Instance;
            }
        }
        public class ZclassicBlockHeader : BlockHeader
        {
            public override uint256 GetPoWHash()
            {
                throw new NotImplementedException();
            }

            public override void ReadWrite(BitcoinStream stream)
            {
                base.ReadWrite(stream);
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
        {
            RegisterDefaultCookiePath("Zclassic");
            // Alternatively,
            /*
			RegisterDefaultCookiePath(Mainnet, ".cookie");
			RegisterDefaultCookiePath(Testnet, "testnet3", ".cookie");
			RegisterDefaultCookiePath(Regtest, "regtest", ".cookie");
            */
        }

        protected override NetworkBuilder CreateMainnet()
        {
            NetworkBuilder builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 840000,
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 4000,
                BIP34Hash = new uint256("0x0007104ccda289427919efc39dc9e4d499804b7bebc22df55f8b834301260602"), // (Genesis)
                PowLimit = new Target(new uint256("07ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60), //TODO ? - maxTipAge = 24 * 60 * 60
                PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
                PowAllowMinDifficultyBlocks = false,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 1916,
                MinerConfirmationWindow = 2016,
                CoinbaseMaturity = 100,
                MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000002f67046eb6a34"),
                ConsensusFactory = ZclassicConsensusFactory.Instance,
                SupportSegwit = false
            })
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x1C, 0xB8 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x1C, 0xBD })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0x80 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
            .SetMagic(0x24e92764)
            .SetPort(8033)
            .SetRPCPort(8023)
            .SetMaxP2PVersion(170002)
            .SetName("zcl-main")
            .AddAlias("zcl-mainnet")
            .AddAlias("Zclassic-mainnet")
            .AddAlias("Zclassic-main")
            .AddDNSSeeds(new[]
            {
				new DNSSeedData("zclassic.org", "na1.zclassic.org"),
				new DNSSeedData("zclassic.org", "na2.zclassic.org"),
				new DNSSeedData("zclassic.org", "na3.zclassic.org"),
				new DNSSeedData("zclassic.org", "eu1.zclassic.org"),
				new DNSSeedData("zclassic.org", "eu2.zclassic.org"),
				new DNSSeedData("zclassic.org", "eu3.zclassic.org"),
				new DNSSeedData("zclassic.org", "as1.zclassic.org"),
				new DNSSeedData("zclassic.org", "as2.zclassic.org"),
				new DNSSeedData("zclassic.org", "as3.zclassic.org"),
				new DNSSeedData("chains.run", "seed.zcl.chains.run"),
				new DNSSeedData("indieonion.org", "dnsseed.indieonion.org"),
				new DNSSeedData("rotorproject.org", "dnsseed.rotorproject.org")
            })
            .AddSeeds(new NetworkAddress[0])
            .SetGenesis("009aaa951ca873376788d3002918d956e371bdf03c1afcfd8eea17867b5480d2e59a2a4dd52ed0d091af0c0909aa66ce2da97266926a9ea69b9ccca389bc120d9c4dbbae727ab9d6dfd1cd847df0ef0cc9bc989f11bdd6522429c15957daa3c5a2612522ded69857c148c0638611a19287599b47683c714b5774d0fcb1341cf4fc3a546a2441a19f02a55c6f9775749e57783b2abd5b25d41753d2f60892bbb4c3173d7787dbf5e50267324db218a14dd65f71bb02cf2566d3201800f866701db8c221424b75c639de58e7e40705157ae7d10da708ec2b9e71b9bc1ad34854a7bdf58d93766b6e291d3b545fa1f785a1a9829eccd525d16856f4317f0449d5c3516736f1e564f17690f13d3c939ad5516f1db70194902c20afd939168037fa404ec962dfbe752f79ac87a2cc3fd07bcd94d1975b1849cc739c0bc144ae4e75eda1bbed5b5ef8f65966257ec7b1fc6bb600e12e1c65c8c13a505f35dd363e07b6238211a0e502e36db5a620310b544360dd9b4a6cedabc34eeb530139daad50d4a5b6eaf4d50be4ba10e970ce984fb705376a3b0b4bf3f3778600f14e739e04406106f707085ab87ca70598c032b6717a54a9fd8ef72fdd78fb41fa9d45ad685caf77e0fc42e8e644634c24bc972f3ab0e3f0345854eda624045feb6bc9d20b5b1fc6903ebc64026e51da598c0d8711c452131a8fd2bbe01403af20e5db88afcd53b6107f001dae78b548d6a1581baca15359de83e54e75d8fc6374ca1edec17a9f4b06931162f9952575c5c3fb5dfc70a0f793049e781926daaafd4f4d330cf7d5635af1541f0d29e709a37c088d6d2e7aa09d15dfb9c2ae6c1ce661e85e9d89772eb47cfea00c621b66faf8a48cfa970b898dbd77b14e7bf44b742c00f76d2435f949f027132adb1e974551488f988e9fe379a0f86538ee59e26637a3d50bf400c7f52aa9457d77c3eb426628bb17909b26a6820d0772d4c6f74472f635e4c6e72272ce01fc475df69e10371457c55e0fbdf3a392850b9924da9c9a55792325c4318562593f0df8d39559065be03a22b1b6c21206aa1958a0d33257d89b74dea42a11aabf8eddbfe6136ab649744b704eb3e3d473654b588927dd9f486c1cd02639cf656ccbf2c4869c2ed1f2ba4ec55e69a42d5af6b3605a0cdf987734727c6fc1c1489870fb300139328c4d12eb6f5e8309cc09f5f3c29ab0957374113931ec9a56e7579446f12faacda9bd50899a17bd0f78e89ed70a723fdadfb1f4bc3317c8caa32757901604fb79ae48e22251c3b1691125ec5a99fabdf62b015bc817e1c30c06565a7071510b014058a77856a150bf86ab0c565b8bbbed159e2fb862c6215752bf3f0563e2bbbf23b0dbfb2de21b366b7e4cda212d69502643ca1f13ce362eef7435d60530b9999027dd39cd01fd8e064f1ccf6b748a2739707c9f76a041f82d3e046a9c184d83396f1f15b5a11eddb2baff40fc7b410f0c43e36ac7d8ff0204219abe4610825191fbb2be15a508c839259bfd6a4c5204c779fad6c23bbd37f90709654a5b93c6f93b4c844be12cd6cd2200afbf600b2ae9b6c133d8cdb3a85312a6d9948213c656db4d076d2bacd10577d7624be0c684bd1e5464bb39006a524d971cd2223ae9e23dea12366355b3cc4c9f6b8104df6abd23029ac4179f718e3a51eba69e4ebeec511312c423e0755b53f72ac18ef1fb445d7ab83b0894435a4b1a9cd1b473792e0628fd40bef624b4fb6ba457494cd1137a4da9e44956143068af9db98135e6890ef589726f4f5fbd45a713a24736acf150b5fb7a4c3448465322dccd7f3458c49cf2d0ef6dd7dd2ed1f1147f4a00af28ae39a73c827a38309f59faf8970448436fbb14766a3247aac4d5c610db9a662b8cb5b3e2");
            return builder;
        }
        protected override NetworkBuilder CreateTestnet()
        {
            var builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 840000,
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 400,
                BIP34Hash = new uint256("03e1c4bb705c871bf9bfda3e74b7f8f86bff267993c215a89d5795e3708e5e1f"),
                PowLimit = new Target(new uint256("07ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
                PowAllowMinDifficultyBlocks = true,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 1512,
                MinerConfirmationWindow = 2016,
                CoinbaseMaturity = 100,
                MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000000000000000013"),
                ConsensusFactory = ZclassicConsensusFactory.Instance,
                SupportSegwit = false
            })
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x1D, 0x25 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x1C, 0xBA })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0xEF })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetMagic(0xfa1af9bf)
            .SetPort(18233)
            .SetRPCPort(18023)
            .SetMaxP2PVersion(170002)
            .SetName("zcl-test")
            .AddAlias("zcl-testnet")
            .AddAlias("Zclassic-test")
            .AddAlias("Zclassic-testnet")
            .AddDNSSeeds(new[]
            {
                new DNSSeedData("zclassic.org", "dnsseed.testnet.zclassic.org")
            })
            .AddSeeds(new NetworkAddress[0])
            .SetGenesis("002b24e10a5d2ab32b053a20ca6ebed779be1d935b1500eeea5c87aec684c6f934196fdfca6539de0cf1141544bffc5c0d1d4bab815fb5d8c2b195ccdf0755599ee492b9d98e3b79a178949f45485ad80dba38ec0461102adaa369b757ebb2bf8d75b5f67a341d666406d862a102c69800f20a7075be360a7eb2d315d78e4ce32c741f3baf7bf3e1e651976f734f367b1f126f62503b34d06d6e99b3659b2a47f5cfcf71c87e24e5023151d4af87454e7638a19b846350dd5fbc53e4ce1cce2597992b36cbcae0c24717e412c8df9ddca3e90c7629bd8c157c66d8906486943cf78e24d55dd4152f45eff49acf9fb9fddef81f2ee55892b38db940c404eaacf819588b83f0f761f1ba5b31a0ea1f8f4c5210638bbb59a2d8ddff9535f546b42a7eac5f3ee87616a075bddc3118b7f2c041f4b1e8dbcd11eea95835403066b5bb50cd23122dcb12166d75aafcfc1ca8f30580b4d48a5aa305657a06b4b650ed4633f2fa496235082feff65f70e19871f41b70632b53e57ddf38c207d631e5a56fa50bb71150f99427f73d82a439a5f70dfc7d8bbfc39d330ca7924527a5deb8950b9fa7020cfde5e07b84546e96764519ef6dd3fdc3a974abd342bdc7e4ee76bc11d5519541015afba1a0517fd347196aa326b0905a5916b83515c16f8f13105479c29f1eff3bc024ddbb07dcc672247cedc0d4ba32332ead0f13c58f50170642e16e076c34f5e75e3e8f5ac7f5238d67564fd385efecf972b0abf939a99bc7ef8f3a21cac21d2168706bbad3f4af66bb01cf61cfbc352a23797b62dcb5480bf2b7b277af233f5ce42a144d47119a89e1d114fa0bec2f13475b6b1df907bc3a429f1771afa3857bf16bfca3f76a5df14da62dc157fff4225bda73c3cfefa989edc24673bf932a024593da4c38b1a4628dd77ad919f4f7b7fb76976e696db69c89016ab30d9aa2d509f78d913d00ca9ac881aa759fc019b8c5e3eac6fddb4e0f044595e10d4997e29c79800f77cf1d97583d534db0f2726cba3739e7371eeffa2aca12b0d290ac45f44973f32f7675a5b49c94c4b608da2926555d16b7eb3670e12345a63f88797e5a5e21252c2c9463d7896001031a81bac0354336b35c5a10c93d9ae3054f6f6e4492f7c1f09a9d75034d5d0b220a9bb231e583659d5b6923a4e879326194de5c9805a02cb648508a8f9b6cd26dc17d322a478c1c599e1ec3adf2da6ce7a7e3a073b55cf30cf6b124f7700409abe14af8c60ab178579623916f165dbfd26f37056bf33c34f3af30939e1277376e4c5cba339f36381a05ef6481db033fb4c07a19e8655f8b12f9ab3c602e127b4ab1ee48e1c6a91382b54ed36ef9bb21b3bfa80a9107864dcb594dcad250e402b312607e648639631a3d1aeb17cfe3370202720ca8a46db15af92e8b46062b5bd035b24c35a592e5620d632faf1bf19a86df179fe52dd4cdbecd3cb7a336ca7489e4d1dc9433f1163c89d88c5eac36fc562496dc7583fe67c559c9a71cf89e9a0a59d5a14764926852d44a88d2ddb361d612ec06f9de874473eaf1d36b3a41911ac072b7826e6acea3d8425dc271833dba2ec17d1a270e49becbf21330ba2f0edc4b05f4df01623f3c82246ae23ea2c022434ef09611aa19ba35c3ecbad965af3ad9bc6c9b0d3b059c239ffbf9272d0150c151b4510d659cbd0e4a9c32945c612681b70ee4dcbeefeacde630b127115fd9af16cef4afefe611c9dfcc63e6833bf4dab79a7e1ae3f70321429557ab9da48bf93647830b5eb5780f23476d3d4d06a39ae532da5b2f30f151587eb5df19ec1acf099e1ac506e071eb52c3c3cc88ccf6622b2913acf07f1b772b5012e39173211e51773f3eb42d667fff1d902c5c87bd507837b3fd993e70ac9706a0");
            return builder;
        }

        protected override NetworkBuilder CreateRegtest()
        {
            var builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 840000,
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                BIP34Hash = new uint256(),
                PowLimit = new Target(new uint256("0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f")),
                PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
                PowAllowMinDifficultyBlocks = true,
                MinimumChainWork = uint256.Zero,
                PowNoRetargeting = true,
                RuleChangeActivationThreshold = 108,
                MinerConfirmationWindow = 144,
                CoinbaseMaturity = 100,
                ConsensusFactory = ZclassicConsensusFactory.Instance,
                SupportSegwit = false
            })
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x19, 0x57 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x19, 0xE0 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0xEF })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetMagic(0xaae83f5f)
            .SetPort(18033)
            .SetRPCPort(18023)
            .SetMaxP2PVersion(170002)
            .SetName("zcl-reg")
            .AddAlias("zcl-regtest")
            .AddAlias("Zclassic-reg")
            .AddAlias("Zclassic-regtest")
            .SetGenesis("05ffd6ad016271ade20cfce093959c3addb2079629f9f123c52ef920caa316531af5af3f");
            // (This Genesis is for Equihash 48, 5)
            return builder;
        }
    }
}
