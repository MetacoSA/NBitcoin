using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class WalletTests
	{
		Network TestNetwork = Network.Main;
		Key _MyKey;
		private Key MyKey
		{
			get
			{
				if(_MyKey == null)
					_MyKey = new Key();
				return _MyKey;
			}
		}

		private BitcoinAddress myAddress;
		public BitcoinAddress MyAddress
		{
			get
			{
				if(myAddress == null)
					myAddress = MyKey.PubKey.GetAddress(TestNetwork);
				return myAddress;
			}
		}

		Wallet myWallet;
		public Wallet MyWallet
		{
			get
			{
				if(myWallet == null)
				{
					myWallet = new Wallet(TestNetwork);
					myWallet.AddKey(MyKey);
				}
				return myWallet;
			}
		}

		[Fact]
		public void CustomTransactionSpending()
		{
			// We'll set up a wallet that receives a coin, then sends a coin of lesser value and keeps the change.
			Money v1 = Money.Parse("3.00");
			Transaction t1 = TestUtils.CreateFakeTx(v1, MyAddress);
			var block = new Block();
			block.AddTransaction(t1);

			MyWallet.ReceiveBlock(block);
			Assert.Equal(v1, MyWallet.Balance);
			Assert.Equal(v1, MyWallet.Pools.Available.Balance);
			Assert.Equal(v1, MyWallet.Pools.Verified.Balance);

			Key k2 = new Key();
			var a2 = k2.PubKey.GetAddress(MyAddress.Network);

			Transaction t2 = new Transaction();
			t2.AddOutput(Money.Parse("0.50"), a2);
			t2.AddOutput(Money.Parse("0.75"), a2);
			t2.AddOutput(Money.Parse("1.25"), a2);
			bool complete = MyWallet.CompleteTx(t2);

			// Do some basic sanity checks.
			Assert.True(complete);
			Assert.Equal(1, t2.VIn.Length);
			Assert.Equal(myAddress, t2.VIn[0].ScriptSig.GetSourcePubKey().GetAddress(TestNetwork));

			Assert.True(MyWallet.SignedByMe(t2));

			Assert.Equal(v1, MyWallet.Balance);
			MyWallet.ReceiveTransaction(t2);
			Assert.Equal(Money.Zero, MyWallet.Balance); //Not confirmed, change did not came back
			Assert.Equal(Money.Zero, MyWallet.Pools.Available.Balance);
			Assert.Equal(Money.Parse("0.50"), MyWallet.Pools.Verified.Balance); //Should get 0.50 back
			Assert.Equal(v1, MyWallet.Pools.Confirmed.Balance); //Not confirmed

			block = new Block();
			block.AddTransaction(t2);
			MyWallet.ReceiveBlock(block);//Finally confirmed
			Assert.Equal(Money.Parse("0.50"), MyWallet.Balance);
			Assert.Equal(Money.Parse("0.50"), MyWallet.Pools.Available.Balance);
			Assert.Equal(Money.Parse("0.50"), MyWallet.Pools.Verified.Balance);
			Assert.Equal(Money.Parse("0.50"), MyWallet.Pools.Confirmed.Balance);

			block = new Block();
			block.AddTransaction(t2);
			MyWallet.ReceiveBlock(block); //spend again ? no
			Assert.Equal(Money.Parse("0.50"), MyWallet.Balance);
			Assert.Equal(Money.Parse("0.50"), MyWallet.Pools.Available.Balance);
			Assert.Equal(Money.Parse("0.50"), MyWallet.Pools.Verified.Balance);
			Assert.Equal(Money.Parse("0.50"), MyWallet.Pools.Confirmed.Balance);
		}

		[Fact]
		public void SideChain()
		{
			// The wallet receives a coin on the main chain, then on a side chain. Only main chain counts towards balance.
			var v1 = Money.Parse("1.0");
			Transaction t1 = TestUtils.CreateFakeTx(v1, MyAddress);
			var block = new Block();
			block.AddTransaction(t1);

			MyWallet.ReceiveBlock(block, BlockType.Main);
			Assert.Equal(v1, MyWallet.Balance);
			Assert.Equal(v1, MyWallet.Pools.Available.Balance);
			Assert.Equal(v1, MyWallet.Pools.Confirmed.Balance);

			var v2 = Money.Parse("0.50");
			Transaction t2 = TestUtils.CreateFakeTx(v2, myAddress);
			block = new Block();
			block.AddTransaction(t2);
			MyWallet.ReceiveBlock(block, BlockType.Side);
			Assert.Equal(v2, MyWallet.Pools.Inactive.Balance);
			Assert.Equal(v1, MyWallet.Pools.Verified.Balance);

			Assert.Equal(v1, MyWallet.Balance);
		}

		[Fact]
		public void Balance()
		{
			// Receive 5 coins then half a coin.
			var v1 = Money.Parse("5.0");
			var v2 = Money.Parse("0.50");
			Transaction t1 = TestUtils.CreateFakeTx(v1, MyAddress);
			Transaction t2 = TestUtils.CreateFakeTx(v2, MyAddress);
			var b1 = TestUtils.CreateFakeBlock(t1);
			var b2 = TestUtils.CreateFakeBlock(t2);
			var expected = Money.Parse("5.50");
			Assert.Equal(Money.Zero, MyWallet.Balance);
			MyWallet.ReceiveBlock(b1, BlockType.Main);
			Assert.Equal(Money.Parse("5.0"), MyWallet.Balance);
			MyWallet.ReceiveBlock(b2, BlockType.Main);
			Assert.Equal(Money.Parse("5.50"), MyWallet.Balance);
			Assert.Equal(expected, MyWallet.Balance);

			// Now spend one coin.
			var v3 = Money.Parse("1.0");
			Transaction spend = MyWallet.CreateSend(new Key().PubKey.GetAddress(TestNetwork), v3);
			MyWallet.ReceiveTransaction(spend);
			Assert.True(MyWallet.SignedByMe(spend));

			// Available and estimated balances should not be the same. We don't check the exact available balance here
			// because it depends on the coin selection algorithm.
			Assert.Equal(Money.Parse("4.5"), MyWallet.Pools.Verified.Balance);

			// Now confirm the transaction by including it into a block.
			var b3 = TestUtils.CreateFakeBlock(spend);
			MyWallet.ReceiveBlock(b3, BlockType.Main);

			// Change is confirmed. We started with 5.50 so we should have 4.50 left.
			var v4 = Money.Parse("4.50");
			Assert.Equal(v4, MyWallet.Balance);
		}

		[Fact]
		public void testSpendToSameWallet()
		{
			// Test that a spend to the same wallet is dealt with correctly
			// It should appear in the wallet and confirm 
			// This is a bit of a silly thing to do in the real world as all it does is burn a fee but it is perfectly valid

			var coin1 = Money.Parse("1.0");
			var coinHalf = Money.Parse("0.50");

			// Start by giving us 1 coin.
			Transaction inbound1 = TestUtils.CreateFakeTx(coin1, MyAddress);
			MyWallet.ReceiveBlock(TestUtils.CreateFakeBlock(inbound1), BlockType.Main);

			// Send half to ourselves. We should then have a balance available to spend of zero
			Transaction outbound1 = MyWallet.CreateSend(MyAddress, coinHalf);
			MyWallet.ReceiveTransaction(outbound1);
			Assert.True(MyWallet.SignedByMe(outbound1));

			// we should have a zero available balance before the next block
			Assert.Equal(Money.Zero, MyWallet.Balance);

			MyWallet.ReceiveBlock(TestUtils.CreateFakeBlock(outbound1), BlockType.Main);

			// we should have a balance of 1 BTC after the block is received
			Assert.Equal(coin1, MyWallet.Balance);
		}

		[Fact]
		public void TestFork()
		{
		}
	}
}