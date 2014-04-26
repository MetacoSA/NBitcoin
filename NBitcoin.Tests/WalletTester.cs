using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class WalletTester
	{
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

		internal Transaction GiveMoney(Money amount, BlockType? blockType)
		{
			var tx = TestUtils.CreateFakeTx(amount, Address);
			return RecieveTransaction(tx, blockType);
		}

		public Transaction RecieveTransaction(Transaction tx, BlockType? blockType)
		{
			if(blockType == null)
			{
				Wallet.ReceiveTransaction(tx);
				return tx;
			}

			var block = TestUtils.CreateFakeBlock(tx);
			Wallet.ReceiveBlock(block, blockType.Value);
			foreach(var c in _Chains)
			{
				c.Add(block);
			}
			return tx;
		}

		public Transaction Pay(Money money, BlockType? blockType)
		{
			var tx = new Transaction();
			tx.AddOutput(money, OtherKey.PubKey.GetAddress(TestNetwork));
			Wallet.CompleteTx(tx);
			return RecieveTransaction(tx, blockType);
		}

		List<BlockChain> _Chains = new List<BlockChain>();
		public BlockChain StartRecordChain()
		{
			var c = new BlockChain();
			_Chains.Add(c);
			return c;
		}
		public void StopRecordChain(BlockChain c)
		{
			_Chains.Remove(c);
		}

		public void AssertPools(BlockChain chain, string confirmedOperations, string availableOperations, string unconfirmedOperations)
		{
			AssertPools(Wallet, chain, confirmedOperations, availableOperations, unconfirmedOperations);
			MemoryStream ms = new MemoryStream();
			Wallet.Save(ms);
			ms.Position = 0;
			var walletCopy = Wallet.Load(ms);
			AssertPools(walletCopy, chain, confirmedOperations, availableOperations, unconfirmedOperations);
		}

		private void AssertPools(Wallet wallet, BlockChain chain, string confirmedOperations, string availableOperations, string unconfirmedOperations)
		{
			if(chain != null)
				wallet.Reorganize(chain);
			AssertAccount(wallet.Pools.Confirmed, confirmedOperations);
			AssertAccount(wallet.Pools.Available, availableOperations);
			AssertAccount(wallet.Pools.Unconfirmed, unconfirmedOperations);

			if(chain != null)
			{
				wallet.Reorganize(chain);
				//Nothing change
				AssertAccount(wallet.Pools.Confirmed, confirmedOperations);
				AssertAccount(wallet.Pools.Available, availableOperations);
				AssertAccount(wallet.Pools.Unconfirmed, unconfirmedOperations);
			}
		}

		[DebuggerHidden]
		private void AssertAccount(WalletPool walletPool, string operations)
		{
			Assert.Equal(operations, walletPool.ToString().Replace("\r\n", ""));
		}
	}
}
