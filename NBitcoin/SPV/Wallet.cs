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
		Connecting,
		Connected,
		Disconnecting,
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
		Dictionary<Script, int> _KnownScripts = new Dictionary<Script, int>();

		/// <summary>
		/// Blocks below this date will not be processed
		/// </summary>
		public DateTimeOffset Created
		{
			get;
			set;
		}

		public Wallet(BitcoinExtKey masterKey, string passphrase = null, KeyPath derivationPath = null)
			: this(masterKey.ExtKey, masterKey.Network, passphrase, derivationPath)
		{
		}
		public Wallet(ExtKey masterKey, Network network, string passphrase = null, KeyPath derivationPath = null)
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
			_Tracker = new Tracker();
			MaximumNodeConnection = 8;
			Created = DateTimeOffset.UtcNow;
		}

		object cs = new object();
		public BitcoinExtPubKey NewKey()
		{
			BitcoinExtPubKey result;
			lock(cs)
			{
				var key = GetKey(0, _CurrentIndex);
				_CurrentIndex++;
				_Tracker.Add(key.PubKey.Hash);
				_KnownScripts.Add(key.PubKey.Hash.ScriptPubKey, _CurrentIndex - 1);
				result = new BitcoinExtPubKey(key, _Network);
			}
			if(_TrackerBehavior != null)
				_TrackerBehavior.RefreshBloomFilter();
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
				return _State;
			}
		}

		public int MaximumNodeConnection
		{
			get;
			set;
		}

		/// <summary>
		/// Connect the wallet to the bitcoin network asynchronously
		/// </summary>
		/// <param name="chain">The chain to keep in sync, if not provided the whole chain will be downloaded on the network (more than 30MB)</param>
		/// <param name="addrman">The Address Manager for speeding up peer discovery</param>
		public void Connect(ConcurrentChain chain = null,
							AddressManager addrman = null)
		{
			if(State != WalletState.Created)
				throw new InvalidOperationException("The wallet is already connecting or connected");
			if(MaximumNodeConnection < 1)
				throw new InvalidOperationException("Invalid MaximumNodeConnection value");
			addrman = addrman ?? new AddressManager();
			chain = chain ?? new ConcurrentChain(_Network);

			var parameters = new NodeConnectionParameters();
			parameters.IsTrusted = false; //Connecting to the wild

			//Optimize for small device
			parameters.ReuseBuffer = false;
			parameters.SendBufferSize = 1024 * 100;
			parameters.ReceiveBufferSize = 1024 * 100;
			parameters.IsRelay = false;

			//Pick the behaviors
			parameters.TemplateBehaviors.FindOrCreate<PingPongBehavior>();	//Ping Pong
			parameters.TemplateBehaviors.Add(new AddressManagerBehavior(addrman));	//Listen addr, help for node discovery
			parameters.TemplateBehaviors.Add(new ChainBehavior(chain));	//Keep chain in sync
			parameters.TemplateBehaviors.Add(new TrackerBehavior(_Tracker)); //Set bloom filters and scan the blockchain
			parameters.TemplateBehaviors.Add(new ConnectedNodesBehavior(_Network, parameters, new NodeRequirement() //Keep a set of connected nodes
			{
				MinVersion = ProtocolVersion.PROTOCOL_VERSION,
				RequiredServices = NodeServices.Network
			})
			{
				MaximumNodeConnection = MaximumNodeConnection
			});

			_State = WalletState.Connecting;
			_TrackerBehavior = parameters.TemplateBehaviors.Find<TrackerBehavior>();
			_ConnectedNodes = parameters.TemplateBehaviors.Find<ConnectedNodesBehavior>();
			_ConnectedNodes.Connect();
		}

		TrackerBehavior _TrackerBehavior;

		ConnectedNodesBehavior _ConnectedNodes;

		public void Disconnect()
		{
			if(_State == WalletState.Disconnecting || _State == WalletState.Created)
				return;
			_State = WalletState.Disconnecting;
			_ConnectedNodes.Disconnect();
			_State = WalletState.Created;
		}

		public int ConnectedNodes
		{
			get
			{
				return _ConnectedNodes.ConnectedNodes.Count;
			}
		}
	}
}
