#if !NOJSONNET
#if !NOSOCKET
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	/// <summary>
	/// Idempotent and thread safe for tracking operations belonging to a set of ScriptPubKeys
	/// </summary>
	[Obsolete]
	public class Tracker
	{
		public delegate void NewTrackerOperation(Tracker sender, IOperation trackerOperation);

		public interface IOperation
		{
			bool ContainsWallet(string wallet);
			WalletTransaction ToWalletTransaction(ChainBase chain, string wallet);
		}
		internal class Operation : IOperation
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
				return string.Format("{0}-{1}-{2}",
					(blockId == null ? (int.MaxValue - 1) : height),
					(blockId ?? uint256.Zero),
					txId);
			}

			public Operation(ConcurrentDictionary<string, TrackedScript> trackedScripts)
			{
				ReceivedCoins = new List<Tuple<Coin, string>>();
				SpentCoins = new List<Tuple<Coin, string>>();
				_TrackedScripts = trackedScripts;
			}

			public Operation(Transaction transaction, ChainedBlock block, MerkleBlock proof, ConcurrentDictionary<string, TrackedScript> trackedScripts)
				: this(trackedScripts)
			{
				Transaction = transaction;
				if(block != null)
				{
					proof = proof.Clone();
					proof.PartialMerkleTree = proof.PartialMerkleTree.Trim(transaction.GetHash());
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
			internal Operation Merge(Operation other, out bool merged)
			{
				bool merged1;
				bool merged2;
				bool merged3 = false;
				if(UnconfirmedSeen > other.UnconfirmedSeen)
					UnconfirmedSeen = other.UnconfirmedSeen;

				if(this.BlockId != other.BlockId && this.Height < other.Height)
				{
					this.BlockId = other.BlockId;
					this.Height = other.Height;
					this.Proof = other.Proof;
					merged3 = true;
				}
				Merge(ReceivedCoins, other.ReceivedCoins, out merged1);
				Merge(SpentCoins, other.SpentCoins, out merged2);
				merged = merged1 || merged2 || merged3;
				return this;
			}

			private void Merge(List<Tuple<Coin, string>> a, List<Tuple<Coin, string>> b, out bool merged)
			{
				merged = false;
				foreach(var coin in b)
				{
					if(a.All(c => c.Item1.Outpoint != coin.Item1.Outpoint))
					{
						merged = true;
						a.Add(coin);
					}
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

			internal readonly ConcurrentDictionary<string, TrackedScript> _TrackedScripts;
			public bool ContainsWallet(string wallet)
			{
				return ContainsWallet(wallet, ReceivedCoins) || ContainsWallet(wallet, SpentCoins);
			}

			private bool ContainsWallet(string wallet, List<Tuple<Coin, string>> coins)
			{
				return coins.Select(c => new
				{
					Coin = c.Item1,
					TrackedScript = _TrackedScripts.TryGet(c.Item2)
				})
				.Where(o => o.TrackedScript != null)
				.Any(c => c.TrackedScript.Wallet.Equals(wallet, StringComparison.Ordinal));
			}

			public WalletTransaction ToWalletTransaction(ChainBase chain, string wallet)
			{
				var chainHeight = chain.Height;
				var tx = new WalletTransaction()
				{
					Proof = Proof,
					Transaction = Transaction,
					UnconfirmedSeen = UnconfirmedSeen,
					AddedDate = AddedDate
				};

				tx.ReceivedCoins = GetCoins(ReceivedCoins, _TrackedScripts, wallet);
				tx.SpentCoins = GetCoins(SpentCoins, _TrackedScripts, wallet);

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

			private Coin[] GetCoins(List<Tuple<Coin, string>> coins, ConcurrentDictionary<string, TrackedScript> trackedScripts, string wallet)
			{
				return coins.Select(c => new
				{
					Coin = c.Item1,
					TrackedScript = trackedScripts.TryGet(c.Item2)
				})
									.Where(o => o.TrackedScript != null)
									.Where(c => c.TrackedScript.Wallet.Equals(wallet, StringComparison.Ordinal))
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


			internal JObject ToJson()
			{
				var obj = new JObject();
				if(BlockId != null)
				{
					obj.Add("Height", Height);
					obj.Add("BlockId", BlockId.ToString());
					obj.Add("Proof", Encoders.Hex.EncodeData(this.Proof.ToBytes()));
				}
				obj.Add("AddedDate", AddedDate);
				obj.Add("UnconfirmedSeen", UnconfirmedSeen);
				obj.Add("Transaction", Encoders.Hex.EncodeData(this.Transaction.ToBytes()));
				if(this.ReceivedCoins != null)
				{
					obj.Add("ReceivedCoins", new JArray(ReceivedCoins.Select(c => ToJson(c))));
				}
				if(this.SpentCoins != null)
				{
					obj.Add("SpentCoins", new JArray(SpentCoins.Select(c => ToJson(c))));
				}
				return obj;
			}

			internal static Operation FromJson(JObject obj, ConcurrentDictionary<string, TrackedScript> trackedScripts)
			{
				var op = new Operation(trackedScripts);

				var blockId = (string)obj["BlockId"];
				if(blockId != null)
				{
					op.Height = (int)(long)obj["Height"];
					op.BlockId = uint256.Parse(blockId);
					op.Proof = new MerkleBlock();
					op.Proof.FromBytes(Encoders.Hex.DecodeData((string)obj["Proof"]));
				}
				op.AddedDate = obj["AddedDate"].Value<DateTimeOffset>();
				op.UnconfirmedSeen = obj["UnconfirmedSeen"].Value<DateTimeOffset>();
				op.Transaction = new Transaction();
				op.Transaction.FromBytes(Encoders.Hex.DecodeData((string)obj["Transaction"]));
				var coins = obj["ReceivedCoins"] as JArray;
				if(coins != null)
				{
					foreach(var c in coins)
					{
						op.ReceivedCoins.Add(FromJson(c));
					}
				}
				coins = obj["SpentCoins"] as JArray;
				if(coins != null)
				{
					foreach(var c in coins)
					{
						op.SpentCoins.Add(FromJson(c));
					}
				}
				return op;
			}

			private static Tuple<Coin, string> FromJson(JToken obj)
			{
				var tracked = (string)obj["TrackedScript"];
				var coin = FromJsonCoin(obj);
				return Tuple.Create(coin, tracked);
			}


			internal static JObject ToJson(Tuple<Coin, string> c)
			{
				JObject obj = new JObject();
				obj.Add("TrackedScript", c.Item2);
				ToJson(c.Item1, obj);
				return obj;
			}

			internal static JToken ToJson(Coin c)
			{
				return ToJson(c, new JObject());
			}

			internal static Coin FromJsonCoin(JToken obj)
			{
				OutPoint outpoint = new OutPoint();
				outpoint.FromBytes(Encoders.Hex.DecodeData((string)obj["Outpoint"]));
				TxOut txout = new TxOut();
				txout.FromBytes(Encoders.Hex.DecodeData((string)obj["TxOut"]));
				return new Coin(outpoint, txout);
			}

			private static JToken ToJson(Coin c, JObject obj)
			{
				obj.Add("Outpoint", Encoders.Hex.EncodeData(c.Outpoint.ToBytes()));
				obj.Add("TxOut", Encoders.Hex.EncodeData(c.TxOut.ToBytes()));
				return obj;
			}

			public override string ToString()
			{
				return JsonConvert.SerializeObject(ToJson(), Formatting.Indented);
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

			public string Wallet
			{
				get;
				set;
			}


			internal JObject ToJson()
			{
				var obj = new JObject();
				obj.Add("ScriptPubKey", Encoders.Hex.EncodeData(ScriptPubKey.ToBytes(true)));
				obj.Add("IsInternal", IsInternal);
				if(RedeemScript != null)
					obj.Add("RedeemScript", Encoders.Hex.EncodeData(RedeemScript.ToBytes(true)));
				obj.Add("AddedDate", AddedDate);
				obj.Add("Filter", Filter);
				obj.Add("Wallet", Wallet);
				return obj;
			}
			internal static TrackedScript FromJson(JObject obj)
			{
				TrackedScript script = new TrackedScript();
				script.ScriptPubKey = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)obj["ScriptPubKey"]));
				var redeem = (string)obj["RedeemScript"];
				if(redeem != null)
				{
					script.RedeemScript = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)obj["RedeemScript"]));
				}
				script.AddedDate = obj["AddedDate"].Value<DateTimeOffset>();
				script.Filter = (string)obj["Filter"];
				script.Wallet = (string)obj["Wallet"];
				return script;
			}
			public override string ToString()
			{
				return JsonConvert.SerializeObject(ToJson(), Formatting.Indented);
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

			internal static TrackedOutpoint FromJson(JObject obj)
			{
				TrackedOutpoint tracked = new TrackedOutpoint();
				tracked.TrackedScriptId = (string)obj["TrackedScriptId"];
				tracked.Filter = (string)obj["Filter"];
				obj = (JObject)obj["Coin"];
				tracked.Coin = Operation.FromJsonCoin(obj);
				return tracked;
			}

			internal JObject ToJson()
			{
				var obj = new JObject();
				obj.Add("TrackedScriptId", TrackedScriptId);
				obj.Add("Filter", Filter);
				obj.Add("Coin", Operation.ToJson(Coin));
				return obj;
			}
			public override string ToString()
			{
				return JsonConvert.SerializeObject(ToJson(), Formatting.Indented);
			}


		}

		public Tracker()
		{
			UpdateTweak();
		}

		public BloomFilter CreateBloomFilter(double fp, BloomFlags flags = BloomFlags.UPDATE_ALL)
		{
			var toTrack = GetDataToTrack().ToArray();
			var scriptCount = _TrackedScripts.Count(s => !s.Value.IsInternal);
			var filter = new BloomFilter(scriptCount == 0 ? 1 : scriptCount, fp, _Tweak, flags);
			foreach(var data in toTrack)
				filter.Insert(data);
			return filter;
		}

		object cs = new object();


		/// <summary>
		/// Register the specified ScriptPubKey
		/// </summary>
		/// <param name="destination">The destination</param>
		/// <param name="isRedeemScript">If true, the P2SH of the destination's script will be tracked (Default: false)</param>
		/// <param name="isInternal">If true, the scriptPubKey will not belong to tracked data, typically, change addresses (Default: false)</param>
		/// <param name="filter">The filter in which this key will appear (http://eprint.iacr.org/2014/763.pdf)</param>
		/// <param name="wallet">The wallet name to which it belongs</param>
		public void Add(IDestination destination, bool isRedeemScript = false, bool isInternal = false, string filter = "a", string wallet = "default")
		{
			Add(destination.ScriptPubKey, isRedeemScript, isInternal, filter, wallet);
		}

		/// <summary>
		/// Register the specified ScriptPubKey
		/// </summary>
		/// <param name="scriptPubKey">The ScriptPubKey</param>
		/// <param name="isRedeemScript">If true, the P2SH of the destination's script will be tracked (Default: false)</param>
		/// <param name="isInternal">If true, the scriptPubKey will not belong to tracked data, typically, change addresses (Default: false)</param>
		/// <param name="filter">The filter in which this key will appear (http://eprint.iacr.org/2014/763.pdf)</param>
		/// <param name="wallet">The wallet name to which it belongs</param>
		public bool Add(Script scriptPubKey, bool isRedeemScript = false, bool isInternal = false, string filter = "a", string wallet = "default")
		{
			if(filter == null)
				throw new ArgumentNullException(nameof(filter));
			if(wallet == null)
				throw new ArgumentNullException(nameof(wallet));
			Script redeem = isRedeemScript ? scriptPubKey : null;
			scriptPubKey = isRedeemScript ? scriptPubKey.Hash.ScriptPubKey : scriptPubKey;
			var data = scriptPubKey.ToOps().First(o => o.PushData != null).PushData;

			var trackedScript = new TrackedScript()
			{
				ScriptPubKey = scriptPubKey,
				RedeemScript = redeem,
				AddedDate = DateTimeOffset.UtcNow,
				IsInternal = isInternal,
				Filter = filter,
				Wallet = wallet
			};

			bool added = false;
			lock(cs)
			{
				added = _TrackedScripts.TryAdd(trackedScript.GetId(), trackedScript);
			}
			return added;
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
			if(chainedBlock != null)
			{
				if(proof == null)
					throw new ArgumentNullException(nameof(proof));
				if(proof.Header.GetHash() != chainedBlock.Header.GetHash())
					throw new InvalidOperationException("The chained block and the merkle block are different blocks");
				if(!proof.PartialMerkleTree.Check(chainedBlock.Header.HashMerkleRoot))
					throw new InvalidOperationException("The MerkleBlock does not have the expected merkle root");
				if(!proof.PartialMerkleTree.GetMatchedTransactions().Contains(transaction.GetHash()))
					throw new InvalidOperationException("The MerkleBlock does not contains the input transaction");
			}

			var interesting = false;
			List<Operation> operations = null;
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
							var op = Spent(parentMetadata, txin, match.Coin, chainedBlock, proof);
							if(op != null)
							{
								operations = operations ?? new List<Operation>();
								operations.Add(op);
							}
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
						var op = Received(match, txout, chainedBlock, proof);
						if(op != null)
						{
							operations = operations ?? new List<Operation>();
							operations.Add(op);
						}
					}
				}
			}
			if(operations != null)
			{
				FireCallbacks(operations);
			}
			return interesting;
		}

		private void FireCallbacks(List<Operation> operations)
		{
			foreach(var operation in operations)
			{
				var newOperation = this.NewOperation;
				if(newOperation != null)
				{
					foreach(var handler in newOperation.GetInvocationList().Cast<NewTrackerOperation>())
					{
						try
						{
							handler.DynamicInvoke(this, operation);
						}
						catch(TargetInvocationException ex)
						{
							NodeServerTrace.Error("Error while calling Tracker callback", ex.InnerException);
						}
					}
				}
			}
		}


		public event NewTrackerOperation NewOperation;

		public WalletTransactionsCollection GetWalletTransactions(ChainBase chain, string wallet = "default")
		{
			lock(cs)
			{
				return new WalletTransactionsCollection(_Operations
					.Select(op => op.Value)
					.Where(op => op.CheckProof())
					.Where(op => op.ContainsWallet(wallet))
					.Select(o => o.ToWalletTransaction(chain, wallet))
					.ToArray());
			}
		}




		private Operation Spent(TrackedScript metadata, IndexedTxIn txin, Coin coin, ChainedBlock block, MerkleBlock proof)
		{
			var operation = new Operation(txin.Transaction, block, proof, _TrackedScripts);
			operation.SpentCoins.Add(Tuple.Create(coin, metadata.GetId()));
			SetUnconfirmedSeenIfPossible(txin.Transaction, block, operation);

			bool merged = false;
			var returned = _Operations.AddOrUpdate(operation.GetId(), operation, (k, old) => old.Merge(operation, out merged));
			return (operation == returned || merged) ? operation : null;
		}

		private Operation Received(TrackedScript match, IndexedTxOut txout, ChainedBlock block, MerkleBlock proof)
		{
			var operation = new Operation(txout.Transaction, block, proof, _TrackedScripts);
			SetUnconfirmedSeenIfPossible(txout.Transaction, block, operation);
			var coin = new Coin(txout);
			operation.ReceivedCoins.Add(Tuple.Create(coin, match.GetId()));

			bool merged = false;
			var returned = _Operations.AddOrUpdate(operation.GetId(), operation, (k, old) => old.Merge(operation, out merged));
			var trackedOutpoint = new TrackedOutpoint()
			{
				Coin = coin,
				TrackedScriptId = match.GetId(),
				Filter = match.Filter
			};
			_TrackedOutpoints.TryAdd(trackedOutpoint.GetId(), trackedOutpoint);
			return (operation == returned || merged) ? operation : null;
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
				.Concat(_TrackedOutpoints.Where(t => t.Value.Filter == filter).Select(o => o.Value.Coin.Outpoint.ToBytes()))
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
			timeExpiration = timeExpiration ?? TimeSpan.FromDays(7.0);
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


		uint _Tweak;
		ConcurrentDictionary<string, Operation> _Operations = new ConcurrentDictionary<string, Operation>();
		ConcurrentDictionary<string, TrackedScript> _TrackedScripts = new ConcurrentDictionary<string, TrackedScript>();
		ConcurrentDictionary<string, TrackedOutpoint> _TrackedOutpoints = new ConcurrentDictionary<string, TrackedOutpoint>();

		public Transaction GetKnownTransaction(uint256 txId)
		{
			return _Operations.Select(o => o.Value.Transaction).FirstOrDefault(o => o.GetHash() == txId);
		}

		public void UpdateTweak()
		{
			_Tweak = RandomUtils.GetUInt32();
		}

		public void Save(Stream stream)
		{
			lock(cs)
			{
				JObject obj = new JObject();
				obj.Add("Tweak", _Tweak);
				obj.Add("Operations", new JArray(_Operations.Select(o => o.Value.ToJson()).ToArray()));
				obj.Add("Outpoints", new JArray(_TrackedOutpoints.Select(o => o.Value.ToJson()).ToArray()));
				obj.Add("Scripts", new JArray(_TrackedScripts.Select(o => o.Value.ToJson()).ToArray()));
				var writer = new StreamWriter(stream);
				writer.Write(JsonConvert.SerializeObject(obj, new JsonSerializerSettings()
				{
					DateParseHandling = DateParseHandling.DateTimeOffset
				}));
				writer.Flush();
			}
		}

		public static Tracker Load(Stream stream)
		{
			var tracker = new Tracker();
			tracker.LoadCore(stream);
			return tracker;
		}
		void LoadCore(Stream stream)
		{
			lock(cs)
			{
				_Operations.Clear();
				_TrackedOutpoints.Clear();
				_TrackedScripts.Clear();
				JObject obj = JObject.Load(new JsonTextReader(new StreamReader(stream))
				{
					DateParseHandling = DateParseHandling.DateTimeOffset
				});
				_Tweak = (uint)(long)obj["Tweak"];
				var operations = (JArray)obj["Operations"];
				foreach(var operation in operations.OfType<JObject>())
				{
					var op = Operation.FromJson(operation, _TrackedScripts);
					_Operations.TryAdd(op.GetId(), op);
				}
				var outpoints = (JArray)obj["Outpoints"];
				foreach(var outpoint in outpoints.OfType<JObject>())
				{
					var op = TrackedOutpoint.FromJson(outpoint);
					_TrackedOutpoints.TryAdd(op.GetId(), op);
				}

				var scripts = (JArray)obj["Scripts"];
				foreach(var script in scripts.OfType<JObject>())
				{
					var op = TrackedScript.FromJson(script);
					_TrackedScripts.TryAdd(op.GetId(), op);
				}
			}
		}
	}
}
#endif
#endif