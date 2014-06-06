using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scanning
{
	public class ScanState : IDisposable
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
			_Persister = persister;
			_OwnPersister = persister.Open(false);
			persister.Rewind();
			if(!persister.AccountEntries.EOF || !persister.ChainChanges.EOF)
			{
				if(_OwnPersister)
					persister.Dispose();
				throw new ArgumentException("This persister already have existing data", "persister");
			}
			_StartHeight = startHeight;
			_Account = new WalletPool();
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

		bool _OwnPersister;
		public ScanState(Scanner scanner, ScanStatePersister persister)
		{
			if(persister == null)
				throw new ArgumentNullException("persister");
			if(scanner == null)
				throw new ArgumentNullException("scanner");


			_Account = new WalletPool();
			_Persister = persister;
			_OwnPersister = persister.Open(false);
			_Scanner = scanner;

			var chain = Chain.Load(persister.ChainChanges);
			if(chain == null)
				throw new ArgumentException("The persister holds an empty chain, please use ScanState(Scanner scanner, ScanStatePersister persister, int height) if you are creating a whole new ScanState", "persister");
			_StartHeight = chain.StartHeight;

			foreach(var entry in persister.AccountEntries.Enumerate())
			{
				_Account.PushAccountEntry(entry);
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
			if(Chain == null)
			{
				newChain = true;
				var startBlock = mainChain.GetBlock(StartHeight);
				_Chain = new Chain(startBlock.Header, StartHeight);
			}
			var forkBlock = mainChain.FindFork(Chain);
			if(forkBlock.HashBlock != Chain.Tip.HashBlock)
			{
				Chain.SetTip(Chain.GetBlock(forkBlock.Height), Persister.ChainChanges);
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
				Chain.GetOrAdd(block.Header, Persister.ChainChanges);
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
