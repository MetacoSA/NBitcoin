#if !NOSOCKET
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using NBitcoin.SPV;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace NBitcoin.Tests
{
	public class BlockchainBuilder
	{
		public Network Network
		{
			get; set;
		}
		public BlockchainBuilder()
		{
			Network = Network.RegTest;
			Chain = new ConcurrentChain(Network);
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
			return BroadcastTransaction(tx);
		}

		public Transaction BroadcastTransaction(Transaction tx)
		{
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
			b.Header.Bits = Chain.Tip.GetNextWorkRequired(Network);
			if(RealPoW)
			{
				while(true)
				{
					b.Header.Nonce = RandomUtils.GetUInt32();
					var header = new ChainedBlock(b.Header, b.Header.GetHash(), Chain.GetBlock(b.Header.HashPrevBlock));
					if(header.Validate(Network))
						break;
				}
			}

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
		public bool RealPoW
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
			lock(Nodes)
			{
				Nodes.Add(AttachedNode);
			}
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
			var filteradd = message.Message.Payload as FilterAddPayload;
			if(filteradd != null)
			{
				_Filter.Insert(filteradd.Data);
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
					var found = FindTransaction(inv.Hash);
					if(inv.Type == InventoryType.MSG_TX && found != null)
						AttachedNode.SendMessageAsync(new TxPayload(found));
				}
			}
			var mempool = message.Message.Payload as MempoolPayload;
			if(mempool != null)
			{
				foreach(var tx in _Builder.Mempool)
				{
					BroadcastCore(tx.Value);
				}
			}

			var invs = message.Message.Payload as InvPayload;
			if(invs != null)
			{
				node.SendMessageAsync(new GetDataPayload(invs.ToArray()));
			}

			var txPayload = message.Message.Payload as TxPayload;
			if(txPayload != null)
			{
				if(!_ReceivedTransactions.TryAdd(txPayload.Object.GetHash(), txPayload.Object))
				{
					node.SendMessageAsync(new RejectPayload()
					{
						Hash = txPayload.Object.GetHash(),
						Code = RejectCode.DUPLICATE,
						Message = "tx"
					});
				}
				else
				{
					foreach(var other in Nodes.Where(n => n != node))
					{
						other.SendMessageAsync(new InvPayload(txPayload.Object));
					}
				}
			}
		}

		internal List<Node> Nodes = new List<Node>();
		internal ConcurrentDictionary<uint256, Transaction> _ReceivedTransactions = new ConcurrentDictionary<uint256, Transaction>();

		public BloomFilter _Filter;
		ConcurrentDictionary<uint256, Block> _Blocks = new ConcurrentDictionary<uint256, Block>();
		ConcurrentDictionary<uint256, Transaction> _Transactions = new ConcurrentDictionary<uint256, Transaction>();
		ConcurrentDictionary<uint256, uint256> _Known = new ConcurrentDictionary<uint256, uint256>();
		void _Builder_NewTransaction(Transaction obj)
		{
			_Transactions.AddOrReplace(obj.GetHash(), obj);
			BroadcastCore(obj);
		}

		private void BroadcastCore(Transaction obj)
		{
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
			behavior._ReceivedTransactions = _ReceivedTransactions;
			behavior.Nodes = Nodes;
			return behavior;
		}

		Transaction FindTransaction(uint256 id)
		{
			return _Builder.Mempool.TryGet(id) ?? _Transactions.TryGet(id) ?? _ReceivedTransactions.TryGet(id);
		}

		#endregion
	}

	public class spv_tests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSyncWallet()
		{
			using(var builder = NodeBuilder.Create())
			{
				var main = builder.CreateNode();
				var walletNode = builder.CreateNode();
				builder.StartAll();
				walletNode.CreateRPCClient().AddNode(main.Endpoint, true);
				main.Generate(101);
				CanSyncWalletCore(builder, walletNode, new WalletCreation()
				{
					Network = Network.RegTest,
					RootKeys = new[] { new ExtKey().Neuter() },
					UseP2SH = false
				});
				CanSyncWalletCore(builder, walletNode, new WalletCreation()
				{
					Network = Network.RegTest,
					RootKeys = new[] { new ExtKey().Neuter() },
					UseP2SH = true
				});

				//CanSyncWalletCore(builder, walletNode, new WalletCreation()
				//{
				//    Network = Network.RegTest,
				//    RootKeys = new[] { new ExtKey().Neuter(), new ExtKey().Neuter() },
				//    SignatureRequired = 2,
				//    UseP2SH = false
				//});

				CanSyncWalletCore(builder, walletNode, new WalletCreation()
				{
					Network = Network.RegTest,
					RootKeys = new[] { new ExtKey().Neuter(), new ExtKey().Neuter() },
					SignatureRequired = 2,
					UseP2SH = true
				});
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSyncWallet2()
		{
			using(var builder = NodeBuilder.Create())
			{
				NodesGroup aliceConnection = CreateGroup(builder, 1);
				NodesGroup bobConnection = CreateGroup(builder, new[] { builder.Nodes[0] }, 1);
				builder.Nodes[0].Generate(101);
				var rpc = builder.Nodes[0].CreateRPCClient();
				var aliceKey = new ExtKey();
				Wallet alice = new Wallet(new WalletCreation()
				{
					Network = Network.RegTest,
					RootKeys = new[] { aliceKey.Neuter() },
					SignatureRequired = 1,
					UseP2SH = false
				}, 11);
				Wallet bob = new Wallet(new WalletCreation()
				{
					Network = Network.RegTest,
					RootKeys = new[] { new ExtKey().Neuter() },
					SignatureRequired = 1,
					UseP2SH = false
				}, 11);

				alice.Configure(aliceConnection);
				alice.Connect();

				bob.Configure(bobConnection);
				bob.Connect();

				TestUtils.Eventually(() => aliceConnection.ConnectedNodes.Count == 1);

				//New address tracked
				var addressAlice = alice.GetNextScriptPubKey();
				builder.Nodes[0].GiveMoney(addressAlice, Money.Coins(1.0m));
				TestUtils.Eventually(() => aliceConnection.ConnectedNodes.Count == 1); //Reconnect
																					   //////

				TestUtils.Eventually(() => alice.GetTransactions().Count == 1);

				//Alice send tx to bob
				var coins = alice.GetTransactions().GetSpendableCoins();
				var keys = coins.Select(c => alice.GetKeyPath(c.ScriptPubKey))
								.Select(k => aliceKey.Derive(k))
								.ToArray();
				var txBuilder = new TransactionBuilder();
				var tx =
					txBuilder
					.SetTransactionPolicy(new Policy.StandardTransactionPolicy()
					{
						MinRelayTxFee = new FeeRate(0)
					})
					.AddCoins(coins)
					.AddKeys(keys)
					.Send(bob.GetNextScriptPubKey(), Money.Coins(0.4m))
					.SetChange(alice.GetNextScriptPubKey(true))
					.SendFees(Money.Coins(0.0001m))
					.BuildTransaction(true);

				Assert.True(txBuilder.Verify(tx));

				builder.Nodes[0].Broadcast(tx);

				//Alice get change
				TestUtils.Eventually(() => alice.GetTransactions().Count == 2);
				coins = alice.GetTransactions().GetSpendableCoins();
				Assert.True(coins.Single().Amount == Money.Coins(0.5999m));
				//////

				//Bob get coins
				TestUtils.Eventually(() => bob.GetTransactions().Count == 1);
				coins = bob.GetTransactions().GetSpendableCoins();
				Assert.True(coins.Single().Amount == Money.Coins(0.4m));
				//////


				MemoryStream bobWalletBackup = new MemoryStream();
				bob.Save(bobWalletBackup);
				bobWalletBackup.Position = 0;

				MemoryStream bobTrakerBackup = new MemoryStream();
				bob.Tracker.Save(bobTrakerBackup);
				bobTrakerBackup.Position = 0;

				bob.Disconnect();

				//Restore bob
				bob = Wallet.Load(bobWalletBackup);
				bobConnection.NodeConnectionParameters.TemplateBehaviors.Remove<TrackerBehavior>();
				bobConnection.NodeConnectionParameters.TemplateBehaviors.Add(new TrackerBehavior(Tracker.Load(bobTrakerBackup), alice.Chain));
				/////

				bob.Configure(bobConnection);

				//Bob still has coins
				TestUtils.Eventually(() => bob.GetTransactions().Count == 1);
				coins = bob.GetTransactions().GetSpendableCoins();
				Assert.True(coins.Single().Amount == Money.Coins(0.4m));
				//////

				bob.Connect();
				TestUtils.Eventually(() => bobConnection.ConnectedNodes.Count == 1);

				//New block found !
				builder.Nodes[0].SelectMempoolTransactions();
				builder.Nodes[0].Generate(1);

				//Alice send tx to bob
				coins = alice.GetTransactions().GetSpendableCoins();
				keys = coins.Select(c => alice.GetKeyPath(c.ScriptPubKey))
								.Select(k => aliceKey.Derive(k))
								.ToArray();
				txBuilder = new TransactionBuilder();
				tx =
					txBuilder
					.SetTransactionPolicy(new Policy.StandardTransactionPolicy()
					{
						MinRelayTxFee = new FeeRate(0)
					})
					.AddCoins(coins)
					.AddKeys(keys)
					.Send(bob.GetNextScriptPubKey(), Money.Coins(0.1m))
					.SetChange(alice.GetNextScriptPubKey(true))
					.SendFees(Money.Coins(0.0001m))
					.BuildTransaction(true);

				Assert.True(txBuilder.Verify(tx));

				builder.Nodes[0].Broadcast(tx);

				//Bob still has coins
				TestUtils.Eventually(() => bob.GetTransactions().Count == 2); //Bob has both, old and new tx
				coins = bob.GetTransactions().GetSpendableCoins();
				//////
			}
		}

		public void CanSyncWalletCore(NodeBuilder builder, CoreNode walletNode, WalletCreation creation)
		{
			var rpc = builder.Nodes[0].CreateRPCClient();
			var notifiedTransactions = new List<WalletTransaction>();
			NodesGroup connected = CreateGroup(builder, new List<CoreNode>(new[] { walletNode }), 1);
			Wallet wallet = new Wallet(creation, keyPoolSize: 11);
			wallet.NewWalletTransaction += (s, a) => notifiedTransactions.Add(a);
			Assert.True(wallet.State == WalletState.Created);
			wallet.Configure(connected);
			wallet.Connect();
			Assert.True(wallet.State == WalletState.Disconnected);
			TestUtils.Eventually(() => connected.ConnectedNodes.Count == 1);
			Assert.True(wallet.State == WalletState.Connected);

			TestUtils.Eventually(() => wallet.Chain.Height == rpc.GetBlockCount());
			for(int i = 0; i < 9; i++)
			{
				wallet.GetNextScriptPubKey();
			}
			wallet.GetNextScriptPubKey(); //Should provoke purge
			TestUtils.Eventually(() => wallet.State == WalletState.Disconnected && wallet.ConnectedNodes == 0);
			Thread.Sleep(100);
			TestUtils.Eventually(() => wallet.ConnectedNodes == 1);

			var k = wallet.GetNextScriptPubKey();
			Assert.NotNull(wallet.GetKeyPath(k));
			if(creation.UseP2SH)
			{
				var p2sh = k.GetDestinationAddress(Network.TestNet) as BitcoinScriptAddress;
				Assert.NotNull(p2sh);
				var redeem = wallet.GetRedeemScript(p2sh);
				Assert.NotNull(redeem);
				Assert.Equal(redeem.Hash, p2sh.Hash);
			}

			Assert.Equal(creation.UseP2SH, k.GetDestinationAddress(Network.TestNet) is BitcoinScriptAddress);
			builder.Nodes[0].GiveMoney(k, Money.Coins(1.0m));
			TestUtils.Eventually(() => wallet.GetTransactions().Count == 1
									&& notifiedTransactions.Count == 1);
			builder.Nodes[0].FindBlock();
			TestUtils.Eventually(() => wallet.GetTransactions().Where(t => t.BlockInformation != null).Count() == 1 &&
									   notifiedTransactions.Count == 2);
			builder.Nodes[0].GiveMoney(k, Money.Coins(1.5m), false);
			builder.Nodes[0].FindBlock();
			TestUtils.Eventually(() => wallet.GetTransactions().Summary.Confirmed.TransactionCount == 2 &&
									   notifiedTransactions.Count == 3);

			builder.Nodes[0].FindBlock(30);
			Assert.True(wallet.GetTransactions().Summary.Confirmed.TransactionCount == 2);
			builder.Nodes[0].GiveMoney(k, Money.Coins(0.001m), false);
			Assert.True(wallet.GetTransactions().Summary.Confirmed.TransactionCount == 2);
			builder.Nodes[0].FindBlock(1, false);
			Assert.True(wallet.GetTransactions().Summary.Confirmed.TransactionCount == 2);
			builder.Nodes[0].FindBlock();
			//Sync automatically
			TestUtils.Eventually(() => wallet.GetTransactions().Summary.Confirmed.TransactionCount == 3);

			//Save and restore wallet
			MemoryStream ms = new MemoryStream();
			wallet.Save(ms);
			ms.Position = 0;
			var wallet2 = Wallet.Load(ms);
			//////

			//Save and restore tracker
			ms = new MemoryStream();
			var tracker = connected.NodeConnectionParameters.TemplateBehaviors.Find<TrackerBehavior>();
			tracker.Tracker.Save(ms);
			ms.Position = 0;
			connected = CreateGroup(builder, new List<CoreNode>(new[] { walletNode }), 1);
			tracker = new TrackerBehavior(Tracker.Load(ms), wallet.Chain);
			connected.NodeConnectionParameters.TemplateBehaviors.Add(tracker);
			//////

			wallet2.Configure(connected);
			wallet2.Connect();
			Assert.Equal(wallet.Created, wallet2.Created);
			Assert.Equal(wallet.GetNextScriptPubKey(), wallet2.GetNextScriptPubKey());
			Assert.True(wallet.GetKnownScripts().Length == wallet2.GetKnownScripts().Length);
			TestUtils.Eventually(() => wallet2.GetTransactions().Summary.Confirmed.TransactionCount == 3);

			//TestUtils.Eventually(() =>
			//{
			//    var fork = wallet.Chain.FindFork(wallet2._ScanLocation);
			//    return fork.Height == rpc.GetBlockCount();
			//});

			wallet2.Disconnect();
			wallet.Disconnect();
			connected.Disconnect();
		}

		private static void SetupSPVBehavior(NodeServerTester servers, BlockchainBuilder chainBuilder)
		{
			List<Node> nodes = new List<Node>();
			ConcurrentDictionary<uint256, Transaction> receivedTxs = new ConcurrentDictionary<uint256, Transaction>();

			foreach(var server in new[] { servers.Server1, servers.Server2 })
			{
				server.InboundNodeConnectionParameters.Services = NodeServices.Network | NodeServices.NODE_BLOOM;
				//Simulate SPV compatible server
				server.InboundNodeConnectionParameters.TemplateBehaviors.Add(new ChainBehavior(chainBuilder.Chain)
				{
					AutoSync = false
				});
				server.InboundNodeConnectionParameters.TemplateBehaviors.Add(new SPVBehavior(chainBuilder)
				{
					Nodes = nodes,
					_ReceivedTransactions = receivedTxs
				});
				/////////////
			}
		}

		[Fact]
		//[Trait("UnitTest", "UnitTest")]
		public void CanBroadcastTransaction()
		{
			using(NodeServerTester servers = new NodeServerTester())
			{
				var notifiedTransactions = new List<WalletTransaction>();
				var chainBuilder = new BlockchainBuilder();
				SetupSPVBehavior(servers, chainBuilder);
				var tx = new Transaction();
				Wallet wallet = new Wallet(new WalletCreation()
				{
					Network = servers.Network,
					RootKeys = new[] { new ExtKey().Neuter() },
					UseP2SH = false
				}, keyPoolSize: 11);
				NodesGroup connected = CreateGroup(servers, 2);
				wallet.Configure(connected);
				wallet.Connect();

				AutoResetEvent evt = new AutoResetEvent(false);
				bool passed = false;
				bool rejected = false;
				BroadcastHub hub = BroadcastHub.GetBroadcastHub(wallet.Group.NodeConnectionParameters);
				hub.ManualBroadcast = true;
				var broadcasting = wallet.BroadcastTransactionAsync(tx);
				wallet.TransactionBroadcasted += (t) =>
				{
					passed = true;
					evt.Set();
				};
				wallet.TransactionRejected += (t, r) =>
				{
					rejected = true;
					evt.Set();
				};
				while(connected.ConnectedNodes.Count != 2 && connected.ConnectedNodes.All(n => n.State == NodeState.HandShaked))
				{
					Thread.Sleep(10);
				}

				var behaviors = connected.ConnectedNodes.Select(n => n.Behaviors.Find<BroadcastHubBehavior>()).ToArray();

				TestUtils.Eventually(() => behaviors.All(b => b.Broadcasts.Count() == 1));
				Assert.Equal(1, hub.BroadcastingTransactions.Count());
				hub.BroadcastTransactions();
				Assert.True(evt.WaitOne(20000));
				Assert.True(broadcasting.Wait(2000));
				Assert.True(passed);
				evt.Reset();


				TestUtils.Eventually(() => behaviors.All(b => b.Broadcasts.Count() == 0));
				Assert.Equal(0, hub.BroadcastingTransactions.Count());
				Assert.Null(broadcasting.Result);

				broadcasting = wallet.BroadcastTransactionAsync(tx);

				TestUtils.Eventually(() => behaviors.All(b => b.Broadcasts.Count() == 1));
				hub.BroadcastTransactions();
				Assert.True(evt.WaitOne(20000));
				Assert.True(broadcasting.Wait(2000));
				Assert.True(rejected);
				TestUtils.Eventually(() => behaviors.All(b => b.Broadcasts.Count() == 0));
				Assert.Equal(0, hub.BroadcastingTransactions.Count());
				Assert.NotNull(broadcasting.Result);
			}
		}



		//[Fact]
		//public void Play()
		//{
		//	var key = new BitcoinSecret("L43ZZbKi25Ad1FWRkA96Kzdt2AD8BjkNXeEURBy9T7UBsBZwMXF4");
		//	//var dest = BitcoinAddress.Create("38CqDGzotfeaPUmRdmULV3XyJuetMNzuEZ");
		//	var dest = new Key().GetBitcoinSecret(Network.Main).GetAddress().ScriptPubKey;

		//	var amount = Money.Coins(0.01122491m);
		//	var fee = Money.Coins(0.0001m);
		//	TransactionBuilder builder = new TransactionBuilder();
		//	var c = new Coin(OutPoint.Parse("c7781b132fbb46cad6e9f2d2b8f5470d6455450a539808d49b82568ccc89adc7-1"), new TxOut()
		//	{
		//		ScriptPubKey = key.GetAddress().ScriptPubKey,
		//		Value = amount
		//	});
		//	builder.AddCoins(c);
		//	builder.Send(dest, amount - fee);
		//	builder.SendFees(fee);
		//	builder.AddKeys(key);

		//	var tx = builder.BuildTransaction(true);
		//	Assert.True(builder.Verify(tx));
		//	var node = Node.Connect(Network.Main, "72.223.114.239", new NodeConnectionParameters()
		//	{
		//		TemplateBehaviors =
		//		{
		//			new BroadcastTransactionBehavior()
		//		}
		//	});
		//	var broadcast = node.Behaviors.Find<BroadcastTransactionBehavior>();
		//	node.VersionHandshake();
		//	var reject = broadcast.BroadcastTransactionAsync(tx).Result;
		//	Thread.Sleep(10000);
		//}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void UseFilterAddIfKeyPoolSizeIsZero()
		{
			using(NodeServerTester servers = new NodeServerTester())
			{
				var chainBuilder = new BlockchainBuilder();
				chainBuilder.RealPoW = true;
				SetupSPVBehavior(servers, chainBuilder);

				var connected = CreateGroup(servers, 1);

				Wallet wallet = new Wallet(new WalletCreation()
				{
					Network = servers.Network,
					RootKeys = new[] { new ExtKey().Neuter() },
					UseP2SH = true
				}, keyPoolSize: 0);
				Assert.True(wallet.State == WalletState.Created);
				wallet.Configure(connected);
				wallet.Connect();
				Assert.True(wallet.State == WalletState.Disconnected);
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 1);
				Assert.True(wallet.State == WalletState.Connected);

				var script = wallet.GetNextScriptPubKey(new KeyPath("0/1/2"));
				Thread.Sleep(1000);
				chainBuilder.GiveMoney(script, Money.Coins(0.001m));
				TestUtils.Eventually(() => wallet.GetTransactions().Count == 1);
				wallet.Disconnect();
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 0);

				MemoryStream ms = new MemoryStream();
				wallet.Save(ms);
				ms.Position = 0;
				var wallet2 = Wallet.Load(ms);
				wallet2.Configure(connected);
				wallet2.Connect();

				var script2 = wallet2.GetNextScriptPubKey(new KeyPath("0/1/2"));
				Thread.Sleep(1000);
				Assert.NotEqual(script, script2);
				Assert.NotNull(wallet2.GetRedeemScript(script));
				Assert.NotNull(wallet2.GetRedeemScript(script2));
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 1);
				var spv = servers.Server1.ConnectedNodes.First().Behaviors.Find<SPVBehavior>();
				TestUtils.Eventually(() => spv._Filter != null);
				chainBuilder.GiveMoney(script2, Money.Coins(0.001m));
				TestUtils.Eventually(() => wallet.GetTransactions().Count == 2);
				chainBuilder.GiveMoney(script, Money.Coins(0.002m));
				TestUtils.Eventually(() => wallet.GetTransactions().Count == 3);
				chainBuilder.FindBlock();
				TestUtils.Eventually(() => wallet.GetTransactions().Count == 3 && wallet.GetTransactions().All(t => t.BlockInformation != null));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanAddScriptsToWallet()
		{
			Wallet wallet = new Wallet(new WalletCreation()
			{
				DerivationPath = new KeyPath("56"),
				Name = "MyWallet",
				RootKeys = new[] { new ExtKey().Neuter() },
				SignatureRequired = 1,
				UseP2SH = false
			}, 11);

			wallet.Configure();
			Assert.True(wallet.GetKnownScripts(true).Length == 0);
			Assert.True(wallet.GetKnownScripts(false).Length == 0);
			wallet.GetNextScriptPubKey();
			Assert.True(wallet.GetKnownScripts(false).Length == 11); //11 normal
			Assert.True(wallet.GetKnownScripts(true).Length == 1);
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

			//Test roundtrip serialization
			var filterBefore = tracker.CreateBloomFilter(0.005);
			MemoryStream ms = new MemoryStream();
			tracker.Save(ms);
			tracker = new Tracker();
			ms.Position = 0;
			tracker = Tracker.Load(ms);
			transactions = tracker.GetWalletTransactions(builder.Chain);
			Assert.True(transactions.Count == 3);
			Assert.True(transactions.Summary.UnConfirmed.TransactionCount == 1);
			var filterAfter = tracker.CreateBloomFilter(0.005);
			Assert.True(filterBefore.ToBytes().SequenceEqual(filterAfter.ToBytes()));
			/////
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

			using(var builder = NodeBuilder.Create())
			{
				NodesGroup connected = CreateGroup(builder, 2);
				connected.Connect();
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 2);

				//Server crash abruptly
				builder.Nodes[0].Kill(false);
				TestUtils.Eventually(() => connected.ConnectedNodes.Count < 2);
				builder.Nodes[0].Start();
				//Reconnect ?
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 2);

				//Client crash abruptly
				connected.ConnectedNodes.First().Disconnect();
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 1);

				//Reconnect ?
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 2);
				connected.Disconnect();
				TestUtils.Eventually(() => connected.ConnectedNodes.Count == 0);
			}

		}

		private NodesGroup CreateGroup(NodeBuilder builder, int connections)
		{
			List<CoreNode> nodes = new List<CoreNode>();
			for(int i = 0; i < connections; i++)
				nodes.Add(builder.CreateNode());
			builder.StartAll();
			return CreateGroup(builder, nodes, connections);
		}

		private static NodesGroup CreateGroup(NodeBuilder builder, IEnumerable<CoreNode> nodes, int connections)
		{
			AddressManagerBehavior behavior = new AddressManagerBehavior(new AddressManager());
			foreach(var node in nodes)
			{
				behavior.AddressManager.Add(new NetworkAddress(node.Endpoint), IPAddress.Parse("127.0.0.1"));
			}
			NodeConnectionParameters parameters = new NodeConnectionParameters();
			parameters.TemplateBehaviors.Add(behavior);
			Wallet.ConfigureDefaultNodeConnectionParameters(parameters);
			NodesGroup connected = new NodesGroup(Network.RegTest, parameters);
			connected.AllowSameGroup = true;
			connected.MaximumNodeConnection = connections;
			builder.AddDisposable(connected);
			return connected;
		}

		private static NodesGroup CreateGroup(NodeServerTester servers, int connections)
		{
			AddressManagerBehavior behavior = new AddressManagerBehavior(new AddressManager());
			if(connections == 1)
			{
				behavior.AddressManager.Add(new NetworkAddress(servers.Server1.ExternalEndpoint), IPAddress.Parse("127.0.0.1"));
			}
			if(connections > 1)
			{
				behavior.AddressManager.Add(new NetworkAddress(servers.Server2.ExternalEndpoint), IPAddress.Parse("127.0.0.1"));
			}
			NodeConnectionParameters parameters = new NodeConnectionParameters();
			parameters.TemplateBehaviors.Add(behavior);
			Wallet.ConfigureDefaultNodeConnectionParameters(parameters);
			NodesGroup connected = new NodesGroup(servers.Network, parameters);
			connected.AllowSameGroup = true;
			connected.MaximumNodeConnection = connections;
			servers.AddDisposable(connected);
			return connected;
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

			for(int i = 0; i < 9; i++)
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

			for(int i = 0; i < 10; i++)
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
			tracker.NotifyTransaction(tx2); //Notifying tx2 should have no effect, since it already is accounted because it was orphaned
			_();
		}
	}
}
#endif