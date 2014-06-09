using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class ScanState : IDisposable
	{
		bool _OwnPersister;
		public ScanState(Scanner scanner, ScanStatePersister persister, int startHeight)
		{
			if(persister == null)
				throw new ArgumentNullException("persister");
			if(scanner == null)
				throw new ArgumentNullException("scanner");


			_Account = new WalletPool();
			_Persister = persister;
			_OwnPersister = persister.Open(false);
			persister.Rewind();
			_Scanner = scanner;
			_StartHeight = startHeight;

			_Chain = new Chain(persister.ChainChanges);
			foreach(var entry in persister.AccountEntries.Enumerate())
			{
				_Account.PushAccountEntry(entry);
			}
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

		private readonly ScanStatePersister _Persister;
		public ScanStatePersister Persister
		{
			get
			{
				return _Persister;
			}
		}


		private readonly WalletPool _Account;
		public WalletPool Account
		{
			get
			{
				return _Account;
			}
		}

		private Chain _Chain;
		public Chain Chain
		{
			get
			{
				return _Chain;
			}
		}

		public void Process(Chain mainChain, IBlockProvider blockProvider)
		{
			bool newChain = false;
			if(!Chain.Initialized)
			{
				newChain = true;

				var firstBlock = mainChain.GetBlock(StartHeight);
				Chain.Initialize(firstBlock.Header, StartHeight);
			}
			var forkBlock = mainChain.FindFork(Chain);
			if(forkBlock.HashBlock != Chain.Tip.HashBlock)
			{
				Chain.SetTip(Chain.GetBlock(forkBlock.Height));
				foreach(var e in Account.GetInChain(Chain, false)
											.Where(e => e.Reason != AccountEntryReason.ChainBlockChanged))
				{
					var neutralized = e.Neutralize();
					Account.PushAccountEntry(neutralized);
					Persister.AccountEntries.WriteNext(neutralized);
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
				foreach(var unspent in Account.Unspent)
				{
					searchedData.Add(unspent.OutPoint.ToBytes());
				}

				var fullBlock = blockProvider.GetBlock(block.HashBlock, searchedData);
				if(fullBlock != null)
				{
					foreach(var spent in Scanner.FindSpent(fullBlock, Account.Unspent))
					{
						var entry = new AccountEntry(AccountEntryReason.Outcome,
													block.HashBlock,
													spent, -spent.TxOut.Value);
						Account.PushAccountEntry(entry);
						Persister.AccountEntries.WriteNext(entry);
					}
					foreach(var coins in Scanner.ScanCoins(fullBlock, (int)block.Height))
					{
						int i = 0;
						foreach(var output in coins.Coins.Outputs)
						{
							if(!output.IsNull)
							{
								var entry = new AccountEntry(AccountEntryReason.Income, block.HashBlock,
													new Spendable(new OutPoint(coins.TxId, i), output), output.Value);
								Account.PushAccountEntry(entry);
								Persister.AccountEntries.WriteNext(entry);
							}
							i++;
						}
					}
				}
				Chain.GetOrAdd(block.Header);
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if(_OwnPersister)
				Persister.Dispose();
		}

		#endregion
	}
}
