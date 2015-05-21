#if !NOSOCKET
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
	}

	/// <summary>
	/// A SPV Wallet respecting recommendation for privacy http://eprint.iacr.org/2014/763.pdf
	/// </summary>
	public class Wallet
	{

		BlockLocator _ScanLocation;
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
				var child = GetScriptPubKey(0, i);
				_Tracker.Add(child, isRedeemScript: _Parameters.UseP2SH, wallet: walletName);
				_KnownScripts.Add(child, new KeyPath((uint)0, (uint)i));

				child = GetScriptPubKey(1, i);
				_Tracker.Add(child, isInternal: true, isRedeemScript: _Parameters.UseP2SH, wallet: walletName);
				_KnownScripts.Add(child, new KeyPath((uint)1, (uint)i));
			}
			_LoadedKeys += count;
			_Tracker.UpdateTweak();
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
		public Script GetNextScriptPubKey()
		{
			Script result;
			lock(cs)
			{
				result = GetScriptPubKey(0, _CurrentIndex);
				if(_Parameters.UseP2SH)
					result = result.Hash.ScriptPubKey;
				_CurrentIndex++;
				var created = (double)_CurrentIndex / (double)_LoadedKeys;
				if(created > 0.9)
				{
					LoadPool(_LoadedKeys, _KeyPoolSize);
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

		protected Script GetScriptPubKey(int chain, int index)
		{
			if(_Parameters.UseP2SH)
			{
				if(_Parameters.RootKeys.Length == 1)
					return Derivate(0, chain, index).PubKey.ScriptPubKey;
				else
					return CreateMultiSig(chain, index);
			}
			else
			{
				if(_Parameters.RootKeys.Length == 1)
					return Derivate(0, chain, index).PubKey.Hash.ScriptPubKey;
				else
					return CreateMultiSig(chain, index);
			}
		}

		private Script CreateMultiSig(int chain, int index)
		{
			return PayToMultiSigTemplate.Instance.GenerateScriptPubKey(_Parameters.SignatureRequired, _Parameters.RootKeys.Select((r, i) => Derivate(i, chain, index).PubKey).ToArray());
		}

		ExtPubKey[] _ParentKeys;
		private ExtPubKey Derivate(int rootKeyIndex, int chain, int index)
		{
			if(_ParentKeys == null)
			{
				_ParentKeys = _Parameters.RootKeys.Select(r => r.Derive(_Parameters.DerivationPath)).ToArray();
			}
			return _ParentKeys[rootKeyIndex].Derive((uint)chain).Derive((uint)index);
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

		public void Connect(NodesGroup group)
		{
			Connect(group.NodeConnectionParameters);
		}
		public void Connect(NodeConnectionParameters parameters)
		{
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

			if(_LoadedKeys == 0)
			{
				LoadPool(0, _KeyPoolSize);
				_Group.Purge("New bloom filter");
			}

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

		public void Disconnect()
		{
			if(_State == WalletState.Created)
				return;
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
	}
}
#endif