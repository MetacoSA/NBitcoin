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


	/// <summary>
	/// A SPV Wallet respecting recommendation for privacy http://eprint.iacr.org/2014/763.pdf
	/// </summary>
	public class Wallet
	{
		ExtKey _DecryptedKey;
		BitcoinEncryptedSecretNoEC _EncryptedKey;
		ExtPubKey _ExtPubKey;
		KeyPath _DerivationPath;
		Network _Network;
		BlockLocator _ScanLocation;
		Tracker _Tracker;

		int _CurrentIndex;
		int _LoadedKeys;
		int _KeyPoolSize;

		Dictionary<Script, KeyPath> _KnownScripts = new Dictionary<Script, KeyPath>();

		/// <summary>
		/// Blocks below this date will not be processed
		/// </summary>
		public DateTimeOffset Created
		{
			get;
			set;
		}

		public Wallet(BitcoinExtKey masterKey, string passphrase = null, KeyPath derivationPath = null, int keyPoolSize = 500)
			: this(masterKey.ExtKey, masterKey.Network, passphrase, derivationPath, keyPoolSize)
		{
		}
		public Wallet(ExtKey masterKey, Network network, string passphrase = null, KeyPath derivationPath = null, int keyPoolSize = 500)
		{
			if(network == null)
				throw new ArgumentNullException("network");
			_Network = network;
			if(passphrase == null)
				_DecryptedKey = masterKey;
			else
			{
				_EncryptedKey = masterKey.PrivateKey.GetEncryptedBitcoinSecret(passphrase, _Network);
			}
			if(derivationPath == null)
				derivationPath = new KeyPath();
			_DerivationPath = derivationPath;
			_ScanLocation = new BlockLocator(new List<uint256>()
			{
				_Network.GetGenesis().GetHash()
			});
			_ExtPubKey = masterKey.Neuter();
			_KeyPoolSize = keyPoolSize;
			Created = DateTimeOffset.UtcNow;
		}

		private void LoadPool(int indexFrom, int count)
		{
			var parent = _ExtPubKey.Derive(_DerivationPath);
			var walletName = GetWalletName();
			for(int i = indexFrom ; i < indexFrom + count ; i++)
			{
				var external = new KeyPath((uint)0, (uint)i);
				var child = parent.Derive(external);
				_Tracker.Add(child.PubKey.Hash, wallet: walletName);
				_KnownScripts.Add(child.PubKey.Hash.ScriptPubKey, external);

				var intern = new KeyPath((uint)1, (uint)i);
				child = parent.Derive(intern);
				_Tracker.Add(child.PubKey.Hash, isInternal: true, wallet: walletName);
				_KnownScripts.Add(child.PubKey.Hash.ScriptPubKey, intern);
			}
			_LoadedKeys += count;
			_Tracker.UpdateTweak();
		}

		private string GetWalletName()
		{
			return _ExtPubKey.Derive(_DerivationPath).PubKey.Hash.ToString();
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
		public BitcoinExtPubKey NewKey()
		{
			BitcoinExtPubKey result;
			lock(cs)
			{
				var key = GetKey(0, _CurrentIndex);
				_CurrentIndex++;
				result = new BitcoinExtPubKey(key, _Network);

				var created = (double)_CurrentIndex / (double)_LoadedKeys;
				if(created > 0.9)
				{
					LoadPool(_LoadedKeys, _KeyPoolSize);
					_Group.Purge("New bloom filter");
				}
			}
			return result;
		}

		private ExtPubKey GetKey(int chain, int index)
		{
			var result = _ExtPubKey.Derive(_DerivationPath).Derive(new KeyPath((uint)chain, (uint)index));
			return result;
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
		public void Connect(ConcurrentChain chain = null,
							AddressManager addrman = null,
							Tracker tracker = null)
		{
			if(State != WalletState.Created)
				throw new InvalidOperationException("The wallet is already connecting or connected");

			addrman = addrman ?? new AddressManager();
			chain = chain ?? new ConcurrentChain(_Network);
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
				group = new NodesGroup(_Network, parameters);
			}
			parameters = group.NodeConnectionParameters;
			group.Requirements.MinVersion = ProtocolVersion.PROTOCOL_VERSION;
			group.Requirements.RequiredServices = NodeServices.Network;

			var chain = parameters.TemplateBehaviors.Find<ChainBehavior>();
			if(chain == null)
			{
				chain = new ChainBehavior(new ConcurrentChain(_Network));
				parameters.TemplateBehaviors.Add(chain);
			}
			if(chain.Chain.Genesis.HashBlock != _Network.GetGenesis().GetHash())
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
			if(_State != WalletState.Created)
			{
				_State = ((NodesCollection)sender).Count == 0 ? WalletState.Disconnected : WalletState.Connected;
			}
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