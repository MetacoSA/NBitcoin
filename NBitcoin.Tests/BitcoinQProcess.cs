#if !NOSOCKET
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin.Watcher
{
	class BitcoinQProcess
	{
		public static IEnumerable<BitcoinQProcess> List()
		{
			return Process.GetProcesses()
					   .Where(n => n.ProcessName == "bitcoin-qt" || n.ProcessName == "bitcoind")
					   .Select(p => new BitcoinQProcess(p));
		}

		private Process _Process;

		public BitcoinQProcess(Process process)
		{
			this._Process = process;

			WqlObjectQuery wqlQuery =
			new WqlObjectQuery("Select * from Win32_Process where Handle=\"" + process.Id + "\"");
			ManagementObjectSearcher searcher =
				new ManagementObjectSearcher(wqlQuery);
			var result = searcher.Get().OfType<ManagementObject>().FirstOrDefault();

			if(result != null)
			{
				_CommandLine = result["CommandLine"].ToString();
				var configurationFile = GetConfigurationFile();
				if(configurationFile != null && File.Exists(configurationFile))
				{
					_Configuration = File.ReadAllText(configurationFile);
					_Configuration = Regex.Replace(_Configuration, "(#.*)", "");

					ParseConfigurationFile();
				}

				ParseCommandLine();
				FillWithDefault();
			}
		}

		private void FillWithDefault()
		{
			if(!Parameters.ContainsKey("testnet"))
				Parameters.Add("testnet", "0");

			if(!Parameters.ContainsKey("server"))
				Parameters.Add("server", "0");

			if(!Parameters.ContainsKey("rpcport"))
			{
				Parameters.Add("rpcport", Parameters["testnet"] == "0" ? Network.Main.RPCPort.ToString() : Network.TestNet.RPCPort.ToString());
			}

			if(!Parameters.ContainsKey("datadir"))
			{
				var datadir = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						"Bitcoin"
						);
				Parameters.Add("datadir", datadir);
			}

			if(!Parameters.ContainsKey("blkdir"))
			{
				var datadir = Parameters["datadir"];
				var isMain = Parameters["testnet"] == "0";
				var blkdir = isMain ? Path.Combine(datadir, "blocks") : Path.Combine(datadir, "testnet3\\blocks");
				Parameters.Add("blkdir", blkdir);
			}
		}

		private void ParseCommandLine()
		{
			if(ContainsParameter("testnet"))
			{
				Parameters.AddOrReplace("testnet", "1");
			}
			if(ContainsParameter("server"))
				Parameters.AddOrReplace("server", "1");

			var rpcuser = GetParameter("rpcuser");
			if(rpcuser != null)
				Parameters.AddOrReplace("rpcuser", rpcuser);

			var rpcpassword = GetParameter("rpcpassword");
			if(rpcpassword != null)
				Parameters.AddOrReplace("rpcpassword", rpcpassword);

			var rpcport = GetParameter("rpcport");
			if(rpcport != null)
				Parameters.AddOrReplace("rpcport", rpcport);

			var datadir = GetParameter("datadir");
			if(datadir != null)
				Parameters.AddOrReplace("datadir", datadir);
		}

		private bool ContainsParameter(string parameter)
		{
			return _CommandLine.Contains("-" + parameter);
		}


		public Dictionary<string, string> Parameters = new Dictionary<string, string>();

		private void ParseConfigurationFile()
		{
			var testnet = GetConfigField("testnet");
			if(testnet != null)
				Parameters.Add("testnet", testnet);

			var server = GetConfigField("server");
			if(server != null)
				Parameters.Add("server", server);

			var rpcuser = GetConfigField("rpcuser");
			if(rpcuser != null)
				Parameters.Add("rpcuser", rpcuser);

			var rpcpassword = GetConfigField("rpcpassword");
			if(rpcpassword != null)
				Parameters.Add("rpcpassword", rpcpassword);

			var rpcport = GetConfigField("rpcport");
			if(rpcport != null)
				Parameters.Add("rpcport", rpcport);


			var datadir = GetConfigField("datadir");
			if(datadir != null)
				Parameters.Add("datadir", datadir);
		}

		public string RPCPassword
		{
			get
			{
				return Parameters["rpcpassword"];
			}
		}

		public string RPCService
		{
			get
			{
				return "http://localhost:" + Parameters["rpcport"] + "/";
			}
		}

		private string GetConfigField(string field)
		{
			var match = Regex.Match(_Configuration, field + "=([^\r]*)");
			if(match.Success)
				return match.Groups[1].Value.Trim();
			return null;
		}




		private string GetConfigurationFile()
		{
			var confFile = GetParameter("conf");
			if(confFile == null)
			{
				confFile = GetParameter("datadir");
				if(confFile == null)
				{
					confFile =
						Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						"Bitcoin"
						);
				}
				confFile = Path.Combine(confFile, "bitcoin.conf");
			}
			return confFile;
		}

		private readonly string _Configuration;
		public string Configuration
		{
			get
			{
				return _Configuration;
			}
		}

		private readonly string _CommandLine;
		public string CommandLine
		{
			get
			{
				return _CommandLine;
			}
		}

		private string GetParameter(string parameter)
		{
			var match = Regex.Match(_CommandLine, "-" + parameter + "=\"(.*?)\"");
			if(!match.Success)
			{
				match = Regex.Match(_CommandLine, "-" + parameter + "=([^ ]*)");
				if(!match.Success)
				{
					return null;
				}
			}
			return match.Groups[1].Value;
		}

		public bool Testnet
		{
			get
			{
				return Parameters["testnet"] == "1";
			}
		}

		public Network Network
		{
			get
			{
				return Testnet ? Network.TestNet : Network.Main;
			}
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			foreach(var key in Parameters.Keys)
			{
				builder.AppendLine(key + "=" + Parameters[key]);
			}
			return builder.ToString();
		}

		public bool Server
		{
			get
			{
				return Parameters["server"] == "1";
			}
		}

		public string RPCUser
		{
			get
			{
				return Parameters["rpcuser"];
			}
		}

		public RPCClient CreateClient()
		{
			if(!Server)
				throw new InvalidOperationException("This BitcoinQ process is not a server (-server parameter)");
			RPCClient client = new RPCClient(new System.Net.NetworkCredential(RPCUser, RPCPassword), new Uri(RPCService, UriKind.Absolute), Network);

			return client;
		}

		
	}


}
#endif