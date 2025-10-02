using System;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System.Collections.Generic;
using NBitcoin.Altcoins.HashX11.Crypto.SHA3;
using NBitcoin.Protocol;
using System.IO;



namespace NBitcoin.Altcoins
{
	public partial class Decred : NetworkSetBase
	{
		public static Decred Instance { get; } = new Decred();
		public override string CryptoCode => "DCR";

		private Decred()
		{

		}

		private static byte[] Blake256(byte[] b, int offset, int length)
		{
			byte[] bCopy = new byte[length];
			Array.Copy(b, bCopy, length);
			var blake = new Blake256();
			return blake.ComputeBytes(bCopy).GetBytes();
		}
		private static byte[] DoubleBlake256(byte[] b, int offset, int length)
		{
			byte[] bCopy = new byte[length];
			Array.Copy(b, bCopy, length);
			var blake = new Blake256();
			var passA = blake.ComputeBytes(bCopy).GetBytes();
			return blake.ComputeBytes(passA).GetBytes();
		}


		public class DecredConsensusFactory : ConsensusFactory
		{
			private DecredConsensusFactory()
			{
			}

			public static DecredConsensusFactory Instance { get; } = new DecredConsensusFactory();

			class DecredProtocolCapabilities : ProtocolCapabilities
			{

				private static readonly DecredProtocolCapabilities _Instance = new DecredProtocolCapabilities();
				public static DecredProtocolCapabilities Instance
				{
					get
					{
						return _Instance;
					}
				}
				public DecredProtocolCapabilities()
				{
					PeerTooOld = false;
					SupportCheckSum = true;
					SupportCompactBlocks = false;
					SupportGetBlock = true;
					SupportMempoolQuery = true;
					SupportNodeBloom = false;
					SupportPingPong = true;
					SupportSendHeaders = true;
					SupportTimeAddress = true;
					SupportUserAgent = true;
					SupportWitness = false;
				}

				public override HashStreamBase GetChecksumHashStream(int hintSize)
				{
					return BufferedHashStream.CreateFrom(Blake256, hintSize);
				}
				public override HashStreamBase GetChecksumHashStream()
				{
					return BufferedHashStream.CreateFrom(Blake256, 32);
				}
			}

			public override ProtocolCapabilities GetProtocolCapabilities(uint protocolVersion)
			{
				return DecredProtocolCapabilities.Instance;
			}

			public override BlockHeader CreateBlockHeader()
			{
				return new DecredBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new DecredBlock((DecredBlockHeader)CreateBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new DecredTransaction();
			}

			public override TxOut CreateTxOut()
			{
				return new DecredTxOut();
			}

			public override TxIn CreateTxIn()
			{
				return new DecredTxIn();
			}
		}

		public class DecredTxIn : TxIn
		{
			protected uint nPrevOutTree = 0;

			public uint PrevOutTree
			{
				get
				{
					return nPrevOutTree;
				}
				set
				{
					nPrevOutTree = value;
				}
			}
			protected ulong nValue = 0;

			public ulong Value
			{
				get
				{
					return nValue;
				}
				set
				{
					nValue = value;
				}
			}

			protected uint nHeight = 0;

			public uint Height
			{
				get
				{
					return nHeight;
				}
				set
				{
					nHeight = value;
				}
			}

			protected uint nIndex = 0;

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

			public override TxIn Clone()
			{
				var txIn = new DecredTxIn();
				txIn.Sequence = this.Sequence;
				txIn.PrevOutTree = this.PrevOutTree;
				txIn.PrevOut = this.PrevOut;
				txIn.Value = this.Value;
				txIn.Height = this.Height;
				txIn.Index = this.Index;
				txIn.ScriptSig = this.ScriptSig;
				return txIn;
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return DecredConsensusFactory.Instance;
			}
		}

		public class DecredTxOut : TxOut
		{

			protected uint nVersion = 0;

			public uint Version
			{
				get
				{
					return nVersion;
				}
				set
				{
					nVersion = value;
				}
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return DecredConsensusFactory.Instance;
			}
		}

		public class DecredTransaction : Transaction
		{

			protected uint nSerType = 0;

			public uint SerType
			{
				get
				{
					return nSerType;
				}
				set
				{
					nSerType = value;
				}
			}

			protected uint nExpiry = 0;

			public uint Expiry
			{
				get
				{
					return nExpiry;
				}
				set
				{
					nExpiry = value;
				}
			}

			private enum serType : uint
			{
				All = 0,
				Prefix = 1,
				Witness = 3,
			};

			public override void ReadWrite(BitcoinStream stream)
			{
				var witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0) &&
								stream.ProtocolCapabilities.SupportWitness;
				var sType = witSupported ? serType.All : serType.Prefix;
				this.readWrite(stream, sType, 0, null);
			}

			private void readWrite(BitcoinStream stream, serType sType, int signInput, Script signScript)
			{
				var witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0) &&
								stream.ProtocolCapabilities.SupportWitness;
				if (!stream.Serializing)
				{
					var versionB = new byte[2];
					var serTypeB = new byte[2];
					stream.ReadWriteBytes(versionB, 0, 2);
					stream.ReadWriteBytes(serTypeB, 0, 2);
					this.Version = BitConverter.ToUInt16(versionB, 0);
					this.SerType = BitConverter.ToUInt16(serTypeB, 0);
					uint txInCount = 0;
					stream.ReadWriteAsVarInt(ref txInCount);
					for (int i = 0; i < txInCount; i++)
					{
						var input = new DecredTxIn();
						var prevOutIndexB = new byte[4];
						var prevOutTreeB = new byte[1];
						var prevOutSequenceB = new byte[4];
						var prevOutHash = new uint256();
						stream.ReadWrite(ref prevOutHash);
						stream.ReadWriteBytes(prevOutIndexB, 0, 4);
						stream.ReadWriteBytes(prevOutTreeB, 0, 1);
						stream.ReadWriteBytes(prevOutSequenceB, 0, 4);
						input.PrevOut.N = BitConverter.ToUInt32(prevOutIndexB, 0);
						input.PrevOutTree = prevOutTreeB[0];
						input.Sequence = BitConverter.ToUInt32(prevOutSequenceB, 0);
						input.PrevOut.Hash = prevOutHash;
						this.Inputs.Add(input);
					}
					uint txOutCount = 0;
					stream.ReadWriteAsVarInt(ref txOutCount);
					for (int i = 0; i < txOutCount; i++)
					{
						var output = new DecredTxOut();
						var txOutValueB = new byte[8];
						var txOutVersionB = new byte[2];
						stream.ReadWriteBytes(txOutValueB, 0, 8);
						stream.ReadWriteBytes(txOutVersionB, 0, 2);
						var script = new Script();
						stream.ReadWrite(ref script);
						output.ScriptPubKey = script;
						output.Value = BitConverter.ToUInt64(txOutValueB, 0);
						output.Version = BitConverter.ToUInt16(txOutVersionB, 0);
						this.Outputs.Add(output);
					}
					var locktimeB = new byte[4];
					var expiryB = new byte[4];
					stream.ReadWriteBytes(locktimeB, 0, 4);
					stream.ReadWriteBytes(expiryB, 0, 4);
					this.LockTime = BitConverter.ToUInt32(locktimeB, 0);
					this.Expiry = BitConverter.ToUInt32(expiryB, 0);
					uint witnessCount = 0;
					stream.ReadWriteAsVarInt(ref witnessCount);
					for (int i = 0; i < witnessCount; i++)
					{
						var input = (DecredTxIn)this.vin[i];
						var witnessValueB = new byte[8];
						var witnessHeightB = new byte[4];
						var witnessIndexB = new byte[4];
						var script = new Script();
						stream.ReadWriteBytes(witnessValueB);
						stream.ReadWriteBytes(witnessHeightB);
						stream.ReadWriteBytes(witnessIndexB);
						stream.ReadWrite(ref script);
						input.ScriptSig = script;
						input.Value = BitConverter.ToUInt64(witnessValueB, 0);
						input.Height = BitConverter.ToUInt32(witnessHeightB, 0);
						input.Index = BitConverter.ToUInt32(witnessIndexB, 0);
					}
				}
				else
				{
					var versionB = BitConverter.GetBytes(this.Version);
					var serTypeB = BitConverter.GetBytes((uint)sType);
					stream.ReadWriteBytes(versionB, 0, 2);
					stream.ReadWriteBytes(serTypeB, 0, 2);
					var txInCount = (uint)this.Inputs.Count;
					if (sType != serType.Witness)
					{
						stream.ReadWriteAsVarInt(ref txInCount);
						for (int i = 0; i < txInCount; i++)
						{
							DecredTxIn input = (DecredTxIn)this.Inputs[i];
							var prevOutIndexB = BitConverter.GetBytes(input.PrevOut.N);
							var prevOutTreeB = BitConverter.GetBytes(input.PrevOutTree);
							var prevOutSequenceB = BitConverter.GetBytes(input.Sequence);
							stream.ReadWrite(input.PrevOut.Hash);
							stream.ReadWriteBytes(prevOutIndexB, 0, 4);
							stream.ReadWriteBytes(prevOutTreeB, 0, 1);
							stream.ReadWriteBytes(prevOutSequenceB, 0, 4);
						}
						var txOutCount = (uint)this.Outputs.Count;
						stream.ReadWriteAsVarInt(ref txOutCount);
						for (int i = 0; i < txOutCount; i++)
						{
							DecredTxOut output = (DecredTxOut)this.Outputs[i];
							var txOutValueB = BitConverter.GetBytes(output.Value);
							var txOutVersionB = BitConverter.GetBytes(output.Version);
							stream.ReadWriteBytes(txOutValueB, 0, 8);
							stream.ReadWriteBytes(txOutVersionB, 0, 2);
							var script = output.ScriptPubKey;
							stream.ReadWrite(ref script);
						}
						var locktimeB = BitConverter.GetBytes(this.LockTime);
						var expiryB = BitConverter.GetBytes(this.Expiry);
						stream.ReadWriteBytes(locktimeB, 0, 4);
						stream.ReadWriteBytes(expiryB, 0, 4);
					}
					if (sType == serType.Prefix) { return; }
					stream.ReadWriteAsVarInt(ref txInCount);
					for (int i = 0; i < txInCount; i++)
					{
						DecredTxIn input = (DecredTxIn)this.Inputs[i];
						if (sType == serType.All)
						{
							var witnessValueB = BitConverter.GetBytes(input.Value);
							var witnessHeightB = BitConverter.GetBytes(input.Height);
							var witnessIndexB = BitConverter.GetBytes(input.Index);
							stream.ReadWriteBytes(witnessValueB, 0, 8);
							stream.ReadWriteBytes(witnessHeightB, 0, 4);
							stream.ReadWriteBytes(witnessIndexB, 0, 4);
						}
						var script = new Script();
						if (sType == serType.All)
						{
							script = input.ScriptSig;
						}
						if (sType == serType.Witness && i == signInput)
						{
							script = signScript;
						}
						stream.ReadWrite(ref script);
					}
				}
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(Blake256, 32);
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return DecredConsensusFactory.Instance;
			}
			public override uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
			{
				// TODO: Correctly handle hash types.
				var hs = this.CreateHashStream();
				var stream = new BitcoinStream(hs, true);
				this.readWrite(stream, serType.Prefix, 0, null);
				var prefixHash = hs.GetHash();

				hs = this.CreateHashStream();
				stream = new BitcoinStream(hs, true);
				this.readWrite(stream, serType.Witness, nIn, scriptCode);
				var witnessHash = hs.GetHash();

				hs = this.CreateHashStream();
				stream = new BitcoinStream(hs, true);
				var hashTypeB = BitConverter.GetBytes(1); // ALL
				stream.ReadWriteBytes(hashTypeB, 0, 4);
				stream.ReadWrite(ref prefixHash);
				stream.ReadWrite(ref witnessHash);
				var sigHash = hs.GetHash();
				return sigHash;
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class DecredBlockHeader : BlockHeader
		{

			protected uint256 nStakeRoot = new uint256();

			public uint256 StakeRoot
			{
				get
				{
					return nStakeRoot;
				}
				set
				{
					nStakeRoot = value;
				}
			}

			protected UInt16 nVoteBits = 0;

			public UInt16 VoteBits
			{
				get
				{
					return nVoteBits;
				}
				set
				{
					nVoteBits = value;
				}
			}

			protected UInt16 nVoters = 0;

			public UInt16 Voters
			{
				get
				{
					return nVoters;
				}
				set
				{
					nVoters = value;
				}
			}

			protected byte[] nFinalState = new byte[6];

			public byte[] FinalState
			{
				get
				{
					return nFinalState;
				}
				set
				{
					nFinalState = value;
				}
			}

			protected byte nFreshStake = new();

			public byte FreshStake
			{
				get
				{
					return nFreshStake;
				}
				set
				{
					nFreshStake = value;
				}
			}

			protected byte nRevocations = new();

			public byte Revocations
			{
				get
				{
					return nRevocations;
				}
				set
				{
					nRevocations = value;
				}
			}

			protected UInt32 nPoolSize = 0;

			public UInt32 PoolSize
			{
				get
				{
					return nPoolSize;
				}
				set
				{
					nPoolSize = value;
				}
			}

			protected Int64 nSBits = 0;

			public Int64 SBits
			{
				get
				{
					return nSBits;
				}
				set
				{
					nSBits = value;
				}
			}

			protected UInt32 nHeight = 0;

			public UInt32 Height
			{
				get
				{
					return nHeight;
				}
				set
				{
					nHeight = value;
				}
			}

			protected UInt32 nSize = 0;

			public UInt32 BlockSize
			{
				get
				{
					return nSize;
				}
				set
				{
					nSize = value;
				}
			}

			protected byte[] nExtraData = new byte[32];

			public byte[] ExtraData
			{
				get
				{
					return nExtraData;
				}
				set
				{
					nExtraData = value;
				}
			}

			protected UInt32 nStakeVersion = 0;

			public UInt32 StakeVersion
			{
				get
				{
					return nStakeVersion;
				}
				set
				{
					nStakeVersion = value;
				}
			}
			public override void ReadWrite(BitcoinStream stream)
			{
				if (!stream.Serializing)
				{
					var versionB = new byte[4];
					var voteBitsB = new byte[2];
					var votersB = new byte[2];
					var freshStakeB = new byte[1];
					var revocationsB = new byte[1];
					var poolSizeB = new byte[4];
					var bitsB = new byte[4];
					var sBitsB = new byte[8];
					var heightB = new byte[4];
					var sizeB = new byte[4];
					var timestampB = new byte[4];
					var nonceB = new byte[4];
					var stakeVersionB = new byte[4];
					stream.ReadWriteBytes(versionB, 0, 4);
					stream.ReadWrite(ref this.hashPrevBlock);
					stream.ReadWrite(ref this.hashMerkleRoot);
					stream.ReadWrite(ref this.nStakeRoot);
					stream.ReadWriteBytes(voteBitsB, 0, 2);
					stream.ReadWriteBytes(this.nFinalState, 0, 6);
					stream.ReadWriteBytes(votersB, 0, 2);
					stream.ReadWriteBytes(freshStakeB, 0, 1);
					stream.ReadWriteBytes(revocationsB, 0, 1);
					stream.ReadWriteBytes(poolSizeB, 0, 4);
					stream.ReadWriteBytes(bitsB, 0, 4);
					stream.ReadWriteBytes(sBitsB, 0, 8);
					stream.ReadWriteBytes(heightB, 0, 4);
					stream.ReadWriteBytes(sizeB, 0, 4);
					stream.ReadWriteBytes(timestampB, 0, 4);
					stream.ReadWriteBytes(nonceB, 0, 4);
					stream.ReadWriteBytes(this.nExtraData, 0, 32);
					stream.ReadWriteBytes(stakeVersionB, 0, 4);
					this.nVersion = (int)BitConverter.ToUInt32(versionB, 0);
					this.nVoteBits = BitConverter.ToUInt16(voteBitsB, 0);
					this.nVoters = BitConverter.ToUInt16(votersB, 0);
					this.nFreshStake = freshStakeB[0];
					this.nRevocations = revocationsB[0];
					this.nPoolSize = BitConverter.ToUInt32(poolSizeB, 0);
					this.nBits = BitConverter.ToUInt32(bitsB, 0);
					this.nSBits = BitConverter.ToInt64(sBitsB, 0);
					this.nHeight = BitConverter.ToUInt32(heightB, 0);
					this.nSize = BitConverter.ToUInt32(sizeB, 0);
					this.nTime = BitConverter.ToUInt32(timestampB, 0);
					this.nNonce = BitConverter.ToUInt32(nonceB, 0);
					this.nStakeVersion = BitConverter.ToUInt32(stakeVersionB, 0);
				}
				else
				{
					var versionB = BitConverter.GetBytes(this.nVersion);
					var voteBitsB = BitConverter.GetBytes(this.nVoteBits); //new byte[2];
					var votersB = BitConverter.GetBytes(this.nVoters); //new byte[2];
					var freshStakeB = BitConverter.GetBytes(this.nFreshStake); //new byte[1];
					var revocationsB = BitConverter.GetBytes(this.nRevocations); //new byte[1];
					var poolSizeB = BitConverter.GetBytes(this.nPoolSize); //new byte[4];
					var bitsB = BitConverter.GetBytes(this.nBits); //new byte[4];
					var sBitsB = BitConverter.GetBytes(this.SBits); //new byte[8];
					var heightB = BitConverter.GetBytes(this.nHeight); //new byte[4];
					var sizeB = BitConverter.GetBytes(this.nSize); //new byte[4];
					var timestampB = BitConverter.GetBytes(this.nTime); //new byte[4];
					var nonceB = BitConverter.GetBytes(this.nNonce); //new byte[4];
					var stakeVersionB = BitConverter.GetBytes(this.nStakeVersion); //new byte[4];
					stream.ReadWriteBytes(versionB, 0, 4);
					stream.ReadWrite(this.hashPrevBlock);
					stream.ReadWrite(this.hashMerkleRoot);
					stream.ReadWrite(this.nStakeRoot);
					stream.ReadWriteBytes(voteBitsB, 0, 2);
					stream.ReadWriteBytes(this.nFinalState, 0, 6);
					stream.ReadWriteBytes(votersB, 0, 2);
					stream.ReadWriteBytes(freshStakeB, 0, 1);
					stream.ReadWriteBytes(revocationsB, 0, 1);
					stream.ReadWriteBytes(poolSizeB, 0, 4);
					stream.ReadWriteBytes(bitsB, 0, 4);
					stream.ReadWriteBytes(sBitsB, 0, 8);
					stream.ReadWriteBytes(heightB, 0, 4);
					stream.ReadWriteBytes(sizeB, 0, 4);
					stream.ReadWriteBytes(timestampB, 0, 4);
					stream.ReadWriteBytes(nonceB, 0, 4);
					stream.ReadWriteBytes(this.nExtraData, 0, 32);
					stream.ReadWriteBytes(stakeVersionB, 0, 4);
				}
			}
			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(Blake256, 32);
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class DecredBlock(Decred.DecredBlockHeader header) : Block(header)
		{

			List<Transaction> svtx = [];

			public List<Transaction> STransactions
			{
				get
				{
					return svtx;
				}
				set
				{
					svtx = value;
				}
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				if (!stream.Serializing)
				{
					DecredBlockHeader header = new DecredBlockHeader();
					stream.ReadWrite(ref header);
					this.Header = header;
					uint txCount = 0;
					stream.ReadWriteAsVarInt(ref txCount);
					for (int i = 0; i < txCount; i++)
					{
						DecredTransaction tx = new DecredTransaction();
						stream.ReadWrite(ref tx);
						this.Transactions.Add(tx);
					}
					stream.ReadWriteAsVarInt(ref txCount);
					for (int i = 0; i < txCount; i++)
					{
						DecredTransaction tx = new DecredTransaction();
						stream.ReadWrite(ref tx);
						this.STransactions.Add(tx);
					}
				}
				else
				{
					stream.ReadWrite(this.Header);
					this.Header = header;
					uint txCount = (uint)this.Transactions.Count;
					stream.ReadWriteAsVarInt(ref txCount);
					for (int i = 0; i < txCount; i++)
					{
						stream.ReadWrite(this.Transactions[i]);
					}
					txCount = (uint)this.STransactions.Count;
					stream.ReadWriteAsVarInt(ref txCount);
					for (int i = 0; i < txCount; i++)
					{
						stream.ReadWrite(this.STransactions[i]);
					}
				}
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return DecredConsensusFactory.Instance;
			}
		}

		public class DecredBase58CheckEncoder : Base58CheckEncoder
		{
			protected override byte[] CalculateHash(byte[] bytes, int offset, int length)
			{
				return Decred.DoubleBlake256(bytes, offset, length);
			}
		}

		private static readonly DecredBase58CheckEncoder _Base58Check = new DecredBase58CheckEncoder();
		public class DecredAddressStringParser : NetworkStringParser
		{
			public override Base58CheckEncoder GetBase58CheckEncoder()
			{
				return (Base58CheckEncoder)_Base58Check;
			}
		}

		private static uint160 _hash160(byte[] data, int offset, int count)
		{
			return new uint160(Hashes.RIPEMD160(Blake256(data, offset, count)));
		}

		protected override NetworkBuilder CreateMainnet()
		{
			return new NetworkBuilder()
			.SetNetworkSet(this)
			.SetConsensus(new Consensus()
			{
				PowLimit = new Target(new uint256("0x00000000ffff0000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000243845fb2fb3d8f20ddfeb"),
				ConsensusFactory = DecredConsensusFactory.Instance,
			}
			)
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [0x07, 0x3f]) // starts with Ds (pubkey hash)
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [0x07, 0x1a]) // starts with Dc (script hash)
			.SetBase58Bytes(Base58Type.SECRET_KEY, [0x22, 0xde]) // starts with Pm
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x02, 0xfd, 0xa9, 0x26]) // starts with dpub
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x02, 0xfd, 0xa4, 0xe8]) // starts with dprv
			.SetNetworkStringParser(new DecredAddressStringParser())
			.SetHasher160(_hash160)
			.SetMagic(0xd9b400f9)
			.SetPort(9108)
			.SetRPCPort(9109)
			.SetWalletRPCPort(9110)
			.SetName("dcr-main")
			.SetIsDecred(true)
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000000dc101dfc3c6a2eb10ca0c5374e10d28feb53f7eabcc850511ceadb99174aa66000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffff011b00c2eb0b000000000000000000000000a0d7b856000000000000000000000000000000000000000000000000000000000000000000000000000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff00ffffffff010000000000000000000020801679e98561ada96caec2949a5d41c4cab3851eb740d951c10ecbcf265c1fd9000000000000000001ffffffffffffffff00000000ffffffff02000000");
		}

		protected override NetworkBuilder CreateRegtest()
		{
			return new NetworkBuilder()
			.SetNetworkSet(this)
			.SetConsensus(new Consensus()
			{
				PowLimit = new Target(new uint256("0x7fffff0000000000000000000000000000000000000000000000000000000000")),
				ConsensusFactory = DecredConsensusFactory.Instance,
			}
			)
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [0x0e, 0x91]) // starts with Ss (pubkey hash)
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [0x0e, 0x6c]) // starts with Sc (script hash)
			.SetBase58Bytes(Base58Type.SECRET_KEY, [0x23, 0x07]) // starts with Ps
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x20, 0xbd, 0x3d]) // starts with spub
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x20, 0xb9, 0x03]) // starts with sprv
			.SetNetworkStringParser(new DecredAddressStringParser())
			.SetHasher160(_hash160)
			.SetMagic(0x12141c16)
			.SetPort(19560)
			.SetRPCPort(19561)
			.SetWalletRPCPort(19562)
			.SetName("dcr-reg")
			.SetIsDecred(true)
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000925629c5582bbfc3609d71a2f4a887443c80d54a1fe31e95e95d42f3e288945c000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffff7f200000000000000000000000000000000045068653000000000000000000000000000000000000000000000000000000000000000000000000000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff00ffffffff0100000000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac000000000000000001000000000000000000000000000000004d04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b7300");
		}

		protected override NetworkBuilder CreateTestnet()
		{
			return new NetworkBuilder()
			.SetNetworkSet(this)
			.SetConsensus(new Consensus()
			{
				PowLimit = new Target(new uint256("0x000000ffff000000000000000000000000000000000000000000000000000000")),
				ConsensusFactory = DecredConsensusFactory.Instance,
			}
			)
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, [0x0f, 0x21]) // starts with Ts (pubkey hash)
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, [0x0e, 0xfc]) // starts with Tc (script hash)
			.SetBase58Bytes(Base58Type.SECRET_KEY, [0x23, 0x0e]) // starts with Pt
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, [0x04, 0x35, 0x87, 0xd1]) // starts with tpub
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, [0x04, 0x35, 0x83, 0x97]) // starts with tprv
			.SetNetworkStringParser(new DecredAddressStringParser())
			.SetHasher160(_hash160)
			.SetMagic(0xb194aa75)
			.SetPort(19108)
			.SetRPCPort(19109)
			.SetWalletRPCPort(19110)
			.SetName("dcr-test")
			.SetIsDecred(true)
			.SetGenesis("0600000000000000000000000000000000000000000000000000000000000000000000002c0ad603d44a16698ac951fa22aab5e7b30293fa1d0ac72560cdfcc9eabcdfe7000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffff001e002d3101000000000000000000000000808f675b1aa4ae180000000000000000000000000000000000000000000000000000000000000000060000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff00ffffffff010000000000000000000020801679e98561ada96caec2949a5d41c4cab3851eb740d951c10ecbcf265c1fd9000000000000000001ffffffffffffffff00000000ffffffff02000000");
		}

	}
}
