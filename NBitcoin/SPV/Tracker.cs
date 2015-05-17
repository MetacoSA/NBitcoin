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
	/// Idempotent and thread safe for tracking operations belonging to a set of ScriptPubKeys
	/// </summary>
	public class Tracker
	{
		internal class Operation
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
				UnconfirmedSeen = DateTimeOffset.UtcNow;
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
				if(UnconfirmedSeen > other.UnconfirmedSeen)
					UnconfirmedSeen = other.UnconfirmedSeen;
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

			public DateTimeOffset UnconfirmedSeen
			{
				get;
				set;
			}
			public DateTimeOffset AddedDate
			{
				get;
				set;
			}

			public WalletTransaction ToWalletTransaction(ChainBase chain, ConcurrentDictionary<string, TrackedScript> trackedScripts)
			{
				var chainHeight = chain.Height;
				var tx = new WalletTransaction()
				{
					Proof = Proof,
					Transaction = Transaction,
					UnconfirmedSeen = UnconfirmedSeen,
					AddedDate = AddedDate
				};

				tx.ReceivedCoins = GetCoins(ReceivedCoins, trackedScripts);
				tx.SpentCoins = GetCoins(SpentCoins, trackedScripts);

				if(BlockId != null)
				{
					var header = chain.GetBlock(BlockId);
					if(header != null)
					{
						tx.BlockInformation = new BlockInformation()
						{
							Confirmations = chainHeight - header.Height + 1,
							Height = header.Height,
							Header = header.Header
						};
					}
				}
				return tx;
			}

			private Coin[] GetCoins(List<Tuple<Coin, string>> coins, ConcurrentDictionary<string, TrackedScript> trackedScripts)
			{
				return coins.Select(c => new
									{
										Coin = c.Item1,
										TrackedScript = trackedScripts[c.Item2]
									})
									.Select(c => c.TrackedScript.RedeemScript != null ? c.Coin.ToScriptCoin(c.TrackedScript.RedeemScript) : c.Coin)
									.ToArray();
			}

			internal bool CheckProof()
			{
				if(BlockId != null)
				{
					if(Proof == null)
						return false;
					if(!Proof.PartialMerkleTree.Check(Proof.Header.HashMerkleRoot))
						return false;
					if(Proof.Header.GetHash() != BlockId)
						return false;
				}
				return true;
			}
		}
		internal class TrackedScript
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


			public string Filter
			{
				get;
				set;
			}
		}
		internal class TrackedOutpoint
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

			public string Filter
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
		/// <param name="filter">The filter in which this key will appear (http://eprint.iacr.org/2014/763.pdf)</param>
		public void Add(IDestination destination, bool isRedeemScript = false, bool isInternal = false, string filter = "a")
		{
			Add(destination.ScriptPubKey, isRedeemScript, isInternal, filter);
		}

		/// <summary>
		/// Register the specified ScriptPubKey
		/// </summary>
		/// <param name="scriptPubKey">The ScriptPubKey</param>
		/// <param name="isRedeemScript">If true, the P2SH of the destination's script will be tracked (Default: false)</param>
		/// <param name="isInternal">If true, the scriptPubKey will not belong to tracked data, typically, change addresses (Default: false)</param>
		/// <param name="filter">The filter in which this key will appear (http://eprint.iacr.org/2014/763.pdf)</param>
		public void Add(Script scriptPubKey, bool isRedeemScript = false, bool isInternal = false, string filter = "a")
		{
			if(filter == null)
				throw new ArgumentNullException("filter");
			Script redeem = isRedeemScript ? scriptPubKey : null;
			scriptPubKey = isRedeemScript ? scriptPubKey.Hash.ScriptPubKey : scriptPubKey;
			var data = scriptPubKey.ToOps().First(o => o.PushData != null).PushData;

			var trackedScript = new TrackedScript()
			{
				ScriptPubKey = scriptPubKey,
				RedeemScript = redeem,
				AddedDate = DateTimeOffset.UtcNow,
				IsInternal = isInternal,
				Filter = filter
			};

			bool added = false;
			lock(cs)
			{
				added = _TrackedScripts.TryAdd(trackedScript.GetId(), trackedScript);
			}
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

		public WalletTransactionsCollection GetWalletTransactions(ChainBase chain)
		{
			lock(cs)
			{
				return new WalletTransactionsCollection(_Operations
					.Select(op => op.Value)
					.Where(op => op.CheckProof())
					.Select(o => o.ToWalletTransaction(chain, _TrackedScripts))
					.ToArray());
			}
		}




		private void Spent(TrackedScript metadata, IndexedTxIn txin, Coin coin, ChainedBlock block, MerkleBlock proof)
		{
			var operation = new Operation(txin.Transaction, block, proof);
			operation.SpentCoins.Add(Tuple.Create(coin, metadata.GetId()));
			SetUnconfirmedSeenIfPossible(txin.Transaction, block, operation);
			_Operations.AddOrUpdate(operation.GetId(), operation, (k, old) => old.Merge(operation));
		}

		private void Received(TrackedScript match, IndexedTxOut txout, ChainedBlock block, MerkleBlock proof)
		{
			var operation = new Operation(txout.Transaction, block, proof);
			SetUnconfirmedSeenIfPossible(txout.Transaction, block, operation);
			var coin = new Coin(txout);
			operation.ReceivedCoins.Add(Tuple.Create(coin, match.GetId()));
			_Operations.AddOrUpdate(operation.GetId(), operation, (k, old) => old.Merge(operation));
			var trackedOutpoint = new TrackedOutpoint()
			{
				Coin = coin,
				TrackedScriptId = match.GetId(),
				Filter = match.Filter
			};
			_TrackedOutpoints.TryAdd(trackedOutpoint.GetId(), trackedOutpoint);
		}


		private void SetUnconfirmedSeenIfPossible(Transaction tx, ChainedBlock block, Operation operation)
		{
			if(block != null)
			{
				Operation unconf;
				if(_Operations.TryGetValue(Operation.GetId(tx.GetHash(), null), out unconf))
					operation.UnconfirmedSeen = unconf.UnconfirmedSeen;
			}
		}

		public IEnumerable<byte[]> GetDataToTrack(string filter = "a")
		{
			return _TrackedScripts
				.Where(t => !t.Value.IsInternal && filter == t.Value.Filter)
				.SelectMany(m => m.Value.GetTrackedData())
				.Concat(_TrackedOutpoints.Where(t=>t.Value.Filter == filter).Select(o => o.Value.Coin.Outpoint.ToBytes()))
				.Where(m => m != null)
				.ToList();
		}

		/// <summary>
		/// Remove old spent & confirmed TrackedOutpoint, old unconf operations, and old forked operations
		/// </summary>
		/// <param name="chain"></param>
		internal List<object> Prune(ConcurrentChain chain, int blockExpiration = 2000, TimeSpan? timeExpiration = null)
		{
			List<object> removed = new List<object>();
			timeExpiration = timeExpiration == null ? TimeSpan.FromDays(7.0) : timeExpiration;
			foreach(var op in _Operations)
			{
				if(op.Value.BlockId != null)
				{
					var chained = chain.GetBlock(op.Value.BlockId);
					var isForked = chained == null;
					if(!isForked)
					{
						bool isOldConfirmed = chain.Height - chained.Height + 1 > blockExpiration;
						if(isOldConfirmed)
						{
							foreach(var spent in op.Value.SpentCoins) //Stop tracking the outpoints
							{
								TrackedOutpoint unused;
								if(_TrackedOutpoints.TryRemove(TrackedOutpoint.GetId(spent.Item1.Outpoint), out unused))
									removed.Add(unused);
							}
						}
					}
					else
					{
						var isOldFork = chain.Height - op.Value.Height + 1 > blockExpiration;
						if(isOldFork) //clear any operation belonging to an old fork
						{
							Operation unused;
							if(_Operations.TryRemove(op.Key, out unused))
								removed.Add(unused);
						}
					}
				}
				else
				{
					var isOldUnconf = (DateTimeOffset.UtcNow - op.Value.AddedDate) > timeExpiration;
					if(isOldUnconf) //clear any old unconfirmed
					{
						Operation unused;
						if(_Operations.TryRemove(op.Key, out unused))
							removed.Add(unused);
					}
				}
			}
			return removed;
		}

		/// <summary>
		/// Check internal consistency
		/// </summary>
		/// <returns></returns>
		public bool Validate()
		{
			foreach(var op in _Operations)
			{
				if(!op.Value.CheckProof())
					return false;
			}
			return true;
		}


		ConcurrentDictionary<string, Operation> _Operations = new ConcurrentDictionary<string, Operation>();
		ConcurrentDictionary<string, TrackedScript> _TrackedScripts = new ConcurrentDictionary<string, TrackedScript>();
		ConcurrentDictionary<string, TrackedOutpoint> _TrackedOutpoints = new ConcurrentDictionary<string, TrackedOutpoint>();
	}
}
