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
	public class Navcoin : NetworkSetBase
	{
		public static Navcoin Instance { get; } = new Navcoin();

		public override string CryptoCode => "NAV";

		private Navcoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x65,0xa4,0x48,0xc3}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6d,0x56,0x51,0xeb}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6e,0xaf,0xf2,0xb4}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6e,0x16,0xa7,0x23}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x76,0xb3,0xfb,0xd2}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x76,0x5c,0x0d,0x52}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x77,0x49,0xa5,0xa3}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x78,0x95,0x2e,0x0e}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x7c,0xbe,0xfc,0x10}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x7d,0xef,0xc5,0xde}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x7d,0xef,0x35,0xe8}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x80,0x8f,0x01,0xed}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x80,0xc7,0xc0,0x66}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x83,0x9b,0x7c,0x1d}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x89,0x93,0x87,0x10}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x8e,0xc4,0x8b,0xf6}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x92,0xb9,0xa5,0x18}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x95,0x1c,0x78,0xc3}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x97,0x1e,0x60,0xfa}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x9d,0xb6,0xd1,0xd9}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x9d,0xb6,0xfd,0xee}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xa7,0x63,0x54,0x6e}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xae,0xda,0x82,0x53}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xae,0xda,0x8a,0x51}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xae,0xda,0x0e,0x59}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xae,0x2d,0x51,0xd4}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xae,0x34,0xfb,0x0f}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb1,0x9e,0xc2,0x2f}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb4,0xbd,0x93,0x5f}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb5,0xe7,0x15,0xdf}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0x9a,0x6f,0x48}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0x9f,0x91,0xbd}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb9,0xd9,0xab,0x2b}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xba,0x12,0x7f,0xa3}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xba,0x12,0x22,0x17}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbb,0x3b,0xf8,0x35}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbc,0x3c,0x5c,0xef}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbe,0xc3,0x65,0x33}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xbf,0xfa,0xd9,0x39}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc3,0xc9,0x8e,0xfd}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x02,0x7a,0x7a,0xf4}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x02,0x1e,0xa1,0x3b}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x02,0x57,0xb4,0x88}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xc8,0x74,0x1f,0x7f}, 44440),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xcc,0x62,0x77,0xfa}, 44440)
	    };

		static Tuple<byte[], int>[] pnSeed6_test = {
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2e,0x04,0x18,0x88}, 15556),
	      Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0xb0,0x09,0x13,0xf5}, 15556),
	    };

#pragma warning disable CS0618 // Type or member is obsolete
		public class NavcoinConsensusFactory : ConsensusFactory
		{
			private NavcoinConsensusFactory()
			{
			}

			public static NavcoinConsensusFactory Instance { get; } = new NavcoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new NavcoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new NavcoinBlock(new NavcoinBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class AuxPow : IBitcoinSerializable
		{
			Transaction tx = new Transaction();

			public Transaction Transactions
			{
				get
				{
					return tx;
				}
				set
				{
					tx = value;
				}
			}

			uint nIndex = 0;

			public uint Index
			{
				get
				{
					return nIndex;
				}
				set
				{
					nIndex = value;
				}
			}

			uint256 hashBlock = new uint256();

			public uint256 HashBlock
			{
				get
				{
					return hashBlock;
				}
				set
				{
					hashBlock = value;
				}
			}

			List<uint256> vMerkelBranch = new List<uint256>();

			public List<uint256> MerkelBranch
			{
				get
				{
					return vMerkelBranch;
				}
				set
				{
					vMerkelBranch = value;
				}
			}

			List<uint256> vChainMerkleBranch = new List<uint256>();

			public List<uint256> ChainMerkleBranch
			{
				get
				{
					return vChainMerkleBranch;
				}
				set
				{
					vChainMerkleBranch = value;
				}
			}

			uint nChainIndex = 0;

			public uint ChainIndex
			{
				get
				{
					return nChainIndex;
				}
				set
				{
					nChainIndex = value;
				}
			}

			BlockHeader parentBlock = new BlockHeader();

			public BlockHeader ParentBlock
			{
				get
				{
					return parentBlock;
				}
				set
				{
					parentBlock = value;
				}
			}

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref tx);
				stream.ReadWrite(ref hashBlock);
				stream.ReadWrite(ref vMerkelBranch);
				stream.ReadWrite(ref nIndex);
				stream.ReadWrite(ref vChainMerkleBranch);
				stream.ReadWrite(ref nChainIndex);
				stream.ReadWrite(ref parentBlock);
			}
		}

		public class NavcoinBlock : Block
		{
			public NavcoinBlock(NavcoinBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return NavcoinConsensusFactory.Instance;
			}
		}
		public class NavcoinBlockHeader : BlockHeader
		{
			const int VERSION_AUXPOW = (1 << 8);

			AuxPow auxPow = new AuxPow();

			public AuxPow AuxPow
			{
				get
				{
					return auxPow;
				}
				set
				{
					auxPow = value;
				}
			}

			public override uint256 GetPoWHash()
			{
				var headerBytes = this.ToBytes();
				var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
				return new uint256(h);
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				if((Version & VERSION_AUXPOW) != 0)
				{
					if(!stream.Serializing)
					{
						stream.ReadWrite(ref auxPow);
					}
				}
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		//static Tuple<byte[], int>[] pnSeed6_main = null;
		//static Tuple<byte[], int>[] pnSeed6_test = null;		


		static uint256 GetPoWHash(BlockHeader header)
		{
			var headerBytes = header.ToBytes();
			var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
			return new uint256(h);
		}

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Navcoin");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0xecb7444214d068028ec1fa4561662433452c1cbbd6b0f8eeb6452bcfa1d0a7d6"),
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(30),
				PowTargetSpacing = TimeSpan.FromSeconds(30),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 15120, // 75% of 20160
				MinerConfirmationWindow = 20160,
				CoinbaseMaturity = 30,
				LitecoinWorkCalculation = true,
				ConsensusFactory = NavcoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 53 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 85 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 150 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("nav"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("nav"))
			.SetMagic(0x20345080)
			.SetPort(44440)
			.SetRPCPort(44444)
			.SetName("nav-main")
			.AddAlias("nav-mainnet")
			.AddAlias("navcoin-mainnet")
			.AddAlias("navcoin-main")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("nav.community", "seed.nav.community"),
				new DNSSeedData("navcoin.org", "seed.navcoin.org"),
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000001ac692738a35b20c57608a4b5946d9d242baafceaf64d73254fdabccc6ee07c590640e57ffff001f211b0000010100000090640e57010000000000000000000000000000000000000000000000000000000000000000ffffffff1200012a0e47616d652069732061666f6f7421ffffffff010000000000000000000000000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00001fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(30),
				PowTargetSpacing = TimeSpan.FromSeconds(30),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 300, // 75% of 400+
				MinerConfirmationWindow = 400,
				CoinbaseMaturity = 30,
				LitecoinWorkCalculation = true,
				ConsensusFactory = NavcoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x40, 0x88, 0x2B, 0xE1 })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x40, 0x88, 0xDA, 0x4E })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tnav"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tnav"))
			.SetMagic(0x2252a43f)
			.SetPort(15556)
			.SetRPCPort(44445)
			.SetName("nav-test")
			.AddAlias("nav-testnet")
			.AddAlias("navcoin-test")
			.AddAlias("navcoin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("nav.community", "seed.nav.community"),
				new DNSSeedData("navcoin.org", "seed.navcoin.org"),
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000001ac692738a35b20c57608a4b5946d9d242baafceaf64d73254fdabccc6ee07c590640e57ffff001f211b0000010100000090640e57010000000000000000000000000000000000000000000000000000000000000000ffffffff1200012a0e47616d652069732061666f6f7421ffffffff010000000000000000000000000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(30),
				PowTargetSpacing = TimeSpan.FromSeconds(30),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 300, // 75% of 400+
				MinerConfirmationWindow = 400,
				CoinbaseMaturity = 30,
				LitecoinWorkCalculation = true,
				ConsensusFactory = NavcoinConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x40, 0x88, 0x2B, 0xE1 })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x40, 0x88, 0xDA, 0x4E })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tnav"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tnav"))
			.SetMagic(0x89b7117d)
			.SetPort(18886)
			.SetRPCPort(44446)
			.SetName("nav-reg")
			.AddAlias("nav-regtest")
			.AddAlias("navcoin-reg")
			.AddAlias("navcoin-regtest")
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000001ac692738a35b20c57608a4b5946d9d242baafceaf64d73254fdabccc6ee07c590640e57ffff001f211b0000010100000090640e57010000000000000000000000000000000000000000000000000000000000000000ffffffff1200012a0e47616d652069732061666f6f7421ffffffff010000000000000000000000000000");
			return builder;
		}
	}
}
