using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class ScanState
	{
		public class SerializedState : IBitcoinSerializable
		{
			public int StartHeight
			{
				get;
				set;
			}
			List<uint256> _Hashes = new List<uint256>();
			public List<uint256> Hashes
			{
				get
				{
					return _Hashes;
				}
				set
				{
					_Hashes = value;
				}
			}

			WalletPool _Confirmed;
			public WalletPool Confirmed
			{
				get
				{
					return _Confirmed;
				}
				set
				{
					_Confirmed = value;
				}
			}

			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref _Hashes);
				stream.ReadWrite(ref _Confirmed);
			}

			#endregion
		}
		public ScanState(Scanner scanner, ScanStatePersister persister, int startHeight)
		{
			if(persister == null)
				throw new ArgumentNullException("persister");
			if(scanner == null)
				throw new ArgumentNullException("scanner");
			persister.Rewind();
			if(!persister.AccountEntries.EOF || !persister.ProcessedBlocks.EOF)
			{
				throw new ArgumentException("This persister already have existing data", "persister");
			}
			persister.Init(startHeight);
			_StartHeight = startHeight;
			_Account = new WalletPool();
			_Persister = persister;
			_Scanner = scanner;
		}
		private readonly int _StartHeight;
		public int StartHeight
		{
			get
			{
				return _StartHeight;
			}
		}

		public ScanState(Scanner scanner, ScanStatePersister persister)
		{
			if(persister == null)
				throw new ArgumentNullException("persister");
			if(scanner == null)
				throw new ArgumentNullException("scanner");



			_StartHeight = persister.GetStartHeight();
			_Account = new WalletPool();
			_Persister = persister;
			_Scanner = scanner;

			var first = persister.ProcessedBlocks.ReadNext();
			if(first != null)
			{
				_Chain = new Chain(first, StartHeight);
				foreach(var block in persister.ProcessedBlocks.Enumerate())
				{
					_Chain.GetOrAdd(block);
				}
			}
			foreach(var entry in persister.AccountEntries.Enumerate())
			{
				_Account.PushAccountEntry(entry);
			}
		}

		Queue<BlockHeader> _UnsavedBlockProgress = new Queue<BlockHeader>();
		Queue<AccountEntry> _UnsavedAccountEntry = new Queue<AccountEntry>();

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
			if(Chain == null)
			{
				newChain = true;
				var startBlock = mainChain.GetBlock(StartHeight);
				_Chain = new Chain(startBlock.Header, StartHeight);
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
					_UnsavedAccountEntry.Enqueue(neutralized);
				}

			}

			Flush();

			var unprocessedBlocks = mainChain.ToEnumerable(true)
									   .TakeWhile(block => block != forkBlock)
									   .Concat(newChain ? new BlockIndex[] { forkBlock } : new BlockIndex[0])
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
						_UnsavedAccountEntry.Enqueue(entry);
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
								_UnsavedAccountEntry.Enqueue(entry);
							}
							i++;
						}
					}
				}
				Chain.GetOrAdd(block.Header);
				_UnsavedBlockProgress.Enqueue(block.Header);
				if(fullBlock != null)
					Flush();
			}
			Flush();
		}

		private void Flush()
		{
			while(_UnsavedAccountEntry.Count != 0)
			{
				Persister.AccountEntries.WriteNext(_UnsavedAccountEntry.Dequeue());
			}
			while(_UnsavedBlockProgress.Count != 0)
			{
				Persister.ProcessedBlocks.WriteNext(_UnsavedBlockProgress.Dequeue());
			}
		}
	}
}
