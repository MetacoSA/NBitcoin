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
		Category			Name						Implemented 
		------------------ --------------------------- -----------------------

		------------------ Wallet
		wallet			 addmultisigaddress
		wallet			 backupwallet				 Yes
		wallet			 dumpprivkey				  Yes
		wallet			 dumpwallet
		wallet			 encryptwallet
		wallet			 getaccountaddress			Yes
		wallet			 getaccount
		wallet			 getaddressesbyaccount		Yes
		wallet			 getbalance
		wallet			 getnewaddress
		wallet			 getrawchangeaddress
		wallet			 getreceivedbyaccount
		wallet			 getreceivedbyaddress		 Yes
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
	public partial class RPCClient
	{
		// backupwallet 

		public void BackupWallet(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			SendCommand("backupwallet", path);
		}

		public async Task BackupWalletAsync(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("path");
			await SendCommandAsync("backupwallet", path).ConfigureAwait(false);
		}


		// dumpprivkey

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


		// getaccountaddress

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


		// getaddressesbyaccount 

		/// <summary>
		/// Returns a list of every address assigned to a particular account.
		/// </summary>
		/// <param name="account">
		/// The name of the account containing the addresses to get. To get addresses from the default account, 
		/// pass an empty string ("").
		/// </param>
		/// <returns>
		/// A collection of all addresses belonging to the specified account. 
		/// If the account has no addresses, the collection will be empty
		/// </returns>
		public IEnumerable<BitcoinAddress> GetAddressesByAccount(string account)
		{
			var response = SendCommand(RPCOperations.getaddressesbyaccount, account);
			return response.Result.Select(t => Network.CreateFromBase58Data<BitcoinAddress>((string)t));
		}

		public FundRawTransactionResponse FundRawTransaction(Transaction transaction, FundRawTransactionOptions options = null)
		{
			return FundRawTransactionAsync(transaction, options).GetAwaiter().GetResult();
		}
		public async Task<FundRawTransactionResponse> FundRawTransactionAsync(Transaction transaction, FundRawTransactionOptions options = null)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");

			RPCResponse response = null;
			if(options != null)
			{
				var jOptions = new JObject();
				if(options.ChangeAddress != null)
					jOptions.Add(new JProperty("changeAddress", options.ChangeAddress.ToString()));
				if(options.ChangePosition != null)
					jOptions.Add(new JProperty("changePosition", options.ChangePosition.Value));
				jOptions.Add(new JProperty("includeWatching", options.IncludeWatching));
				jOptions.Add(new JProperty("lockUnspents", options.LockUnspents));
				if(options.ReserveChangeKey != null)
					jOptions.Add(new JProperty("reserveChangeKey", options.ReserveChangeKey));
				if(options.FeeRate != null)
					jOptions.Add(new JProperty("feeRate", options.FeeRate.GetFee(1000).ToDecimal(MoneyUnit.BTC)));
				if(options.SubtractFeeFromOutputs != null)
				{
					JArray array = new JArray();
					foreach(var v in options.SubtractFeeFromOutputs)
					{
						array.Add(new JValue(v));
					}
					jOptions.Add(new JProperty("subtractFeeFromOutputs", array));
				}
				response = await SendCommandAsync("fundrawtransaction", ToHex(transaction), jOptions).ConfigureAwait(false);
			}
			else
			{
				response = await SendCommandAsync("fundrawtransaction", ToHex(transaction)).ConfigureAwait(false);
			}
			var r = (JObject)response.Result;
			return new FundRawTransactionResponse()
			{
				Transaction = new Transaction(r["hex"].Value<string>()),
				Fee = Money.Coins(r["fee"].Value<decimal>()),
				ChangePos = r["changepos"].Value<int>()
			};
		}

		//NBitcoin internally put a bit in the version number to make difference between transaction without input and transaction with witness.
		private string ToHex(Transaction tx)
		{
			// if there is inputs, then it can't be confusing
			if(tx.Inputs.Count > 0)
				return tx.ToHex();
			// if there is, do this ACK so that NBitcoin does not change the version number
			return Encoders.Hex.EncodeData(tx.ToBytes(NBitcoin.Protocol.ProtocolVersion.WITNESS_VERSION - 1));
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
			var response = SendCommand(RPCOperations.getreceivedbyaddress, address.ToWif());
			return Money.Coins(response.Result.Value<decimal>());
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
			var response = SendCommand(RPCOperations.getreceivedbyaddress, address.ToWif(), confirmations);
			return Money.Coins(response.Result.Value<decimal>());
		}


		// importprivkey

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


		// importaddress

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


		// listaccounts

		/// <summary>
		/// Lists accounts and their balances, with the default number of confirmations for balances (1), 
		/// and not including watch only addresses (default false).
		/// </summary>
		public IEnumerable<RPCAccount> ListAccounts()
		{
			var response = SendCommand(RPCOperations.listaccounts);
			return AsRPCAccount(response);
		}

		/// <summary>
		/// Lists accounts and their balances, based on the specified number of confirmations.
		/// </summary>
		/// <param name="confirmations">
		/// The minimum number of confirmations an externally-generated transaction must have before 
		/// it is counted towards the balance. Transactions generated by this node are counted immediately. 
		/// Typically, externally-generated transactions are payments to this wallet and transactions 
		/// generated by this node are payments to other wallets. Use 0 to count unconfirmed transactions. 
		/// Default is 1.
		/// </param>
		/// <returns>
		/// A list of accounts and their balances.
		/// </returns>
		public IEnumerable<RPCAccount> ListAccounts(int confirmations)
		{
			var response = SendCommand(RPCOperations.listaccounts, confirmations);
			return AsRPCAccount(response);
		}

		/// <summary>
		/// Lists accounts and their balances, based on the specified number of confirmations, 
		/// and including watch only accounts if specified. (Added in Bitcoin Core 0.10.0)
		/// </summary>
		/// <param name="confirmations">
		/// The minimum number of confirmations an externally-generated transaction must have before 
		/// it is counted towards the balance. Transactions generated by this node are counted immediately. 
		/// Typically, externally-generated transactions are payments to this wallet and transactions 
		/// generated by this node are payments to other wallets. Use 0 to count unconfirmed transactions. 
		/// Default is 1.
		/// </param>
		/// <param name="includeWatchOnly">
		/// If set to true, include watch-only addresses in details and calculations as if they were 
		/// regular addresses belonging to the wallet. If set to false (the default), treat watch-only 
		/// addresses as if they didn’t belong to this wallet.
		/// </param>
		/// <returns>
		/// A list of accounts and their balances.
		/// </returns>
		public IEnumerable<RPCAccount> ListAccounts(int confirmations,
			// Added in Bitcoin Core 0.10.0
			bool includeWatchOnly)
		{
			var response = SendCommand(RPCOperations.listaccounts, confirmations, includeWatchOnly);
			return AsRPCAccount(response);
		}

		private IEnumerable<RPCAccount> AsRPCAccount(RPCResponse response)
		{
			var obj = (JObject)response.Result;
			foreach (var prop in obj.Properties())
			{
				yield return new RPCAccount()
				{
					AccountName = prop.Name,
					Amount = Money.Coins((decimal)prop.Value)
				};
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
				grouping.PublicAddress = BitcoinAddress.Create(group[0][0].ToString());
				grouping.Amount = Money.Coins(group[0][1].Value<decimal>());
				grouping.Account = group[0].Count() > 2 ? group[0][2].ToString() : null;

				foreach (var subgroup in group.Skip(1))
				{
					var change = new ChangeAddress();
					change.Address = BitcoinAddress.Create(subgroup[0].ToString());
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
			var response = SendCommand("listunspent");
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet,
		/// specifying the minimum and maximum number of confirmations to include,
		/// and the list of addresses to include. 
		/// </summary>
		public UnspentCoin[] ListUnspent(int minconf, int maxconf, params BitcoinAddress[] addresses)
		{
			var addr = from a in addresses select a.ToString();
			var response = SendCommand("listunspent", minconf, maxconf, addr.ToArray());
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}
		
		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet. 
		/// </summary>
		public async Task<UnspentCoin[]> ListUnspentAsync()
		{
			var response = await SendCommandAsync("listunspent").ConfigureAwait(false);
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		/// <summary>
		/// Returns an array of unspent transaction outputs belonging to this wallet,
		/// specifying the minimum and maximum number of confirmations to include,
		/// and the list of addresses to include. 
		/// </summary>
		public async Task<UnspentCoin[]> ListUnspentAsync(int minconf, int maxconf, params BitcoinAddress[] addresses)
		{
			var addr = from a in addresses select a.ToString();
			var response = await SendCommandAsync("listunspent", minconf, maxconf, addr.ToArray()).ConfigureAwait(false);
			return response.Result.Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		//listlockunspent
		public async Task<OutPoint[]> ListLockUnspentAsync()
		{
			var unspent = await SendCommandAsync("listlockunspent").ConfigureAwait(false);
			return ((JArray)unspent.Result)
				.Select(i => new OutPoint(new uint256(i["txid"].Value<string>()), i["vout"].Value<int>()))
				.ToArray();
		}

		public OutPoint[] ListLockUnspent()
		{
			return ListLockUnspentAsync().GetAwaiter().GetResult();
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
			try
			{
				LockUnspentCoreAsync(unlock, outpoints).Wait();
			}
			catch (AggregateException ex)
			{
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			}
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
			await SendCommandAsync("lockunspent", parameters.ToArray()).ConfigureAwait(false);
		}
	}
}
#endif