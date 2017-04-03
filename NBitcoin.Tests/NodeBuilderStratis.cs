using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Payloads;
using NBitcoin.RPC;

namespace NBitcoin.Tests
{

		// Require a stratis node running with the following stratis.conf file
		//server=1
		//rpcuser=rpcuser
		//rpcpassword = rpcpassword
		//rpcallowip=*
		//txindex=1 

	public class NodeBuilderStratis : IDisposable
	{
		public static NodeBuilderStratis Create([CallerMemberName]string caller = null, string version = "0.12.1")
		{
			//version = version ?? "0.12.1";
			var path = string.Empty;//EnsureDownloaded(version);
			//try
			//{
			//	Directory.Delete(caller, true);
			//}
			//catch(DirectoryNotFoundException)
			//{
			//}
			//Directory.CreateDirectory(caller);
			return new NodeBuilderStratis(caller, path);
		}

		//private static string EnsureDownloaded(string version)
		//{
		//	//is a file
		//	if(version.Length >= 2 && version[1] == ':')
		//	{
		//		return version;
		//	}

		//	var bitcoind = String.Format("bitcoin-{0}/bin/bitcoind.exe", version);
		//	if(File.Exists(bitcoind))
		//		return bitcoind;
		//	var zip = String.Format("bitcoin-{0}-win32.zip", version);
		//	string url = String.Format("https://bitcoin.org/bin/bitcoin-core-{0}/" + zip, version);
		//	WebClient client = new WebClient();
		//	client.DownloadFile(url, zip);
		//	ZipFile.ExtractToDirectory(zip, new FileInfo(zip).Directory.FullName);
		//	return bitcoind;
		//}

		int last = 0;
		private string _Root;
		private string _Bitcoind;
		public NodeBuilderStratis(string root, string bitcoindPath)
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


		private readonly List<CoreNodeStratis> _Nodes = new List<CoreNodeStratis>();
		public List<CoreNodeStratis> Nodes
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

		public CoreNodeStratis CreateNode(bool start = false)
		{
			var child = Path.Combine(_Root, last.ToString());
			last++;
			//try
			//{
			//	Directory.Delete(child, true);
			//}
			//catch(DirectoryNotFoundException)
			//{
			//}
			var node = new CoreNodeStratis(child, this);
			Nodes.Add(node);
			if(start)
				node.Start();
			return node;
		}

		public void StartAll()
		{
			if (!Process.GetProcesses().Any(p => p.ProcessName.Contains("stratis")))
				throw new NotSupportedException("stratis node is not running");

			//Task.WaitAll(Nodes.Where(n => n.State == CoreNodeState.Stopped).Select(n => n.StartAsync()).ToArray());
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

	public class CoreNodeStratis
	{
		private readonly NodeBuilderStratis _Builder;
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

		public CoreNodeStratis(string folder, NodeBuilderStratis builder)
		{
			this._Builder = builder;
			this._Folder = folder;
			//_State = CoreNodeState.Stopped;
			//CleanFolder();
			//Directory.CreateDirectory(folder);
			//dataDir = Path.Combine(folder, "data");
			//Directory.CreateDirectory(dataDir);
			//var pass = Encoders.Hex.EncodeData(RandomUtils.GetBytes(20));
			//creds = new NetworkCredential(pass, pass);
			//_Config = Path.Combine(dataDir, "bitcoin.conf");
			//ConfigParameters.Import(builder.ConfigParameters);
			ports = new int[2];
			FindPorts(ports);
			//ports[1] = Network.StratisMain.RPCPort;
			//ports[0] = Network.StratisMain.DefaultPort;
		}

		private void CleanFolder()
		{
			try
			{
				//Directory.Delete(_Folder, true);
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
			//return new RPCClient(creds, new Uri("http://127.0.0.1:" + ports[1].ToString() + "/"), Network.RegTest);
			// currently only use mainnet
			// credentials should be set in advance
			return new RPCClient(new NetworkCredential("rpcuser", "rpcpassword"), new Uri("http://127.0.0.1:" + Network.StratisMain.RPCPort + "/"), Network.StratisMain);
		}

		public RestClient CreateRESTClient()
		{
			return new RestClient(new Uri("http://127.0.0.1:" + ports[1].ToString() + "/"));
		}
#if !NOSOCKET
		public Node CreateNodeClient()
		{
			return Node.Connect(Network.StratisMain, "127.0.0.1:" + Network.StratisMain.DefaultPort); //ports[0].ToString());
		}
		public Node CreateNodeClient(NodeConnectionParameters parameters)
		{
			return Node.Connect(Network.RegTest, "127.0.0.1:" + ports[0].ToString(), parameters);
		}
#endif

		public async Task StartAsync()
		{
			NodeConfigParameters config = new NodeConfigParameters();
			config.Add("regtest", "1");
			config.Add("rest", "1");
			config.Add("server", "1");
			config.Add("txindex", "1");
			config.Add("rpcuser", creds.UserName);
			config.Add("rpcpassword", creds.Password);
			config.Add("port", ports[0].ToString());
			config.Add("rpcport", ports[1].ToString());
			config.Add("printtoconsole", "1");
			config.Add("keypool", "10");
			config.Import(ConfigParameters);
			File.WriteAllText(_Config, config.ToString());
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
