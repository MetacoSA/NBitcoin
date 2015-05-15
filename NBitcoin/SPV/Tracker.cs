using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	[Flags]
	public enum ScriptPubKeyMode
	{
		/// <summary>
		/// Use key as a P2PKH
		/// </summary>
		P2PKH = 0,
		/// <summary>
		/// Use key as a P2PK
		/// </summary>
		P2PK = 1,
		/// <summary>
		/// Use key as a P2SH
		/// </summary>
		P2SH = 2,
	}

	/// <summary>
	/// Idempotent and thread safe class for registering and querying operations belonging to specified ScriptPubKeys
	/// </summary>
	public class Tracker
	{
		class Operation
		{
			public string GetId()
			{
				return GetId(Transaction.GetHash(), BlockId, Height);
			}
			public static string GetId(uint256 txId, ChainedBlock block)
			{
				return GetId(txId, block == null ? null : block.HashBlock, block == null ? 0 : block.Height);
			}

			public static string GetId(uint256 txId, uint256 blockId, int height)
			{
				return (blockId == null ? (int.MaxValue - 1) : height) + "-"
					+ (blockId == null ? new uint256(0) : blockId) + "-"
					+ txId;
			}

			private Operation()
			{
				ReceivedCoins = new List<Tuple<Coin, string>>();
				SpentCoins = new List<Tuple<Coin, string>>();
			}

			public Operation(Transaction transaction, ChainedBlock block, MerkleBlock proof)
				: this()
			{
				Transaction = transaction;
				if(block != null)
				{
					Height = block.Height;
					BlockId = block.HashBlock;
					Proof = proof;
				}
				AddedDate = DateTimeOffset.UtcNow;
			}
			public uint256 BlockId
			{
				get;
				set;
			}
			public int Height
			{
				get;
				set;
			}
			public List<Tuple<Coin, string>> ReceivedCoins
			{
				get;
				set;
			}
			public List<Tuple<Coin, string>> SpentCoins
			{
				get;
				set;
			}

			public MerkleBlock Proof
			{
				get;
				set;
			}
			public Transaction Transaction
			{
				get;
				set;
			}
			internal Operation Merge(Operation other)
			{
				Merge(ReceivedCoins, other.ReceivedCoins);
				Merge(SpentCoins, other.SpentCoins);
				return this;
			}

			private void Merge(List<Tuple<Coin, string>> a, List<Tuple<Coin, string>> b)
			{
				foreach(var coin in b)
				{
					if(!a.Any(c => c.Item1.Outpoint == coin.Item1.Outpoint))
						a.Add(coin);
				}
			}
			public DateTimeOffset AddedDate
			{
				get;
				set;
			}
		}
		class TrackedScript
		{
			public string GetId()
			{
				return GetId(ScriptPubKey);
			}
			public static string GetId(Script scriptPubKey)
			{
				return "s-" + scriptPubKey.ToHex();
			}

			public Script ScriptPubKey
			{
				get;
				set;
			}
			public bool IsInternal
			{
				get;
				set;
			}
			public IEnumerable<byte[]> GetTrackedData()
			{
				return ScriptPubKey.ToOps().Select(o => o.PushData).Where(o => o != null);
			}

			public Script RedeemScript
			{
				get;
				set;
			}

			public DateTimeOffset AddedDate
			{
				get;
				set;
			}

		}
		class TrackedOutpoint
		{
			public string GetId()
			{
				return GetId(Coin.Outpoint);
			}

			public static string GetId(OutPoint outPoint)
			{
				return "o-" + Encoders.Hex.EncodeData(outPoint.ToBytes());
			}
			public string TrackedScriptId
			{
				get;
				set;
			}

			public Coin Coin
			{
				get;
				set;
			}
		}

		object cs = new object();


		/// <summary>
		/// Register the specified ScriptPubKey
		/// </summary>
		/// <param name="destination">The destination</param>
		/// <param name="isRedeemScript">If true, the P2SH of the destination's script will be tracked (Default: false)</param>
		/// <param name="isInternal">If true, the scriptPubKey will not belong to tracked data, typically, change addresses (Default: false)</param>
		public void Add(IDestination destination, bool isRedeemScript = false, bool isInternal = false)
		{
			Add(destination.ScriptPubKey, isRedeemScript, isInternal);
		}

		/// <summary>
		/// Register the specified ScriptPubKey
		/// </summary>
		/// <param name="scriptPubKey">The ScriptPubKey</param>
		/// <param name="isRedeemScript">If true, the P2SH of the destination's script will be tracked (Default: false)</param>
		/// <param name="isInternal">If true, the scriptPubKey will not belong to tracked data, typically, change addresses (Default: false)</param>
		public void Add(Script scriptPubKey, bool isRedeemScript = false, bool isInternal = false)
		{
			Script redeem = isRedeemScript ? scriptPubKey : null;
			scriptPubKey = isRedeemScript ? scriptPubKey.Hash.ScriptPubKey : scriptPubKey;
			var data = scriptPubKey.ToOps().First(o => o.PushData != null).PushData;

			var trackedScript = new TrackedScript()
			{
				ScriptPubKey = scriptPubKey,
				RedeemScript = redeem,
				AddedDate = DateTimeOffset.UtcNow,
				IsInternal = isInternal
			};

			bool added = false;
			lock(cs)
			{
				added = _TrackedScripts.TryAdd(trackedScript.GetId(), trackedScript);
			}
			if(added && !isInternal)
			{
				OnNewDataToTrack();
			}
		}

		private void OnNewDataToTrack()
		{
			var track = NewDataToTrack;
			if(track != null)
				track(this, EventArgs.Empty);
		}


		public bool NotifyTransaction(Transaction transaction, ChainedBlock chainedBlock, Block block)
		{
			if(chainedBlock == null)
				return NotifyTransaction(transaction);
			return NotifyTransaction(transaction, chainedBlock, new MerkleBlock(block, new uint256[] { transaction.GetHash() }));
		}

		public bool NotifyTransaction(Transaction transaction)
		{
			return NotifyTransaction(transaction, null, null as MerkleBlock);
		}

		public bool NotifyTransaction(Transaction transaction, ChainedBlock chainedBlock, MerkleBlock proof)
		{
			bool interesting = false;

			if(chainedBlock != null)
			{
				if(proof == null)
					throw new ArgumentNullException("proof");
				if(proof.Header.GetHash() != chainedBlock.Header.GetHash())
					throw new InvalidOperationException("The chained block and the merkle block are different blocks");
				if(!proof.PartialMerkleTree.Check(chainedBlock.Header.HashMerkleRoot))
					throw new InvalidOperationException("The MerkleBlock does not have the expected merkle root");
				if(!proof.PartialMerkleTree.GetMatchedTransactions().Contains(transaction.GetHash()))
					throw new InvalidOperationException("The MerkleBlock does not contains the input transaction");
			}
			lock(cs)
			{
				foreach(var txin in transaction.Inputs.AsIndexedInputs())
				{
					var key = TrackedOutpoint.GetId(txin.PrevOut);
					TrackedOutpoint match;
					if(_TrackedOutpoints.TryGetValue(key, out match))
					{
						TrackedScript parentMetadata;
						if(_TrackedScripts.TryGetValue(match.TrackedScriptId, out parentMetadata))
						{
							interesting = true;
							Spent(parentMetadata, txin, match.Coin, chainedBlock, proof);
						}

					}
				}
				foreach(var txout in transaction.Outputs.AsIndexedOutputs())
				{
					var key = TrackedScript.GetId(txout.TxOut.ScriptPubKey);
					TrackedScript match;
					if(_TrackedScripts.TryGetValue(key, out match))
					{
						interesting = true;
						Received(match, txout, chainedBlock, proof);
					}
				}
			}
			return interesting;
		}




		private void Spent(TrackedScript metadata, IndexedTxIn txin, Coin coin, ChainedBlock block, MerkleBlock proof)
		{
			var operation = new Operation(txin.Transaction, block, proof);
			operation.SpentCoins.Add(Tuple.Create(coin, metadata.GetId()));
			_Operations.AddOrUpdate(operation.GetId(), operation, (k, old) => old.Merge(operation));
		}

		public IEnumerable<byte[]> GetDataToTrack()
		{
			return _TrackedScripts
				.Where(t => !t.Value.IsInternal)
				.SelectMany(m => m.Value.GetTrackedData())
				.Concat(_TrackedOutpoints.Select(o => o.Value.Coin.Outpoint.ToBytes()))
				.Where(m => m != null)
				.ToList();
		}

		/// <summary>
		/// Remove old spent & confirmed TrackedOutpoint and old unconf operations
		/// </summary>
		/// <param name="chain"></param>
		void Prune(ConcurrentChain chain)
		{
			foreach(var op in _Operations)
			{
				if(op.Value.BlockId != null)
				{
					var chained = chain.GetBlock(op.Value.BlockId);
					if(chained != null)
					{
						if(chain.Height - chained.Height > 2000)
						{
							foreach(var spent in op.Value.SpentCoins)
							{
								TrackedOutpoint unused;
								_TrackedOutpoints.TryRemove(TrackedOutpoint.GetId(spent.Item1.Outpoint), out unused);
							}
						}
					}
				}
				else
				{
					if((DateTimeOffset.UtcNow - op.Value.AddedDate) > TimeSpan.FromDays(14.0))
					{
						Operation unused;
						_Operations.TryRemove(op.Key, out unused);
					}
				}
			}
		}

		private void Received(TrackedScript match, IndexedTxOut txout, ChainedBlock block, MerkleBlock proof)
		{
			var operation = new Operation(txout.Transaction, block, proof);

			var coin = new Coin(txout);
			operation.ReceivedCoins.Add(Tuple.Create(coin, match.GetId()));

			_Operations.AddOrUpdate(operation.GetId(), operation, (k, old) => old.Merge(operation));

			var trackedOutpoint = new TrackedOutpoint()
			 {
				 Coin = coin,
				 TrackedScriptId = match.GetId(),
			 };
			_TrackedOutpoints.TryAdd(trackedOutpoint.GetId(), trackedOutpoint);

		}

		/// <summary>
		/// Check internal consistency
		/// </summary>
		/// <returns></returns>
		public bool Validate()
		{
			foreach(var op in _Operations)
			{
				if(op.Value.BlockId != null)
				{
					if(op.Value.Proof == null)
						return false;
					if(!op.Value.Proof.PartialMerkleTree.Check(op.Value.Proof.Header.HashMerkleRoot))
						return false;
					if(op.Value.Proof.Header.GetHash() != op.Value.BlockId)
						return false;
				}
			}
			return true;
		}

		public event EventHandler NewDataToTrack;

		ConcurrentDictionary<string, Operation> _Operations = new ConcurrentDictionary<string, Operation>();
		ConcurrentDictionary<string, TrackedScript> _TrackedScripts = new ConcurrentDictionary<string, TrackedScript>();
		ConcurrentDictionary<string, TrackedOutpoint> _TrackedOutpoints = new ConcurrentDictionary<string, TrackedOutpoint>();
	}
}
