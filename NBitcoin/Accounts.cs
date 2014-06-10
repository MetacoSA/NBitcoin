using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace NBitcoin
{
	public class Accounts
	{
		public Accounts()
		{
			_Confirmed = new Account();
			_Available = new Account();
			_Unconfirmed = new Account();
		}

		private Account _Unconfirmed;
		public Account Unconfirmed
		{
			get
			{
				return _Unconfirmed;
			}
		}
		private Account _Available;
		public Account Available
		{
			get
			{
				return _Available;
			}
		}
		private Account _Confirmed;
		public Account Confirmed
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

		private void NeutralizeUnconfirmed(Chain chain, Account account)
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
	}
}
