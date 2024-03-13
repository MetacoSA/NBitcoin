#if !NOJSONNET
using NBitcoin.DataEncoders;
using NBitcoin.JsonConverters;
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
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
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
		Category			Name						Implemented
		------------------ --------------------------- -----------------------

		------------------ Wallet
		wallet			 addmultisigaddress
		wallet			 backupwallet				Yes
		wallet			 dumpprivkey				Yes
		wallet			 createwallet				Yes
		wallet			 dumpwallet
		wallet			 encryptwallet
		wallet			 getaccountaddress			Yes
		wallet			 getaccount
		wallet			 getaddressesbyaccount		Yes
		wallet			 getaddressesinfo
		wallet			 getbalance
		wallet			 getnewaddress
		wallet			 getrawchangeaddress
		wallet			 getreceivedbyaccount
		wallet			 getreceivedbyaddress		Yes
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
		wallet			 loadwallet					Yes
		wallet			 lockunspent				Yes
		wallet			 move
		wallet			 sendfrom
		wallet			 sendmany
		wallet			 sendtoaddress
		wallet			 setaccount
		wallet			 settxfee
		wallet			 signmessage
		wallet			 unloadwallet				Yes
		wallet			 walletlock
		wallet			 walletpassphrasechange
		wallet			 walletpassphrase			Yes
		wallet			 walletprocesspsbt
		wallet			 walletcreatefundedpsbt
	*/

	public partial class RPCClient
	{
#nullable enable
		[Obsolete("This method is being renamed, use SetWalletContext instead")]
		public RPCClient GetWallet(string? walletName)
		{
			return SetWalletContext(walletName);
		}
		public RPCClient SetWalletContext(string? walletName)
		{
			RPCCredentialString credentialString;;

			if (_BatchedRequests is null)
			{
				credentialString = RPCCredentialString.Parse(CredentialString.ToString());
			}
			else
			{
				if (string.IsNullOrEmpty(CredentialString.WalletName))
				{
					credentialString = CredentialString;
				}
				else
				{
					throw new InvalidOperationException("Batch RPC client already has a wallet assigned.");
				}
			}
			credentialString.WalletName = walletName;

			return new RPCClient(credentialString, Address, Network)
			{
				_BatchedRequests = _BatchedRequests,
				Capabilities = Capabilities,
				_HttpClient = _HttpClient,
				AllowBatchFallback = AllowBatchFallback
			};
		}

		public async Task<RPCClient> CreateWalletAsync(string walletNameOrPath, CreateWalletOptions? options = null, CancellationToken cancellationToken = default)
		{
			if (walletNameOrPath is null)
				throw new ArgumentNullException(nameof(walletNameOrPath));

			var parameters = new Dictionary<string, object>();
			parameters.Add("wallet_name", walletNameOrPath);
			if (options?.DisablePrivateKeys is bool disablePrivateKeys)
				parameters.Add("disable_private_keys", disablePrivateKeys);
			if (options?.Blank is bool blank)
				parameters.Add("blank", blank);
			if (options?.Passphrase is string passphrase && passphrase.Length > 0)
				parameters.Add("passphrase", passphrase);
			if (options?.AvoidReuse is bool avoidReuse)
				parameters.Add("avoid_reuse", avoidReuse);
			if (options?.Descriptors is bool descriptors)
				parameters.Add("descriptors", descriptors);
			if (options?.LoadOnStartup is bool loadOnStartup)
				parameters.Add("load_on_startup", loadOnStartup);
			var result = await SendCommandWithNamedArgsAsync(RPCOperations.createwallet.ToString(), parameters, cancellationToken).ConfigureAwait(false);
			return SetWalletContext(result.Result.Value<string>("name"));
		}

		public RPCClient CreateWallet(string walletNameOrPath, CreateWalletOptions? options = null)
		{
			if (walletNameOrPath is null)
				throw new ArgumentNullException(nameof(walletNameOrPath));

			return CreateWalletAsync(walletNameOrPath, options).GetAwaiter().GetResult();
		}

		public Task<RPCClient> LoadWalletAsync(bool? loadOnStartup = null)
		{
			return LoadWalletAsync(null, loadOnStartup);
		}
		public RPCClient LoadWallet(bool? loadOnStartup = null)
		{
			return LoadWallet(null, loadOnStartup);
		}
		public async Task<RPCClient> LoadWalletAsync(string? walletName, bool? loadOnStartup = null, CancellationToken cancellationToken = default)
		{
			var req = GetLoadUnloadWalletRequest("loadwallet", walletName, loadOnStartup);
			var response = await SendCommandAsync(req, cancellationToken: cancellationToken).ConfigureAwait(false);
			return SetWalletContext(response.Result.Value<string>("name"));
		}

		private RPCRequest GetLoadUnloadWalletRequest(string methodName, string? walletName, bool? loadOnStartup)
		{
			List<object> parameters = new List<object>();
			RPCRequest req;
			if (walletName != null)
			{
				parameters.Add(walletName);
				if (loadOnStartup is bool b)
					parameters.Add(b);
				req = new RPCRequest(methodName, parameters.ToArray());
			}
			else
			{
				if (loadOnStartup is bool b)
				{
					req = new RPCRequest()
					{
						Method = methodName,
						NamedParams = new Dictionary<string, object>()
						{
							{ "load_on_startup", b }
						}
					};
				}
				else
				{
					req = new RPCRequest(methodName, parameters.ToArray());
				}
			}
			return req;
		}

		public RPCClient LoadWallet(string? walletName, bool? loadOnStartup = null)
		{
			return LoadWalletAsync(walletName, loadOnStartup).GetAwaiter().GetResult();
		}
		public void UnloadWallet()
		{
			UnloadWalletAsync(null).GetAwaiter().GetResult();
		}
		public void UnloadWallet(bool? loadOnStartup = null)
		{
			UnloadWalletAsync(loadOnStartup).GetAwaiter().GetResult();
		}
		public Task UnloadWalletAsync(bool? loadOnStartup = null)
		{
			return UnloadWalletAsync(null, loadOnStartup);
		}
		public Task UnloadWalletAsync(string? walletName, bool? loadOnStartup = null, CancellationToken cancellationToken = default)
		{
			var req = GetLoadUnloadWalletRequest("unloadwallet", walletName, loadOnStartup);
			return SendCommandAsync(req, cancellationToken: cancellationToken);
		}


#nullable restore

		// backupwallet
		public void BackupWallet(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));
			SendCommand(RPCOperations.backupwallet, path);
		}

		public async Task BackupWalletAsync(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));
			await SendCommandAsync(RPCOperations.backupwallet, path).ConfigureAwait(false);
		}


		// dumpprivkey

		public BitcoinSecret DumpPrivKey(BitcoinAddress address, CancellationToken cancellationToken = default)
		{
			var response = SendCommand(RPCOperations.dumpprivkey, cancellationToken, address.ToString());
			return Network.Parse<BitcoinSecret>((string)response.Result);
		}

		public async Task<BitcoinSecret> DumpPrivKeyAsync(BitcoinAddress address)
		{
			var response = await SendCommandAsync(RPCOperations.dumpprivkey, address.ToString()).ConfigureAwait(false);
			return Network.Parse<BitcoinSecret>((string)response.Result);
		}

		public FundRawTransactionResponse FundRawTransaction(Transaction transaction, FundRawTransactionOptions options = null)
		{
			return FundRawTransactionAsync(transaction, options).GetAwaiter().GetResult();
		}

		/// <summary>
		/// throws an error if an address is not from the wallet.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public GetAddressInfoResponse GetAddressInfo(IDestination address) => GetAddressInfoAsync(address).GetAwaiter().GetResult();

		public async Task<GetAddressInfoResponse> GetAddressInfoAsync(IDestination address)
		{
			var addrString = address.ScriptPubKey.GetDestinationAddress(Network).ToString();
			var response = await SendCommandAsync(RPCOperations.getaddressinfo, addrString);

			return GetAddressInfoResponse.FromJsonResponse((JObject)response.Result, Network);
		}

		public Money GetBalance(int minConf, bool includeWatchOnly)
		{
			return GetBalanceAsync(minConf, includeWatchOnly).GetAwaiter().GetResult();
		}
		public Money GetBalance()
		{
			return GetBalanceAsync().GetAwaiter().GetResult();
		}

		public async Task<Money> GetBalanceAsync()
		{
			var data = await SendCommandAsync(RPCOperations.getbalance, "*").ConfigureAwait(false);
			return Money.Coins(data.Result.Value<decimal>());
		}

		public async Task<Money> GetBalanceAsync(int minConf, bool includeWatchOnly)
		{
			var data = await SendCommandAsync(RPCOperations.getbalance, "*", minConf, includeWatchOnly).ConfigureAwait(false);
			return Money.Coins(data.Result.Value<decimal>());
		}

		public async Task<FundRawTransactionResponse> FundRawTransactionAsync(Transaction transaction, FundRawTransactionOptions options = null, CancellationToken cancellationToken = default)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));

			RPCResponse response = null;
			if (options != null)
			{
				var jOptions = FundRawTransactionOptionsToJson(options);
				response = await SendCommandAsync("fundrawtransaction", cancellationToken, ToHex(transaction), jOptions).ConfigureAwait(false);
			}
			else
			{
				response = await SendCommandAsync("fundrawtransaction", cancellationToken, ToHex(transaction)).ConfigureAwait(false);
			}
			var r = (JObject)response.Result;
			return new FundRawTransactionResponse()
			{
				Transaction = ParseTxHex(r["hex"].Value<string>()),
				Fee = Money.Coins(r["fee"].Value<decimal>()),
				ChangePos = r["changepos"].Value<int>()
			};
		}

		private JObject FundRawTransactionOptionsToJson(FundRawTransactionOptions options)
		{
			var jOptions = new JObject();
			if (options.ChangeAddress != null)
				jOptions.Add(new JProperty("changeAddress", options.ChangeAddress.ToString()));
			if (options.ChangePosition != null)
				jOptions.Add(new JProperty("changePosition", options.ChangePosition.Value));
			jOptions.Add(new JProperty("includeWatching", options.IncludeWatching));
			jOptions.Add(new JProperty("lockUnspents", options.LockUnspents));
			if (options.ReserveChangeKey != null)
				jOptions.Add(new JProperty("reserveChangeKey", options.ReserveChangeKey));
			if (options.FeeRate != null)
				jOptions.Add(new JProperty("feeRate", options.FeeRate.GetFee(1000).ToDecimal(MoneyUnit.BTC)));
			if (options.SubtractFeeFromOutputs != null)
			{
				JArray array = new JArray();
				foreach (var v in options.SubtractFeeFromOutputs)
				{
					array.Add(new JValue(v));
				}
				jOptions.Add(new JProperty("subtractFeeFromOutputs", array));
			}
			return jOptions;
		}

		//NBitcoin internally put a bit in the version number to make difference between transaction without input and transaction with witness.
		private string ToHex(Transaction tx)
		{
			// if there is inputs, then it can't be confusing
			if (tx.Inputs.Count > 0)
				return tx.ToHex();
			// if there is, do this ACK so that NBitcoin does not change the version number
			return Encoders.Hex.EncodeData(tx.ToBytes(70012 - 1));
		}


		// getreceivedbyaddress

		/// <summary>
		/// Returns the total amount received by the specified address in transactions with at
		/// least one (default) confirmations. It does not count coinbase transactions.
		/// </summary>
		/// <param name="address">The address whose transactions should be tallied.</param>
		/// <returns>The number of bitcoins received by the address, excluding coinbase transactions. May be 0.</returns>
		public Money GetReceivedByAddress(BitcoinAddress address)
		{
			var response = SendCommand(RPCOperations.getreceivedbyaddress, address.ToString());
			return Money.Coins(response.Result.Value<decimal>());
		}

		/// <summary>
		/// Returns the total amount received by the specified address in transactions with at
		/// least one (default) confirmations. It does not count coinbase transactions.
		/// </summary>
		/// <param name="address">The address whose transactions should be tallied.</param>
		/// <returns>The number of bitcoins received by the address, excluding coinbase transactions. May be 0.</returns>
		public async Task<Money> GetReceivedByAddressAsync(BitcoinAddress address)
		{
			var response = await SendCommandAsync(RPCOperations.getreceivedbyaddress, address.ToString()).ConfigureAwait(false);
			return GetMoney(response);
		}

		/// <summary>
		/// Returns the total amount received by the specified address in transactions with the
		/// specified number of confirmations. It does not count coinbase transactions.
		/// </summary>
		/// <param name="confirmations">
		/// The minimum number of confirmations an externally-generated transaction must have before
		/// it is counted towards the balance. Transactions generated by this node are counted immediately.
		/// Typically, externally-generated transactions are payments to this wallet and transactions
		/// generated by this node are payments to other wallets. Use 0 to count unconfirmed transactions.
		/// Default is 1.
		/// </param>
		/// <returns>The number of bitcoins received by the address, excluding coinbase transactions. May be 0.</returns>
		public Money GetReceivedByAddress(BitcoinAddress address, int confirmations)
		{
			var response = SendCommand(RPCOperations.getreceivedbyaddress, address.ToString(), confirmations);
			return GetMoney(response);
		}

		/// <summary>
		/// Returns the total amount received by the specified address in transactions with the
		/// specified number of confirmations. It does not count coinbase transactions.
		/// </summary>
		/// <param name="confirmations">
		/// The minimum number of confirmations an externally-generated transaction must have before
		/// it is counted towards the balance. Transactions generated by this node are counted immediately.
		/// Typically, externally-generated transactions are payments to this wallet and transactions
		/// generated by this node are payments to other wallets. Use 0 to count unconfirmed transactions.
		/// Default is 1.
		/// </param>
		/// <returns>The number of bitcoins received by the address, excluding coinbase transactions. May be 0.</returns>
		public async Task<Money> GetReceivedByAddressAsync(BitcoinAddress address, int confirmations)
		{
			var response = await SendCommandAsync(RPCOperations.getreceivedbyaddress, address.ToString(), confirmations).ConfigureAwait(false);
			return GetMoney(response);
		}


		// importprivkey

		public void ImportPrivKey(BitcoinSecret secret)
		{
			SendCommand(RPCOperations.importprivkey, secret.ToWif());
		}

		public void ImportPrivKey(BitcoinSecret secret, string label, bool rescan)
		{
			ImportPrivKeyAsync(secret, label, rescan).GetAwaiter().GetResult();
		}

		public async Task ImportPrivKeyAsync(BitcoinSecret secret)
		{
			await SendCommandAsync(RPCOperations.importprivkey, secret.ToWif()).ConfigureAwait(false);
		}

		public async Task ImportPrivKeyAsync(BitcoinSecret secret, string label, bool rescan)
		{
			try
			{
				await SendCommandAsync(RPCOperations.importprivkey, secret.ToWif(), label, rescan).ConfigureAwait(false);
			}
			catch (RPCException ex) when (label is null && ex.RPCCode == RPCErrorCode.RPC_MISC_ERROR)
			{
				// Some old node (like dogecoin) don't support null label
				await SendCommandAsync(RPCOperations.importprivkey, secret.ToWif(), "*", rescan).ConfigureAwait(false);
			}
		}


		// importaddress

		public void ImportAddress(IDestination address)
		{
			SendCommand(RPCOperations.importaddress, address.ScriptPubKey.ToHex());
		}

		public void ImportAddress(IDestination address, string label, bool rescan)
		{
			SendCommand(RPCOperations.importaddress, address.ScriptPubKey.ToHex(), label, rescan);
		}

		public void ImportAddress(Script scriptPubKey)
		{
			SendCommand(RPCOperations.importaddress, scriptPubKey.ToHex());
		}

		public void ImportAddress(Script scriptPubKey, string label, bool rescan)
		{
			SendCommand(RPCOperations.importaddress, scriptPubKey.ToHex(), label, rescan);
		}

		public async Task ImportAddressAsync(Script scriptPubKey)
		{
			await SendCommandAsync(RPCOperations.importaddress, scriptPubKey.ToHex()).ConfigureAwait(false);
		}

		public async Task ImportAddressAsync(Script scriptPubKey, string label, bool rescan)
		{
			await SendCommandAsync(RPCOperations.importaddress, scriptPubKey.ToHex(), label, rescan).ConfigureAwait(false);
		}

		public async Task ImportAddressAsync(BitcoinAddress address)
		{
			await SendCommandAsync(RPCOperations.importaddress, address.ToString()).ConfigureAwait(false);
		}

		public async Task ImportAddressAsync(BitcoinAddress address, string label, bool rescan)
		{
			await SendCommandAsync(RPCOperations.importaddress, address.ToString(), label, rescan).ConfigureAwait(false);
		}


		// importmulti
		public void ImportMulti(ImportMultiAddress[] addresses, bool rescan) =>
			ImportMulti(addresses, rescan, null);

		#nullable enable
		public void ImportMulti(ImportMultiAddress[] addresses, bool rescan, ISigningRepository? signingRepository)
		{
			ImportMultiAsync(addresses, rescan, signingRepository).GetAwaiter().GetResult();
		}
		public Task ImportMultiAsync(ImportMultiAddress[] addresses, bool rescan)
			=> ImportMultiAsync(addresses, rescan, null);
		/// <summary>
		///
		/// </summary>
		/// <param name="addresses"></param>
		/// <param name="rescan"></param>
		/// <param name="signingRepository">If you specify this, This method tries to serialize OutputDescriptor with the private key (If there is any entry in the repository).</param>
		/// <returns></returns>
		/// <exception cref="RPCException"></exception>
		public async Task ImportMultiAsync(ImportMultiAddress[] addresses, bool rescan, ISigningRepository? signingRepository, CancellationToken cancellationToken = default)
		{
			var parameters = new List<object>();

			var array = new JArray();
			parameters.Add(array);
			var seria = JsonSerializer.CreateDefault(JsonSerializerSettings);
			// -- replace json converter with the one with new `ISigningRepository`
			var oldConverter =
				seria.Converters
					.FirstOrDefault(converter => converter is OutputDescriptorJsonConverter);
			if (oldConverter != null)
			{
				seria.Converters.Remove(oldConverter);
			}

			signingRepository ??= new FlatSigningRepository();
			foreach (var key in addresses.Where(x => x.Keys != null).SelectMany(x => x.Keys))
			{
				if (key != null)
				{
					signingRepository.SetSecret(key.PubKeyHash, key);
				}
			}
			seria.Converters.Add(new OutputDescriptorJsonConverter(Network, false, signingRepository));

			// -- --
			foreach (var addr in addresses)
			{
				var obj = JObject.FromObject(addr, seria);
				if (obj["timestamp"] == null || obj["timestamp"]?.Type is JTokenType.Null)
					obj["timestamp"] = "now";
				else
					obj["timestamp"] = new JValue(Utils.DateTimeToUnixTime(addr.Timestamp!.Value));
				array.Add(obj);
			}

			var oRescan = JObject.FromObject(new { rescan = rescan });
			parameters.Add(oRescan);

			var response = await SendCommandAsync("importmulti", cancellationToken, parameters.ToArray()).ConfigureAwait(false);
			response.ThrowIfError();

			//Somehow, this one has error embedded
			var error = ((JArray)response.Result).OfType<JObject>()
				.Select(j => j.GetValue("error") as JObject)
				.FirstOrDefault(o => o != null);
			if (error != null)
			{
				var errorObj = new RPCError(error);
				throw new RPCException(errorObj.Code, errorObj.Message, response);
			}
		}

		#nullable  disable


		JsonSerializerSettings _JsonSerializer;
		JsonSerializerSettings JsonSerializerSettings
		{
			get
			{
				if (_JsonSerializer == null)
				{
					var seria = new JsonSerializerSettings();
					Serializer.RegisterFrontConverters(seria, Network);
					_JsonSerializer = seria;
				}
				return _JsonSerializer;
			}
		}

		// listaddressgroupings

		public IEnumerable<AddressGrouping> ListAddressGroupings()
		{
			var result = SendCommand(RPCOperations.listaddressgroupings);
			var array = (JArray)result.Result;
			foreach (var group in array.Children<JArray>())
			{
				var grouping = new AddressGrouping();
				grouping.PublicAddress = BitcoinAddress.Create(group[0][0].ToString(), Network);
				grouping.Amount = Money.Coins(group[0][1].Value<decimal>());
				grouping.Account = group[0].Count() > 2 ? group[0][2].ToString() : null;

				foreach (var subgroup in group.Skip(1))
				{
					var change = new ChangeAddress();
					change.Address = BitcoinAddress.Create(subgroup[0].ToString(), Network);
					change.Amount = Money.Coins(subgroup[1].Value<decimal>());
					grouping.ChangeAddresses.Add(change);
				}

				yield return grouping;
			}
		}

		public IEnumerable<BitcoinSecret> ListSecrets()
		{
			foreach (var grouping in ListAddressGroupings())
			{
				yield return DumpPrivKey(grouping.PublicAddress);
				foreach (var change in grouping.ChangeAddresses)
					yield return DumpPrivKey(change.Address);
			}
		}


		// listunspent

		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Note: as of Bitcoin Core 0.10.0, outputs affecting watch-only addresses will be returned;
		/// see the spendable field in the results.
		/// </para>
		/// </remarks>
		public UnspentCoin[] ListUnspent()
		{
			var response = SendCommand(RPCOperations.listunspent);
			return response.Result.Select(i => new UnspentCoin((JObject)i, Network)).ToArray();
		}

		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet,
		/// specifying the minimum and maximum number of confirmations to include,
		/// and the list of addresses to include.
		/// </summary>
		public UnspentCoin[] ListUnspent(int minconf, int maxconf, params BitcoinAddress[] addresses)
		{
			var addr = from a in addresses select a.ToString();
			var response = SendCommand(RPCOperations.listunspent, minconf, maxconf, addr.ToArray());
			return response.Result.Select(i => new UnspentCoin((JObject)i, Network)).ToArray();
		}

		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet.
		/// </summary>
		public async Task<UnspentCoin[]> ListUnspentAsync()
		{
			var response = await SendCommandAsync(RPCOperations.listunspent).ConfigureAwait(false);
			return response.Result.Select(i => new UnspentCoin((JObject)i, Network)).ToArray();
		}

		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet,
		/// specifying the minimum and maximum number of confirmations to include,
		/// and the list of addresses to include.
		/// </summary>
		public async Task<UnspentCoin[]> ListUnspentAsync(int minconf, int maxconf, params BitcoinAddress[] addresses)
		{
			var addr = from a in addresses select a.ToString();
			var response = await SendCommandAsync(RPCOperations.listunspent, minconf, maxconf, addr.ToArray()).ConfigureAwait(false);
			return response.Result.Select(i => new UnspentCoin((JObject)i, Network)).ToArray();
		}

		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet,
		/// with query_options and the list of addresses to include.
		/// </summary>
		/// <param name="options">
		/// MinimumAmount - Minimum value of each UTXO
		/// MaximumAmount - Maximum value of each UTXO
		/// MaximumCount - Maximum number of UTXOs
		/// MinimumSumAmount - Minimum sum value of all UTXOs
		/// </param>
		public async Task<UnspentCoin[]> ListUnspentAsync(ListUnspentOptions options, params BitcoinAddress[] addresses)
		{
			return await ListUnspentAsync(options, CancellationToken.None, addresses);
		}

		public async Task<UnspentCoin[]> ListUnspentAsync(ListUnspentOptions options, CancellationToken cancellationToken, params BitcoinAddress[] addresses)
		{
			var queryOptions = new Dictionary<string, object>();
			var queryObjects = new JObject();

			if (options.MinimumAmount != null)
			{
				queryObjects.Add("minimumAmount", options.MinimumAmount);
			}
			if (options.MaximumAmount != null)
			{
				queryObjects.Add("maximumAmount", options.MaximumAmount);
			}
			if (options.MaximumCount != null)
			{
				queryObjects.Add("maximumCount", options.MaximumCount);
			}
			if (options.MinimumSumAmount != null)
			{
				queryObjects.Add("minimumSumAmount", options.MinimumSumAmount);
			}

			queryOptions.Add("query_options", queryObjects);

			var addr = (from a in addresses select a.ToString()).ToArray();
			queryOptions.Add("addresses", addr);

			var response = await SendCommandWithNamedArgsAsync(RPCOperations.listunspent.ToString(), queryOptions, cancellationToken).ConfigureAwait(false);
			return response.Result.Select(i => new UnspentCoin((JObject)i, Network)).ToArray();
		}

		//listlockunspent
		public async Task<OutPoint[]> ListLockUnspentAsync()
		{
			var unspent = await SendCommandAsync(RPCOperations.listlockunspent).ConfigureAwait(false);
			return ((JArray)unspent.Result)
				.Select(i => new OutPoint(new uint256(i["txid"].Value<string>()), i["vout"].Value<int>()))
				.ToArray();
		}

		public OutPoint[] ListLockUnspent()
		{
			return ListLockUnspentAsync().GetAwaiter().GetResult();
		}

		// abandon transaction

		/// <summary>
		/// Marks a transaction and all its in-wallet descendants as abandoned which will allow
		/// for their inputs to be respent.
		/// </summary>
		/// <param name="txId">the transaction id to be marked as abandoned.</param>
		public void AbandonTransaction(uint256 txId)
		{
			if (txId is null) throw new ArgumentNullException(nameof(txId));

			SendCommand(RPCOperations.abandontransaction, txId.ToString());
		}

		/// <summary>
		/// Marks a transaction and all its in-wallet descendants as abandoned which will allow
		/// for their inputs to be respent.
		/// </summary>
		/// <param name="txId">the transaction id to be marked as abandoned.</param>
		public async Task AbandonTransactionAsync(uint256 txId)
		{
			if (txId is null) throw new ArgumentNullException(nameof(txId));

			await SendCommandAsync(RPCOperations.abandontransaction, txId.ToString()).ConfigureAwait(false);
		}


		// lockunspent

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
			LockUnspentCoreAsync(unlock, outpoints).GetAwaiter().GetResult();
		}

		private async Task LockUnspentCoreAsync(bool unlock, OutPoint[] outpoints)
		{
			if (outpoints == null || outpoints.Length == 0)
				return;
			var parameters = new List<object>();
			parameters.Add(unlock);
			var array = new JArray();
			parameters.Add(array);
			foreach (var outp in outpoints)
			{
				var obj = new JObject();
				obj["txid"] = outp.Hash.ToString();
				obj["vout"] = outp.N;
				array.Add(obj);
			}
			await SendCommandAsync(RPCOperations.lockunspent, parameters.ToArray()).ConfigureAwait(false);
		}

		// walletpassphrase

		/// <summary>
		/// The walletpassphrase RPC stores the wallet decryption key in memory for the indicated number of seconds.Issuing the walletpassphrase command while the wallet is already unlocked will set a new unlock time that overrides the old one.
		/// </summary>
		/// <param name="passphrase">The passphrase</param>
		/// <param name="timeout">Timeout in seconds</param>
		public void WalletPassphrase(string passphrase, int timeout)
		{
			WalletPassphraseAsync(passphrase, timeout).GetAwaiter().GetResult();
		}

		/// <summary>
		/// The walletpassphrase RPC stores the wallet decryption key in memory for the indicated number of seconds.Issuing the walletpassphrase command while the wallet is already unlocked will set a new unlock time that overrides the old one.
		/// </summary>
		/// <param name="passphrase">The passphrase</param>
		/// <param name="timeout">Timeout in seconds</param>
		public async Task WalletPassphraseAsync(string passphrase, int timeout)
		{
			var parameters = new List<object>();
			parameters.Add(passphrase);
			parameters.Add(timeout);
			await SendCommandAsync(RPCOperations.walletpassphrase, parameters.ToArray()).ConfigureAwait(false);
		}

		/// <summary>
		/// Sign a transaction, if RPCClient.Capabilities is set, will call SignRawTransactionWithWallet if available
		/// </summary>
		/// <param name="tx">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public Transaction SignRawTransaction(Transaction tx)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			return SignRawTransactionAsync(tx).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Sign a transaction, if RPCClient.Capabilities is set, will call SignRawTransactionWithWallet if available
		/// </summary>
		/// <param name="tx">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public async Task<Transaction> SignRawTransactionAsync(Transaction tx)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			if (Capabilities != null && Capabilities.SupportSignRawTransactionWith)
			{
				return (await SignRawTransactionWithWalletAsync(new SignRawTransactionRequest()
				{
					Transaction = tx
				}).ConfigureAwait(false)).SignedTransaction;
			}
			else
			{
				var result = await SendCommandAsync(RPCOperations.signrawtransaction, tx.ToHex()).ConfigureAwait(false);
				return ParseTxHex(result.Result["hex"].Value<string>());
			}
		}

		/// <summary>
		/// Sign a transaction
		/// </summary>
		/// <param name="request">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public SignRawTransactionResponse SignRawTransactionWithKey(SignRawTransactionWithKeyRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));
			return SignRawTransactionWithKeyAsync(request).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Sign a transaction
		/// </summary>
		/// <param name="request">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public async Task<SignRawTransactionResponse> SignRawTransactionWithKeyAsync(SignRawTransactionWithKeyRequest request, CancellationToken cancellationToken = default)
		{
			Dictionary<string, object> values = new Dictionary<string, object>();
			values.Add("hexstring", request.Transaction.ToHex());
			JArray keys = new JArray();
			foreach (var k in request.PrivateKeys ?? new Key[0])
			{
				keys.Add(k.GetBitcoinSecret(Network).ToString());
			}
			values.Add("privkeys", keys);

			if (request.PreviousTransactions != null)
			{
				JArray prevs = new JArray();
				foreach (var prev in request.PreviousTransactions)
				{
					JObject prevObj = new JObject();
					prevObj.Add(new JProperty("txid", prev.OutPoint.Hash.ToString()));
					prevObj.Add(new JProperty("vout", prev.OutPoint.N));
					prevObj.Add(new JProperty("scriptPubKey", prev.ScriptPubKey.ToHex()));
					if (prev.RedeemScript != null)
						prevObj.Add(new JProperty("redeemScript", prev.RedeemScript.ToHex()));
					prevObj.Add(new JProperty("amount", prev.Amount.ToDecimal(MoneyUnit.BTC).ToString()));
					prevs.Add(prevObj);
				}
				values.Add("prevtxs", prevs);

				if (request.SigHash.HasValue)
				{
					values.Add("sighashtype", SigHashToString(request.SigHash.Value));
				}
			}

			var result = await SendCommandWithNamedArgsAsync("signrawtransactionwithkey", values, cancellationToken).ConfigureAwait(false);
			var response = new SignRawTransactionResponse();
			response.SignedTransaction = ParseTxHex(result.Result["hex"].Value<string>());
			response.Complete = result.Result["complete"].Value<bool>();
			var errors = result.Result["errors"] as JArray;
			var errorList = new List<SignRawTransactionResponse.ScriptError>();
			if (errors != null)
			{
				foreach (var error in errors)
				{
					var scriptError = new SignRawTransactionResponse.ScriptError();
					scriptError.OutPoint = OutPoint.Parse($"{error["txid"].Value<string>()}-{(int)error["vout"].Value<long>()}");
					scriptError.ScriptSig = Script.FromBytesUnsafe(Encoders.Hex.DecodeData(error["scriptSig"].Value<string>()));
					scriptError.Sequence = new Sequence((uint)error["sequence"].Value<long>());
					scriptError.Error = error["error"].Value<string>();
					errorList.Add(scriptError);
				}
			}
			response.Errors = errorList.ToArray();
			return response;
		}

		/// <summary>
		/// Sign a transaction with wallet keys
		/// </summary>
		/// <param name="request">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public SignRawTransactionResponse SignRawTransactionWithWallet(SignRawTransactionRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));
			return SignRawTransactionWithWalletAsync(request).GetAwaiter().GetResult();
		}

		/// <summary>
		/// Sign a transaction with wallet keys
		/// </summary>
		/// <param name="request">The transaction to be signed</param>
		/// <returns>The signed transaction</returns>
		public async Task<SignRawTransactionResponse> SignRawTransactionWithWalletAsync(SignRawTransactionRequest request, CancellationToken cancellationToken = default)
		{
			Dictionary<string, object> values = new Dictionary<string, object>();
			values.Add("hexstring", request.Transaction.ToHex());

			if (request.PreviousTransactions != null)
			{
				JArray prevs = new JArray();
				foreach (var prev in request.PreviousTransactions)
				{
					JObject prevObj = new JObject();
					prevObj.Add(new JProperty("txid", prev.OutPoint.Hash.ToString()));
					prevObj.Add(new JProperty("vout", prev.OutPoint.N));
					prevObj.Add(new JProperty("scriptPubKey", prev.ScriptPubKey.ToHex()));
					if (prev.RedeemScript != null)
						prevObj.Add(new JProperty("redeemScript", prev.RedeemScript.ToHex()));
					prevObj.Add(new JProperty("amount", prev.Amount.ToDecimal(MoneyUnit.BTC).ToString()));
					prevs.Add(prevObj);
				}
				values.Add("prevtxs", prevs);

				if (request.SigHash.HasValue)
				{
					values.Add("sighashtype", SigHashToString(request.SigHash.Value));
				}
			}

			var result = await SendCommandWithNamedArgsAsync("signrawtransactionwithwallet", values, cancellationToken).ConfigureAwait(false);
			var response = new SignRawTransactionResponse();
			response.SignedTransaction = ParseTxHex(result.Result["hex"].Value<string>());
			response.Complete = result.Result["complete"].Value<bool>();
			var errors = result.Result["errors"] as JArray;
			var errorList = new List<SignRawTransactionResponse.ScriptError>();
			if (errors != null)
			{
				foreach (var error in errors)
				{
					var scriptError = new SignRawTransactionResponse.ScriptError();
					scriptError.OutPoint = OutPoint.Parse($"{error["txid"].Value<string>()}-{(int)error["vout"].Value<long>()}");
					scriptError.ScriptSig = Script.FromBytesUnsafe(Encoders.Hex.DecodeData(error["scriptSig"].Value<string>()));
					scriptError.Sequence = new Sequence((uint)error["sequence"].Value<long>());
					scriptError.Error = error["error"].Value<string>();
					errorList.Add(scriptError);
				}
			}
			response.Errors = errorList.ToArray();
			return response;
		}

		public WalletProcessPSBTResponse WalletProcessPSBT(PSBT psbt, bool sign = true, SigHash hashType = SigHash.All, bool bip32derivs = false)
			 => WalletProcessPSBTAsync(psbt, sign, hashType, bip32derivs).GetAwaiter().GetResult();
		public async Task<WalletProcessPSBTResponse> WalletProcessPSBTAsync(PSBT psbt, bool sign = true, SigHash sighashType = SigHash.All, bool bip32derivs = false)
		{
			if (psbt == null)
				throw new ArgumentNullException(nameof(psbt));

			var response = await SendCommandAsync(RPCOperations.walletprocesspsbt, psbt.ToBase64(), sign, SigHashToString(sighashType), bip32derivs).ConfigureAwait(false);
			var result = (JObject)response.Result;
			var psbt2 = PSBT.Parse(result.Property("psbt").Value.Value<string>(), Network.Main);
			var complete = result.Property("complete").Value.Value<bool>();

			return new WalletProcessPSBTResponse(psbt2, complete);
		}

		public WalletCreateFundedPSBTResponse WalletCreateFundedPSBT(
			TxIn[] inputs,
			Tuple<Dictionary<BitcoinAddress, Money>, Dictionary<string, string>> outputs,
			LockTime locktime,
			FundRawTransactionOptions options = null,
			bool bip32derivs = false
			)
			=> WalletCreateFundedPSBTAsync(inputs, outputs, locktime, options, bip32derivs).GetAwaiter().GetResult();

		public async Task<WalletCreateFundedPSBTResponse> WalletCreateFundedPSBTAsync(
		TxIn[] inputs,
		Tuple<Dictionary<BitcoinAddress, Money>, Dictionary<string, string>> outputs,
		LockTime locktime = default(LockTime),
		FundRawTransactionOptions options = null,
		bool bip32derivs = false,
		CancellationToken cancellationToken = default
		)
		{
			var values = new object[] { };
			if (inputs == null)
				inputs = new TxIn[] { };
			if (outputs == null)
				throw new ArgumentNullException(nameof(outputs));

			var rpcInputs = inputs.Select(i => i.ToRPCInputs()).ToArray();

			var outputToSend = new JObject { };
			if (outputs.Item1 != null)
			{
				foreach (var kv in outputs.Item1)
				{
					outputToSend.Add(kv.Key.ToString(), kv.Value.ToUnit(MoneyUnit.BTC));
				}
			}
			if (outputs.Item2 != null)
			{
				foreach (var kv in outputs.Item2)
				{
					outputToSend.Add(kv.Key, kv.Value);
				}
			}
			JObject jOptions;
			if (options != null)
			{
				jOptions = FundRawTransactionOptionsToJson(options);
			}
			else
			{
				jOptions = (JObject)"";
			}
			RPCResponse response = await SendCommandAsync(
				"walletcreatefundedpsbt",
				cancellationToken,
				rpcInputs,
				outputToSend,
				locktime.Value,
				jOptions,
				bip32derivs).ConfigureAwait(false);
			var result = (JObject)response.Result;
			var psbt = PSBT.Parse(result.Property("psbt").Value.Value<string>(), Network.Main);
			var fee = Money.Coins(result.Property("fee").Value.Value<decimal>());
			var changePos = result.Property("changepos").Value.Value<int>();
			var tmp = changePos == -1 ? (int?)null : (int?)changePos;
			return new WalletCreateFundedPSBTResponse { PSBT = psbt, Fee = fee, ChangePos = tmp };
		}
		public WalletCreateFundedPSBTResponse WalletCreateFundedPSBT(
			TxIn[] inputs,
			Dictionary<BitcoinAddress, Money> outputs,
			LockTime locktime,
			FundRawTransactionOptions options = null,
			bool bip32derivs = false
		) => WalletCreateFundedPSBT(
			inputs,
			Tuple.Create<Dictionary<BitcoinAddress, Money>, Dictionary<string, string>>(outputs, null),
			locktime,
			options,
			bip32derivs);

		public WalletCreateFundedPSBTResponse WalletCreateFundedPSBT(
			TxIn[] inputs,
			Dictionary<string, string> outputs,
			LockTime locktime,
			FundRawTransactionOptions options = null,
			bool bip32derivs = false
		) => WalletCreateFundedPSBT(
			inputs,
			Tuple.Create<Dictionary<BitcoinAddress, Money>, Dictionary<string, string>>(null, outputs),
			locktime,
			options,
			bip32derivs);

		public string SigHashToString(SigHash value)
		{
			switch (value)
			{
				case SigHash.All:
					return "ALL";
				case SigHash.None:
					return "NONE";
				case SigHash.Single:
					return "SINGLE";
				case SigHash.All | SigHash.AnyoneCanPay:
					return "ALL|ANYONECANPAY";
				case SigHash.None | SigHash.AnyoneCanPay:
					return "NONE|ANYONECANPAY";
				case SigHash.Single | SigHash.AnyoneCanPay:
					return "SINGLE|ANYONECANPAY";
				case 0:
					return "DEFAULT";
				default:
					throw new NotSupportedException();
			}
		}

		private Money GetMoney(RPCResponse response)
		{
			decimal coins = response.Result is JValue jVal
							? Convert.ToDecimal(jVal.Value)
							: response.Result.Value<decimal>();
			return Money.Coins(coins);
		}
	}
}
#endif
