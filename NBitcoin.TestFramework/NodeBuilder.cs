using NBitcoin;
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
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
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
			foreach(var kv in configParameters)
			{
				if(!ContainsKey(kv.Key))
					Add(kv.Key, kv.Value);
				else if(overrides)
				{
					Remove(kv.Key);
					Add(kv.Key, kv.Value);
				}
			}
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach(var kv in this)
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
		public static NodeBuilder Create(NodeDownloadData downloadData, Network network = null, [CallerMemberNameAttribute]string caller = null)
		{
			network = network ?? Network.RegTest;
			var isFilePath = downloadData.Version.Length >= 2 && downloadData.Version[1] == ':';
			var path = isFilePath ? downloadData.Version : EnsureDownloaded(downloadData);
			if(!Directory.Exists(caller))
				Directory.CreateDirectory(caller);
			return new NodeBuilder(caller, path) { Network = network };
		}

		private static string EnsureDownloaded(NodeDownloadData downloadData)
		{
			if(!Directory.Exists("TestData"))
				Directory.CreateDirectory("TestData");

			string zip;
			string bitcoind;

			var osDownloadData = downloadData.GetCurrentOSDownloadData();
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				bitcoind = "TestData/" + String.Format(osDownloadData.Executable, downloadData.Version);
				if(File.Exists(bitcoind))
					return bitcoind;
				zip = "TestData/" + String.Format(osDownloadData.Archive, downloadData.Version);
				string url = String.Format(osDownloadData.DownloadLink, downloadData.Version);
				HttpClient client = new HttpClient();
				client.Timeout = TimeSpan.FromMinutes(10.0);
				var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
				CheckHash(osDownloadData, data);
				File.WriteAllBytes(zip, data);
				ZipFile.ExtractToDirectory(zip, new FileInfo(zip).Directory.FullName);
			}
			else
			{
				bitcoind = "TestData/" + String.Format(osDownloadData.Executable, downloadData.Version);
				if(File.Exists(bitcoind))
					return bitcoind;

				zip = "TestData/" + String.Format(osDownloadData.Archive, downloadData.Version);

				string url = String.Format(osDownloadData.DownloadLink, downloadData.Version);
				HttpClient client = new HttpClient();
				client.Timeout = TimeSpan.FromMinutes(10.0);
				var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
				CheckHash(osDownloadData, data);
				File.WriteAllBytes(zip, data);
				Process.Start("tar", "-zxvf " + zip + " -C TestData").WaitForExit();
			}
			File.Delete(zip);
			return bitcoind;
		}

		private static void CheckHash(NodeOSDownloadData osDownloadData, byte[] data)
		{
			if(Encoders.Hex.EncodeData(Hashes.SHA256(data)) != osDownloadData.Hash)
				throw new Exception("Hash of downloaded file does not match");
		}

		int last = 0;
		private string _Root;
		private string _Bitcoind;
		public NodeBuilder(string root, string bitcoindPath)
		{
			this._Root = root;
			this._Bitcoind = bitcoindPath;
		}

		public string BitcoinD
		{
			get
			{
				return _Bitcoind;
			}
		}

		public bool CleanBeforeStartingNode
		{
			get; set;
		} = true;


		private readonly List<CoreNode> _Nodes = new List<CoreNode>();
		public List<CoreNode> Nodes
		{
			get
			{
				return _Nodes;
			}
		}


		private readonly NodeConfigParameters _ConfigParameters = new NodeConfigParameters();
		public NodeConfigParameters ConfigParameters
		{
			get
			{
				return _ConfigParameters;
			}
		}

		public Network Network
		{
			get;
			set;
		} = Network.RegTest;

		public CoreNode CreateNode(bool start = false)
		{
			var child = Path.Combine(_Root, last.ToString());
			last++;
			var node = new CoreNode(child, this) { Network = Network };
			Nodes.Add(node);
			if(start)
				node.Start();
			return node;
		}

		public void StartAll()
		{
			Task.WaitAll(Nodes.Where(n => n.State == CoreNodeState.Stopped).Select(n => n.StartAsync()).ToArray());
		}

		public void Dispose()
		{
			foreach(var node in Nodes)
				node.Kill();
			foreach(var disposable in _Disposables)
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
			var pass = Hashes.Hash256(Encoding.UTF8.GetBytes(folder)).ToString();
			creds = new NetworkCredential(pass, pass);
			_Config = Path.Combine(dataDir, "bitcoin.conf");
			ConfigParameters.Import(builder.ConfigParameters, true);
			ports = new int[2];

			if(builder.CleanBeforeStartingNode && File.Exists(_Config))
			{
				var oldCreds = ExtractCreds(File.ReadAllText(_Config));
				CookieAuth = oldCreds == null;
				ExtractPorts(ports, File.ReadAllText(_Config));

				try
				{

					this.CreateRPCClient().SendCommand("stop");
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
				while(!cts.IsCancellationRequested && Directory.Exists(_Folder))
				{
					try
					{
						CleanFolder();
						break;
					}
					catch { }
					Thread.Sleep(100);
				}
				if(cts.IsCancellationRequested)
					throw new InvalidOperationException("You seem to have a running node from a previous test, please kill the process and restart the test.");
			}

			CookieAuth = true;
			Directory.CreateDirectory(folder);
			Directory.CreateDirectory(dataDir);
			FindPorts(ports);
		}

		public string GetRPCAuth()
		{
			if(!CookieAuth)
				return creds.UserName + ":" + creds.Password;
			else
				return "cookiefile=" + Path.Combine(dataDir, "regtest", ".cookie");
		}

		private void ExtractPorts(int[] ports, string config)
		{
			var p = Regex.Match(config, "rpcport=(.*)");
			ports[1] = int.Parse(p.Groups[1].Value.Trim());
		}

		private NetworkCredential ExtractCreds(string config)
		{
			var user = Regex.Match(config, "rpcuser=(.*)");
			if(!user.Success)
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
			catch(DirectoryNotFoundException) { }
		}
#if !NOSOCKET
		public void Sync(CoreNode node, bool keepConnection = false)
		{
			var rpc = CreateRPCClient();
			var rpc1 = node.CreateRPCClient();
			rpc.AddNode(node.Endpoint, true);
			while(rpc.GetBestBlockHash() != rpc1.GetBestBlockHash())
			{
				Thread.Sleep(200);
			}
			if(!keepConnection)
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

		readonly NetworkCredential creds;
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

		public async Task StartAsync()
		{
			NodeConfigParameters config = new NodeConfigParameters();
			config.Add("regtest", "1");
			config.Add("rest", "1");
			config.Add("server", "1");
			config.Add("txindex", "1");
			if(!CookieAuth)
			{
				config.Add("rpcuser", creds.UserName);
				config.Add("rpcpassword", creds.Password);
			}
			if(!WhiteBind)
				config.Add("port", ports[0].ToString());
			else
				config.Add("whitebind", "127.0.0.1:" + ports[0].ToString());
			config.Add("rpcport", ports[1].ToString());
			config.Add("printtoconsole", "1");
			config.Add("keypool", "10");
			config.Import(ConfigParameters, true);
			File.WriteAllText(_Config, config.ToString());
			await Run();
		}

		private async Task Run()
		{
			lock(l)
			{
				_Process = Process.Start(new FileInfo(this._Builder.BitcoinD).FullName, "-conf=bitcoin.conf" + " -datadir=" + dataDir + " -debug=net");
				_State = CoreNodeState.Starting;
			}
			while(true)
			{
				try
				{
					await CreateRPCClient().GetBlockHashAsync(0).ConfigureAwait(false);
					_State = CoreNodeState.Running;
					break;
				}
				catch { }
				if(_Process == null || _Process.HasExited)
					break;
			}
		}


		Process _Process;
		private readonly string dataDir;

		private void FindPorts(int[] ports)
		{
			int i = 0;
			while(i < ports.Length)
			{
				var port = RandomUtils.GetUInt32() % 4000;
				port = port + 10000;
				if(ports.Any(p => p == port))
					continue;
				try
				{
					TcpListener l = new TcpListener(IPAddress.Loopback, (int)port);
					l.Start();
					l.Stop();
					ports[i] = (int)port;
					i++;
				}
				catch(SocketException) { }
			}
		}

		List<Transaction> transactions = new List<Transaction>();
		HashSet<OutPoint> locked = new HashSet<OutPoint>();
		Money fee = Money.Coins(0.0001m);
		public Transaction GiveMoney(Script destination, Money amount, bool broadcast = true)
		{
			var rpc = CreateRPCClient();
			TransactionBuilder builder = new TransactionBuilder();
			builder.AddKeys(rpc.ListSecrets().OfType<ISecret>().ToArray());
			builder.AddCoins(rpc.ListUnspent().Where(c => !locked.Contains(c.OutPoint)).Select(c => c.AsCoin()));
			builder.Send(destination, amount);
			builder.SendFees(fee);
			builder.SetChange(GetFirstSecret(rpc));
			var tx = builder.BuildTransaction(true);
			foreach(var outpoint in tx.Inputs.Select(i => i.PrevOut))
			{
				locked.Add(outpoint);
			}
			if(broadcast)
				Broadcast(tx);
			else
				transactions.Add(tx);
			return tx;
		}

		public void Rollback(Transaction tx)
		{
			transactions.Remove(tx);
			foreach(var outpoint in tx.Inputs.Select(i => i.PrevOut))
			{
				locked.Remove(outpoint);
			}

		}

#if !NOSOCKET
		public void Broadcast(Transaction transaction)
		{
			using(var node = CreateNodeClient())
			{
				node.VersionHandshake();
				node.SendMessageAsync(new InvPayload(transaction));
				node.SendMessageAsync(new TxPayload(transaction));
				node.PingPong();
			}
		}
#else
        public void Broadcast(Transaction transaction)
        {
            var rpc = CreateRPCClient();
            rpc.SendRawTransaction(transaction);
        }
#endif
		public void SelectMempoolTransactions()
		{
			var rpc = CreateRPCClient();
			var txs = rpc.GetRawMempool();
			var tasks = txs.Select(t => rpc.GetRawTransactionAsync(t)).ToArray();
			Task.WaitAll(tasks);
			transactions.AddRange(tasks.Select(t => t.Result).ToArray());
		}

		public void Broadcast(Transaction[] transactions)
		{
			foreach(var tx in transactions)
				Broadcast(tx);
		}

		public void Split(Money amount, int parts)
		{
			var rpc = CreateRPCClient();
			TransactionBuilder builder = new TransactionBuilder();
			builder.AddKeys(rpc.ListSecrets().OfType<ISecret>().ToArray());
			builder.AddCoins(rpc.ListUnspent().Select(c => c.AsCoin()));
			var secret = GetFirstSecret(rpc);
			foreach(var part in (amount - fee).Split(parts))
			{
				builder.Send(secret, part);
			}
			builder.SendFees(fee);
			builder.SetChange(secret);
			var tx = builder.BuildTransaction(true);
			Broadcast(tx);
		}

		object l = new object();
		public void Kill(bool cleanFolder = true)
		{
			lock(l)
			{
				if(_Process != null && !_Process.HasExited)
				{
					_Process.Kill();
					_Process.WaitForExit();
				}
				_State = CoreNodeState.Killed;
				if(cleanFolder)
					CleanFolder();
			}
		}

		public void WaitForExit()
		{
			if(_Process != null && !_Process.HasExited)
			{
				_Process.WaitForExit();
			}
		}

		public DateTimeOffset? MockTime
		{
			get;
			set;
		}

		public void SetMinerSecret(BitcoinSecret secret)
		{
			CreateRPCClient().ImportPrivKey(secret);
			MinerSecret = secret;
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

		public Block[] Generate(int blockCount, bool includeUnbroadcasted = true, bool broadcast = true)
		{
			var rpc = CreateRPCClient();
			BitcoinSecret dest = GetFirstSecret(rpc);
			var bestBlock = rpc.GetBestBlockHash();
			ConcurrentChain chain = null;
			List<Block> blocks = new List<Block>();
			DateTimeOffset now = MockTime == null ? DateTimeOffset.UtcNow : MockTime.Value;
#if !NOSOCKET
			using(var node = CreateNodeClient())
			{

				node.VersionHandshake();
				chain = bestBlock == node.Network.GenesisHash ? new ConcurrentChain(node.Network) : node.GetChain();
				for(int i = 0; i < blockCount; i++)
				{
					uint nonce = 0;
					Block block = Network.Consensus.ConsensusFactory.CreateBlock();
					block.Header.HashPrevBlock = chain.Tip.HashBlock;
					block.Header.Bits = block.Header.GetWorkRequired(rpc.Network, chain.Tip);
					block.Header.UpdateTime(now, rpc.Network, chain.Tip);
					var coinbase = new Transaction();
					coinbase.AddInput(TxIn.CreateCoinbase(chain.Height + 1));
					coinbase.AddOutput(new TxOut(rpc.Network.GetReward(chain.Height + 1), dest.GetAddress()));
					block.AddTransaction(coinbase);
					if(includeUnbroadcasted)
					{
						transactions = Reorder(transactions);
						block.Transactions.AddRange(transactions);
						transactions.Clear();
					}
					block.UpdateMerkleRoot();
					while(!block.CheckProofOfWork())
						block.Header.Nonce = ++nonce;
					blocks.Add(block);
					chain.SetTip(block.Header);
				}
				if(broadcast)
					BroadcastBlocks(blocks.ToArray(), node);
			}
			return blocks.ToArray();
#endif
		}

		public void BroadcastBlocks(Block[] blocks)
		{
			using(var node = CreateNodeClient())
			{
				node.VersionHandshake();
				BroadcastBlocks(blocks, node);
			}
		}

		public void BroadcastBlocks(Block[] blocks, Node node)
		{
			Block lastSent = null;
			foreach(var block in blocks)
			{
				node.SendMessageAsync(new InvPayload(block));
				node.SendMessageAsync(new BlockPayload(block));
				lastSent = block;
			}
			node.PingPong();
		}

		public Block[] FindBlock(int blockCount = 1, bool includeMempool = true)
		{
			SelectMempoolTransactions();
			return Generate(blockCount, includeMempool);
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

		private List<Transaction> Reorder(List<Transaction> transactions)
		{
			if(transactions.Count == 0)
				return transactions;
			var result = new List<Transaction>();
			var dictionary = transactions.ToDictionary(t => t.GetHash(), t => new TransactionNode(t));
			foreach(var transaction in dictionary.Select(d => d.Value))
			{
				foreach(var input in transaction.Transaction.Inputs)
				{
					var node = dictionary.TryGet(input.PrevOut.Hash);
					if(node != null)
					{
						transaction.DependsOn.Add(node);
					}
				}
			}
			while(dictionary.Count != 0)
			{
				foreach(var node in dictionary.Select(d => d.Value).ToList())
				{
					foreach(var parent in node.DependsOn.ToList())
					{
						if(!dictionary.ContainsKey(parent.Hash))
							node.DependsOn.Remove(parent);
					}
					if(node.DependsOn.Count == 0)
					{
						result.Add(node.Transaction);
						dictionary.Remove(node.Hash);
					}
				}
			}
			return result;
		}

		private BitcoinSecret GetFirstSecret(RPCClient rpc)
		{
			if(MinerSecret != null)
				return MinerSecret;
			var dest = rpc.ListSecrets().FirstOrDefault();
			if(dest == null)
			{
				var address = rpc.GetNewAddress();
				dest = rpc.DumpPrivKey(address);
			}
			return dest;
		}

		public void Restart()
		{
			Kill(false);
			Run().GetAwaiter().GetResult();
		}
	}
}
