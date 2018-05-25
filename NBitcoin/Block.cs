﻿using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.RPC;
#if !NOJSONNET
using Newtonsoft.Json.Linq;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NBitcoin
{
	/// <summary>
	/// Nodes collect new transactions into a block, hash them into a hash tree,
	/// and scan through nonce values to make the block's hash satisfy proof-of-work
	/// requirements.  When they solve the proof-of-work, they broadcast the block
	/// to everyone and the block is added to the block chain.  The first transaction
	/// in the block is a special one that creates a new coin owned by the creator
	/// of the block.
	/// </summary>
	public class BlockHeader : IBitcoinSerializable
	{
		internal const int Size = 80;


		public static BlockHeader Parse(string hex, Network network)
		{
			if(network == null)
				throw new ArgumentNullException(nameof(network));
			return Parse(hex, network.Consensus.ConsensusFactory);
		}

		public static BlockHeader Parse(string hex, Consensus consensus)
		{
			if(consensus == null)
				throw new ArgumentNullException(nameof(consensus));
			return Parse(hex, consensus.ConsensusFactory);
		}

		public static BlockHeader Parse(string hex, ConsensusFactory consensusFactory)
		{
			if(consensusFactory == null)
				throw new ArgumentNullException(nameof(consensusFactory));
			return new BlockHeader(Encoders.Hex.DecodeData(hex), consensusFactory);
		}


		[Obsolete("Use Parse(string hex, Network|Consensus|ConsensusFactory) instead")]
		public static BlockHeader Parse(string hex)
		{
			return Parse(hex, Consensus.Main.ConsensusFactory);
		}

		[Obsolete("You should instantiate BlockHeader from ConsensusFactory.CreateBlockHeader")]
		public BlockHeader()
		{
			SetNull();
		}

		public BlockHeader(string hex, Network network)
			: this(hex, network?.Consensus?.ConsensusFactory ?? throw new ArgumentNullException(nameof(network)))
		{

		}

		public BlockHeader(string hex, Consensus consensus)
			: this(hex, consensus?.ConsensusFactory ?? throw new ArgumentNullException(nameof(consensus)))
		{

		}

		public BlockHeader(string hex, ConsensusFactory consensusFactory)
		{
			if(hex == null)
				throw new ArgumentNullException(nameof(hex));
			if(consensusFactory == null)
				throw new ArgumentNullException(nameof(consensusFactory));
			BitcoinStream bs = new BitcoinStream(Encoders.Hex.DecodeData(hex))
			{
				ConsensusFactory = consensusFactory
			};
			this.ReadWrite(bs);
		}

		[Obsolete("Use new BlockHeader(string hex, Network|Consensus|ConsensusFactory) instead")]
		public BlockHeader(string hex)
			: this(Encoders.Hex.DecodeData(hex))
		{

		}


		public BlockHeader(byte[] data, Network network)
			: this(data, network?.Consensus?.ConsensusFactory ?? throw new ArgumentNullException(nameof(network)))
		{

		}

		public BlockHeader(byte[] data, Consensus consensus)
			: this(data, consensus?.ConsensusFactory ?? throw new ArgumentNullException(nameof(consensus)))
		{

		}

		public BlockHeader(byte[] data, ConsensusFactory consensusFactory)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));
			if(consensusFactory == null)
				throw new ArgumentNullException(nameof(consensusFactory));
			BitcoinStream bs = new BitcoinStream(data)
			{
				ConsensusFactory = consensusFactory
			};
			this.ReadWrite(bs);
		}


		[Obsolete("Use new BlockHeader(byte[] hex, Network|Consensus|ConsensusFactory) instead")]
		public BlockHeader(byte[] bytes)
		{
			this.ReadWrite(bytes);
		}


		// header
		const int CURRENT_VERSION = 3;

		protected uint256 hashPrevBlock;

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
		protected uint256 hashMerkleRoot;

		protected uint nTime;
		protected uint nBits;

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

		protected int nVersion;

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

		protected uint nNonce;

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

		public virtual void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref nVersion);
			stream.ReadWrite(ref hashPrevBlock);
			stream.ReadWrite(ref hashMerkleRoot);
			stream.ReadWrite(ref nTime);
			stream.ReadWrite(ref nBits);
			stream.ReadWrite(ref nNonce);

		}


		#endregion


		public virtual uint256 GetPoWHash()
		{
			return GetHash();
		}

		public virtual uint256 GetHash()
		{
			uint256 h = null;
			var hashes = _Hashes;
			if(hashes != null)
			{
				h = hashes[0];
			}
			if(h != null)
				return h;

			using(HashStream hs = new HashStream())
			{
				this.ReadWrite(new BitcoinStream(hs, true));
				h = hs.GetHash();
			}

			hashes = _Hashes;
			if(hashes != null)
			{
				hashes[0] = h;
			}
			return h;
		}



		[Obsolete("Call PrecomputeHash(true, true) instead")]
		public void CacheHashes()
		{
			PrecomputeHash(true, true);
		}

		/// <summary>
		/// Precompute the block header hash so that later calls to GetHash() will returns the precomputed hash
		/// </summary>
		/// <param name="invalidateExisting">If true, the previous precomputed hash is thrown away, else it is reused</param>
		/// <param name="lazily">If true, the hash will be calculated and cached at the first call to GetHash(), else it will be immediately</param>
		public void PrecomputeHash(bool invalidateExisting, bool lazily)
		{
			_Hashes = invalidateExisting ? new uint256[1] : _Hashes ?? new uint256[1];
			if(!lazily && _Hashes[0] == null)
				_Hashes[0] = GetHash();
		}


		uint256[] _Hashes;

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

		static BigInteger Pow256 = BigInteger.ValueOf(2).Pow(256);
		public bool CheckProofOfWork()
		{
			var bits = Bits.ToBigInteger();
			if(bits.CompareTo(BigInteger.Zero) <= 0 || bits.CompareTo(Pow256) >= 0)
				return false;
			// Check proof of work matches claimed amount
			return GetPoWHash() <= Bits.ToUInt256();
		}

		[Obsolete]
		public bool CheckProofOfWork(Consensus consensus)
		{
			return CheckProofOfWork();
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
		private BlockHeader header;

		//FIXME: it needs to be changed when Gavin Andresen increase the max block size. 
		public const uint MAX_BLOCK_SIZE = 1000 * 1000;

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


		[Obsolete("Should use Network.Consensus.ConsensusFactory.CreateNewBlock()")]
		public Block() : this(Consensus.Main.ConsensusFactory.CreateBlockHeader())
		{
		}

		[Obsolete("Should use ConsensusFactories")]
		public Block(BlockHeader blockHeader)
		{
			if(blockHeader == null)
				throw new ArgumentNullException(nameof(blockHeader));
			SetNull();
			header = blockHeader;
		}

		[Obsolete("Should use Block.Load outside of ConsensusFactories")]
		public Block(byte[] bytes, Network network) : this(bytes, network.Consensus.ConsensusFactory)
		{

		}

		[Obsolete("Should use Block.Load outside of ConsensusFactories")]
		public Block(byte[] bytes, Consensus consensus) : this(bytes, consensus.ConsensusFactory)
		{

		}

		[Obsolete("Should use Block.Load outside of ConsensusFactories")]
		public Block(byte[] bytes, ConsensusFactory consensusFactory)
		{
			BitcoinStream stream = new BitcoinStream(bytes)
			{
				ConsensusFactory = consensusFactory
			};
			ReadWrite(stream);
		}

		[Obsolete("Should use Block.Load outside of ConsensusFactories")]
		public Block(byte[] bytes) : this(bytes, Consensus.Main.ConsensusFactory)
		{
		}


		public void ReadWrite(BitcoinStream stream)
		{
			using(stream.ConsensusFactoryScope(GetConsensusFactory()))
			{
				stream.ReadWrite(ref header);
				stream.ReadWrite(ref vtx);
			}
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
			if(header != null)
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
		public uint256 GetHash()
		{
			//Block's hash is his header's hash
			return header.GetHash();
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

		/// <summary>
		/// Create a block with the specified option only. (useful for stripping data from a block)
		/// </summary>
		/// <param name="options">Options to keep</param>
		/// <returns>A new block with only the options wanted</returns>
		public Block WithOptions(TransactionOptions options)
		{
			if(Transactions.Count == 0)
				return this;
			if(options == TransactionOptions.Witness && Transactions[0].HasWitness)
				return this;
			if(options == TransactionOptions.None && !Transactions[0].HasWitness)
				return this;
			var instance = GetConsensusFactory().CreateBlock();
			var ms = new MemoryStream();
			var bms = new BitcoinStream(ms, true);
			bms.TransactionOptions = options;
			this.ReadWrite(bms);
			ms.Position = 0;
			bms = new BitcoinStream(ms, false);
			bms.TransactionOptions = options;
			instance.ReadWrite(bms);
			return instance;
		}

		public virtual ConsensusFactory GetConsensusFactory()
		{
			return Consensus.Main.ConsensusFactory;
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

		/// <summary>
		/// Check proof of work and merkle root
		/// </summary>
		/// <param name="consensus"></param>
		/// <returns></returns>
		[Obsolete("Use Check() instead")]
		public bool Check(Consensus consensus)
		{
			return Check();
		}

		public bool CheckProofOfWork()
		{
			return Header.CheckProofOfWork();
		}
		[Obsolete("Use CheckProofOfWork() instead")]
		public bool CheckProofOfWork(Consensus consensus)
		{
			return CheckProofOfWork();
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
				throw new ArgumentNullException(nameof(address));
			Block block = GetConsensusFactory().CreateBlock();
			block.Header.Nonce = RandomUtils.GetUInt32();
			block.Header.HashPrevBlock = this.GetHash();
			block.Header.BlockTime = now;
			var tx = block.AddTransaction(GetConsensusFactory().CreateTransaction());
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



		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value, DateTimeOffset now, ConsensusFactory consensusFactory)
		{
			Block block = consensusFactory.CreateBlock();
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

		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value, ConsensusFactory consensusFactory)
		{
			return CreateNextBlockWithCoinbase(pubkey, value, DateTimeOffset.UtcNow, consensusFactory);
		}

		[Obsolete("Use CreateNextBlockWithCoinbase with consensusFactory instead")]
		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value)
		{
			return CreateNextBlockWithCoinbase(pubkey, value, DateTimeOffset.UtcNow, Consensus.Main.ConsensusFactory);
		}

		[Obsolete("Use CreateNextBlockWithCoinbase with consensusFactory instead")]
		public Block CreateNextBlockWithCoinbase(PubKey pubkey, Money value, DateTimeOffset now)
		{
			return CreateNextBlockWithCoinbase(pubkey, value, now, Consensus.Main.ConsensusFactory);
		}

#if !NOJSONNET
		[Obsolete("Always use an hex encoded version of the block instead")]
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
#endif
		public static Block Parse(string hex, Network network)
		{
			if(network == null)
				throw new ArgumentNullException(nameof(network));
			return Parse(hex, network.Consensus.ConsensusFactory);
		}

		public static Block Parse(string hex, Consensus consensus)
		{
			if(consensus == null)
				throw new ArgumentNullException(nameof(consensus));
			return Parse(hex, consensus.ConsensusFactory);
		}

		public static Block Parse(string hex, ConsensusFactory consensusFactory)
		{
			if(hex == null)
				throw new ArgumentNullException(nameof(hex));
			if(consensusFactory == null)
				throw new ArgumentNullException(nameof(consensusFactory));
			var block = consensusFactory.CreateBlock();
			block.ReadWrite(Encoders.Hex.DecodeData(hex));
			return block;
		}

		public static Block Load(byte[] hex, Network network)
		{
			if(hex == null)
				throw new ArgumentNullException(nameof(hex));
			if(network == null)
				throw new ArgumentNullException(nameof(network));
			return Load(hex, network.Consensus.ConsensusFactory);
		}
		public static Block Load(byte[] hex, Consensus consensus)
		{
			if(hex == null)
				throw new ArgumentNullException(nameof(hex));
			if(consensus == null)
				throw new ArgumentNullException(nameof(consensus));
			return Load(hex, consensus.ConsensusFactory);
		}
		public static Block Load(byte[] hex, ConsensusFactory consensusFactory)
		{
			if(hex == null)
				throw new ArgumentNullException(nameof(hex));
			if(consensusFactory == null)
				throw new ArgumentNullException(nameof(consensusFactory));
			var block = consensusFactory.CreateBlock();
			block.ReadWrite(hex);
			return block;
		}

		[Obsolete("Use Parse(byte[], Network|Consensus|ConsensusFactory)")]
		public static Block Parse(string hex)
		{
			return Parse(hex, Consensus.Main.ConsensusFactory);
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
