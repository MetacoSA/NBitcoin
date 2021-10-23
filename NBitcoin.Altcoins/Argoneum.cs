using System;
using System.IO;
using System.Linq;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.Altcoins.ArgoneumInternals;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/Argoneum/argoneum/blob/master/src/chainparams.cpp
	public class Argoneum : NetworkSetBase
	{
		private const uint PHI2_HARDFORK_TIME = 1541934671; // 11/11/2018 @ 11:11:11am (UTC)

		public static Argoneum Instance { get; } = new Argoneum();

		public override string CryptoCode => "AGM";

		private Argoneum()
		{

		}

		public class ArgoneumConsensusFactory : ConsensusFactory
		{
			private ArgoneumConsensusFactory()
			{
			}

			// ReSharper disable once MemberHidesStaticFromOuterClass
			public static ArgoneumConsensusFactory Instance { get; } = new ArgoneumConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new ArgoneumBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new ArgoneumBlock(new ArgoneumBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new ArgoneumTransaction();
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class ArgoneumBlockHeader : BlockHeader
		{
			// Reference: https://github.com/Argoneum/argoneum/blob/master/src/primitives/block.cpp#L13
			byte[] CalculateHash(byte[] data, int offset, int count)
			{
				if (this.nTime < PHI2_HARDFORK_TIME) {
					return new Skein().ComputeBytes(data.Skip(offset).Take(count).ToArray());
				}
				return new Phi2().ComputeHash(data.Skip(offset).Take(count).ToArray());
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
		public enum ArgoneumTransactionType
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
			/// Version number. Currently set to 1 for all ArgoneumTransactionTypes
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
		/// For ArgoneumTransactionType.MasternodeListMerkleProof
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
		public class ArgoneumTransaction : Transaction
		{
			public uint ArgoneumVersion => Version & 0xffff;
			public ArgoneumTransactionType ArgoneumType => (ArgoneumTransactionType)((Version >> 16) & 0xffff);
			public byte[] ExtraPayload = new byte[0];
			public ProviderRegistrationTransaction ProRegTx =>
				ArgoneumType == ArgoneumTransactionType.MasternodeRegistration
					? new ProviderRegistrationTransaction(ExtraPayload)
					: null;
			public ProviderUpdateServiceTransaction ProUpServTx =>
				ArgoneumType == ArgoneumTransactionType.UpdateMasternodeService
					? new ProviderUpdateServiceTransaction(ExtraPayload)
					: null;
			public ProviderUpdateRegistrarTransaction ProUpRegTx =>
				ArgoneumType == ArgoneumTransactionType.UpdateMasternodeOperator
					? new ProviderUpdateRegistrarTransaction(ExtraPayload)
					: null;
			public ProviderUpdateRevocationTransaction ProUpRevTx =>
				ArgoneumType == ArgoneumTransactionType.MasternodeRevocation
					? new ProviderUpdateRevocationTransaction(ExtraPayload)
					: null;
			public CoinbaseSpecialTransaction CbTx =>
				ArgoneumType == ArgoneumTransactionType.MasternodeListMerkleProof
					? new CoinbaseSpecialTransaction(ExtraPayload)
					: null;
			public QuorumCommitmentTransaction QcTx =>
				ArgoneumType == ArgoneumTransactionType.QuorumCommitment
					? new QuorumCommitmentTransaction(ExtraPayload)
					: null;

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				// Support for Argoneum 1.4 extraPayload for Special Transactions
				// https://github.com/dashpay/dips/blob/master/dip-0002-special-transactions.md
				if (ArgoneumVersion >= 3 && ArgoneumType != ArgoneumTransactionType.StandardTransaction)
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

		public class ArgoneumBlock : Block
		{
			public ArgoneumBlock(ArgoneumBlockHeader h) : base(h)
			{
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return Instance.Mainnet.Consensus.ConsensusFactory;
			}

			public override string ToString()
			{
				return "ArgoneumBlock " + Header + ", Height=" + GetCoinbaseHeight() +
					", Version=" + Header.Version + ", Txs=" + Transactions.Count;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Argoneum");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 525600,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x0000019913c8bb39636467e961b8c0f4d3d656437de2cd876f2da6a05cc8d393"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x00000000000000000000000000000000000000000000000006cc052ef41cc2e8"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = ArgoneumConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x32 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x61 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 0xbf })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("agm"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("agm"))
			.SetMagic(0x004d4741)
			.SetPort(9898)
			.SetRPCPort(9899)
			.SetMaxP2PVersion(70215)
			.SetName("argoneum-main")
			.AddAlias("argoneum-mainnet")
			.SetUriScheme("argoneum")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("argoneum.net", "seed1.argoneum.net"),
				new DNSSeedData("argoneum.net", "seed2.argoneum.net"),
				new DNSSeedData("argoneum.net", "seed3.argoneum.net"),
				new DNSSeedData("argoneum.net", "seed4.argoneum.net"),
				new DNSSeedData("argoneum.net", "seed5.argoneum.net"),
			})
			.AddSeeds(new NetworkAddress[0])
			// FIXME
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000008413e4d942bcac7d58d727e5e9f45900e048966acec55bfa815bceb64c5ca4ed90dfd95bf0ff0f1ea36617000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440323031382f31312f30313a204172676f6e65756d2c20746865206d61737465726e6f646520736f6c7574696f6e7320706c6174666f726d2077617320626f726effffffff010000000000000000434104480f351d994e150563c3c686e25247513ccfcd98d8826fb450f164f9400659e50b066b6fc2d110b7ed61a27a2b932e3f4e5564da19716dd3e5d5fc0e4bfed625ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 525600,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x00000e91f3a63b1b81797a52348931a7078c0eba642bb79e64090cdf38764e83"),
				PowLimit = new Target(new uint256("0x00000fffff000000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = ArgoneumConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x80 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x42 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("targoneum"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("targoneum"))
			.SetMagic(0x014d4741)
			.SetPort(19898)
			.SetRPCPort(19899)
			.SetMaxP2PVersion(70215)
		   .SetName("argoneum-test")
		   .AddAlias("argoneum-testnet")
		   .AddDNSSeeds(new[]
		   {
				new DNSSeedData("argoneum.net",  "testnet-seed1.argoneum.net"),
				new DNSSeedData("argoneum.net",  "testnet-seed2.argoneum.net"),
		   })
		   .SetUriScheme("argoneum")
		   .AddSeeds(new NetworkAddress[0])
			// FIXME
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000008413e4d942bcac7d58d727e5e9f45900e048966acec55bfa815bceb64c5ca4ed90dfd95bf0ff0f1ea36617000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440323031382f31312f30313a204172676f6e65756d2c20746865206d61737465726e6f646520736f6c7574696f6e7320706c6174666f726d2077617320626f726effffffff010000000000000000434104480f351d994e150563c3c686e25247513ccfcd98d8826fb450f164f9400659e50b066b6fc2d110b7ed61a27a2b932e3f4e5564da19716dd3e5d5fc0e4bfed625ac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 525,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256(),
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(1 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = ArgoneumConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0x80 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 0x42 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("targoneum"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("targoneum"))
			.SetMagic(0x024d4741)
			.SetPort(20898)
			.SetRPCPort(20899)
			.SetMaxP2PVersion(70215)
			.SetName("argoneum-reg")
			.AddAlias("argoneum-regtest")
			.SetUriScheme("argoneum")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			// FIXME
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000008413e4d942bcac7d58d727e5e9f45900e048966acec55bfa815bceb64c5ca4ed90dfd95bf0ff0f1ea36617000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4804ffff001d010440323031382f31312f30313a204172676f6e65756d2c20746865206d61737465726e6f646520736f6c7574696f6e7320706c6174666f726d2077617320626f726effffffff010000000000000000434104480f351d994e150563c3c686e25247513ccfcd98d8826fb450f164f9400659e50b066b6fc2d110b7ed61a27a2b932e3f4e5564da19716dd3e5d5fc0e4bfed625ac00000000");
			return builder;
		}
	}
}
