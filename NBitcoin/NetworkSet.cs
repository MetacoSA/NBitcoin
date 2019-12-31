using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface INetworkSet
	{
		Network GetNetwork(NetworkType networkType);
		Network Mainnet
		{
			get;
		}
		Network Testnet
		{
			get;
		}
		Network Regtest
		{
			get;
		}
		string CryptoCode
		{
			get;
		}
	}
	public abstract class NetworkSetBase : INetworkSet
	{
		object l = new object();
		public NetworkSetBase()
		{
		}
		public Network GetNetwork(NetworkType networkType)
		{
			switch (networkType)
			{
				case NetworkType.Mainnet:
					return Mainnet;
				case NetworkType.Testnet:
					return Testnet;
				case NetworkType.Regtest:
					return Regtest;
			}
			throw new NotSupportedException(networkType.ToString());
		}

		volatile bool _Registered;
		volatile bool _Registering;
		public void EnsureRegistered()
		{
			if (_Registered)
				return;
			lock (l)
			{
				if (_Registered)
					return;
				if (_Registering)
					throw new InvalidOperationException("It seems like you are recursively accessing a Network which is not yet built.");
				_Registering = true;
				var builder = CreateMainnet();
				if (builder != null)
				{
					builder.SetNetworkType(NetworkType.Mainnet);
					builder.SetNetworkSet(this);
					_Mainnet = builder.BuildAndRegister();
				}
				builder = CreateTestnet();
				if (builder != null)
				{
					builder.SetNetworkType(NetworkType.Testnet);
					builder.SetNetworkSet(this);
					_Testnet = builder.BuildAndRegister();
				}
				builder = CreateRegtest();
				if (builder != null)
				{
					builder.SetNetworkType(NetworkType.Regtest);
					builder.SetNetworkSet(this);
					_Regtest = builder.BuildAndRegister();
				}
				_Registered = true;
				_Registering = false;
				PostInit();
			}
		}

		protected virtual void PostInit()
		{
		}

		protected abstract NetworkBuilder CreateMainnet();
		protected abstract NetworkBuilder CreateTestnet();
		protected abstract NetworkBuilder CreateRegtest();



		private Network _Mainnet;
		public Network Mainnet
		{
			get
			{
				EnsureRegistered();
				return _Mainnet;
			}
		}

		private Network _Testnet;
		public Network Testnet
		{
			get
			{
				EnsureRegistered();
				return _Testnet;
			}
		}

		private Network _Regtest;
		public Network Regtest
		{
			get
			{
				EnsureRegistered();
				return _Regtest;
			}
		}

		public abstract string CryptoCode
		{
			get;
		}

#if !NOFILEIO

		protected class FolderName
		{
			public string TestnetFolder
			{
				get; set;
			} = "testnet3";
			public string RegtestFolder { get; set; } = "regtest";
			public string MainnetFolder { get; set; }
		}

		protected void RegisterDefaultCookiePath(string folderName, FolderName folder = null)
		{
			folder = folder ?? new FolderName();
			var home = Environment.GetEnvironmentVariable("HOME");
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");

			if (string.IsNullOrEmpty(home) && string.IsNullOrEmpty(localAppData))
				return;

			if (!string.IsNullOrEmpty(home) && string.IsNullOrEmpty(localAppData))
			{
				var bitcoinFolder = Path.Combine(home, "." + folderName.ToLowerInvariant());

				if (Mainnet != null)
				{
					var mainnet = folder.MainnetFolder == null ? Path.Combine(bitcoinFolder, ".cookie")
															   : Path.Combine(bitcoinFolder, folder.MainnetFolder, ".cookie");
					;
					RPCClient.RegisterDefaultCookiePath(Mainnet, mainnet);
				}

				if (Testnet != null)
				{
					var testnet = Path.Combine(bitcoinFolder, folder.TestnetFolder, ".cookie");
					RPCClient.RegisterDefaultCookiePath(Testnet, testnet);
				}

				if (Regtest != null)
				{
					var regtest = Path.Combine(bitcoinFolder, folder.RegtestFolder, ".cookie");
					RPCClient.RegisterDefaultCookiePath(Regtest, regtest);
				}
			}
			else if (!string.IsNullOrEmpty(localAppData))
			{
				var bitcoinFolder = Path.Combine(localAppData, char.ToUpperInvariant(folderName[0]) + folderName.Substring(1));
				if (Mainnet != null)
				{
					var mainnet = folder.MainnetFolder == null ? Path.Combine(bitcoinFolder, ".cookie")
															   : Path.Combine(bitcoinFolder, folder.MainnetFolder, ".cookie");
					RPCClient.RegisterDefaultCookiePath(Mainnet, mainnet);
				}

				if (Testnet != null)
				{
					var testnet = Path.Combine(bitcoinFolder, folder.TestnetFolder, ".cookie");
					RPCClient.RegisterDefaultCookiePath(Testnet, testnet);
				}

				if (Regtest != null)
				{
					var regtest = Path.Combine(bitcoinFolder, folder.RegtestFolder, ".cookie");
					RPCClient.RegisterDefaultCookiePath(Regtest, regtest);
				}
			}
		}

#else
		public static void RegisterDefaultCookiePath(Network network, params string[] subfolders) {}
		protected void RegisterDefaultCookiePath(string folderName) {}
#endif
#if !NOSOCKET
		protected static IEnumerable<NetworkAddress> ToSeed(Tuple<byte[], int>[] tuples)
		{
			return tuples
					.Select(t => new NetworkAddress(new IPAddress(t.Item1), t.Item2))
					.ToArray();
		}
#endif
	}
}
