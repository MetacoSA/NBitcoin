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
	public enum WalletState
	{
		Created,
		Disconnected,
		Connected
	}

	public class WalletCreation
	{

		public WalletCreation()
		{
			SignatureRequired = 1;
			RootKeys = new ExtPubKey[0];
			Network = Network.Main;
			DerivationPath = new KeyPath();
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

		internal JObject ToJson()
		{
			JObject obj = new JObject();
			obj["SignatureRequired"] = SignatureRequired;
			obj["DerivationPath"] = DerivationPath.ToString();
			obj["UseP2SH"] = UseP2SH;
			obj["RootKeys"] = new JArray(RootKeys.Select(c => c.GetWif(Network).ToString()));
			return obj;
		}

		static internal WalletCreation FromJson(JObject obj)
		{
			WalletCreation creation = new WalletCreation();
			creation.SignatureRequired = (int)(long)obj["SignatureRequired"];
			creation.DerivationPath = new KeyPath((string)obj["DerivationPath"]);
			creation.UseP2SH = (bool)obj["UseP2SH"];
			var array = (JArray)obj["RootKeys"];
			var keys = array.Select(i => new BitcoinExtPubKey((string)i)).ToArray();
			creation.Network = keys[0].Network;
			creation.RootKeys = keys.Select(k => k.ExtPubKey).ToArray();
			return creation;
		}
	}

	/// <summary>
	/// A SPV Wallet respecting recommendation for privacy http://eprint.iacr.org/2014/763.pdf
	/// </summary>
	public class Wallet
	{

		internal BlockLocator _ScanLocation;
		Tracker _Tracker;
		int _CurrentIndex;
		int _LoadedKeys;
		int _KeyPoolSize;

		WalletCreation _Parameters;

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

		/// <summary>
		/// Create a new wallet
		/// </summary>
		/// <param name="creation">Creation parameters</param>
		/// <param name="keyPoolSize">The number of keys which will be pre-created</param>
		public Wallet(WalletCreation creation, int keyPoolSize = 500)
		{
			if(creation == null)
				throw new ArgumentNullException("creation");
			_Parameters = creation;
			_ScanLocation = new BlockLocator(new List<uint256>()
			{
				creation.Network.GetGenesis().GetHash()
			});
			_KeyPoolSize = keyPoolSize;
			Created = DateTimeOffset.UtcNow;
			LoadPool(0, keyPoolSize);
		}

		/// <summary>
		/// Create a new wallet P2PKH with one key
		/// </summary>
		/// <param name="rootKey"></param>
		/// <param name="keyPoolSize"></param>
		public Wallet(BitcoinExtPubKey rootKey, int keyPoolSize = 500)
			: this(new WalletCreation(rootKey), keyPoolSize)
		{
		}

		private void LoadPool(int indexFrom, int count)
		{

			var walletName = GetWalletName();
			for(int i = indexFrom ; i < indexFrom + count ; i++)
			{
				var child = GetScriptPubKey(false, i);
				_KnownScripts.Add(child, new KeyPath((uint)0, (uint)i));

				child = GetScriptPubKey(true, i);
				_KnownScripts.Add(child, new KeyPath((uint)1, (uint)i));
			}
			_LoadedKeys += count;
		}

		private bool AddKnownScriptToTracker()
		{
			string walletName = GetWalletName();
			bool added = false;
			foreach(var known in _KnownScripts)
			{
				var child = known.Key;
				var isInternal = known.Value.Indexes[0] == 1;
				if(_Tracker.Add(child, _Parameters.UseP2SH, isInternal, wallet: walletName))
					added = true;
			}
			if(added)
				_Tracker.UpdateTweak();
			return added;
		}

		private string GetWalletName()
		{
			return Created.ToString();
		}

		public WalletTransactionsCollection GetTransactions()
		{
			if(State == WalletState.Created)
				throw new InvalidOperationException("Wallet should be in Connected/Disconnected state to get the transactions");
			return _Tracker.GetWalletTransactions(_Chain, GetWalletName());
		}

		private ConcurrentChain _Chain;
		public ConcurrentChain Chain
		{
			get
			{
				return _Chain;
			}
		}
		AddressManager _AddressManager;
		public AddressManager AddressManager
		{
			get
			{
				return _AddressManager;
			}
		}

		public Tracker Tracker
		{
			get
			{
				return _Tracker;
			}
		}

		object cs = new object();
		public Script GetNextScriptPubKey(bool changeAddress = false)
		{
			if(_Tracker == null)
				throw new InvalidOperationException("Wallet.Connect should have been called");
			Script result;
			lock(cs)
			{
				result = GetScriptPubKey(changeAddress, _CurrentIndex);
				if(_Parameters.UseP2SH)
					result = result.Hash.ScriptPubKey;
				_CurrentIndex++;
				var created = (double)_CurrentIndex / (double)_LoadedKeys;
				if(created > 0.9)
				{
					LoadPool(_LoadedKeys, _KeyPoolSize);
					if(AddKnownScriptToTracker())
						_Group.Purge("New bloom filter");
				}
			}
			return result;
		}

		public KeyPath GetKeyPath(Script script)
		{
			lock(cs)
			{
				return _KnownScripts.TryGet(script);
			}
		}

		protected Script GetScriptPubKey(bool isChange, int index)
		{
			if(_Parameters.UseP2SH)
			{
				if(_Parameters.RootKeys.Length == 1)
					return Derivate(0, isChange, index).PubKey.ScriptPubKey;
				else
					return CreateMultiSig(isChange, index);
			}
			else
			{
				if(_Parameters.RootKeys.Length == 1)
					return Derivate(0, isChange, index).PubKey.Hash.ScriptPubKey;
				else
					return CreateMultiSig(isChange, index);
			}
		}

		private Script CreateMultiSig(bool isChange, int index)
		{
			return PayToMultiSigTemplate.Instance.GenerateScriptPubKey(_Parameters.SignatureRequired, _Parameters.RootKeys.Select((r, i) => Derivate(i, isChange, index).PubKey).ToArray());
		}

		ExtPubKey[] _ParentKeys;
		private ExtPubKey Derivate(int rootKeyIndex, bool isChange, int index)
		{
			if(_ParentKeys == null)
			{
				_ParentKeys = _Parameters.RootKeys.Select(r => r.Derive(_Parameters.DerivationPath)).ToArray();
			}
			return _ParentKeys[rootKeyIndex].Derive((uint)(isChange ? 1 : 0)).Derive((uint)index);
		}

		WalletState _State;
		public WalletState State
		{
			get
			{
				if(_State == WalletState.Created)
					return _State;
				return _Group.ConnectedNodes.Count == _Group.MaximumNodeConnection ? WalletState.Connected : WalletState.Disconnected;
			}
		}



		/// <summary>
		/// Connect the wallet to the bitcoin network asynchronously
		/// </summary>
		/// <param name="chain">The chain to keep in sync, if not provided the whole chain will be downloaded on the network (more than 30MB)</param>
		/// <param name="addrman">The Address Manager for speeding up peer discovery</param>
		/// <param name="tracker">The tracker responsible for providing bloom filters</param>
		public void Connect(ConcurrentChain chain = null,
							AddressManager addrman = null,
							Tracker tracker = null)
		{
			if(State != WalletState.Created)
				throw new InvalidOperationException("The wallet is already connecting or connected");

			addrman = addrman ?? new AddressManager();
			chain = chain ?? new ConcurrentChain(_Parameters.Network);
			tracker = tracker ?? new Tracker();

			var parameters = new NodeConnectionParameters();
			ConfigureDefaultNodeConnectionParameters(parameters);


			//Pick the behaviors
			if(addrman != null)
				parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addrman));	//Listen addr, help for node discovery
			if(chain != null)
				parameters.TemplateBehaviors.Add(new ChainBehavior(chain));	//Keep chain in sync
			if(tracker != null)
				parameters.TemplateBehaviors.Add(new TrackerBehavior(tracker, chain)); //Set bloom filters and scan the blockchain

			Connect(parameters);
		}

		/// <summary>
		/// Connect the wallet by using the group's parameters
		/// </summary>
		/// <param name="group"></param>
		public void Connect(NodesGroup group)
		{
			Connect(group.NodeConnectionParameters);
		}

		/// <summary>
		/// Connect the wallet with the given connection parameters
		/// </summary>
		/// <param name="parameters"></param>
		public void Connect(NodeConnectionParameters parameters)
		{
			if(State != WalletState.Created)
				throw new InvalidOperationException("The wallet is already connecting or connected");
			var group = NodesGroup.GetNodeGroup(parameters);
			if(group == null)
			{
				group = new NodesGroup(_Parameters.Network, parameters);
			}
			parameters = group.NodeConnectionParameters;
			group.Requirements.MinVersion = ProtocolVersion.PROTOCOL_VERSION;
			group.Requirements.RequiredServices = NodeServices.Network;

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

			var tracker = parameters.TemplateBehaviors.Find<TrackerBehavior>();
			if(tracker == null)
			{
				tracker = new TrackerBehavior(new Tracker(), chain.Chain);
				parameters.TemplateBehaviors.Add(tracker);
			}

			_Chain = chain.Chain;
			_AddressManager = addrman.AddressManager;
			_Tracker = tracker.Tracker;
			_TrackerBehavior = tracker;
			_Group = group;
			if(AddKnownScriptToTracker())
				_Group.Purge("Bloom filter renew");
			_State = WalletState.Disconnected;
			_Group.Connect();
			_Group.ConnectedNodes.Added += ConnectedNodes_Added;
			_Group.ConnectedNodes.Removed += ConnectedNodes_Added;
			foreach(var node in _Group.ConnectedNodes)
			{
				node.Behaviors.Find<TrackerBehavior>().Scan(_ScanLocation, Created);
			}
		}

		public static void ConfigureDefaultNodeConnectionParameters(NodeConnectionParameters parameters)
		{
			parameters = parameters ?? new NodeConnectionParameters();
			parameters.IsTrusted = false; //Connecting to the wild

			//Optimize for small device
			parameters.ReuseBuffer = false;
			parameters.SendBufferSize = 1024 * 100;
			parameters.ReceiveBufferSize = 1024 * 100;
			parameters.IsRelay = false;

			parameters.TemplateBehaviors.FindOrCreate<PingPongBehavior>();	//Ping Pong
		}

		void ConnectedNodes_Added(object sender, NodeEventArgs e)
		{
			e.Node.Behaviors.Find<TrackerBehavior>().Scan(_ScanLocation, Created);
		}

		TrackerBehavior _TrackerBehavior;

		NodesGroup _Group;


		private void TryUpdateLocation()
		{
			var group = _Group;
			if(group != null)
			{
				var progress =
						group.ConnectedNodes
					   .Select(f => f.Behaviors.Find<TrackerBehavior>().CurrentProgress)
					   .Where(p => p != null)
					   .Select(l => new
					   {
						   Locator = l,
						   Block = Chain.FindFork(l)
					   })
					   .OrderByDescending(o => o.Block.Height)
					   .Select(o => o.Block)
					   .FirstOrDefault();
				if(progress != null)
				{
					progress = progress.EnumerateToGenesis().Skip(5).FirstOrDefault() ?? progress; //Step down 5 blocks, it does not cost a lot to rescan them in case we missed something
					_ScanLocation = progress.GetLocator();
				}
			}
		}

		public void Disconnect()
		{
			if(_State == WalletState.Created)
				return;
			TryUpdateLocation();
			_Group.Disconnect();
			_Group.ConnectedNodes.Added -= ConnectedNodes_Added;
			_Group.ConnectedNodes.Removed -= ConnectedNodes_Added;
			_State = WalletState.Created;
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
				obj.Add("CurrentIndex", this._CurrentIndex);
				obj.Add("KeyPoolSize", this._KeyPoolSize);
				obj.Add("LoadedKeys", this._LoadedKeys);
				obj.Add("Created", this.Created);
				obj.Add("Parameters", this._Parameters.ToJson());

				TryUpdateLocation();
				obj.Add("Location", Encoders.Hex.EncodeData(this._ScanLocation.ToBytes()));

				var knownScripts = new JArray();
				foreach(var knownScript in _KnownScripts)
				{
					JObject known = new JObject();
					known.Add("ScriptPubKey", Encoders.Hex.EncodeData(knownScript.Key.ToBytes()));
					known.Add("KeyPath", knownScript.Value.ToString());
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
			_CurrentIndex = (int)(long)obj["CurrentIndex"];
			_KeyPoolSize = (int)(long)obj["KeyPoolSize"];
			_LoadedKeys = (int)(long)obj["LoadedKeys"];
			Created = (DateTimeOffset)obj["Created"];
			_ScanLocation = new BlockLocator();
			_ScanLocation.FromBytes(Encoders.Hex.DecodeData((string)obj["Location"]));
			_Parameters = WalletCreation.FromJson((JObject)obj["Parameters"]);
			_KnownScripts.Clear();
			var knownScripts = (JArray)obj["KnownScripts"];
			foreach(var known in knownScripts.OfType<JObject>())
			{
				Script script = Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)known["ScriptPubKey"]));
				KeyPath keypath = new KeyPath((string)known["KeyPath"]);
				_KnownScripts.Add(script, keypath);
			}
		}

		public KeyValuePair<Script, KeyPath>[] GetKnownScripts(bool onlyGenerated = false)
		{
			KeyValuePair<Script, KeyPath>[] result;
			lock(cs)
			{
				result = _KnownScripts.Where(s => !onlyGenerated || s.Value[1] < _CurrentIndex).ToArray();
			}
			return result;
		}
	}
}
#endif