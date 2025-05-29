#nullable enable
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Buffers;

namespace NBitcoin
{
	public class DNSSeedData
	{
		string name, host;
		public string Name
		{
			get
			{
				return name;
			}
		}
		public string Host
		{
			get
			{
				return host;
			}
		}
		public DNSSeedData(string name, string host)
		{
			this.name = name;
			this.host = host;
		}
#if !NOSOCKET
		public Task<IPEndPoint[]> GetAddressNodesAsync(int port)
		{
			return GetAddressNodesAsync(port, null, default);
		}
		public Task<IPEndPoint[]> GetAddressNodesAsync(int port, IDnsResolver? dnsResolver, CancellationToken cancellationToken = default)
		{
			var dns = new DnsEndPoint(Host, port);
			return dns.ResolveToIPEndpointsAsync(dnsResolver, cancellationToken);
		}
#endif
		public override string ToString()
		{
			return name + " (" + host + ")";
		}
	}
	public class ChainName
	{
		static ChainName()
		{
			Mainnet = new ChainName("Mainnet");
			Testnet = new ChainName("Testnet");
			Regtest = new ChainName("Regtest");
		}
		public static ChainName Mainnet { get; }
		public static ChainName Testnet { get; }
		public static ChainName Regtest { get; }

		private readonly string nameInvariant;

		public ChainName(string chainName)
		{
			if (chainName == null)
				throw new ArgumentNullException(nameof(chainName));
			if (chainName.Length is 0)
				throw new ArgumentException("Empty chainName is invalid", nameof(chainName));

#if !HAS_SPAN
			var invariant = chainName.ToLowerInvariant().ToCharArray();
			invariant[0] = char.ToUpperInvariant(invariant[0]);
			this.nameInvariant = new string(invariant);
#else
			this.nameInvariant = String.Create<string>(chainName.Length, chainName, CreateInvariant);
#endif
		}
#if HAS_SPAN
		static void CreateInvariant(Span<char> span, string arg)
		{
			MemoryExtensions.ToLowerInvariant(arg.AsSpan(), span);
			span[0] = char.ToUpperInvariant(span[0]);
		}
#endif

		public override bool Equals(object? obj)
		{
			ChainName? item = obj as ChainName;
			if (item is null)
				return false;
			return nameInvariant.Equals(item.nameInvariant);
		}
		public static bool operator ==(ChainName a, ChainName b)
		{
			if (a is ChainName && b is ChainName)
				return a.nameInvariant == b.nameInvariant;
			if (a is null && b is null)
				return true;
			return false;
		}

		public static bool operator !=(ChainName a, ChainName b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return nameInvariant.GetHashCode();
		}
		public override string ToString()
		{
			return nameInvariant;
		}
	}

	public enum Base58Type
	{
		PUBKEY_ADDRESS,
		SCRIPT_ADDRESS,
		SECRET_KEY,
		EXT_PUBLIC_KEY,
		EXT_SECRET_KEY,
		ENCRYPTED_SECRET_KEY_EC,
		ENCRYPTED_SECRET_KEY_NO_EC,
		PASSPHRASE_CODE,
		CONFIRMATION_CODE,
		ASSET_ID,
		COLORED_ADDRESS,
		BLINDED_ADDRESS,
		MAX_BASE58_TYPES,
	};

	public enum Bech32Type
	{
		WITNESS_PUBKEY_ADDRESS,
		WITNESS_SCRIPT_ADDRESS,
		TAPROOT_ADDRESS,
		BLINDED_ADDRESS
	}

	public partial class Network
	{
		static Network()
		{
			Bitcoin.Instance.Init();
		}
		internal byte[][] base58Prefixes = new byte[13][];
		internal Bech32Encoder[] bech32Encoders = new Bech32Encoder[3];

		public String UriScheme { get; internal set; }
		public uint MaxP2PVersion
		{
			get;
			internal set;
		}

		public Bech32Encoder? GetBech32Encoder(Bech32Type type, bool throws)
		{
			var encoder = bech32Encoders[(int)type];
			if (encoder == null && throws)
				throw Bech32NotSupported(type);
			return encoder;
		}

		internal NotSupportedException Bech32NotSupported(Bech32Type type)
		{
			return new NotSupportedException("The network " + this + " does not have any prefix for bech32 " + Enum.GetName(typeof(Bech32Type), type));
		}

		public byte[]? GetVersionBytes(Base58Type type, bool throws)
		{
			var prefix = base58Prefixes[(int)type];
			if (prefix == null && throws)
				throw Base58NotSupported(type);
			return prefix?.ToArray();
		}
#if HAS_SPAN
		public ReadOnlyMemory<byte>? GetVersionMemory(Base58Type type, bool throws)
		{
			var prefix = base58Prefixes[(int)type];
			if (prefix == null && throws)
				throw Base58NotSupported(type);
			return prefix?.AsMemory();
		}
#endif

		internal NotSupportedException Base58NotSupported(Base58Type type)
		{
			return new NotSupportedException("The network " + this + " does not have any prefix for base58 " + Enum.GetName(typeof(Base58Type), type));
		}

		internal static string CreateBase58(Base58Type type, byte[] bytes, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			var versionBytes = network.GetVersionBytes(type, true)!;
			return network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(versionBytes.Concat(bytes));
		}

		internal static string CreateBech32(Bech32Type type, byte[] bytes, byte witnessVersion, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if (network.GetBech32Encoder(type, false) is Bech32Encoder encoder)
				return encoder.Encode(witnessVersion, bytes);
			throw network.Bech32NotSupported(type);
		}

		public Transaction CreateTransaction()
		{
			return Consensus.ConsensusFactory.CreateTransaction();
		}
	}

	public enum BuriedDeployments : int
	{
		/// <summary>
		/// Height in coinbase
		/// </summary>
		BIP34,
		/// <summary>
		/// Height in OP_CLTV
		/// </summary>
		BIP65,
		/// <summary>
		/// Strict DER signature
		/// </summary>
		BIP66
	}

	public class Consensus
	{
		public static Consensus Main
		{
			get
			{
				return Network.Main.Consensus;
			}
		}
		public static Consensus TestNet
		{
			get
			{
				return Network.TestNet.Consensus;
			}
		}
		public static Consensus RegTest
		{
			get
			{
				return Network.RegTest.Consensus;
			}
		}
		public class BuriedDeploymentsArray
		{
			Consensus _Parent;
			int[] _Heights;
			public BuriedDeploymentsArray(Consensus parent)
			{
				_Parent = parent;
				_Heights = new int[Enum.GetValues(typeof(BuriedDeployments)).Length];
			}
			public int this[BuriedDeployments index]
			{
				get
				{
					return _Heights[(int)index];
				}
				set
				{
					_Parent.EnsureNotFrozen();
					_Heights[(int)index] = value;
				}
			}
		}
		public class BIP9DeploymentsArray
		{
			Consensus _Parent;
			BIP9DeploymentsParameters[] _Parameters;
			public BIP9DeploymentsArray(Consensus parent)
			{
				_Parent = parent;
				_Parameters = new BIP9DeploymentsParameters[Enum.GetValues(typeof(BIP9Deployments)).Length];
			}

			public BIP9DeploymentsParameters this[BIP9Deployments index]
			{
				get
				{
					return _Parameters[(int)index];
				}
				set
				{
					_Parent.EnsureNotFrozen();
					_Parameters[(int)index] = value;
				}
			}
		}

		public Consensus()
		{
			_BuriedDeployments = new BuriedDeploymentsArray(this);
			_BIP9Deployments = new BIP9DeploymentsArray(this);
		}
		private readonly BuriedDeploymentsArray _BuriedDeployments;
		public BuriedDeploymentsArray BuriedDeployments
		{
			get
			{
				return _BuriedDeployments;
			}
		}


		private readonly BIP9DeploymentsArray _BIP9Deployments;
		public BIP9DeploymentsArray BIP9Deployments
		{
			get
			{
				return _BIP9Deployments;
			}
		}

		int _SubsidyHalvingInterval;
		public int SubsidyHalvingInterval
		{
			get
			{
				return _SubsidyHalvingInterval;
			}
			set
			{
				EnsureNotFrozen();
				_SubsidyHalvingInterval = value;
			}
		}

		private ConsensusFactory _ConsensusFactory = new ConsensusFactory();
		public ConsensusFactory ConsensusFactory
		{
			get
			{
				return _ConsensusFactory;
			}
			set
			{
				EnsureNotFrozen();
				_ConsensusFactory = value;
			}
		}


		int _MajorityEnforceBlockUpgrade;

		public int MajorityEnforceBlockUpgrade
		{
			get
			{
				return _MajorityEnforceBlockUpgrade;
			}
			set
			{
				EnsureNotFrozen();
				_MajorityEnforceBlockUpgrade = value;
			}
		}

		int _MajorityRejectBlockOutdated;
		public int MajorityRejectBlockOutdated
		{
			get
			{
				return _MajorityRejectBlockOutdated;
			}
			set
			{
				EnsureNotFrozen();
				_MajorityRejectBlockOutdated = value;
			}
		}

		int _MajorityWindow;
		public int MajorityWindow
		{
			get
			{
				return _MajorityWindow;
			}
			set
			{
				EnsureNotFrozen();
				_MajorityWindow = value;
			}
		}

		uint256? _BIP34Hash;
		public uint256? BIP34Hash
		{
			get
			{
				return _BIP34Hash;
			}
			set
			{
				EnsureNotFrozen();
				_BIP34Hash = value;
			}
		}


		Target? _PowLimit;
		public Target? PowLimit
		{
			get
			{
				return _PowLimit;
			}
			set
			{
				EnsureNotFrozen();
				_PowLimit = value;
			}
		}


		TimeSpan _PowTargetTimespan;
		public TimeSpan PowTargetTimespan
		{
			get
			{
				return _PowTargetTimespan;
			}
			set
			{
				EnsureNotFrozen();
				_PowTargetTimespan = value;
			}
		}


		TimeSpan _PowTargetSpacing;
		public TimeSpan PowTargetSpacing
		{
			get
			{
				return _PowTargetSpacing;
			}
			set
			{
				EnsureNotFrozen();
				_PowTargetSpacing = value;
			}
		}


		bool _PowAllowMinDifficultyBlocks;
		public bool PowAllowMinDifficultyBlocks
		{
			get
			{
				return _PowAllowMinDifficultyBlocks;
			}
			set
			{
				EnsureNotFrozen();
				_PowAllowMinDifficultyBlocks = value;
			}
		}


		bool _PowNoRetargeting;
		public bool PowNoRetargeting
		{
			get
			{
				return _PowNoRetargeting;
			}
			set
			{
				EnsureNotFrozen();
				_PowNoRetargeting = value;
			}
		}

		internal void SetBlock(byte[] genesis)
		{
			if (genesis == null)
				throw new ArgumentNullException(nameof(genesis));
			EnsureNotFrozen();
			_genesis = genesis;
		}

		uint256? _HashGenesisBlock;
		byte[]? _genesis;
		public uint256 HashGenesisBlock
		{
			get
			{
				if (_HashGenesisBlock == null)
				{
					if (_genesis == null)
						throw new NotImplementedException("The genesis block is missing");
					var block = ConsensusFactory.CreateBlock();
					block.ReadWrite(_genesis, ConsensusFactory);
					_HashGenesisBlock = block.GetHash();
				}
				return _HashGenesisBlock;
			}
		}

		uint256? _MinimumChainWork;
		public uint256? MinimumChainWork
		{
			get
			{
				return _MinimumChainWork;
			}
			set
			{
				EnsureNotFrozen();
				_MinimumChainWork = value;
			}
		}

		public long DifficultyAdjustmentInterval
		{
			get
			{
				return ((long)PowTargetTimespan.TotalSeconds / (long)PowTargetSpacing.TotalSeconds);
			}
		}

		int _MinerConfirmationWindow;
		public int MinerConfirmationWindow
		{
			get
			{
				return _MinerConfirmationWindow;
			}
			set
			{
				EnsureNotFrozen();
				_MinerConfirmationWindow = value;
			}
		}

		int _RuleChangeActivationThreshold;
		public int RuleChangeActivationThreshold
		{
			get
			{
				return _RuleChangeActivationThreshold;
			}
			set
			{
				EnsureNotFrozen();
				_RuleChangeActivationThreshold = value;
			}
		}


		int _CoinbaseMaturity = 100;
		public int CoinbaseMaturity
		{
			get
			{
				return _CoinbaseMaturity;
			}
			set
			{
				EnsureNotFrozen();
				_CoinbaseMaturity = value;
			}
		}

		int _CoinType;

		/// <summary>
		/// Specify the BIP44 coin type for this network
		/// </summary>
		public int CoinType
		{
			get
			{
				return _CoinType;
			}
			set
			{
				EnsureNotFrozen();
				_CoinType = value;
			}
		}


		bool _LitecoinWorkCalculation;
		/// <summary>
		/// Specify using litecoin calculation for difficulty
		/// </summary>
		public bool LitecoinWorkCalculation
		{
			get
			{
				return _LitecoinWorkCalculation;
			}
			set
			{
				EnsureNotFrozen();
				_LitecoinWorkCalculation = value;
			}
		}

		bool frozen = false;
		public void Freeze()
		{
			frozen = true;
		}
		private void EnsureNotFrozen()
		{
			if (frozen)
				throw new InvalidOperationException("This instance can't be modified");
		}

		bool _SupportTaproot = false;
		public bool SupportTaproot
		{
			get
			{
				return _SupportTaproot;
			}
			set
			{
				EnsureNotFrozen();
				_SupportTaproot = value;
			}
		}

		bool _SupportSegwit = false;
		public bool SupportSegwit
		{
			get
			{
				return _SupportSegwit;
			}
			set
			{
				EnsureNotFrozen();
				_SupportSegwit = value;
			}
		}

		bool _NeverNeedPreviousTxForSigning;
		public bool NeverNeedPreviousTxForSigning
		{
			get
			{
				return _NeverNeedPreviousTxForSigning;
			}
			set
			{
				EnsureNotFrozen();
				_NeverNeedPreviousTxForSigning = value;
			}
		}

		public virtual Consensus Clone()
		{
			var consensus = new Consensus();
			Fill(consensus);
			return consensus;
		}

		public TimeSpan GetExpectedTimeFor(double blockCount)
		{
			return TimeSpan.FromSeconds(blockCount * PowTargetSpacing.TotalSeconds);
		}

		public double GetExpectedBlocksFor(TimeSpan timeSpan)
		{
			return timeSpan.TotalSeconds / PowTargetSpacing.TotalSeconds;
		}

		protected void Fill(Consensus consensus)
		{
			consensus.EnsureNotFrozen();
			consensus._BIP34Hash = _BIP34Hash;
			consensus._HashGenesisBlock = _HashGenesisBlock;
			consensus._MajorityEnforceBlockUpgrade = _MajorityEnforceBlockUpgrade;
			consensus._MajorityRejectBlockOutdated = _MajorityRejectBlockOutdated;
			consensus._MajorityWindow = _MajorityWindow;
			consensus._MinerConfirmationWindow = _MinerConfirmationWindow;
			consensus._PowAllowMinDifficultyBlocks = _PowAllowMinDifficultyBlocks;
			consensus._PowLimit = _PowLimit;
			consensus._PowNoRetargeting = _PowNoRetargeting;
			consensus._PowTargetSpacing = _PowTargetSpacing;
			consensus._PowTargetTimespan = _PowTargetTimespan;
			consensus._RuleChangeActivationThreshold = _RuleChangeActivationThreshold;
			consensus._SubsidyHalvingInterval = _SubsidyHalvingInterval;
			consensus._CoinbaseMaturity = _CoinbaseMaturity;
			consensus._MinimumChainWork = _MinimumChainWork;
			consensus._CoinType = CoinType;
			consensus._ConsensusFactory = _ConsensusFactory;
			consensus._LitecoinWorkCalculation = _LitecoinWorkCalculation;
			consensus._SupportSegwit = _SupportSegwit;
			consensus._SupportTaproot = _SupportTaproot;
			consensus._NeverNeedPreviousTxForSigning = _NeverNeedPreviousTxForSigning;
		}
	}
	public partial class Network
	{





		readonly uint magic;

#if !NOSOCKET
		List<DNSSeedData> vSeeds = new List<DNSSeedData>();
		List<NetworkAddress> vFixedSeeds = new List<NetworkAddress>();
#else
		List<string> vSeeds = new List<string>();
		List<string> vFixedSeeds = new List<string>();
#endif
		readonly byte[] _GenesisBytes;

		private int nRPCPort;
		public int RPCPort
		{
			get
			{
				return nRPCPort;
			}
		}

		private int nDefaultPort;
		public int DefaultPort
		{
			get
			{
				return nDefaultPort;
			}
		}


		private Consensus consensus = new Consensus();
		public Consensus Consensus
		{
			get
			{
				return consensus;
			}
		}

		private Network(string name, byte[] genesis, uint magic, string? uriScheme, INetworkSet networkSet)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (genesis == null)
				throw new ArgumentNullException(nameof(genesis));
			if (networkSet == null)
				throw new ArgumentNullException(nameof(networkSet));
			this.UriScheme = uriScheme ?? "bitcoin";
			this._GenesisBytes = genesis;
			this.magic = magic;
			this._NetworkSet = networkSet;
			this._MagicBytes = new byte[]
			{
				(byte)Magic,
				(byte)(Magic >> 8),
				(byte)(Magic >> 16),
				(byte)(Magic >> 24)
			};
			this.name = name;
		}

		private readonly string name;

		public string Name
		{
			get
			{
				return name;
			}
		}

		private ChainName? chainName;

		public ChainName ChainName
		{
			get
			{
				if (chainName is null)
					throw new InvalidOperationException("Network.ChainName is not set");
				return chainName;
			}
		}

		public static Network Main => Bitcoin.Instance.Mainnet;

		public static Network TestNet => Bitcoin.Instance.Testnet;
		public static Network TestNet4 => Bitcoin.Instance.Testnet4;

		public static Network RegTest => Bitcoin.Instance.Regtest;
		internal const uint BITCOIN_MAX_P2P_VERSION = 70016;
		private static ConcurrentDictionary<string, Network> _OtherAliases = new();
		static List<Network> _OtherNetworks = new List<Network>();


		private readonly INetworkSet _NetworkSet;
		public INetworkSet NetworkSet
		{
			get
			{
				return _NetworkSet;
			}
		}
#if !NOFILEIO
		/// <summary>
		/// Returns the default data directory of bitcoin correctly accross OS
		/// </summary>
		/// <param name="folderName">The name of the folder</param>
		/// <returns>The full path to the data directory of Bitcoin</returns>
		public static string? GetDefaultDataFolder(string folderName)
		{
			var home = Environment.GetEnvironmentVariable("HOME");
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");
			if (string.IsNullOrEmpty(home) && string.IsNullOrEmpty(localAppData))
				return null;
			if (!string.IsNullOrEmpty(home) && string.IsNullOrEmpty(localAppData))
				return Path.Combine(home, "." + folderName.ToLowerInvariant());
			else if (!string.IsNullOrEmpty(localAppData))
				return Path.Combine(localAppData, char.ToUpperInvariant(folderName[0]) + folderName.Substring(1));
			return null;
		}
#endif

		internal static Network Register(NetworkBuilder builder)
		{
			if (builder._Name == null)
				throw new InvalidOperationException("A network name need to be provided");
			if (GetNetwork(builder._Name) != null)
				throw new InvalidOperationException("The network " + builder._Name + " is already registered");
			Network network = new Network(builder._Name, builder._Genesis.ToArray(), builder._Magic, builder._UriScheme, builder._NetworkSet);
			network.chainName = builder._ChainName;
			network.consensus = builder._Consensus;
			network.nDefaultPort = builder._Port;
			network.nRPCPort = builder._RPCPort;
			network.NetworkStringParser = builder._NetworkStringParser;
			network.MaxP2PVersion = builder._MaxP2PVersion == null ? BITCOIN_MAX_P2P_VERSION : builder._MaxP2PVersion.Value;

#if !NOSOCKET
			foreach (var seed in builder.vSeeds)
			{
				network.vSeeds.Add(seed);
			}
			foreach (var seed in builder.vFixedSeeds)
			{
				network.vFixedSeeds.Add(seed);
			}
#endif
			network.base58Prefixes = builder._Name == "Main" ?network.base58Prefixes :  Main.base58Prefixes.ToArray();
			foreach (var kv in builder._Base58Prefixes)
			{
				network.base58Prefixes[(int)kv.Key] = kv.Value;
			}
			var bech32Encoders =  builder._Name == "Main" ? new List<Bech32Encoder>() : new List<Bech32Encoder>(Main.bech32Encoders);
			foreach (var kv in builder._Bech32Prefixes)
			{
				var index = (int)kv.Key;
				if (index < bech32Encoders.Count)
				{
					bech32Encoders[index] = kv.Value;
				}
				else
				{
					bech32Encoders.Add(kv.Value);
				}
			}
			network.bech32Encoders = bech32Encoders.ToArray();

				foreach (var alias in builder._Aliases)
				{
					_OtherAliases.TryAdd(alias.ToLowerInvariant(), network);
				}
				_OtherAliases.TryAdd(network.name.ToLowerInvariant(), network);
				var defaultAlias = network._NetworkSet.CryptoCode.ToLowerInvariant() + "-" + network.ChainName.ToString().ToLowerInvariant();
				_OtherAliases.TryAdd(defaultAlias, network);


			lock (_OtherNetworks)
			{
				_OtherNetworks.Add(network);
			}

			network.consensus.SetBlock(builder._Genesis);
			network.consensus.Freeze();
			return network;
		}


		private Block CreateGenesisBlock(uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
		{
			string pszTimestamp = "The Times 03/Jan/2009 Chancellor on brink of second bailout for banks";
			Script genesisOutputScript = new Script(Op.GetPushOp(Encoders.Hex.DecodeData("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5f")), OpcodeType.OP_CHECKSIG);
			return CreateGenesisBlock(pszTimestamp, genesisOutputScript, nTime, nNonce, nBits, nVersion, genesisReward);
		}

		private Block CreateGenesisBlock(string pszTimestamp, Script genesisOutputScript, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
		{
			Transaction txNew = Consensus.ConsensusFactory.CreateTransaction();
			txNew.Version = 1;
			txNew.Inputs.Add(scriptSig: new Script(Op.GetPushOp(486604799), new Op()
			{
				Code = (OpcodeType)0x1,
				PushData = new[] { (byte)4 }
			}, Op.GetPushOp(Encoders.ASCII.DecodeData(pszTimestamp))));
			txNew.Outputs.Add(genesisReward, genesisOutputScript);
			Block genesis = Consensus.ConsensusFactory.CreateBlock();
			genesis.Header.BlockTime = Utils.UnixTimeToDateTime(nTime);
			genesis.Header.Bits = nBits;
			genesis.Header.Nonce = nNonce;
			genesis.Header.Version = nVersion;
			genesis.Transactions.Add(txNew);
			genesis.Header.HashPrevBlock = uint256.Zero;
			genesis.UpdateMerkleRoot();
			return genesis;
		}



		public BitcoinSecret CreateBitcoinSecret(string base58)
		{
			return new BitcoinSecret(base58, this);
		}

		/// <summary>
		/// Create a bitcoin address from base58 data, return a BitcoinAddress or BitcoinScriptAddress
		/// </summary>
		/// <param name="base58">base58 address</param>
		/// <exception cref="System.FormatException">Invalid base58 address</exception>
		/// <returns>BitcoinScriptAddress, BitcoinAddress</returns>
		public BitcoinAddress CreateBitcoinAddress(string base58)
		{
			var type = GetBase58Type(base58);
			if (!type.HasValue)
				throw new FormatException("Invalid Base58 version");
			if (type == Base58Type.PUBKEY_ADDRESS)
				return new BitcoinPubKeyAddress(base58, this);
			if (type == Base58Type.SCRIPT_ADDRESS)
				return new BitcoinScriptAddress(base58, this);
			throw new FormatException("Invalid Base58 version");
		}

		public BitcoinScriptAddress CreateBitcoinScriptAddress(string base58)
		{
			return new BitcoinScriptAddress(base58, this);
		}

		private Base58Type? GetBase58Type(string base58)
		{
			return GetBase58Type(NetworkStringParser.GetBase58CheckEncoder().DecodeData(base58), out _);
		}
		private Base58Type? GetBase58Type(byte[] bytes, out int prefixLength)
		{
			for (int i = 0; i < base58Prefixes.Length; i++)
			{
				var prefix = base58Prefixes[i];
				if (prefix == null)
					continue;
				if (bytes.Length < prefix.Length)
					continue;
				if (Utils.ArrayEqual(bytes, 0, prefix, 0, prefix.Length))
				{
					prefixLength = prefix.Length;
					return (Base58Type)i;
				}
			}
			prefixLength = 0;
			return null;
		}


		internal static Network? GetNetworkFromBase58Data(string base58, Base58Type? expectedType = null)
		{
			foreach (var network in GetNetworks())
			{
				var type = network.GetBase58Type(base58);
				if (type.HasValue)
				{
					if (expectedType != null && expectedType.Value != type.Value)
						continue;
					if (type.Value == Base58Type.COLORED_ADDRESS)
					{
						var raw = network.NetworkStringParser.GetBase58CheckEncoder().DecodeData(base58);
						var version = network.GetVersionBytes(type.Value, false);
						if (version == null)
							continue;
						raw = raw.Skip(version.Length).ToArray();
						base58 = network.NetworkStringParser.GetBase58CheckEncoder().EncodeData(raw);
						return GetNetworkFromBase58Data(base58, null);
					}
					return network;
				}
			}
			return null;
		}

		public T Parse<T>(string str) where T : IBitcoinString
		{
			return (T)Parse(str, typeof(T));
		}
		public IBitcoinString Parse(string str)
		{
			return Parse(str, null);
		}
		public IBitcoinString Parse(string str, Type? targetType)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));
			targetType ??= typeof(IBitcoinString);
			if (NetworkStringParser.TryParse(str, this, targetType, out var o))
				return o;
			var base58Encoder = NetworkStringParser.GetBase58CheckEncoder();

			var maybeb58 = base58Encoder.IsMaybeEncoded(str);
			if (maybeb58)
			{
				byte[]? decoded = null;
				try
				{
					decoded = base58Encoder.DecodeData(str);
				}
				catch (FormatException) { maybeb58 = false; }
				if (maybeb58)
				{
					var candidate = GetCandidate(str, decoded!);
					if (candidate != null && targetType.GetTypeInfo().IsAssignableFrom((candidate.GetType().GetTypeInfo())))
						return candidate;
					throw new FormatException("Invalid base58 string");
				}
			}

			int i = -1;
#if !NO_TUPLE
			(Bech32Encoder? encoder, byte[]? bytes, byte witVersion) cache = (null, null, 0);
#endif
			foreach (var encoder in bech32Encoders)
			{
				i++;
				if (encoder == null)
					continue;
				var type = (Bech32Type)i;
				try
				{
#if !NO_TUPLE
					byte witVersion;
					byte[] bytes;
					if (cache.encoder == encoder && cache.bytes is not null)
					{
						witVersion = cache.witVersion;
						bytes = cache.bytes;
					}
					else
					{
						bytes = encoder.Decode(str, out witVersion);
						cache = (encoder, bytes, witVersion);
					}
#else
					byte[] bytes = encoder.Decode(str, out var witVersion);
#endif
					IBitcoinString? candidate = null;
					if (witVersion == 0 && bytes.Length == 20 && type == Bech32Type.WITNESS_PUBKEY_ADDRESS)
						candidate = new BitcoinWitPubKeyAddress(str.ToLowerInvariant(), bytes, this);
					if (witVersion == 0 && bytes.Length == 32 && type == Bech32Type.WITNESS_SCRIPT_ADDRESS)
						candidate = new BitcoinWitScriptAddress(str.ToLowerInvariant(), bytes, this);
					if (witVersion == 1 && bytes.Length == 32 && type == Bech32Type.TAPROOT_ADDRESS)
						candidate = new TaprootAddress(str.ToLowerInvariant(), bytes, this);
					if (candidate != null && targetType.GetTypeInfo().IsAssignableFrom((candidate.GetType().GetTypeInfo())))
						return candidate;
				}
				catch (Bech32FormatException) { throw; }
				catch (FormatException) { continue; }
			}
			throw new FormatException("Invalid string");
		}

		public static IBitcoinString Parse(string str, Network expectedNetwork, Type? targetType = null)
		{
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));
			if (str == null)
				throw new ArgumentNullException(nameof(str));
			return expectedNetwork.Parse(str, targetType);
		}

		public static T Parse<T>(string str, Network expectedNetwork) where T : IBitcoinString
		{
			if (expectedNetwork == null)
				throw new ArgumentNullException(nameof(expectedNetwork));
			if (str == null)
				throw new ArgumentNullException(nameof(str));
			return expectedNetwork.Parse<T>(str);
		}

		private IBase58Data? GetCandidate(string base58, byte[] decoded)
		{
			if (base58 == null)
				throw new ArgumentNullException(nameof(base58));
			var maybeType = GetBase58Type(decoded, out var prefixLength);
			if (maybeType is Base58Type type)
			{
				if (type == Base58Type.COLORED_ADDRESS)
				{
					var wrapped = BitcoinColoredAddress.GetWrappedBase58(base58, this);
					var wrappedType = GetBase58Type(wrapped);
					if (wrappedType == null)
						return null;
					try
					{
						var inner = CreateBase58Data(wrappedType.Value, wrapped);
						if (inner.Network != this)
							return null;
					}
					catch (FormatException) { }
				}
				try
				{
					if (type is Base58Type.PUBKEY_ADDRESS && decoded.Length == 20 + prefixLength)
						return new BitcoinPubKeyAddress(new KeyId(decoded.Skip(prefixLength).ToArray()), this);
					if (type is Base58Type.SCRIPT_ADDRESS && decoded.Length == 20 + prefixLength)
						return new BitcoinScriptAddress(new ScriptId(decoded.Skip(prefixLength).ToArray()), this);
					return CreateBase58Data(type, base58);
				}
				catch (FormatException) { }
			}
			return null;
		}


		internal NetworkStringParser NetworkStringParser
		{
			get;
			set;
		} = new NetworkStringParser();

		public TransactionBuilder CreateTransactionBuilder()
		{
			var builder = this.Consensus.ConsensusFactory.CreateTransactionBuilderCore2(this);
			return builder;
		}

		public TransactionBuilder CreateTransactionBuilder(int seed)
		{
			var builder = this.Consensus.ConsensusFactory.CreateTransactionBuilderCore2(this);
			builder.ShuffleRandom = new Random(seed);
			return builder;
		}

		public Base58CheckEncoder GetBase58CheckEncoder()
		{
			return NetworkStringParser.GetBase58CheckEncoder();
		}

		private IBase58Data CreateBase58Data(Base58Type type, string base58)
		{
			if (type == Base58Type.EXT_PUBLIC_KEY)
				return CreateBitcoinExtPubKey(base58);
			if (type == Base58Type.EXT_SECRET_KEY)
				return CreateBitcoinExtKey(base58);
			if (type == Base58Type.PUBKEY_ADDRESS)
				return new BitcoinPubKeyAddress(base58, this);
			if (type == Base58Type.SCRIPT_ADDRESS)
				return CreateBitcoinScriptAddress(base58);
			if (type == Base58Type.SECRET_KEY)
				return CreateBitcoinSecret(base58);
			if (type == Base58Type.CONFIRMATION_CODE)
				return CreateConfirmationCode(base58);
			if (type == Base58Type.ENCRYPTED_SECRET_KEY_EC)
				return CreateEncryptedKeyEC(base58);
			if (type == Base58Type.ENCRYPTED_SECRET_KEY_NO_EC)
				return CreateEncryptedKeyNoEC(base58);
			if (type == Base58Type.PASSPHRASE_CODE)
				return CreatePassphraseCode(base58);
			if (type == Base58Type.ASSET_ID)
				return CreateAssetId(base58);
			if (type == Base58Type.COLORED_ADDRESS)
				return CreateColoredAddress(base58);
			throw new NotSupportedException("Invalid Base58Data type : " + type.ToString());
		}

		//private BitcoinWitScriptAddress CreateWitScriptAddress(string base58)
		//{
		//	return new BitcoinWitScriptAddress(base58, this);
		//}

		//private BitcoinWitPubKeyAddress CreateWitPubKeyAddress(string base58)
		//{
		//	return new BitcoinWitPubKeyAddress(base58, this);
		//}

		private BitcoinColoredAddress CreateColoredAddress(string base58)
		{
			return new BitcoinColoredAddress(base58, this);
		}

		public NBitcoin.OpenAsset.BitcoinAssetId CreateAssetId(string base58)
		{
			return new NBitcoin.OpenAsset.BitcoinAssetId(base58, this);
		}

		private BitcoinPassphraseCode CreatePassphraseCode(string base58)
		{
			return new BitcoinPassphraseCode(base58, this);
		}

		private BitcoinEncryptedSecretNoEC CreateEncryptedKeyNoEC(string base58)
		{
			return new BitcoinEncryptedSecretNoEC(base58, this);
		}

		private BitcoinEncryptedSecretEC CreateEncryptedKeyEC(string base58)
		{
			return new BitcoinEncryptedSecretEC(base58, this);
		}

		private Base58Data CreateConfirmationCode(string base58)
		{
			return new BitcoinConfirmationCode(base58, this);
		}

		private Base58Data CreateBitcoinExtPubKey(string base58)
		{
			return new BitcoinExtPubKey(base58, this);
		}


		public BitcoinExtKey CreateBitcoinExtKey(ExtKey key)
		{
			return new BitcoinExtKey(key, this);
		}

		public BitcoinExtPubKey CreateBitcoinExtPubKey(ExtPubKey pubkey)
		{
			return new BitcoinExtPubKey(pubkey, this);
		}

		public BitcoinExtKey CreateBitcoinExtKey(string base58)
		{
			return new BitcoinExtKey(base58, this);
		}

		public override string ToString()
		{
			return name;
		}

		public Block GetGenesis()
		{
			var block = Consensus.ConsensusFactory.CreateBlock();
			block.ReadWrite(_GenesisBytes, Consensus.ConsensusFactory);
			return block;
		}


		public uint256 GenesisHash
		{
			get
			{
				return consensus.HashGenesisBlock;
			}
		}

		public static IEnumerable<Network> GetNetworks()
		{
			if (_OtherNetworks.Count != 0)
			{
				List<Network> others = new List<Network>();
				lock (_OtherNetworks)
				{
					others = _OtherNetworks.ToList();
				}
				foreach (var network in others)
				{
					yield return network;
				}
			}
		}

		/// <summary>
		/// Get network from name
		/// </summary>
		/// <param name="name">main,mainnet,testnet,test,testnet3,reg,regtest,sig,signet</param>
		/// <returns>The network or null of the name does not match any network</returns>
		public static Network? GetNetwork(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			name = name.ToLowerInvariant();
			return _OtherAliases.TryGet(name);
		}

		public BitcoinSecret CreateBitcoinSecret(Key key)
		{
			return new BitcoinSecret(key, this);
		}
		public BitcoinPubKeyAddress CreateBitcoinAddress(KeyId dest)
		{
			if (dest == null)
				throw new ArgumentNullException(nameof(dest));
			return NetworkStringParser.CreateP2PKH(dest, this);
		}

		private BitcoinScriptAddress CreateBitcoinScriptAddress(ScriptId scriptId)
		{
			return NetworkStringParser.CreateP2SH(scriptId, this);
		}

		public Message ParseMessage(byte[] bytes, uint? version = null)
		{
			BitcoinStream bstream = new BitcoinStream(bytes);
			bstream.ConsensusFactory = this.Consensus.ConsensusFactory;
			Message message = new Message();
			using (bstream.ProtocolVersionScope(version))
			{
				message.ReadWrite(bstream);
			}
			if (message.Magic != magic)
				throw new FormatException("Unexpected magic field in the message");
			return message;
		}

#if !NOSOCKET
		public IEnumerable<NetworkAddress> SeedNodes
		{
			get
			{
				return this.vFixedSeeds;
			}
		}
		public IEnumerable<DNSSeedData> DNSSeeds
		{
			get
			{
				return this.vSeeds;
			}
		}
#endif
		readonly byte[] _MagicBytes;
		public byte[] MagicBytes
		{
			get
			{
				return _MagicBytes;
			}
		}
		public uint Magic
		{
			get
			{
				return magic;
			}
		}

		public Money GetReward(int nHeight)
		{
			long nSubsidy = new Money(50 * Money.COIN);
			int halvings = nHeight / consensus.SubsidyHalvingInterval;

			// Force block reward to zero when right shift is undefined.
			if (halvings >= 64)
				return Money.Zero;

			// Subsidy is cut in half every 210,000 blocks which will occur approximately every 4 years.
			nSubsidy >>= halvings;

			return new Money(nSubsidy);
		}

		public bool ReadMagic(Stream stream, CancellationToken cancellation, bool throwIfEOF = false)
		{
			byte[] bytes = new byte[1];
			for (int i = 0; i < MagicBytes.Length; i++)
			{
				i = Math.Max(0, i);
				cancellation.ThrowIfCancellationRequested();

				var read = stream.ReadEx(bytes, 0, bytes.Length, cancellation);
				if (read == 0)
					if (throwIfEOF)
						throw new EndOfStreamException("No more bytes to read");
					else
						return false;
				if (read != 1)
					i--;
				else if (_MagicBytes[i] != bytes[0])
					i = _MagicBytes[0] == bytes[0] ? 0 : -1;
			}
			return true;
		}
	}
}
#nullable disable
