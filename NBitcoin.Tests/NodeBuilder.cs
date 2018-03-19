#if !NOSOCKET
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.InteropServices;
using NBitcoin.Crypto;

namespace NBitcoin.Tests
{
	public enum CoreNodeState
	{
		Stopped,
		Starting,
		Running,
		Killed
	}
	public class NodeConfigParameters : IEnumerable<KeyValuePair<string,string>>
	{
		public void Import(NodeConfigParameters configParameters)
		{
			var toAdd = new List<KeyValuePair<string, string>>();
			foreach(var kv in configParameters)
			{
				if(!ContainsKey(kv.Key))
					toAdd.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
			}

			foreach(var add in toAdd)
			{
				Add(add.Key, add.Value);
			}
		}

		public bool ContainsKey(string key)
		{
			return _KValues.Any(k => k.Key == key);
		}

		List<KeyValuePair<string, string>> _KValues = new List<KeyValuePair<string, string>>();
		public void Add(string key, string value)
		{
			_KValues.Add(new KeyValuePair<string, string>(key, value));
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach(var kv in this)
				builder.AppendLine(kv.Key + "=" + kv.Value);
			return builder.ToString();
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return _KValues.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
	public class NodeBuilder : IDisposable
	{
		public static NodeBuilder Create([CallerMemberNameAttribute]string caller = null, string version = "0.15.1")
		{
			version = version ?? "0.15.1";
			var path = EnsureDownloaded(version);

			try
			{
				Directory.Delete(caller, true);
			}
			catch(DirectoryNotFoundException)
			{
			}
			Directory.CreateDirectory(caller);
			return new NodeBuilder(caller, path);
		}

		internal static string EnsureDownloaded(string version)
		{
			//is a file
			if (version.Length >= 2 && version[1] == ':')
			{
				return version;
			}

			var dataDir = ".";
			string zip;
			string bitcoind;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				bitcoind = Path.Combine(dataDir,$"bitcoin-{version}", "bin", "bitcoind.exe");
				if (File.Exists(bitcoind))
					return bitcoind;
				var environment = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "win64" : "win32";

				zip = Path.Combine(dataDir, $"bitcoin-{version}-{environment}.zip");
				string url = string.Format("https://bitcoin.org/bin/bitcoin-core-{0}/" + Path.GetFileName(zip), version);
				HttpClient client = new HttpClient();
				client.Timeout = TimeSpan.FromMinutes(10.0);
				var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();

				VerifyBinary(zip, data);
				File.WriteAllBytes(zip, data);
				ZipFile.ExtractToDirectory(zip, new FileInfo(zip).Directory.FullName);
			}
			else
			{
				bitcoind = Path.Combine(dataDir, $"bitcoin-{version}", "bin", "bitcoind");
				if (File.Exists(bitcoind))
					return bitcoind;

				zip = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
					Path.Combine(dataDir, $"bitcoin-{version}-x86_64-linux-gnu.tar.gz")
					: Path.Combine(dataDir, $"bitcoin-{version}-osx64.tar.gz");

				string url = string.Format("https://bitcoin.org/bin/bitcoin-core-{0}/" + Path.GetFileName(zip), version);
				HttpClient client = new HttpClient();
				client.Timeout = TimeSpan.FromMinutes(10.0);
				var data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();

				VerifyBinary(zip, data);
				File.WriteAllBytes(zip, data);
				Process.Start("tar", "-zxvf " + zip + " -C " + dataDir).WaitForExit();
			}
			File.Delete(zip);
			return bitcoind;
		}		

		private static void VerifyBinary(string compressedFilePath, byte[] bytes)
		{
			var fileLines = File.ReadAllLines("data/SHA256SUMS.asc");
			var hashes = fileLines
				.Skip(3)
				.TakeWhile(x=> !x.StartsWith("-----")) 
				.Select(x=>x.Split(new string[]{"  "}, StringSplitOptions.None))
				.ToDictionary(x=> x[1], x=>x[0]);

			var fileName = Path.GetFileName(compressedFilePath);
			var isValid = hashes.ContainsKey(fileName) && hashes[fileName] == Encoders.Hex.EncodeData(Hashes.SHA256(bytes));

			if(!isValid)
			{
				throw new InvalidDataException("Downloaded Bitcoin Core node couldn't be verified");
			}
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

		public CoreNode CreateNode(bool start = false)
		{
			var child = Path.Combine(_Root, last.ToString());
			last++;
			try
			{
				Directory.Delete(child, true);
			}
			catch(DirectoryNotFoundException)
			{
			}
			var node = new CoreNode(child, this);
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
			CleanFolder();
			Directory.CreateDirectory(folder);
			dataDir = Path.Combine(folder, "data");
			Directory.CreateDirectory(dataDir);
			var pass = Encoders.Hex.EncodeData(RandomUtils.GetBytes(20));
			creds = new NetworkCredential(pass, pass);
			_Config = Path.Combine(dataDir, "bitcoin.conf");
			ConfigParameters.Import(builder.ConfigParameters);
			ports = new int[2];
			FindPorts(ports);
		}

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
			return new RPCClient(GetRPCAuth(), new Uri("http://127.0.0.1:" + ports[1].ToString() + "/"), Network.RegTest);
		}

		public RestClient CreateRESTClient()
		{
			return new RestClient(new Uri("http://127.0.0.1:" + ports[1].ToString() + "/"));
		}
#if !NOSOCKET
		public Node CreateNodeClient()
		{
			return Node.Connect(Network.RegTest, "127.0.0.1:" + ports[0].ToString());
		}
		public Node CreateNodeClient(NodeConnectionParameters parameters)
		{
			return Node.Connect(Network.RegTest, "127.0.0.1:" + ports[0].ToString(), parameters);
		}
#endif

		string GetRPCAuth()
		{
			if(!CookieAuth)
				return creds.UserName + ":" + creds.Password;
			else
				return "cookiefile=" + Path.Combine(dataDir, "regtest", ".cookie");
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
			config.Add("port", ports[0].ToString());
			config.Add("rpcport", ports[1].ToString());
			config.Add("printtoconsole", "1");
			config.Add("keypool", "10");
			config.Add("whitebind", "127.0.0.1:" + ports[0].ToString());
			config.Import(ConfigParameters);
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

		public void Restart()
		{
			Kill(false);
			Run().GetAwaiter().GetResult();
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
			internal set;
		}

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
					Block block = new Block();
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
					while(!block.CheckProofOfWork(node.Network.Consensus))
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

		public void FindBlock(int blockCount = 1, bool includeMempool = true)
		{
			SelectMempoolTransactions();
			Generate(blockCount, includeMempool);
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
	}
}
#endif