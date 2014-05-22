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
			return Transaction.Parse(response.Result.ToString(), RawFormat.Satoshi);
		}

		public void SendRawTransaction(Transaction tx)
		{
			var response = SendCommand("getrawtransaction", Encoders.Hex.EncodeData(tx.ToBytes()));
		}
	}
}
