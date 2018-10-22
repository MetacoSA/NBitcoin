using NBitcoin.DataEncoders;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NBitcoin.Altcoins.GroestlcoinInternals;
using NBitcoin.Protocol;
using NBitcoin.Crypto;

namespace NBitcoin.Altcoins
{
	public class Groestlcoin : NetworkSetBase
	{
		public class GroestlEncoder : Base58CheckEncoder
		{

			private static readonly GroestlEncoder _Instance = new GroestlEncoder();
			public static GroestlEncoder Instance
			{
				get
				{
					return _Instance;
				}
			}
			private GroestlEncoder()
			{

			}
			protected override byte[] CalculateHash(byte[] bytes, int offset, int length)
			{
				return GroestlHash(bytes, offset, length);
			}
		}

		public static Groestlcoin Instance { get; } = new Groestlcoin();

		public static byte[] GroestlHash(byte[] arr)
		{
			return GroestlHash(arr, 0, arr.Length);
		}

		public static byte[] GroestlHash(byte[] arr, int offset, int length)
		{
			var digest = new Groestl512();
			digest.update(arr, offset, length);
			var h1 = digest.digest();
			digest.reset();
			digest.update(h1, 0, h1.Length);
			return digest.digest();
		}

		public override string CryptoCode => "GRS";

		private Groestlcoin()
		{
		}

		class GroestlcoinConsensusFactory : ConsensusFactory
		{

			private static readonly GroestlcoinConsensusFactory _Instance = new GroestlcoinConsensusFactory();
			public static GroestlcoinConsensusFactory Instance
			{
				get
				{
					return _Instance;
				}
			}
			class GroestlcoinProtocolCapabilities : ProtocolCapabilities
			{

				private static readonly GroestlcoinProtocolCapabilities _Instance = new GroestlcoinProtocolCapabilities();
				public static GroestlcoinProtocolCapabilities Instance
				{
					get
					{
						return _Instance;
					}
				}
				public GroestlcoinProtocolCapabilities()
				{
					PeerTooOld = false;
					SupportCheckSum = true;
					SupportCompactBlocks = true;
					SupportGetBlock = true;
					SupportMempoolQuery = true;
					SupportNodeBloom = true;
					SupportPingPong = true;
					SupportReject = true;
					SupportSendHeaders = true;
					SupportTimeAddress = true;
					SupportUserAgent = true;
					SupportWitness = true;
				}

				public override HashStreamBase GetChecksumHashStream(int hintSize)
				{
					return BufferedHashStream.CreateFrom(GroestlHash, hintSize);
				}
				public override HashStreamBase GetChecksumHashStream()
				{
					return BufferedHashStream.CreateFrom(GroestlHash, 300);
				}
			}

			public override BlockHeader CreateBlockHeader()
			{
				return new GroestlcoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new GroestlcoinBlock(new GroestlcoinBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new GroestlcoinTransaction();
			}

#pragma warning disable CS0618 // Type or member is obsolete

			public class GroestlcoinTransaction : Transaction
			{
				protected override HashStreamBase CreateHashStream()
				{
					return BufferedHashStream.CreateFrom(Hashes.SHA256, 300);
				}

				public override ConsensusFactory GetConsensusFactory()
				{
					return GroestlcoinConsensusFactory.Instance;
				}

				protected override HashStreamBase CreateSignatureHashStream()
				{
					return BufferedHashStream.CreateFrom(Hashes.SHA256, 150);
				}
			}

			public class GroestlcoinBlockHeader : BlockHeader
			{
				protected override HashStreamBase CreateHashStream()
				{
					return BufferedHashStream.CreateFrom(GroestlHash, 80);
				}
				public override uint256 GetPoWHash()
				{
					throw new NotSupportedException("PoW for Groestlcoin is not supported");
				}
			}

			public class GroestlcoinBlock : Block
			{
#pragma warning disable CS0612 // Type or member is obsolete
				public GroestlcoinBlock(GroestlcoinBlockHeader h) : base(h)
#pragma warning restore CS0612 // Type or member is obsolete
				{

				}
				public override ConsensusFactory GetConsensusFactory()
				{
					return Groestlcoin.Instance.Mainnet.Consensus.ConsensusFactory;
				}
			}
#pragma warning restore CS0618 // Type or member is obsolete


			public override ProtocolCapabilities GetProtocolCapabilities(uint protocolVersion)
			{
				return GroestlcoinProtocolCapabilities.Instance;
			}
		}

		class GroestlcoinStringParser : NetworkStringParser
		{

			private static readonly GroestlcoinStringParser _Instance = new GroestlcoinStringParser();
			public static GroestlcoinStringParser Instance
			{
				get
				{
					return _Instance;
				}
			}
			private GroestlcoinStringParser()
			{

			}

			public override Base58CheckEncoder GetBase58CheckEncoder()
			{
				return GroestlEncoder.Instance;
			}
		}


		static Tuple<byte[], int>[] pnSeed6_main = { };
		static Tuple<byte[], int>[] pnSeed6_test = { };

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 10080,
				BIP34Hash = new uint256("000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 120,
				LitecoinWorkCalculation = true,
				ConsensusFactory = GroestlcoinConsensusFactory.Instance
			})
			.SetNetworkStringParser(GroestlcoinStringParser.Instance)
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 36 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 5 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("grs"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("grs"))
			.SetMagic(0xd4b4bef9)
			.SetPort(1331)
			.SetRPCPort(1441)
			.SetName("groestl-main")
			.AddAlias("groestl-mainnet")
			.AddAlias("groestlcoin-mainnet")
			.AddAlias("groestlcoin-main")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("groestlcoin.org", "groestlcoin.org"),
				new DNSSeedData("electrum1.groestlcoin.org", "electrum1.groestlcoin.org"),
				new DNSSeedData("electrum2.groestlcoin.org", "electrum2.groestlcoin.org"),
				new DNSSeedData("jswallet.groestlcoin.org", "jswallet.groestlcoin.org"),
				new DNSSeedData("groestlsight.groestlcoin.org", "groestlsight.groestlcoin.org"),
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("700000000000000000000000000000000000000000000000000000000000000000000000bb2866aaca46c4428ad08b57bc9d1493abaf64724b6c3052a7c8f958df68e93ced3d2b53ffff0f1e835b03000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3a04ffff001d0104325072657373757265206d75737420626520707574206f6e20566c6164696d697220507574696e206f766572204372696d6561ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 1052100,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(1.1 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 120,
				LitecoinWorkCalculation = true,
				ConsensusFactory = GroestlcoinConsensusFactory.Instance
			})
			.SetNetworkStringParser(GroestlcoinStringParser.Instance)
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tgrs"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tgrs"))
			.SetMagic(0x0709110b)
			.SetPort(17777)
			.SetRPCPort(17766)
			.SetName("groestl-test")
			.AddAlias("groestl-testnet")
			.AddAlias("groestlcoin-test")
			.AddAlias("groestlcoin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("testnet1.groestlcoin.org", "testnet1.groestlcoin.org"),
				new DNSSeedData("testnet2.groestlcoin.org", "testnet2.groestlcoin.org"),
				new DNSSeedData("testp2pool.groestlcoin.org", "testp2pool.groestlcoin.org"),
				new DNSSeedData("testp2pool2.groestlcoin.org", "testp2pool2.groestlcoin.org"),
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("030000000000000000000000000000000000000000000000000000000000000000000000bb2866aaca46c4428ad08b57bc9d1493abaf64724b6c3052a7c8f958df68e93c02a8d455ffff001e950a64000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3a04ffff001d0104325072657373757265206d75737420626520707574206f6e20566c6164696d697220507574696e206f766572204372696d6561ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
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
				PowLimit = new Target(new uint256("00ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(1.1 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				CoinbaseMaturity = 120,
				LitecoinWorkCalculation = true,
				ConsensusFactory = GroestlcoinConsensusFactory.Instance
			})
			.SetNetworkStringParser(GroestlcoinStringParser.Instance)
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("grsrt"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("grsrt"))
			.SetMagic(0xdab5bffa)
			.SetPort(18888)
			.SetRPCPort(18443)
			.SetName("groestl-reg")
			.AddAlias("groestl-regtest")
			.AddAlias("groestlcoin-reg")
			.AddAlias("groestlcoin-regtest")
			.SetGenesis("030000000000000000000000000000000000000000000000000000000000000000000000bb2866aaca46c4428ad08b57bc9d1493abaf64724b6c3052a7c8f958df68e93c02a8d455ffff001e950a64000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3a04ffff001d0104325072657373757265206d75737420626520707574206f6e20566c6164696d697220507574696e206f766572204372696d6561ffffffff010000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Groestlcoin");
		}
	}
}
