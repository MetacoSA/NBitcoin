using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/mogwaicoin/mogwai/blob/master/src/chainparams.cpp
	public class Mogwai : NetworkSetBase
	{
		public static Mogwai Instance { get; } = new Mogwai();

		public override string CryptoCode => "MOG";

		private Mogwai()
		{

		}

		public class MogwaiConsensusFactory : ConsensusFactory
		{
			private MogwaiConsensusFactory()
			{
			}

			public static MogwaiConsensusFactory Instance { get; } = new MogwaiConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new MogwaiBlockHeader();
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class MogwaiBlockHeader : BlockHeader
		{
			// https://github.com/mogwaicoin/mogwai/blob/master/src/primitives/block.cpp
			static byte[] CalculateHash(byte[] data, int offset, int count)
			{
				var output = new byte[32];
				// reference https://github.com/mogwaicoin/NeoScryptCSharp for correct neoscrypt native c# hashing
				// haven't added source because it will need to have unsafe code checked for the project.
				//NeoScrypt.NeoScrypt.neoscrypt(data, ref output, 0x0);
				return output;
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash);
			}

		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("MogwaiCore");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 365 * 720,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x000006ba48cbdecd71bc411a3e0b609f1acab9806fc652040f247c8b86831d06"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 101,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = MogwaiConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 50 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 16 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 204 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x03, 0xA3, 0xFD, 0xC2 })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x03, 0xA3, 0xF9, 0x89 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("mog"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("mog"))
			.SetMagic(0xCACA7091)
			.SetPort(17777)
			.SetRPCPort(17710)
			.SetMaxP2PVersion(70209)
			.SetName("mogwai-main")
			.AddAlias("mogwai-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("mogwaicoin.org", "dns-seed1.mogwaicoin.org"),
				new DNSSeedData("mogwaicoin.org", "dns-seed2.mogwaicoin.org"),
				new DNSSeedData("mogwaicoin.org", "dns-seed3.mogwaicoin.org"),
				new DNSSeedData("mogwaicoin.org", "dns-seed4.mogwaicoin.org")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000ced6de01d8d26ce7613669df2fc002d6f2138159744cf84d3c68d6245bb8989db0f62f5bf0ff0f1ea5060a000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404279652d6279652c20576f6f6620576f6f662e20576520617265206d6f67776169732e20457870656374207573206f6e204a756e652032362028323031382921ffffffff0100c08f312e0000004341047d476d8fec5e400a30657039003432293111167dc8357d1c66bcc64b7903f8eb9e4332cc073bda542e98a763d59e56e1c65563d0401a88a532d2eebed29da1b3ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			var res = builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 10 * 777,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x000008bdc1b40e9842b7dd84cd59ea24a4920a5e10d631f1fe0fe50e82250197"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(10 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1.85 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 101,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = MogwaiConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 127 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tmog"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tmog"))
			.SetMagic(0xCBCB7092)
			.SetPort(17888)
			.SetRPCPort(17810)
			.SetMaxP2PVersion(70208)
		   .SetName("mogwai-test")
		   .AddAlias("mogwai-testnet")
		   .AddDNSSeeds(new[]
		   {
				new DNSSeedData("mogwaicoin.info",  "dns-seed-test1.mogwaicoin.info")
		   })
		   .AddSeeds(new NetworkAddress[0])
		   .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000ced6de01d8d26ce7613669df2fc002d6f2138159744cf84d3c68d6245bb8989d49f2565bf0ff0f1ee73410000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404279652d6279652c20576f6f6620576f6f662e20576520617265206d6f67776169732e20457870656374207573206f6e204a756e652032362028323031382921ffffffff0100c08f312e0000004341047d476d8fec5e400a30657039003432293111167dc8357d1c66bcc64b7903f8eb9e4332cc073bda542e98a763d59e56e1c65563d0401a88a532d2eebed29da1b3ac00000000");
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
				BIP34Hash = new uint256("0x00000e927f57650792f29e62bccde332a814e20de07a7e3ac1402e0a886b2200"),
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 101,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = MogwaiConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 110 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tmog"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tmog"))
			.SetMagic(0xCCCC7093)
			.SetPort(17999)
			.SetRPCPort(17910)
			.SetMaxP2PVersion(70209)
			.SetName("mogwai-reg")
			.AddAlias("mogwai-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000ced6de01d8d26ce7613669df2fc002d6f2138159744cf84d3c68d6245bb8989d4392605bf0ff0f1e5aae08000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d0104404279652d6279652c20576f6f6620576f6f662e20576520617265206d6f67776169732e20457870656374207573206f6e204a756e652032362028323031382921ffffffff0100c08f312e0000004341047d476d8fec5e400a30657039003432293111167dc8357d1c66bcc64b7903f8eb9e4332cc073bda542e98a763d59e56e1c65563d0401a88a532d2eebed29da1b3ac00000000");
			return builder;
		}
	}
}
