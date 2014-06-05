using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
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



		public RPCResponse SendCommand(RPCRequest request)
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
			Stream dataStream = webRequest.GetRequestStream();
			dataStream.Write(bytes, 0, bytes.Length);
			dataStream.Close();

			RPCResponse response = null;
			try
			{
				WebResponse webResponse = webRequest.GetResponse();
				response = RPCResponse.Load(webResponse.GetResponseStream());
				response.ThrowIfError();
			}
			catch(WebException ex)
			{
				if(ex.Response == null)
					throw;
				response = RPCResponse.Load(ex.Response.GetResponseStream());
				response.ThrowIfError();
			}
			return response;
		}

		public UnspentCoin[] ListUnspent()
		{
			var response = SendCommand("listunspent");
			return ((JArray)response.Result).Select(i => new UnspentCoin((JObject)i)).ToArray();
		}

		public BitcoinAddress GetAccountAddress(string account)
		{
			var response = SendCommand("getaccountaddress", account);
			return Network.CreateFromBase58Data<BitcoinAddress>((string)response.Result);
		}

		public BitcoinSecret DumpPrivKey(BitcoinAddress address)
		{
			var response = SendCommand("dumpprivkey", address.ToString());
			return Network.CreateFromBase58Data<BitcoinSecret>((string)response.Result);
		}

		public BitcoinSecret GetAccountSecret(string account)
		{
			var address = GetAccountAddress(account);
			return DumpPrivKey(address);
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

		public Transaction GetRawTransaction(uint256 txid)
		{
			var response = SendCommand("getrawtransaction", txid.ToString());
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

		public void LockUnspent(params OutPoint[] outpoints)
		{
			LockUnspentCore(false, outpoints);
		}
		public void UnlockUnspent(params OutPoint[] outpoints)
		{
			LockUnspentCore(true, outpoints);
		}

		private void LockUnspentCore(bool unlock, OutPoint[] outpoints)
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
			SendCommand("lockunspent", parameters.ToArray());
		}

		public BlockHeader GetBlockHeader(int height)
		{
			var hash = GetBlockHash(height);
			return GetBlockHeader(hash);
		}

		public BlockHeader GetBlockHeader(uint256 blockHash)
		{
			var resp = SendCommand("getblock", blockHash.ToString());
			BlockHeader header = new BlockHeader();
			header.Version = (int)resp.Result["version"];
			header.Nonce = (uint)resp.Result["nonce"];
			header.Bits = new Target(Encoders.Hex.DecodeData((string)resp.Result["bits"]));
			if(resp.Result["previousblockhash"] != null)
			{
				header.HashPrevBlock = new uint256(Encoders.Hex.DecodeData((string)resp.Result["previousblockhash"]),false);
			}
			if(resp.Result["time"] != null)
			{
				header.BlockTime = Utils.UnixTimeToDateTime((uint)resp.Result["time"]);
			}
			if(resp.Result["merkleroot"] != null)
			{
				header.HashMerkleRoot = new uint256(Encoders.Hex.DecodeData((string)resp.Result["merkleroot"]),false);
			}
			return header;
		}

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
					yield return GetRawTransaction(new uint256(item.ToString()));
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

		public int GetBlockCount()
		{
			return (int)SendCommand("getblockcount").Result;
		}
	}
}
