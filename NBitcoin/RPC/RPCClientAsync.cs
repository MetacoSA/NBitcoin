using NBitcoin.DataEncoders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
    public class RPCClientAsync
    {
        private HttpClient http;

        public Uri Address
		{
			get
			{
				return http.BaseAddress;
			}
		}

        public NetworkCredential Credentials { get; private set; }

        public Network Network { get; private set; }

        public RPCClientAsync(NetworkCredential credentials, string host, Network network)
			: this(credentials, (new UriBuilder() { Host = host, Scheme = "http", Port = network.RPCPort}).Uri , network)
		{
		}

		public RPCClientAsync(NetworkCredential credentials, Uri address, Network network = null)
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
			Credentials = credentials;
			Network = network;

            var handler = new HttpClientHandler();
            handler.Credentials = credentials;
            http = new HttpClient(handler);
            http.BaseAddress = address;
		}

		public async Task<RPCResponse> SendCommand(RPCOperations commandName, params object[] parameters)
		{
			return await SendCommand(commandName.ToString(), parameters);
		}

		/// <summary>
		/// Send a command
		/// </summary>
		/// <param name="commandName">https://en.bitcoin.it/wiki/Original_Bitcoin_client/API_calls_list</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public async Task<RPCResponse> SendCommand(string commandName, params object[] parameters)
		{
			return await SendCommand(new RPCRequest(commandName, parameters));
		}

		public async Task<RPCResponse> SendCommand(RPCRequest request, bool throwIfRPCError = true)
		{
            var httpResponse = await http.PostAsync("", request, new JsonMediaTypeFormatter(), "application/json-rpc");

			RPCResponse response = null;

            response = await httpResponse.Content.ReadAsAsync<RPCResponse>();
             
			if(throwIfRPCError)
			    response.ThrowIfError();

            return response;
		}

		public async Task<UnspentCoin[]> ListUnspent()
		{
			var response = await SendCommand(RPCOperations.listunspent);
			return ((JArray)response.Result).Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		public async Task<BitcoinAddress> GetAccountAddress(string account)
		{
			var response = await SendCommand(RPCOperations.getaccountaddress, account);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}

		public async Task<BitcoinSecret> DumpPrivKey(BitcoinAddress address)
		{
			var response = await SendCommand(RPCOperations.dumpprivkey, address.ToString());
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}

		public async Task<uint256> GetBestBlockHash()
		{
            var response = await SendCommand(RPCOperations.getbestblockhash);
			return new uint256((string)response.Result);
		}

		public async Task<BitcoinSecret> GetAccountSecret(string account)
		{
			var address = await GetAccountAddress(account);
			return await DumpPrivKey(address);
		}

		public async Task<Transaction> DecodeRawTransaction(string rawHex)
		{
			var response = await SendCommand(RPCOperations.decoderawtransaction, rawHex);
			return Transaction.Parse(response.Result.ToString(), RawFormat.Satoshi);
		}
		public async Task<Transaction> DecodeRawTransaction(byte[] raw)
		{
			return await DecodeRawTransaction(Encoders.Hex.EncodeData(raw));
		}

		/// <summary>
		/// getrawtransaction only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="txid"></param>
		/// <returns></returns>
		public async Task<Transaction> GetRawTransaction(uint256 txid, bool throwIfNotFound = true)
		{
			var response = await SendCommand(new RPCRequest("getrawtransaction", new[] { txid.ToString() }), throwIfNotFound);
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

		public async Task SendRawTransaction(byte[] bytes)
		{
			await SendCommand(RPCOperations.sendrawtransaction, Encoders.Hex.EncodeData(bytes));
		}

		public async Task SendRawTransaction(Transaction tx)
		{
			await SendRawTransaction(tx.ToBytes());
		}

		public async Task LockUnspent(params OutPoint[] outpoints)
		{
			await LockUnspentCore(false, outpoints);
		}

		public async Task UnlockUnspent(params OutPoint[] outpoints)
		{
			await LockUnspentCore(true, outpoints);
		}

		private async Task LockUnspentCore(bool unlock, OutPoint[] outpoints)
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
			await SendCommand(RPCOperations.lockunspent, parameters.ToArray());
		}

		public async Task<BlockHeader> GetBlockHeader(int height)
		{
			var hash = await GetBlockHash(height);
			return await GetBlockHeader(hash);
		}

		/// <summary>
		/// Get the a whole block, will fail if bitcoinq does not run with txindex=1 and one of the transaction of the block is entirely spent
		/// </summary>
		/// <param name="blockId"></param>
		/// <returns></returns>
		public async Task<Block> GetBlock(uint256 blockId)
		{
			var resp = await SendCommand(RPCOperations.getblock, blockId.ToString());
			var header = ParseBlockHeader(resp);
			Block block = new Block(header);
			var transactions = resp.Result["tx"] as JArray;
			try
			{
				foreach(var tx in transactions)
				{
					block.AddTransaction(await GetRawTransaction(new uint256(tx.ToString())));
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

		public async Task<BlockHeader> GetBlockHeader(uint256 blockHash)
		{
			var resp = await SendCommand(RPCOperations.getblock, blockHash.ToString());
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

        // TODO: Change sync IEnumerable<T> to async IObservable<T> using Rx.Net
        /// <summary>
		/// GetTransactions only returns on txn which are not entirely spent unless you run bitcoinq with txindex=1.
		/// </summary>
		/// <param name="blockHash"></param>
		/// <returns></returns>
        public IEnumerable<Transaction> GetTransactionsSync(uint256 blockHash)
        {
            if (blockHash == null)
                throw new ArgumentNullException("blockHash");

            var resp = SendCommand(RPCOperations.getblock, blockHash.ToString()).Result;

            var tx = resp.Result["tx"] as JArray;
            if (tx != null)
            {
                foreach (var item in tx)
                {
                    var result = GetRawTransaction(new uint256(item.ToString()), false).Result;
                    if (result != null)
                        yield return result;
                }
            }
        }

        // With Rx.Net, we can do this:
        //public IEnumerable<Transaction> GetTransactions(uint256 blockHash)
        //{
        //    if(blockHash == null)
        //        throw new ArgumentNullException("blockHash");

        //    return GetTransactionsSource(blockHash).ToEnumerable();
        //}

        //private IObservable<Transaction> GetTransactionsSource(uint256 blockHash)
        //{
        //    return Observable.Create<string>(
        //            async obs =>
        //            {
        //                var resp = await SendCommand(RPCOperations.getblock, blockHash.ToString());

        //                var tx = resp.Result["tx"] as JArray;
        //                if (tx != null)
        //                {
        //                    foreach (var item in tx)
        //                    {
        //                        var result = await GetRawTransaction(new uint256(item.ToString()), false);
        //                        if (result != null)
        //                            obs.OnNext(result);
        //                    }
        //                }
        //            });
        //}

        // TODO: Change sync IEnumerable<T> to async IObservable<T> using Rx.Net
        public IEnumerable<Transaction> GetTransactionsSync(int height)
		{
			return GetTransactionsSync(GetBlockHash(height).Result);
		}

		public async Task<uint256> GetBlockHash(int height)
		{
			var resp = await SendCommand(RPCOperations.getblockhash, height);
			return new uint256(resp.Result.ToString());
		}

		public async Task<int> GetBlockCount()
		{
            var response = await SendCommand(RPCOperations.getblockcount);
			return (int)response.Result;
		}

		public async Task<uint256[]> GetRawMempool()
		{
			var result = await SendCommand(RPCOperations.getrawmempool);
			var array = (JArray)result.Result;
			return array.Select(o => new uint256((string)o)).ToArray();
		}
    }
}
