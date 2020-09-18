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
using System.Threading.Tasks;
 namespace NBitcoin.Altcoins
{
	public class GoByte : NetworkSetBase
	{
		public static GoByte Instance { get; } = new GoByte();
 		public override string CryptoCode => "GBX";
 		private GoByte()
		{
 		}
		public class GoByteConsensusFactory : ConsensusFactory
		{
			private GoByteConsensusFactory()
			{
			}
 			public static GoByteConsensusFactory Instance { get; } = new GoByteConsensusFactory();
 			public override BlockHeader CreateBlockHeader()
			{
				return new GoByteBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new GoByteBlock(new GoByteBlockHeader());
			}
		}
 #pragma warning disable CS0618 // Type or member is obsolete
		public class GoByteBlockHeader : BlockHeader
		{
			
			static byte[] CalculateHash(byte[] data, int offset, int count)
			{
				// TODO: change the hash algorithm
				return new HashX11.X11().ComputeBytes(data.Skip(offset).Take(count).ToArray());
			}
			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash, 80);
			}
		}
 		public class GoByteBlock : Block
		{
			public GoByteBlock(GoByteBlockHeader h) : base(h)
			{
 			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return GoByteConsensusFactory.Instance;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete
 		protected override void PostInit()
		{
			RegisterDefaultCookiePath("GoByteCore");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210240, // one year
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x00000c8a1ff01bae3f3875c81cb14115429af5744643b34b4ad1cbb7d2d59ca2"),
				PowLimit = new Target(new uint256("00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("00000fffff000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916, // 95% of 2016
				MinerConfirmationWindow = 2016,
				ConsensusFactory = GoByteConsensusFactory.Instance,
				SupportSegwit = false
			}) // done
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 38 }) 
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 10 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 198 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("gobyte"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("gobyte"))
			.SetMagic(0xD4C3B21A) 
			.SetPort(12455) 
			.SetRPCPort(12454)
			.SetMaxP2PVersion(70209)
			.SetName("gobyte-main")
			.AddAlias("gobyte-mainnet")
			.AddDNSSeeds(new[]
			{
				new  DNSSeedData("seed1.gobyte.network", "seed1.gobyte.network"),
				new  DNSSeedData("seed2.gobyte.network", "seed2.gobyte.network"),
				new  DNSSeedData("seed3.gobyte.network", "seed3.gobyte.network"),
				new  DNSSeedData("seed4.gobyte.network", "seed4.gobyte.network"),
				new  DNSSeedData("seed5.gobyte.network", "seed5.gobyte.network"),
				new  DNSSeedData("seed6.gobyte.network", "seed6.gobyte.network"),
				new  DNSSeedData("seed7.gobyte.network", "seed7.gobyte.network"),
				new  DNSSeedData("seed8.gobyte.network", "seed8.gobyte.network"),
				new  DNSSeedData("seed9.gobyte.network", "seed9.gobyte.network"),
				new  DNSSeedData("seed10.gobyte.network", "seed10.gobyte.network")
			}) // done
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000219f39f283f43185a0ef69cba1702151ab0cf02454a57e1039dabcc19d719adc00b60d5af0ff0f1e6fe618000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4204ffff001d01043a5468652053746172204d616c61797369612031377468204e6f76656d626572203230313720476f427974652047656e65736973205265626f726effffffff0100f2052a010000004341043e5a5fbfbb2caa5f4b7c8fd24d890d6c244de254d579b5ba629f64c1b48275f59e0e1c834a60f6ffb4aaa022aaa4866434ca729a12465f80618fb2070045cb16ac00000000");
			return builder;
		}
 		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210240,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x0000064a72bc327f2c93169784623348d8ad8975873563a2c3e0e1deb6bcc9f7"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000006a96dd9119d"),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512, 
				MinerConfirmationWindow = 2016, 
				ConsensusFactory = GoByteConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 112 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 20 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tgobyte"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tgobyte"))
			.SetMagic(0xFFCAE2CE)
			.SetPort(13455)
			.SetRPCPort(13454)
			.SetMaxP2PVersion(70209)
		   .SetName("gobyte-test")
		   .AddAlias("gobyte-testnet")
		   .AddDNSSeeds(new[]
		   {
			   new DNSSeedData("gobyte.network",  "testnet-dns.gobyte.network"),
			   new DNSSeedData("gobyte.network",  "testnet2-dns.gobyte.network")
		   })
		   .AddSeeds(new NetworkAddress[0])
		   .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000219f39f283f43185a0ef69cba1702151ab0cf02454a57e1039dabcc19d719adc20de0b5af0ff0f1ebbc02d000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4204ffff001d01043a5468652053746172204d616c61797369612031377468204e6f76656d626572203230313720476f427974652047656e65736973205265626f726effffffff0100f2052a010000004341043e5a5fbfbb2caa5f4b7c8fd24d890d6c244de254d579b5ba629f64c1b48275f59e0e1c834a60f6ffb4aaa022aaa4866434ca729a12465f80618fb2070045cb16ac00000000");
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
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000924e924a21715"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = GoByteConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 112 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 20 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 240 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tgobyte"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tgobyte"))
			.SetMagic(0x7BD5B3A1)
			.SetPort(13565)
			.SetRPCPort(13564)
			.SetMaxP2PVersion(70209) 
			.SetName("gobyte-reg")
			.AddAlias("gobyte-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000219f39f283f43185a0ef69cba1702151ab0cf02454a57e1039dabcc19d719adcbcdd0b5af0ff0f1e63c00d000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4204ffff001d01043a5468652053746172204d616c61797369612031377468204e6f76656d626572203230313720476f427974652047656e65736973205265626f726effffffff0100f2052a010000004341043e5a5fbfbb2caa5f4b7c8fd24d890d6c244de254d579b5ba629f64c1b48275f59e0e1c834a60f6ffb4aaa022aaa4866434ca729a12465f80618fb2070045cb16ac00000000");
			return builder;
		}
 	}
} 
