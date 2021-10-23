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
	// Reference: https://github.com/polispay/polis/blob/master/src/chainparams.cpp
	public class Polis : NetworkSetBase
	{
		public static Polis Instance { get; } = new Polis();

		public override string CryptoCode => "POLIS";

		private Polis()
		{

		}
		public class PolisConsensusFactory : ConsensusFactory
		{
			private PolisConsensusFactory()
			{
			}

			public static PolisConsensusFactory Instance { get; } = new PolisConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new PolisBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new PolisBlock(new PolisBlockHeader());
			}
			public override Transaction CreateTransaction()
			{
				return new PolisTransaction();
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class PolisBlockHeader : BlockHeader
		{
			static byte[] CalculateHash(byte[] data, int offset, int count)
			{
				return new HashX11.X11().ComputeBytes(data.Skip(offset).Take(count).ToArray());
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash, 80);
			}
		}

		/// <summary>
		/// Transactions with version >= 3 have a special transaction type in the version code
		/// https://docs.dash.org/en/stable/merchants/technical.html#v0-13-0-integration-notes
		/// 0.14 will add more types: https://github.com/dashpay/dips/blob/master/dip-0002-special-transactions.md
		/// </summary>
		public enum PolisTransactionType
		{
			StandardTransaction = 0,
			MasternodeRegistration = 1,
			UpdateMasternodeService = 2,
			UpdateMasternodeOperator = 3,
			MasternodeRevocation = 4,
			MasternodeListMerkleProof = 5,
			QuorumCommitment = 6
		}

		public abstract class SpecialTransaction
		{
			protected SpecialTransaction(byte[] extraPayload)
			{
				data = new BinaryReader(new MemoryStream(extraPayload));
				Version = data.ReadUInt16();
			}

			protected readonly BinaryReader data;
			/// <summary>
			/// Version number. Currently set to 1 for all DashTransactionTypes
			/// </summary>
			public ushort Version { get; set; }

			/// <summary>
			/// https://github.com/dashevo/dashcore-lib/blob/master/lib/constants/index.js
			/// </summary>
			public const int PUBKEY_ID_SIZE = 20;
			public const int COMPACT_SIGNATURE_SIZE = 65;
			public const int SHA256_HASH_SIZE = 32;
			public const int BLS_PUBLIC_KEY_SIZE = 48;
			public const int BLS_SIGNATURE_SIZE = 96;
			public const int IpAddressLength = 16;

			protected void MakeSureWeAreAtEndOfPayload()
			{
				if (data.BaseStream.Position < data.BaseStream.Length)
					throw new Exception(
						"Failed to parse payload: raw payload is bigger than expected (pos=" +
						data.BaseStream.Position + ", len=" + data.BaseStream.Length + ")");
			}
		}

		/// <summary>
		/// https://github.com/dashpay/dips/blob/master/dip-0003.md
		/// </summary>
		public class ProviderRegistrationTransaction : SpecialTransaction
		{
			public ProviderRegistrationTransaction(byte[] extraPayload) : base(extraPayload)
			{
				Type = data.ReadUInt16();
				Mode = data.ReadUInt16();
				CollateralHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				CollateralIndex = data.ReadUInt32();
				IpAddress = data.ReadBytes(IpAddressLength);
				Port = BitConverter.ToUInt16(data.ReadBytes(2).Reverse().ToArray(), 0);
				KeyIdOwner = new uint160(data.ReadBytes(PUBKEY_ID_SIZE), true);
				KeyIdOperator = data.ReadBytes(BLS_PUBLIC_KEY_SIZE);
				KeyIdVoting = new uint160(data.ReadBytes(PUBKEY_ID_SIZE), true);
				OperatorReward = data.ReadUInt16();
				var bs = new BitcoinStream(data.BaseStream, false);
				bs.ReadWriteAsVarInt(ref ScriptPayoutSize);
				ScriptPayout = new Script(data.ReadBytes((int)ScriptPayoutSize));
				InputsHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				bs.ReadWriteAsVarInt(ref PayloadSigSize);
				PayloadSig = data.ReadBytes((int)PayloadSigSize);
				MakeSureWeAreAtEndOfPayload();
			}

			public ushort Type { get; set; }
			public ushort Mode { get; set; }
			public uint256 CollateralHash { get; set; }
			public uint CollateralIndex { get; set; }
			public byte[] IpAddress { get; set; }
			public ushort Port { get; set; }
			public uint160 KeyIdOwner { get; set; }
			public byte[] KeyIdOperator { get; set; }
			public uint160 KeyIdVoting { get; set; }
			public ushort OperatorReward { get; set; }
			public uint ScriptPayoutSize;
			public Script ScriptPayout { get; set; }
			public uint256 InputsHash { get; set; }
			public uint PayloadSigSize;
			public byte[] PayloadSig { get; set; }
		}

		public class ProviderUpdateServiceTransaction : SpecialTransaction
		{
			public ProviderUpdateServiceTransaction(byte[] extraPayload) : base(extraPayload)
			{
				ProTXHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				IpAddress = data.ReadBytes(IpAddressLength);
				Port = BitConverter.ToUInt16(data.ReadBytes(2).Reverse().ToArray(), 0);
				var bs = new BitcoinStream(data.BaseStream, false);
				bs.ReadWriteAsVarInt(ref ScriptOperatorPayoutSize);
				ScriptOperatorPayout = new Script(data.ReadBytes((int)ScriptOperatorPayoutSize));
				InputsHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				PayloadSig = data.ReadBytes(BLS_SIGNATURE_SIZE);
				MakeSureWeAreAtEndOfPayload();
			}

			public uint256 ProTXHash { get; set; }
			public byte[] IpAddress { get; set; }
			public ushort Port { get; set; }
			public uint ScriptOperatorPayoutSize;
			public Script ScriptOperatorPayout { get; set; }
			public uint256 InputsHash { get; set; }
			public byte[] PayloadSig { get; set; }
		}

		public class ProviderUpdateRegistrarTransaction : SpecialTransaction
		{
			public ProviderUpdateRegistrarTransaction(byte[] extraPayload) : base(extraPayload)
			{
				ProTXHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				Mode = data.ReadUInt16();
				PubKeyOperator = data.ReadBytes(BLS_PUBLIC_KEY_SIZE);
				KeyIdVoting = new uint160(data.ReadBytes(PUBKEY_ID_SIZE), true);
				var bs = new BitcoinStream(data.BaseStream, false);
				bs.ReadWriteAsVarInt(ref ScriptPayoutSize);
				ScriptPayout = new Script(data.ReadBytes((int)ScriptPayoutSize));
				InputsHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				if (data.BaseStream.Position < data.BaseStream.Length)
				{
					bs.ReadWriteAsVarInt(ref PayloadSigSize);
					PayloadSig = data.ReadBytes((int)PayloadSigSize);
				}
				else
					PayloadSig = new byte[0];
				MakeSureWeAreAtEndOfPayload();
			}

			public uint256 ProTXHash { get; set; }
			public ushort Mode { get; set; }
			public byte[] PubKeyOperator { get; set; }
			public uint160 KeyIdVoting { get; set; }
			public uint ScriptPayoutSize;
			public Script ScriptPayout { get; set; }
			public uint256 InputsHash { get; set; }
			public uint PayloadSigSize;
			public byte[] PayloadSig { get; set; }
		}

		public class ProviderUpdateRevocationTransaction : SpecialTransaction
		{
			public ProviderUpdateRevocationTransaction(byte[] extraPayload) : base(extraPayload)
			{
				ProTXHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				Reason = data.ReadUInt16();
				InputsHash = new uint256(data.ReadBytes(SHA256_HASH_SIZE), true);
				PayloadSig = data.ReadBytes(BLS_SIGNATURE_SIZE);
				MakeSureWeAreAtEndOfPayload();
			}

			public uint256 ProTXHash { get; set; }
			public ushort Reason { get; set; }
			public uint256 InputsHash { get; set; }
			public uint PayloadSigSize;
			public byte[] PayloadSig { get; set; }
		}

		public abstract class SpecialTransactionWithHeight : SpecialTransaction
		{
			protected SpecialTransactionWithHeight(byte[] extraPayload) : base(extraPayload)
			{
				Height = data.ReadUInt32();
			}

			/// <summary>
			/// Height of the block
			/// </summary>
			public uint Height { get; set; }
		}

		/// <summary>
		/// For DashTransactionType.MasternodeListMerkleProof
		/// https://github.com/dashpay/dips/blob/master/dip-0004.md
		/// Only needs deserialization here, ExtraPayload can still be serialized
		/// </summary>
		public class CoinbaseSpecialTransaction : SpecialTransactionWithHeight
		{
			public CoinbaseSpecialTransaction(byte[] extraPayload) : base(extraPayload)
			{
				MerkleRootMNList = new uint256(data.ReadBytes(SHA256_HASH_SIZE));
				MakeSureWeAreAtEndOfPayload();
			}

			/// <summary>
			/// Merkle root of the masternode list
			/// </summary>
			public uint256 MerkleRootMNList { get; set; }
		}

		/// <summary>
		/// https://github.com/dashevo/dashcore-lib/blob/master/lib/transaction/payload/commitmenttxpayload.js
		/// </summary>
		public class QuorumCommitmentTransaction : SpecialTransactionWithHeight
		{
			public QuorumCommitmentTransaction(byte[] extraPayload) : base(extraPayload)
			{
				Commitment = new QuorumCommitment(data);
				MakeSureWeAreAtEndOfPayload();
			}

			public QuorumCommitment Commitment { get; set; }
		}

		public class QuorumCommitment
		{
			public QuorumCommitment(BinaryReader data)
			{
				QfcVersion = data.ReadUInt16();
				LlmqType = data.ReadByte();
				QuorumHash = new uint256(data.ReadBytes(SpecialTransaction.SHA256_HASH_SIZE));
				var bs = new BitcoinStream(data.BaseStream, false);
				bs.ReadWriteAsVarInt(ref SignersSize);
				Signers = data.ReadBytes(((int)SignersSize + 7) / 8);
				bs.ReadWriteAsVarInt(ref ValidMembersSize);
				ValidMembers = data.ReadBytes(((int)ValidMembersSize + 7) / 8);
				QuorumPublicKey = data.ReadBytes(SpecialTransaction.BLS_PUBLIC_KEY_SIZE);
				QuorumVvecHash = new uint256(data.ReadBytes(SpecialTransaction.SHA256_HASH_SIZE));
				QuorumSig = data.ReadBytes(SpecialTransaction.BLS_SIGNATURE_SIZE);
				Sig = data.ReadBytes(SpecialTransaction.BLS_SIGNATURE_SIZE);
			}

			public ushort QfcVersion { get; set; }
			public byte LlmqType { get; set; }
			public uint256 QuorumHash { get; set; }
			public uint SignersSize;
			public byte[] Signers { get; set; }
			public uint ValidMembersSize;
			public byte[] ValidMembers { get; set; }
			public byte[] QuorumPublicKey { get; set; }
			public uint256 QuorumVvecHash { get; set; }
			public byte[] QuorumSig { get; set; }
			public byte[] Sig { get; set; }
		}

		/// <summary>
		/// https://docs.dash.org/en/stable/merchants/technical.html#v0-13-0-integration-notes
		/// </summary>
		public class PolisTransaction : Transaction
		{
			public uint PolisVersion => Version & 0xffff;
			public PolisTransactionType PolisType => (PolisTransactionType)((Version >> 16) & 0xffff);
			public byte[] ExtraPayload = new byte[0];
			public ProviderRegistrationTransaction ProRegTx =>
				PolisType == PolisTransactionType.MasternodeRegistration
					? new ProviderRegistrationTransaction(ExtraPayload)
					: null;
			public ProviderUpdateServiceTransaction ProUpServTx =>
				PolisType == PolisTransactionType.UpdateMasternodeService
					? new ProviderUpdateServiceTransaction(ExtraPayload)
					: null;
			public ProviderUpdateRegistrarTransaction ProUpRegTx =>
				PolisType == PolisTransactionType.UpdateMasternodeOperator
					? new ProviderUpdateRegistrarTransaction(ExtraPayload)
					: null;
			public ProviderUpdateRevocationTransaction ProUpRevTx =>
				PolisType == PolisTransactionType.MasternodeRevocation
					? new ProviderUpdateRevocationTransaction(ExtraPayload)
					: null;
			public CoinbaseSpecialTransaction CbTx =>
				PolisType == PolisTransactionType.MasternodeListMerkleProof
					? new CoinbaseSpecialTransaction(ExtraPayload)
					: null;
			public QuorumCommitmentTransaction QcTx =>
				PolisType == PolisTransactionType.QuorumCommitment
					? new QuorumCommitmentTransaction(ExtraPayload)
					: null;

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				// Support for Dash 0.13 extraPayload for Special Transactions
				// https://github.com/dashpay/dips/blob/master/dip-0002-special-transactions.md
				if (PolisVersion >= 3 && PolisType != PolisTransactionType.StandardTransaction)
				{
					// Extra payload size is VarInt
					uint extraPayloadSize = (uint)ExtraPayload.Length;
					stream.ReadWriteAsVarInt(ref extraPayloadSize);
					if (ExtraPayload.Length != extraPayloadSize)
						ExtraPayload = new byte[extraPayloadSize];
					stream.ReadWrite(ref ExtraPayload);
				}
			}
		}

		public class PolisBlock : Block
		{
			public PolisBlock(PolisBlockHeader h) : base(h)
			{
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return PolisConsensusFactory.Instance;
			}
			public override string ToString()
			{
				return "DashBlock " + Header + ", Height=" + GetCoinbaseHeight() +
					", Version=" + Header.Version + ", Txs=" + Transactions.Count;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("PolisCore");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 262800,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x000001f35e70f7c5705f64c6c5cc3dea9449e74d5b5c7cf74dad1bcca14a8012"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000e72e9b868ce8efb7"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 15,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = PolisConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 55 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 56 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 60 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x03, 0xE2, 0x5D, 0x7E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x03, 0xE2, 0x59, 0x45 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("polis"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("polis"))
			.SetMagic(0xBD6B0CBF)
			.SetPort(24126)
			.SetRPCPort(24127)
			.SetMaxP2PVersion(70220)
			.SetName("polis-main")
			.AddAlias("polis-mainnet")
			.SetUriScheme("polis")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("blockbook.polispay.org", "blockbook.polispay.org"),
				new DNSSeedData("insight.polispay.org", "insight.polispay.org"),
				new DNSSeedData("polis.seeds.mn.zone", "polis.seeds.mn.zone"),
				new DNSSeedData("polis.mnseeds.com", "polis.mnseeds.com")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000fc4b8cb903aed54e11e1ae8a5b7ad097ade34988a84500ad2d80e4d1f5bcc95d2bb73b5af0ff0f1edbff04000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2404ffff001d01041c506f6c69732c2066726f6d2070656f706c6520746f2070656f706c65ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
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
				BIP34Hash = new uint256("0x0000047d24635e347be3aaaeb66c26be94901a2f962feccd4f95090191f208c1"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000924e924a21715"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 15,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = PolisConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 140 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tpolis"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tpolis"))
			.SetMagic(0xFFCAE2CE)
			.SetPort(24130)
			.SetRPCPort(24131)
			.SetMaxP2PVersion(70220)
		   	.SetName("polis-test")
		   	.AddAlias("polis-testnet")
			.SetUriScheme("polis")
		   	.AddSeeds(new NetworkAddress[0])
		   	.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000fc4b8cb903aed54e11e1ae8a5b7ad097ade34988a84500ad2d80e4d1f5bcc95d678f875af0ff0f1e94ba01000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2404ffff001d01041c506f6c69732c2066726f6d2070656f706c6520746f2070656f706c65ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
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
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 15,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = PolisConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 140 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tpolis"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tpolis"))
			.SetMagic(0xDCB7C1FC)
			.SetPort(19994)
			.SetRPCPort(19993)
			.SetMaxP2PVersion(70220)
			.SetName("polis-reg")
			.AddAlias("polis-regtest")
			.SetUriScheme("polis")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000fc4b8cb903aed54e11e1ae8a5b7ad097ade34988a84500ad2d80e4d1f5bcc95d9a3b3b5af0ff0f1e3c8b0d000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff2404ffff001d01041c506f6c69732c2066726f6d2070656f706c6520746f2070656f706c65ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

	}
}
