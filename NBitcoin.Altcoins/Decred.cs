using System;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System.Collections.Generic;
using NBitcoin.Altcoins.HashX11.Crypto.SHA3;
using NBitcoin.Protocol;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;



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

		private static MerkleNode BlakeHashedMerkleNode(MerkleNode node)
		{
			var right = node.Right ?? node.Left;
			if (node.Left != null && node.Left.Hash != null && right.Hash != null)
			{
				var both = node.Left.Hash.ToBytes().Concat(right.Hash.ToBytes()).ToArray();
				node.Hash = new uint256(Blake256(both, 0, both.Length), 0, 32);
			}
			return node;
		}

		public class DecredConsensus : Consensus
		{
			// DCP0005ActivationHeight is the block height that the DCP0005
			// (Block Header Commitments) deployment activates at.
			public uint DCP0005ActivationHeight { get; set; }

			public static DecredConsensus Instance(ChainName chainName)
			{
				return Decred.Instance.GetNetwork(chainName).Consensus as DecredConsensus;
			}

			// IsBlockHeaderCommitmentsAgendaActive checks if the block header
			// commitment agenda (DCP0005) is active at for specified block and
			// network chain.
			//
			// Prior to the activation of the header commitments agenda, the
			// block's merkle root field is the merkle root of the block's
			// regular transactions alone, while the stake root field is the
			// merkle root of the block's stake transactions.
			//
			// Conversely, when the header commitments agenda is active, the
			// merkle root field of the header is required to be the root of a
			// merkle tree that has the individual merkle roots of the regular
			// and stake transactions as leaves. The block's stake root field
			// then houses the block's header commitments.
			public static bool IsBlockHeaderCommitmentsAgendaActive(DecredBlockHeader blockHeader)
			{
				return blockHeader.Height >= Instance(blockHeader.ChainName).DCP0005ActivationHeight;
			}

			public override Consensus Clone()
			{
				var consensus = new DecredConsensus();
				Fill(consensus);
				consensus.DCP0005ActivationHeight = this.DCP0005ActivationHeight;
				return consensus;
			}
		}

		public class DecredConsensusFactory : ConsensusFactory
		{
			private ChainName chainName;

			public DecredConsensusFactory(ChainName chainName)
			{
				this.chainName = chainName;
			}

			public override bool ParseGetBlockRPCRespose(JObject json, bool withFullTx, out BlockHeader blockHeader, out Block block, out List<uint256> txids)
			{
				// Parse the header first.
				var decredBH = new DecredBlockHeader(chainName);
				decredBH.Bits = new Target(Encoders.Hex.DecodeData(json.Value<string>("bits")));
				decredBH.Version = json.Value<int>("version");
				decredBH.HashMerkleRoot = new uint256(json.Value<string>("merkleroot"));
				decredBH.BlockTime = Utils.UnixTimeToDateTime(json.Value<uint>("time"));
				decredBH.Nonce = json.Value<uint>("nonce");
				// prevblock field does not exist for the genesis.
				if (json.TryGetValue("previousblockhash", StringComparison.Ordinal, out var prevBlockHash))
				{
					decredBH.HashPrevBlock = uint256.Parse(prevBlockHash.ToString());
				}
				else
				{
					decredBH.HashPrevBlock = null;
				}

				// Load decred-specific header properties
				decredBH.BlockSize = json.Value<uint>("nonce");
				decredBH.ExtraData = Encoders.Hex.DecodeData(json.Value<string>("extradata"));
				decredBH.FinalState = Encoders.Hex.DecodeData(json.Value<string>("finalstate"));
				decredBH.FreshStake = json.Value<byte>("freshstake");
				decredBH.Height = json.Value<uint>("height");
				decredBH.PoolSize = json.Value<uint>("poolsize");
				decredBH.Revocations = json.Value<byte>("revocations");
				decredBH.SBits = json.Value<long>("sbits");
				decredBH.StakeRoot = new uint256(json.Value<string>("stakeroot"));
				decredBH.StakeVersion = json.Value<uint>("stakeversion");
				decredBH.VoteBits = json.Value<ushort>("votebits");
				decredBH.Voters = json.Value<ushort>("voters");

				blockHeader = decredBH;
				block = null; // overwritten below if the rpc response includes full txs
				txids = new List<uint256>();

				if (withFullTx)
				{
					var decredBlock = new DecredBlock(decredBH);
					decredBlock.Transactions = new List<Transaction>();
					foreach (var txInfo in json.Value<JArray>("rawtx"))
					{
						var tx = CreateTransaction();
						tx.FromBytes(Encoders.Hex.DecodeData(txInfo.Value<string>("hex")));
						decredBlock.Transactions.Add(tx);
						txids.Add(tx.GetHash());
					}

					decredBlock.STransactions = new List<Transaction>();
					foreach (var txInfo in json.Value<JArray>("rawstx") ?? [])
					{
						var tx = CreateTransaction();
						tx.FromBytes(Encoders.Hex.DecodeData(txInfo.Value<string>("hex")));
						decredBlock.STransactions.Add(tx);
						// TODO: append tx ids from json.Value<JArray>("rawstx") too?
					}
					block = decredBlock;
				}
				else
				{
					foreach (var tx in json.Value<JArray>("tx"))
					{
						txids.Add(uint256.Parse(tx.ToString()));
					}
					// TODO: append tx ids from json.Value<JArray>("stx") too?
				}

				return true;
			}

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
					SupportWitness = true; // i.e. witness tx, not segwit addresses which we don't support
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
				return new DecredBlockHeader(chainName);
			}

			public override Block CreateBlock()
			{
				return new DecredBlock((DecredBlockHeader)CreateBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new DecredTransaction(chainName);
			}

			public override TxOut CreateTxOut()
			{
				return new DecredTxOut(chainName);
			}

			public override TxIn CreateTxIn()
			{
				return new DecredTxIn(chainName);
			}
		}

		public class DecredTxIn : TxIn
		{
			private ChainName chainName;

			protected byte nPrevOutTree = 0;

			public byte PrevOutTree
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

			public DecredTxIn(ChainName chainName) : base()
			{
				this.chainName = chainName;
			}

			public override TxIn Clone()
			{
				var txIn = new DecredTxIn(chainName);
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
				return DecredConsensus.Instance(chainName).ConsensusFactory;
			}
		}

		public class DecredTxOut : TxOut
		{
			private ChainName chainName;
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

			public DecredTxOut(ChainName chainName)
			{
				this.chainName = chainName;
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return DecredConsensus.Instance(chainName).ConsensusFactory;
			}
		}

		public class DecredTransaction : Transaction
		{
			private ChainName chainName;

			public enum TxSerializeType : ushort
			{
				// Full indicates a transaction be serialized with the prefix
				// and all witness data.
				Full = 0,

				// NoWitness indicates a transaction be serialized with only the
				// prefix.
				NoWitness = 1,

				// OnlyWitness indicates a transaction be serialized with only
				// the witness data.
				OnlyWitness = 2,
			}

			protected TxSerializeType nSerializeType = 0;

			public TxSerializeType SerializeType
			{
				get
				{
					return nSerializeType;
				}
				set
				{
					nSerializeType = value;
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

			public override bool HasWitness => SerializeType == TxSerializeType.Full;

			public DecredTransaction(ChainName chainName) : base()
			{
				this.chainName = chainName;
			}

			/// GetWitnessOnlyHash returns the hash of the witness portion of
			/// the tx alone. This differs from GetWitHash which returns the
			/// hash of the full tx (prefix + witness portions).
			/// 
			/// TODO: Use this to override GetWitHash?
			public virtual uint256 GetWitnessOnlyHash()
			{
				using (var hs = this.CreateHashStream())
				{
					var stream = new BitcoinStream(hs, true);
					this.serialize(stream, TxSerializeType.OnlyWitness);
					return hs.GetHash();
				}
			}

			// GetFullHash generates the hash for the transaction prefix ||
			// witness. It first obtains the hashes for both the transaction
			// prefix and witness, then concatenates them and hashes the result.
			public uint256 GetFullHash()
			{
				var prefixHash = this.GetHash();
				var witnessHash = this.GetWitnessOnlyHash();
				return BlakeHashedMerkleNode(MerkleNode.GetRoot([prefixHash, witnessHash])).Hash;
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				if (stream.Serializing)
				{
					var witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0) &&
									stream.ProtocolCapabilities.SupportWitness;
					var sType = witSupported ? TxSerializeType.Full : TxSerializeType.NoWitness;
					if (sType == TxSerializeType.Full && !this.HasWitness)
					{
						// We can only serialize prefix because witness data is
						// not available.
						sType = TxSerializeType.NoWitness;
					}
					this.serialize(stream, sType);
				}
				else
				{
					this.deserialize(stream);
				}
			}

			private void serialize(BitcoinStream stream, TxSerializeType serializeType)
			{
				// The serialized encoding of the version includes the real transaction
				// version in the lower 16 bits and the transaction serialization type
				// in the upper 16 bits.
				ushort version = (ushort)this.Version, sType = (ushort)serializeType;
				stream.ReadWrite(ref version);
				stream.ReadWrite(ref sType);

				switch (serializeType)
				{
					case TxSerializeType.NoWitness:
						this.encodePrefix(stream);
						break;

					case TxSerializeType.OnlyWitness:
						this.encodeWitness(stream);
						break;

					case TxSerializeType.Full:
						this.encodePrefix(stream);
						this.encodeWitness(stream);
						break;
				}
			}

			private void encodePrefix(BitcoinStream stream)
			{
				var txInCount = (ulong)this.Inputs.Count;
				stream.ReadWriteAsVarInt(ref txInCount);

				for (int i = 0; i < this.Inputs.Count; i++)
				{
					DecredTxIn input = (DecredTxIn)this.Inputs[i];
					stream.ReadWrite(input.PrevOut); // prevout (hash and index)
					stream.ReadWriteBytes([input.PrevOutTree]); // prevout tree
					stream.ReadWrite(input.Sequence); // sequence
				}

				var txOutCount = (uint)this.Outputs.Count;
				stream.ReadWriteAsVarInt(ref txOutCount);

				for (int i = 0; i < txOutCount; i++)
				{
					DecredTxOut output = (DecredTxOut)this.Outputs[i];
					stream.ReadWrite(output.Value); // value
					stream.ReadWrite((ushort)output.Version); // version
					stream.ReadWrite(output.ScriptPubKey); // script
				}

				stream.ReadWrite(this.LockTime.Value); // locktime
				stream.ReadWrite(this.Expiry); // expiry
			}

			private void encodeWitness(BitcoinStream stream)
			{
				var txInCount = (uint)this.Inputs.Count;
				stream.ReadWriteAsVarInt(ref txInCount);
				for (int i = 0; i < txInCount; i++)
				{
					DecredTxIn input = (DecredTxIn)this.Inputs[i];
					stream.ReadWrite(input.Value); // ValueIn
					stream.ReadWrite(input.Height); // BlockHeight
					stream.ReadWrite(input.Index); // BlockIndex
					stream.ReadWrite(input.ScriptSig); // SignatureScript
				}
			}

			private void deserialize(BitcoinStream stream)
			{
				// The serialized encoding of the version includes the real
				// transaction version in the lower 16 bits and the transaction
				// serialization type in the upper 16 bits.
				var version = new byte[4];
				stream.ReadWriteBytes(version, 0, 4);
				this.Version = BitConverter.ToUInt16(version, 0);
				this.SerializeType = (TxSerializeType)BitConverter.ToUInt16(version, 2);

				switch (this.SerializeType)
				{
					case TxSerializeType.NoWitness:
						this.decodePrefix(stream);
						break;

					case TxSerializeType.OnlyWitness:
						this.decodeWitness(stream, false);
						break;

					case TxSerializeType.Full:
						this.decodePrefix(stream);
						this.decodeWitness(stream, true);
						break;
				}
			}

			private void decodePrefix(BitcoinStream stream)
			{
				// TxIns.
				uint txInCount = 0;
				stream.ReadWriteAsVarInt(ref txInCount);
				for (int i = 0; i < txInCount; i++)
				{
					// prevout (hash and index)
					OutPoint prevOut = new();
					stream.ReadWrite(ref prevOut);
					// prevout tree
					var prevOutTreeB = new byte[1];
					stream.ReadWriteBytes(prevOutTreeB);
					// sequence
					uint sequence = new();
					stream.ReadWrite(ref sequence);

					var input = new DecredTxIn(chainName)
					{
						PrevOut = prevOut,
						PrevOutTree = prevOutTreeB[0],
						Sequence = sequence
					};
					this.Inputs.Add(input);
				}

				// TxOuts.
				uint txOutCount = 0;
				stream.ReadWriteAsVarInt(ref txOutCount);
				for (int i = 0; i < txOutCount; i++)
				{
					// value
					ulong value = new();
					stream.ReadWrite(ref value);
					// version
					ushort version = new();
					stream.ReadWrite(ref version);
					// script
					var script = new Script();
					stream.ReadWrite(ref script);

					var output = new DecredTxOut(chainName)
					{
						Value = new Money(value),
						Version = version,
						ScriptPubKey = script
					};
					this.Outputs.Add(output);
				}

				// Locktime and expiry.
				uint locktime = 0, expiry = 0;
				stream.ReadWrite(ref locktime);
				stream.ReadWrite(ref expiry);
				this.LockTime = locktime;
				this.Expiry = expiry;
			}

			private void decodeWitness(BitcoinStream stream, bool isFull)
			{
				// Read in the number of signature scripts.
				uint witnessCount = 0;
				stream.ReadWriteAsVarInt(ref witnessCount);

				if (!isFull)
				{
					// Witness only; generate the TxIn list and fill out only
					// the witness data.
					for (int i = 0; i < witnessCount; i++)
					{
						// ValueIn.
						ulong valueIn = new();
						stream.ReadWrite(ref valueIn);
						// BlockHeight.
						uint blockHeight = new();
						stream.ReadWrite(ref blockHeight);
						// BlockIndex. uint32
						uint blockIndex = new();
						stream.ReadWrite(ref blockIndex);
						// Signature script.
						var script = new Script();
						stream.ReadWrite(ref script);

						var input = new DecredTxIn(chainName);
						input.Value = valueIn;
						input.Height = blockHeight;
						input.Index = blockIndex;
						input.ScriptSig = script;
						this.Inputs.Add(input);
					}
					return;
				}

				// We're decoding witnesses from a full transaction, so check to
				// make sure that the witness count is the same as the number of
				// TxIns we currently have, then fill in the signature scripts.
				if (witnessCount != this.Inputs.Count)
					throw new Exception($"non equal witness and prefix txin quantities (witness {witnessCount}, prefix {this.Inputs.Count})");

				// Read in the witnesses, and copy them into the already
				// generated Inputs.
				for (int i = 0; i < witnessCount; i++)
				{
					// ValueIn.
					ulong valueIn = new();
					stream.ReadWrite(ref valueIn);
					// BlockHeight.
					uint blockHeight = new();
					stream.ReadWrite(ref blockHeight);
					// BlockIndex. uint32
					uint blockIndex = new();
					stream.ReadWrite(ref blockIndex);
					// Signature script.
					var script = new Script();
					stream.ReadWrite(ref script);

					var input = (DecredTxIn)this.vin[i];
					input.Value = valueIn;
					input.Height = blockHeight;
					input.Index = blockIndex;
					input.ScriptSig = script;
				}
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(Blake256, 32);
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return DecredConsensus.Instance(chainName).ConsensusFactory;
			}

			public override uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
			{
				// TODO: Correctly handle hash types.
				var hs = this.CreateHashStream();
				var stream = new BitcoinStream(hs, true);
				this.serialize(stream, TxSerializeType.NoWitness);
				var prefixHash = hs.GetHash();

				hs = this.CreateHashStream();
				stream = new BitcoinStream(hs, true);
				this.serializeSpecificInputWitnessData(stream, scriptCode, nIn);
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

			private void serializeSpecificInputWitnessData(BitcoinStream stream, Script scriptCode, int nIn)
			{
				// Serialize the version and serialization type values. This is
				// an unorthodox serialization, so don't use any of the known
				// serialization types.
				var knownSerializationTypes = (ushort[])Enum.GetValues(typeof(TxSerializeType));
				var randomSerializationType = knownSerializationTypes.Max() + 1;
				ushort version = (ushort)this.Version, sType = (ushort)randomSerializationType;
				stream.ReadWrite(ref version);
				stream.ReadWrite(ref sType);

				// Serialize inputs witness data, using an empty script for
				// inputs at index != nIn.
				var txInCount = (uint)this.Inputs.Count;
				stream.ReadWriteAsVarInt(ref txInCount);
				for (int i = 0; i < txInCount; i++)
				{
					var script = new Script();
					if (i == nIn)
						script = scriptCode;
					stream.ReadWrite(ref script);
				}
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class DecredBlockHeader : BlockHeader
		{
			private ChainName chainName;
			public ChainName ChainName => chainName;

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

			public DecredBlockHeader(ChainName chainName) : base()
			{
				this.chainName = chainName;
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
		public class DecredBlock : Block
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

			public DecredBlock(DecredBlockHeader header) : base(header) { }

			/// GetMerkleRoot calculates and returns a merkle root depending on
			/// the result of the header commitments agenda vote. In particular,
			/// before the agenda is active, it returns the merkle root of the
			/// regular transaction tree. Once the agenda is active, it returns
			/// the combined merkle root for the regular and stake transaction
			/// trees in accordance with DCP0005.
			public override MerkleNode GetMerkleRoot()
			{
				var isDCP0005Active = DecredConsensus.IsBlockHeaderCommitmentsAgendaActive(Header as DecredBlockHeader);
				var txFullHash = (Transaction tx) => (tx as DecredTransaction).GetFullHash();
				if (!isDCP0005Active)
					return BlakeHashedMerkleNode(MerkleNode.GetRoot(Transactions.Select(txFullHash)));

				var regularRoot = MerkleNode.GetRoot(Transactions.Select(txFullHash));
				var stakeRoot = MerkleNode.GetRoot(STransactions.Select(txFullHash));
				return BlakeHashedMerkleNode(new MerkleNode(regularRoot, stakeRoot));
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				var chainName = (Header as DecredBlockHeader).ChainName;
				if (!stream.Serializing)
				{
					DecredBlockHeader header = new DecredBlockHeader(chainName);
					stream.ReadWrite(ref header);
					this.Header = header;
					uint txCount = 0;
					stream.ReadWriteAsVarInt(ref txCount);
					for (int i = 0; i < txCount; i++)
					{
						DecredTransaction tx = new DecredTransaction(chainName);
						stream.ReadWrite(ref tx);
						this.Transactions.Add(tx);
					}
					stream.ReadWriteAsVarInt(ref txCount);
					for (int i = 0; i < txCount; i++)
					{
						DecredTransaction tx = new DecredTransaction(chainName);
						stream.ReadWrite(ref tx);
						this.STransactions.Add(tx);
					}
				}
				else
				{
					stream.ReadWrite(this.Header);
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
				return DecredConsensus.Instance((Header as DecredBlockHeader).ChainName).ConsensusFactory;
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
			.SetConsensus(new DecredConsensus()
			{
				PowLimit = new Target(new uint256("0x00000000ffff0000000000000000000000000000000000000000000000000000")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000243845fb2fb3d8f20ddfeb"),
				PowTargetTimespan = TimeSpan.FromMinutes(144 * 5), // 144 blocks
				PowTargetSpacing = TimeSpan.FromMinutes(5), // TargetTimePerBlock
				ConsensusFactory = new DecredConsensusFactory(ChainName.Mainnet),
				CoinbaseMaturity = 256,

				// Activation block heights for relevant DCPs.
				DCP0005ActivationHeight = 431488,
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
			.AddAlias("dcr-mainnet")
			.SetIsDecred(true)
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000000dc101dfc3c6a2eb10ca0c5374e10d28feb53f7eabcc850511ceadb99174aa66000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffff011b00c2eb0b000000000000000000000000a0d7b856000000000000000000000000000000000000000000000000000000000000000000000000000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff00ffffffff010000000000000000000020801679e98561ada96caec2949a5d41c4cab3851eb740d951c10ecbcf265c1fd9000000000000000001ffffffffffffffff00000000ffffffff02000000");
		}

		protected override NetworkBuilder CreateRegtest()
		{
			return new NetworkBuilder()
			.SetNetworkSet(this)
			.SetConsensus(new DecredConsensus()
			{
				PowLimit = new Target(new uint256("0x7fffff0000000000000000000000000000000000000000000000000000000000")),
				PowTargetTimespan = TimeSpan.FromSeconds(8 * 1), // 8 blocks
				PowTargetSpacing = TimeSpan.FromSeconds(1), // TargetTimePerBlock
				ConsensusFactory = new DecredConsensusFactory(ChainName.Regtest),
				CoinbaseMaturity = 16,

				// Activation block heights for relevant DCPs.
				DCP0005ActivationHeight = 1, // always active after genesis block, for simnet
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
			.AddAlias("dcr-simnet")
			.SetIsDecred(true)
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000925629c5582bbfc3609d71a2f4a887443c80d54a1fe31e95e95d42f3e288945c000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffff7f200000000000000000000000000000000045068653000000000000000000000000000000000000000000000000000000000000000000000000000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff00ffffffff0100000000000000000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac000000000000000001000000000000000000000000000000004d04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b7300");
		}

		protected override NetworkBuilder CreateTestnet()
		{
			return new NetworkBuilder()
			.SetNetworkSet(this)
			.SetConsensus(new DecredConsensus()
			{
				PowLimit = new Target(new uint256("0x000000ffff000000000000000000000000000000000000000000000000000000")),
				PowTargetTimespan = TimeSpan.FromMinutes(144 * 2), // 144 blocks
				PowTargetSpacing = TimeSpan.FromMinutes(2), // TargetTimePerBlock
				ConsensusFactory = new DecredConsensusFactory(ChainName.Testnet),
				CoinbaseMaturity = 16,

				// Activation block heights for relevant DCPs.
				DCP0005ActivationHeight = 323328,
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
			.AddAlias("dcr-testnet")
			.SetIsDecred(true)
			.SetGenesis("0600000000000000000000000000000000000000000000000000000000000000000000002c0ad603d44a16698ac951fa22aab5e7b30293fa1d0ac72560cdfcc9eabcdfe7000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ffff001e002d3101000000000000000000000000808f675b1aa4ae180000000000000000000000000000000000000000000000000000000000000000060000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff00ffffffff010000000000000000000020801679e98561ada96caec2949a5d41c4cab3851eb740d951c10ecbcf265c1fd9000000000000000001ffffffffffffffff00000000ffffffff02000000");
		}

	}
}
