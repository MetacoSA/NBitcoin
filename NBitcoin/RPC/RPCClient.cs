#if !NOJSONNET
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.Scripting;
using static NBitcoin.RPC.BlockchainInfo;

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
		network			addnode						Yes
		network			disconnectnode
		network			getaddednodeinfo			Yes
		network			getconnectioncount
		network			getnettotals
		network			getpeerinfo					Yes
		network			ping
		network			setban
		network			listbanned
		network			clearbanned

		------------------ Block chain and UTXO
		blockchain		getblockchaininfo			Yes
		blockchain		getbestblockhash			Yes
		blockchain		getblockcount				Yes
		blockchain		getblock					Yes
		blockchain		getblockhash				Yes
		blockchain		getblockstats				Yes
		blockchain		getchaintips
		blockchain		getdifficulty
		blockchain		getmempoolinfo
		blockchain		getrawmempool				Yes
		blockchain		gettxout					Yes
		blockchain		gettxoutproof
		blockchain		verifytxoutproof
		blockchain		gettxoutsetinfo				Yes
		blockchain		verifychain
		blockchain		savemempool					Yes

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

		------------------ PSBT
		psbt - decodepsbt
		psbt - combinepsbt
		psbt - finalizepsbt
		psbt - createpsbt
		psbt - convertopsbt

		------------------ Utility functions
		util			createmultisig
		util			validateaddress
		util			verifymessage
		util			estimatefee					Yes
		util			estimatesmartfee			Yes
		------------------ Not shown in help
		hidden			invalidateblock				Yes
		hidden			reconsiderblock
		hidden			setmocktime
		hidden			resendwallettransactions

		------------------ Wallet
		wallet			 addmultisigaddress
		wallet			 backupwallet				Yes
		wallet			 dumpprivkey				Yes
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
		wallet			 listaccounts				Yes
		wallet			 listaddressgroupings		Yes
		wallet			 listlockunspent
		wallet			 listreceivedbyaccount
		wallet			 listreceivedbyaddress
		wallet			 listsinceblock
		wallet			 listtransactions
		wallet			 listunspent				Yes
		wallet			 lockunspent				Yes
		wallet			 move
		wallet			 sendfrom
		wallet			 sendmany
		wallet			 sendtoaddress
		wallet			 setaccount
		wallet			 settxfee
		wallet			 signmessage
		wallet			 walletlock
		wallet			 walletpassphrasechange
		wallet			 walletpassphrase			Yes
		wallet			 walletprocesspsbt
		wallet			 walletcreatefundedpsbt
	*/
	public partial class RPCClient : IBlockRepository
	{
		public static string GetRPCAuth(NetworkCredential credentials)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));
			var salt = Encoders.Hex.EncodeData(RandomUtils.GetBytes(16));
			var nobom = new UTF8Encoding(false);
			var result = Hashes.HMACSHA256(nobom.GetBytes(salt), nobom.GetBytes(credentials.Password));
			return $"{credentials.UserName}:{salt}${Encoders.Hex.EncodeData(result)}";
		}

		private static Lazy<HttpClient> _Shared = new Lazy<HttpClient>(() => new HttpClient() { Timeout = System.Threading.Timeout.InfiniteTimeSpan });

		HttpClient _HttpClient;
		public HttpClient HttpClient
		{
			get
			{
				return _HttpClient ?? _Shared.Value;
			}
			set
			{
				_HttpClient = value;
			}
		}

		private string _Authentication;
		private readonly Uri _address;
		public Uri Address
		{
			get
			{
				return _address;
			}
		}


		RPCCredentialString _CredentialString;
		public RPCCredentialString CredentialString
		{
			get
			{
				return _CredentialString;
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

		/// <summary>
		/// Use default bitcoin parameters to configure a RPCClient.
		/// </summary>
		/// <param name="network">The network used by the node. Must not be null.</param>
		public RPCClient(Network network) : this(null as string, BuildUri(null, null, network.RPCPort), network)
		{
		}

		[Obsolete("Use RPCClient(ConnectionString, string, Network)")]
		public RPCClient(NetworkCredential credentials, string host, Network network)
			: this(credentials, BuildUri(host, null, network.RPCPort), network)
		{
		}

		public RPCClient(RPCCredentialString credentials, Network network)
			: this(credentials, null as String, network)
		{
		}

		public RPCClient(RPCCredentialString credentials, string host, Network network)
			: this(credentials, BuildUri(host, credentials.ToString(), network.RPCPort), network)
		{
		}

		public RPCClient(RPCCredentialString credentials, Uri address, Network network)
		{
			credentials = credentials ?? new RPCCredentialString();

			if (address != null && network == null)
			{
				network = Network.GetNetworks().FirstOrDefault(n => n.RPCPort == address.Port);
				if (network == null)
					throw new ArgumentNullException(nameof(network));
			}

			if (credentials.UseDefault && network == null)
				throw new ArgumentException("network parameter is required if you use default credentials");
			if (address == null && network == null)
				throw new ArgumentException("network parameter is required if you use default uri");

			if (address == null)
				address = new Uri("http://127.0.0.1:" + network.RPCPort + "/");


			if (credentials.UseDefault)
			{
				//will throw impossible to get the cookie path
				GetDefaultCookieFilePath(network);
			}

			_CredentialString = credentials;
			_address = address;
			_network = network;

			if (credentials.UserPassword != null)
			{
				_Authentication = $"{credentials.UserPassword.UserName}:{credentials.UserPassword.Password}";
			}

			if (_Authentication == null)
			{
				TryRenewCookie();
			}
		}


		static ConcurrentDictionary<Network, string> _DefaultPaths = new ConcurrentDictionary<Network, string>();
		static RPCClient()
		{

#if !NOFILEIO
			var bitcoinFolder = Network.GetDefaultDataFolder("bitcoin");
			if (bitcoinFolder is null)
				return;

			var mainnet = Path.Combine(bitcoinFolder, ".cookie");
			RegisterDefaultCookiePath(Network.Main, mainnet);

			var testnet = Path.Combine(bitcoinFolder, "testnet3", ".cookie");
			RegisterDefaultCookiePath(Network.TestNet, testnet);

			var regtest = Path.Combine(bitcoinFolder, "regtest", ".cookie");
			RegisterDefaultCookiePath(Network.RegTest, regtest);
#endif
		}
		public static void RegisterDefaultCookiePath(Network network, string path)
		{
			_DefaultPaths.TryAdd(network, path);
		}


		private string GetCookiePath()
		{
			if (CredentialString.UseDefault && Network == null)
				throw new InvalidOperationException("NBitcoin bug, report to the developers");
			if (CredentialString.UseDefault)
				return GetDefaultCookieFilePath(Network);
			if (CredentialString.CookieFile != null)
				return CredentialString.CookieFile;
			return null;
		}

		/// <summary>
		/// The RPC Capabilities of this RPCClient instance, this property will be set by a call to ScanRPCCapabilitiesAsync
		/// </summary>
		public RPCCapabilities Capabilities { get; set; }

		/// <summary>
		/// Run several RPC function to scan the RPC capabilities, then set RPCClient.Capabilities
		/// </summary>
		/// <returns>The RPCCapabilities</returns>
		public async Task<RPCCapabilities> ScanRPCCapabilitiesAsync(CancellationToken cancellationToken = default)
		{
			var capabilities = new RPCCapabilities();
			var rpc = this.PrepareBatch();
			rpc.AllowBatchFallback = true;
			var waiting = Task.WhenAll(
			SetVersion(capabilities),
			CheckCapabilitiesAsync(rpc, "scantxoutset", v => capabilities.SupportScanUTXOSet = v, cancellationToken),
			CheckCapabilitiesAsync(rpc, "signrawtransactionwithkey", v => capabilities.SupportSignRawTransactionWith = v, cancellationToken),
			CheckCapabilitiesAsync(rpc, "testmempoolaccept", v => capabilities.SupportTestMempoolAccept = v, cancellationToken),
			CheckCapabilitiesAsync(rpc, "estimatesmartfee", v => capabilities.SupportEstimateSmartFee = v, cancellationToken),
			CheckCapabilitiesAsync(rpc, "generatetoaddress", v => capabilities.SupportGenerateToAddress = v, cancellationToken),
			CheckSegwitCapabilitiesAsync(rpc, v => capabilities.SupportSegwit = v, ScriptPubKeyType.Segwit, cancellationToken),
#pragma warning disable CS0618 // Type or member is obsolete
			CheckSegwitCapabilitiesAsync(rpc, v => capabilities.SupportTaproot = v, ScriptPubKeyType.TaprootBIP86, cancellationToken)
#pragma warning restore CS0618 // Type or member is obsolete
			);
			await rpc.SendBatchAsync().ConfigureAwait(false);
			await waiting.ConfigureAwait(false);
#if !NETSTANDARD1X
			Thread.MemoryBarrier();
#endif
			if (!capabilities.SupportGetNetworkInfo)
			{
#pragma warning disable CS0618 // Type or member is obsolete
				var getInfo = await SendCommandAsync(RPCOperations.getinfo);
#pragma warning restore CS0618 // Type or member is obsolete
				capabilities.Version = ((JObject)getInfo.Result)["version"].Value<int>();
			}
			Capabilities = capabilities;
			return capabilities;
		}

		private async Task SetVersion(RPCCapabilities capabilities)
		{
			try
			{
				var getInfo = await SendCommandAsync(RPCOperations.getnetworkinfo);
				capabilities.Version = ((JObject)getInfo.Result)["version"].Value<int>();
				capabilities.SupportGetNetworkInfo = true;
				return;
			}
			catch (RPCException ex) when (ex.RPCCode == RPCErrorCode.RPC_METHOD_NOT_FOUND || ex.RPCCode == RPCErrorCode.RPC_METHOD_DEPRECATED)
			{
				capabilities.SupportGetNetworkInfo = false;
			}
		}

		/// <summary>
		/// Run several RPC function to scan the RPC capabilities, then set RPCClient.RPCCapabilities
		/// </summary>
		/// <returns>The RPCCapabilities</returns>
		public RPCCapabilities ScanRPCCapabilities()
		{
			return ScanRPCCapabilitiesAsync().GetAwaiter().GetResult();
		}

		async static Task CheckSegwitCapabilitiesAsync(RPCClient rpc, Action<bool> setResult, ScriptPubKeyType type, CancellationToken cancellationToken)
		{
			if (type == ScriptPubKeyType.Segwit)
			{
				if (!rpc.Network.Consensus.SupportSegwit)
				{
					setResult(false);
					return;
				}
			}
#if HAS_SPAN
			if (type == ScriptPubKeyType.TaprootBIP86)
			{
				if (!rpc.Network.Consensus.SupportTaproot)
				{
					setResult(false);
					return;
				}
			}
#else
#pragma warning disable CS0618
			if (type == ScriptPubKeyType.TaprootBIP86)
#pragma warning restore CS0618
			{
				setResult(false);
				return;
			}
#endif
				var address = new Key().GetAddress(type, rpc.Network);
			if (address == null)
			{
				setResult(false);
				return;
			}
			try
			{
				var result = await rpc.SendCommandAsync("validateaddress", cancellationToken, address.ToString()).ConfigureAwait(false);
				result.ThrowIfError();
				setResult(result.Result["isvalid"].Value<bool>());
			}
			catch (RPCException ex)
			{
				setResult(ex.RPCCode == RPCErrorCode.RPC_TYPE_ERROR);
			}
			catch
			{
				setResult(false);
			}
		}

		private static async Task CheckCapabilities(Func<Task> command, Action<bool> setResult)
		{
			try
			{
				await command().ConfigureAwait(false);
				setResult(true);
			}
			catch (RPCException ex) when (ex.RPCCode == RPCErrorCode.RPC_METHOD_NOT_FOUND || ex.RPCCode == RPCErrorCode.RPC_METHOD_DEPRECATED)
			{
				setResult(false);
			}
			catch (RPCException)
			{
				setResult(true);
			}
			catch
			{
				setResult(false);
			}
		}
		private static Task CheckCapabilitiesAsync(RPCClient rpc, string command, Action<bool> setResult, CancellationToken cancellationToken)
		{
			return CheckCapabilities(() => rpc.SendCommandAsync(command, cancellationToken, "random"), setResult);
		}

		public static string GetDefaultCookieFilePath(Network network)
		{
			string path = null;
			if (!_DefaultPaths.TryGetValue(network, out path))
				throw new ArgumentException("This network has no default cookie file path registered, use RPCClient.RegisterDefaultCookiePath to register", "network");
			return path;
		}

		public static string TryGetDefaultCookieFilePath(Network network)
		{
			string path = null;
			if (!_DefaultPaths.TryGetValue(network, out path))
				return null;
			return path;
		}

		/// <summary>
		/// Create a new RPCClient instance
		/// </summary>
		/// <param name="authenticationString">username:password, the content of the .cookie file, or cookiefile=pathToCookieFile</param>
		/// <param name="hostOrUri"></param>
		/// <param name="network"></param>
		public RPCClient(string authenticationString, string hostOrUri, Network network)
			: this(authenticationString, BuildUri(hostOrUri, authenticationString, network.RPCPort), network)
		{
		}

		private static Uri BuildUri(string hostOrUri, string connectionString, int port)
		{
			RPCCredentialString connString;
			if (connectionString != null && RPCCredentialString.TryParse(connectionString, out connString))
			{
				if (connString.Server != null)
					hostOrUri = connString.Server;
			}
			if (hostOrUri != null)
			{
				hostOrUri = hostOrUri.Trim();
				try
				{
					if (hostOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
					   hostOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
						return new Uri(hostOrUri, UriKind.Absolute);
				}
				catch { }
			}
			hostOrUri = hostOrUri ?? "127.0.0.1";
			var indexOfPort = hostOrUri.IndexOf(":");
			if (indexOfPort != -1)
			{
				port = int.Parse(hostOrUri.Substring(indexOfPort + 1));
				hostOrUri = hostOrUri.Substring(0, indexOfPort);
			}
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
			: this(authenticationString == null ? null as RPCCredentialString : RPCCredentialString.Parse(authenticationString), address, network)
		{
		}

		public string Authentication
		{
			get
			{
				return _Authentication;
			}
		}

		ConcurrentQueue<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>> _BatchedRequests;

		public RPCClient PrepareBatch()
		{
			return new RPCClient(CredentialString, Address, Network)
			{
				_BatchedRequests = new ConcurrentQueue<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>>(),
				Capabilities = Capabilities,
				_HttpClient = _HttpClient,
				AllowBatchFallback = AllowBatchFallback
			};
		}
		public RPCClient Clone()
		{
			return new RPCClient(CredentialString, Address, Network)
			{
				_BatchedRequests = _BatchedRequests,
				Capabilities = Capabilities,
				_HttpClient = _HttpClient,
				AllowBatchFallback = AllowBatchFallback
			};
		}

		public RPCResponse SendCommand(RPCOperations commandName, params object[] parameters)
		{
			return SendCommand(commandName, default, parameters);
		}
		public RPCResponse SendCommand(string commandName, params object[] parameters)
		{
			return SendCommand(commandName, default, parameters);
		}
		public RPCResponse SendCommand(RPCOperations commandName, CancellationToken cancellationToken, params object[] parameters)
		{
			return SendCommand(commandName.ToString(), cancellationToken, parameters);
		}
		public RPCResponse SendCommand(string commandName, CancellationToken cancellationToken, params object[] parameters)
		{
			return SendCommandAsync(new RPCRequest(commandName, parameters), cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public BitcoinAddress GetNewAddress(CancellationToken cancellationToken = default)
		{
			return BitcoinAddress.Create(SendCommand(RPCOperations.getnewaddress, cancellationToken).Result.ToString(), Network);
		}

		public BitcoinAddress GetNewAddress(GetNewAddressRequest request)
		{
			return GetNewAddressAsync(request).GetAwaiter().GetResult();
		}

		public async Task<BitcoinAddress> GetNewAddressAsync(CancellationToken cancellationToken = default)
		{
			var result = await SendCommandAsync(RPCOperations.getnewaddress, cancellationToken).ConfigureAwait(false);
			return BitcoinAddress.Create(result.Result.ToString(), Network);
		}

		public async Task<BitcoinAddress> GetNewAddressAsync(GetNewAddressRequest request, CancellationToken cancellationToken = default)
		{
			var p = new Dictionary<string, object>();
			if (request != null)
			{
				if (request.Label != null)
				{
					p.Add("label", request.Label);
				}
				if (request.AddressType != null)
				{
					p.Add("address_type", request.AddressType.Value == AddressType.Bech32 ? "bech32" :
										  request.AddressType.Value == AddressType.Legacy ? "legacy" :
										  request.AddressType.Value == AddressType.P2SHSegwit ? "p2sh-segwit" :
										  throw new NotSupportedException(request.AddressType.Value.ToString())
										  );
				}
			}
			var getAddressResponse = await SendCommandWithNamedArgsAsync(RPCOperations.getnewaddress.ToString(), p, cancellationToken).ConfigureAwait(false);
			return BitcoinAddress.Create(getAddressResponse.Result.ToString(), Network);
		}

		public BitcoinAddress GetRawChangeAddress()
		{
			return GetRawChangeAddressAsync().GetAwaiter().GetResult();
		}

		public async Task<BitcoinAddress> GetRawChangeAddressAsync()
		{
			var result = await SendCommandAsync(RPCOperations.getrawchangeaddress).ConfigureAwait(false);
			return BitcoinAddress.Create(result.Result.ToString(), Network);
		}

		public Task<RPCResponse> SendCommandAsync(RPCOperations commandName, params object[] parameters)
		{
			return SendCommandAsync(commandName, CancellationToken.None, parameters);
		}

		public Task<RPCResponse> SendCommandAsync(RPCOperations commandName, CancellationToken cancellationToken, params object[] parameters)
		{
			return SendCommandAsync(commandName.ToString(), cancellationToken, parameters);
		}

		public RPCResponse SendCommandWithNamedArgs(string commandName, Dictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return SendCommandAsync(new RPCRequest() { Method = commandName, NamedParams = parameters }, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public Task<RPCResponse> SendCommandWithNamedArgsAsync(string commandName, Dictionary<string, object> parameters, CancellationToken cancellationToken)
		{
			return SendCommandAsync(new RPCRequest() { Method = commandName, NamedParams = parameters }, cancellationToken);
		}

		// Should we obsolete this non-cancellable async method?
		public Task<RPCResponse> SendCommandAsync(string commandName, params object[] parameters)
		{
			return SendCommandAsync(commandName, CancellationToken.None, parameters);
		}

		public Task<RPCResponse> SendCommandAsync(string commandName, CancellationToken cancellationToken, params object[] parameters)
		{
			return SendCommandAsync(new RPCRequest(commandName, parameters), cancellationToken: cancellationToken);
		}

		/// <summary>
		///	Send all commands in one batch
		/// </summary>
		public void SendBatch()
		{
			SendBatchAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		///	Cancel all commands
		/// </summary>
		public void CancelBatch()
		{
			var batches = _BatchedRequests;
			if (batches == null)
				throw new InvalidOperationException("This RPCClient instance is not a batch, use PrepareBatch");
			_BatchedRequests = null;
			Tuple<RPCRequest, TaskCompletionSource<RPCResponse>> req;
			while (batches.TryDequeue(out req))
			{
				req.Item2.TrySetCanceled();
			}
		}

		static object[] NoParams = new object[0];

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			await SendCommandAsync(RPCOperations.stop, cancellationToken).ConfigureAwait(false);
		}

		public void Stop(CancellationToken cancellationToken = default)
		{
			SendCommand(RPCOperations.stop, cancellationToken);
		}

		/// <summary>
		/// Returns the total uptime of the server.
		/// </summary>
		public TimeSpan Uptime()
		{
			return UptimeAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		/// Returns the total uptime of the server.
		/// </summary>
		public async Task<TimeSpan> UptimeAsync(CancellationToken cancellationToken = default)
		{
			var res = await SendCommandAsync(RPCOperations.uptime, cancellationToken).ConfigureAwait(false);
			return TimeSpan.FromSeconds(res.Result.Value<double>());
		}

		public ScanTxoutSetResponse StartScanTxoutSet(ScanTxoutSetParameters parameters, CancellationToken cancellationToken = default)
			=> StartScanTxoutSetAsync(parameters, cancellationToken).GetAwaiter().GetResult();

		public async Task<ScanTxoutSetResponse> StartScanTxoutSetAsync(ScanTxoutSetParameters parameters, CancellationToken cancellationToken = default)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			JArray descriptorsJson = new JArray();
			foreach (var descObj in parameters.Descriptors ?? new ScanTxoutDescriptor[0])
			{
				JObject descJson = new JObject();
				descJson.Add(new JProperty("desc", descObj.Descriptor.ToString()));
				if (descObj.Descriptor.IsRange())
				{
					var r = new JArray();
					if (descObj.Begin is null && descObj.End is int end)
					{
						descJson.Add(new JProperty("range", end));
					}
					if (descObj.Begin is int begin && descObj.End is int end2)
					{
						descJson.Add(new JProperty("range", new[] { begin, end2 }));
					}
				}
				descriptorsJson.Add(descJson);
			}

			var result = await SendCommandAsync(RPCOperations.scantxoutset, cancellationToken, "start", descriptorsJson).ConfigureAwait(false);
			result.ThrowIfError();

			var jobj = result.Result as JObject;
			var amount = Money.Coins(jobj.Property("total_amount").Value.Value<decimal>());
			var success = jobj.Property("success").Value.Value<bool>();
			//searched_items

			var searchedItems = (int)(jobj.Property("txouts") ?? jobj.Property("searched_items")).Value.Value<long>();
			var outputs = new List<ScanTxoutOutput>();
			foreach (var unspent in (jobj.Property("unspents").Value as JArray).OfType<JObject>())
			{
				OutPoint outpoint = OutPoint.Parse($"{unspent.Property("txid").Value.Value<string>()}-{(int)unspent.Property("vout").Value.Value<long>()}");
				var a = Money.Coins(unspent.Property("amount").Value.Value<decimal>());
				int height = (int)unspent.Property("height").Value.Value<long>();
				var scriptPubKey = Script.FromBytesUnsafe(Encoders.Hex.DecodeData(unspent.Property("scriptPubKey").Value.Value<string>()));
				outputs.Add(new ScanTxoutOutput()
				{
					Coin = new Coin(outpoint, new TxOut(a, scriptPubKey)),
					Height = height
				});
			}

			return new ScanTxoutSetResponse()
			{
				Outputs = outputs.ToArray(),
				TotalAmount = amount,
				Success = success,
				SearchedItems = searchedItems
			};
		}

		/// <summary>
		/// Get the progress report (in %) of the current scan
		/// </summary>
		/// <returns>The progress in %</returns>
		public async Task<decimal?> GetStatusScanTxoutSetAsync(CancellationToken cancellationToken = default)
		{
			var result = await SendCommandAsync(RPCOperations.scantxoutset, cancellationToken, "status", NoParams).ConfigureAwait(false);
			result.ThrowIfError();
			return (result.Result as JObject)?.Property("progress")?.Value?.Value<decimal>();
		}

		/// <summary>
		/// Get the progress report (in %) of the current scan
		/// </summary>
		/// <returns>The progress in %</returns>
		public decimal? GetStatusScanTxoutSet()
		{
			return GetStatusScanTxoutSetAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		/// Aborting the current scan
		/// </summary>
		/// <returns>Returns true when abort was successful</returns>
		public async Task<bool> AbortScanTxoutSetAsync(CancellationToken cancellationToken = default)
		{
			var result = await SendCommandAsync(RPCOperations.scantxoutset, cancellationToken, "abort", NoParams);
			result.ThrowIfError();
			return ((JValue)result.Result).Value<bool>();
		}

		/// <summary>
		/// Aborting the current scan
		/// </summary>
		/// <returns>Returns true when abort was successful</returns>
		public bool AbortScanTxoutSet()
		{
			return AbortScanTxoutSetAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		///	Send all commands in one batch
		/// </summary>
		public async Task SendBatchAsync(CancellationToken cancellationToken = default)
		{
			Tuple<RPCRequest, TaskCompletionSource<RPCResponse>> req;
			List<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>> requests = new List<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>>();
			var batches = _BatchedRequests;
			if (batches == null)
				throw new InvalidOperationException("This RPCClient instance is not a batch, use PrepareBatch");
			_BatchedRequests = null;
			while (batches.TryDequeue(out req))
			{
				requests.Add(req);
			}
			if (requests.Count == 0)
				return;
			await SendBatchAsyncCore(requests, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Since bitcoin core 0.20, if a RPC batch request is made, and one of the request fails due
		/// to permission issue (whitelisting feature), the whole RPC Batch would fail, throwing a
		/// HttpRequestException.
		///
		/// If we set AllowBatchFallback to true, we will fallback by sending all requests in the batch
		/// one by one.
		/// However, only use this for idempotent operations.
		/// When a batch operation fails because of permission issue, some of the requests in the batch
		/// may nevertheless have succeed without Bitcoin Core giving a clue about which one.
		/// </summary>
		public bool AllowBatchFallback { get; set; }

		private async Task SendBatchAsyncCore(List<Tuple<RPCRequest, TaskCompletionSource<RPCResponse>>> requests, CancellationToken cancellationToken)
		{
			var writer = new StringWriter();
			writer.Write("[");
			bool first = true;
			foreach (var item in requests)
			{
				if (!first)
				{
					writer.Write(",");
				}
				first = false;
				item.Item1.WriteJSON(writer);
			}
			writer.Write("]");
			writer.Flush();

			int responseIndex = 0;
			JArray response;
			try
			{
			retry:
				var webRequest = CreateWebRequest(writer.ToString());
				using (var httpResponse = await HttpClient.SendAsync(webRequest, cancellationToken).ConfigureAwait(false))
				{
					if (httpResponse.IsSuccessStatusCode)
					{
						response = JArray.Load(new JsonTextReader(
						new StreamReader(await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), NoBOMUTF8)));
						foreach (var jobj in response.OfType<JObject>())
						{
							try
							{
								RPCResponse rpcResponse = new RPCResponse(jobj);
								requests[responseIndex].Item2.TrySetResult(rpcResponse);
							}
							catch (Exception ex)
							{
								requests[responseIndex].Item2.TrySetException(ex);
							}
							responseIndex++;
						}
					}
					else
					{
						if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
						{
							if (TryRenewCookie())
								goto retry;
							httpResponse.EnsureSuccessStatusCode(); // Let's throw
						}
						// Bitcoin RPC 0.20 whitelisting feature throw a forbidden status
						// code if one of the message fail. So we fall back to un-batched
						// request. However, this might result in some requests being executed twice...
						if (AllowBatchFallback && httpResponse.StatusCode == HttpStatusCode.Forbidden)
						{
							foreach (var req in requests)
							{
								try
								{
									var resp = await SendCommandAsync(req.Item1, cancellationToken);
									req.Item2.TrySetResult(resp);
								}
								catch (Exception ex)
								{
									req.Item2.TrySetException(ex);
								}
							}
							return;
						}
						if (httpResponse.Content == null ||
							(httpResponse.Content.Headers.ContentLength == null || httpResponse.Content.Headers.ContentLength.Value == 0) ||
							!httpResponse.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.Ordinal))
						{
							httpResponse.EnsureSuccessStatusCode(); // Let's throw
						}
					}
				}
			}
			catch (Exception ex)
			{
				foreach (var item in requests)
				{
					item.Item2.TrySetException(ex);
				}
			}
			// Because TaskCompletionSources are executing on the threadpool adding a delay make sure they are all treated
			// when the function returns. Not quite useful, but make that when SendBatch, all tasks are finished running
			await Task.Delay(1);
		}

		private bool TryRenewCookie()
		{
			var cookiePath = GetCookiePath();
			if (cookiePath == null)
				return false;

#if !NOFILEIO
			try
			{
				var newCookie = File.ReadAllText(cookiePath);
				if (_Authentication == newCookie)
					return false;
				_Authentication = newCookie;
				return true;
			}
			//We are only interested into the previous exception
			catch
			{
				return false;
			}
#else
			throw new NotSupportedException("Cookie authentication is not supported for this platform");
#endif
		}

		static Encoding NoBOMUTF8 = new UTF8Encoding(false);
		public async Task<RPCResponse> SendCommandAsync(RPCRequest request, CancellationToken cancellationToken = default)
		{
			RPCResponse response = null;
			var batches = _BatchedRequests;
			if (batches != null)
			{
#if NO_RCA
				TaskCompletionSource<RPCResponse> source = new TaskCompletionSource<RPCResponse>();
#else
				TaskCompletionSource<RPCResponse> source = new TaskCompletionSource<RPCResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
				batches.Enqueue(Tuple.Create(request, source));
				response = await source.Task.ConfigureAwait(false);
				if (request.ThrowIfRPCError)
					response?.ThrowIfError();
			}

			if (response == null)
			{
				var writer = new StringWriter();
				request.WriteJSON(writer);
				writer.Flush();
				bool renewedCookie = false;
				TimeSpan retryTimeout = TimeSpan.FromSeconds(1.0);
				TimeSpan maxRetryTimeout = TimeSpan.FromSeconds(10.0);
			retry:
				var webRequest = CreateWebRequest(writer.ToString());
				using (var httpResponse = await HttpClient.SendAsync(webRequest, cancellationToken).ConfigureAwait(false))
				{
					if (httpResponse.IsSuccessStatusCode)
					{
						response = RPCResponse.Load(await httpResponse.Content.ReadAsStreamAsync());
						if (request.ThrowIfRPCError)
							response.ThrowIfError();
					}
					else
					{
						if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
						{
							if (!renewedCookie && TryRenewCookie())
							{
								renewedCookie = true;
								goto retry;
							}
							httpResponse.EnsureSuccessStatusCode(); // Let's throw
						}
						if (IsJson(httpResponse))
						{
							response = RPCResponse.Load(await httpResponse.Content.ReadAsStreamAsync());
							if (request.ThrowIfRPCError)
								response.ThrowIfError();
						}
						else if (await IsWorkQueueFull(httpResponse))
						{
							await Task.Delay(retryTimeout, cancellationToken);
							retryTimeout = TimeSpan.FromTicks(retryTimeout.Ticks * 2);
							if (retryTimeout > maxRetryTimeout)
								retryTimeout = maxRetryTimeout;
							goto retry;
						}
						else
						{
							httpResponse.EnsureSuccessStatusCode(); // Let's throw
						}
					}
				}
			}
			return response;
		}

		private bool IsJson(HttpResponseMessage httpResponse)
		{
			return httpResponse.Content?.Headers?.ContentType?.MediaType?.Equals("application/json", StringComparison.Ordinal) is true;
		}

		private async Task<bool> IsWorkQueueFull(HttpResponseMessage httpResponse)
		{
			// Changed in 22.0
			if (httpResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
				return true;
			if (httpResponse.StatusCode != HttpStatusCode.InternalServerError)
				return false;
			return (await httpResponse.Content?.ReadAsStringAsync())?.Equals("Work queue depth exceeded", StringComparison.Ordinal) is true;
		}

		private HttpRequestMessage CreateWebRequest(string json)
		{
			var address = Address.AbsoluteUri;
			if (!string.IsNullOrEmpty(CredentialString.WalletName))
			{
				if (!address.EndsWith("/"))
					address = address + "/";
				address += "wallet/" + CredentialString.WalletName;
			}
			var webRequest = new HttpRequestMessage(HttpMethod.Post, address);
			webRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Encoders.Base64.EncodeData(Encoders.ASCII.DecodeData(_Authentication)));
			webRequest.Content = new StringContent(json, NoBOMUTF8, "application/json-rpc");
			return webRequest;
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

			peers = GetPeersInfoAsync().GetAwaiter().GetResult();
			return peers;
		}

		public async Task<PeerInfo[]> GetPeersInfoAsync(CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync(RPCOperations.getpeerinfo, cancellationToken).ConfigureAwait(false);
			var peers = resp.Result as JArray;
			var result = new PeerInfo[peers.Count];
			var i = 0;
			foreach (var peer in peers)
			{
				var localAddr = (string)peer["addrlocal"];
				var pingWait = peer["pingwait"] != null ? (double)peer["pingwait"] : 0;

				localAddr = string.IsNullOrEmpty(localAddr) ? "127.0.0.1:8333" : localAddr;

				ulong services;
				if (!ulong.TryParse((string)peer["services"], out services))
				{
					services = Utils.ToUInt64(Encoders.Hex.DecodeData((string)peer["services"]), false);
				}

				Utils.TryParseEndpoint((string)peer["addr"], this.Network.DefaultPort, out var addressEnpoint);
				Utils.TryParseEndpoint(localAddr, this.Network.DefaultPort, out var localEndpoint);

				result[i++] = new PeerInfo
				{
					Id = (int)peer["id"],
					Address = addressEnpoint,
					LocalAddress = localEndpoint,
					Services = (NodeServices)services,
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
					IsWhiteListed = peer["whitelisted"] != null ? (bool)peer["whitelisted"] : false,
					BanScore = peer["banscore"] == null ? 0 : (int)peer["banscore"],
					Permissions = peer["permissions"] is JArray permissions ? permissions.Select(p => p.Value<string>()).ToArray() : new string[0],
					Inflight = peer["inflight"].Select(x => uint.Parse((string)x)).ToArray()
				};
			}
			return result;
		}

		public void AddNode(EndPoint nodeEndPoint, bool onetry = false)
		{
			if (nodeEndPoint == null)
				throw new ArgumentNullException(nameof(nodeEndPoint));
			SendCommand("addnode", nodeEndPoint.ToString(), onetry ? "onetry" : "add");
		}

		public async Task AddNodeAsync(EndPoint nodeEndPoint, bool onetry = false, CancellationToken cancellationToken = default)
		{
			if (nodeEndPoint == null)
				throw new ArgumentNullException(nameof(nodeEndPoint));
			await SendCommandAsync(RPCOperations.addnode, cancellationToken, nodeEndPoint.ToString(), onetry ? "onetry" : "add").ConfigureAwait(false);
		}

		public void RemoveNode(EndPoint nodeEndPoint)
		{
			if (nodeEndPoint == null)
				throw new ArgumentNullException(nameof(nodeEndPoint));
			SendCommandAsync(RPCOperations.addnode, nodeEndPoint.ToString(), "remove");
		}

		public async Task RemoveNodeAsync(EndPoint nodeEndPoint, CancellationToken cancellationToken = default)
		{
			if (nodeEndPoint == null)
				throw new ArgumentNullException(nameof(nodeEndPoint));
			await SendCommandAsync(RPCOperations.addnode, cancellationToken, nodeEndPoint.ToString(), "remove").ConfigureAwait(false);
		}

		public async Task<AddedNodeInfo[]> GetAddedNodeInfoAsync(bool detailed, CancellationToken cancellationToken = default)
		{
			var result = await SendCommandAsync(RPCOperations.getaddednodeinfo, cancellationToken, detailed).ConfigureAwait(false);
			var obj = result.Result;
			return obj.Select(entry => new AddedNodeInfo
			{
				AddedNode = Utils.ParseEndpoint((string)entry["addednode"], 8333),
				Connected = (bool)entry["connected"],
				Addresses = entry["addresses"].Select(x => new NodeAddressInfo
				{
					Address = Utils.ParseEndpoint((string)x["address"], 8333) as IPEndPoint,
					Connected = (bool)x["connected"]
				})
			}).ToArray();
		}

		public AddedNodeInfo[] GetAddedNodeInfo(bool detailed)
		{
			AddedNodeInfo[] addedNodesInfo = null;

			addedNodesInfo = GetAddedNodeInfoAsync(detailed).GetAwaiter().GetResult();
			return addedNodesInfo;
		}

		public AddedNodeInfo GetAddedNodeInfo(bool detailed, EndPoint nodeEndPoint)
		{
			AddedNodeInfo addedNodeInfo = null;

			addedNodeInfo = GetAddedNodeInfoAync(detailed, nodeEndPoint).GetAwaiter().GetResult();
			return addedNodeInfo;
		}

		public async Task<AddedNodeInfo> GetAddedNodeInfoAync(bool detailed, EndPoint nodeEndPoint, CancellationToken cancellationToken = default)
		{
			if (nodeEndPoint == null)
				throw new ArgumentNullException(nameof(nodeEndPoint));

			try
			{

				var result = await SendCommandAsync(RPCOperations.getaddednodeinfo, cancellationToken, detailed, nodeEndPoint.ToString()).ConfigureAwait(false);
				var e = result.Result;
				return e.Select(entry => new AddedNodeInfo
				{
					AddedNode = Utils.ParseEndpoint((string)entry["addednode"], 8333),
					Connected = (bool)entry["connected"],
					Addresses = entry["addresses"].Select(x => new NodeAddressInfo
					{
						Address = Utils.ParseEndpoint((string)x["address"], 8333) as IPEndPoint,
						Connected = (bool)x["connected"]
					})
				}).FirstOrDefault();
			}
			catch (RPCException ex)
			{
				if (ex.RPCCode == RPCErrorCode.RPC_CLIENT_NODE_NOT_ADDED)
					return null;
				throw;
			}
		}
#endif

#endregion

#region Block chain and UTXO

		public async Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken = default)
		{
			var response = await SendCommandAsync(RPCOperations.getblockchaininfo, cancellationToken).ConfigureAwait(false);
			var result = response.Result;

			var epochToDtateTimeOffset = new Func<long, DateTimeOffset>(epoch =>
			{
				try
				{
					return Utils.UnixTimeToDateTime(epoch);
				}
				catch (OverflowException)
				{
					return DateTimeOffset.MaxValue;
				}
			});

			JToken softForksToken = result["softforks"];
			List<SoftFork> softForks;
			List<Bip9SoftFork> bip9SoftForks;
			try
			{
				softForks = softForksToken
					?.Cast<JProperty>()
					?.Select(x =>
						new SoftFork
						{
							Bip = x.Name,
							ForkType = x.Value.Value<string>("type"),
							Activated = x.Value.Value<bool>("activated"),
							Height = x.Value.Value<uint>("height")
						})
					?.ToList();

				bip9SoftForks = Enumerable.Empty<Bip9SoftFork>().ToList();
			}
			catch (InvalidCastException)
			{
				// Then the client may be pre Biitcoin Core 19, so Ensure backwards compatibility.
				softForks = softForksToken
					?.Cast<JObject>()
					?.Select(x =>
						new SoftFork
						{
							Bip = x.Value<string>("id")
						})
					?.ToList();

				bip9SoftForks = result["bip9_softforks"]
					?.Cast<JProperty>()
					?.Select(x =>
						new Bip9SoftFork
						{
							Name = x.Name,
							Status = x.Value.Value<string>("status"),
							StartTime = epochToDtateTimeOffset(x.Value.Value<long>("startTime")),
							Timeout = epochToDtateTimeOffset(x.Value.Value<long>("timeout")),
							SinceHeight = x.Value.Value<ulong?>("since") ?? 0
						})
					?.ToList();
			}

#pragma warning disable CS0612 // Type or member is obsolete
			var blockchainInfo = new BlockchainInfo
			{
				Chain = Network.GetNetwork(result.Value<string>("chain")),
				Blocks = result.Value<ulong>("blocks"),
				Headers = result.Value<ulong>("headers"),
				BestBlockHash = new uint256(result.Value<string>("bestblockhash")), // the block hash
				Difficulty = result.Value<ulong>("difficulty"),
				MedianTime = result.Value<ulong>("mediantime"),
				VerificationProgress = result.Value<float>("verificationprogress"),
				InitialBlockDownload = result.Value<bool?>("initialblockdownload") ?? false,
				ChainWork = new uint256(result.Value<string>("chainwork")),
				SizeOnDisk = result.Value<ulong?>("size_on_disk") ?? 0,
				Pruned = result.Value<bool>("pruned"),
				SoftForks = softForks,
				Bip9SoftForks = bip9SoftForks
			};
#pragma warning restore CS0612 // Type or member is obsolete

			return blockchainInfo;
		}

		public BlockchainInfo GetBlockchainInfo()
		{
			return GetBlockchainInfoAsync().Result;
		}

		public uint256 GetBestBlockHash()
		{
			return uint256.Parse((string)SendCommand(RPCOperations.getbestblockhash).Result);
		}

		public async Task<uint256> GetBestBlockHashAsync(CancellationToken cancellationToken = default)
		{
			var bestBlockHashResponse = await SendCommandAsync(RPCOperations.getbestblockhash, cancellationToken).ConfigureAwait(false);
			return uint256.Parse((string)(bestBlockHashResponse.Result));
		}

		public BlockHeader GetBlockHeader(int height)
		{
			var hash = GetBlockHash(height);
			return GetBlockHeader(hash);
		}

		public BlockHeader GetBlockHeader(uint height)
		{
			var hash = GetBlockHash(height);
			return GetBlockHeader(hash);
		}

		public async Task<BlockHeader> GetBlockHeaderAsync(int height, CancellationToken cancellationToken = default)
		{
			var hash = await GetBlockHashAsync(height, cancellationToken).ConfigureAwait(false);
			return await GetBlockHeaderAsync(hash, cancellationToken).ConfigureAwait(false);
		}

		public async Task<BlockHeader> GetBlockHeaderAsync(uint height, CancellationToken cancellationToken = default)
		{
			var hash = await GetBlockHashAsync(height, cancellationToken).ConfigureAwait(false);
			return await GetBlockHeaderAsync(hash, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public async Task<Block> GetBlockAsync(uint256 blockId, CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync(RPCOperations.getblock, cancellationToken, blockId, false).ConfigureAwait(false);
			return Block.Parse(resp.Result.ToString(), Network);
		}

		/// <summary>
		/// Get the a whole block
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public Block GetBlock(uint256 blockId)
		{
			return GetBlockAsync(blockId).GetAwaiter().GetResult();
		}

		public Block GetBlock(int height)
		{
			return GetBlockAsync(height).GetAwaiter().GetResult();
		}

		public Block GetBlock(uint height)
		{
			return GetBlockAsync(height).GetAwaiter().GetResult();
		}

		public async Task<Block> GetBlockAsync(int height, CancellationToken cancellationToken = default)
		{
			var hash = await GetBlockHashAsync(height, cancellationToken).ConfigureAwait(false);
			return await GetBlockAsync(hash, cancellationToken).ConfigureAwait(false);
		}

		public async Task<Block> GetBlockAsync(uint height, CancellationToken cancellationToken = default)
		{
			var hash = await GetBlockHashAsync(height, cancellationToken).ConfigureAwait(false);
			return await GetBlockAsync(hash, cancellationToken).ConfigureAwait(false);
		}

		public BlockHeader GetBlockHeader(uint256 blockHash)
		{
			var resp = SendCommand("getblockheader", blockHash, false);
			return ParseBlockHeader(resp);
		}

		public async Task<BlockHeader> GetBlockHeaderAsync(uint256 blockHash, CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync("getblockheader", cancellationToken, blockHash, false).ConfigureAwait(false);
			return ParseBlockHeader(resp);
		}

		private BlockHeader ParseBlockHeader(RPCResponse resp)
		{
			var header = Network.Consensus.ConsensusFactory.CreateBlockHeader();
			var hex = Encoders.Hex.DecodeData(resp.Result.Value<string>());
			header.ReadWrite(new BitcoinStream(hex));
			return header;
		}

		public GetBlockRPCResponse GetBlock(uint256 blockHash, GetBlockVerbosity verbosity)
		{
			return GetBlockAsync(blockHash, verbosity).GetAwaiter().GetResult();
		}

		public async Task<GetBlockRPCResponse> GetBlockAsync(uint256 blockHash, GetBlockVerbosity verbosity, CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync("getblock", cancellationToken, blockHash, (int)verbosity).ConfigureAwait(false);
			return ParseVerboseBlock(resp, (int)verbosity);
		}

		private GetBlockRPCResponse ParseVerboseBlock(RPCResponse resp, int verbosity)
		{
			var json = (JObject)resp.Result;
			var blockHeader = Network.Consensus.ConsensusFactory.CreateBlockHeader();
			blockHeader.Bits = new Target(Encoders.Hex.DecodeData(json.Value<string>("bits")));
			blockHeader.Version = json.Value<int>("version");
			blockHeader.HashMerkleRoot = new uint256(json.Value<string>("merkleroot"));
			blockHeader.BlockTime = Utils.UnixTimeToDateTime(json.Value<uint>("time"));
			blockHeader.Nonce = json.Value<uint>("nonce");

			// prevblock field does not exist for the genesis.
			if (json.TryGetValue("previousblockhash", StringComparison.Ordinal, out var prevBlockHash))
			{
				blockHeader.HashPrevBlock = uint256.Parse(prevBlockHash.ToString());
			}
			else
			{
				blockHeader.HashPrevBlock = null;
			}
			// nextblockhash field does not exist for the chain tip.
			uint256 nextBlockHash = null;
			if (json.TryGetValue("nextblockhash", StringComparison.Ordinal, out var nextBlockHashHex))
			{
				nextBlockHash = uint256.Parse(nextBlockHashHex.ToString());
			}

			Block block = null;
			var txids = new List<uint256>();
			if (verbosity == 2)
			{
				var txs = new List<Transaction>();
				foreach (var txInfo in json.Value<JArray>("tx"))
				{

					var tx = ParseTxHex(txInfo.Value<string>("hex"));
					txs.Add(tx);
					txids.Add(tx.GetHash());
				}
				block = Network.Consensus.ConsensusFactory.CreateBlock();
				block.Header = blockHeader;
				block.Transactions = txs;
				if (!block.GetMerkleRoot().Hash.Equals(blockHeader.HashMerkleRoot))
				{
					throw new FormatException($"Bogus GetBlockRPCResponse! merkle root mistmach (expected: {blockHeader.HashMerkleRoot}. actual: {block.GetMerkleRoot().Hash})");
				}
			}
			else if (verbosity == 1)
			{
				foreach (var tx in json.Value<JArray>("tx"))
				{
					txids.Add(uint256.Parse(tx.ToString()));
				}
			}
			else
			{
				throw new Exception("Unreachable!");
			}

			var nTx = json.Value<int>("nTx");
			if (nTx != txids.Count)
			{
				throw new FormatException($"Bogus GetBlockRPCResponse! nTx mismatch (expected: {nTx}. actual: {txids.Count})");
			}
			return new GetBlockRPCResponse()
			{
				Confirmations = json.Value<int>("confirmations"),
				Size = json.Value<int>("size"),
				StrippedSize = json.Value<int>("strippedsize"),
				Weight = json.Value<int>("weight"),
				Height = json.Value<int>("height"),
				VersionHex = json.Value<string>("versionHex"),
				MedianTimeUnix = json.Value<uint>("mediantime"),
				Difficulty = json.Value<double>("difficulty"),
				ChainWork = uint256.Parse(json.Value<string>("chainwork")),
				NextBlockHash = nextBlockHash,
				Block = block,
				Header = blockHeader,
				TxIds = txids,
			};
		}

		public uint256 GetBlockHash(int height)
		{
			var resp = SendCommand(RPCOperations.getblockhash, height);
			return uint256.Parse(resp.Result.ToString());
		}

		public uint256 GetBlockHash(uint height)
		{
			var resp = SendCommand(RPCOperations.getblockhash, height);
			return uint256.Parse(resp.Result.ToString());
		}

		public async Task<uint256> GetBlockHashAsync(int height, CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync(RPCOperations.getblockhash, cancellationToken, height).ConfigureAwait(false);
			return uint256.Parse(resp.Result.ToString());
		}

		public async Task<uint256> GetBlockHashAsync(uint height, CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync(RPCOperations.getblockhash, cancellationToken, height).ConfigureAwait(false);
			return uint256.Parse(resp.Result.ToString());
		}

		/// <summary>
		/// Retrieve a BIP 157 content filter for a particular block.
		/// </summary>
		/// <param name="blockHash">The hash of the block.</param>
		public BlockFilter GetBlockFilter(uint256 blockHash)
		{
			return GetBlockFilterAsync(blockHash).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Retrieve a BIP 157 content filter for a particular block.
		/// </summary>
		/// <param name="blockHash">The hash of the block.</param>
		public async Task<BlockFilter> GetBlockFilterAsync(uint256 blockHash, CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync(RPCOperations.getblockfilter, cancellationToken, blockHash, "basic").ConfigureAwait(false);
			return ParseCompactFilter(resp);
		}

		private BlockFilter ParseCompactFilter(RPCResponse resp)
		{
			var json = (JObject)resp.Result;
			return new BlockFilter(
				GolombRiceFilter.Parse(json.Value<string>("filter")),
				uint256.Parse(json.Value<string>("header"))
			);
		}

		public BlockStats GetBlockStats(uint256 blockHash, string[] stats = null)
		{
			return GetBlockStatsAsync(blockHash, stats).GetAwaiter().GetResult();
		}

		public Task<BlockStats> GetBlockStatsAsync(uint256 blockHash, CancellationToken cancellationToken = default)
		{
			return GetBlockStatsAsync(blockHash, null, cancellationToken);
		}

		public async Task<BlockStats> GetBlockStatsAsync(uint256 blockHash, string[] stats, CancellationToken cancellationToken = default)
		{
			var resp = await SendCommandAsync(RPCOperations.getblockstats, cancellationToken, blockHash, stats).ConfigureAwait(false);

			return new BlockStats
			{
				AvgFee = Money.Satoshis(resp.Result.Value<ulong>("avgfee")),
				AvgFeeRate = Money.Satoshis(resp.Result.Value<ulong>("avgfeerate")),
				AvgTxSize = resp.Result.Value<int>("avgtxsize"),
				BlockHash = blockHash,
				FeeRatePercentiles = resp.Result["feerate_percentiles"] == null ? null : resp.Result.Value<JArray>("feerate_percentiles").Select(x => Money.Satoshis(x.Value<int>())).ToArray(),
				Height = resp.Result.Value<int>("height"),
				Ins = resp.Result.Value<int>("ins"),
				MaxFee = Money.Satoshis(resp.Result.Value<ulong>("maxfee")),
				MaxFeeRate = Money.Satoshis(resp.Result.Value<ulong>("maxfeerate")),
				MaxTxSize = resp.Result.Value<int>("maxtxsize"),
				MedianFee = Money.Satoshis(resp.Result.Value<ulong>("medianfee")),
				MedianTime = Utils.UnixTimeToDateTime(resp.Result.Value<ulong>("mediantime")),
				MedianTxSize = resp.Result.Value<int>("maxtxsize"),
				MinFee = Money.Satoshis(resp.Result.Value<ulong>("minfee")),
				MinFeeRate = Money.Satoshis(resp.Result.Value<ulong>("minfeerate")),
				MinTxSize = resp.Result.Value<int>("mintxsize"),
				Outs = resp.Result.Value<int>("outs"),
				Subsidy = resp.Result.Value<long>("outs"),
				SWTotalSize = resp.Result.Value<long>("swtotal_size"),
				SWTotalWeight = resp.Result.Value<long>("swtotal_weight"),
				SWTxs = resp.Result.Value<int>("swtxs"),
				Time = Utils.UnixTimeToDateTime(resp.Result.Value<ulong>("time")),
				TotalFee = Money.Satoshis(resp.Result.Value<ulong>("totalfee")),
				TotalOut = resp.Result.Value<long>("total_out"),
				TotalSize = resp.Result.Value<long>("total_size"),
				TotalWeight = resp.Result.Value<long>("total_weight"),
				Txs = resp.Result.Value<int>("txs"),
				UTXOIncrease = resp.Result.Value<int>("utxo_increase"),
				UTXOSizeInc = resp.Result.Value<int>("utxo_size_inc"),
			};
		}

		public int GetBlockCount()
		{
			return (int)SendCommand(RPCOperations.getblockcount).Result;
		}

		public async Task<int> GetBlockCountAsync(CancellationToken cancellationToken = default)
		{
			var getBlockCountResponse = await SendCommandAsync(RPCOperations.getblockcount, cancellationToken).ConfigureAwait(false);
			return (int)(getBlockCountResponse.Result);
		}

		public MemPoolInfo GetMemPool()
		{
			return this.GetMemPoolAsync().GetAwaiter().GetResult();
		}

		public async Task<MemPoolInfo> GetMemPoolAsync(CancellationToken cancellationToken = default)
		{
			var response = await SendCommandAsync(RPCOperations.getmempoolinfo, cancellationToken);

			static IEnumerable<FeeRateGroup> ExtractFeeRateGroups(JToken jt) =>
				jt switch
				{
					JObject jo => jo.Properties()
						.Where(p => p.Name != "total_fees")
						.Select(p => new FeeRateGroup
						{
							Group = int.Parse(p.Name),
							Sizes = p.Value.Value<ulong>("sizes"),
							Count = p.Value.Value<uint>("count"),
							Fees = Money.Satoshis(p.Value.Value<ulong>("fees")),
							From = new FeeRate(Money.Satoshis(p.Value.Value<ulong>("from_feerate"))),
							To = new FeeRate(Money.Satoshis(p.Value.Value<ulong>("to_feerate")))
						}),
					_ => Enumerable.Empty<FeeRateGroup>()
				};

			return new MemPoolInfo()
			{
				Size = Int32.Parse((string)response.Result["size"], CultureInfo.InvariantCulture),
				Bytes = Int32.Parse((string)response.Result["bytes"], CultureInfo.InvariantCulture),
				Usage = Int32.Parse((string)response.Result["usage"], CultureInfo.InvariantCulture),
				MaxMemPool = Double.Parse((string)response.Result["maxmempool"], CultureInfo.InvariantCulture),
				MemPoolMinFee = Double.Parse((string)response.Result["mempoolminfee"], CultureInfo.InvariantCulture),
				MinRelayTxFee = Double.Parse((string)response.Result["minrelaytxfee"], CultureInfo.InvariantCulture),
				Histogram = ExtractFeeRateGroups(response.Result["fee_histogram"]).ToArray()
			};
		}

		public uint256[] GetRawMempool()
		{
			var result = SendCommand(RPCOperations.getrawmempool);
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(uint256.Parse).ToArray();
		}

		public async Task<uint256[]> GetRawMempoolAsync(CancellationToken cancellationToken = default)
		{
			var result = await SendCommandAsync(RPCOperations.getrawmempool, cancellationToken).ConfigureAwait(false);
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(uint256.Parse).ToArray();
		}

		public MempoolEntry GetMempoolEntry(uint256 txid, bool throwIfNotFound = true)
		{
			return GetMempoolEntryAsync(txid, throwIfNotFound).GetAwaiter().GetResult();
		}

		public async Task<MempoolEntry> GetMempoolEntryAsync(uint256 txid, bool throwIfNotFound = true, CancellationToken cancellationToken = default)
		{
			var request = new RPCRequest(RPCOperations.getmempoolentry, new[] { txid });
			request.ThrowIfRPCError = throwIfNotFound;
			var response = await SendCommandAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
			if (throwIfNotFound)
				response.ThrowIfError();
			if (response.Error != null && response.Error.Code == RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY)
				return null;

			var jobj = (JObject)response.Result;
			return new MempoolEntry
			{
				TransactionId = txid,
				VirtualSizeBytes = (jobj["size"] ?? jobj["vsize"]).Value<int>(),
				Time = Utils.UnixTimeToDateTime(jobj["time"].Value<long>()),
				Height = jobj["height"].Value<int>(),
				DescendantCount = jobj["descendantcount"].Value<int>(),
				DescendantVirtualSizeBytes = jobj["descendantsize"].Value<int>(),
				AncestorCount = jobj["ancestorcount"].Value<int>(),
				AncestorVirtualSizeBytes = jobj["ancestorsize"].Value<int>(),
				TransactionIdWithWitness = uint256.Parse((string)jobj["wtxid"]),
				BaseFee = new Money(jobj["fees"]["base"].Value<decimal>(), MoneyUnit.BTC),
				ModifiedFee = new Money(jobj["fees"]["modified"].Value<decimal>(), MoneyUnit.BTC),
				DescendantFees = new Money(jobj["fees"]["descendant"].Value<decimal>(), MoneyUnit.BTC),
				AncestorFees = new Money(jobj["fees"]["ancestor"].Value<decimal>(), MoneyUnit.BTC),
				Depends = jobj["depends"]?.Select(x => uint256.Parse((string)x)).ToArray(),
				SpentBy = jobj["spentby"]?.Select(x => uint256.Parse((string)x)).ToArray()
			};
		}

		public async Task SaveMempoolAsync(CancellationToken cancellationToken = default)
		{
			await SendCommandAsync(RPCOperations.savemempool, cancellationToken).ConfigureAwait(false);
		}

		public void SaveMempool()
		{
			SaveMempoolAsync().GetAwaiter().GetResult();
		}

#nullable enable
		public MempoolAcceptResult TestMempoolAccept(Transaction transaction, TestMempoolParameters? parameters, CancellationToken cancellationToken = default)
		{
			return TestMempoolAcceptAsync(transaction, parameters, cancellationToken).GetAwaiter().GetResult();
		}
		public MempoolAcceptResult TestMempoolAccept(Transaction transaction, CancellationToken cancellationToken = default)
		{
			return TestMempoolAcceptAsync(transaction, null, cancellationToken).GetAwaiter().GetResult();
		}

		public async Task<MempoolAcceptResult> TestMempoolAcceptAsync(Transaction transaction, CancellationToken cancellationToken = default)
		{
			return await TestMempoolAcceptAsync(transaction, null, cancellationToken).ConfigureAwait(false);
		}

		public async Task<MempoolAcceptResult> TestMempoolAcceptAsync(Transaction transaction, TestMempoolParameters? parameters, CancellationToken cancellationToken = default)
		{
			RPCResponse response;
			if (parameters?.MaxFeeRate is FeeRate feeRate)
			{
				try
				{
					var feeRateDecimal = feeRate.FeePerK.ToDecimal(MoneyUnit.Satoshi);
					response = await SendCommandAsync(RPCOperations.testmempoolaccept, cancellationToken, new[] { transaction.ToHex() }, feeRateDecimal).ConfigureAwait(false);
				}
				catch (RPCException ex) when (ex.Message == "Expected type bool, got number")
				{
					response = await SendCommandAsync(RPCOperations.testmempoolaccept, cancellationToken, new[] { transaction.ToHex() }).ConfigureAwait(false);
				}
			}
			else
			{
				response = await SendCommandAsync(RPCOperations.testmempoolaccept, cancellationToken, new[] { new[] { transaction.ToHex() } }).ConfigureAwait(false);
			}

			var first = response.Result[0]!;
			var allowed = first["allowed"]!.Value<bool>();

			RejectCode rejectedCode = RejectCode.INVALID;
			var rejectedReason = string.Empty;
			if (!allowed)
			{
				var rejected = first["reject-reason"]!.Value<string>();
				var separatorIdx = rejected!.IndexOf(':');
				if (separatorIdx != -1)
				{
					rejectedCode = (RejectCode)int.Parse(rejected.Substring(0, separatorIdx));
					rejectedReason = rejected.Substring(separatorIdx + 2);
				}
				else
				{
					rejectedReason = rejected;
				}
			}
			return new MempoolAcceptResult
			{
				TxId = uint256.Parse(first["txid"]!.Value<string>()),
				IsAllowed = allowed,
				RejectCode = rejectedCode,
				RejectReason = rejectedReason
			};
		}
#nullable restore
		/// <summary>
		/// Returns details about an unspent transaction output.
		/// </summary>
		/// <param name="txid">The transaction id</param>
		/// <param name="index">vout number</param>
		/// <param name="includeMempool">Whether to include the mempool. Note that an unspent output that is spent in the mempool won't appear.</param>
		/// <returns>null if spent or never existed</returns>
		public GetTxOutResponse GetTxOut(uint256 txid, int index, bool includeMempool = true)
		{
			return GetTxOutAsync(txid, index, includeMempool).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Returns details about an unspent transaction output.
		/// </summary>
		/// <param name="txid">The transaction id</param>
		/// <param name="index">vout number</param>
		/// <param name="includeMempool">Whether to include the mempool. Note that an unspent output that is spent in the mempool won't appear.</param>
		/// <returns>null if spent or never existed</returns>
		public async Task<GetTxOutResponse> GetTxOutAsync(uint256 txid, int index, bool includeMempool = true, CancellationToken cancellationToken = default)
		{
			var response = await SendCommandAsync(RPCOperations.gettxout, cancellationToken, txid, index, includeMempool).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(response?.ResultString))
			{
				return null;
			}

			var result = response.Result;
			var value = result.Value<decimal>("value"); // The transaction value in BTC
			var txOut = new TxOut(Money.Coins(value), new Script(result["scriptPubKey"].Value<string>("asm")));

			return new GetTxOutResponse
			{
				BestBlock = new uint256(result.Value<string>("bestblock")), // the block hash
				Confirmations = result.Value<int>("confirmations"), // The number of confirmations
				IsCoinBase = result.Value<bool>("coinbase"), // Coinbase or not
				ScriptPubKeyType = result["scriptPubKey"].Value<string>("type"),  // The type, eg pubkeyhash
				TxOut = txOut
			};
		}

		/// <summary>
		/// Returns statistics about the unspent transaction output (UTXO) set
		/// </summary>
		/// <returns>Parsed object containing all info</returns>
		public GetTxOutSetInfoResponse GetTxoutSetInfo()
		{
			return GetTxoutSetInfoAsync().GetAwaiter().GetResult();
		}

		public async Task<GetTxOutSetInfoResponse> GetTxoutSetInfoAsync(CancellationToken cancellationToken = default)
		{
			var response = await SendCommandAsync(RPCOperations.gettxoutsetinfo, cancellationToken).ConfigureAwait(false);

			var result = response.Result;
			return new GetTxOutSetInfoResponse
			{
				Height = result.Value<int>("height"),
				Bestblock = uint256.Parse(result.Value<string>("bestblock")),
				Transactions = result.Value<int>("transactions"),
				Txouts = result.Value<long>("txouts"),
				Bogosize = result.Value<long>("bogosize"),
				HashSerialized2 = result.Value<string>("hash_serialized_2"),
				DiskSize = result.Value<long>("disk_size"),
				TotalAmount = Money.FromUnit(result.Value<decimal>("total_amount"), MoneyUnit.BTC)
			};
		}

		/// <summary>
		/// GetTransactions only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="blockHash"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public IEnumerable<Transaction> GetTransactions(uint256 blockHash, CancellationToken cancellationToken = default)
		{
			if (blockHash == null)
				throw new ArgumentNullException(nameof(blockHash));

			var resp = SendCommand(RPCOperations.getblock, cancellationToken, blockHash);

			var tx = resp.Result["tx"] as JArray;
			if (tx != null)
			{
				foreach (var item in tx)
				{
					var result = GetRawTransaction(uint256.Parse(item.ToString()), false);
					if (result != null)
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
			return ParseTxHex(rawHex);
		}

		public Transaction DecodeRawTransaction(byte[] raw)
		{
			return DecodeRawTransaction(Encoders.Hex.EncodeData(raw));
		}
		public Task<Transaction> DecodeRawTransactionAsync(string rawHex)
		{
			return Task.FromResult(ParseTxHex(rawHex));
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
			return GetRawTransactionAsync(txid, throwIfNotFound).GetAwaiter().GetResult();
		}

		public Task<Transaction> GetRawTransactionAsync(uint256 txid, bool throwIfNotFound = true, CancellationToken cancellationToken = default)
		{
			return GetRawTransactionAsync(txid, null, throwIfNotFound, cancellationToken);
		}

		public Transaction GetRawTransaction(uint256 txid, uint256 blockId, bool throwIfNotFound = true)
		{
			return GetRawTransactionAsync(txid, blockId, throwIfNotFound).GetAwaiter().GetResult();
		}

		public async Task<Transaction> GetRawTransactionAsync(uint256 txid, uint256 blockId, bool throwIfNotFound = true, CancellationToken cancellationToken = default)
		{
			List<object> args = new List<object>(3);
			args.Add(txid);
			args.Add(0);
			if (blockId != null)
				args.Add(blockId);
			var response = await SendCommandAsync(new RPCRequest(RPCOperations.getrawtransaction, args.ToArray()) { ThrowIfRPCError = throwIfNotFound }, cancellationToken).ConfigureAwait(false);
			if (throwIfNotFound)
				response.ThrowIfError();
			if (response.Error != null && response.Error.Code == RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY)
				return null;

			response.ThrowIfError();
			var tx = Network.Consensus.ConsensusFactory.CreateTransaction();
			tx.ReadWrite(Encoders.Hex.DecodeData(response.Result.ToString()), Network);
			return tx;
		}

		public RawTransactionInfo GetRawTransactionInfo(uint256 txid)
		{
			return GetRawTransactionInfoAsync(txid).GetAwaiter().GetResult();
		}

		private Transaction ParseTxHex(string hex)
		{
			var tx = Network.Consensus.ConsensusFactory.CreateTransaction();
			tx.ReadWrite(Encoders.Hex.DecodeData(hex), Network);
			return tx;
		}

		public async Task<RawTransactionInfo> GetRawTransactionInfoAsync(uint256 txId, CancellationToken cancellationToken = default)
		{
			var request = new RPCRequest(RPCOperations.getrawtransaction, new object[] { txId, true });
			var response = await SendCommandAsync(request, cancellationToken: cancellationToken);
			var json = response.Result;

			return new RawTransactionInfo
			{
				Transaction = ParseTxHex(json.Value<string>("hex")),
				TransactionId = uint256.Parse(json.Value<string>("txid")),
				TransactionTime = json["time"] != null ? NBitcoin.Utils.UnixTimeToDateTime(json.Value<long>("time")) : (DateTimeOffset?)null,
				Hash = json["hash"] is JToken token ? uint256.Parse(token.Value<string>()) : null,
				Size = json.Value<uint>("size"),
				VirtualSize = json.Value<uint>("vsize"),
				Version = json.Value<uint>("version"),
				LockTime = new LockTime(json.Value<uint>("locktime")),
				BlockHash = json["blockhash"] != null ? uint256.Parse(json.Value<string>("blockhash")) : null,
				Confirmations = json.Value<uint>("confirmations"),
				BlockTime = json["blocktime"] != null ? NBitcoin.Utils.UnixTimeToDateTime(json.Value<long>("blocktime")) : (DateTimeOffset?)null
			};
		}

		public uint256 SendRawTransaction(Transaction tx)
		{
			return SendRawTransaction(tx.ToBytes());
		}

		public uint256 SendRawTransaction(byte[] bytes)
		{
			return SendRawTransactionAsync(bytes).GetAwaiter().GetResult();
		}

		public Task<uint256> SendRawTransactionAsync(Transaction tx, CancellationToken cancellationToken = default)
		{
			return SendRawTransactionAsync(tx.ToBytes(), cancellationToken);
		}

		public async Task<uint256> SendRawTransactionAsync(byte[] bytes, CancellationToken cancellationToken = default)
		{
			var result = await SendCommandAsync(RPCOperations.sendrawtransaction, cancellationToken, Encoders.Hex.EncodeData(bytes)).ConfigureAwait(false);
			result.ThrowIfError();
			if (result.Result.Type != JTokenType.String)
				return null;
			return new uint256(result.Result.Value<string>());
		}

		public BumpResponse BumpFee(uint256 txid)
		{
			return BumpFeeAsync(txid).GetAwaiter().GetResult();
		}

		public async Task<BumpResponse> BumpFeeAsync(uint256 txid, CancellationToken cancellationToken = default)
		{
			var response = await SendCommandAsync(RPCOperations.bumpfee, cancellationToken, txid);
			var o = response.Result;
			return new BumpResponse
			{
				TransactionId = uint256.Parse((string)o["txid"]),
				OriginalFee = (ulong)o["origfee"],
				Fee = (ulong)o["fee"],
				Errors = o["errors"].Select(x => (string)x).ToList()
			};
		}


#endregion

#region Utility functions

		// Estimates the approximate fee per kilobyte needed for a transaction to begin
		// confirmation within conf_target blocks if possible and return the number of blocks
		// for which the estimate is valid.Uses virtual transaction size as defined
		// in BIP 141 (witness data is discounted).
#region Fee Estimation

		/// <summary>
		/// (>= Bitcoin Core v0.14) Get the estimated fee per kb for being confirmed in nblock
		/// If Capabilities is set and estimatesmartfee is not supported, will fallback on estimatefee
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">Whether to return a more conservative estimate which also satisfies a longer history. A conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired target, but is not as responsive to short term drops in the prevailing fee market.</param>
		/// <returns>The estimated fee rate, block number where estimate was found</returns>
		/// <exception cref="NoEstimationException">The Fee rate couldn't be estimated because of insufficient data from Bitcoin Core</exception>
		public EstimateSmartFeeResponse EstimateSmartFee(int confirmationTarget, EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
		{
			return EstimateSmartFeeAsync(confirmationTarget, estimateMode).GetAwaiter().GetResult();
		}

		/// <summary>
		/// (>= Bitcoin Core v0.14) Tries to get the estimated fee per kb for being confirmed in nblock
		/// If Capabilities is set and estimatesmartfee is not supported, will fallback on estimatefee
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">Whether to return a more conservative estimate which also satisfies a longer history. A conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired target, but is not as responsive to short term drops in the prevailing fee market.</param>
		/// <returns>The estimated fee rate, block number where estimate was found or null</returns>
		public async Task<EstimateSmartFeeResponse> TryEstimateSmartFeeAsync(int confirmationTarget, EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative, CancellationToken cancellationToken = default)
		{
			return await EstimateSmartFeeImplAsync(confirmationTarget, estimateMode, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// (>= Bitcoin Core v0.14) Tries to get the estimated fee per kb for being confirmed in nblock
		/// If Capabilities is set and estimatesmartfee is not supported, will fallback on estimatefee
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">Whether to return a more conservative estimate which also satisfies a longer history. A conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired target, but is not as responsive to short term drops in the prevailing fee market.</param>
		/// <returns>The estimated fee rate, block number where estimate was found or null</returns>
		public EstimateSmartFeeResponse TryEstimateSmartFee(int confirmationTarget, EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
		{
			return TryEstimateSmartFeeAsync(confirmationTarget, estimateMode).GetAwaiter().GetResult();
		}

		/// <summary>
		/// (>= Bitcoin Core v0.14) Get the estimated fee per kb for being confirmed in nblock
		/// If Capabilities is set and estimatesmartfee is not supported, will fallback on estimatefee
		/// </summary>
		/// <param name="confirmationTarget">Confirmation target in blocks (1 - 1008)</param>
		/// <param name="estimateMode">Whether to return a more conservative estimate which also satisfies a longer history. A conservative estimate potentially returns a higher feerate and is more likely to be sufficient for the desired target, but is not as responsive to short term drops in the prevailing fee market.</param>
		/// <returns>The estimated fee rate, block number where estimate was found</returns>
		/// <exception cref="NoEstimationException">when fee couldn't be estimated</exception>
		public async Task<EstimateSmartFeeResponse> EstimateSmartFeeAsync(int confirmationTarget, EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative, CancellationToken cancellationToken = default)
		{
			var feeRate = await EstimateSmartFeeImplAsync(confirmationTarget, estimateMode, cancellationToken);
			if (feeRate == null)
				throw new NoEstimationException(confirmationTarget);
			return feeRate;
		}

		/// <summary>
		/// (>= Bitcoin Core v0.14)
		/// </summary>
		private async Task<EstimateSmartFeeResponse> EstimateSmartFeeImplAsync(int confirmationTarget, EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative, CancellationToken cancellationToken = default)
		{
			if (Capabilities == null || Capabilities.SupportEstimateSmartFee)
			{
				var parameters = new List<object>() { confirmationTarget };
				if (estimateMode != EstimateSmartFeeMode.Conservative)
				{
					parameters.Add(estimateMode.ToString().ToUpperInvariant());
				}

				var request = new RPCRequest(RPCOperations.estimatesmartfee.ToString(), parameters.ToArray());
				request.ThrowIfRPCError = false;
				var response = await SendCommandAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);

				if (response?.Error != null)
				{
					return null;
				}
				var resultJToken = response.Result;
				var feeRateDecimal = resultJToken.Value<decimal>("feerate"); // estimate fee-per-kilobyte (in BTC)
				var blocks = resultJToken.Value<int>("blocks"); // block number where estimate was found
				var money = Money.Coins(feeRateDecimal);
				if (money.Satoshi <= 0)
				{
					return null;
				}
				return new EstimateSmartFeeResponse
				{
					FeeRate = new FeeRate(money),
					Blocks = blocks
				};
			}
			else
			{
				RPCResponse response = await SendCommandAsync(new RPCRequest(RPCOperations.estimatefee, new object[] { confirmationTarget }) { ThrowIfRPCError = false }, cancellationToken).ConfigureAwait(false);
				if (response.Error != null)
				{
					if (response.Error.Code is RPCErrorCode.RPC_MISC_ERROR)
					{
						// Some shitcoins do not require a parameter to estimatefee anymore
						response = await SendCommandAsync(RPCOperations.estimatefee).ConfigureAwait(false);
					}
					else
					{
						response.ThrowIfError();
					}
				}
				var result = response.Result.Value<decimal>();
				var money = Money.Coins(result);
				if (money.Satoshi < 0)
					return null;
				return new EstimateSmartFeeResponse() { FeeRate = new FeeRate(money), Blocks = confirmationTarget };
			}
		}

#endregion


#nullable enable
		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="parameters"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public uint256 SendToAddress(
			BitcoinAddress address,
			Money amount,
			SendToAddressParameters? parameters,
			CancellationToken cancellationToken = default
		)
		{
			return SendToAddressAsync(address, amount, parameters, cancellationToken).GetAwaiter().GetResult();
		}
		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public uint256 SendToAddress(
			BitcoinAddress address,
			Money amount,
			CancellationToken cancellationToken = default
		)
		{
			return SendToAddressAsync(address, amount, null, cancellationToken).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public uint256 SendToAddress(
			Script scriptPubKey,
			Money amount,
			CancellationToken cancellationToken = default
		)
		{
			return SendToAddressAsync(scriptPubKey, amount, null, cancellationToken).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="parameters"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public Task<uint256> SendToAddressAsync(
			Script scriptPubKey,
			Money amount,
			SendToAddressParameters? parameters,
			CancellationToken cancellationToken = default
			)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			return SendToAddressAsync(scriptPubKey.GetDestinationAddress(Network) ?? throw new ArgumentException("scriptPubKey can't be converted into an address", nameof(scriptPubKey)), amount, parameters, cancellationToken);
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="scriptPubKey">The scriptPubKey where the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public Task<uint256> SendToAddressAsync(
			Script scriptPubKey,
			Money amount,
			CancellationToken cancellationToken = default
			)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			return SendToAddressAsync(scriptPubKey.GetDestinationAddress(Network) ?? throw new ArgumentException("scriptPubKey can't be converted into an address", nameof(scriptPubKey)), amount, null, cancellationToken);
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="scriptPubKey">The scriptPubKey where the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="parameters"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public uint256 SendToAddress(
			Script scriptPubKey,
			Money amount,
			SendToAddressParameters? parameters,
			CancellationToken cancellationToken = default
			)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			return SendToAddress(scriptPubKey.GetDestinationAddress(Network) ?? throw new ArgumentException("scriptPubKey can't be converted into an address", nameof(scriptPubKey)), amount, parameters, cancellationToken);
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public Task<uint256> SendToAddressAsync(
			BitcoinAddress address,
			Money amount,
			CancellationToken cancellationToken = default
			)
		{
			return SendToAddressAsync(address, amount, null, cancellationToken);
		}

		/// <summary>
		/// Requires wallet support. Requires an unlocked wallet or an unencrypted wallet.
		/// </summary>
		/// <param name="address">A P2PKH or P2SH address to which the bitcoins should be sent</param>
		/// <param name="amount">The amount to spend</param>
		/// <param name="parameters"></param>
		/// <param name="cancellationToken"></param>
		/// <returns>The TXID of the sent transaction</returns>
		public async Task<uint256> SendToAddressAsync(
			BitcoinAddress address,
			Money amount,
			SendToAddressParameters? parameters,
			CancellationToken cancellationToken = default
			)
		{
			if (address is null)
				throw new ArgumentNullException(nameof(address));
			if (amount is null)
				throw new ArgumentNullException(nameof(amount));

			// Maximum compatiblity
			if (parameters is null)
			{
				List<object> list = new List<object>();
				list.Add(address.ToString());
				list.Add(amount.ToString());
				var resp = await SendCommandAsync(RPCOperations.sendtoaddress, cancellationToken, list.ToArray()).ConfigureAwait(false);
				return uint256.Parse(resp.Result.ToString());
			}
			else
			{
				var list = new Dictionary<string, object>();
				list.Add("address", address.ToString());
				list.Add("amount", amount.ToString());
				if (parameters.Comment is string)
					list.Add("comment", parameters.Comment);
				if (parameters.CommentTo is string)
					list.Add("comment_to", parameters.CommentTo);
				if (parameters.SubstractFeeFromAmount is bool v)
					list.Add("subtractfeefromamount", v);
				if (parameters.Replaceable is bool v1)
					list.Add("replaceable", v1);
				if (parameters.ConfTarget is int v2)
					list.Add("conf_target", v2);
				if (parameters.EstimateMode is EstimateSmartFeeMode v3)
					list.Add("estimate_mode", v3 == EstimateSmartFeeMode.Conservative ? "conservative" : "economical");
				if (parameters.FeeRate is FeeRate v4)
					list.Add("fee_rate", v4.SatoshiPerByte.ToString(CultureInfo.InvariantCulture));
				var resp = await SendCommandWithNamedArgsAsync("sendtoaddress", list, cancellationToken).ConfigureAwait(false);
				return uint256.Parse(resp.Result.ToString());
			}

		}
#nullable restore
		public bool SetTxFee(FeeRate feeRate, CancellationToken cancellationToken = default)
		{
			return SendCommand(RPCOperations.settxfee, cancellationToken, new[] { feeRate.FeePerK.ToString() }).Result.ToString() == "true";
		}

#endregion

		public async Task<uint256[]> GenerateAsync(int nBlocks, CancellationToken cancellationToken = default)
		{
			if (nBlocks < 0)
				throw new ArgumentOutOfRangeException("nBlocks");

			if (Capabilities != null && Capabilities.SupportGenerateToAddress)
			{
				var address = await GetNewAddressAsync(cancellationToken).ConfigureAwait(false);
				return await GenerateToAddressAsync(nBlocks, address, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				try
				{
					var result = (JArray)(await SendCommandAsync(RPCOperations.generate, cancellationToken, nBlocks).ConfigureAwait(false)).Result;
					return result.Select(r => new uint256(r.Value<string>())).ToArray();
				}
				catch (RPCException rpc) when (
					rpc.RPCCode == RPCErrorCode.RPC_METHOD_DEPRECATED
					|| rpc.RPCCode == RPCErrorCode.RPC_METHOD_NOT_FOUND
					|| (rpc.RPCCode == RPCErrorCode.RPC_MISC_ERROR))
				{
					var address = await GetNewAddressAsync(cancellationToken).ConfigureAwait(false);
					return await GenerateToAddressAsync(nBlocks, address, cancellationToken).ConfigureAwait(false);
				}
			}
		}
		public uint256[] Generate(int nBlocks)
		{
			return GenerateAsync(nBlocks).GetAwaiter().GetResult();
		}

		public async Task<uint256[]> GenerateToAddressAsync(int nBlocks, BitcoinAddress address, CancellationToken cancellationToken = default)
		{
			if (nBlocks < 0)
				throw new ArgumentOutOfRangeException(nameof(nBlocks));
			if (address == null)
				throw new ArgumentNullException(nameof(address));

			var generatetoaddressResponse = await SendCommandAsync(RPCOperations.generatetoaddress, cancellationToken, nBlocks, address.ToString()).ConfigureAwait(false);
			var result = (JArray)(generatetoaddressResponse.Result);
			return result.Select(r => new uint256(r.Value<string>())).ToArray();
		}

		public uint256[] GenerateToAddress(int nBlocks, BitcoinAddress address)
		{
			return GenerateToAddressAsync(nBlocks, address).GetAwaiter().GetResult();
		}

#region Region Hidden Methods

		/// <summary>
		/// Permanently marks a block as invalid, as if it violated a consensus rule.
		/// </summary>
		/// <param name="blockhash">the hash of the block to mark as invalid</param>
		/// <param name="cancellationToken"></param>
		public void InvalidateBlock(uint256 blockhash, CancellationToken cancellationToken = default)
		{
			SendCommand(RPCOperations.invalidateblock, cancellationToken, blockhash);
		}

		/// <summary>
		/// Permanently marks a block as invalid, as if it violated a consensus rule.
		/// </summary>
		/// <param name="blockhash">the hash of the block to mark as invalid</param>
		public async Task InvalidateBlockAsync(uint256 blockhash, CancellationToken cancellationToken = default)
		{
			await SendCommandAsync(RPCOperations.invalidateblock, cancellationToken, blockhash).ConfigureAwait(false);
		}

#if !NOSOCKET

		/// <summary>
		/// Add the address of a potential peer to the address manager. This RPC is for testing only.
		/// </summary>
		public bool AddPeerAddress(IPAddress ip, int port)
		{
			if (ip is null) throw new ArgumentNullException(nameof(ip));

			return AddPeerAddressAsync(ip, port).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Add the address of a potential peer to the address manager. This RPC is for testing only.
		/// </summary>
		public async Task<bool> AddPeerAddressAsync(IPAddress ip, int port, CancellationToken cancellationToken = default)
		{
			if (ip is null) throw new ArgumentNullException(nameof(ip));

			var result = await SendCommandAsync(RPCOperations.addpeeraddress, cancellationToken, ip.ToString(), port).ConfigureAwait(false);
			return result.Result["success"].Value<bool>();
		}

#endif

#endregion
	}

#if !NOSOCKET
	public class PeerInfo
	{
		public int Id
		{
			get; internal set;
		}
		public EndPoint Address
		{
			get; internal set;
		}
		public EndPoint LocalAddress
		{
			get; internal set;
		}
		public NodeServices Services
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
		public string[] Permissions { get; set; } = new string[0];
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

	public class BlockchainInfo
	{
		public class SoftFork
		{
			public string Bip { get; set; }
			public string ForkType { get; set; }
			public bool Activated { get; set; }
			public uint Height { get; set; }
		}

		public class Bip9SoftFork
		{
			public string Name { get; set; }
			public string Status { get; set; }
			public DateTimeOffset StartTime { get; set; }
			public DateTimeOffset Timeout { get; set; }
			public ulong SinceHeight { get; set; }

		}

		public Network Chain { get; set; }
		public ulong Blocks { get; set; }
		public ulong Headers { get; set; }
		public uint256 BestBlockHash { get; set; }
		public ulong Difficulty { get; set; }
		public ulong MedianTime { get; set; }

		public float VerificationProgress { get; set; }
		public bool InitialBlockDownload { get; set; }
		public uint256 ChainWork { get; set; }
		public ulong SizeOnDisk { get; set; }
		public bool Pruned { get; set; }

		[Obsolete]
		public List<SoftFork> SoftForks { get; set; }
		[Obsolete]
		public List<Bip9SoftFork> Bip9SoftForks { get; set; }
	}

	public class RawTransactionInfo
	{
		public Transaction Transaction { get; internal set; }
		public uint256 TransactionId { get; internal set; }
		public uint256 Hash { get; internal set; }
		public uint Size { get; internal set; }
		public uint VirtualSize { get; internal set; }
		public uint Version { get; internal set; }
		public LockTime LockTime { get; internal set; }
		public uint256 BlockHash { get; internal set; }
		public uint Confirmations { get; internal set; }
		public DateTimeOffset? TransactionTime { get; internal set; }
		public DateTimeOffset? BlockTime { get; internal set; }
	}

	public class BumpResponse
	{
		public uint256 TransactionId { get; set; }
		public ulong OriginalFee { get; set; }
		public ulong Fee { get; set; }
		public List<string> Errors { get; set; }
	}

	public class NoEstimationException : Exception
	{
		public NoEstimationException(int nblock)
			: base("The FeeRate couldn't be estimated because of insufficient data from Bitcoin Core. Try to use smaller nBlock, or wait Bitcoin Core to gather more data.")
		{
		}
	}

	public class MempoolEntry
	{
		/// <summary>
		/// The transaction id (must be in mempool.)
		/// </summary>
		public uint256 TransactionId { get; set; }
		/// <summary>
		/// Virtual transaction size as defined in BIP 141. This is different from actual serialized size for witness transactions as witness data is discounted.
		/// </summary>
		public int VirtualSizeBytes { get; set; }
		/// <summary>
		/// Local time transaction entered pool in seconds since 1 Jan 1970 GMT.
		/// </summary>
		public DateTimeOffset Time { get; set; }
		/// <summary>
		/// Block height when transaction entered pool.
		/// </summary>
		public int Height { get; set; }
		/// <summary>
		/// Number of in-mempool descendant transactions (including this one.)
		/// </summary>
		public int DescendantCount { get; set; }
		/// <summary>
		/// Virtual transaction size of in-mempool descendants (including this one.)
		/// </summary>
		public int DescendantVirtualSizeBytes { get; set; }
		/// <summary>
		/// Number of in-mempool ancestor transactions (including this one.)
		/// </summary>
		public int AncestorCount { get; set; }
		/// <summary>
		/// Virtual transaction size of in-mempool ancestors (including this one.)
		/// </summary>
		public int AncestorVirtualSizeBytes { get; set; }
		/// <summary>
		/// Hash of serialized transaction, including witness data.
		/// </summary>
		public uint256 TransactionIdWithWitness { get; set; }
		/// <summary>
		/// Transaction fee.
		/// </summary>
		public Money BaseFee { get; set; }
		/// <summary>
		/// Transaction fee with fee deltas used for mining priority.
		/// </summary>
		public Money ModifiedFee { get; set; }
		/// <summary>
		/// Modified fees (see above) of in-mempool ancestors (including this one.)
		/// </summary>
		public Money AncestorFees { get; set; }
		/// <summary>
		/// Modified fees (see above) of in-mempool descendants (including this one.)
		/// </summary>
		public Money DescendantFees { get; set; }
		/// <summary>
		/// Unconfirmed transactions used as inputs for this transaction.
		/// </summary>
		public uint256[] Depends { get; set; }
		/// <summary>
		/// Unconfirmed transactions spending outputs from this transaction.
		/// </summary>
		public uint256[] SpentBy { get; set; }
	}

#nullable enable
	public class TestMempoolParameters
	{
		public FeeRate? MaxFeeRate { get; set; }
	}

	public class SendToAddressParameters
	{
		/// <summary>
		/// A locally-stored (not broadcast) comment assigned to this transaction. Default is no comment.
		/// </summary>
		public string? Comment { get; set; }
		/// <summary>
		/// A locally-stored (not broadcast) comment assigned to this transaction. Meant to be used for describing who the payment was sent to. Default is no comment.
		/// </summary>
		public string? CommentTo { get; set; }
		/// <summary>
		/// The fee will be deducted from the amount being sent. The recipient will receive less bitcoins than you enter in the amount field. Default is false.
		/// </summary>
		public bool? SubstractFeeFromAmount { get; set; }
		/// <summary>
		/// Allow this transaction to be replaced by a transaction with higher fees. 
		/// </summary>
		public bool? Replaceable { get; set; }

		/// <summary>
		/// Confirmation target in blocks
		/// </summary>
		public int? ConfTarget { get; set; }

		/// <summary>
		/// The fee estimate mode
		/// </summary>
		public EstimateSmartFeeMode? EstimateMode { get; set; }

		/// <summary>
		/// Specify a fee rate in sat/vB.
		/// </summary>
		public FeeRate? FeeRate { get; set; }
	}
#nullable restore
	public class MempoolAcceptResult
	{
		public uint256 TxId { get; internal set; }
		public bool IsAllowed { get; internal set; }
		public RejectCode RejectCode { get; internal set; }
		public string RejectReason { get; internal set; }
	}

	public class BlockFilter
	{
		public GolombRiceFilter Filter { get; }
		public uint256 Header { get; }

		public BlockFilter(GolombRiceFilter filter, uint256 header)
		{
			Filter = filter;
			Header = header;
		}
	}

	public class BlockStats
	{
		public Money AvgFee { get; set; }
		public Money AvgFeeRate { get; set; }
		public int AvgTxSize { get; set; }
		public uint256 BlockHash { get; set; }
		public Money[] FeeRatePercentiles { get; set; }
		public int Height { get; set; }
		public int Ins { get; set; }
		public Money MaxFee { get; set; }
		public Money MaxFeeRate { get; set; }
		public int MaxTxSize { get; set; }
		public Money MedianFee { get; set; }
		public DateTimeOffset MedianTime { get; set; }
		public int MedianTxSize { get; set; }
		public Money MinFee { get; set; }
		public Money MinFeeRate { get; set; }
		public int MinTxSize { get; set; }
		public int Outs { get; set; }
		public long Subsidy { get; set; }
		public long SWTotalSize { get; set; }
		public long SWTotalWeight { get; set; }
		public int SWTxs { get; set; }
		public DateTimeOffset Time { get; set; }
		public long TotalOut { get; set; }
		public long TotalSize { get; set; }
		public long TotalWeight { get; set; }
		public Money TotalFee { get; set; }
		public int Txs { get; set; }
		public int UTXOIncrease { get; set; }
		public int UTXOSizeInc { get; set; }
		
	}
}
#endif
