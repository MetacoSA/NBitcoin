using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum BlockType
	{
		Main,
		Side
	}

	public enum WalletEntryType
	{
		Cancel,
		Income,
		Outcome
	}

	public class Spendable
	{
		public Spendable(OutPoint output, TxOut txout)
		{
			if(output == null)
				throw new ArgumentNullException("output");
			if(txout == null)
				throw new ArgumentNullException("txout");
			_Out = txout;
			_OutPoint = output;
		}

		private readonly OutPoint _OutPoint;
		public OutPoint OutPoint
		{
			get
			{
				return _OutPoint;
			}
		}
		private readonly TxOut _Out;
		public TxOut TxOut
		{
			get
			{
				return _Out;
			}
		}
	}
	public class WalletEntry
	{
		public int Confidence
		{
			get
			{
				if(Block == null)
					return 0;
				return 1;
			}
		}

		public Script ScriptPubKey
		{
			get;
			set;
		}
		public OutPoint OutPoint
		{
			get;
			set;
		}

		public uint256 Block
		{
			get;
			set;
		}

		public Money Value
		{
			get;
			set;
		}

		internal Spendable GetSpendable()
		{
			if(Value == null || OutPoint == null)
				return null;
			return new Spendable(OutPoint, new TxOut(Value, ScriptPubKey));
		}

		public WalletEntryType Type
		{
			get
			{
				if(CancelEntry != null)
					return WalletEntryType.Cancel;
				return Value == null ? WalletEntryType.Outcome : WalletEntryType.Income;
			}
		}

		public WalletEntry CancelEntry
		{
			get;
			set;
		}
		public WalletEntry Cancel()
		{
			if(Block == null)
				throw new InvalidOperationException("Can only cancel transaction from a block");

			return new WalletEntry()
			{
				CancelEntry = this
			};
		}
	}


	public class WalletPools
	{
		public WalletPools()
		{
			Confirmed = new WalletPool();
			Verified = new WalletPool();
			Available = new WalletPool();
			Inactive = new WalletPool();
		}

		public WalletPool Inactive
		{
			get;
			private set;
		}

		public WalletPool Available
		{
			get;
			private set;
		}
		public WalletPool Confirmed
		{
			get;
			private set;
		}

		public WalletPool Verified
		{
			get;
			private set;
		}

		public void Reorganize(BlockChain chain)
		{
			var missed = Inactive.GetInChain(chain, true);

			foreach(var miss in missed)
			{
				Inactive.PushEntry(miss.Cancel());
			}
			Inactive.EnsureLoaded();
			var newInactives = Verified.GetInChain(chain, false);
			foreach(var inactive in newInactives)
			{
				Inactive.PushEntry(inactive);
			}

			CancelNotIn(chain, Verified);
			CancelNotIn(chain, Available);
			CancelNotIn(chain, Confirmed);


			foreach(var miss in missed)
			{
				PushEntry(miss, BlockType.Main);
			}
		}

		private void CancelNotIn(BlockChain chain, WalletPool pool)
		{
			var toCancel = pool.GetInChain(chain, false);
			foreach(var cancel in toCancel)
			{
				pool.PushEntry(cancel.Cancel());
			}
		}

		public void PushEntries(IEnumerable<WalletEntry> entries, BlockType? blockType)
		{
			foreach(var entry in entries)
			{
				PushEntry(entry, blockType);
			}
		}

		public void PushEntry(WalletEntry entry, BlockType? blockType)
		{
			if(blockType == null && entry.Block != null)
				throw new ArgumentException("An entry coming from a block should have a blocktype");
			if(entry.Block != null && blockType == BlockType.Side)
			{
				Inactive.PushEntry(entry);
				return;
			}
			Verified.PushEntry(entry);
			if(entry.Type == WalletEntryType.Outcome)
				Available.PushEntry(entry);
			if(entry.Block != null)
			{
				Available.PushEntry(entry);
				Confirmed.PushEntry(entry);
			}
		}
	}
	public class WalletPool
	{
		List<WalletEntry> _Entries = new List<WalletEntry>();
		List<WalletEntry> _CleanedEntries = new List<WalletEntry>();

		internal void PushEntry(WalletEntry entry)
		{
			if(!_Reloading)
				_Entries.Add(entry);
			if(entry.Type != WalletEntryType.Cancel)
				_CleanedEntries.Add(entry);
			if(entry.Type == WalletEntryType.Income)
			{
				if(_UnknowSpent.Contains(entry.OutPoint))
				{
					_UnknowSpent.Remove(entry.OutPoint);
					_Spent.Add(entry.OutPoint, entry.GetSpendable());
				}
				else
				{
					if(_Unspent.TryAdd(entry.OutPoint, entry.GetSpendable()))
						_Balance += entry.Value;
				}
			}
			else if(entry.Type == WalletEntryType.Outcome)
			{
				if(_Unspent.ContainsKey(entry.OutPoint))
				{
					var spendable = _Unspent[entry.OutPoint];
					_Unspent.Remove(entry.OutPoint);
					_Spent.Add(spendable.OutPoint, spendable);
					_Balance -= spendable.TxOut.Value;
				}
				else
				{
					if(!_Spent.ContainsKey(entry.OutPoint)) //Probably already received
						_UnknowSpent.Add(entry.OutPoint);
				}
			}
			else if(entry.Type == WalletEntryType.Cancel)
			{
				_NeedReload = true;
			}
		}


		Money _Balance = Money.Zero;
		public Money Balance
		{
			get
			{
				EnsureLoaded();
				return _Balance;
			}
		}



		HashSet<OutPoint> _UnknowSpent = new HashSet<OutPoint>();
		public IEnumerable<OutPoint> UnknowSpent
		{
			get
			{
				EnsureLoaded();
				return _UnknowSpent;
			}
		}

		Dictionary<OutPoint, Spendable> _Unspent = new Dictionary<OutPoint, Spendable>();
		public IEnumerable<Spendable> Unspent
		{
			get
			{
				EnsureLoaded();
				return _Unspent.Values;
			}
		}

		Dictionary<OutPoint, Spendable> _Spent = new Dictionary<OutPoint, Spendable>();
		public IEnumerable<Spendable> Spent
		{
			get
			{
				EnsureLoaded();
				return _Spent.Values;
			}
		}

		public Spendable[] GetEntriesToCover(Money money)
		{
			EnsureLoaded();
			if(Balance < money)
				return null;
			var result = new List<Spendable>();
			Money current = Money.Zero;
			var unspent = Unspent.OrderBy(o => o.TxOut.Value.Satoshi).ToArray();
			int i = 0;
			while(current < money)
			{
				result.Add(unspent[i]);
				current += unspent[i].TxOut.Value;
				i++;
			}
			return result.ToArray();
		}

		public WalletEntry[] GetInChain(BlockChain chain, bool value)
		{
			EnsureLoaded();
			List<WalletEntry> entries = new List<WalletEntry>();
			foreach(var entry in _CleanedEntries.Where(e => e.Block != null))
			{
				if(chain.Contains(entry.Block) == value)
					entries.Add(entry);
			}
			return entries.ToArray();
		}

		bool _NeedReload;
		bool _Reloading;
		internal void EnsureLoaded()
		{
			if(_NeedReload)
			{
				_Reloading = true;
				_NeedReload = false;

				_CleanedEntries.Clear();
				_Balance = Money.Zero;
				_Spent.Clear();
				_UnknowSpent.Clear();
				_Unspent.Clear();
				List<WalletEntry> canceled = new List<WalletEntry>();
				foreach(var canceledEntry in _Entries.Where(e => e.Type == WalletEntryType.Cancel).Select(e => e.CancelEntry))
				{
					canceled.Add(canceledEntry);
				}
				foreach(var entry in _Entries.Where(e => e.Type != WalletEntryType.Cancel))
				{
					if(canceled.Contains(entry))
						canceled.Remove(entry);
					else
						PushEntry(entry);
				}

				_Reloading = false;
				if(_NeedReload || canceled.Count != 0)
					throw new NotSupportedException("A bug... should never happen");
			}
		}
	}

	public delegate void WalletBalanceChangedDelegate(Wallet wallet, Transaction tx, Money oldBalance, Money newBalance);
	public class Wallet
	{
		public event WalletBalanceChangedDelegate BalanceChanged;
		public event WalletBalanceChangedDelegate CoinsReceived;
		public event WalletBalanceChangedDelegate CoinsSent;

		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}


		public Wallet(Network network)
		{
			this._Network = network;
			this._Pools = new WalletPools();
		}



		public void AddKey(BitcoinSecret key)
		{
			if(key.Network != Network)
				throw new InvalidOperationException("This key is does not belong to the same network as the wallet");
			AddKey(key.Key);
		}

		List<Key> _Keys = new List<Key>();

		public void AddKey(Key key)
		{
			_Keys.Add(key);
		}

		public Money Balance
		{
			get
			{
				return Pools.Available.Balance;
			}
		}

		WalletPools _Pools;
		public WalletPools Pools
		{
			get
			{
				return _Pools;
			}
		}

		public void ReceiveTransaction(Transaction tx)
		{
			ReceiveTransaction(null, null, tx);
		}


		private void ReceiveTransaction(uint256 block, BlockType? blockType, Transaction tx)
		{
			var oldBalance = Balance;

			var txHash = tx.GetHash();
			for(int i = 0 ; i < tx.VOut.Length ; i++)
			{
				foreach(var key in _Keys)
				{
					if(tx.VOut[i].IsTo(key.PubKey))
					{
						var entry = new WalletEntry();
						entry.Block = block;
						entry.ScriptPubKey = tx.VOut[i].ScriptPubKey;
						entry.OutPoint = new OutPoint(tx, i);
						entry.Value = tx.VOut[i].Value;
						_Pools.PushEntry(entry, blockType);
					}
				}
			}

			foreach(var txin in tx.VIn)
			{
				foreach(var key in _Keys)
				{
					if(txin.IsFrom(key.PubKey))
					{
						var entry = new WalletEntry();
						entry.Block = block;
						entry.OutPoint = txin.PrevOut;
						_Pools.PushEntry(entry, blockType);
					}
				}
			}

			OnBalanceChanged(tx, oldBalance, Balance);
		}

		private void OnBalanceChanged(Transaction tx, Money oldBalance, Money newBalance)
		{
			if(oldBalance < newBalance)
			{
				if(CoinsReceived != null)
					CoinsReceived(this, tx, oldBalance, newBalance);
			}
			if(oldBalance > newBalance)
			{
				if(CoinsSent != null)
					CoinsSent(this, tx, oldBalance, newBalance);
			}
			if(oldBalance != newBalance)
			{
				if(BalanceChanged != null)
					BalanceChanged(this, tx, oldBalance, newBalance);
			}
		}

		public void ReceiveBlock(Block block, BlockType blockType = BlockType.Main)
		{
			var hash = block.GetHash();
			foreach(var tx in block.Vtx)
			{
				ReceiveTransaction(hash, blockType, tx);
			}
		}

		public bool CompleteTx(Transaction tx)
		{
			return CompleteTx(tx, Money.Zero);
		}
		public bool CompleteTx(Transaction tx, Money fees)
		{
			var entries = Pools.Available.GetEntriesToCover(tx.TotalOut + fees);
			if(entries == null)
				return false;

			foreach(var entry in entries)
			{
				tx.AddInput(new TxIn(entry.OutPoint));
			}
			var surplus = entries.Sum(i => i.TxOut.Value) - fees - tx.TotalOut;
			if(surplus != Money.Zero)
				tx.AddOutput(surplus, GetKey().PubKey.ID);

			for(int i = 0 ; i < entries.Length ; i++)
			{
				var entry = entries[i];
				var vin = tx.VIn[i];
				var key = GetKey(entry.TxOut.ScriptPubKey.GetDestination());
				tx.VIn[i].ScriptSig = new PayToPubkeyHashScriptTemplate().GenerateOutputScript(key.PubKey);
				var hash = tx.VIn[i].ScriptSig.SignatureHash(tx, i, SigHash.All);
				var sig = key.Sign(hash);
				tx.VIn[i].ScriptSig = new PayToPubkeyHashScriptTemplate().GenerateInputScript(new TransactionSignature(sig, SigHash.All), key.PubKey);
			}
			return true;
		}

		public bool SignedByMe(Transaction tx)
		{
			for(int i = 0 ; i < tx.VIn.Length ; i++)
			{
				var vin = tx.VIn[i];
				var key = GetKey(vin.ScriptSig.GetSourcePubKey());
				if(key == null)
					return false;
				var pubkeyScript = new PayToPubkeyHashScriptTemplate().GenerateOutputScript(key.PubKey);
				var eval = new ScriptEvaluationContext();
				eval.SigHash = SigHash.All;
				if(!eval.VerifyScript(vin.ScriptSig, pubkeyScript, tx, i))
					return false;
			}
			return true;
		}



		Random _RandKey = new Random();
		private Key GetKey()
		{
			return _Keys[_RandKey.Next(0, _Keys.Count)];
		}

		public Key GetKey(PubKey pubKey)
		{
			return _Keys.FirstOrDefault(k => k.PubKey == pubKey);
		}
		public Key GetKey(KeyId pubKeyId)
		{
			return _Keys.FirstOrDefault(k => k.PubKey.ID == pubKeyId);
		}

		public Transaction CreateSend(BitcoinAddress bitcoinAddress, Money value)
		{
			if(bitcoinAddress.Network != Network)
				throw new InvalidOperationException("This bitcoin address does not belong to the network");
			Transaction tx = new Transaction();
			tx.AddOutput(value, bitcoinAddress);
			this.CompleteTx(tx);
			return tx;
		}



		public void Reorganize(BlockChain chain)
		{
			Pools.Reorganize(chain);
		}
	}
}
