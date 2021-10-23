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
	public class Viacoin : NetworkSetBase
	{
		public static Viacoin Instance { get; } = new Viacoin();

		public override string CryptoCode => "VIA";

		private Viacoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		static Tuple<byte[], int>[] pnSeed6_main = {
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x05,0x65,0x6a,0x35}, 5223),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x52,0xc4,0x0b,0xc9}, 5223),
	Tuple.Create(new byte[]{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x6b,0xaa,0xad,0x9d}, 5223),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x8c,0x14,0xde,0x2c,0x7d,0x6b,0xc0,0xa3,0xdc,0xf7}, 5223),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x9b,0x65,0xda,0xa4,0x3d,0xf5,0x2f,0x21,0x89,0xd5}, 5223),
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x0d,0xfd,0x43,0xe6,0xeb,0xbc,0xc3,0xe4,0xf4,0x02}, 5223)
};
		static Tuple<byte[], int>[] pnSeed6_test = {
	Tuple.Create(new byte[]{0xfd,0x87,0xd8,0x7e,0xeb,0x43,0x8c,0x95,0x7f,0x9b,0x49,0x18,0xd4,0xca,0x06,0x11}, 25223),
};

		public class ViacoinConsensusFactory : ConsensusFactory
		{
			private ViacoinConsensusFactory()
			{
			}

			public static ViacoinConsensusFactory Instance { get; } = new ViacoinConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new ViacoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new ViacoinBlock(new ViacoinBlockHeader());
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

		public class ViacoinBlock : Block
		{
			public ViacoinBlock(ViacoinBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return ViacoinConsensusFactory.Instance;
			}
		}
		public class ViacoinBlockHeader : BlockHeader
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

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Viacoin");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 657000,
				MajorityEnforceBlockUpgrade = 15000,
				MajorityRejectBlockOutdated = 19000,
				MajorityWindow = 20000,
				BIP34Hash = new uint256("4e9b54001f9976049830128ec0331515eaabe35a70970d79971da1539a400ba1"),
				PowLimit = new Target(new uint256("000001ffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 24),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 8100,
				MinerConfirmationWindow = 10800,
				CoinbaseMaturity = 30,
				LitecoinWorkCalculation = true,
				ConsensusFactory = ViacoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 71 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 33 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 199 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("via"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("via"))
			.SetMagic(0xcbc6680f)
			.SetPort(5223)
			.SetRPCPort(5222)
			.SetName("via-main")
			.AddAlias("via-mainnet")
			.AddAlias("viacoin-mainnet")
			.AddAlias("viacoin-main")
			.SetUriScheme("viacoin")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("viacoin.net", "seed.viacoin.net"),
				new DNSSeedData("barbatos.fr", "viaseeder.barbatos.fr"),
				new DNSSeedData("viacoin.net", "mainnet.viacoin.net"),
			})
			.AddSeeds(ToSeed(pnSeed6_main))
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000000522753002939c78659b4fdc6ed56c6b6aacdc7586facf2f6ada2012ed31703e61cc153ffff011ea1473d000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5704ffff001d01044c4e426c6f636b20233331303337393a30303030303030303030303030303030323431323532613762623237626539376265666539323138633132393064666633366331666631323965633732313161ffffffff01000000000000000043410459934a6a228ce9716fa0b13aa1cdc01593fca5f8599473c803a5109ff834dfdaf4c9ee35f2218c9ee3e7cf7db734e1179524b9d6ae8ebbeba883d4cb89b6c7bfac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 657000,
				MajorityEnforceBlockUpgrade = 510,
				MajorityRejectBlockOutdated = 750,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00001fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60), // 3.5 days
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 24),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 30,
				LitecoinWorkCalculation = true,
				ConsensusFactory = ViacoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 127 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 255 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tvia"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tvia"))
			.SetMagic(0x92efc5a9)
			.SetPort(25223)
			.SetRPCPort(25222)
			.SetName("via-test")
			.AddAlias("via-testnet")
			.AddAlias("viacoin-test")
			.AddAlias("viacoin-testnet")
			.SetUriScheme("viacoin")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("viacoin.net",  "testnet.viacoin.net"),
				new DNSSeedData("viacoin.net", "seed-testnet.viacoin.net")
			})
			.AddSeeds(ToSeed(pnSeed6_test))
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000000522753002939c78659b4fdc6ed56c6b6aacdc7586facf2f6ada2012ed31703842cc153ffff1f1e110304000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5704ffff001d01044c4e426c6f636b20233331303337393a30303030303030303030303030303030323431323532613762623237626539376265666539323138633132393064666633366331666631323965633732313161ffffffff01000000000000000043410459934a6a228ce9716fa0b13aa1cdc01593fca5f8599473c803a5109ff834dfdaf4c9ee35f2218c9ee3e7cf7db734e1179524b9d6ae8ebbeba883d4cb89b6c7bfac00000000");
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
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 24),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 2700,
				MinerConfirmationWindow = 3600,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,
				ConsensusFactory = ViacoinConsensusFactory.Instance,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tvia"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tvia"))
			.SetMagic(0x377b972d)
			.SetPort(15224)
			.SetRPCPort(15223)
			.SetName("via-reg")
			.AddAlias("via-regtest")
			.AddAlias("viacoin-reg")
			.AddAlias("viacoin-regtest")
			.SetUriScheme("viacoin")
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000000522753002939c78659b4fdc6ed56c6b6aacdc7586facf2f6ada2012ed31703d321c153ffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5704ffff001d01044c4e426c6f636b20233331303337393a30303030303030303030303030303030323431323532613762623237626539376265666539323138633132393064666633366331666631323965633732313161ffffffff01000000000000000043410459934a6a228ce9716fa0b13aa1cdc01593fca5f8599473c803a5109ff834dfdaf4c9ee35f2218c9ee3e7cf7db734e1179524b9d6ae8ebbeba883d4cb89b6c7bfac00000000");
			return builder;
		}
	}
}
