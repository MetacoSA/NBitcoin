using NBitcoin.Crypto;
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
		// header
		const int CURRENT_VERSION = 2;

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
			if(stream.NetworkFormat)
			{
				VarInt txCount = new VarInt(0);
				stream.ReadWrite(ref txCount);
			}
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
			if(GetHash() > Bits.ToUInt256())
				return false;
			return true;
		}

		public override string ToString()
		{
			return GetHash().ToString();
		}
	}


	public class Block : IBitcoinSerializable
	{
		public const uint MAX_BLOCK_SIZE = 1000000;
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

		// memory only
		public List<uint256> vMerkleTree = new List<uint256>();




		public Block()
		{
			SetNull();
		}

		public Block(BlockHeader blockHeader)
		{
			SetNull();
			header = blockHeader;
		}


		public void ReadWrite(BitcoinStream stream)
		{
			using(stream.NetworkFormatScope(false))
			{
				stream.ReadWrite(ref header);
			}
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

		public uint256 ComputeMerkleRoot()
		{
			vMerkleTree.Clear();
			foreach(var tx in Transactions)
				vMerkleTree.Add(tx.GetHash());
			int j = 0;
			for(int nSize = vtx.Count ; nSize > 1 ; nSize = (nSize + 1) / 2)
			{
				for(int i = 0 ; i < nSize ; i += 2)
				{
					int i2 = Math.Min(i + 1, nSize - 1);
					vMerkleTree.Add(Hash(vMerkleTree[j + i],
										 vMerkleTree[j + i2]));
				}
				j += nSize;
			}
			return (vMerkleTree.Count == 0 ? 0 : vMerkleTree.Last());
		}

		private static uint256 Hash(uint256 a, uint256 b)
		{
			return Hashes.Hash256(a.ToBytes().Concat(b.ToBytes()).ToArray());
		}

		public uint256 GetTxHash(int nIndex)
		{
			if(vMerkleTree.Count <= 0)
				throw new InvalidOperationException("BuildMerkleTree must have been called first");
			if(nIndex >= vtx.Count)
				throw new InvalidOperationException("nIndex >= vtx.Length");
			return vMerkleTree[nIndex];
		}

		public List<uint256> GetMerkleBranch(int nIndex)
		{
			if(vMerkleTree.Count == 0)
				ComputeMerkleRoot();
			List<uint256> vMerkleBranch = new List<uint256>();
			int j = 0;
			for(int nSize = vtx.Count ; nSize > 1 ; nSize = (nSize + 1) / 2)
			{
				int i = Math.Min(nIndex ^ 1, nSize - 1);
				vMerkleBranch.Add(vMerkleTree[j + i]);
				nIndex >>= 1;
				j += nSize;
			}
			return vMerkleBranch;
		}

		public static uint256 CheckMerkleBranch(uint256 hash, List<uint256> vMerkleBranch, int nIndex)
		{
			if(nIndex == -1)
				return 0;
			foreach(var otherside in vMerkleBranch)
			{
				if((nIndex & 1) != 0)
					hash = Hash(otherside, hash);
				else
					hash = Hash(hash, otherside);
				nIndex >>= 1;
			}
			return hash;
		}

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
			this.Header.HashMerkleRoot = ComputeMerkleRoot();
		}

		public bool CheckMerkleRoot()
		{
			ComputeMerkleRoot();
			if(vMerkleTree.Count == 0)
				return false;
			return Header.HashMerkleRoot == vMerkleTree.Last();
		}

		public Block CreateNextBlockWithCoinbase(BitcoinAddress address, int height)
		{
			return CreateNextBlockWithCoinbase(address, height, DateTimeOffset.UtcNow);
		}
		public Block CreateNextBlockWithCoinbase(BitcoinAddress address, int height, DateTimeOffset now)
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
				ScriptPubKey = PayToPubkeyHashTemplate.GenerateScriptPubKey(pubkey)
			});
			return block;
		}

		public static Block Parse(string json)
		{
			var formatter = new BlockExplorerFormatter();
			var block = JObject.Parse(json);
			var txs = (JArray)block["tx"];
			Block blk = new Block();
			blk.Header.Bits = new Target((uint)block["bits"]);
			blk.Header.BlockTime = Utils.UnixTimeToDateTime((uint)block["time"]);
			blk.Header.Nonce = (uint)block["nonce"];
			blk.Header.Version = (int)block["ver"];
			blk.Header.HashPrevBlock = new uint256((string)block["prev_block"]);
			blk.Header.HashMerkleRoot = new uint256((string)block["mrkl_root"]);
			foreach(var tx in txs)
			{
				blk.AddTransaction(formatter.Parse((JObject)tx));
			}
			return blk;
		}
	}
}
