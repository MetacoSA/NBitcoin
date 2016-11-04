using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
		const int CURRENT_VERSION = 7;

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
		public uint Time
		{
			get
			{
				return nTime;
			}
			set
			{
				nTime = value;
			}
		}

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

		/// <summary>
		/// Additional POS parameters attached to the header.
		/// This is not used in serialization but only as a reference 
		/// to set POS flags and make calculation.
		/// </summary>
		public PosParameters PosParameters { get; set; }

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
			PosParameters = null;
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
            if (this.nVersion > 6)
                return Hashes.Hash256(this.ToBytes());
            else
                return this.GetPoWHash();
		}

        public uint256 GetPoWHash()
        {
            return HashX13.Instance.Hash(this.ToBytes());
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

		static System.Numerics.BigInteger Pow256 = System.Numerics.BigInteger.Pow(2, 256);
		public bool CheckProofOfWork()
		{
			var bits = Bits.ToBigInteger();
            // todo: change this to use the Network.PowLimit
			if(bits <= System.Numerics.BigInteger.Zero || bits >= Pow256)
				return false;
			// Check proof of work matches claimed amount
			return GetPoWHash() <= Bits.ToUInt256();
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
		/// <param name="consensus">Consensus</param>
		/// <param name="prev">previous block</param>
		public void UpdateTime(Consensus consensus, ChainedBlock prev)
		{
			UpdateTime(DateTimeOffset.UtcNow, consensus, prev);
		}

		/// <summary>
		/// Set time to consensus acceptable value
		/// </summary>
		/// <param name="now">The expected date</param>
		/// <param name="consensus">Consensus</param>
		/// <param name="prev">previous block</param>		
		public void UpdateTime(DateTimeOffset now, Consensus consensus, ChainedBlock prev)
		{
			var nOldTime = this.BlockTime;
			var mtp = prev.GetMedianTimePast() + TimeSpan.FromSeconds(1);
			var nNewTime = mtp > now ? mtp : now;

			if(nOldTime < nNewTime)
				this.BlockTime = nNewTime;

			// Updating time can change work required on testnet:
			if(consensus.PowAllowMinDifficultyBlocks)
				Bits = GetWorkRequired(consensus, prev);
		}

		/// <summary>
		/// Set time to consensus acceptable value
		/// </summary>
		/// <param name="now">The expected date</param>
		/// <param name="network">Network</param>
		/// <param name="prev">previous block</param>		
		public void UpdateTime(DateTimeOffset now, Network network, ChainedBlock prev)
		{
			UpdateTime(now, network.Consensus, prev);
		}

		public Target GetWorkRequired(Network network, ChainedBlock prev)
		{
			return GetWorkRequired(network.Consensus, prev);
		}

		public Target GetWorkRequired(Consensus consensus, ChainedBlock prev)
		{
			return new ChainedBlock(this, null, prev).GetWorkRequired(consensus);
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
			return MerkleNode.GetRoot(this.Transactions.Select(t => t.GetHash()));
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

		public void SetPosParams()
		{
			if (!this.HeaderOnly)
			{
				this.Header.PosParameters = new PosParameters();
			
				if(this.IsProofOfStake())
				{
					this.Header.PosParameters.SetProofOfStake();
					this.Header.PosParameters.StakeTime = this.Transactions[1].Time;
					this.Header.PosParameters.PrevoutStake = this.Transactions[1].Inputs[0].PrevOut;
				}
			}
		}

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref header);
			stream.ReadWrite(ref vtx);

			this.SetPosParams();
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


		public BlockHeader Header => this.header;

	    public ulong GetStakeEntropyBit()
	    {
            // Take last bit of block hash as entropy bit
            ulong nEntropyBit = (this.GetHash().GetLow64() & (ulong)1);

	        //LogPrint("stakemodifier", "GetStakeEntropyBit: hashBlock=%s nEntropyBit=%u\n", GetHash().ToString(), nEntropyBit);
	        return nEntropyBit;
	    }

	    public uint256 GetHash()
		{
			//Block's hash is his header's hash
			//return Hashes.Hash256(header.ToBytes());
		    return this.header.GetHash();
		}

        // ppcoin: two types of block: proof-of-work or proof-of-stake
        public bool IsProofOfStake()
        {
            return this.vtx.Count() > 1 && this.vtx[1].IsCoinStake;
        }

        public bool IsProofOfWork()
        {
            return !this.IsProofOfStake();
        }

        public Tuple<OutPoint, ulong> GetProofOfStake()
        {
            return this.IsProofOfStake() ? 
            new Tuple<OutPoint, ulong>(vtx[1].Inputs.First().PrevOut, vtx[1].LockTime) : 
            new Tuple<OutPoint, ulong>(new OutPoint(), (ulong)0);
        }

        public int Length
		{
			get
			{
				return this.header.ToBytes().Length;
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
			return CheckMerkleRoot() && CheckProofOfWork() && CheckProofOfStake();
		}

		public bool CheckProofOfWork()
		{
            // if POS return true else check POW algo
			return this.IsProofOfStake() || Header.CheckProofOfWork();
		}

        public bool CheckProofOfStake()
        {
            // todo: move this to the full node code.
            // this code is temporary and will move to the full node implementation when its ready
            if (IsProofOfWork())
                return true;

            // Coinbase output should be empty if proof-of-stake block
            if (this.vtx[0].Outputs.Count != 1 || !this.vtx[0].Outputs[0].IsEmpty)
                return false;

            // Second transaction must be coinstake, the rest must not be
            if (!vtx.Any() || !this.vtx[1].IsCoinStake)
                return false;
            for (int i = 2; i < vtx.Count; i++)
                if (vtx[i].IsCoinStake)
                    return false;

            return true;
        }

        public bool CheckMerkleRoot()
		{
			return this.Header.HashMerkleRoot == this.GetMerkleRoot().Hash;
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

	[Flags]
	public enum BlockFlag //block index flags

	{
		BLOCK_PROOF_OF_STAKE = (1 << 0), // is proof-of-stake block
		BLOCK_STAKE_ENTROPY = (1 << 1), // entropy bit for stake modifier
		BLOCK_STAKE_MODIFIER = (1 << 2), // regenerated stake modifier
	};

	public class PosParameters : IBitcoinSerializable
	{
		public int Mint;

		public OutPoint PrevoutStake;

		public uint StakeTime;

		public uint StakeModifier; // hash modifier for proof-of-stake

		public uint256 StakeModifierV2;

		private int flags;

		public BlockFlag Flags
		{
			get
			{
				return (BlockFlag)this.flags;
			}
			set
			{
				this.flags = (int)value;
			}
		}

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref this.flags);
			stream.ReadWrite(ref this.StakeTime);
			stream.ReadWrite(ref this.PrevoutStake);
		}

		public bool IsProofOfWork()
		{
			return !((this.Flags & BlockFlag.BLOCK_PROOF_OF_STAKE) > 0);
		}

		public bool IsProofOfStake()
		{
			return (this.Flags & BlockFlag.BLOCK_PROOF_OF_STAKE) > 0;
		}

		public void SetProofOfStake()
		{
			this.Flags |= BlockFlag.BLOCK_PROOF_OF_STAKE;
		}

		public uint GetStakeEntropyBit()
		{
			return (uint)(this.Flags & BlockFlag.BLOCK_STAKE_ENTROPY) >> 1;
		}

		public bool SetStakeEntropyBit(uint nEntropyBit)
		{
			if (nEntropyBit > 1)
				return false;
			this.Flags |= (nEntropyBit != 0 ? BlockFlag.BLOCK_STAKE_ENTROPY : 0);
			return true;
		}

		public bool GeneratedStakeModifier()
		{
			return (this.Flags & BlockFlag.BLOCK_STAKE_MODIFIER) > 0;
		}

		public void SetStakeModifier(uint modifier, bool fGeneratedStakeModifier)
		{
			this.StakeModifier = modifier;
			if (fGeneratedStakeModifier) this.Flags |= BlockFlag.BLOCK_STAKE_MODIFIER;
		}
	}
}
