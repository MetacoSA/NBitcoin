using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.IO;
using System.Linq;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/dashpay/dash/blob/master/src/chainparams.cpp
	public class Dash : NetworkSetBase
	{
		public static Dash Instance { get; } = new Dash();

		public override string CryptoCode => "DASH";

		private Dash()
		{

		}

		public class DashConsensusFactory : ConsensusFactory
		{
			private DashConsensusFactory()
			{
			}

			// ReSharper disable once MemberHidesStaticFromOuterClass
			public static DashConsensusFactory Instance { get; } = new DashConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new DashBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new DashBlock(new DashBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new DashTransaction();
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class DashBlockHeader : BlockHeader
		{
			// https://github.com/dashpay/dash/blob/e596762ca22d703a79c6880a9d3edb1c7c972fd3/src/primitives/block.cpp#L13
			static byte[] CalculateHash(byte[] data, int offset, int count)
			{
				return new HashX11.X11().ComputeBytes(data.Skip(offset).Take(count).ToArray());
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash);
			}
		}

		/// <summary>
		/// Transactions with version >= 3 have a special transaction type in the version code
		/// https://docs.dash.org/en/stable/merchants/technical.html#v0-13-0-integration-notes
		/// 0.14 will add more types: https://github.com/dashpay/dips/blob/master/dip-0002-special-transactions.md
		/// </summary>
		public enum DashTransactionType
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
		public class DashTransaction : Transaction
		{
			public uint DashVersion => Version & 0xffff;
			public DashTransactionType DashType => (DashTransactionType)((Version >> 16) & 0xffff);
			public byte[] ExtraPayload = new byte[0];
			public ProviderRegistrationTransaction ProRegTx =>
				DashType == DashTransactionType.MasternodeRegistration
					? new ProviderRegistrationTransaction(ExtraPayload)
					: null;
			public ProviderUpdateServiceTransaction ProUpServTx =>
				DashType == DashTransactionType.UpdateMasternodeService
					? new ProviderUpdateServiceTransaction(ExtraPayload)
					: null;
			public ProviderUpdateRegistrarTransaction ProUpRegTx =>
				DashType == DashTransactionType.UpdateMasternodeOperator
					? new ProviderUpdateRegistrarTransaction(ExtraPayload)
					: null;
			public ProviderUpdateRevocationTransaction ProUpRevTx =>
				DashType == DashTransactionType.MasternodeRevocation
					? new ProviderUpdateRevocationTransaction(ExtraPayload)
					: null;
			public CoinbaseSpecialTransaction CbTx =>
				DashType == DashTransactionType.MasternodeListMerkleProof
					? new CoinbaseSpecialTransaction(ExtraPayload)
					: null;
			public QuorumCommitmentTransaction QcTx =>
				DashType == DashTransactionType.QuorumCommitment
					? new QuorumCommitmentTransaction(ExtraPayload)
					: null;

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				// Support for Dash 0.13 extraPayload for Special Transactions
				// https://github.com/dashpay/dips/blob/master/dip-0002-special-transactions.md
				if (DashVersion >= 3 && DashType != DashTransactionType.StandardTransaction)
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

		public class DashBlock : Block
		{
			public DashBlock(DashBlockHeader h) : base(h)
			{
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return Instance.Mainnet.Consensus.ConsensusFactory;
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
			RegisterDefaultCookiePath("DashCore");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210240,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x000007d91d1254d60e2dd1ae580383070a4ddffa4c64c2eeb4a2f9ecc0414343"),
				PowLimit = new Target(new uint256("0x000007d91d1254d60e2dd1ae580383070a4ddffa4c64c2eeb4a2f9ecc0414343")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000100a308553b4863b755"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = DashConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 76 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 16 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 204 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("dash"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("dash"))
			.SetUriScheme("dash")
			.SetMagic(0xBD6B0CBF)
			.SetPort(9999)
			.SetRPCPort(9998)
			.SetMaxP2PVersion(70223)
			.SetName("dash-main")
			.AddAlias("dash-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("dash.org", "dnsseed.dash.org"),
				new DNSSeedData("dashdot.io", "dnsseed.dashdot.io"),
				new DNSSeedData("masternode.io", "dnsseed.masternode.io"),
				new DNSSeedData("dashpay.io", "dnsseed.dashpay.io")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000c762a6567f3cc092f0684bb62b7e00a84890b990f07cc71a6bb58d64b98e02e0022ddb52f0ff0f1ec23fb9010101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6204ffff001d01044c5957697265642030392f4a616e2f3230313420546865204772616e64204578706572696d656e7420476f6573204c6976653a204f76657273746f636b2e636f6d204973204e6f7720416363657074696e6720426974636f696e73ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
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
				PowTargetSpacing = TimeSpan.FromSeconds(2.5 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = DashConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 140 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tdash"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tdash"))
			.SetMagic(0xFFCAE2CE)
			.SetPort(19999)
			.SetRPCPort(19998)
			.SetMaxP2PVersion(70223)
		   .SetName("dash-test")
		   .AddAlias("dash-testnet")
		   .SetUriScheme("dash")
		   .AddDNSSeeds(new[]
		   {
				new DNSSeedData("dashdot.io",  "testnet-seed.dashdot.io"),
				new DNSSeedData("masternode.io", "test.dnsseed.masternode.io")
		   })
		   .AddSeeds(new NetworkAddress[0])
		   .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000c762a6567f3cc092f0684bb62b7e00a84890b990f07cc71a6bb58d64b98e02e0dee1e352f0ff0f1ec3c927e60101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6204ffff001d01044c5957697265642030392f4a616e2f3230313420546865204772616e64204578706572696d656e7420476f6573204c6976653a204f76657273746f636b2e636f6d204973204e6f7720416363657074696e6720426974636f696e73ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
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
				ConsensusFactory = DashConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetMaxP2PVersion(70223)
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 140 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tdash"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tdash"))
			.SetMagic(0xDCB7C1FC)
			.SetPort(19994)
			.SetRPCPort(19993)
			.SetName("dash-reg")
			.AddAlias("dash-regtest")
			.SetUriScheme("dash")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000c762a6567f3cc092f0684bb62b7e00a84890b990f07cc71a6bb58d64b98e02e0b9968054ffff7f20ffba10000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff6204ffff001d01044c5957697265642030392f4a616e2f3230313420546865204772616e64204578706572696d656e7420476f6573204c6976653a204f76657273746f636b2e636f6d204973204e6f7720416363657074696e6720426974636f696e73ffffffff0100f2052a010000004341040184710fa689ad5023690c80f3a49c8f13f8d45b8c857fbcbc8bc4a8e4d3eb4b10f4d4604fa08dce601aaf0f470216fe1b51850b4acf21b179c45070ac7b03a9ac00000000");
			return builder;
		}
	}
}
