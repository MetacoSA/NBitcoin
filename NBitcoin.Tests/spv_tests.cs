#if !NOSOCKET
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.SPV;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
			Broadcast = true;
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
			OnNewTransaction(tx);
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
			OnNewTransaction(tx);
			return tx;
		}

		private void OnNewTransaction(Transaction tx)
		{
			if(NewTransaction != null)
				NewTransaction(tx);
		}

		public Block FindBlock()
		{
			Block b = new Block();
			b.Transactions.Add(new Transaction()
			{
				Inputs =
				{
					new TxIn(new Script(RandomUtils.GetBytes(32)))
				}
			});
			foreach(var tx in Mempool)
				b.Transactions.Add(tx.Value);
			b.Header.BlockTime = DateTimeOffset.UtcNow;
			b.UpdateMerkleRoot();
			b.Header.HashPrevBlock = Chain.Tip.HashBlock;
			Chain.SetTip(b.Header);
			Mempool.Clear();
			if(NewBlock != null)
				NewBlock(b);
			return b;
		}

		public event Action<Transaction> NewTransaction;
		public event Action<Block> NewBlock;

		/// <summary>
		/// The true the remote server will not broadcast new tx and blocks
		/// </summary>
		public bool Broadcast
		{
			get;
			set;
		}
	}

	public class SPVBehavior : NodeBehavior
	{
		BlockchainBuilder _Builder;
		public SPVBehavior(BlockchainBuilder builder)
		{
			_Builder = builder;
		}
		protected override void AttachCore()
		{
			_Builder.NewBlock += _Builder_NewBlock;
			_Builder.NewTransaction += _Builder_NewTransaction;
			AttachedNode.MessageReceived += AttachedNode_MessageReceived;
		}

		void AttachedNode_MessageReceived(Node node, IncomingMessage message)
		{
			var filterload = message.Message.Payload as FilterLoadPayload;
			if(filterload != null)
			{
				_Filter = filterload.Object;
			}
			var getdata = message.Message.Payload as GetDataPayload;
			if(getdata != null)
			{
				foreach(var inv in getdata.Inventory)
				{
					if(inv.Type == InventoryType.MSG_FILTERED_BLOCK && _Filter != null)
					{
						var merkle = new MerkleBlock(_Blocks[inv.Hash], _Filter);
						AttachedNode.SendMessageAsync(new MerkleBlockPayload(merkle));
						foreach(var tx in merkle.PartialMerkleTree.GetMatchedTransactions())
						{
							if(_Known.TryAdd(tx, tx))
							{
								AttachedNode.SendMessageAsync(new InvPayload(InventoryType.MSG_TX, tx));
							}
						}
					}
					if(inv.Type == InventoryType.MSG_TX)
						AttachedNode.SendMessageAsync(new TxPayload(_Transactions[inv.Hash]));
				}
			}
		}
		public BloomFilter _Filter;
		ConcurrentDictionary<uint256, Block> _Blocks = new ConcurrentDictionary<uint256, Block>();
		ConcurrentDictionary<uint256, Transaction> _Transactions = new ConcurrentDictionary<uint256, Transaction>();
		ConcurrentDictionary<uint256, uint256> _Known = new ConcurrentDictionary<uint256, uint256>();
		void _Builder_NewTransaction(Transaction obj)
		{
			_Transactions.AddOrReplace(obj.GetHash(), obj);
			if(_Builder.Broadcast)
				if(_Filter != null && _Filter.IsRelevantAndUpdate(obj) && _Known.TryAdd(obj.GetHash(), obj.GetHash()))
				{
					AttachedNode.SendMessageAsync(new InvPayload(obj));
				}
		}

		void _Builder_NewBlock(Block obj)
		{
			_Blocks.AddOrReplace(obj.GetHash(), obj);
			foreach(var tx in obj.Transactions)
				_Transactions.TryAdd(tx.GetHash(), tx);
			if(_Builder.Broadcast)
				AttachedNode.SendMessageAsync(new InvPayload(obj));
		}

		protected override void DetachCore()
		{
			_Builder.NewTransaction -= _Builder_NewTransaction;
			_Builder.NewBlock -= _Builder_NewBlock;
			AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
		}

		#region ICloneable Members

		public override object Clone()
		{
			var behavior = new SPVBehavior(_Builder);
			behavior._Blocks = _Blocks;
			behavior._Transactions = _Transactions;
			return behavior;
		}

		#endregion
	}

	public class spv_tests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSyncWallet()
		{
			using(NodeServerTester servers = new NodeServerTester(Network.TestNet))
			{
				var chainBuilder = new BlockchainBuilder();

				//Simulate SPV compatible server
				servers.Server1.InboundNodeConnectionParameters.Services = NodeServices.Network;
				servers.Server1.InboundNodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(chainBuilder.Chain)
				{
					AutoSync = false
				});
				servers.Server1.InboundNodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(chainBuilder));
				/////////////

				//The SPV client does not verify the chain and keep one connection alive with Server1
				NodeConnectionParameters parameters = new NodeConnectionParameters();
				Wallet.ConfigureDefaultNodeConnectionParameters(parameters);
				parameters.IsTrusted = true;
				AddressManagerBehavior addrman = new AddressManagerBehavior(new AddressManager());
				addrman.AddressManager.Add(new NetworkAddress(servers.Server1.ExternalEndpoint), IPAddress.Parse("127.0.0.1"));
				parameters.TemplateBehaviors.Add(addrman);
				NodesGroup connected = new NodesGroup(Network.TestNet, parameters);
				connected.AllowSameGroup = true;
				connected.MaximumNodeConnection = 1;
				/////////////

				var bob = new ExtKey();
				Wallet wallet = new Wallet(bob, Network.TestNet, keyPoolSize: 11);
				Assert.True(wallet.State == WalletState.Created);
				wallet.Connect(connected);
				Assert.True(wallet.State == WalletState.Disconnected);
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 1);
				Assert.True(wallet.State == WalletState.Connected);

				chainBuilder.FindBlock();
				TestUtils.Eventually(() => wallet.Chain.Height == 1);
				for(int i = 0 ; i < 9 ; i++)
				{
					wallet.NewKey();
				}
				wallet.NewKey(); //Should provoke purge
				TestUtils.Eventually(() => wallet.State == WalletState.Disconnected && wallet.ConnectedNodes == 0);
				TestUtils.Eventually(() => wallet.ConnectedNodes == 1);
				TestUtils.Eventually(() => servers.Server1.ConnectedNodes.Count == 1);
				var spv = servers.Server1.ConnectedNodes.First().Behaviors.Find<SPVBehavior>();
				TestUtils.Eventually(() => spv._Filter != null);

				var k = wallet.NewKey();
				chainBuilder.GiveMoney(k.ExtPubKey.ScriptPubKey, Money.Coins(1.0m));
				TestUtils.Eventually(() => wallet.GetTransactions().Count == 1);
				chainBuilder.FindBlock();
				TestUtils.Eventually(() => wallet.GetTransactions().Where(t => t.BlockInformation != null).Count() == 1);

				chainBuilder.Broadcast = false;
				chainBuilder.GiveMoney(k.ExtPubKey.ScriptPubKey, Money.Coins(1.5m));
				chainBuilder.Broadcast = true;
				chainBuilder.FindBlock();
				TestUtils.Eventually(() => wallet.GetTransactions().Summary.Confirmed.TransactionCount == 2);

				chainBuilder.Broadcast = false;
				for(int i = 0 ; i < 50 ; i++)
				{
					chainBuilder.FindBlock();
				}
				chainBuilder.GiveMoney(k.ExtPubKey.ScriptPubKey, Money.Coins(0.001m));
				chainBuilder.FindBlock();
				chainBuilder.Broadcast = true;
				chainBuilder.FindBlock();
				//Sync automatically
				TestUtils.Eventually(() => wallet.GetTransactions().Summary.Confirmed.TransactionCount == 3);
			}
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
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
				if(!btx.IsCoinBase)
				{
					Assert.True(tracker.NotifyTransaction(btx, builder.Chain.GetBlock(block.GetHash()), block));
					Assert.True(tracker.NotifyTransaction(btx, builder.Chain.GetBlock(block.GetHash()), block)); //Idempotent
				}
			}

			var transactions = tracker.GetWalletTransactions(builder.Chain);

			Assert.True(transactions.Count == 2);
			Assert.True(transactions[0].Transaction.GetHash() == tx2.GetHash());
			Assert.True(transactions[1].Transaction.GetHash() == tx1.GetHash());
			Assert.True(transactions[0].Balance == -Money.Coins(0.6m));

			var tx3 = builder.GiveMoney(bob, Money.Coins(0.01m));
			coin = tx3.Outputs.AsCoins().First();
			block = builder.FindBlock();
			Assert.True(tracker.NotifyTransaction(block.Transactions[1], builder.Chain.GetBlock(block.GetHash()), block));

			transactions = tracker.GetWalletTransactions(builder.Chain);
			Assert.True(transactions.Count == 3);
			Assert.True(transactions.Summary.UnConfirmed.TransactionCount == 0);
			Assert.True(transactions[0].Transaction.GetHash() == block.Transactions[1].GetHash());

			Assert.Equal(2, transactions.GetSpendableCoins().Count()); // the 1 change + 1 gift

			builder.Chain.SetTip(builder.Chain.Tip.Previous);
			transactions = tracker.GetWalletTransactions(builder.Chain);
			Assert.True(transactions.Count == 3);
			Assert.True(transactions.Summary.UnConfirmed.TransactionCount == 1);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanTrackScriptCoins()
		{
			BlockchainBuilder builder = new BlockchainBuilder();
			Tracker tracker = new Tracker();
			Key bob = new Key();
			tracker.Add(bob.PubKey, true);
			var tx1 = builder.GiveMoney(bob.PubKey.ScriptPubKey.Hash, Money.Coins(1.0m));
			Assert.True(tracker.NotifyTransaction(tx1));
			Assert.True(tracker.GetWalletTransactions(builder.Chain)[0].ReceivedCoins[0] is ScriptCoin);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanMaintainConnectionToNodes()
		{
			using(NodeServerTester servers = new NodeServerTester(Network.TestNet))
			{
				servers.Server1.InboundNodeConnectionParameters.Services = NodeServices.Network;
				AddressManagerBehavior behavior = new AddressManagerBehavior(new AddressManager());
				behavior.AddressManager.Add(new NetworkAddress(servers.Server1.ExternalEndpoint), IPAddress.Parse("127.0.0.1"));
				NodeConnectionParameters parameters = new NodeConnectionParameters();
				parameters.TemplateBehaviors.Add(behavior);
				NodesGroup connected = new NodesGroup(Network.TestNet, parameters, new NodeRequirement()
				{
					RequiredServices = NodeServices.Network
				});
				connected.AllowSameGroup = true;
				connected.MaximumNodeConnection = 2;
				connected.Connect();

				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 2);

				//Server crash abruptly
				servers.Server1.ConnectedNodes.First().Disconnect();
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 1);

				//Reconnect ?
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 2);

				//Client crash abruptly
				connected.ConnectedNodes.First().Disconnect();
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 1);

				//Reconnect ?
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 2);
			}
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanPrune()
		{
			BlockchainBuilder builder = new BlockchainBuilder();
			Tracker tracker = new Tracker();
			Key bob = new Key();
			Key alice = new Key();
			tracker.Add(bob);

			var oldUnconf = builder.GiveMoney(bob, Money.Coins(1.0m));
			Assert.True(tracker.NotifyTransaction(oldUnconf));

			builder.Mempool.Clear();

			var oldConf = builder.GiveMoney(bob, Money.Coins(0.9m));
			var oldConfSpent = builder.SpendCoin(oldConf.Outputs.AsCoins().First(), alice, Money.Coins(0.01m));

			var block = builder.FindBlock();

			Assert.True(tracker.NotifyTransaction(oldConf, builder.Chain.Tip, block));
			Assert.True(tracker.NotifyTransaction(oldConfSpent, builder.Chain.Tip, block));

			for(int i = 0 ; i < 9 ; i++)
			{
				builder.FindBlock();
			}
			Assert.True(tracker.Prune(builder.Chain, 10).Count == 0);
			builder.FindBlock();

			//Prune tracked outpoint
			var pruned = tracker.Prune(builder.Chain, 10);
			Assert.Equal(1, pruned.Count);
			Assert.True(pruned.First() is Tracker.TrackedOutpoint);

			//Prune old unconf
			pruned = tracker.Prune(builder.Chain, timeExpiration: TimeSpan.Zero);
			Assert.Equal(1, pruned.Count);
			var op = pruned.OfType<Tracker.Operation>().First();
			Assert.True(op.BlockId == null);

			var conf = builder.GiveMoney(bob, Money.Coins(0.9m));
			block = builder.FindBlock();
			Assert.True(tracker.NotifyTransaction(conf, builder.Chain.Tip, block));

			var oldSpentForked = builder.SpendCoin(conf.Outputs.AsCoins().First(), alice, Money.Coins(0.021m));
			block = builder.FindBlock();
			Assert.True(tracker.NotifyTransaction(oldSpentForked, builder.Chain.Tip, block));

			var forked = builder.Chain.Tip;
			builder.Chain.SetTip(builder.Chain.Tip.Previous);

			for(int i = 0 ; i < 10 ; i++)
			{
				builder.FindBlock();
			}

			pruned = tracker.Prune(builder.Chain, 10);
			Assert.True(pruned.Count == 1); //Tracked outpoint of conf
			Assert.True(pruned.First() is Tracker.TrackedOutpoint);
			block = builder.FindBlock();

			pruned = tracker.Prune(builder.Chain, 10); //Old forked spent
			Assert.Equal(1, pruned.Count);
			op = pruned.OfType<Tracker.Operation>().First();
			Assert.Equal(forked.HashBlock, op.BlockId);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
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
				Assert.True(transactions.Summary.UnConfirmed.TransactionCount == 2);
				Assert.True(transactions.Summary.UnConfirmed.Amount == -Money.Coins(0.3m));
			};
			_();
			tracker.NotifyTransaction(tx2);	//Notifying tx2 should have no effect, since it already is accounted because it was orphaned
			_();
		}
	}
}
#endif