using NBitcoin.RPC;
using Newtonsoft.Json;
using System;
using System.Net;

namespace NBitcoin.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			RPCClient client = new RPCClient(new NetworkCredential("amo2017", "amo123456"), "127.0.0.1:8332", Network.Main);
			Console.WriteLine(JsonConvert.SerializeObject(client.ListAccounts()));
			Console.WriteLine("Hello World!");
		}
	}
}
