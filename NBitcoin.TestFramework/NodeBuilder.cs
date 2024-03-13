using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	public enum RPCWalletType
	{
		Legacy,
		Descriptors
	}
	public enum CoreNodeState
	{
		Stopped,
		Starting,
		Running,
		Killed
	}
	public class NodeConfigParameters : Dictionary<string, string>
	{
		public void Import(NodeConfigParameters configParameters, bool overrides)
		{
			foreach (var kv in configParameters)
			{
				if (!ContainsKey(kv.Key))
					Add(kv.Key, kv.Value);
				else if (overrides)
				{
					Remove(kv.Key);
					Add(kv.Key, kv.Value);
				}
			}
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach (var kv in this)
				builder.AppendLine(kv.Key + "=" + kv.Value);
			return builder.ToString();
		}
	}

	public class NodeOSDownloadData
	{
		public string Archive
		{
			get; set;
		}
		public string DownloadLink
		{
			get; set;
		}
		public string Executable
		{
			get; set;
		}
		public string Hash
		{
			get; set;
		}
		public string CreateFolder
		{
			get;
			internal set;
		}
	}

	public partial class NodeDownloadData
	{
		public string Version
		{
			get; set;
		}

		public NodeOSDownloadData Linux
		{
			get; set;
		}

		public NodeOSDownloadData Mac
		{
			get; set;
		}

		public NodeOSDownloadData Windows
		{
			get; set;
		}
		public string RegtestFolderName { get; set; }

		public bool CreateWallet
		{
			get; set;
		}

		public string WalletExecutable { get; set; } = "bitcoin-wallet";
		public string GetWalletChainSpecifier = "-{0}";

		/// <summary>
		/// For blockchains that use an arbitrary chain (e.g. instead of main, testnet and regtest
		/// Elements can use chain=elementsregtest).
		/// </summary>
		public string Chain { get; set; } = "regtest";

		public NodeOSDownloadData GetCurrentOSDownloadData()
		{
			return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Windows :
				   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Linux :
				   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Mac :
				   throw new NotSupportedException();
		}
	}
	public class NodeBuilder : IDisposable
	{
		public static NodeBuilder Create(NodeDownloadData downloadData, Network network = null, [CallerMemberNameAttribute]string caller = null, bool showNodeConsole = false)
		{
			network = network ?? Network.RegTest;
			var isFilePath = downloadData.Version.Length >= 2 && downloadData.Version[1] == ':';
			var path = isFilePath ? downloadData.Version : EnsureDownloaded(downloadData);
			if (!Directory.Exists(caller))
				Directory.CreateDirectory(caller);
			return new NodeBuilder(caller, path) { Network = network, NodeImplementation = downloadData, ShowNodeConsole = showNodeConsole };
		}

		public static string EnsureDownloaded(NodeDownloadData downloadData)
		{
			if (!Directory.Exists("TestData"))
				Directory.CreateDirectory("TestData");

			var osDownloadData = downloadData.GetCurrentOSDownloadData();
			if (osDownloadData == null)
				throw new Exception("This platform does not support tests involving this crypto currency, DownloadData for this OS are unavailable");
			var bitcoind = Path.Combine("TestData", String.Format(osDownloadData.Executable, downloadData.Version));
			var zip = Path.Combine("TestData", String.Format(osDownloadData.Archive, downloadData.Version));
			if (File.Exists(bitcoind))
				return bitcoind;

			string url = String.Format(osDownloadData.DownloadLink, downloadData.Version);
			HttpClient client = new HttpClient();
			client.Timeout = TimeSpan.FromMinutes(10.0);
			var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
			CheckHash(osDownloadData, data);
			File.WriteAllBytes(zip, data);

			var extractDirectory = "TestData";
			if (osDownloadData.CreateFolder != null)
			{
				if (!Directory.Exists(osDownloadData.CreateFolder))
					Directory.CreateDirectory(osDownloadData.CreateFolder);
				extractDirectory = Path.Combine(extractDirectory, string.Format(osDownloadData.CreateFolder, downloadData.Version));
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				ZipFile.ExtractToDirectory(zip, extractDirectory);
			}
			else
			{
				Process.Start("tar", "-zxvf " + zip + " -C " + extractDirectory).WaitForExit();
			}
			File.Delete(zip);
			return bitcoind;
		}

		private static void CheckHash(NodeOSDownloadData osDownloadData, byte[] data)
		{
			var actual = Encoders.Hex.EncodeData(Hashes.SHA256(data));
			if (!actual.Equals(osDownloadData.Hash, StringComparison.OrdinalIgnoreCase))
				throw new Exception($"Hash of downloaded file does not match (Expected: {osDownloadData.Hash}, Actual: {actual})");
		}

		int last = 0;
		private string _Root;

		/// <summary>
		/// If true, the bitcoind process will set printtoconsole=1, default to false
		/// </summary>
		public bool ShowNodeConsole { set; get; }

		public string BitcoinD { get; }

		public List<CoreNode> Nodes { get; } = new List<CoreNode>();

		public NodeConfigParameters ConfigParameters { get; } = new NodeConfigParameters();

		public NodeBuilder(string root, string bitcoindPath)
		{
			this._Root = root;
			this.BitcoinD = bitcoindPath;
		}

		public bool CleanBeforeStartingNode
		{
			get; set;
		} = true;

		public Network Network
		{
			get;
			set;
		} = Network.RegTest;
		public NodeDownloadData NodeImplementation { get; private set; }
		public RPCWalletType? RPCWalletType { get; set; }
		public bool CreateWallet { get; set; } = true;

		public CoreNode CreateNode(bool start = false)
		{
			var child = Path.Combine(_Root, last.ToString());
			last++;
			var node = new CoreNode(child, this) { Network = Network };
			Nodes.Add(node);
			if (start)
				node.Start();
			return node;
		}

		public void StartAll()
		{
			Task.WaitAll(Nodes.Where(n => n.State == CoreNodeState.Stopped).Select(n => n.StartAsync()).ToArray());
		}

		public void Dispose()
		{
			foreach (var node in Nodes)
				node.Kill();
			foreach (var disposable in _Disposables)
				disposable.Dispose();
		}
		List<IDisposable> _Disposables = new List<IDisposable>();
		internal void AddDisposable(IDisposable group)
		{
			_Disposables.Add(group);
		}
	}

	public class CoreNode
	{
		private readonly NodeBuilder _Builder;
		private string _Folder;
		public string Folder
		{
			get
			{
				return _Folder;
			}
		}

		public IPEndPoint Endpoint
		{
			get
			{
				return new IPEndPoint(IPAddress.Parse("127.0.0.1"), ports[0]);
			}
		}

		public string Config
		{
			get
			{
				return _Config;
			}
		}

		private readonly NodeConfigParameters _ConfigParameters = new NodeConfigParameters();
		private string _Config;

		public NodeConfigParameters ConfigParameters
		{
			get
			{
				return _ConfigParameters;
			}
		}

		public CoreNode(string folder, NodeBuilder builder)
		{
			this._Builder = builder;
			this._Folder = folder;
			_State = CoreNodeState.Stopped;

			dataDir = Path.Combine(folder, "data");
			var pass = Hashes.DoubleSHA256(Encoding.UTF8.GetBytes(folder)).ToString();
			creds = new NetworkCredential(pass, pass);
			_Config = Path.Combine(dataDir, "bitcoin.conf");
			ConfigParameters.Import(builder.ConfigParameters, true);
			ports = new int[2];

			if (builder.CleanBeforeStartingNode && File.Exists(_Config))
			{
				var oldCreds = ExtractCreds(File.ReadAllText(_Config));
				CookieAuth = oldCreds == null;
				ExtractPorts(ports, File.ReadAllText(_Config));

				try
				{

					this.CreateRPCClient().Stop();
				}
				catch
				{
					try
					{
						CleanFolder();
					}
					catch
					{
						throw new InvalidOperationException("A running instance of bitcoind of a previous run prevent this test from starting. Please close bitcoind process manually and restart the test.");
					}
				}
				CancellationTokenSource cts = new CancellationTokenSource();
				cts.CancelAfter(10000);
				while (!cts.IsCancellationRequested && Directory.Exists(_Folder))
				{
					try
					{
						CleanFolder();
						break;
					}
					catch { }
					Thread.Sleep(100);
				}
				if (cts.IsCancellationRequested)
					throw new InvalidOperationException("You seem to have a running node from a previous test, please kill the process and restart the test.");
			}

			CookieAuth = NodeImplementation.SupportCookieFile;
			Directory.CreateDirectory(folder);
			Directory.CreateDirectory(dataDir);
			FindPorts(ports);
		}

		public string GetRPCAuth()
		{
			if (!CookieAuth)
				return creds.UserName + ":" + creds.Password;
			else
				return "cookiefile=" + Path.Combine(dataDir, this._Builder.NodeImplementation.RegtestFolderName ?? "regtest", ".cookie");
		}

		private void ExtractPorts(int[] ports, string config)
		{
			var p = Regex.Match(config, "rpcport=(.*)");
			ports[1] = int.Parse(p.Groups[1].Value.Trim());
		}

		private NetworkCredential ExtractCreds(string config)
		{
			var user = Regex.Match(config, "rpcuser=(.*)");
			if (!user.Success)
				return null;
			var pass = Regex.Match(config, "rpcpassword=(.*)");
			return new NetworkCredential(user.Groups[1].Value.Trim(), pass.Groups[1].Value.Trim());
		}

		public Network Network
		{
			get; set;
		} = Network.RegTest;

		private void CleanFolder()
		{
			try
			{
				Directory.Delete(_Folder, true);
			}
			catch (DirectoryNotFoundException) { }
		}
#if !NOSOCKET
		public void Sync(CoreNode node, bool keepConnection = false)
		{
			var rpc = CreateRPCClient();
			var rpc1 = node.CreateRPCClient();
			rpc.AddNode(node.Endpoint, true);
			while (rpc.GetBestBlockHash() != rpc1.GetBestBlockHash())
			{
				Task.Delay(200).GetAwaiter().GetResult();
			}
			if (!keepConnection)
				rpc.RemoveNode(node.Endpoint);
		}
#endif
		private CoreNodeState _State;
		public CoreNodeState State
		{
			get
			{
				return _State;
			}
		}

		int[] ports;

		public int ProtocolPort
		{
			get
			{
				return ports[0];
			}
		}
		public void Start()
		{
			StartAsync().Wait();
		}

		NetworkCredential creds;
		public NetworkCredential RPCCredentials
		{
			get
			{
				if (CookieAuth)
					throw new InvalidOperationException("CookieAuth should be false");
				return creds;
			}
			set
			{
				if (CookieAuth)
					throw new InvalidOperationException("CookieAuth should be false");
				creds = value;
			}
		}
		public RPCClient CreateRPCClient()
		{
			return new RPCClient(GetRPCAuth(), RPCUri, Network);
		}

		public Uri RPCUri
		{
			get
			{
				return new Uri("http://127.0.0.1:" + ports[1].ToString() + "/");
			}
		}

		public IPEndPoint NodeEndpoint
		{
			get
			{
				return new IPEndPoint(IPAddress.Parse("127.0.0.1"), ports[0]);
			}
		}

		public RestClient CreateRESTClient()
		{
			return new RestClient(new Uri("http://127.0.0.1:" + ports[1].ToString() + "/"));
		}
#if !NOSOCKET
		public Node CreateNodeClient()
		{
			return Node.Connect(Network, NodeEndpoint);
		}
		public Node CreateNodeClient(NodeConnectionParameters parameters)
		{
			return Node.Connect(Network, "127.0.0.1:" + ports[0].ToString(), parameters);
		}
#endif

		/// <summary>
		/// Nodes connecting to this node will be whitelisted (default: false)
		/// </summary>
		public bool WhiteBind
		{
			get; set;
		}

		NodeDownloadData _NodeImplementation;
		public NodeDownloadData NodeImplementation
		{
			get
			{
				return _NodeImplementation ?? this._Builder.NodeImplementation;
			}
			set
			{
				_NodeImplementation = value;
			}
		}

		public async Task StartAsync()
		{
			NodeConfigParameters config = new NodeConfigParameters();
			StringBuilder configStr = new StringBuilder();
			if (String.IsNullOrEmpty(NodeImplementation.Chain))
			{
				configStr.AppendLine("regtest=1");
			}
			else
			{
				configStr.AppendLine($"chain={NodeImplementation.Chain}");
			}
			if (NodeImplementation.UseSectionInConfigFile)
			{
				if (String.IsNullOrEmpty(NodeImplementation.Chain))
				{
					configStr.AppendLine("[regtest]");
				}
				else
				{
					configStr.AppendLine($"[{NodeImplementation.Chain}]");
				}
			}
			if (CreateWallet)
				config.Add("wallet", "wallet.dat");

			config.Add("rest", "1");
			config.Add("server", "1");
			config.Add("txindex", "1");
			config.Add("peerbloomfilters", "1");
			// Somehow got problems on windows with it time to time...
			//config.Add("blockfilterindex", "1");
			if (!CookieAuth)
			{
				config.Add("rpcuser", creds.UserName);
				config.Add("rpcpassword", creds.Password);
			}
			if (!WhiteBind)
				config.Add("port", ports[0].ToString());
			else
				config.Add("whitebind", "127.0.0.1:" + ports[0].ToString());
			config.Add("rpcport", ports[1].ToString());
			config.Add("printtoconsole", _Builder.ShowNodeConsole ? "1" : "0");
			config.Add("keypool", "10");
			config.Add("fallbackfee", "0.0002"); // https://github.com/bitcoin/bitcoin/pull/16524
			config.Import(ConfigParameters, true);
			configStr.AppendLine(config.ToString());
			if (NodeImplementation.AdditionalRegtestConfig != null)
				configStr.AppendLine(NodeImplementation.AdditionalRegtestConfig);
			File.WriteAllText(_Config, configStr.ToString());
			
			await Run();
		}

		private async Task Run()
		{
			lock (l)
			{
				if (CreateWallet)
					CreateDefaultWallet();

				string appPath = new FileInfo(this._Builder.BitcoinD).FullName;
				string args = "-conf=bitcoin.conf" + " -datadir=" + dataDir + " -debug=net";

				if (_Builder.ShowNodeConsole)
				{
					ProcessStartInfo info = new ProcessStartInfo(appPath, args);
					info.UseShellExecute = true;
					_Process = Process.Start(info);
				}
				else
				{
					_Process = Process.Start(appPath, args);
				}

				_State = CoreNodeState.Starting;
			}
			while (true)
			{
				try
				{
					await CreateRPCClient().GetBlockHashAsync(0).ConfigureAwait(false);
					_State = CoreNodeState.Running;
					break;
				}
				catch { }
				if (_Process == null || _Process.HasExited)
					break;
			}
		}

		public RPCWalletType? RPCWalletType { get; set; }

		private void CreateDefaultWallet()
		{
			var walletToolPath = Path.Combine(Path.GetDirectoryName(this._Builder.BitcoinD), _Builder.NodeImplementation.WalletExecutable);

			var walletType = (RPCWalletType ?? this._Builder.RPCWalletType) switch
			{
				Tests.RPCWalletType.Descriptors => " -descriptors",
				Tests.RPCWalletType.Legacy => " -legacy",
				_ => string.Empty
			};

			retry:
			string walletToolArgs = $"{string.Format(_Builder.NodeImplementation.GetWalletChainSpecifier, _Builder.NodeImplementation.Chain)} -wallet=\"wallet.dat\"{walletType} -datadir=\"{dataDir}\" create";

			var info = new ProcessStartInfo(walletToolPath, walletToolArgs)
			{
				UseShellExecute = _Builder.ShowNodeConsole
			};
			if (!_Builder.ShowNodeConsole)
			{
				info.RedirectStandardError = true;
				info.RedirectStandardOutput = true;
			}
			using (var walletToolProcess = Process.Start(info))
			{ 
				walletToolProcess.WaitForExit();
				// Some doesn't support this
				if (walletToolProcess.ExitCode != 0 && walletType != string.Empty)
				{
					walletType = string.Empty;
					goto retry;
				}
			}
		}

		Process _Process;
		private readonly string dataDir;

		private void FindPorts(int[] ports)
		{
			int i = 0;
			while (i < ports.Length)
			{
				var port = RandomUtils.GetUInt32() % 4000;
				port = port + 10000;
				if (ports.Any(p => p == port))
					continue;
				try
				{
					TcpListener l = new TcpListener(IPAddress.Loopback, (int)port);
					l.Start();
					l.Stop();
					ports[i] = (int)port;
					i++;
				}
				catch (SocketException) { }
			}
		}

		public uint256[] Generate(int blockCount)
		{
			uint256[] blockIds = new uint256[blockCount];
			int generated = 0;
			while (generated < blockCount)
			{
				foreach (var id in CreateRPCClient().Generate(blockCount - generated))
				{
					blockIds[generated++] = id;
				}
			}
			return blockIds;
		}

		public void Broadcast(params Transaction[] transactions)
		{
			var rpc = CreateRPCClient();
			var batch = rpc.PrepareBatch();
			foreach (var tx in transactions)
			{
				batch.SendRawTransactionAsync(tx);
			}
			batch.SendBatch();
		}

		object l = new object();
		public void Kill(bool cleanFolder = false)
		{
			lock (l)
			{
				if (_Process != null && !_Process.HasExited)
				{
					_Process.Kill();
					_Process.WaitForExit();
				}
				_State = CoreNodeState.Killed;
				if (cleanFolder)
					CleanFolder();
			}
		}

		public void WaitForExit()
		{
			if (_Process != null && !_Process.HasExited)
			{
				_Process.WaitForExit();
			}
		}

		public BitcoinSecret MinerSecret
		{
			get;
			private set;
		}

		public bool CookieAuth
		{
			get;
			set;
		} = true;

		bool? _CreateWallet;
		public bool CreateWallet
		{
			get
			{
				if (!NodeImplementation.CreateWallet)
					return false;
				return _CreateWallet ?? _Builder.CreateWallet;
			}
			set
			{
				_CreateWallet = value;
			}
		}

		class TransactionNode
		{
			public TransactionNode(Transaction tx)
			{
				Transaction = tx;
				Hash = tx.GetHash();
			}
			public uint256 Hash = null;
			public Transaction Transaction = null;
			public List<TransactionNode> DependsOn = new List<TransactionNode>();
		}

		public void Restart()
		{
			Kill(false);
			Run().GetAwaiter().GetResult();
		}
	}
}
