using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

	public class RPCClient
	{
		private readonly NetworkCredential _Credentials;
		public NetworkCredential Credentials
		{
			get
			{
				return _Credentials;
			}
		}
		private readonly Uri _Address;
		public Uri Address
		{
			get
			{
				return _Address;
			}
		}
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
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
			_Credentials = credentials;
			_Address = address;
			_Network = network;
		}

		public RPCResponse SendCommand(RPCOperations commandName, params object[] parameters)
		{
			return SendCommand(commandName.ToString(), parameters);
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
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Address);
			webRequest.Credentials = Credentials;
			webRequest.ContentType = "application/json-rpc";
			webRequest.Method = "POST";

			var writer = new StringWriter();
			request.WriteJSON(writer);
			writer.Flush();
			var json = writer.ToString();
			var bytes = Encoding.UTF8.GetBytes(json);
			webRequest.ContentLength = bytes.Length;
			Stream dataStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false);
			dataStream.Write(bytes, 0, bytes.Length);
			dataStream.Close();
			RPCResponse response = null;
			try
			{
				WebResponse webResponse = await webRequest.GetResponseAsync().ConfigureAwait(false);
				response = RPCResponse.Load(webResponse.GetResponseStream());
				if(throwIfRPCError)
					response.ThrowIfError();
			}
			catch(WebException ex)
			{
				if(ex.Response == null)
					throw;
				response = RPCResponse.Load(ex.Response.GetResponseStream());
				if(throwIfRPCError)
					response.ThrowIfError();
			}
			return response;
		}

		public UnspentCoin[] ListUnspent()
		{
			var response = SendCommand("listunspent");
			return ((JArray)response.Result).Select(i => new UnspentCoin((JObject)i)).ToArray();
		}
		public async Task<UnspentCoin[]> ListUnspentAsync()
		{
			var response = await SendCommandAsync("listunspent");
			return ((JArray)response.Result).Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		public BitcoinAddress GetAccountAddress(string account)
		{
			var response = SendCommand("getaccountaddress", account);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}
		public async Task<BitcoinAddress> GetAccountAddressAsync(string account)
		{
			var response = await SendCommandAsync("getaccountaddress", account);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}

		public BitcoinSecret DumpPrivKey(BitcoinAddress address)
		{
			var response = SendCommand("dumpprivkey", address.ToString());
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}
		public async Task<BitcoinSecret> DumpPrivKeyAsync(BitcoinAddress address)
		{
			var response = await SendCommandAsync("dumpprivkey", address.ToString());
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}

		public uint256 GetBestBlockHash()
		{
			return new uint256((string)SendCommand("getbestblockhash").Result);
		}
		public async Task<uint256> GetBestBlockHashAsync()
		{
			return new uint256((string)(await SendCommandAsync("getbestblockhash")).Result);
		}

		public BitcoinSecret GetAccountSecret(string account)
		{
			var address = GetAccountAddress(account);
			return DumpPrivKey(address);
		}
		public async Task<BitcoinSecret> GetAccountSecretAsync(string account)
		{
			var address = await GetAccountAddressAsync(account);
			return await DumpPrivKeyAsync(address);
		}

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
			var response = await SendCommandAsync("decoderawtransaction", rawHex);
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
			var response = SendCommand(new RPCRequest("getrawtransaction", new[] { txid.ToString() }), throwIfNotFound);
			if(throwIfNotFound)
				response.ThrowIfError();
			if(response.Error != null && response.Error.Code == RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY)
				return null;
			else
				response.ThrowIfError();

			var tx = new Transaction();
			tx.ReadWrite(Encoders.Hex.DecodeData(response.Result.ToString()));
			return tx;
		}

		public void SendRawTransaction(byte[] bytes)
		{
			SendCommand("sendrawtransaction", Encoders.Hex.EncodeData(bytes));
		}
		public void SendRawTransaction(Transaction tx)
		{
			SendRawTransaction(tx.ToBytes());
		}
		public Task SendRawTransactionAsync(byte[] bytes)
		{
			return SendCommandAsync("sendrawtransaction", Encoders.Hex.EncodeData(bytes));
		}
		public Task SendRawTransactionAsync(Transaction tx)
		{
			return SendRawTransactionAsync(tx.ToBytes());
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
				return;
			}
		}
		private async Task LockUnspentCoreAsync(bool unlock, OutPoint[] outpoints)
		{
			if(outpoints == null || outpoints.Length == 0)
				return;
			List<object> parameters = new List<object>();
			parameters.Add(unlock);
			JArray array = new JArray();
			parameters.Add(array);
			foreach(var outp in outpoints)
			{
				var obj = new JObject();
				obj["txid"] = outp.Hash.ToString();
				obj["vout"] = outp.N;
				array.Add(obj);
			}
			await SendCommandAsync("lockunspent", parameters.ToArray());
		}

		public BlockHeader GetBlockHeader(int height)
		{
			var hash = GetBlockHash(height);
			return GetBlockHeader(hash);
		}
		public async Task<BlockHeader> GetBlockHeaderAsync(int height)
		{
			var hash = await GetBlockHashAsync(height);
			return await GetBlockHeaderAsync(hash);
		}

		/// <summary>
		/// Get the a whole block, will fail if bitcoinq does not run with txindex=1 and one of the transaction of the block is entirely spent
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public Block GetBlock(uint256 blockId)
		{
			var resp = SendCommand("getblock", blockId.ToString());
			var header = ParseBlockHeader(resp);
			Block block = new Block(header);
			var transactions = resp.Result["tx"] as JArray;
			try
			{
				foreach(var tx in transactions)
				{
					block.AddTransaction(GetRawTransaction(new uint256(tx.ToString())));
				}
			}
			catch(RPCException ex)
			{
				if(ex.RPCCode == RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY)
					return null;
				throw;
			}
			return block;
		}

		public BlockHeader GetBlockHeader(uint256 blockHash)
		{
			var resp = SendCommand("getblock", blockHash.ToString());
			return ParseBlockHeader(resp);
		}
		public async Task<BlockHeader> GetBlockHeaderAsync(uint256 blockHash)
		{
			var resp = await SendCommandAsync("getblock", blockHash.ToString());
			return ParseBlockHeader(resp);
		}

		private BlockHeader ParseBlockHeader(RPCResponse resp)
		{
			BlockHeader header = new BlockHeader();
			header.Version = (int)resp.Result["version"];
			header.Nonce = (uint)resp.Result["nonce"];
			header.Bits = new Target(Encoders.Hex.DecodeData((string)resp.Result["bits"]));
			if(resp.Result["previousblockhash"] != null)
			{
				header.HashPrevBlock = new uint256(Encoders.Hex.DecodeData((string)resp.Result["previousblockhash"]), false);
			}
			if(resp.Result["time"] != null)
			{
				header.BlockTime = Utils.UnixTimeToDateTime((uint)resp.Result["time"]);
			}
			if(resp.Result["merkleroot"] != null)
			{
				header.HashMerkleRoot = new uint256(Encoders.Hex.DecodeData((string)resp.Result["merkleroot"]), false);
			}
			return header;
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
					var result = GetRawTransaction(new uint256(item.ToString()), false);
					if(result != null)
						yield return result;
				}
			}

		}

		public IEnumerable<Transaction> GetTransactions(int height)
		{
			return GetTransactions(GetBlockHash(height));
		}

		public uint256 GetBlockHash(int height)
		{
			var resp = SendCommand("getblockhash", height);
			return new uint256(resp.Result.ToString());
		}

		public async Task<uint256> GetBlockHashAsync(int height)
		{
			var resp = await SendCommandAsync("getblockhash", height);
			return new uint256(resp.Result.ToString());
		}


		public int GetBlockCount()
		{
			return (int)SendCommand("getblockcount").Result;
		}
		public async Task<int> GetBlockCountAsync()
		{
			return (int)(await SendCommandAsync("getblockcount")).Result;
		}

		public uint256[] GetRawMempool()
		{
			var result = SendCommand("getrawmempool");
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(s => new uint256(s)).ToArray();
		}
		public async Task<uint256[]> GetRawMempoolAsync()
		{
			var result = await SendCommandAsync("getrawmempool");
			var array = (JArray)result.Result;
			return array.Select(o => (string)o).Select(s => new uint256(s)).ToArray();
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
				grouping.Account = group[0][2].ToString();

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
	}
}
