#if !NOSOCKET
using NBitcoin.Protocol;
#endif
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class NetworkBuilder
	{
		internal NetworkStringParser _NetworkStringParser = new NetworkStringParser();
		internal string _Name;
		internal ChainName _ChainName;
		internal Dictionary<Base58Type, byte[]> _Base58Prefixes = new Dictionary<Base58Type, byte[]>();
		internal Dictionary<Bech32Type, Bech32Encoder> _Bech32Prefixes = new Dictionary<Bech32Type, Bech32Encoder>();
		internal List<string> _Aliases = new List<string>();
		internal int _RPCPort;
		internal int _Port;
		internal uint _Magic;
		internal Consensus _Consensus;
#if !NOSOCKET
		internal List<DNSSeedData> vSeeds = new List<DNSSeedData>();
		internal List<NetworkAddress> vFixedSeeds = new List<NetworkAddress>();
#endif
		internal byte[] _Genesis;
		internal uint? _MaxP2PVersion;
		internal INetworkSet _NetworkSet;
		internal string _UriScheme;

		public NetworkBuilder SetNetworkSet(INetworkSet networkSet)
		{
			_NetworkSet = networkSet;
			return this;
		}

		public NetworkBuilder SetUriScheme(string uriScheme)
		{
			_UriScheme = uriScheme;
			return this;
		}

		public NetworkBuilder SetMaxP2PVersion(uint version)
		{
			_MaxP2PVersion = version;
			return this;
		}

		public NetworkBuilder SetName(string name)
		{
			_Name = name;
			return this;
		}

		public void CopyFrom(Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			_Base58Prefixes.Clear();
			_Bech32Prefixes.Clear();
			for (int i = 0; i < network.base58Prefixes.Length; i++)
			{
				SetBase58Bytes((Base58Type)i, network.base58Prefixes[i]);
			}
			for (int i = 0; i < network.bech32Encoders.Length; i++)
			{
				SetBech32((Bech32Type)i, network.bech32Encoders[i]);
			}
			SetConsensus(network.Consensus).
			SetGenesis(Encoders.Hex.EncodeData(network.GetGenesis().ToBytes())).
			SetMagic(_Magic).
			SetPort(network.DefaultPort).
			SetRPCPort(network.RPCPort);
			SetNetworkStringParser(network.NetworkStringParser);
			SetNetworkSet(network.NetworkSet);
			SetChainName(network.ChainName);
		}

		public NetworkBuilder SetNetworkStringParser(NetworkStringParser networkStringParser)
		{
			_NetworkStringParser = networkStringParser ?? new NetworkStringParser();
			return this;
		}
		public NetworkBuilder AddAlias(string alias)
		{
			_Aliases.Add(alias);
			return this;
		}

		public NetworkBuilder SetRPCPort(int port)
		{
			_RPCPort = port;
			return this;
		}

		public NetworkBuilder SetPort(int port)
		{
			_Port = port;
			return this;
		}


		public NetworkBuilder SetMagic(uint magic)
		{
			_Magic = magic;
			return this;
		}

#if !NOSOCKET
		public NetworkBuilder AddDNSSeeds(IEnumerable<DNSSeedData> seeds)
		{
			vSeeds.AddRange(seeds);
			return this;
		}
		public NetworkBuilder AddSeeds(IEnumerable<NetworkAddress> seeds)
		{
			vFixedSeeds.AddRange(seeds);
			return this;
		}
#endif

		public NetworkBuilder SetConsensus(Consensus consensus)
		{
			_Consensus = consensus == null ? null : consensus.Clone();
			return this;
		}

		public NetworkBuilder SetGenesis(string hex)
		{
			_Genesis = Encoders.Hex.DecodeData(hex);
			return this;
		}

		public NetworkBuilder SetBase58Bytes(Base58Type type, byte[] bytes)
		{
			_Base58Prefixes.AddOrReplace(type, bytes);
			return this;
		}

		public NetworkBuilder SetBech32(Bech32Type type, string humanReadablePart)
		{
			_Bech32Prefixes.AddOrReplace(type, Encoders.Bech32(humanReadablePart));
			return this;
		}
		public NetworkBuilder SetBech32(Bech32Type type, Bech32Encoder encoder)
		{
			_Bech32Prefixes.AddOrReplace(type, encoder);
			return this;
		}

		public NetworkBuilder SetChainName(ChainName chainName)
		{
			_ChainName = chainName;
			return this;
		}

		/// <summary>
		/// Create an immutable Network instance, and register it globally so it is queriable through Network.GetNetwork(string name) and Network.GetNetworks().
		/// </summary>
		/// <returns></returns>
		public Network BuildAndRegister()
		{
			return Network.Register(this);
		}
	}
}
