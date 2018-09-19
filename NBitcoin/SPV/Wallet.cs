#if !NOJSONNET
#if !NOSOCKET
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.SPV
{
	[Obsolete]
	public enum WalletState
	{
		Created,
		Disconnected,
		Connected
	}
	[Obsolete]
	public class WalletCreation
	{

		public WalletCreation()
		{
			SignatureRequired = 1;
			RootKeys = new ExtPubKey[0];
			Network = Network.Main;
			DerivationPath = new KeyPath();
			Name = Guid.NewGuid().ToString();
			PurgeConnectionOnFilterChange = true;
		}

		/// <summary>
		/// Create a P2PKH wallet with one key
		/// </summary>
		/// <param name="rootKey">The master key to use</param>
		public WalletCreation(BitcoinExtPubKey rootKey)
		{
			Network = rootKey.Network;
			RootKeys = new[] { rootKey.ExtPubKey };
			SignatureRequired = 1;
			UseP2SH = false;
			Name = Guid.NewGuid().ToString();
		}

		public string Name
		{
			get;
			set;
		}
		public Network Network
		{
			get;
			set;
		}

		public int SignatureRequired
		{
			get;
			set;
		}
		public ExtPubKey[] RootKeys
		{
			get;
			set;
		}
		public KeyPath DerivationPath
		{
			get;
			set;
		}
		public bool UseP2SH
		{
			get;
			set;
		}

		public bool PurgeConnectionOnFilterChange
		{
			get;
			set;
		}

		internal JObject ToJson()
		{
			JObject obj = new JObject();
			if(Name != null)
				obj["Name"] = Name;
			obj["Network"] = Network.ToString();
			obj["SignatureRequired"] = SignatureRequired;
			obj["DerivationPath"] = DerivationPath.ToString();
			obj["UseP2SH"] = UseP2SH;
			obj["PurgeConnectionOnFilterChange"] = PurgeConnectionOnFilterChange;
			obj["RootKeys"] = new JArray(RootKeys.Select(c => c.GetWif(Network).ToString()));
			return obj;
		}

		static internal WalletCreation FromJson(JObject obj)
		{
			WalletCreation creation = new WalletCreation();
			creation.Network = null;
			JToken unused;
			if(obj.TryGetValue("Name", out unused))
				creation.Name = (string)obj["Name"];
			else
				creation.Name = null;
			if(obj.Property("PurgeConnectionOnFilterChange") != null)
			{
				creation.PurgeConnectionOnFilterChange = (bool)obj["PurgeConnectionOnFilterChange"];
			}
			JToken network;
			if(obj.TryGetValue("Network", out network))
				creation.Network = Network.GetNetwork((string)network);
			creation.SignatureRequired = (int)(long)obj["SignatureRequired"];
			creation.DerivationPath = KeyPath.Parse((string)obj["DerivationPath"]);
			creation.UseP2SH = (bool)obj["UseP2SH"];
			var array = (JArray)obj["RootKeys"];
			var keys = array.Select(i => new BitcoinExtPubKey((string)i)).ToArray();
			creation.Network = creation.Network ?? keys[0].Network;
			creation.RootKeys = keys.Select(k => k.ExtPubKey).ToArray();
			return creation;
		}
	}

#pragma warning disable CS0612 // Type or member is obsolete
	public delegate void NewWalletTransactionDelegate(Wallet sender, WalletTransaction walletTransaction);
#pragma warning restore CS0612 // Type or member is obsolete

	/// <summary>
	/// A SPV Wallet respecting recommendation for privacy http://eprint.iacr.org/2014/763.pdf
	/// </summary>
	public class Wallet
	{
		class WalletBehavior : NodeBehavior
		{
			Wallet _Wallet;
			public Wallet Wallet
			{
				get
				{
					return _Wallet;
				}
			}
#pragma warning disable CS0612 // Type or member is obsolete
			TrackerBehavior _Tracker;
#pragma warning restore CS0612 // Type or member is obsolete
			BroadcastHub _Broadcast;
			public WalletBehavior(Wallet wallet)
			{
				_Wallet = wallet;
			}
			protected override void AttachCore()
			{
#pragma warning disable CS0612 // Type or member is obsolete
				_Tracker = AttachedNode.Behaviors.Find<TrackerBehavior>();
#pragma warning restore CS0612 // Type or member is obsolete
				if(_Tracker != null)
				{
					AttachedNode.Disconnected += AttachedNode_Disconnected;
					AttachedNode.StateChanged += AttachedNode_StateChanged;
				}
				_Broadcast = BroadcastHub.GetBroadcastHub(AttachedNode);
				if(_Broadcast != null)
				{
					_Broadcast.TransactionBroadcasted += _Broadcast_TransactionBroadcasted;
					_Broadcast.TransactionRejected += _Broadcast_TransactionRejected;
				}
			}

			void _Broadcast_TransactionRejected(Transaction transaction, RejectPayload reject)
			{
				_Wallet.OnTransactionRejected(transaction, reject);
			}

			void _Broadcast_TransactionBroadcasted(Transaction transaction)
			{
				_Wallet.OnTransactionBroadcasted(transaction);
			}

			void AttachedNode_StateChanged(Node node, NodeState oldState)
			{
				if(node.State == NodeState.HandShaked)
				{
					_Tracker.Scan(_Wallet._ScanLocation, _Wallet.Created);
					_Tracker.SendMessageAsync(new MempoolPayload());
				}
			}

			void AttachedNode_Disconnected(Node node)
			{
				_Wallet.TryUpdateLocation(new[] { node });
			}

			protected override void DetachCore()
			{
				if(_Tracker != null)
				{
					AttachedNode.Disconnected -= AttachedNode_Disconnected;
					AttachedNode.StateChanged -= AttachedNode_StateChanged;
				}
			}

			public override object Clone()
			{
				return new WalletBehavior(_Wallet);
			}
		}
		/// <summary>
		/// Get incoming transactions of the wallet, subscribers should not make any blocking call
		/// </summary>
		public event NewWalletTransactionDelegate NewWalletTransaction;
		class PathState
		{
			public int Loaded;
			public int Next;
		}
		internal BlockLocator _ScanLocation;
		Dictionary<KeyPath, PathState> _PathStates = new Dictionary<KeyPath, PathState>();

		int _KeyPoolSize;

#pragma warning disable CS0612 // Type or member is obsolete
		WalletCreation _Parameters;
#pragma warning restore CS0612 // Type or member is obsolete

		Dictionary<Script, KeyPath> _KnownScripts = new Dictionary<Script, KeyPath>();

		/// <summary>
		/// Blocks below this date will not be processed
		/// </summary>
		public DateTimeOffset Created
		{
			get;
			set;
		}

		private Wallet()
		{

		}

#pragma warning disable CS0612 // Type or member is obsolete
		/// <summary>
		/// Create a new wallet
		/// </summary>
		/// <param name="creation">Creation parameters</param>
		/// <param name="keyPoolSize">The number of keys which will be pre-created</param>
		public Wallet(WalletCreation creation, int keyPoolSize = 500)
#pragma warning restore CS0612 // Type or member is obsolete
		{
			if(creation == null)
				throw new ArgumentNullException(nameof(creation));
			_Parameters = creation;
			_ScanLocation = new BlockLocator();
			_ScanLocation.Blocks.Add(creation.Network.GetGenesis().GetHash());
			_KeyPoolSize = keyPoolSize;
			Created = DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Create a new wallet P2PKH with one key
		/// </summary>
		/// <param name="rootKey"></param>
		/// <param name="keyPoolSize"></param>
		public Wallet(BitcoinExtPubKey rootKey, int keyPoolSize = 500)
#pragma warning disable CS0612 // Type or member is obsolete
			: this(new WalletCreation(rootKey), keyPoolSize)
#pragma warning restore CS0612 // Type or member is obsolete
		{
		}

		private void LoadPool(KeyPath keyPath)
		{
			var lastLoaded = GetLastLoaded(keyPath);
			var isInternal = IsInternal(keyPath);
			var tracker = Tracker;
			for(int i = lastLoaded; i < lastLoaded + _KeyPoolSize; i++)
			{
				var childPath = keyPath.Derive(i, false);
				AddKnown(childPath, tracker, isInternal);
			}
			IncrementLastLoaded(keyPath, _KeyPoolSize);
		}

		private bool IsInternal(KeyPath keyPath)
		{
			return _Parameters.DerivationPath.Derive(1, false) == keyPath;
		}

		private void AddKnown(KeyPath keyPath)
		{
			AddKnown(keyPath, Tracker, IsInternal(keyPath));
		}
#pragma warning disable CS0612 // Type or member is obsolete
		private void AddKnown(KeyPath keyPath, Tracker tracker, bool isInternal)
#pragma warning restore CS0612 // Type or member is obsolete
		{
			if(_Parameters.UseP2SH)
			{
				var script = GetScriptPubKey(keyPath, true);
				_KnownScripts.Add(script.Hash.ScriptPubKey, keyPath);
				tracker.Add(script, true, isInternal, wallet: Name);
			}
			else
			{
				var script = GetScriptPubKey(keyPath, false);
				_KnownScripts.Add(script, keyPath);
				tracker.Add(script, false, isInternal, wallet: Name);
			}
		}

		public bool Rescan(uint256 blockId)
		{
			var block = Chain?.GetBlock(blockId);
			if(block == null)
				return false;
			_ScanLocation = block.GetLocator();
			_Group?.Purge("Rescanning");
			return true;
		}

		private bool AddKnownScriptToTracker()
		{
			string walletName = Name;
			bool added = false;
			var tracker = Tracker;
			var internalChain = _Parameters.DerivationPath.Derive(1);
			foreach(var known in _KnownScripts)
			{
				var child = known.Key;
				var isInternal = known.Value.Parent == internalChain;
				if(tracker.Add(child, _Parameters.UseP2SH, isInternal, wallet: walletName))
					added = true;
			}
			if(added)
				tracker.UpdateTweak();
			return added;
		}

		public string Name
		{
			get
			{
				return _Parameters.Name ?? Created.ToString();
			}
		}

#pragma warning disable CS0612 // Type or member is obsolete
		public WalletTransactionsCollection GetTransactions()
#pragma warning restore CS0612 // Type or member is obsolete
		{
			AssertGroupAffected();
			return Tracker.GetWalletTransactions(Chain, Name);
		}

#pragma warning disable CS0612 // Type or member is obsolete
		public WalletCreation CreationSettings
#pragma warning restore CS0612 // Type or member is obsolete
		{
			get
			{
				return _Parameters;
			}
		}

		public ConcurrentChain Chain
		{
			get
			{
				if(_Group == null)
					return null;
				return _Group.NodeConnectionParameters.TemplateBehaviors
							.OfType<ChainBehavior>()
							.Select(t => t.Chain)
							.FirstOrDefault();
			}
		}

		public AddressManager AddressManager
		{
			get
			{
				if(_Group == null)
					return null;
				return _Group.NodeConnectionParameters.TemplateBehaviors
							.OfType<AddressManagerBehavior>()
							.Select(t => t.AddressManager)
							.FirstOrDefault();
			}
		}

#pragma warning disable CS0612 // Type or member is obsolete
		public Tracker Tracker
#pragma warning restore CS0612 // Type or member is obsolete
		{
			get
			{
				if(_Group == null)
					return null;
				return _Group.NodeConnectionParameters.TemplateBehaviors
#pragma warning disable CS0612 // Type or member is obsolete
							.OfType<TrackerBehavior>()
#pragma warning restore CS0612 // Type or member is obsolete
							.Select(t => t.Tracker)
							.FirstOrDefault();
			}
		}

		object cs = new object();

		public Script GetNextScriptPubKey(bool changeAddress = false)
		{
			return GetNextScriptPubKey(_Parameters.DerivationPath.Derive(changeAddress ? 1 : 0, false));
		}

		public Script GetNextScriptPubKey(KeyPath keyPath)
		{
			AssertGroupAffected();
			Script result;
			lock(cs)
			{
				var currentIndex = GetNextIndex(keyPath);
				KeyPath childPath = keyPath.Derive(currentIndex, false);

				result = GetScriptPubKey(childPath, false);
				IncrementCurrentIndex(keyPath);

				if(_KeyPoolSize != 0)
				{
					var created = (double)(currentIndex + 1) / (double)GetLastLoaded(keyPath);
					if(created > 0.9)
					{
						LoadPool(keyPath);
						RefreshFilter();
					}
				}
				else
				{
					AddKnown(childPath);
					if(_Group != null)
					{
						foreach(var node in _Group.ConnectedNodes)
						{
#pragma warning disable CS0612 // Type or member is obsolete
							var tracker = node.Behaviors.Find<TrackerBehavior>();
#pragma warning restore CS0612 // Type or member is obsolete
							if(tracker == null)
								continue;
							foreach(var data in result.ToOps().Select(o => o.PushData).Where(o => o != null))
							{
								tracker.SendMessageAsync(new FilterAddPayload(data));
							}
						}
					}
				}
			}
			return result;
		}

		private void RefreshFilter()
		{
			if(CreationSettings.PurgeConnectionOnFilterChange)
				_Group.Purge("New bloom filter");
			else
			{
				if(_Group != null)
				{
					foreach(var node in _Group.ConnectedNodes)
					{
#pragma warning disable CS0612 // Type or member is obsolete
						node.Behaviors.Find<TrackerBehavior>().RefreshBloomFilter();
#pragma warning restore CS0612 // Type or member is obsolete
					}
				}
			}
		}

		private void IncrementCurrentIndex(KeyPath keyPath)
		{
			if(!_PathStates.ContainsKey(keyPath))
				_PathStates.Add(keyPath, new PathState());
			_PathStates[keyPath].Next++;
		}

		private int GetNextIndex(KeyPath keyPath)
		{
			if(!_PathStates.ContainsKey(keyPath))
				return 0;
			return _PathStates[keyPath].Next;
		}

		private void IncrementLastLoaded(KeyPath keyPath, int value)
		{
			if(!_PathStates.ContainsKey(keyPath))
				_PathStates.Add(keyPath, new PathState());
			_PathStates[keyPath].Loaded += value;
		}

		private int GetLastLoaded(KeyPath keyPath)
		{
			if(!_PathStates.ContainsKey(keyPath))
				return 0;
			return _PathStates[keyPath].Loaded;
		}

		private void AssertGroupAffected()
		{
			if(_Group == null)
				throw new InvalidOperationException("Wallet.Configure should have been called before for setting up Wallet's components");
		}

		/// <summary>
		/// Get the KeyPath of the given scriptPubKey
		/// </summary>
		/// <param name="scriptPubKey">ScriptPubKey</param>
		/// <returns>The key path to the scriptPubKey</returns>
		public KeyPath GetKeyPath(Script scriptPubKey)
		{
			lock(cs)
			{
				return _KnownScripts.TryGet(scriptPubKey);
			}
		}


		public Script GetRedeemScript(BitcoinScriptAddress address)
		{
			return GetRedeemScript(address.ScriptPubKey);
		}
		public Script GetRedeemScript(Script scriptPubKey)
		{
			if(!_Parameters.UseP2SH)
				throw new InvalidOperationException("This is not a P2SH wallet");
			var path = GetKeyPath(scriptPubKey);
			if(path == null)
				return null;
			return GetScriptPubKey(path, true);
		}

		public Script GetScriptPubKey(KeyPath keyPath, bool redeem)
		{
			if(!_Parameters.UseP2SH && redeem)
				throw new ArgumentException("The wallet is not P2SH so there is no redeem script", "redeem");

			Script scriptPubKey = null;
			if(_Parameters.RootKeys.Length == 1)
			{
				var pubkey = Derivate(0, keyPath).PubKey;
				scriptPubKey = _Parameters.UseP2SH ? pubkey.ScriptPubKey : pubkey.Hash.ScriptPubKey;
			}
			else
			{
				scriptPubKey = CreateMultiSig(keyPath);
			}
			return redeem || !_Parameters.UseP2SH ? scriptPubKey : scriptPubKey.Hash.ScriptPubKey;
		}

		private Script CreateMultiSig(KeyPath keyPath)
		{
			return PayToMultiSigTemplate.Instance.GenerateScriptPubKey(_Parameters.SignatureRequired, _Parameters.RootKeys.Select((r, i) => Derivate(i, keyPath).PubKey).ToArray());
		}

		ExtPubKey[] _ParentKeys;
		private ExtPubKey Derivate(int rootKeyIndex, KeyPath keyPath)
		{
			if(_ParentKeys == null)
			{
				_ParentKeys = _Parameters.RootKeys.Select(r => r.Derive(_Parameters.DerivationPath)).ToArray();
			}
			return _ParentKeys[rootKeyIndex].Derive(keyPath);
		}

#pragma warning disable CS0612 // Type or member is obsolete
		WalletState _State;
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
		public WalletState State
#pragma warning restore CS0612 // Type or member is obsolete
		{
			get
			{
#pragma warning disable CS0612 // Type or member is obsolete
				if(_State == WalletState.Created)
#pragma warning restore CS0612 // Type or member is obsolete
					return _State;
#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
				return _Group.ConnectedNodes.Count == _Group.MaximumNodeConnection ? WalletState.Connected : WalletState.Disconnected;
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0612 // Type or member is obsolete
			}
		}



		/// <summary>
		/// Configure the components of the wallet
		/// </summary>
		/// <param name="chain">The chain to keep in sync, if not provided the whole chain will be downloaded on the network (more than 30MB)</param>
		/// <param name="addrman">The Address Manager for speeding up peer discovery</param>
		/// <param name="tracker">The tracker responsible for providing bloom filters</param>
		public void Configure(ConcurrentChain chain = null,
							AddressManager addrman = null,
#pragma warning disable CS0612 // Type or member is obsolete
							Tracker tracker = null)
#pragma warning restore CS0612 // Type or member is obsolete
		{
#pragma warning disable CS0612 // Type or member is obsolete
			if(State != WalletState.Created)
#pragma warning restore CS0612 // Type or member is obsolete
				throw new InvalidOperationException("The wallet is already connecting or connected");

			var parameters = new NodeConnectionParameters();
			ConfigureDefaultNodeConnectionParameters(parameters);


			//Pick the behaviors
			if(addrman != null)
				parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addrman));  //Listen addr, help for node discovery
			if(chain != null)
				parameters.TemplateBehaviors.Add(new ChainBehavior(chain)); //Keep chain in sync
			if(tracker != null)
#pragma warning disable CS0612 // Type or member is obsolete
				parameters.TemplateBehaviors.Add(new TrackerBehavior(tracker, chain)); //Set bloom filters and scan the blockchain
#pragma warning restore CS0612 // Type or member is obsolete

			Configure(parameters);
		}

		/// <summary>
		/// Configure the components of the wallet
		/// </summary>
		/// <param name="parameters">The parameters to the connection</param>
		public void Configure(NodeConnectionParameters parameters)
		{
			if(parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			Configure(new NodesGroup(_Parameters.Network, parameters));
		}

		/// <summary>
		/// Configure the components of the wallet
		/// </summary>
		/// <param name="group">The group to use</param>
		public void Configure(NodesGroup group)
		{
			if(group == null)
				throw new ArgumentNullException(nameof(group));

			var parameters = group.NodeConnectionParameters;
			group.Requirements.SupportSPV = true;

			var chain = parameters.TemplateBehaviors.Find<ChainBehavior>();
			if(chain == null)
			{
				chain = new ChainBehavior(new ConcurrentChain(_Parameters.Network));
				parameters.TemplateBehaviors.Add(chain);
			}
			if(chain.Chain.Genesis.HashBlock != _Parameters.Network.GetGenesis().GetHash())
				throw new InvalidOperationException("ChainBehavior with invalid network chain detected");

			var addrman = parameters.TemplateBehaviors.Find<AddressManagerBehavior>();
			if(addrman == null)
			{
				addrman = new AddressManagerBehavior(new AddressManager());
				parameters.TemplateBehaviors.Add(addrman);
			}

#pragma warning disable CS0612 // Type or member is obsolete
			var tracker = parameters.TemplateBehaviors.Find<TrackerBehavior>();
#pragma warning restore CS0612 // Type or member is obsolete
			if(tracker == null)
			{
#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
				tracker = new TrackerBehavior(new Tracker(), chain.Chain);
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0612 // Type or member is obsolete
				parameters.TemplateBehaviors.Add(tracker);
			}
			var wallet = FindWalletBehavior(parameters.TemplateBehaviors);
			if(wallet == null)
			{
				wallet = new WalletBehavior(this);
				parameters.TemplateBehaviors.Add(wallet);
			}
			var broadcast = parameters.TemplateBehaviors.Find<BroadcastHubBehavior>();
			if(broadcast == null)
			{
				broadcast = new BroadcastHubBehavior();
				parameters.TemplateBehaviors.Add(broadcast);
			}

			_Group = group;
			if(_ListenedTracker != null)
			{
				_ListenedTracker.NewOperation -= _ListenerTracked_NewOperation;
			}
			_ListenedTracker = tracker.Tracker;
			_ListenedTracker.NewOperation += _ListenerTracked_NewOperation;
		}

		private WalletBehavior FindWalletBehavior(NodeBehaviorsCollection behaviors)
		{
			return behaviors.OfType<WalletBehavior>().FirstOrDefault(o => o.Wallet == this);
		}

#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
		void _ListenerTracked_NewOperation(Tracker sender, Tracker.IOperation trackerOperation)
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0612 // Type or member is obsolete
		{
			var newWalletTransaction = NewWalletTransaction;
			if(newWalletTransaction != null && _Group != null)
			{
				if(trackerOperation.ContainsWallet(Name))
				{
					newWalletTransaction(this, trackerOperation.ToWalletTransaction(Chain, Name));
				}
			}
		}
#pragma warning disable CS0612 // Type or member is obsolete
		Tracker _ListenedTracker;
#pragma warning restore CS0612 // Type or member is obsolete

		/// <summary>
		/// Start the connection to the NodeGroup
		/// </summary>
		public void Connect()
		{
			AssertGroupAffected();
#pragma warning disable CS0612 // Type or member is obsolete
			if(State != WalletState.Created)
#pragma warning restore CS0612 // Type or member is obsolete
				throw new InvalidOperationException("The wallet is already connecting or connected");
#pragma warning disable CS0612 // Type or member is obsolete
			_State = WalletState.Disconnected;
#pragma warning restore CS0612 // Type or member is obsolete
			_Group.Connect();
			_Group.ConnectedNodes.Added += ConnectedNodes_Added;
			foreach(var node in _Group.ConnectedNodes)
			{
				if(FindWalletBehavior(node.Behaviors) == null)
					node.DisconnectAsync("The node is not configured for wallet");
			}
		}

		void ConnectedNodes_Added(object sender, NodeEventArgs e)
		{
			if(FindWalletBehavior(e.Node.Behaviors) == null)
				e.Node.DisconnectAsync("The node is not configured for wallet");
		}

		public static void ConfigureDefaultNodeConnectionParameters(NodeConnectionParameters parameters)
		{
			parameters = parameters ?? new NodeConnectionParameters();

			//Optimize for small device
			parameters.SendBufferSize = 1024 * 100;
			parameters.ReceiveBufferSize = 1024 * 100;
			parameters.IsRelay = false;

			parameters.TemplateBehaviors.FindOrCreate<PingPongBehavior>();  //Ping Pong
		}


		NodesGroup _Group;
		public NodesGroup Group
		{
			get
			{
				return _Group;
			}
		}


		private void TryUpdateLocation(IEnumerable<Node> nodes)
		{
			if(nodes != null)
			{
				var current = Chain.FindFork(_ScanLocation);
				if(current == null)
					return;
				var progress =
						nodes
#pragma warning disable CS0612 // Type or member is obsolete
					   .Select(f => f.Behaviors.Find<TrackerBehavior>())
#pragma warning restore CS0612 // Type or member is obsolete
					   .Where(f => f != null)
					   .Select(f => f.CurrentProgress)
					   .Where(p => p != null)
					   .Select(l => new
					   {
						   Locator = l,
						   Block = Chain.FindFork(l)
					   })
					   .Where(o => o.Block.Height > current.Height)
					   .OrderByDescending(o => o.Block.Height)
					   .Select(o => o.Block)
					   .FirstOrDefault();
				if(progress != null)
				{
					_ScanLocation = progress.GetLocator();
				}
			}
		}
		void TryUpdateLocation()
		{
			TryUpdateLocation(_Group == null ? null : _Group.ConnectedNodes);
		}

		public void Disconnect()
		{
#pragma warning disable CS0612 // Type or member is obsolete
			if(_State == WalletState.Created)
#pragma warning restore CS0612 // Type or member is obsolete
				return;
			TryUpdateLocation();
			_Group.Disconnect();
			_Group.ConnectedNodes.Added -= ConnectedNodes_Added;
#pragma warning disable CS0612 // Type or member is obsolete
			_State = WalletState.Created;
#pragma warning restore CS0612 // Type or member is obsolete
		}

		public int ConnectedNodes
		{
			get
			{
				return _Group.ConnectedNodes.Count;
			}
		}


		public static Wallet Load(Stream stream)
		{
			Wallet wallet = new Wallet();
			wallet.LoadCore(stream);
			return wallet;
		}
		public void Save(Stream stream)
		{
			lock(cs)
			{
				JObject obj = new JObject();
				var indices = new JArray();
				foreach(var indice in _PathStates)
				{
					JObject index = new JObject();
					index.Add(new JProperty("KeyPath", indice.Key.ToString()));
					index.Add(new JProperty("Next", indice.Value.Next));
					index.Add(new JProperty("Loaded", indice.Value.Loaded));
					indices.Add(index);
				}
				obj.Add("Indices", indices);
				obj.Add("KeyPoolSize", this._KeyPoolSize);
				obj.Add("Created", this.Created);
				obj.Add("Parameters", this._Parameters.ToJson());

				TryUpdateLocation();
				obj.Add("Location", Encoders.Hex.EncodeData(this._ScanLocation.ToBytes()));

				var knownScripts = new JArray();
				foreach(var knownScript in _KnownScripts)
				{
					JObject known = new JObject();
					known.Add("ScriptPubKey", Encoders.Hex.EncodeData(knownScript.Key.ToBytes()));
					known.Add("AbsoluteKeyPath", knownScript.Value.ToString());
					knownScripts.Add(known);
				}
				obj.Add("KnownScripts", knownScripts);
				var writer = new StreamWriter(stream);
				writer.Write(JsonConvert.SerializeObject(obj, new JsonSerializerSettings()
				{
					DateParseHandling = DateParseHandling.DateTimeOffset
				}));
				writer.Flush();
			}
		}


		void LoadCore(Stream stream)
		{
			JObject obj = JObject.Load(new JsonTextReader(new StreamReader(stream))
			{
				DateParseHandling = DateParseHandling.DateTimeOffset
			});
#pragma warning disable CS0612 // Type or member is obsolete
			_Parameters = WalletCreation.FromJson((JObject)obj["Parameters"]);
#pragma warning restore CS0612 // Type or member is obsolete
			_PathStates = new Dictionary<KeyPath, PathState>();
			if(obj.Property("CurrentIndex") != null) //legacy
			{
				var idx = (int)(long)obj["CurrentIndex"];
				var loadedKeys = (int)(long)obj["LoadedKeys"];
				_PathStates.Add(_Parameters.DerivationPath.Derive(0), new PathState()
				{
					Next = idx,
					Loaded = loadedKeys
				});
				_PathStates.Add(_Parameters.DerivationPath.Derive(1), new PathState()
				{
					Next = idx,
					Loaded = loadedKeys
				});
			}

			var indices = obj["Indices"] as JArray;
			if(indices != null)
			{
				foreach(var indice in indices.OfType<JObject>())
				{
					_PathStates.Add(KeyPath.Parse((string)indice["KeyPath"]), new PathState()
					{
						Next = (int)(long)indice["Next"],
						Loaded = (int)(long)indice["Loaded"]
					});
				}
			}
			_KeyPoolSize = (int)(long)obj["KeyPoolSize"];
			Created = (DateTimeOffset)obj["Created"];
			_ScanLocation = new BlockLocator();
			_ScanLocation.FromBytes(Encoders.Hex.DecodeData((string)obj["Location"]));
			_KnownScripts.Clear();
			var knownScripts = (JArray)obj["KnownScripts"];
			foreach(var known in knownScripts.OfType<JObject>())
			{
				Script script = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)known["ScriptPubKey"]));
				if(known["KeyPath"] != null) //Legacy data
				{
					KeyPath keypath = KeyPath.Parse((string)known["KeyPath"]);
					_KnownScripts.Add(script, _Parameters.DerivationPath.Derive(keypath));
				}
				if(known["AbsoluteKeyPath"] != null)
				{
					KeyPath keypath = KeyPath.Parse((string)known["AbsoluteKeyPath"]);
					_KnownScripts.Add(script, keypath);
				}
			}
		}

		public KeyValuePair<Script, KeyPath>[] GetKnownScripts(bool onlyGenerated = false)
		{
			KeyValuePair<Script, KeyPath>[] result;
			lock(cs)
			{
				result = _KnownScripts.Where(s => !onlyGenerated ||
												  s.Value.Indexes.Last() < GetNextIndex(s.Value.Parent)).ToArray();
			}
			return result;
		}

		/// <summary>
		/// Broadcast a transaction, if the same template behavior as been used for other nodes, they will also broadcast
		/// </summary>
		/// <param name="transaction">The transaction to broadcast</param>
		/// <returns>The cause of the rejection or null</returns>
		public Task<RejectPayload> BroadcastTransactionAsync(Transaction transaction)
		{
			AssertGroupAffected();
			var hub = BroadcastHub.GetBroadcastHub(_Group.NodeConnectionParameters);
			if(hub == null)
				throw new InvalidOperationException("No broadcast hub detected in the group");
			return hub.BroadcastTransactionAsync(transaction);
		}

		public event TransactionBroadcastedDelegate TransactionBroadcasted;
		public event TransactionRejectedDelegate TransactionRejected;

		internal void OnTransactionBroadcasted(Transaction tx)
		{
			var transactionBroadcasted = TransactionBroadcasted;
			if(transactionBroadcasted != null)
				transactionBroadcasted(tx);
		}

		internal void OnTransactionRejected(Transaction tx, RejectPayload reject)
		{
			var transactionRejected = TransactionRejected;
			if(transactionRejected != null)
				transactionRejected(tx, reject);
		}
	}
}
#endif
#endif