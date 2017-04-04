#if !NOJSONNET
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	/*
		Category			Name						Implemented 
		------------------ --------------------------- -----------------------
		------------------ Overall control/query calls 
		control			getinfo
		control			help
		control			stop

		------------------ P2P networking
		network			getnetworkinfo
		network			addnode					  Yes
		network			disconnectnode
		network			getaddednodeinfo			 Yes
		network			getconnectioncount
		network			getnettotals
		network			getpeerinfo				  Yes
		network			ping
		network			setban
		network			listbanned
		network			clearbanned

		------------------ Block chain and UTXO
		blockchain		 getblockchaininfo
		blockchain		 getbestblockhash			 Yes
		blockchain		 getblockcount				Yes
		blockchain		 getblock					 Yes
		blockchain		 getblockhash				 Yes
		blockchain		 getchaintips
		blockchain		 getdifficulty
		blockchain		 getmempoolinfo
		blockchain		 getrawmempool				Yes
		blockchain		 gettxout
		blockchain		 gettxoutproof
		blockchain		 verifytxoutproof
		blockchain		 gettxoutsetinfo
		blockchain		 verifychain

		------------------ Mining
		mining			 getblocktemplate
		mining			 getmininginfo
		mining			 getnetworkhashps
		mining			 prioritisetransaction
		mining			 submitblock

		------------------ Coin generation
		generating		 getgenerate
		generating		 setgenerate
		generating		 generate

		------------------ Raw transactions
		rawtransactions	createrawtransaction
		rawtransactions	decoderawtransaction
		rawtransactions	decodescript
		rawtransactions	getrawtransaction
		rawtransactions	sendrawtransaction
		rawtransactions	signrawtransaction
		rawtransactions	fundrawtransaction

		------------------ Utility functions
		util			   createmultisig
		util			   validateaddress
		util			   verifymessage
		util			   estimatefee				  Yes
		util			   estimatepriority			 Yes

		------------------ Not shown in help
		hidden			 invalidateblock
		hidden			 reconsiderblock
		hidden			 setmocktime
		hidden			 resendwallettransactions

		------------------ Wallet
		wallet			 addmultisigaddress
		wallet			 backupwallet				 Yes
		wallet			 dumpprivkey				  Yes
		wallet			 dumpwallet
		wallet			 encryptwallet
		wallet			 getaccountaddress			Yes
		wallet			 getaccount
		wallet			 getaddressesbyaccount
		wallet			 getbalance
		wallet			 getnewaddress
		wallet			 getrawchangeaddress
		wallet			 getreceivedbyaccount
		wallet			 getreceivedbyaddress
		wallet			 gettransaction
		wallet			 getunconfirmedbalance
		wallet			 getwalletinfo
		wallet			 importprivkey				Yes
		wallet			 importwallet
		wallet			 importaddress				Yes
		wallet			 keypoolrefill
		wallet			 listaccounts				 Yes
		wallet			 listaddressgroupings		 Yes
		wallet			 listlockunspent
		wallet			 listreceivedbyaccount
		wallet			 listreceivedbyaddress
		wallet			 listsinceblock
		wallet			 listtransactions
		wallet			 listunspent				  Yes
		wallet			 lockunspent				  Yes
		wallet			 move
		wallet			 sendfrom
		wallet			 sendmany
		wallet			 sendtoaddress
		wallet			 setaccount
		wallet			 settxfee
		wallet			 signmessage
		wallet			 walletlock
		wallet			 walletpassphrasechange
		wallet			 walletpassphrase
	*/
	public partial class RPCClient : IBlockRepository
	{
		private readonly string _Authentication;
		private readonly Uri _address;
		public Uri Address
		{
			get
			{
				return _address;
			}
		}
		private readonly Network _network;
		public Network Network
		{
			get
			{
				return _network;
			}
		}
#if !NOFILEIO
		/// <summary>
		/// Use default bitcoin parameters to configure a RPCClient.
		/// </summary>
		/// <param name="network">The network used by the node. Must not be null.</param>
		public RPCClient(Network network) : this(null as string, BuildUri(null, network.RPCPort), network)
		{
		}
#endif
		public RPCClient(NetworkCredential credentials, string host, Network network)
			: this(credentials, BuildUri(host, network.RPCPort), network)
		{
		}

		/// <summary>
		/// Create a new RPCClient instance
		/// </summary>
		/// <param name="authenticationString">username:password, the content of the .cookie file, or cookiefile=pathToCookieFile</param>
		/// <param name="hostOrUri"></param>
		/// <param name="network"></param>
		public RPCClient(string authenticationString, string hostOrUri, Network network)
			: this(authenticationString, BuildUri(hostOrUri, network.RPCPort), network)
		{
		}

		private static Uri BuildUri(string hostOrUri, int port)
		{
			if(hostOrUri != null)
			{
				hostOrUri = hostOrUri.Trim();
				try
				{
					if(hostOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
					   hostOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
						return new Uri(hostOrUri, UriKind.Absolute);
				}
				catch { }
			}
			hostOrUri = hostOrUri ?? "127.0.0.1";
			UriBuilder builder = new UriBuilder();
			builder.Host = hostOrUri;
			builder.Scheme = "http";
			builder.Port = port;
			return builder.Uri;
		}
		public RPCClient(NetworkCredential credentials, Uri address, Network network = null)
			: this(credentials == null ? null : (credentials.UserName + ":" + credentials.Password), address, network)
		{
		}

		/// <summary>
		/// Create a new RPCClient instance
		/// </summary>
		/// <param name="authenticationString">username:password or the content of the .cookie file or null to auto configure</param>
		/// <param name="address"></param>
		/// <param name="network"></param>
		public RPCClient(string authenticationString, Uri address, Network network = null)
		{
			authenticationString = string.IsNullOrWhiteSpace(authenticationString) ? null : authenticationString;
#if !NOFILEIO
			if(authenticationString != null)
			{
				if(authenticationString.StartsWith("cookiefile=", StringComparison.OrdinalIgnoreCase))
				{
					authenticationString = File.ReadAllText(authenticationString.Substring("cookiefile=".Length).Trim());
					if(!authenticationString.StartsWith("__cookie__:", StringComparison.OrdinalIgnoreCase))
						throw new ArgumentException("The authentication string to RPC is not provided and can't be inferred");
				}
			}
#endif

			authenticationString = authenticationString ?? GetAuthenticationString(network);
			if(authenticationString == null)
				throw new ArgumentException("The authentication string to RPC is not provided and can't be inferred");
			if(address == null && network == null)
				throw new ArgumentNullException("address");

			if(address != null && network == null)
			{
				network = Network.GetNetworks().FirstOrDefault(n => n.RPCPort == address.Port);
				if(network == null)
					throw new ArgumentNullException("network");
			}

			if(address == null && network != null)
			{
				address = new Uri("http://127.0.0.1:" + network.RPCPort + "/");
			}

			_Authentication = authenticationString;
			_address = address;
			_network = network;
		}

		private string GetAuthenticationString(Network network)
		{
#if !NOFILEIO
			if(network == null)
				return null;
			var home = Environment.GetEnvironmentVariable("HOME");
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");
			if(string.IsNullOrEmpty(home) && string.IsNullOrEmpty(localAppData))
				return null;
			string bitcoinFolder = null;
			if(string.IsNullOrEmpty(localAppData))
				bitcoinFolder = Path.Combine(home, ".bitcoin");
			else
				bitcoinFolder = Path.Combine(localAppData, "Bitcoin");
			if(network == Network.TestNet)
				bitcoinFolder = Path.Combine(bitcoinFolder, "testnet3");
			if(network == Network.RegTest)
				bitcoinFolder = Path.Combine(bitcoinFolder, "regtest");
			var cookiePath = Path.Combine(bitcoinFolder, ".cookie");
			try
			{
				return File.ReadAllText(cookiePath);
			}
			catch { return null; }
#else
			return null;
#endif
		}

		public RPCResponse SendCommand(RPCOperations commandName, params object[] parameters)
		{
			return SendCommand(commandName.ToString(), parameters);
		}

		public BitcoinAddress GetNewAddress()
		{
			return BitcoinAddress.Create(SendCommand(RPCOperations.getnewaddress).Result.ToString(), Network);
		}

		public async Task<BitcoinAddress> GetNewAddressAsync()
		{
			var result = await SendCommandAsync(RPCOperations.getnewaddress).ConfigureAwait(false);
			return BitcoinAddress.Create(result.Result.ToString(), Network);
		}

		public Task<RPCResponse> SendCommandAsync(RPCOperations commandName, params object[] parameters)
		{
			return SendCommandAsync(commandName.ToString(), parameters);
		}

		/// <summary>
		/// Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public async Task<RPCBlock> GetRPCBlock(uint256 blockId)
		{
			var resp = await SendCommandAsync("getblock", blockId.ToString(), false).ConfigureAwait(false);
			return SatoshiBlockFormatter.Parse(resp.Result as JObject);
		}

		/// <summary>
		/// Send a command
		/// </summary>
		/// <param name="commandName">https://en.bitcoin.it/wiki/Original_Bitcoin_client/API_calls_list</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public RPCResponse SendCommand(string commandName, params object[] parameters)
		{
			return SendCommand(new RPCRequest(commandName, parameters));
		}

		public Task<RPCResponse> SendCommandAsync(string commandName, params object[] parameters)
		{
			return SendCommandAsync(new RPCRequest(commandName, parameters));
		}

		public RPCResponse SendCommand(RPCRequest request, bool throwIfRPCError = true)
		{
			try
			{
				return SendCommandAsync(request, throwIfRPCError).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}

		public async Task<RPCResponse> SendCommandAsync(RPCRequest request, bool throwIfRPCError = true)
		{
			var webRequest = (HttpWebRequest)WebRequest.Create(Address);
			webRequest.Headers[HttpRequestHeader.Authorization] = "Basic " + Encoders.Base64.EncodeData(Encoders.ASCII.DecodeData(_Authentication));
			webRequest.ContentType = "application/json-rpc";
			webRequest.Method = "POST";

			var writer = new StringWriter();
			request.WriteJSON(writer);
			writer.Flush();
			var json = writer.ToString();
			var bytes = Encoding.UTF8.GetBytes(json);
#if !(PORTABLE || NETCORE)
			webRequest.ContentLength = bytes.Length;
#endif
			var dataStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false);
			await dataStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
			await dataStream.FlushAsync().ConfigureAwait(false);
			dataStream.Dispose();
			RPCResponse response;
			WebResponse webResponse = null;
			WebResponse errorResponse = null;
			try
			{
				webResponse = await webRequest.GetResponseAsync().ConfigureAwait(false);
				response = RPCResponse.Load(await ToMemoryStreamAsync(webResponse.GetResponseStream()).ConfigureAwait(false));

				if(throwIfRPCError)
					response.ThrowIfError();
			}
			catch(WebException ex)
			{
				if(ex.Response == null || ex.Response.ContentLength == 0)
					throw;
				errorResponse = ex.Response;
				response = RPCResponse.Load(await ToMemoryStreamAsync(errorResponse.GetResponseStream()).ConfigureAwait(false));
				if(throwIfRPCError)
					response.ThrowIfError();
			}
			finally
			{
				if(errorResponse != null)
				{
					errorResponse.Dispose();
					errorResponse = null;
				}
				if(webResponse != null)
				{
					webResponse.Dispose();
					webResponse = null;
				}
			}
			return response;
		}

		private async Task<Stream> ToMemoryStreamAsync(Stream stream)
		{
			MemoryStream ms = new MemoryStream();
			await stream.CopyToAsync(ms).ConfigureAwait(false);
			ms.Position = 0;
			return ms;
		}

		#region P2P Networking
#if !NOSOCKET
		public PeerInfo[] GetPeersInfo()
		{
			PeerInfo[] peers = null;
			try
			{
				peers = GetPeersInfoAsync().Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
			return peers;
		}

		public async Task<PeerInfo[]> GetPeersInfoAsync()
		{
			var resp = await SendCommandAsync("getpeerinfo").ConfigureAwait(false);
			var peers = resp.Result as JArray;
			var result = new PeerInfo[peers.Count];
			var i = 0;
			foreach(var peer in peers)
			{
				var localAddr = (string)peer["addrlocal"];
				var pingWait = peer["pingwait"] != null ? (double)peer["pingwait"] : 0;

				localAddr = string.IsNullOrEmpty(localAddr) ? "127.0.0.1:8333" : localAddr;

				result[i++] = new PeerInfo
				{
					Id = (int)peer["id"],
					Address = Utils.ParseIpEndpoint((string)peer["addr"], this.Network.DefaultPort),
					LocalAddress = Utils.ParseIpEndpoint(localAddr, this.Network.DefaultPort),
					Services = ulong.Parse((string)peer["services"]),
					LastSend = Utils.UnixTimeToDateTime((uint)peer["lastsend"]),
					LastReceive = Utils.UnixTimeToDateTime((uint)peer["lastrecv"]),
					BytesSent = (long)peer["bytessent"],
					BytesReceived = (long)peer["bytesrecv"],
					ConnectionTime = Utils.UnixTimeToDateTime((uint)peer["conntime"]),
					TimeOffset = TimeSpan.FromSeconds(Math.Min((long)int.MaxValue, (long)peer["timeoffset"])),
					PingTime = peer["pingtime"] == null ? (TimeSpan?)null : TimeSpan.FromSeconds((double)peer["pingtime"]),
					PingWait = TimeSpan.FromSeconds(pingWait),
					Blocks = peer["blocks"] != null ? (int)peer["blocks"] : -1,
					Version = (int)peer["version"],
					SubVersion = (string)peer["subver"],
					Inbound = (bool)peer["inbound"],
					StartingHeight = (int)peer["startingheight"],
					SynchronizedBlocks = (int)peer["synced_blocks"],
					SynchronizedHeaders = (int)peer["synced_headers"],
					IsWhiteListed = (bool)peer["whitelisted"],
					BanScore = peer["banscore"] == null ? 0 : (int)peer["banscore"],
					Inflight = peer["inflight"].Select(x => uint.Parse((string)x)).ToArray()
				};
			}
			return result;
		}

		public async Task<PeerInfo[]> GetStratisPeersInfoAsync()
		{
			var resp = await SendCommandAsync("getpeerinfo").ConfigureAwait(false);
			var peers = resp.Result as JArray;
			var result = new PeerInfo[peers.Count];
			var i = 0;
			foreach (var peer in peers)
			{
				var localAddr = (string)peer["addrlocal"];
				var pingWait = peer["pingwait"] != null ? (double)peer["pingwait"] : 0;

				localAddr = string.IsNullOrEmpty(localAddr) ? "127.0.0.1:8333" : localAddr;

				result[i++] = new PeerInfo
				{
					//Id = (int)peer["id"],
					Address = Utils.ParseIpEndpoint((string)peer["addr"], this.Network.DefaultPort),
					LocalAddress = Utils.ParseIpEndpoint(localAddr, this.Network.DefaultPort),
					Services = ulong.Parse((string)peer["services"]),
					LastSend = Utils.UnixTimeToDateTime((uint)peer["lastsend"]),
					LastReceive = Utils.UnixTimeToDateTime((uint)peer["lastrecv"]),
					BytesSent = (long)peer["bytessent"],
					BytesReceived = (long)peer["bytesrecv"],
					ConnectionTime = Utils.UnixTimeToDateTime((uint)peer["conntime"]),
					TimeOffset = TimeSpan.FromSeconds(Math.Min((long)int.MaxValue, (long)peer["timeoffset"])),
					PingTime = TimeSpan.FromSeconds((double)peer["pingtime"]),
					PingWait = TimeSpan.FromSeconds(pingWait),
					//Blocks = peer["blocks"] != null ? (int)peer["blocks"] : -1,
					Version = (int)peer["version"],
					SubVersion = (string)peer["subver"],
					Inbound = (bool)peer["inbound"],
					StartingHeight = (int)peer["startingheight"],
					//SynchronizedBlocks = (int)peer["synced_blocks"],
					//SynchronizedHeaders = (int)peer["synced_headers"],
					//IsWhiteListed = (bool)peer["whitelisted"],
					BanScore = peer["banscore"] == null ? 0 : (int)peer["banscore"],
					//Inflight = peer["inflight"].Select(x => uint.Parse((string)x)).ToArray()
				};
			}
			return result;
		}

		public void AddNode(EndPoint nodeEndPoint, bool onetry = false)
		{
			if(nodeEndPoint == null)
				throw new ArgumentNullException("nodeEndPoint");
			SendCommand("addnode", nodeEndPoint.ToString(), onetry ? "onetry" : "add");
		}

		public async Task AddNodeAsync(EndPoint nodeEndPoint, bool onetry = false)
		{
			if(nodeEndPoint == null)
				throw new ArgumentNullException("nodeEndPoint");
			await SendCommandAsync("addnode", nodeEndPoint.ToString(), onetry ? "onetry" : "add").ConfigureAwait(false);
		}

		public void RemoveNode(EndPoint nodeEndPoint)
		{
			if(nodeEndPoint == null)
				throw new ArgumentNullException("nodeEndPoint");
			SendCommandAsync("addnode", nodeEndPoint.ToString(), "remove");
		}

		public async Task RemoveNodeAsync(EndPoint nodeEndPoint)
		{
			if(nodeEndPoint == null)
				throw new ArgumentNullException("nodeEndPoint");
			await SendCommandAsync("addnode", nodeEndPoint.ToString(), "remove").ConfigureAwait(false);
		}

		public async Task<AddedNodeInfo[]> GetAddedNodeInfoAsync(bool detailed)
		{
			var result = await SendCommandAsync("getaddednodeinfo", detailed).ConfigureAwait(false);
			var obj = result.Result;
			return obj.Select(entry => new AddedNodeInfo
			{
				AddedNode = Utils.ParseIpEndpoint((string)entry["addednode"], 8333),
				Connected = (bool)entry["connected"],
				Addresses = entry["addresses"].Select(x => new NodeAddressInfo
				{
					Address = Utils.ParseIpEndpoint((string)x["address"], 8333),
					Connected = (bool)x["connected"]
				})
			}).ToArray();
		}

		public AddedNodeInfo[] GetAddedNodeInfo(bool detailed)
		{
			AddedNodeInfo[] addedNodesInfo = null;
			try
			{
				addedNodesInfo = GetAddedNodeInfoAsync(detailed).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
			return addedNodesInfo;
		}

		public AddedNodeInfo GetAddedNodeInfo(bool detailed, EndPoint nodeEndPoint)
		{
			AddedNodeInfo addedNodeInfo = null;
			try
			{
				addedNodeInfo = GetAddedNodeInfoAync(detailed, nodeEndPoint).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
			return addedNodeInfo;
		}

		public async Task<AddedNodeInfo> GetAddedNodeInfoAync(bool detailed, EndPoint nodeEndPoint)
		{
			if(nodeEndPoint == null)
				throw new ArgumentNullException("nodeEndPoint");

			try
			{

				var result = await SendCommandAsync("getaddednodeinfo", detailed, nodeEndPoint.ToString()).ConfigureAwait(false);
				var e = result.Result;
				return e.Select(entry => new AddedNodeInfo
				{
					AddedNode = Utils.ParseIpEndpoint((string)entry["addednode"], 8333),
					Connected = (bool)entry["connected"],
					Addresses = entry["addresses"].Select(x => new NodeAddressInfo
					{
						Address = Utils.ParseIpEndpoint((string)x["address"], 8333),
						Connected = (bool)x["connected"]
					})
				}).FirstOrDefault();
			}
			catch(RPCException ex)
			{
				if(ex.RPCCode == RPCErrorCode.RPC_CLIENT_NODE_NOT_ADDED)
					return null;
				throw;
			}
		}
#endif

		#endregion

		#region Block chain and UTXO

		public uint256 GetBestBlockHash()
		{
			return uint256.Parse((string)SendCommand("getbestblockhash").Result);
		}

		public async Task<uint256> GetBestBlockHashAsync()
		{
			return uint256.Parse((string)(await SendCommandAsync("getbestblockhash").ConfigureAwait(false)).Result);
		}

		public BlockHeader GetBlockHeader(int height)
		{
			var hash = GetBlockHash(height);
			return GetBlockHeader(hash);
		}

		public async Task<BlockHeader> GetBlockHeaderAsync(int height)
		{
			var hash = await GetBlockHashAsync(height).ConfigureAwait(false);
			return await GetBlockHeaderAsync(hash).ConfigureAwait(false);
		}

		/// <summary>
		/// Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public async Task<Block> GetBlockAsync(uint256 blockId)
		{
			var resp = await SendCommandAsync("getblock", blockId.ToString(), false).ConfigureAwait(false);
			return new Block(Encoders.Hex.DecodeData(resp.Result.ToString()));
		}

		/// <summary>
		/// Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public Block GetBlock(uint256 blockId)
		{
			try
			{
				return GetBlockAsync(blockId).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				throw;
			}
		}

		public Block GetBlock(int height)
		{
			try
			{
				return GetBlockAsync(height).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				throw;
			}
		}

		public async Task<Block> GetBlockAsync(int height)
		{
			var hash = await GetBlockHashAsync(height).ConfigureAwait(false);
			return await GetBlockAsync(hash).ConfigureAwait(false);
		}

		public BlockHeader GetBlockHeader(uint256 blockHash)
		{
			var resp = SendCommand("getblock", blockHash.ToString());
			return ParseBlockHeader(resp);
		}

		public async Task<BlockHeader> GetBlockHeaderAsync(uint256 blockHash)
		{
			var resp = await SendCommandAsync("getblock", blockHash.ToString()).ConfigureAwait(false);
			return ParseBlockHeader(resp);
		}

		private static BlockHeader ParseBlockHeader(RPCResponse resp)
		{
			var header = new BlockHeader();
			header.Version = (int)resp.Result["version"];
			header.Nonce = (uint)resp.Result["nonce"];
			header.Bits = new Target(Encoders.Hex.DecodeData((string)resp.Result["bits"]));
			if(resp.Result["previousblockhash"] != null)
			{
				header.HashPrevBlock = uint256.Parse((string)resp.Result["previousblockhash"]);
			}
			if(resp.Result["time"] != null)
			{
				header.BlockTime = Utils.UnixTimeToDateTime((uint)resp.Result["time"]);
			}
			if(resp.Result["merkleroot"] != null)
			{
				header.HashMerkleRoot = uint256.Parse((string)resp.Result["merkleroot"]);
			}
			return header;
		}

		public uint256 GetBlockHash(int height)
		{
			var resp = SendCommand("getblockhash", height);
			return uint256.Parse(resp.Result.ToString());
		}

		public async Task<uint256> GetBlockHashAsync(int height)
		{
			var resp = await SendCommandAsync("getblockhash", height).ConfigureAwait(false);
			return uint256.Parse(resp.Result.ToString());
		}		

		public int GetBlockCount()
		{
			return (int)SendCommand("getblockcount").Result;
		}

		public async Task<int> GetBlockCountAsync()
		{
			return (int)(await SendCommandAsync("getblockcount").ConfigureAwait(false)).Result;
		}

		public uint256[] GetRawMempool()
		{
			var result = SendCommand("getrawmempool");
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(uint256.Parse).ToArray();
		}

		public async Task<uint256[]> GetRawMempoolAsync()
		{
			var result = await SendCommandAsync("getrawmempool").ConfigureAwait(false);
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(uint256.Parse).ToArray();
		}

		/// <summary>
		/// GetTransactions only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="blockHash"></param>
		/// <returns></returns>
		public IEnumerable<Transaction> GetTransactions(uint256 blockHash)
		{
			if(blockHash == null)
				throw new ArgumentNullException("blockHash");

			var resp = SendCommand("getblock", blockHash.ToString());

			var tx = resp.Result["tx"] as JArray;
			if(tx != null)
			{
				foreach(var item in tx)
				{
					var result = GetRawTransaction(uint256.Parse(item.ToString()), false);
					if(result != null)
						yield return result;
				}
			}
		}

		public IEnumerable<Transaction> GetTransactions(int height)
		{
			return GetTransactions(GetBlockHash(height));
		}

		#endregion

		#region Coin generation

		#endregion

		#region Raw Transaction

		public Transaction DecodeRawTransaction(string rawHex)
		{
			var response = SendCommand("decoderawtransaction", rawHex);
			return Transaction.Parse(response.Result.ToString(), RawFormat.Satoshi);
		}

		public Transaction DecodeRawTransaction(byte[] raw)
		{

			return DecodeRawTransaction(Encoders.Hex.EncodeData(raw));
		}
		public async Task<Transaction> DecodeRawTransactionAsync(string rawHex)
		{
			var response = await SendCommandAsync("decoderawtransaction", rawHex).ConfigureAwait(false);
			return Transaction.Parse(response.Result.ToString(), RawFormat.Satoshi);
		}

		public Task<Transaction> DecodeRawTransactionAsync(byte[] raw)
		{
			return DecodeRawTransactionAsync(Encoders.Hex.EncodeData(raw));
		}

		/// <summary>
		/// getrawtransaction only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="txid"></param>
		/// <returns></returns>
		public Transaction GetRawTransaction(uint256 txid, bool throwIfNotFound = true)
		{
			try
			{
				return GetRawTransactionAsync(txid, throwIfNotFound).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return null; //Can't happen
			}
		}

		public async Task<Transaction> GetRawTransactionAsync(uint256 txid, bool throwIfNotFound = true)
		{
			var response = await SendCommandAsync(new RPCRequest("getrawtransaction", new[] { txid.ToString() }), throwIfNotFound).ConfigureAwait(false);
			if(throwIfNotFound)
				response.ThrowIfError();
			if(response.Error != null && response.Error.Code == RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY)
				return null;

			response.ThrowIfError();
			var tx = new Transaction();
			tx.ReadWrite(Encoders.Hex.DecodeData(response.Result.ToString()));
			return tx;
		}

		public void SendRawTransaction(Transaction tx)
		{
			SendRawTransaction(tx.ToBytes());
		}

		public void SendRawTransaction(byte[] bytes)
		{
			SendCommand("sendrawtransaction", Encoders.Hex.EncodeData(bytes));
		}

		public Task SendRawTransactionAsync(Transaction tx)
		{
			return SendRawTransactionAsync(tx.ToBytes());
		}

		public Task SendRawTransactionAsync(byte[] bytes)
		{
			return SendCommandAsync("sendrawtransaction", Encoders.Hex.EncodeData(bytes));
		}

		#endregion

		#region Utility functions
		/// <summary>
		/// Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock"></param>
		/// <returns></returns>
		[Obsolete("Use EstimateFeeRate or TryEstimateFeeRate instead")]
		public FeeRate EstimateFee(int nblock)
		{
			var response = SendCommand(RPCOperations.estimatefee, nblock);
			var result = response.Result.Value<decimal>();
			var money = Money.Coins(result);
			if(money.Satoshi < 0)
				money = Money.Zero;
			return new FeeRate(money);
		}

		/// <summary>
		/// Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock"></param>
		/// <returns></returns>
		[Obsolete("Use EstimateFeeRateAsync instead")]
		public async Task<Money> EstimateFeeAsync(int nblock)
		{
			var response = await SendCommandAsync(RPCOperations.estimatefee, nblock).ConfigureAwait(false);
			return Money.Parse(response.Result.ToString());
		}

		/// <summary>
		/// Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock">The time expected, in block, before getting confirmed</param>
		/// <returns>The estimated fee rate</returns>
		/// <exception cref="NoEstimationException">The Fee rate couldn't be estimated because of insufficient data from Bitcoin Core</exception>
		public FeeRate EstimateFeeRate(int nblock)
		{
			return EstimateFeeRateAsync(nblock).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Tries to get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock">The time expected, in block, before getting confirmed</param>
		/// <returns>The estimated fee rate or null</returns>
		public async Task<FeeRate> TryEstimateFeeRateAsync(int nblock)
		{
			return await EstimateFeeRateImplAsync(nblock).ConfigureAwait(false);
		}

		/// <summary>
		/// Tries to get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock">The time expected, in block, before getting confirmed</param>
		/// <returns>The estimated fee rate or null</returns>
		public FeeRate TryEstimateFeeRate(int nblock)
		{
			return TryEstimateFeeRateAsync(nblock).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock">The time expected, in block, before getting confirmed</param>
		/// <returns>The estimated fee rate</returns>
		/// <exception cref="NoEstimationException">when fee couldn't be estimated</exception>
		public async Task<FeeRate> EstimateFeeRateAsync(int nblock)
		{
			var feeRate = await EstimateFeeRateImplAsync(nblock);
			if(feeRate == null)
				throw new NoEstimationException(nblock);
			return feeRate;
		}

		private async Task<FeeRate> EstimateFeeRateImplAsync(int nblock)
		{
			var response = await SendCommandAsync(RPCOperations.estimatefee, nblock).ConfigureAwait(false);
			var result = response.Result.Value<decimal>();
			var money = Money.Coins(result);
			if(money.Satoshi < 0)
				return null;
			return new FeeRate(money);
		}

		public decimal EstimatePriority(int nblock)
		{
			decimal priority = 0;
			try
			{
				priority = EstimatePriorityAsync(nblock).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
			return priority;
		}

		public async Task<decimal> EstimatePriorityAsync(int nblock)
		{
			if(nblock < 0)
				throw new ArgumentOutOfRangeException("nblock", "nblock must be greater or equal to zero");
			var response = await SendCommandAsync("estimatepriority", nblock).ConfigureAwait(false);
			return response.Result.Value<decimal>();
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="commentTx">A locally-stored (not broadcast) comment assigned to this transaction. Default is no comment</param>
		/// <param name="commentDest">A locally-stored (not broadcast) comment assigned to this transaction. Meant to be used for describing who the payment was sent to. Default is no comment</param>
		/// <returns>The TXID of the sent transaction</returns>
		public uint256 SendToAddress(BitcoinAddress address, Money amount, string commentTx = null, string commentDest = null)
		{
			uint256 txid = null;
			try
			{
				txid = SendToAddressAsync(address, amount, commentTx, commentDest).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
			return txid;
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="commentTx">A locally-stored (not broadcast) comment assigned to this transaction. Default is no comment</param>
		/// <param name="commentDest">A locally-stored (not broadcast) comment assigned to this transaction. Meant to be used for describing who the payment was sent to. Default is no comment</param>
		/// <returns>The TXID of the sent transaction</returns>
		public async Task<uint256> SendToAddressAsync(BitcoinAddress address, Money amount, string commentTx = null, string commentDest = null)
		{
			List<object> parameters = new List<object>();
			parameters.Add(address.ToString());
			parameters.Add(amount.ToString());
			if(commentTx != null)
				parameters.Add(commentTx);
			if(commentDest != null)
				parameters.Add(commentDest);
			var resp = await SendCommandAsync(RPCOperations.sendtoaddress, parameters.ToArray()).ConfigureAwait(false);
			return uint256.Parse(resp.Result.ToString());
		}

		public bool SetTxFee(FeeRate feeRate)
		{
			return SendCommand(RPCOperations.settxfee, new[] { feeRate.FeePerK.ToString() }).Result.ToString() == "true";
		}

		#endregion
	}

#if !NOSOCKET
	public class PeerInfo
	{
		public int Id
		{
			get; internal set;
		}
		public IPEndPoint Address
		{
			get; internal set;
		}
		public IPEndPoint LocalAddress
		{
			get; internal set;
		}
		public ulong Services
		{
			get; internal set;
		}
		public DateTimeOffset LastSend
		{
			get; internal set;
		}
		public DateTimeOffset LastReceive
		{
			get; internal set;
		}
		public long BytesSent
		{
			get; internal set;
		}
		public long BytesReceived
		{
			get; internal set;
		}
		public DateTimeOffset ConnectionTime
		{
			get; internal set;
		}
		public TimeSpan? PingTime
		{
			get; internal set;
		}
		public int Version
		{
			get; internal set;
		}
		public string SubVersion
		{
			get; internal set;
		}
		public bool Inbound
		{
			get; internal set;
		}
		public int StartingHeight
		{
			get; internal set;
		}
		public int BanScore
		{
			get; internal set;
		}
		public int SynchronizedHeaders
		{
			get; internal set;
		}
		public int SynchronizedBlocks
		{
			get; internal set;
		}
		public uint[] Inflight
		{
			get; internal set;
		}
		public bool IsWhiteListed
		{
			get; internal set;
		}
		public TimeSpan PingWait
		{
			get; internal set;
		}
		public int Blocks
		{
			get; internal set;
		}
		public TimeSpan TimeOffset
		{
			get; internal set;
		}
	}

	public class AddedNodeInfo
	{
		public EndPoint AddedNode
		{
			get; internal set;
		}
		public bool Connected
		{
			get; internal set;
		}
		public IEnumerable<NodeAddressInfo> Addresses
		{
			get; internal set;
		}
	}

	public class NodeAddressInfo
	{
		public IPEndPoint Address
		{
			get; internal set;
		}
		public bool Connected
		{
			get; internal set;
		}
	}
#endif

	public class NoEstimationException : Exception
	{
		public NoEstimationException(int nblock)
			: base("The FeeRate couldn't be estimated because of insufficient data from Bitcoin Core. Try to use smaller nBlock, or wait Bitcoin Core to gather more data.")
		{
		}
	}

}
#endif