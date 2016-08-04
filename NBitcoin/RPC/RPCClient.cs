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
	public class RPCAccount
	{
		public Money Amount
		{
			get;
			set;
		}
		public String AccountName
		{
			get;
			set;
		}
	}

	public class ChangeAddress
	{
		public Money Amount
		{
			get;
			set;
		}
		public BitcoinAddress Address
		{
			get;
			set;
		}
	}

	public class AddressGrouping
	{
		public AddressGrouping()
		{
			ChangeAddresses = new List<ChangeAddress>();
		}
		public BitcoinAddress PublicAddress
		{
			get;
			set;
		}
		public Money Amount
		{
			get;
			set;
		}
		public string Account
		{
			get;
			set;
		}

		public List<ChangeAddress> ChangeAddresses
		{
			get;
			set;
		}
	}

	/*
        Category            Name                        Implemented 
        ------------------ --------------------------- -----------------------
        ------------------ Overall control/query calls 
        control            getinfo
        control            help
        control            stop

        ------------------ P2P networking
        network            getnetworkinfo
        network            addnode                      Yes
        network            disconnectnode
        network            getaddednodeinfo             Yes
        network            getconnectioncount
        network            getnettotals
        network            getpeerinfo                  Yes
        network            ping
        network            setban
        network            listbanned
        network            clearbanned

        ------------------ Block chain and UTXO
        blockchain         getblockchaininfo
        blockchain         getbestblockhash             Yes
        blockchain         getblockcount                Yes
        blockchain         getblock                     Yes
        blockchain         getblockhash                 Yes
        blockchain         getchaintips
        blockchain         getdifficulty
        blockchain         getmempoolinfo
        blockchain         getrawmempool                Yes
        blockchain         gettxout
        blockchain         gettxoutproof
        blockchain         verifytxoutproof
        blockchain         gettxoutsetinfo
        blockchain         verifychain

        ------------------ Mining
        mining             getblocktemplate
        mining             getmininginfo
        mining             getnetworkhashps
        mining             prioritisetransaction
        mining             submitblock

        ------------------ Coin generation
        generating         getgenerate
        generating         setgenerate
        generating         generate

        ------------------ Raw transactions
        rawtransactions    createrawtransaction
        rawtransactions    decoderawtransaction
        rawtransactions    decodescript
        rawtransactions    getrawtransaction
        rawtransactions    sendrawtransaction
        rawtransactions    signrawtransaction
        rawtransactions    fundrawtransaction

        ------------------ Utility functions
        util               createmultisig
        util               validateaddress
        util               verifymessage
        util               estimatefee                  Yes
        util               estimatepriority             Yes

        ------------------ Not shown in help
        hidden             invalidateblock
        hidden             reconsiderblock
        hidden             setmocktime
        hidden             resendwallettransactions

        ------------------ Wallet
        wallet             addmultisigaddress
        wallet             backupwallet                 Yes
        wallet             dumpprivkey                  Yes
        wallet             dumpwallet
        wallet             encryptwallet
        wallet             getaccountaddress
        wallet             getaccount                   Yes
        wallet             getaddressesbyaccount
        wallet             getbalance
        wallet             getnewaddress
        wallet             getrawchangeaddress
        wallet             getreceivedbyaccount
        wallet             getreceivedbyaddress
        wallet             gettransaction               Yes
        wallet             getunconfirmedbalance
        wallet             getwalletinfo
        wallet             importprivkey                Yes
        wallet             importwallet
        wallet             importaddress                Yes
        wallet             keypoolrefill
        wallet             listaccounts                 Yes
        wallet             listaddressgroupings         Yes
        wallet             listlockunspent
        wallet             listreceivedbyaccount
        wallet             listreceivedbyaddress
        wallet             listsinceblock
        wallet             listtransactions
        wallet             listunspent                  Yes
        wallet             lockunspent                  Yes
        wallet             move
        wallet             sendfrom
        wallet             sendmany
        wallet             sendtoaddress
        wallet             setaccount
        wallet             settxfee
        wallet             signmessage
        wallet             walletlock
        wallet             walletpassphrasechange
        wallet             walletpassphrase
    */
	public class RPCClient : IBlockRepository
	{
		private readonly NetworkCredential _credentials;
		public NetworkCredential Credentials
		{
			get
			{
				return _credentials;
			}
		}
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
		public RPCClient(NetworkCredential credentials, string host, Network network)
			: this(credentials, BuildUri(host, network.RPCPort), network)
		{
		}

		private static Uri BuildUri(string host, int port)
		{
			UriBuilder builder = new UriBuilder();
			builder.Host = host;
			builder.Scheme = "http";
			builder.Port = port;
			return builder.Uri;
		}
		public RPCClient(NetworkCredential credentials, Uri address, Network network = null)
		{

			if(credentials == null)
				throw new ArgumentNullException("credentials");
			if(address == null)
				throw new ArgumentNullException("address");
			if(network == null)
			{
				network = new[] { Network.Main, Network.TestNet, Network.RegTest }.FirstOrDefault(n => n.RPCPort == address.Port);
				if(network == null)
					throw new ArgumentNullException("network");
			}
			_credentials = credentials;
			_address = address;
			_network = network;
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
			webRequest.Headers[HttpRequestHeader.Authorization] = "Basic " + Encoders.Base64.EncodeData(Encoders.ASCII.DecodeData(Credentials.UserName + ":" + Credentials.Password));
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
			dataStream.Dispose();
			RPCResponse response;
			try
			{
				using(var webResponse = await webRequest.GetResponseAsync().ConfigureAwait(false))
				{
					response = RPCResponse.Load(webResponse.GetResponseStream());
				}
				if(throwIfRPCError)
					response.ThrowIfError();
			}
			catch(WebException ex)
			{
				if(ex.Response == null || ex.Response.ContentLength == 0)
					throw;
				response = RPCResponse.Load(ex.Response.GetResponseStream());
				if(throwIfRPCError)
					response.ThrowIfError();
			}
			return response;
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
					PingTime = TimeSpan.FromSeconds((double)peer["pingtime"]),
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

		#region Wallet

		public void ImportPrivKey(BitcoinSecret secret)
		{
			SendCommand("importprivkey", secret.ToWif());
		}

		public void ImportPrivKey(BitcoinSecret secret, string label, bool rescan)
		{
			SendCommand("importprivkey", secret.ToWif(), label, rescan);
		}

		public async Task ImportPrivKeyAsync(BitcoinSecret secret)
		{
			await SendCommandAsync("importprivkey", secret.ToWif()).ConfigureAwait(false);
		}

		public async Task ImportPrivKeyAsync(BitcoinSecret secret, string label, bool rescan)
		{
			await SendCommandAsync("importprivkey", secret.ToWif(), label, rescan).ConfigureAwait(false);
		}

		public void ImportAddress(BitcoinAddress address)
		{
			SendCommand("importaddress", address.ToString());
		}

		public void ImportAddress(BitcoinAddress address, string label, bool rescan)
		{
			SendCommand("importaddress", address.ToString(), label, rescan);
		}

		public async Task ImportAddressAsync(BitcoinAddress address)
		{
			await SendCommandAsync("importaddress", address.ToString()).ConfigureAwait(false);
		}

		public async Task ImportAddressAsync(BitcoinAddress address, string label, bool rescan)
		{
			await SendCommandAsync("importaddress", address.ToString(), label, rescan).ConfigureAwait(false);
		}

		public BitcoinSecret DumpPrivKey(BitcoinAddress address)
		{
			var response = SendCommand("dumpprivkey", address.ToString());
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}

		public async Task<BitcoinSecret> DumpPrivKeyAsync(BitcoinAddress address)
		{
			var response = await SendCommandAsync("dumpprivkey", address.ToString()).ConfigureAwait(false);
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}

		public BitcoinAddress GetAccountAddress(string account)
		{
			var response = SendCommand("getaccountaddress", account);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}

		public async Task<BitcoinAddress> GetAccountAddressAsync(string account)
		{
			var response = await SendCommandAsync("getaccountaddress", account).ConfigureAwait(false);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}

		public BitcoinSecret GetAccountSecret(string account)
		{
			var address = GetAccountAddress(account);
			return DumpPrivKey(address);
		}

		public async Task<BitcoinSecret> GetAccountSecretAsync(string account)
		{
			var address = await GetAccountAddressAsync(account).ConfigureAwait(false);
			return await DumpPrivKeyAsync(address).ConfigureAwait(false);
		}

		public UnspentCoin[] ListUnspent()
		{
			var response = SendCommand("listunspent");
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		public UnspentCoin[] ListUnspent(int minconf, int maxconf, params BitcoinAddress[] addresses)
		{
			var addr = from a in addresses select a.ToString();
			var response = SendCommand("listunspent", minconf, maxconf, addr.ToArray());
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		public async Task<UnspentCoin[]> ListUnspentAsync()
		{
			var response = await SendCommandAsync("listunspent").ConfigureAwait(false);
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		public async Task<UnspentCoin[]> ListUnspentAsync(int minconf, int maxconf, params BitcoinAddress[] addresses)
		{
			var addr = from a in addresses select a.ToString();
			var response = await SendCommandAsync("listunspent", minconf, maxconf, addr.ToArray()).ConfigureAwait(false);
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		public void LockUnspent(params OutPoint[] outpoints)
		{
			LockUnspentCore(false, outpoints);
		}

		public void UnlockUnspent(params OutPoint[] outpoints)
		{
			LockUnspentCore(true, outpoints);
		}

		public Task LockUnspentAsync(params OutPoint[] outpoints)
		{
			return LockUnspentCoreAsync(false, outpoints);
		}

		public Task UnlockUnspentAsync(params OutPoint[] outpoints)
		{
			return LockUnspentCoreAsync(true, outpoints);
		}

		private void LockUnspentCore(bool unlock, OutPoint[] outpoints)
		{
			try
			{
				LockUnspentCoreAsync(unlock, outpoints).Wait();
			}
			catch(AggregateException ex)
			{
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			}
		}

		private async Task LockUnspentCoreAsync(bool unlock, OutPoint[] outpoints)
		{
			if(outpoints == null || outpoints.Length == 0)
				return;
			var parameters = new List<object>();
			parameters.Add(unlock);
			var array = new JArray();
			parameters.Add(array);
			foreach(var outp in outpoints)
			{
				var obj = new JObject();
				obj["txid"] = outp.Hash.ToString();
				obj["vout"] = outp.N;
				array.Add(obj);
			}
			await SendCommandAsync("lockunspent", parameters.ToArray()).ConfigureAwait(false);
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

		public IEnumerable<RPCAccount> ListAccounts()
		{
			var result = SendCommand(RPCOperations.listaccounts);
			var obj = (JObject)result.Result;
			foreach(var prop in obj.Properties())
			{
				yield return new RPCAccount()
				{
					AccountName = prop.Name,
					Amount = Money.Coins((decimal)prop.Value)
				};
			}
		}

		public void BackupWallet(string path)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			SendCommand("backupwallet", path);
		}

		public async Task BackupWalletAsync(string path)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			await SendCommandAsync("backupwallet", path);
		}

		public IEnumerable<BitcoinSecret> ListSecrets()
		{
			foreach(var grouping in ListAddressGroupings())
			{
				yield return DumpPrivKey(grouping.PublicAddress);
				foreach(var change in grouping.ChangeAddresses)
					yield return DumpPrivKey(change.Address);
			}
		}

		public IEnumerable<AddressGrouping> ListAddressGroupings()
		{
			var result = SendCommand(RPCOperations.listaddressgroupings);
			var array = (JArray)result.Result;
			foreach(var group in array.Children<JArray>())
			{
				var grouping = new AddressGrouping();
				grouping.PublicAddress = BitcoinAddress.Create(group[0][0].ToString());
				grouping.Amount = Money.Coins(group[0][1].Value<decimal>());
				grouping.Account = group[0].Count() > 2 ? group[0][2].ToString() : null;

				foreach(var subgroup in group.Skip(1))
				{
					var change = new ChangeAddress();
					change.Address = BitcoinAddress.Create(subgroup[0].ToString());
					change.Amount = Money.Coins(subgroup[1].Value<decimal>());
					grouping.ChangeAddresses.Add(change);
				}

				yield return grouping;
			}
		}

		#endregion

		#region Utility functions
		/// <summary>
		/// Get the estimated fee per kb for being confirmed in nblock
		/// </summary>
		/// <param name="nblock"></param>
		/// <returns></returns>
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
		public async Task<Money> EstimateFeeAsync(int nblock)
		{
			var response = await SendCommandAsync(RPCOperations.estimatefee, nblock).ConfigureAwait(false);
			return Money.Parse(response.Result.ToString());
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
		public TimeSpan PingTime
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
}
