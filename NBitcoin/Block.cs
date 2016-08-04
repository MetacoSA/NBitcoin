using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	/** Nodes collect new transactions into a block, hash them into a hash tree,
	 * and scan through nonce values to make the block's hash satisfy proof-of-work
	 * requirements.  When they solve the proof-of-work, they broadcast the block
	 * to everyone and the block is added to the block chain.  The first transaction
	 * in the block is a special one that creates a new coin owned by the creator
	 * of the block.
	 */
	public class BlockHeader : IBitcoinSerializable
	{
		internal const int Size = 80;

		public static BlockHeader Parse(string hex)
		{
			return new BlockHeader(Encoders.Hex.DecodeData(hex));
		}

		public BlockHeader(string hex)
			: this(Encoders.Hex.DecodeData(hex))
		{

		}

		public BlockHeader(byte[] bytes)
		{
			this.ReadWrite(bytes);
		}


		// header
		const int CURRENT_VERSION = 3;

		uint256 hashPrevBlock;

		public uint256 HashPrevBlock
		{
			get
			{
				return hashPrevBlock;
			}
			set
			{
				hashPrevBlock = value;
			}
		}
		uint256 hashMerkleRoot;

		uint nTime;
		uint nBits;

		public Target Bits
		{
			get
			{
				return nBits;
			}
			set
			{
				nBits = value;
			}
		}

		int nVersion;

		public int Version
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

		uint nNonce;

		public uint Nonce
		{
			get
			{
				return nNonce;
			}
			set
			{
				nNonce = value;
			}
		}
		public uint256 HashMerkleRoot
		{
			get
			{
				return hashMerkleRoot;
			}
			set
			{
				hashMerkleRoot = value;
			}
		}

		public BlockHeader()
		{
			SetNull();
		}


		internal void SetNull()
		{
			nVersion = CURRENT_VERSION;
			hashPrevBlock = 0;
			hashMerkleRoot = 0;
			nTime = 0;
			nBits = 0;
			nNonce = 0;
		}

		public bool IsNull
		{
			get
			{
				return (nBits == 0);
			}
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref nVersion);
			stream.ReadWrite(ref hashPrevBlock);
			stream.ReadWrite(ref hashMerkleRoot);
			stream.ReadWrite(ref nTime);
			stream.ReadWrite(ref nBits);
			stream.ReadWrite(ref nNonce);
		}

		#endregion

		public uint256 GetHash()
		{
			return Hashes.Hash256(this.ToBytes());
		}

		public DateTimeOffset BlockTime
		{
			get
			{
				return Utils.UnixTimeToDateTime(nTime);
			}
			set
			{
				this.nTime = Utils.DateTimeToUnixTime(value);
			}
		}

		public bool CheckProofOfWork()
		{
			// Check proof of work matches claimed amount
			return GetHash() <= Bits.ToUInt256();
		}

		public override string ToString()
		{
			return GetHash().ToString();
		}

		/// <summary>
		/// Set time to consensus acceptable value
		/// </summary>
		/// <param name="network">Network</param>
		/// <param name="prev">previous block</param>
		public void UpdateTime(Network network, ChainedBlock prev)
		{
			UpdateTime(DateTimeOffset.UtcNow, network, prev);
		}

		/// <summary>
		/// Set time to consensus acceptable value
		/// </summary>
		/// <param name="now">The expected date</param>
		/// <param name="network">Network</param>
		/// <param name="prev">previous block</param>		
		public void UpdateTime(DateTimeOffset now, Network network, ChainedBlock prev)
		{
			var nOldTime = this.BlockTime;
			var mtp = prev.GetMedianTimePast() + TimeSpan.FromSeconds(1);
			var nNewTime = mtp > now ? mtp : now;

			if(nOldTime < nNewTime)
				this.BlockTime = nNewTime;

			// Updating time can change work required on testnet:
			if(network.Consensus.PowAllowMinDifficultyBlocks)
				Bits = GetWorkRequired(network, prev);
		}

		public Target GetWorkRequired(Network network, ChainedBlock prev)
		{
			return new ChainedBlock(this, null, prev).GetWorkRequired(network);
		}
	}


	public class Block : IBitcoinSerializable
	{
		//FIXME: it needs to be changed when Gavin Andresen increase the max block size. 
		public const uint MAX_BLOCK_SIZE = 1000 * 1000;

		BlockHeader header = new BlockHeader();
		// network and disk
		List<Transaction> vtx = new List<Transaction>();

		public List<Transaction> Transactions
		{
			get
			{
				return vtx;
			}
			set
			{
				vtx = value;
			}
		}

		public MerkleNode GetMerkleRoot()
		{
			return MerkleNode.GetRoot(Transactions.Select(t => t.GetHash()));
		}


		public Block()
		{
			SetNull();
		}

		public Block(BlockHeader blockHeader)
		{
			SetNull();
			header = blockHeader;
		}
		public Block(byte[] bytes)
		{
			this.ReadWrite(bytes);
		}


		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref header);
			stream.ReadWrite(ref vtx);
		}

		public bool HeaderOnly
		{
			get
			{
				return vtx == null || vtx.Count == 0;
			}
		}


		void SetNull()
		{
			header.SetNull();
			vtx.Clear();
		}

		public BlockHeader Header
		{
			get
			{
				return header;
			}
		}


		//public MerkleBranch GetMerkleBranch(int txIndex)
		//{
		//	if(vMerkleTree.Count == 0)
		//		ComputeMerkleRoot();
		//	List<uint256> vMerkleBranch = new List<uint256>();
		//	int j = 0;
		//	for(int nSize = vtx.Count ; nSize > 1 ; nSize = (nSize + 1) / 2)
		//	{
		//		int i = Math.Min(txIndex, nSize - 1);
		//		vMerkleBranch.Add(vMerkleTree[j + i]);
		//		txIndex >>= 1;
		//		j += nSize;
		//	}
		//	return new MerkleBranch(vMerkleBranch);
		//}

		//public static uint256 CheckMerkleBranch(uint256 hash, List<uint256> vMerkleBranch, int nIndex)
		//{
		//	if(nIndex == -1)
		//		return 0;
		//	foreach(var otherside in vMerkleBranch)
		//	{
		//		if((nIndex & 1) != 0)
		//			hash = Hash(otherside, hash);
		//		else
		//			hash = Hash(hash, otherside);
		//		nIndex >>= 1;
		//	}
		//	return hash;
		//}

		//std::vector<uint256> GetMerkleBranch(int nIndex) const;
		//static uint256 CheckMerkleBranch(uint256 hash, const std::vector<uint256>& vMerkleBranch, int nIndex);
		//void print() const;

		public uint256 GetHash()
		{
			//Block's hash is his header's hash
			return Hashes.Hash256(header.ToBytes());
		}

		public int Length
		{
			get
			{
				return header.ToBytes().Length;
			}
		}

		public void ReadWrite(byte[] array, int startIndex)
		{
			var ms = new MemoryStream(array);
			ms.Position += startIndex;
			BitcoinStream bitStream = new BitcoinStream(ms, false);
			ReadWrite(bitStream);
		}

		public Transaction AddTransaction(Transaction tx)
		{
			Transactions.Add(tx);
			return tx;
		}

		public void UpdateMerkleRoot()
		{
			this.Header.HashMerkleRoot = GetMerkleRoot().Hash;
		}

		/// <summary>
		/// Check proof of work and merkle root
		/// </summary>
		/// <returns></returns>
		public bool Check()
		{
			return CheckMerkleRoot() && Header.CheckProofOfWork();
		}

		public bool CheckProofOfWork()
		{
			return Header.CheckProofOfWork();
		}

		public bool CheckMerkleRoot()
		{
			return Header.HashMerkleRoot == GetMerkleRoot().Hash;
		}

		public Block CreateNextBlockWithCoinbase(BitcoinAddress address, int height)
		{
			return CreateNextBlockWithCoinbase(address, height, DateTimeOffset.UtcNow);
		}
		public Block CreateNextBlockWithCoinbase(BitcoinAddress address, int height, DateTimeOffset now)
		{
			if(address == null)
				throw new ArgumentNullException("address");
			Block block = new Block();
			block.Header.Nonce = RandomUtils.GetUInt32();
			block.Header.HashPrevBlock = this.GetHash();
			block.Header.BlockTime = now;
			var tx = block.AddTransaction(new Transaction());
			tx.AddInput(new TxIn()
			{
				ScriptSig = new Script(Op.GetPushOp(RandomUtils.GetBytes(30)))
			});
			tx.Outputs.Add(new TxOut(address.Network.GetReward(height), address)
			{
				Value = address.Network.GetReward(height)
			});
			return block;
		}

		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value)
		{
			return CreateNextBlockWithCoinbase(pubkey, value, DateTimeOffset.UtcNow);
		}
		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value, DateTimeOffset now)
		{
			Block block = new Block();
			block.Header.Nonce = RandomUtils.GetUInt32();
			block.Header.HashPrevBlock = this.GetHash();
			block.Header.BlockTime = now;
			var tx = block.AddTransaction(new Transaction());
			tx.AddInput(new TxIn()
			{
				ScriptSig = new Script(Op.GetPushOp(RandomUtils.GetBytes(30)))
			});
			tx.Outputs.Add(new TxOut()
			{
				Value = value,
				ScriptPubKey = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(pubkey)
			});
			return block;
		}

		public static Block ParseJson(string json)
		{
			var formatter = new BlockExplorerFormatter();
			var block = JObject.Parse(json);
			var txs = (JArray)block["tx"];
			Block blk = new Block();
			blk.Header.Bits = new Target((uint)block["bits"]);
			blk.Header.BlockTime = Utils.UnixTimeToDateTime((uint)block["time"]);
			blk.Header.Nonce = (uint)block["nonce"];
			blk.Header.Version = (int)block["ver"];
			blk.Header.HashPrevBlock = uint256.Parse((string)block["prev_block"]);
			blk.Header.HashMerkleRoot = uint256.Parse((string)block["mrkl_root"]);
			foreach(var tx in txs)
			{
				blk.AddTransaction(formatter.Parse((JObject)tx));
			}
			return blk;
		}

		public static Block Parse(string hex)
		{
			return new Block(Encoders.Hex.DecodeData(hex));
		}

		public MerkleBlock Filter(params uint256[] txIds)
		{
			return new MerkleBlock(this, txIds);
		}

		public MerkleBlock Filter(BloomFilter filter)
		{
			return new MerkleBlock(this, filter);
		}
	}
}
