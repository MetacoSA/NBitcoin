using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum AccountEntryReason
	{
		ChainBlockChanged,
		Income,
		Outcome,
		Lock,
		Unlock
	}
	public class AccountEntry : IBitcoinSerializable
	{
		public AccountEntry()
		{

		}
		public AccountEntry(AccountEntryReason reason, uint256 block, Spendable spendable, Money balanceChange, uint256 txId)
		{
			_Block = block;
			_Spendable = spendable;
			_BalanceChange = balanceChange;
			_Reason = (byte)reason;
			_TxId = txId;
		}

		uint256 _TxId;
		public uint256 TxId
		{
			get
			{
				if(Reason == AccountEntryReason.Income)
					return Spendable.OutPoint.Hash;
				return _TxId;
			}
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
			return new AccountEntry(AccountEntryReason.ChainBlockChanged, Block, Spendable, -BalanceChange, null);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			var b = _Block == null ? new uint256(0).ToBytes() : _Block.ToBytes();
			stream.ReadWrite(ref b);
			if(!stream.Serializing)
				_Block = new uint256(b) == 0 ? null : new uint256(b);
			stream.ReadWrite(ref _Reason);
			if(_Reason != (byte)AccountEntryReason.Income)
			{
				var bytes = _TxId == null ? new byte[0] : _TxId.ToBytes();
				stream.ReadWriteAsVarString(ref bytes);
				if(!stream.Serializing)
				{
					_TxId = bytes.Length == 0 ? null : new uint256(bytes);
				}
			}
			stream.ReadWrite(ref _Spendable);

			var change = new BigInteger(BalanceChange.Satoshi).ToByteArray();
			stream.ReadWriteAsVarString(ref change);
			if(!stream.Serializing)
				_BalanceChange = new Money(new BigInteger(change));
		}

		#endregion

		public override string ToString()
		{
			return Reason + " " + BalanceChange.ToString(true, false);
		}
	}
	public class Account
	{
		public Account()
			: this(null as ObjectStream<AccountEntry>)
		{

		}
		public Account(ObjectStream<AccountEntry> entries)
		{
			if(entries == null)
				entries = new StreamObjectStream<AccountEntry>();
			entries.Rewind();
			_Entries = entries;
			Process();
		}

		public Account(Account copied)
			: this(copied, null)
		{
		}
		public Account(Account copied, ObjectStream<AccountEntry> entries)
		{
			if(entries == null)
				entries = new StreamObjectStream<AccountEntry>();
			_Entries = entries;
			copied.Entries.Rewind();
			entries.Rewind();
			foreach(var entry in copied.Entries.Enumerate())
			{
				if(_NextToProcess < copied._NextToProcess)
				{
					PushAccountEntry(entry);
				}
				else
					entries.WriteNext(entry);
			}
		}


		private readonly ObjectStream<AccountEntry> _Entries;
		public ObjectStream<AccountEntry> Entries
		{
			get
			{
				return _Entries;
			}
		}

		private int _NextToProcess;


		public AccountEntry PushAccountEntry(AccountEntry entry)
		{
			entry = Process(entry);
			if(entry != null)
			{
				_Entries.WriteNext(entry);
				_NextToProcess++;
			}
			return entry;
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
		public IEnumerable<Spendable> Locked
		{
			get
			{
				return _Unspent.Values.Where(v => v.IsLocked);
			}
		}

		public Spendable[] GetEntriesToCover(Money money, bool lockUsed = true)
		{
			var result = new List<Spendable>();
			Money current = Money.Zero;
			var unspent = Unspent
							.Where(o => !o.IsLocked)
							.OrderBy(o => o.TxOut.Value.Satoshi).ToArray();
			int i = 0;
			while(current < money)
			{
				if(unspent.Length <= i)
					return null;
				result.Add(unspent[i]);
				current += unspent[i].TxOut.Value;
				i++;
			}
			if(lockUsed)
			{
				foreach(var r in result)
				{
					r.IsLocked = true;
				}
			}
			return result.ToArray();
		}



		//Do not get all entries, but only the one you can generate with spent/unspent.
		public AccountEntry[] GetInChain(Chain chain, bool value)
		{
			List<AccountEntry> entries = new List<AccountEntry>();
			foreach(var entry in AccountEntries.Where(e => e.Reason != AccountEntryReason.ChainBlockChanged))
			{
				if(entry.Block == null)
					continue;

				if(chain.Contains(entry.Block, false) == value)
				{
					entries.Add(entry);
				}
			}
			return entries.ToArray();
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			foreach(var entry in AccountEntries)
			{
				if(entry.Reason != AccountEntryReason.Lock && entry.Reason != AccountEntryReason.Unlock)
					builder.AppendLine(entry.BalanceChange.ToString(true));
			}

			return builder.ToString();
		}


		public void Process(int untilPosition = Int32.MaxValue)
		{
			if(untilPosition <= _NextToProcess)
				return;

			Entries.GoTo(_NextToProcess);
			while(true)
			{
				if(untilPosition == _NextToProcess)
					break;

				var change = Entries.ReadNext();
				if(change == null)
					break;
				Process(change);
				_NextToProcess = Entries.Position;
			}
		}

		internal HashSet<OutPoint> _Locked = new HashSet<OutPoint>();
		AccountEntry Process(AccountEntry entry)
		{
			if(entry.Spendable._Account != null && entry.Spendable._Account != this)
				throw new InvalidOperationException("Entry already processed by another account");
			entry.Spendable._Account = this;
			try
			{
				if(entry.Reason == AccountEntryReason.Income || entry.Reason == AccountEntryReason.Outcome)
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
				else if(entry.Reason == AccountEntryReason.Lock || entry.Reason == AccountEntryReason.Unlock)
				{
					if(entry.Reason == AccountEntryReason.Lock)
					{
						_Locked.Add(entry.Spendable.OutPoint);
					}
					else
					{
						_Locked.Remove(entry.Spendable.OutPoint);
					}
				}
				else if(entry.Reason == AccountEntryReason.ChainBlockChanged)
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


		public IEnumerable<AccountEntry> AccountEntries
		{
			get
			{
				List<AccountEntry> entries = new List<AccountEntry>();
				_Entries.Rewind();
				while(_Entries.Position < _NextToProcess)
				{
					if(_Entries.EOF)
						throw new InvalidOperationException("The entries stream is shorter than during initialization");
					entries.Add(_Entries.ReadNext());
				}
				return entries;
			}
		}

		public Account Clone()
		{
			return Clone(null);
		}
		public Account Clone(ObjectStream<AccountEntry> entries)
		{
			return new Account(this, entries);
		}

		public void PushAccountEntries(ObjectStream<AccountEntry> entries)
		{
			foreach(var entry in entries.Enumerate())
			{
				PushAccountEntry(entry);
			}
		}
	}
}