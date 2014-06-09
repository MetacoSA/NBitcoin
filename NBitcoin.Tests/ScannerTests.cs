using NBitcoin.Scanning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class ScannerTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanScanPubKeyHash()
		{
			var alice = new Key().PubKey.ID;
			var bob = new Key().PubKey.ID;
			Transaction tx = new Transaction();
			tx.Outputs.Add(new TxOut(Money.Parse("1.5"), alice));
			tx.Outputs.Add(new TxOut(Money.Parse("2.0"), bob));
			PubKeyHashScanner scanner = new PubKeyHashScanner(alice);
			var coins = scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("1.5"), coins.Value);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanScanScriptHash()
		{
			var alice = new Script(OpcodeType.OP_RETURN).ID;
			var bob = new Key().PubKey.ID;
			Transaction tx = new Transaction();
			tx.Outputs.Add(new TxOut(Money.Parse("1.5"), alice));
			tx.Outputs.Add(new TxOut(Money.Parse("2.0"), bob));
			var scanner = new ScriptHashScanner(alice);
			var coins = scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("1.5"), coins.Value);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanScanStealthPayment()
		{
			var alice = CreateStealthUser();
			var bob = CreateStealthUser();

			Transaction tx = new Transaction();
			alice.Address.CreatePayment().AddToTransaction(tx, "1.5");

			var coins = alice.Scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("1.5"), coins.Value);

			coins = bob.Scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("0"), coins.Value);

			alice.Prefix = new BitField(1, 3);
			alice.UpdateAddress();

			tx = new Transaction();
			alice.Address.CreatePayment().AddToTransaction(tx, "1.5");
			coins = alice.Scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("1.5"), coins.Value);

			alice.Prefix = new BitField(2, 3);
			alice.UpdateAddress();

			coins = alice.Scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("0"), coins.Value);
		}

		private StealthTestUser CreateStealthUser()
		{
			return new StealthTestUser();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanScanBlocks()
		{
			using(var tester = CreateTester())
			{
				var alice = tester.CreateScannerUser(new Key().PubKey.ID, 1);
				var bob = tester.CreateScannerUser(new Key().PubKey.ID, 1);

				Chain main = new Chain(Network.TestNet);
				tester.GiveMoney("1.5", alice, main);

				//Assert scanner get the received money
				alice.Process(main);
				alice.AssertMoney("1.5");
				alice.Process(main);

				//Chain did not changed, so no change detected
				alice.AssertMoney("1.5");

				alice.GiveMoney("1.5", bob, main);
				alice.Process(main);

				//No money left to alice
				alice.AssertMoney("0");

				//Money transfered to bob
				bob.Process(main);
				bob.AssertMoney("1.5");

				bob.GiveMoney("1.0", alice, main);

				bob.Process(main);
				alice.Process(main);

				int bobToAliceHeight = main.Height;

				bob.AssertMoney("0.5");
				alice.AssertMoney("1.0");

				alice.ReloadScanner();

				alice.AssertMoney("1.0");
				alice.Process(main);
				alice.AssertMoney("1.0");

				var lateScanner = tester.CreateScannerUser(bob.Id, bobToAliceHeight);
				lateScanner.Process(main);
				lateScanner.AssertMoney("0.5");
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//In this test, alice makes a double spend. In fork, she give money to satoshi, in main to bob.
		//The code checks if the ScanState updates correctly alice's account going back and forth between main and fork.
		public void ScannerCanHandleForks()
		{
			using(var tester = CreateTester())
			{
				var alice = tester.CreateScannerUser(new Key().PubKey.ID, 0);
				var bob = tester.CreateScannerUser(new Key().PubKey.ID, 0);
				var satoshi = tester.CreateScannerUser(new Key().PubKey.ID, 0);

				var aliceFork = tester.CreateScannerUser(alice.Id, 0);

				Chain main = new Chain(Network.TestNet);
				Chain fork = new Chain(Network.TestNet);
				tester.GiveMoney("1.5", alice, main, fork);
				alice.Process(main);
				aliceFork.Process(main);


				aliceFork.GiveMoney("0.9", satoshi, fork);
				aliceFork.Process(fork);
				aliceFork.AssertMoney("0.6");

				//The block will appear in the fork, but not yet in aliceFork, the aliceFork.Process is done later.
				aliceFork.GiveMoney("0.2", satoshi, fork);

				//Meanwhile, double spend of alice to bob
				alice.GiveMoney("1.0", bob, main);

				//aliceFork scanner go back to main, previous 0.9 transaction is canceled
				aliceFork.Process(main);
				aliceFork.AssertMoney("0.5");

				//Now back to fork
				aliceFork.Process(fork);
				//The result should be the 0.6 from last time minus the previous 0.2 the scanner should just have processed
				aliceFork.AssertMoney("0.4");

				//Now back to main
				aliceFork.Process(main);
				//Nothing changed on main
				aliceFork.AssertMoney("0.5");

				//Sanity check of serialization of scanstate
				aliceFork.ReloadScanner();
				aliceFork.AssertMoney("0.5");
			}
		}

		private ScannerTester CreateTester([CallerMemberName]string folderName = null)
		{
			return new ScannerTester(folderName);
		}



	}

	public class ScannerTester : IDisposable
	{
		public ScannerTester(string folderName)
		{
			TestUtils.EnsureNew(folderName);
			_FolderName = folderName;
			string index = Path.Combine(folderName, "Index.dat");
			_Index = new IndexedBlockStore(new SQLiteNoSqlRepository(index, true), new BlockStore(folderName, Network.TestNet));
		}
		internal string _FolderName;
		private readonly IndexedBlockStore _Index;
		public IndexedBlockStore Index
		{
			get
			{
				return _Index;
			}
		}
		public void GiveMoney(string amount, ScannerUser to, params Chain[] chains)
		{
			var tx = TestUtils.CreateFakeTx(amount, to.Id);
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
		}

		public void GiveMoney(Money amount, WalletPool account, KeyId returnAddress, KeyId to, params Chain[] chains)
		{
			var entries = account.GetEntriesToCover(amount);
			var tx = new Transaction();
			foreach(var entry in entries)
			{
				tx.Inputs.Add(new TxIn(entry.OutPoint));
			}
			var refund = entries.Select(e => e.TxOut.Value).Sum() - amount;
			if(refund < 0)
				throw new InvalidOperationException("Not enough money in account");
			if(refund > 0)
			{
				tx.Outputs.Add(new TxOut(refund, returnAddress));
			}
			tx.Outputs.Add(new TxOut(amount, to));

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
		}

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion


		internal int scanner = 0;

		public ScannerUser CreateScannerUser(KeyId keyId, int start)
		{
			scanner++;
			ScannerUser user = new ScannerUser(keyId, start, this);
			return user;
		}
	}

	public class ScannerUser
	{
		private readonly KeyId _Id;
		public KeyId Id
		{
			get
			{
				return _Id;
			}
		}
		public ScannerUser(KeyId keyId, int start, ScannerTester tester)
		{
			var folder = Path.Combine(tester._FolderName, tester.scanner.ToString());
			TestUtils.EnsureNew(folder);

			var chainStream = new StreamObjectStream<ChainChange>(File.Open(Path.Combine(folder, "Chain"), FileMode.OpenOrCreate));
			var accountStream = new StreamObjectStream<AccountEntry>(File.Open(Path.Combine(folder, "Entries"), FileMode.OpenOrCreate));
			_Id = keyId;
			_Scanner = new PubKeyHashScanner(keyId);
			_ScanState = new ScanState(new PubKeyHashScanner(keyId),
							new Chain(chainStream),
							new WalletPool(accountStream),
							start);
			_Tester = tester;
		}

		private readonly PubKeyHashScanner _Scanner;
		public PubKeyHashScanner Scanner
		{
			get
			{
				return _Scanner;
			}
		}

		private ScanState _ScanState;
		private ScannerTester _Tester;
		public ScanState ScanState
		{
			get
			{
				return _ScanState;
			}
		}

		public void Process(Chain chain)
		{
			ScanState.Process(chain, _Tester.Index);
		}

		public void AssertMoney(Money amount)
		{
			Assert.Equal(amount, ScanState.Account.Balance);
		}

		public void GiveMoney(Money amount, ScannerUser to, params Chain[] chains)
		{
			_Tester.GiveMoney(amount, ScanState.Account, Id, to.Id, chains);
		}

		internal void ReloadScanner()
		{
			var old = _ScanState;
			_ScanState = new ScanState(_ScanState.Scanner, _ScanState.Chain.Clone(),
				_ScanState.Account.Clone(), ScanState.StartHeight);
			old.Dispose();

		}
	}

	class StealthTestUser
	{
		public StealthTestUser()
		{
			Scan = new Key();
			Spend = new Key();
			Prefix = new BitField(0, 0);
			UpdateAddress();
		}

		public void UpdateAddress()
		{
			Address = new BitcoinStealthAddress(Scan.PubKey, new PubKey[] { Spend.PubKey }, 1, Prefix, Network.TestNet);
			Scanner = new StealthPaymentScanner(Address, Scan);
		}
		public Scanner Scanner
		{
			get;
			set;
		}
		public Key Scan
		{
			get;
			set;
		}
		public Key Spend
		{
			get;
			set;
		}
		public BitField Prefix
		{
			get;
			set;
		}
		public BitcoinStealthAddress Address
		{
			get;
			set;
		}
	}
}
