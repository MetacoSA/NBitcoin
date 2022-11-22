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
	// Reference: https://github.com/qtumproject/qtum/blob/f925e854a84165f8302aa2772cc90faf8a98cb61/src/chainparams.cpp
	public class Qtum : NetworkSetBase
	{
		public static Qtum Instance { get; } = new Qtum();

		public override string CryptoCode => "QTUM";

		private Qtum()
		{

		}
		public class QtumConsensusFactory : ConsensusFactory
		{
			private QtumConsensusFactory()
			{
			}
			public static QtumConsensusFactory Instance { get; } = new QtumConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new QtumBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new QtumBlock(new QtumBlockHeader());
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

		public class QtumBlock : Block
		{
			public QtumBlock(QtumBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return QtumConsensusFactory.Instance;
			}
		}
		public class QtumBlockHeader : BlockHeader
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
				SubsidyHalvingInterval = 985500,
				MajorityEnforceBlockUpgrade = 1916,
				MajorityRejectBlockOutdated = 1916,
				MajorityWindow = 1916,
				PowLimit = new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(4000),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 64),
				PowAllowMinDifficultyBlocks = false,
				// Reference: https://github.com/qtumproject/qtum/blob/f925e854a84165f8302aa2772cc90faf8a98cb61/src/consensus/consensus.h#L27
				CoinbaseMaturity = 500,
				PowNoRetargeting = false,
				ConsensusFactory = QtumConsensusFactory.Instance,
				LitecoinWorkCalculation = false,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 58 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 50 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("qc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("qc"))
			.SetMagic(0xd3a6cff1)
			.SetPort(3888)
			.SetRPCPort(3889)
			.SetMaxP2PVersion(70020)
			.SetName("qtum-main")
			.AddAlias("qtum-mainnet")
			.AddDNSSeeds(new[]{
				new DNSSeedData("qtum3.dynu.net", "seed.qtum3.dynu.net"),
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000006db905142382324db417761891f2d2f355ea92f27ab0fc35e59e90b50e0534edf5d2af59ffff001ff9787a00e965ffd002cd6ad0e2dc402b8044de833e06b23127ea8c3d80aec9141077149556e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b4210000000000000000000000000000000000000000000000000000000000000000ffffffff000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff420004bf91221d0104395365702030322c203230313720426974636f696e20627265616b732024352c30303020696e206c6174657374207072696365206672656e7a79ffffffff0100f2052a010000004341040d61d8653448c98731ee5fffd303c15e71ec2057b77f11ab3601979728cdaff2d68afbba14e4fa0bc44f2072b0b23ef63717f8cdfbe58dcd33f32b6afe98741aac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 985500,
				MajorityEnforceBlockUpgrade = 1512,
				MajorityRejectBlockOutdated = 1512,
				MajorityWindow = 1512,
				PowLimit = new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(4000),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 64),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 500,
				PowNoRetargeting = false,
				ConsensusFactory = QtumConsensusFactory.Instance,
				LitecoinWorkCalculation = false,
				SupportSegwit = true
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 120 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 110 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tq"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tq"))
			.SetMagic(0x0615220d)
			.SetPort(13888)
			.SetRPCPort(13889)
			.SetMaxP2PVersion(70020)
			.SetName("qtum-test")
			.AddAlias("qtum-testnet")
			.AddDNSSeeds(new[]{
				new DNSSeedData("qtum4.dynu.net", "seed.qtum4.dynu.net"),
			})
			.AddSeeds(new NetworkAddress[0])
			// Incorrect, using mainnet for now
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000006db905142382324db417761891f2d2f355ea92f27ab0fc35e59e90b50e0534edf5d2af59ffff001ff9787a00e965ffd002cd6ad0e2dc402b8044de833e06b23127ea8c3d80aec9141077149556e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b4210000000000000000000000000000000000000000000000000000000000000000ffffffff000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff420004bf91221d0104395365702030322c203230313720426974636f696e20627265616b732024352c30303020696e206c6174657374207072696365206672656e7a79ffffffff0100f2052a010000004341040d61d8653448c98731ee5fffd303c15e71ec2057b77f11ab3601979728cdaff2d68afbba14e4fa0bc44f2072b0b23ef63717f8cdfbe58dcd33f32b6afe98741aac00000000");
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
				PowTargetTimespan = TimeSpan.FromSeconds(4000),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 64),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 500,
				PowNoRetargeting = false,
				ConsensusFactory = QtumConsensusFactory.Instance,
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
			.SetMagic(0xe1c6ddfd)
			.SetPort(23888)
			.SetRPCPort(13889)
			.SetMaxP2PVersion(70020)
			.SetName("qtum-reg")
			.AddAlias("qtum-regtest")
			.AddSeeds(new NetworkAddress[0])
			// Incorrect, using mainnet for now
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000006db905142382324db417761891f2d2f355ea92f27ab0fc35e59e90b50e0534edf5d2af59ffff7f2011000000e965ffd002cd6ad0e2dc402b8044de833e06b23127ea8c3d80aec9141077149556e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b4210000000000000000000000000000000000000000000000000000000000000000ffffffff000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff420004bf91221d0104395365702030322c203230313720426974636f696e20627265616b732024352c30303020696e206c6174657374207072696365206672656e7a79ffffffff0100f2052a010000004341040d61d8653448c98731ee5fffd303c15e71ec2057b77f11ab3601979728cdaff2d68afbba14e4fa0bc44f2072b0b23ef63717f8cdfbe58dcd33f32b6afe98741aac00000000");
			return builder;
		}

		protected override void PostInit()
		{
			// Reference: https://github.com/qtumproject/qtum/blob/342d769cf60ccfc46c0669507dcd154988d87d4f/src/util/system.cpp#L701
			RegisterDefaultCookiePath("Qtum");
		}

	}
}
