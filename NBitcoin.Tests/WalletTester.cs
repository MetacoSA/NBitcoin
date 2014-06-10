using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class WalletTester
	{
		public WalletTester([CallerMemberName] string name = null)
		{
			_Index = RepositoryTests.CreateIndexedStore(name);
			_Index.Put(Network.Main.GetGenesis());
		}
		IndexedBlockStore _Index;

		Network TestNetwork = Network.Main;
		Key _Key;
		private Key Key
		{
			get
			{
				if(_Key == null)
					_Key = new Key();
				return _Key;
			}
		}

		Key _OtherKey;
		private Key OtherKey
		{
			get
			{
				if(_OtherKey == null)
					_OtherKey = new Key();
				return _OtherKey;
			}
		}

		private BitcoinAddress _Address;
		public BitcoinAddress Address
		{
			get
			{
				if(_Address == null)
					_Address = Key.PubKey.GetAddress(TestNetwork);
				return _Address;
			}
		}

		Wallet _Wallet;
		public Wallet Wallet
		{
			get
			{
				if(_Wallet == null)
				{
					_Wallet = new Wallet(TestNetwork);
					_Wallet.AddKey(Key);
				}
				return _Wallet;
			}
		}

		internal Transaction GiveMoney(Money amount, params Chain[] chains)
		{
			var tx = TestUtils.CreateFakeTx(amount, Address);
			return RecieveTransaction(tx, chains);
		}

		private Transaction RecieveTransaction(Transaction tx, Chain[] chains)
		{
			var block = TestUtils.CreateFakeBlock(tx);
			if(chains != null)
			{
				foreach(var c in chains)
				{
					var localBlock = block.Clone();
					localBlock.Header.HashPrevBlock = c.Tip.Header.GetHash();
					c.GetOrAdd(localBlock.Header);
					_Index.Put(localBlock);
				}
			}
			else
			{
				Wallet.UnconfirmedTransaction(tx);
			}
			return tx;
		}

		public Transaction Pay(Money money, bool fromConfirmedPool, params Chain[] chains)
		{
			var tx = new Transaction();
			tx.AddOutput(money, OtherKey.PubKey.GetAddress(Network.Main));
			Wallet.CompleteTx(tx, fromConfirmedPool ? Wallet.Accounts.Confirmed : Wallet.Accounts.Available);
			return RecieveTransaction(tx, chains);
		}


		public void AssertPools(string confirmedOperations, string availableOperations, string unconfirmedOperations)
		{
			AssertPools(Wallet, confirmedOperations, availableOperations, unconfirmedOperations);
			//MemoryStream ms = new MemoryStream();
			//Wallet.Save(ms);
			//ms.Position = 0;
			//var walletCopy = Wallet.Load(ms);
			//AssertPools(walletCopy, chain, confirmedOperations, availableOperations, unconfirmedOperations);
		}

		private void AssertPools(Wallet wallet, string confirmedOperations, string availableOperations, string unconfirmedOperations)
		{
			AssertAccount(wallet.Accounts.Confirmed, confirmedOperations);
			AssertAccount(wallet.Accounts.Available, availableOperations);
			AssertAccount(wallet.Accounts.Unconfirmed, unconfirmedOperations);
		}

		[DebuggerHidden]
		private void AssertAccount(Account walletPool, string operations)
		{
			Assert.Equal(operations, walletPool.ToString().Replace("\r\n", ""));
		}

		public bool UpdateWallet(Chain chain)
		{
			return Wallet.Update(chain, _Index);
		}

		internal void AppendBlock(Transaction tx, Chain chain)
		{
			var block = TestUtils.CreateFakeBlock(tx);
			block.Header.HashPrevBlock = chain.Tip.HashBlock;
			chain.GetOrAdd(block.Header);
			_Index.Put(block);
			Assert.NotNull(_Index.Get(block.Header.GetHash())); //Seems not useful but already detected a bug in index thanks to that.
		}
	}
}
