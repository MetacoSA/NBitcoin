using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum BlockType
	{
		Main,
		Side
	}

	public enum AccountEntryReason
	{
		ChainBlockChanged,
		Income,
		Outcome
	}
	public class AccountEntry : IBitcoinSerializable
	{
		public AccountEntry()
		{

		}
		public AccountEntry(AccountEntryReason reason, uint256 block, Spendable spendable, Money balanceChange)
		{
			_Block = block;
			_Spendable = spendable;
			_BalanceChange = balanceChange;
			_Reason = (byte)reason;
		}

		private uint256 _Block;
		public uint256 Block
		{
			get
			{
				return _Block;
			}
		}

		private byte _Reason;
		public AccountEntryReason Reason
		{
			get
			{
				return (AccountEntryReason)_Reason;
			}
		}


		private Spendable _Spendable;
		public Spendable Spendable
		{
			get
			{
				return _Spendable;
			}
		}

		private Money _BalanceChange = Money.Zero;
		public Money BalanceChange
		{
			get
			{
				return _BalanceChange;
			}
		}

		public AccountEntry Neutralize()
		{
			return new AccountEntry(AccountEntryReason.ChainBlockChanged, Block, Spendable, -BalanceChange);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			var b = _Block == null ? new uint256(0).ToBytes() : _Block.ToBytes();
			stream.ReadWrite(ref b);
			if(!stream.Serializing)
				_Block = new uint256(b) == 0 ? null : new uint256(b);
			stream.ReadWrite(ref _Reason);
			stream.ReadWrite(ref _Spendable);

			var change = BalanceChange.Satoshi.ToByteArray();
			stream.ReadWriteAsVarString(ref change);
			if(!stream.Serializing)
				_BalanceChange = new Money(new BigInteger(change));
		}

		#endregion
	}

	public enum WalletEntryType : byte
	{
		Income,
		Outcome
	}

	public class Spendable : IBitcoinSerializable
	{
		public Spendable()
		{

		}
		public Spendable(OutPoint output, TxOut txout)
		{
			if(output == null)
				throw new ArgumentNullException("output");
			if(txout == null)
				throw new ArgumentNullException("txout");
			_Out = txout;
			_OutPoint = output;
		}

		private OutPoint _OutPoint;
		public OutPoint OutPoint
		{
			get
			{
				return _OutPoint;
			}
		}
		private TxOut _Out;
		public TxOut TxOut
		{
			get
			{
				return _Out;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _OutPoint);
			if(stream.Serializing)
			{
				TxOutCompressor compressor = new TxOutCompressor(_Out);
				stream.ReadWrite(ref compressor);
			}
			else
			{
				TxOutCompressor compressor = new TxOutCompressor();
				stream.ReadWrite(ref compressor);
				_Out = compressor.TxOut;
			}
		}

		#endregion
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
				return Value == null ? WalletEntryType.Outcome : WalletEntryType.Income;
			}
		}
	}


	public class WalletPools : IBitcoinSerializable
	{
		public WalletPools()
		{
			_Confirmed = new WalletPool();
			_Available = new WalletPool();
			_Unconfirmed = new WalletPool();
		}

		private WalletPool _Unconfirmed;
		public WalletPool Unconfirmed
		{
			get
			{
				return _Unconfirmed;
			}
		}
		private WalletPool _Available;
		public WalletPool Available
		{
			get
			{
				return _Available;
			}
		}
		private WalletPool _Confirmed;
		public WalletPool Confirmed
		{
			get
			{
				return _Confirmed;
			}
		}

		public void Update(Chain chain)
		{
			NeutralizeUnconfirmed(chain, Confirmed);
			NeutralizeUnconfirmed(chain, Available);
		}

		private void NeutralizeUnconfirmed(Chain chain, WalletPool account)
		{
			var unconfirmed = account.GetInChain(chain, false).Where(e => e.Reason != AccountEntryReason.ChainBlockChanged);
			foreach(var e in unconfirmed)
			{
				account.PushAccountEntry(e.Neutralize());
			}

			var confirmedCanceled = account.GetInChain(chain, true).Where(e => e.Reason == AccountEntryReason.ChainBlockChanged);
			foreach(var e in confirmedCanceled)
			{
				account.PushAccountEntry(e.Neutralize());
			}
		}



		public void PushEntries(IEnumerable<WalletEntry> entries, BlockType? blockType)
		{
			foreach(var entry in entries)
			{
				PushEntry(entry, blockType);
			}
		}


		private void PushAccountEntry(AccountEntry entry)
		{
			if(entry.Spendable.TxOut.Value < Money.Zero)
				Available.PushAccountEntry(entry);
			if(entry.Block != null)
			{
				Available.PushAccountEntry(entry);
				Confirmed.PushAccountEntry(entry);
			}
		}
		public void PushEntry(WalletEntry entry, BlockType? blockType)
		{
			if(blockType == null && entry.Block != null)
				throw new ArgumentException("An entry coming from a block should have a blocktype");

			Unconfirmed.PushEntry(entry);
			if(entry.Type == WalletEntryType.Outcome)
				Available.PushEntry(entry);
			if(entry.Block != null && blockType == BlockType.Main)
			{
				Available.PushEntry(entry);
				Confirmed.PushEntry(entry);
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Unconfirmed);
			stream.ReadWrite(ref _Available);
			stream.ReadWrite(ref _Confirmed);
		}

		#endregion
	}
	public class WalletPool : IBitcoinSerializable
	{
		List<AccountEntry> _AccountEntries = new List<AccountEntry>();

		internal AccountEntry PushAccountEntry(uint256 block, Spendable spendable, Money balanceChange)
		{
			return PushAccountEntry(new AccountEntry(balanceChange < 0 ? AccountEntryReason.Outcome : AccountEntryReason.Income, block, spendable, balanceChange));
		}
		internal AccountEntry PushAccountEntry(AccountEntry entry)
		{
			try
			{
				if(entry.Reason != AccountEntryReason.ChainBlockChanged)
				{
					if(entry.BalanceChange < Money.Zero)
					{
						if(!_Unspent.Remove(entry.Spendable.OutPoint))
							return null;
					}
					if(entry.BalanceChange > Money.Zero)
					{
						if(!_Unspent.TryAdd(entry.Spendable.OutPoint, entry.Spendable))
							return null;
					}
					if(entry.BalanceChange == Money.Zero)
						return null;
				}
				else
				{
					if(entry.BalanceChange < Money.Zero)
					{
						if(!_Unspent.Remove(entry.Spendable.OutPoint))
							return null;
					}
					if(entry.BalanceChange > Money.Zero)
					{
						if(!_Unspent.TryAdd(entry.Spendable.OutPoint, entry.Spendable))
							return null;
					}
				}
				_AccountEntries.Add(entry);
				_Balance += entry.BalanceChange;
				return entry;
			}
			finally
			{
#if DEBUG
				if(_Balance != Unspent.Select(o => o.TxOut.Value).Sum())
					throw new NotSupportedException("Something is going wrong");
#endif
			}
		}

		internal void PushEntry(WalletEntry entry)
		{
			if(entry.Type == WalletEntryType.Income)
			{
				var spendable = entry.GetSpendable();
				PushAccountEntry(entry.Block, spendable, spendable.TxOut.Value);
			}
			else if(entry.Type == WalletEntryType.Outcome)
			{
				if(_Unspent.ContainsKey(entry.OutPoint))
				{
					var spendable = _Unspent[entry.OutPoint];
					PushAccountEntry(entry.Block, spendable, -spendable.TxOut.Value);
				}
			}
		}




		Money _Balance = Money.Zero;
		public Money Balance
		{
			get
			{
				return _Balance;
			}
		}





		Dictionary<OutPoint, Spendable> _Unspent = new Dictionary<OutPoint, Spendable>();
		public IEnumerable<Spendable> Unspent
		{
			get
			{
				return _Unspent.Values;
			}
		}

		public Spendable[] GetEntriesToCover(Money money)
		{
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


		//Do not get all entries, but only the one you can generate with spent/unspent.
		public AccountEntry[] GetInChain(Chain chain, bool value)
		{
			Dictionary<OutPoint, AccountEntry> entries = new Dictionary<OutPoint, AccountEntry>();
			foreach(var entry in _AccountEntries)
			{
				if(entry.Block == null)
					continue;

				if(chain.Contains(entry.Block) == value)
				{
					if(entry.Reason == AccountEntryReason.Income && _Unspent.ContainsKey(entry.Spendable.OutPoint))
						entries.AddOrReplace(entry.Spendable.OutPoint, entry);
					if(entry.Reason == AccountEntryReason.ChainBlockChanged && !_Unspent.ContainsKey(entry.Spendable.OutPoint))
						entries.AddOrReplace(entry.Spendable.OutPoint, entry);
				}
			}
			return entries.Values.ToArray();
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			foreach(var entry in _AccountEntries)
			{
				builder.AppendLine(entry.BalanceChange.ToString(true));
			}

			return builder.ToString();
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
				stream.ReadWrite(ref _AccountEntries);
			else
			{
				var entries = new List<AccountEntry>();
				stream.ReadWrite(ref entries);
				foreach(var e in entries)
					PushAccountEntry(e);
			}

		}

		#endregion
	}

	public delegate void WalletBalanceChangedDelegate(Wallet wallet, Transaction tx, Money oldBalance, Money newBalance);
	public class Wallet : IBitcoinSerializable
	{
		public event WalletBalanceChangedDelegate BalanceChanged;
		public event WalletBalanceChangedDelegate CoinsReceived;
		public event WalletBalanceChangedDelegate CoinsSent;

		private Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		byte _NetworkByte;

		private Wallet()
		{

		}
		public Wallet(Network network)
		{
			_NetworkByte = network == Network.Main ? (byte)0 :
							network == Network.TestNet ? (byte)1 :
							network == Network.RegTest ? (byte)2 : (byte)3;
			InitNetwork();
			this._Network = network;
			this._Pools = new WalletPools();
		}

		private void InitNetwork()
		{
			_Network = _NetworkByte == 0 ? Network.Main :
				_NetworkByte == 1 ? Network.TestNet :
				_NetworkByte == 2 ? Network.RegTest : null;
			if(_Network == null)
				throw new FormatException("Incorrect network byte detected");
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

		public void UnconfirmedTransaction(Transaction tx)
		{
			ReceiveTransaction(null, null, tx);
		}


		private void ReceiveTransaction(uint256 block, BlockType? blockType, Transaction tx)
		{
			var oldBalance = Balance;

			var txHash = tx.GetHash();
			for(int i = 0 ; i < tx.Outputs.Count ; i++)
			{
				foreach(var key in _Keys)
				{
					if(tx.Outputs[i].IsTo(key.PubKey))
					{
						var entry = new WalletEntry();
						entry.Block = block;
						entry.ScriptPubKey = tx.Outputs[i].ScriptPubKey;
						entry.OutPoint = new OutPoint(tx, i);
						entry.Value = tx.Outputs[i].Value;
						_Pools.PushEntry(entry, blockType);
					}
				}
			}

			foreach(var txin in tx.Inputs)
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

		private void ReceiveBlock(Block block, BlockType blockType = BlockType.Main)
		{
			var hash = block.GetHash();
			foreach(var tx in block.Transactions)
			{
				ReceiveTransaction(hash, blockType, tx);
			}
		}

		public bool CompleteTx(Transaction tx, WalletPool pool = null)
		{
			return CompleteTx(tx, Money.Zero, pool);
		}
		public bool CompleteTx(Transaction tx, Money fees, WalletPool pool = null)
		{
			pool = pool ?? Pools.Available;
			var entries = pool.GetEntriesToCover(tx.TotalOut + fees);
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
				var vin = tx.Inputs[i];
				var key = GetKey(entry.TxOut.ScriptPubKey.GetDestination());
				tx.Inputs[i].ScriptSig = new PayToPubkeyHashTemplate().GenerateScriptPubKey(key.PubKey);
				var hash = tx.Inputs[i].ScriptSig.SignatureHash(tx, i, SigHash.All);
				var sig = key.Sign(hash);
				tx.Inputs[i].ScriptSig = new PayToPubkeyHashTemplate().GenerateScriptSig(new TransactionSignature(sig, SigHash.All), key.PubKey);
			}
			return true;
		}

		public bool SignedByMe(Transaction tx)
		{
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				var vin = tx.Inputs[i];
				var key = GetKey(vin.ScriptSig.GetSourcePubKey());
				if(key == null)
					return false;
				var pubkeyScript = new PayToPubkeyHashTemplate().GenerateScriptPubKey(key.PubKey);
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



		Chain _CurrentChain;

		public bool Update(Chain chain, IndexedBlockStore store)
		{
			if(_CurrentChain == null || !chain.SameTip(_CurrentChain))
			{
				List<BlockIndex> unprocessed = null;
				Pools.Update(chain);
				if(_CurrentChain == null)
				{
					_CurrentChain = chain.Clone();
					unprocessed = chain.ToEnumerable(false).ToList();
				}
				else
				{
					var fork = _CurrentChain.SetTip(chain.Tip);
					unprocessed = _CurrentChain.EnumerateAfter(fork).ToList();
				}

				foreach(var block in unprocessed)
				{
					ReceiveBlock(store.Get(block.HashBlock));
				}
				return true;
			}
			return false;
		}

		public void Save(Stream stream)
		{
			this.ReadWrite(stream, true);
		}

		public static Wallet Load(Stream stream)
		{
			Wallet w = new Wallet();
			BitcoinStream bitStream = new BitcoinStream(stream, false);
			bitStream.ReadWrite(ref w);
			return w;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _NetworkByte);
			InitNetwork();
			stream.ReadWrite(ref _Keys);
			stream.ReadWrite(ref _Pools);
		}

		#endregion
	}
}
