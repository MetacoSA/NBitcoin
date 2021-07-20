using NBitcoin.DataEncoders;
using System;
using System.Reflection;
using System.Text;

namespace NBitcoin.Altcoins
{
	public class Triptourcoin : NetworkSetBase
	{
		public static Triptourcoin Instance { get; } = new Triptourcoin();

		public override string CryptoCode => "TTC";

		private Triptourcoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = { };
		static Tuple<byte[], int>[] pnSeed6_test = { };

#pragma warning disable CS0618 // Type or member is obsolete
		public class TriptourcoinConsensusFactory : ConsensusFactory
		{
			private TriptourcoinConsensusFactory()
			{
			}

			public static TriptourcoinConsensusFactory Instance { get; } = new TriptourcoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new TriptourcoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new TriptourcoinBlock(new TriptourcoinBlockHeader());
			}
		}

		public class TriptourcoinBlockHeader : BlockHeader
		{
			public override uint256 GetPoWHash()
			{
				var headerBytes = this.ToBytes();
				var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
				return new uint256(h);
			}
		}

		public class TriptourcoinBlock : Block
		{
			public TriptourcoinBlock(TriptourcoinBlockHeader header) : base(header)
			{

			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return TriptourcoinConsensusFactory.Instance;
			}
		}

		public class TriptourcoinMainnetAddressStringParser : NetworkStringParser
		{
			public override bool TryParse(string str, Network network, Type targetType, out IBitcoinString result)
			{
				if (str.StartsWith("Ltpv", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtKey).GetTypeInfo()))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x88;
						decoded[2] = 0xAD;
						decoded[3] = 0xE4;
						result = new BitcoinExtKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				if (str.StartsWith("Ltub", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtPubKey).GetTypeInfo()))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x88;
						decoded[2] = 0xB2;
						decoded[3] = 0x1E;
						result = new BitcoinExtPubKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				return base.TryParse(str, network, targetType, out result);
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Triptourcoin", new FolderName() { TestnetFolder = "testnet4" });
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var pchMessageStart = new byte[4];
			pchMessageStart[0] = 0x28;
			pchMessageStart[1] = 0x23;
			pchMessageStart[2] = 0x01;
			pchMessageStart[3] = 0x1c;
			var magic = BitConverter.ToUInt32(pchMessageStart, 0); //0xfabfb5da; 
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256(),
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(10 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(5 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 6048,
				MinerConfirmationWindow = 8064,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = false,
				ConsensusFactory = TriptourcoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 66 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 65 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 193 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetNetworkStringParser(new TriptourcoinMainnetAddressStringParser())
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("TTC"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("TTC"))
			.SetMagic(magic)
			.SetPort(37320)
			.SetRPCPort(37319)
			.SetName("TTC-main")
			.AddAlias("TTC-mainnet")
			.AddAlias("Triptourcoin-mainnet")
			.AddAlias("Triptourcoin-main")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("node1.triptourcoin.com", "node1.triptourcoin.com"),
				new DNSSeedData("node2.walletbuilders.com", "node2.walletbuilders.com")
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000004e29a1fe826c04feebef3cdd334ab075eea218096531e803ec87aeba628ec3cf36fc460f0ff0f1e34b205000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2a04ffff001d01042252657075626c69636120446f6d696e6963616e61206c6f207469656e6520746f646fffffffff0100f2052a01000000434104995610d100bab4548e7a97a9877c47334141fe9ed28db94338583b312591fff18aa8e87bc1d476c1a0950401c78ad3ee7adb5b457a2e08089a91bbf20007ef85ac000000002823011cf900000000000020eaa2dc11c7668716aaabe2262d622dca5477d58fa7abdc");


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
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = false,
				ConsensusFactory = TriptourcoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 58 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tTTC"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tTTC"))
			.SetMagic(0xf1c8d2fd)
			.SetPort(33719)
			.SetRPCPort(33720)
			.SetName("TTC-test")
			.AddAlias("TTC-testnet")
			.AddAlias("Triptourcoin-test")
			.AddAlias("Triptourcoin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("Triptourcointools.com", "testnet-seed.Triptourcointools.com"),
				new DNSSeedData("loshan.co.uk", "seed-b.Triptourcoin.loshan.co.uk"),
				new DNSSeedData("thrasher.io", "dnsseed-testnet.thrasher.io"),
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
				MajorityWindow = 144,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = false,
				ConsensusFactory = TriptourcoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 58 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("rTTC"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("rTTC"))
			.SetMagic(0xdab5bffa)
			.SetPort(18443)
			.SetRPCPort(18444)
			.SetName("TTC-reg")
			.AddAlias("TTC-regtest")
			.AddAlias("Triptourcoin-reg")
			.AddAlias("Triptourcoin-regtest")
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000d9ced4ed1130f7b7faad9be25323ffafa33232a17c3edf6cfd97bee6bafbdd97dae5494dffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404e592054696d65732030352f4f63742f32303131205374657665204a6f62732c204170706c65e280997320566973696f6e6172792c2044696573206174203536ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}

	}
}
