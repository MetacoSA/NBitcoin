using System;
using System.IO;
using System.Runtime.InteropServices;
using NBitcoin.Crypto;
using NBitcoin.RPC;

namespace NBitcoin.Altcoins
{
	public partial class Obtc : NetworkSetBase
	{
        public static Obtc Instance { get; } = new Obtc();
		public override string CryptoCode => "OBTC";

        private Obtc()
        {

        }

		public class ObtcConsensusFactory : ConsensusFactory
		{
			private ObtcConsensusFactory()
			{
			}

			public static ObtcConsensusFactory Instance { get; } = new ObtcConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
#pragma warning disable CS0618 // Type or member is obsolete
				return new ObtcBlockHeader();
#pragma warning restore CS0618 // Type or member is obsolete
			}
			public override Block CreateBlock()
			{
#pragma warning disable CS0618 // Type or member is obsolete
				return new ObtcBlock((ObtcBlockHeader) CreateBlockHeader());
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		[Obsolete("You should instantiate ObtcBlockHeader from ObtcConsensusFactory.CreateBlockHeader")]
		public class ObtcBlockHeader : BlockHeader
		{
			protected override HashStreamBase CreateHashStream()
			{
				return new HeavyHashStream(this);
			}
		}

		public class ObtcBlock : Block
		{
			[Obsolete("Should use ObtcConsensusFactory")]
			public ObtcBlock(ObtcBlockHeader header) : base(header)
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return ObtcConsensusFactory.Instance;
			}
		}

		protected override NetworkBuilder CreateMainnet()
		{
			return new NetworkBuilder()
			.SetNetworkSet(this)
            .SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 420000,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 144,
				BIP34Hash = new uint256("0000000000115c7a7e3ff65d77ee96de527953ca6e43e77246929741408f95c0"),
				PowLimit = new Target(new uint256("0000000000ffff00000000000000000000000000000000000000000000000000")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = false,
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000001000011"),
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				ConsensusFactory = ObtcConsensusFactory.Instance,
				SupportSegwit = true
            }
            )
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "bc")
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "bc")
			.SetBech32(Bech32Type.TAPROOT_ADDRESS, "bc")
			.SetPort(9901)
			.SetRPCPort(9898)
            .SetMagic(0xEEBBAFFA)
			.SetName("obtc-main")
            .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000f5febbad19864a6900b6ce84287511e6e746229ce6239ff3df7481654778a4c4d3e15d60ffff001c0747d0420101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440536369656e63655820342f4d61792f3230323020506f776572696e6720646f776e20426974636f696e20776974682073696c69636f6e2070686f746f6e696373ffffffff0100f2052a01000000434104c4f0e6a028395d7c557c712216f043cebfb774cb3136d6728f16429d6ab7cbc82e75de8ea5f5be54ef960e9747ca9a5ec565b14b929898526468838ea9579420ac00000000");

		}

		protected override NetworkBuilder CreateRegtest()
		{
            return new NetworkBuilder()
			.SetNetworkSet(this)
            .SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 144,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 100,
				ConsensusFactory = ObtcConsensusFactory.Instance,
				SupportSegwit = true
            }
            )
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "bcrt")
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "bcrt")
			.SetBech32(Bech32Type.TAPROOT_ADDRESS, "bcrt")
			.SetPort(18444)
			.SetRPCPort(19998)
            .SetMagic(0xDFFDDCCD)
			.SetName("obtc-regtest")
			.AddAlias("obtc-reg")
			.AddAlias("optical-bitcoin-regtest")
            .SetGenesis("000000200000000000000000000000000000000000000000000000000000000000000000f5febbad19864a6900b6ce84287511e6e746229ce6239ff3df7481654778a4c4fb162c5fffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440536369656e63655820342f4d61792f3230323020506f776572696e6720646f776e20426974636f696e20776974682073696c69636f6e2070686f746f6e696373ffffffff0100f2052a01000000434104c4f0e6a028395d7c557c712216f043cebfb774cb3136d6728f16429d6ab7cbc82e75de8ea5f5be54ef960e9747ca9a5ec565b14b929898526468838ea9579420ac00000000");
		}

		protected override NetworkBuilder CreateTestnet()
		{
			return new NetworkBuilder()
			.SetNetworkSet(this)
            .SetConsensus(new Consensus()
            {
                SubsidyHalvingInterval = 420000,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 144,
				PowLimit = new Target(new uint256("000000003fff0000000000000000000000000000000000000000000000000000")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				ConsensusFactory = ObtcConsensusFactory.Instance,
				SupportSegwit = true
            }
            )
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "tb")
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "tb")
			.SetBech32(Bech32Type.TAPROOT_ADDRESS, "tb")
			.SetPort(19899)
			.SetRPCPort(19898)
            .SetMagic(0xFEEFBAAB)
			.SetName("obtc-test")
            .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000f5febbad19864a6900b6ce84287511e6e746229ce6239ff3df7481654778a4c4e5f0bf60ff3f001d1bcb807b0101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440536369656e63655820342f4d61792f3230323020506f776572696e6720646f776e20426974636f696e20776974682073696c69636f6e2070686f746f6e696373ffffffff0100f2052a01000000434104c4f0e6a028395d7c557c712216f043cebfb774cb3136d6728f16429d6ab7cbc82e75de8ea5f5be54ef960e9747ca9a5ec565b14b929898526468838ea9579420ac00000000");
		}

		protected override void PostInit()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				RegisterOsxDefaultCookiePath();
			} 
			else 
			{
				RegisterDefaultCookiePath("Obtc");
			}
		}

		private void RegisterOsxDefaultCookiePath() 
		{
			var home = Environment.GetEnvironmentVariable("HOME");
			var obtcDataFolder = "Library/Application Support/Obtc";
			if (Mainnet != null)
			{
				var mainnet = Path.Combine(home, obtcDataFolder, ".cookie");
				RPCClient.RegisterDefaultCookiePath(Mainnet, mainnet);
			}

			if (Testnet != null)
			{
				var testnet = Path.Combine(home, obtcDataFolder, "testnet3", ".cookie");
				RPCClient.RegisterDefaultCookiePath(Testnet, testnet);
			}

			if (Regtest != null)
			{
				var regtest = Path.Combine(home, obtcDataFolder, "regtest", ".cookie");
				RPCClient.RegisterDefaultCookiePath(Regtest, regtest);
			}
		}
	}
}