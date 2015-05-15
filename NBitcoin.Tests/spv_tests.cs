using NBitcoin.SPV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public Coin GiveMoney(IDestination destination, Money value)
		{
			return GiveMoney(destination.ScriptPubKey, value);
		}

		public Coin GiveMoney(Script scriptPubKey, Money value)
		{
			var tx = new Transaction();
			tx.Outputs.Add(new TxOut(value, scriptPubKey));
			var h = tx.GetHash();
			Mempool.Add(h, tx);
			return tx.Outputs.AsCoins().FirstOrDefault();
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
			var coin = builder.GiveMoney(bob, Money.Coins(1.0m));
			Assert.True(tracker.NotifyTransaction(builder.Mempool[coin.Outpoint.Hash]));

			Key alice = new Key();
			var tx = builder.SpendCoin(coin, alice, Money.Coins(0.6m));
			Assert.True(tracker.NotifyTransaction(tx));

			var block = builder.FindBlock();

			foreach(var btx in block.Transactions)
			{
				Assert.True(tracker.NotifyTransaction(btx, builder.Chain.GetBlock(btx.GetHash()), block));
				Assert.True(tracker.NotifyTransaction(btx, builder.Chain.GetBlock(btx.GetHash()), block)); //Idempotent
			}
		}
	}
}
