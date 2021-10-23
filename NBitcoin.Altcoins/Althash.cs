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
	// Reference: https://github.com/HTMLCOIN/HTMLCOIN/blob/master-2.5/src/chainparams.cpp
	public class Althash : NetworkSetBase
	{
		public static Althash Instance { get; } = new Althash();

		public override string CryptoCode => "HTML";

		private Althash()
		{
		}
		public class AlthashConsensusFactory : ConsensusFactory
		{
			private AlthashConsensusFactory()
			{
			}
			public static AlthashConsensusFactory Instance { get; } = new AlthashConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new AlthashBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new AlthashBlock(new AlthashBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class AuxPow : IBitcoinSerializable
		{
			uint256 hashStateRoot = uint256.Zero;

			public uint256 HashStateRoot
			{
				get
				{
					return hashStateRoot;
				}
				set
				{
					hashStateRoot = value;
				}
			}

			uint256 hashUtxoRoot = uint256.Zero;

			public uint256 HashUtxoRoot
			{
				get
				{
					return hashUtxoRoot;
				}
				set
				{
					hashUtxoRoot = value;
				}
			}

			OutPoint prevoutStake = new OutPoint(uint256.Zero, uint.MaxValue);

			public OutPoint PrevoutStake
			{
				get
				{
					return prevoutStake;
				}
				set
				{
					prevoutStake = value;
				}
			}

			byte[] blockSignature = null;

			public byte[] BlockSignature
			{
				get
				{
					return blockSignature;
				}
				set
				{
					blockSignature = value;
				}
			}

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref hashStateRoot);
				stream.ReadWrite(ref hashUtxoRoot);
				stream.ReadWrite(ref prevoutStake);
				stream.ReadWriteAsVarString(ref blockSignature);
			}
		}

		public class AlthashBlock : Block
		{
			public AlthashBlock(AlthashBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return AlthashConsensusFactory.Instance;
			}
		}
		public class AlthashBlockHeader : BlockHeader
		{
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

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				stream.ReadWrite(ref auxPow);
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		//static Tuple<byte[], int>[] pnSeed6_main = null;
		//static Tuple<byte[], int>[] pnSeed6_test = null;

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 7680000,
				MajorityEnforceBlockUpgrade = 15120,
				MajorityRejectBlockOutdated = 15120,
				MajorityWindow = 15120,
				PowLimit = new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(120),
				PowTargetSpacing = TimeSpan.FromSeconds(120),
				PowAllowMinDifficultyBlocks = false,
				// Reference: https://github.com/HTMLCOIN/HTMLCOIN/blob/c17d801571080d0721c8eddb394f919eab5d60f6/src/consensus/consensus.h#L27
				CoinbaseMaturity = 500,
				PowNoRetargeting = false,
				ConsensusFactory = AlthashConsensusFactory.Instance,
				LitecoinWorkCalculation = false,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 41 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 100 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 169 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x13, 0x97, 0xC1, 0x0D })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x13, 0x97, 0xBC, 0xF3 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("hc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("hc"))
			.SetMagic(0x4c3d2e1f)
			.SetPort(4888)
			.SetRPCPort(4889)
			.SetMaxP2PVersion(70018)
			.SetName("althash-main")
			.AddAlias("althash-mainnet")
			.SetUriScheme("htmlcoin")
			.AddDNSSeeds(new[]{
				new DNSSeedData("seed1.htmlcoin.com", "seed2.htmlcoin.com"),
			})
			.AddSeeds(new NetworkAddress[0])

			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000681f98dd984a3a860269b17d5dba8de354c619b34f073cc2bf1e6f7e97607bb080f5c659ffff001fa3700100e965ffd002cd6ad0e2dc402b8044de833e06b23127ea8c3d80aec9141077149556e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b4210000000000000000000000000000000000000000000000000000000000000000ffffffff000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3f0004bf91221d01043642424320392f32342f32303137204765726d616e7920656c656374696f6e204d65726b656c2077696e7320666f75727468207465726dffffffff0100e1f50500000000434104e67225ab32299deaf6312b5b77f0cd2a5264f3757c9663f8dc401ff8b3ad8b012fde713be690ab819f977f84eaef078767168aeb1cb1287941b6319b76d8e582ac00000000");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 7680000,
				MajorityEnforceBlockUpgrade = 1512,
				MajorityRejectBlockOutdated = 1512,
				MajorityWindow = 1512,
				PowLimit = new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(10),
				PowTargetSpacing = TimeSpan.FromSeconds(10),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 500,
				PowNoRetargeting = false,
				ConsensusFactory = AlthashConsensusFactory.Instance,
				LitecoinWorkCalculation = false,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 100 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 110 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tq"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tq"))
			.SetMagic(0x5c4d3e2f)
			.SetPort(14888)
			.SetRPCPort(14889)
			.SetMaxP2PVersion(70018)
			.SetName("althash-test")
			.AddAlias("althash-testnet")
			.SetUriScheme("htmlcoin")
			.AddDNSSeeds(new[]{
				new DNSSeedData("testnet-seed1.htmlcoin.com", "testnet-seed1.htmlcoin.com"),
			})
			.AddSeeds(new NetworkAddress[0])
			// Incorrect, using mainnet for now

			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000681f98dd984a3a860269b17d5dba8de354c619b34f073cc2bf1e6f7e97607bb080f5c659ffff001fa3700100e965ffd002cd6ad0e2dc402b8044de833e06b23127ea8c3d80aec9141077149556e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b4210000000000000000000000000000000000000000000000000000000000000000ffffffff000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3f0004bf91221d01043642424320392f32342f32303137204765726d616e7920656c656374696f6e204d65726b656c2077696e7320666f75727468207465726dffffffff0100e1f50500000000434104e67225ab32299deaf6312b5b77f0cd2a5264f3757c9663f8dc401ff8b3ad8b012fde713be690ab819f977f84eaef078767168aeb1cb1287941b6319b76d8e582ac00000000");

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 108,
				MajorityRejectBlockOutdated = 108,
				MajorityWindow = 108,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 500,
				PowNoRetargeting = false,
				ConsensusFactory = AlthashConsensusFactory.Instance,
				LitecoinWorkCalculation = false,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 120 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 110 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("qcrt"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("qcrt"))
			.SetMagic(0x6c5d4e3f)
			.SetPort(24888)
			.SetRPCPort(14889)
			.SetMaxP2PVersion(70018)
			.SetName("althash-reg")
			.AddAlias("althash-regtest")
			.SetUriScheme("htmlcoin")
			.AddSeeds(new NetworkAddress[0])
			// Incorrect, using mainnet for now

			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000681f98dd984a3a860269b17d5dba8de354c619b34f073cc2bf1e6f7e97607bb080f5c659ffff001fa3700100e965ffd002cd6ad0e2dc402b8044de833e06b23127ea8c3d80aec9141077149556e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b4210000000000000000000000000000000000000000000000000000000000000000ffffffff000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff3f0004bf91221d01043642424320392f32342f32303137204765726d616e7920656c656374696f6e204d65726b656c2077696e7320666f75727468207465726dffffffff0100e1f50500000000434104e67225ab32299deaf6312b5b77f0cd2a5264f3757c9663f8dc401ff8b3ad8b012fde713be690ab819f977f84eaef078767168aeb1cb1287941b6319b76d8e582ac00000000");

			return builder;
		}

		protected override void PostInit()
		{
			// Reference: https://github.com/HTMLCOIN/HTMLCOIN/blob/c17d801571080d0721c8eddb394f919eab5d60f6/src/util/system.cpp#L701
			RegisterDefaultCookiePath("HTMLCOIN");
		}

	}
}
