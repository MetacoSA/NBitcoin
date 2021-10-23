using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NBitcoin.Altcoins.HashX11;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	// https://github.com/chaincoin/chaincoin/blob/0.16/src/chainparams.cpp
	public class Chaincoin : NetworkSetBase
	{
		public static Chaincoin Instance { get; } = new Chaincoin();

		public override string CryptoCode => "CHC";

		private Chaincoin()
		{
		}

		public class ChaincoinConsensusFactory : ConsensusFactory
		{
			private ChaincoinConsensusFactory()
			{
			}

			public static ChaincoinConsensusFactory Instance { get; } = new ChaincoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new ChaincoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new ChaincoinBlock(new ChaincoinBlockHeader());
			}
		}


#pragma warning disable CS0618 // Type or member is obsolete
		public class ChaincoinBlockHeader : BlockHeader
		{
			// blob
			private static byte[] CalculateHash(byte[] data, int offset, int count)
			{
				return new HashX11.C11().ComputeBytes(data.Skip(offset).Take(count).ToArray());
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash);
			}
		}

		public class ChaincoinBlock : Block
		{
			public ChaincoinBlock(ChaincoinBlockHeader h) : base(h)
			{
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return Instance.Mainnet.Consensus.ConsensusFactory;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Chaincoin", new FolderName() { TestnetFolder = "testnet4" });
		}
		public class ChaincoinMainnetAddressStringParser : NetworkStringParser
		{
			public override bool TryParse(string str, Network network, Type targetType, out IBitcoinString result)
			{
				if (str.StartsWith("xprv", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtKey).GetTypeInfo()))
				{
					try
					{
						result = new BitcoinExtKey(str, network);
						return true;
					}
					catch
					{
					}
				}
				if (str.StartsWith("xpub", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtPubKey).GetTypeInfo()))
				{
					try
					{
						result = new BitcoinExtPubKey(str, network);
						return true;
					}
					catch
					{
					}
				}
				return base.TryParse(str, network, targetType, out result);
			}
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 700800,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x00000012f1c40ff12a9e6b0e9076fe4fa7ad27012e256a5ad7bcb80dc02c0409"),
				PowLimit = new Target(new uint256("0x00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x00000000000000000000000000000000000000000000000004b643d48e088b67"),
				PowTargetTimespan = TimeSpan.FromSeconds(90),
				PowTargetSpacing = TimeSpan.FromSeconds(90),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 10752,
				MinerConfirmationWindow = 13440,
				ConsensusFactory = ChaincoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 28 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 4 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 28 + 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("chc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("chc"))
			.SetNetworkStringParser(new ChaincoinMainnetAddressStringParser())
			.SetMagic(0x037AD2A3)
			.SetPort(11994)
			.SetRPCPort(11995)
			.SetMaxP2PVersion(70015)
			.SetName("chaincoin-main")
			.AddAlias("chaincoin-mainnet")
			.SetUriScheme("chaincoin")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("chc1.hashunlimited.com", "chc1.hashunlimited.com"),
				new DNSSeedData("chc2.hashunlimited.com", "chc2.hashunlimited.com"),
				new DNSSeedData("seed1.chaincoin.org", "seed1.chaincoin.org"),
				new DNSSeedData("seed2.chaincoin.org", "seed2.chaincoin.org"),
				new DNSSeedData("seed3.chaincoin.org", "seed3.chaincoin.org"),
				new DNSSeedData("seed4.chaincoin.org", "seed4.chaincoin.org")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000887c5c20f3075215e164877a6de732695a13c0f8ec0fcf6296fa942487f96efa0ce9da52ffff0f1e43cc217d0101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d01044531382d30312d3134202d20416e74692d667261636b696e672063616d706169676e65727320636861696e207468656d73656c76657320746f20706574726f6c2070756d7073ffffffff0100105e5f00000000434104becedf6ebadd4596964d890f677f8d2e74fdcc313c6416434384a66d6d8758d1c92de272dc6713e4a81d98841dfdfdc95e204ba915447d2fe9313435c78af3e8ac00000000");
			return builder;
		}
		
		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			var res = builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 56600,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x00000352de593a01e0efcbaec00345ec80d20c7bd2024ec7c2beec048af0e6d9"),
				PowLimit = new Target(new uint256("0x00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000000060e06d35d"),
				PowTargetTimespan = TimeSpan.FromSeconds(90),
				PowTargetSpacing = TimeSpan.FromSeconds(90),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 30,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = ChaincoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 80 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 44 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 88+128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tchc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tchc"))
			.SetMagic(0x0211C2FB)
			.SetPort(21994)
			.SetRPCPort(21995)
			.SetMaxP2PVersion(70015)
			.SetName("chaincoin-test")
			.AddAlias("chaincoin-testnet")
			.SetUriScheme("chaincoin")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("testseed.hashunlimited.com",  "testseed.hashunlimited.com")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000887c5c20f3075215e164877a6de732695a13c0f8ec0fcf6296fa942487f96efadae5494dffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d01044531382d30312d3134202d20416e74692d667261636b696e672063616d706169676e65727320636861696e207468656d73656c76657320746f20706574726f6c2070756d7073ffffffff0100105e5f00000000434104becedf6ebadd4596964d890f677f8d2e74fdcc313c6416434384a66d6d8758d1c92de272dc6713e4a81d98841dfdfdc95e204ba915447d2fe9313435c78af3e8ac00000000");
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
				BIP34Hash = new uint256(),
				PowLimit = new Target(new uint256("0x7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(90),
				PowTargetSpacing = TimeSpan.FromSeconds(90),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = ChaincoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("chcrt"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("chcrt"))
			.SetMagic(0x56C31FFC)
			.SetPort(18444)
			.SetRPCPort(18445)
			.SetMaxP2PVersion(70015)
			.SetName("chaincoin-reg")
			.AddAlias("chaincoin-regtest")
			.SetUriScheme("chaincoin")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000887c5c20f3075215e164877a6de732695a13c0f8ec0fcf6296fa942487f96efadae5494dffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d01044531382d30312d3134202d20416e74692d667261636b696e672063616d706169676e65727320636861696e207468656d73656c76657320746f20706574726f6c2070756d7073ffffffff0100105e5f00000000434104becedf6ebadd4596964d890f677f8d2e74fdcc313c6416434384a66d6d8758d1c92de272dc6713e4a81d98841dfdfdc95e204ba915447d2fe9313435c78af3e8ac00000000");
			return builder;
		}
	}
}
