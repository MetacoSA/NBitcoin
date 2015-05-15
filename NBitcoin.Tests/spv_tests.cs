using NBitcoin.SPV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class BlockchainBuilder
	{
		public BlockchainBuilder()
		{
			Chain = new ConcurrentChain(Network.TestNet);
			Mempool = new Dictionary<uint256, Transaction>();
			Blocks = new Dictionary<uint256, Block>();
		}

		public Transaction GiveMoney(IDestination destination, Money value)
		{
			return GiveMoney(destination.ScriptPubKey, value);
		}

		public Transaction GiveMoney(Script scriptPubKey, Money value)
		{
			var tx = new Transaction();
			tx.Outputs.Add(new TxOut(value, scriptPubKey));
			var h = tx.GetHash();
			Mempool.Add(h, tx);
			return tx;
		}

		public ConcurrentChain Chain
		{
			get;
			private set;
		}

		public Dictionary<uint256, Block> Blocks
		{
			get;
			private set;
		}

		public Dictionary<uint256, Transaction> Mempool
		{
			get;
			private set;
		}

		public Transaction SpendCoin(Coin coin, IDestination destination, Money money)
		{
			return SpendCoin(coin, destination.ScriptPubKey, money);
		}

		public Transaction SpendCoin(Coin coin, Script scriptPubKey, Money value)
		{
			var tx = new Transaction();
			tx.Inputs.Add(new TxIn(coin.Outpoint));
			tx.Outputs.Add(new TxOut(value, scriptPubKey));
			tx.Outputs.Add(new TxOut(coin.Amount - value, coin.ScriptPubKey));
			var h = tx.GetHash();
			Mempool.Add(h, tx);
			return tx;
		}

		public Block FindBlock()
		{
			Block b = new Block();
			foreach(var tx in Mempool)
				b.Transactions.Add(tx.Value);
			b.UpdateMerkleRoot();
			b.Header.HashPrevBlock = Chain.Tip.HashBlock;
			Chain.SetTip(b.Header);
			Mempool.Clear();
			return b;
		}
	}
	public class spv_tests
	{
		[Fact]
		public void CanTrackKey()
		{
			BlockchainBuilder builder = new BlockchainBuilder();
			Key bob = new Key();
			Tracker tracker = new Tracker();
			tracker.Add(bob);
			var tx1 = builder.GiveMoney(bob, Money.Coins(1.0m));
			var coin = tx1.Outputs.AsCoins().First();
			Assert.True(tracker.NotifyTransaction(tx1));
			Thread.Sleep(10);
			Key alice = new Key();
			var tx2 = builder.SpendCoin(coin, alice, Money.Coins(0.6m));
			Assert.True(tracker.NotifyTransaction(tx2));

			var block = builder.FindBlock();

			foreach(var btx in block.Transactions)
			{
				Assert.True(tracker.NotifyTransaction(btx, builder.Chain.GetBlock(block.GetHash()), block));
				Assert.True(tracker.NotifyTransaction(btx, builder.Chain.GetBlock(block.GetHash()), block)); //Idempotent
			}

			var transactions = tracker.GetWalletTransactions(builder.Chain);

			Assert.True(transactions.Count == 2);
			Assert.True(transactions[0].Transaction.GetHash() == tx2.GetHash());
			Assert.True(transactions[1].Transaction.GetHash() == tx1.GetHash());
			Assert.True(transactions[0].Balance == -Money.Coins(0.6m));

			var tx3 = builder.GiveMoney(bob, Money.Coins(0.01m));
			coin = tx3.Outputs.AsCoins().First();
			block = builder.FindBlock();
			Assert.True(tracker.NotifyTransaction(block.Transactions[0], builder.Chain.GetBlock(block.GetHash()), block));

			transactions = tracker.GetWalletTransactions(builder.Chain);
			Assert.True(transactions.Count == 3);
			Assert.True(transactions[0].Transaction.GetHash() == block.Transactions[0].GetHash());

			Assert.Equal(2, transactions.GetSpendableCoins().Count()); // the 1 change + 1 gift

			builder.Chain.SetTip(builder.Chain.Tip.Previous);
			transactions = tracker.GetWalletTransactions(builder.Chain);
			Assert.True(transactions.Count == 2);
		}

		[Fact]
		public void UnconfirmedTransactionsWithoutReceivedCoinsShouldNotShowUp()
		{
			BlockchainBuilder builder = new BlockchainBuilder();
			Tracker tracker = new Tracker();

			Key bob = new Key();
			tracker.Add(bob);
			Key alice = new Key();

			var tx1 = builder.GiveMoney(bob, Money.Coins(1.0m));
			var b = builder.FindBlock();
			tracker.NotifyTransaction(tx1, builder.Chain.Tip, b);

			var tx2 = builder.SpendCoin(tx1.Outputs.AsCoins().First(), alice, Money.Coins(0.1m));
			b = builder.FindBlock();
			tracker.NotifyTransaction(tx2, builder.Chain.Tip, b);

			var tx3 = builder.SpendCoin(tx2.Outputs.AsCoins().Skip(1).First(), alice, Money.Coins(0.2m));
			Assert.True(tracker.NotifyTransaction(tx3));

			var transactions = tracker.GetWalletTransactions(builder.Chain);
			Assert.True(transactions.Count == 3);
			Assert.True(transactions.Summary.UnConfirmed.TransactionCount == 1);
			Assert.True(transactions.Summary.UnConfirmed.Amount == -Money.Coins(0.2m));

			Assert.True(transactions.Summary.Confirmed.TransactionCount == 2);
			Assert.True(transactions.Summary.Confirmed.Amount == Money.Coins(0.9m));

			Assert.True(transactions.Summary.Spendable.TransactionCount == 3);
			Assert.True(transactions.Summary.Spendable.Amount == Money.Coins(0.7m));

			builder.Chain.SetTip(builder.Chain.GetBlock(1));

			transactions = tracker.GetWalletTransactions(builder.Chain);

			Action _ = () =>
			{
				Assert.True(transactions.Count == 3);
				Assert.True(transactions.Summary.Confirmed.TransactionCount == 1);
				Assert.True(transactions.Summary.Confirmed.Amount == Money.Coins(1.0m));
				Assert.True(transactions.Summary.Spendable.TransactionCount == 3);
				Assert.True(transactions.Summary.Spendable.Amount == Money.Coins(0.7m));
			};
			_();
			tracker.NotifyTransaction(tx2);	//Notifying tx2 should have no effect, since it already is accounted because it was orphaned
			_();
		}
	}
}
