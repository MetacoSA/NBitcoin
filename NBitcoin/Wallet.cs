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
		public PubKey PubKey
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

		public bool Income
		{
			get
			{
				return Value != null;
			}
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
			if(!entry.Income)
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
		internal void PushEntry(WalletEntry entry)
		{
			_Entries.Add(entry);
			InvalidateCache();
		}

		private void InvalidateCache()
		{
			_Balance = null;
			_Unspent = null;
			_Spent = null;
		}

		Money _Balance;
		public Money Balance
		{
			get
			{
				if(_Balance == null)
				{
					_Balance = Unspent.Sum(w => w.Value);
				}
				return _Balance;
			}
		}

		WalletEntry[] _Unspent;
		public WalletEntry[] Unspent
		{
			get
			{
				if(_Unspent == null)
				{
					RefreshSpent();
				}
				return _Unspent;
			}
		}

		WalletEntry[] _Spent;
		public WalletEntry[] Spent
		{
			get
			{
				if(_Spent == null)
				{
					RefreshSpent();
				}
				return _Spent;
			}
		}

		void RefreshSpent()
		{
			HashSet<OutPoint> spent = new HashSet<OutPoint>();
			Dictionary<OutPoint, WalletEntry> all = new Dictionary<OutPoint, WalletEntry>();
			foreach(var entry in _Entries)
			{
				if(entry.Income)
				{
					WalletEntry existingEntry = null;
					if(all.TryGetValue(entry.OutPoint, out existingEntry))
					{
						if(existingEntry.Confidence < entry.Confidence)
							all[entry.OutPoint] = entry;
					}
					else
					{
						all.Add(entry.OutPoint, entry);
					}
				}
				else
					spent.Add(entry.OutPoint);
			}
			_Unspent = all.Values.Where(i => !spent.Contains(i.OutPoint)).ToArray();
			_Spent = all.Values.Where(i => spent.Contains(i.OutPoint)).ToArray();
		}

		public WalletEntry[] GetEntriesToCover(Money money)
		{
			if(Balance < money)
				return null;
			var result = new List<WalletEntry>();
			Money current = Money.Zero;
			var unspent = Unspent.OrderBy(o => o.Value.Satoshi).ToArray();
			int i = 0;
			while(current < money)
			{
				result.Add(unspent[i]);
				current += unspent[i].Value;
				i++;
			}
			return result.ToArray();
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
			Func<Key, WalletEntry> createEntry = (key) => new WalletEntry()
			{
				PubKey = key.PubKey,
				Block = block
			};

			for(int i = 0 ; i < tx.VOut.Length ; i++)
			{
				foreach(var key in _Keys)
				{
					if(tx.VOut[i].IsTo(key.PubKey))
					{
						var entry = createEntry(key);
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
						var entry = createEntry(key);
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
			var ins = Pools.Confirmed.GetEntriesToCover(tx.TotalOut + fees);
			if(ins == null)
				return false;

			foreach(var vin in ins)
			{
				tx.AddInput(new TxIn(vin.OutPoint));
			}
			for(int i = 0 ; i < tx.VIn.Length ; i++)
			{
				var entry = ins[i];
				var vin = tx.VIn[i];
				var key = GetKey(entry.PubKey);
				tx.VIn[i].ScriptSig = new PayToPubkeyHashScriptTemplate().GenerateInputScript(entry.PubKey);
				var hash = tx.VIn[i].ScriptSig.SignatureHash(tx, i, SigHash.All);
				var sig = key.Sign(hash);
				tx.VIn[i].ScriptSig = new PayToPubkeyHashScriptTemplate().GenerateInputScript(new TransactionSignature(sig, SigHash.All), entry.PubKey);
			}

			var surplus = ins.Sum(i => i.Value) - fees - tx.TotalOut;
			if(surplus != Money.Zero)
				tx.AddOutput(surplus, GetKey().PubKey.ID);
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

		public Transaction CreateSend(BitcoinAddress bitcoinAddress, Money value)
		{
			if(bitcoinAddress.Network != Network)
				throw new InvalidOperationException("This bitcoin address does not belong to the network");
			Transaction tx = new Transaction();
			tx.AddOutput(value, bitcoinAddress);
			this.CompleteTx(tx);
			return tx;
		}



	}
}
