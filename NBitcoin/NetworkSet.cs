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
			switch(networkType)
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
			if(_Registered)
				return;
			lock(l)
			{
				if(_Registered)
					return;
				if(_Registering)
					throw new InvalidOperationException("It seems like you are recursively accessing a Network which is not yet built.");
				_Registering = true;
				var builder = CreateMainnet();
				builder.SetNetworkType(NetworkType.Mainnet);
				builder.SetNetworkSet(this);
				_Mainnet = builder.BuildAndRegister();
				builder = CreateTestnet();
				builder.SetNetworkType(NetworkType.Testnet);
				builder.SetNetworkSet(this);
				_Testnet = builder.BuildAndRegister();
				builder = CreateRegtest();
				builder.SetNetworkType(NetworkType.Regtest);
				builder.SetNetworkSet(this);
				_Regtest = builder.BuildAndRegister();
				PostInit();
				_Registered = true;
				_Registering = false;
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
				if(_Mainnet == null)
					EnsureRegistered();
				return _Mainnet;
			}
		}

		private Network _Testnet;
		public Network Testnet
		{
			get
			{
				if(_Testnet == null)
					EnsureRegistered();
				return _Testnet;
			}
		}

		private Network _Regtest;
		public Network Regtest
		{
			get
			{
				if(_Regtest == null)
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
		}

		protected void RegisterDefaultCookiePath(string folderName, FolderName folder = null)
		{
			folder = folder ?? new FolderName();
			var home = Environment.GetEnvironmentVariable("HOME");
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");

			if(string.IsNullOrEmpty(home) && string.IsNullOrEmpty(localAppData))
				return;

			if(!string.IsNullOrEmpty(home))
			{
				var bitcoinFolder = Path.Combine(home, "." + folderName.ToLowerInvariant());

				var mainnet = Path.Combine(bitcoinFolder, ".cookie");
				RPCClient.RegisterDefaultCookiePath(Mainnet, mainnet);

				var testnet = Path.Combine(bitcoinFolder, folder.TestnetFolder, ".cookie");
				RPCClient.RegisterDefaultCookiePath(Testnet, testnet);

				var regtest = Path.Combine(bitcoinFolder, "regtest", ".cookie");
				RPCClient.RegisterDefaultCookiePath(Regtest, regtest);
			}
			else if(!string.IsNullOrEmpty(localAppData))
			{
				var bitcoinFolder = Path.Combine(localAppData, char.ToUpperInvariant(folderName[0]) + folderName.Substring(1));

				var mainnet = Path.Combine(bitcoinFolder, ".cookie");
				RPCClient.RegisterDefaultCookiePath(Mainnet, mainnet);

				var testnet = Path.Combine(bitcoinFolder, folder.TestnetFolder, ".cookie");
				RPCClient.RegisterDefaultCookiePath(Testnet, testnet);

				var regtest = Path.Combine(bitcoinFolder, "regtest", ".cookie");
				RPCClient.RegisterDefaultCookiePath(Regtest, regtest);
			}
		}

		public static void RegisterDefaultCookiePath(Network network, params string[] subfolders)
		{
			var home = Environment.GetEnvironmentVariable("HOME");
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");
			if(!string.IsNullOrEmpty(home))
			{
				var pathList = new List<string> { home, ".dash" };
				pathList.AddRange(subfolders);

				var fullPath = Path.Combine(pathList.ToArray());
				RPCClient.RegisterDefaultCookiePath(network, fullPath);
			}
			else if(!string.IsNullOrEmpty(localAppData))
			{
				var pathList = new List<string> { localAppData, "Dash" };
				pathList.AddRange(subfolders);

				var fullPath = Path.Combine(pathList.ToArray());
				RPCClient.RegisterDefaultCookiePath(network, fullPath);
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
