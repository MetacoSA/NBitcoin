using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class ScanState : IDisposable
	{
		public ScanState(Scanner scanner,
						 Chain chain,
						 Account account, int startHeight)
		{
			if(scanner == null)
				throw new ArgumentNullException("scanner");


			_Account = account;
			_Chain = chain;
			_Scanner = scanner;
			_StartHeight = startHeight;
		}

		private readonly int _StartHeight;
		public int StartHeight
		{
			get
			{
				return _StartHeight;
			}
		}

		private readonly Scanner _Scanner;
		public Scanner Scanner
		{
			get
			{
				return _Scanner;
			}
		}


		private readonly Account _Account;
		public Account Account
		{
			get
			{
				return _Account;
			}
		}

		private readonly Chain _Chain;
		public Chain Chain
		{
			get
			{
				return _Chain;
			}
		}

		public bool CheckDoubleSpend
		{
			get;
			set;
		}

		public bool Process(ChainBase mainChain, IBlockProvider blockProvider)
		{
			var chainCopy = Chain.Clone();
			var chainPosition = chainCopy.Changes.Position;
			var accountCopy = Account.Clone();
			var accountPosition = accountCopy.Entries.Position;

			bool newChain = false;
			if(!chainCopy.Initialized)
			{
				newChain = true;

				var firstBlock = mainChain.GetBlock(StartHeight);
				chainCopy.Initialize(firstBlock.Header, StartHeight);
			}
			var forkBlock = mainChain.FindFork(chainCopy);
			if(forkBlock.HashBlock != chainCopy.Tip.HashBlock)
			{
				var subChain = chainCopy.CreateSubChain(forkBlock, false, chainCopy.Tip, true);
				chainCopy.SetTip(chainCopy.GetBlock(forkBlock.Height));
				foreach(var e in accountCopy.GetInChain(subChain, true)
										.Where(c => c.Reason != AccountEntryReason.Lock && c.Reason != AccountEntryReason.Unlock)
										.Reverse())
				{
					var neutralized = e.Neutralize();
					accountCopy.PushAccountEntry(neutralized);
				}
			}

			var unprocessedBlocks = mainChain.ToEnumerable(true)
									   .TakeWhile(block => block != forkBlock)
									   .Concat(newChain ? new ChainedBlock[] { forkBlock } : new ChainedBlock[0])
									   .Reverse().ToArray();
			foreach(var block in unprocessedBlocks)
			{
				List<byte[]> searchedData = new List<byte[]>();
				Scanner.GetScannedPushData(searchedData);
				foreach(var unspent in accountCopy.Unspent)
				{
					searchedData.Add(unspent.OutPoint.ToBytes());
				}

				var fullBlock = blockProvider.GetBlock(block.HashBlock, searchedData);
				if(fullBlock == null)
					continue;

				List<Tuple<OutPoint, AccountEntry>> spents = new List<Tuple<OutPoint, AccountEntry>>();
				foreach(var spent in FindSpent(fullBlock, accountCopy.Unspent))
				{
					var entry = new AccountEntry(AccountEntryReason.Outcome,
												block.HashBlock,
												spent.Spendable, -spent.Spendable.TxOut.Value, spent.TxId);
					spents.Add(Tuple.Create(entry.Spendable.OutPoint, entry));
				}

				if(CheckDoubleSpend)
				{
					var spentsDico = spents.ToDictionary(t => t.Item1, t => t.Item2);
					foreach(var spent in Scanner.FindSpent(fullBlock))
					{
						if(!spentsDico.ContainsKey(spent.PrevOut))
							return false;
					}
				}

				foreach(var spent in spents)
				{
					if(accountCopy.PushAccountEntry(spent.Item2) == null)
						return false;
				}

				foreach(var coins in Scanner.ScanCoins(fullBlock, (int)block.Height))
				{
					int i = 0;
					foreach(var output in coins.Coins.Outputs)
					{
						if(!output.IsNull)
						{
							var entry = new AccountEntry(AccountEntryReason.Income, block.HashBlock,
												new Spendable(new OutPoint(coins.TxId, i), output), output.Value, null);
							if(accountCopy.PushAccountEntry(entry) == null)
								return false;
						}
						i++;
					}
				}

				chainCopy.SetTip(block);
			}

			accountCopy.Entries.GoTo(accountPosition);
			Account.PushAccountEntries(accountCopy.Entries);

			chainCopy.Changes.GoTo(chainPosition);
			Chain.PushChanges(chainCopy.Changes);
			return true;
		}

		class Spent
		{
			public uint256 TxId;
			public Spendable Spendable;
		}
		IEnumerable<Spent> FindSpent(Block block, IEnumerable<Spendable> among)
		{
			var amongDico = among.ToDictionary(o => o.OutPoint);
			foreach(var spent in block
									.Transactions
									.Where(t => !t.IsCoinBase)
									.SelectMany(t => t.Inputs.Select(i => new
									{
										Tx = t,
										Input = i
									}))
									.Where(o => amongDico.ContainsKey(o.Input.PrevOut)))
			{
				var spendable = amongDico[spent.Input.PrevOut];
				amongDico.Remove(spent.Input.PrevOut);
				yield return new Spent()
				{
					TxId = spent.Tx.GetHash(),
					Spendable = spendable
				};
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			Account.Entries.Dispose();
			Chain.Changes.Dispose();
		}

		#endregion
	}
}
