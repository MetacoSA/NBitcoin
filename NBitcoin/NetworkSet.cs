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
		Network GetNetwork(ChainName chainName);
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
		public virtual Network GetNetwork(ChainName chainName)
		{
			if (chainName == null)
				throw new ArgumentNullException(nameof(chainName));
			if (chainName == ChainName.Mainnet)
				return Mainnet;
			if (chainName == ChainName.Testnet)
				return Testnet;
			if (chainName == ChainName.Regtest)
				return Regtest;
			return null;
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
					builder.SetChainName(ChainName.Mainnet);
					builder.SetNetworkSet(this);
					_Mainnet = builder.BuildAndRegister();
				}
				builder = CreateTestnet();
				if (builder != null)
				{
					builder.SetChainName(ChainName.Testnet);
					builder.SetNetworkSet(this);
					_Testnet = builder.BuildAndRegister();
				}
				builder = CreateRegtest();
				if (builder != null)
				{
					builder.SetChainName(ChainName.Regtest);
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
			var bitcoinFolder = Network.GetDefaultDataFolder(folderName);
			if (bitcoinFolder is null)
				return;

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
