using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.Stealth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class DNSSeedData
	{
		string name, host;
		public string Name
		{
			get
			{
				return name;
			}
		}
		public string Host
		{
			get
			{
				return host;
			}
		}
		public DNSSeedData(string name, string host)
		{
			this.name = name;
			this.host = host;
		}
#if !PORTABLE
		IPAddress[] _Addresses = null;
		public IPAddress[] GetAddressNodes()
		{
			if(_Addresses != null)
				return _Addresses;
			return Dns.GetHostAddresses(host);
		}
#endif
		public override string ToString()
		{
			return name + " (" + host + ")";
		}
	}
	public enum Base58Type
	{
		PUBKEY_ADDRESS,
		SCRIPT_ADDRESS,
		SECRET_KEY,
		EXT_PUBLIC_KEY,
		EXT_SECRET_KEY,
		ENCRYPTED_SECRET_KEY_EC,
		ENCRYPTED_SECRET_KEY_NO_EC,
		PASSPHRASE_CODE,
		CONFIRMATION_CODE,
		STEALTH_ADDRESS,
		ASSET_ID,
		COLORED_ADDRESS,
		MAX_BASE58_TYPES,
	};
	public class Network
	{
		byte[][] base58Prefixes = new byte[12][];


		string[] pnSeed = new[] { "206.190.134.44", "91.234.48.232", "141.255.162.215", "141.0.103.214", "85.10.195.44", "83.150.2.99", "185.45.193.44", "193.95.2.149", "107.150.39.42", "74.140.213.145", "5.135.198.129", "68.203.162.177", "50.116.20.36", "93.65.55.164", "176.126.243.24", "5.9.74.107", "192.95.30.227", "83.160.76.197", "213.129.248.139", "99.113.64.43", "217.173.74.91", "63.142.242.169", "108.0.13.191", "192.222.131.59", "82.164.213.27", "77.172.123.53", "178.254.34.161", "209.126.70.159", "175.126.124.92", "168.144.27.112", "108.61.149.222", "192.146.137.1", "83.212.103.212", "76.171.175.153", "24.241.229.236", "62.203.14.61", "84.52.179.9", "178.79.179.49", "205.178.7.169", "61.220.55.224", "94.100.215.18", "23.30.243.153", "85.150.30.142", "78.102.244.7", "157.13.61.3", "97.93.102.87", "64.180.16.213", "60.51.76.100", "5.100.123.19", "46.105.210.3", "83.172.25.92", "70.176.9.49", "71.236.217.58", "91.121.23.52", "37.72.100.197", "89.248.169.6", "50.180.178.216", "173.57.64.40", "62.146.50.200", "64.232.216.213", "81.38.11.202", "82.238.124.41", "104.45.25.155", "173.201.253.231", "109.163.235.239", "46.105.210.151", "124.191.238.150", "98.255.144.176", "75.139.135.103", "104.236.112.213", "94.242.210.167", "72.91.164.51", "162.156.36.6", "104.58.133.176", "108.61.10.90", "46.28.204.51", "50.200.78.107", "98.121.127.116", "67.85.230.189", "37.188.68.169", "63.142.215.160", "96.28.206.67", "198.23.139.230", "71.170.30.34", "86.28.118.33", "5.135.186.15", "1.34.168.128", "70.168.53.148", "209.81.9.223", "83.233.64.214", "50.151.164.128", "68.12.133.49", "176.31.244.215", "60.225.67.170", "198.12.67.124", "54.202.252.197", "173.233.92.236", "46.105.210.33", "71.97.86.189", "119.246.71.52", "83.89.31.249", "188.174.202.14", "71.179.88.144", "151.100.179.44", "80.95.63.129", "146.120.88.92", "178.254.1.170", "93.152.166.29", "141.255.166.194", "85.170.204.241", "178.199.146.178", "84.104.97.25", "84.15.61.60", "85.1.100.87", "92.222.161.110", "195.137.203.27", "89.160.205.13", "107.170.220.159", "162.42.224.42", "192.52.166.141", "70.125.145.149", "94.102.53.241", "24.98.95.201", "75.72.10.173", "223.18.254.55", "83.161.64.45", "50.190.205.137", "75.129.141.35", "64.187.102.153", "162.247.54.200", "108.36.68.179", "104.131.217.133", "204.11.33.220", "83.77.102.36", "72.53.65.16", "85.25.142.92", "81.80.9.71", "91.214.197.113", "108.13.10.109", "135.23.178.163", "66.228.49.201", "70.178.113.51", "54.154.44.169", "89.212.15.202", "203.195.153.17", "81.167.144.219", "75.152.209.239", "192.99.195.17", "71.200.242.89", "64.140.125.98", "72.18.213.82", "187.37.174.171", "78.47.44.174", "85.169.55.254", "128.32.8.248", "123.255.47.170", "37.120.168.204", "42.62.41.194", "92.51.151.226", "176.198.91.237", "184.107.206.45", "74.57.199.180", "73.52.172.91", "67.252.142.125", "191.238.241.166", "59.167.219.142", "106.186.116.245", "96.44.166.190", "84.200.84.210", "188.120.252.184", "95.211.148.154", "91.121.160.59", "94.198.100.111", "178.63.104.217", "83.87.194.238", "75.143.236.5", "76.92.243.220", "199.33.124.186", "66.172.33.144", "85.17.238.8", "104.131.126.235", "108.166.183.31", "154.20.2.139", "24.159.186.144", "70.15.167.116", "176.9.18.10", "24.207.103.56", "75.83.197.114", "178.74.62.35", "77.240.112.3", "174.136.39.51", "192.75.95.104", "75.132.196.4", "107.170.246.25", "77.72.147.193", "79.20.54.61", "72.200.3.19", "137.116.160.176", "93.185.177.71", "71.58.95.78", "68.81.55.227", "70.67.189.16", "81.207.8.49", "67.183.38.189", "173.236.101.34", "178.74.102.24", "86.156.118.90", "98.168.215.241", "71.122.18.135", "174.7.97.199", "174.109.74.177", "89.212.33.237", "70.126.232.93", "95.25.242.177", "98.159.211.48", "62.122.207.218", "70.187.5.85", "167.160.36.73", "82.217.133.145", "82.247.103.117", "68.5.138.9", "50.169.192.116", "89.85.220.84", "24.68.121.113", "69.94.30.177", "50.43.42.71", "108.51.234.171", "178.79.189.150", "72.82.142.141", "116.87.211.135", "73.172.110.138", "82.231.21.209", "104.236.22.62", "213.222.208.93", "95.85.32.31", "162.244.27.106", "46.191.191.173", "87.106.2.186", "173.79.184.121", "5.135.187.85", "198.38.93.227", "37.44.44.11", "54.149.64.82", "192.95.20.208", "108.161.136.113", "37.187.156.122", "182.164.51.111", "198.206.133.68", "72.228.185.182", "46.236.116.209", "188.165.209.148", "188.165.179.154", "185.53.128.227", "199.91.66.218", "37.187.98.144", "98.230.117.192", "138.229.216.238", "81.7.6.110", "172.245.5.196", "50.68.46.172", "63.239.159.40", "123.1.157.189", "210.66.254.236", "108.44.251.88", "173.61.2.43", "103.243.94.140", "37.187.108.172", "71.59.152.182", "119.81.66.229", "71.15.80.197", "94.244.160.84", "128.199.235.80", "108.19.83.13", "71.202.122.234", "213.179.158.253", "77.78.22.68", "128.199.235.174", "210.209.74.8", "31.170.110.127", "91.121.6.19", "173.240.140.205", "77.37.240.142", "75.120.93.101", "24.30.154.41", "24.111.218.27", "24.248.64.242", "14.200.200.145", "144.76.59.187", "72.78.68.86", "104.236.106.208", "137.116.225.142", "84.212.210.135", "194.100.58.197", "213.187.244.23", "70.177.215.244", "174.51.23.224", "82.45.161.18", "82.242.0.245", "66.175.217.124", "97.118.8.236", "82.196.8.44", "67.159.13.34", "202.8.177.50", "108.18.111.25", "103.254.208.58", "107.191.37.252", "91.82.217.115", "80.0.88.41", "202.96.138.245", "93.186.202.8", "72.89.237.106", "195.182.142.71", "128.199.164.96", "95.85.46.114", "108.16.54.90", "128.199.235.175", "24.158.100.226", "206.248.184.127", "178.63.85.22", "50.188.192.133", "75.84.193.254", "24.216.194.173", "24.209.208.33", "93.191.133.245", "62.16.235.100", "178.21.112.84", "24.27.81.126", "64.15.76.238", "74.193.65.79", "161.67.132.40", "95.226.6.3", "94.27.9.30", "86.72.214.67", "31.7.57.235", "216.157.21.35", "88.198.241.196", "109.80.207.237", "168.158.129.29", "135.23.200.5", "82.26.245.81", "76.28.154.232", "107.150.8.27", "217.75.88.178", "47.55.14.65", "88.198.66.149", "5.228.1.230", "129.186.17.17", "178.62.179.246", "75.109.156.8", "23.91.142.147", "104.200.17.214", "87.121.52.207", "95.241.163.2", "54.86.148.8", "198.50.157.119", "176.223.201.198", "104.236.106.209", "108.40.10.201", "89.66.77.45", "192.227.139.229", "104.156.251.241", "66.172.27.218", "67.212.80.15", "58.165.177.149", "69.247.195.200", "46.59.39.101", "69.172.231.7", "176.28.45.22", "72.49.75.223", "172.245.5.156", "50.253.137.98", "66.222.19.194", "66.228.56.49", "84.114.82.82", "67.169.255.17", "174.108.112.235", "92.27.7.209", "71.177.61.126", "68.103.198.100", "109.228.152.2", "50.53.4.148", "61.35.225.19", "71.42.73.110", "202.159.161.72", "24.181.254.159", "206.188.231.210", "212.25.37.124", "76.126.137.197", "81.7.10.197", "46.151.84.88", "84.29.231.211", "198.245.51.112", "68.225.221.20", "73.181.204.170", "178.62.26.83", "37.157.250.36", "50.140.21.62", "108.170.140.21", "192.99.183.131", "153.127.251.67", "142.179.136.20", "5.196.116.21", "118.200.223.207", "78.73.56.35", "195.6.17.142", "70.173.255.54", "182.92.180.216", "71.72.133.97", "110.174.204.118", "188.120.194.140", "173.174.215.76", "77.58.2.117", "69.196.158.199", "80.163.38.28", "83.94.225.66", "175.126.82.244", "94.199.186.42", "80.84.55.2", "113.105.233.126", "188.166.62.197", "80.220.99.227", "1.34.180.245", "61.72.211.228", "107.214.165.35", "80.229.28.60", "73.168.101.225", "174.55.112.243", "69.164.193.73", "217.147.94.30", "24.147.80.212", "97.86.205.137", "83.252.63.212", "46.105.102.78", "89.22.96.204", "96.47.143.227", "173.220.42.2", "46.163.76.230", "89.84.5.8", "59.126.51.247", "188.138.125.48", "73.9.117.156", "176.28.51.121", "69.12.226.165", "174.96.164.211", "71.58.168.175", "198.50.214.62", "70.56.40.70", "81.226.140.100", "174.97.137.157", "120.27.35.77", "75.137.208.136", "94.23.10.183", "82.227.4.240", "176.9.45.123", "208.66.208.146", "178.79.178.147", "104.131.186.220", "195.113.34.16", "195.197.175.190", "72.74.150.204", "95.154.200.216", "82.4.55.245", "96.37.69.179", "209.105.243.229", "217.83.213.206", "73.43.71.152", "184.175.20.4", "203.219.14.204", "50.115.43.253", "71.165.11.202", "76.184.136.2", "83.212.103.14", "5.135.188.133", "194.213.123.22", "82.220.2.59", "173.255.228.94", "91.207.5.14", "99.238.235.119", "188.191.97.208", "185.21.216.156", "162.221.191.37", "5.35.251.225", "173.70.136.191", "95.111.107.136", "67.162.238.30", "87.117.231.104", "101.251.203.6", "69.125.179.241", "24.41.10.204", "222.112.145.179", "24.19.85.3", "173.230.144.211", "23.253.241.22", "195.154.255.182", "84.95.240.99", "71.12.5.70", "68.102.174.34", "205.250.190.49", "70.162.87.92", "98.166.131.15", "212.114.48.31", "84.24.19.223", "172.10.25.207", "23.99.105.9", "73.31.30.56", "68.45.234.253", "46.42.38.112", "72.225.47.72", "185.52.26.34", "50.78.49.181", "68.5.29.117", "80.39.113.100", "87.236.196.77", "69.136.175.241", "81.99.52.29", "136.243.0.218", "182.92.222.233", "92.7.149.75", "64.130.111.51", "198.52.200.56", "68.231.107.108", "98.172.71.156", "89.106.109.238", "98.192.87.223", "162.239.254.100", "71.11.7.246", "94.42.115.50", "178.78.236.136", "71.220.171.182", "206.123.112.180", "195.56.63.10", "173.64.215.173", "14.203.184.94", "76.174.20.247", "79.205.249.62", "65.30.47.116", "98.226.66.65", "108.199.77.32", "97.107.141.177", "50.252.52.49", "24.34.137.205", "23.239.27.92", "72.90.78.78", "198.52.212.235", "162.253.32.143", "50.168.230.216", "66.175.214.173", "151.228.196.42", "198.52.212.234", "84.200.67.181", "176.9.38.40", "72.44.79.44", "86.146.235.54", "100.38.11.146", "58.96.105.85", "1.234.82.66", "50.183.23.51", "70.168.116.190", "50.253.111.26", "80.212.49.199", "192.3.89.159", "188.126.8.14", "85.219.144.231", "24.136.22.179", "193.12.238.204", "54.193.80.253", "73.183.10.222", "188.226.146.44", "24.24.190.90", "5.198.93.103", "107.170.51.67", "98.237.42.22", "178.62.1.28", "46.244.10.86", "122.106.120.166", "147.32.204.59", "124.198.206.100", "162.244.28.113", "173.232.106.81", "23.255.227.231", "85.128.85.217", "23.236.144.69", "63.141.228.138", "5.77.42.130", "185.45.192.129", "174.0.25.200", "46.244.18.35", "85.139.76.185", "50.158.150.75", "94.19.12.244", "112.124.96.217", "87.81.141.250", "66.175.210.120", "81.174.247.50", "97.107.132.78", "188.138.94.6", "69.164.219.74", "69.112.51.199", "68.193.15.115", "80.81.242.110" };


		uint magic;
		byte[] vAlertPubKey;
		PubKey _AlertPubKey;
		public PubKey AlertPubKey
		{
			get
			{
				if(_AlertPubKey == null)
				{
					_AlertPubKey = new PubKey(vAlertPubKey);
				}
				return _AlertPubKey;
			}
		}

#if !PORTABLE
		List<DNSSeedData> vSeeds = new List<DNSSeedData>();
		List<NetworkAddress> vFixedSeeds = new List<NetworkAddress>();
#endif
		Block genesis = new Block();

		private int nRPCPort;
		public int RPCPort
		{
			get
			{
				return nRPCPort;
			}
		}

		private uint256 hashGenesisBlock;

		private int nDefaultPort;
		public int DefaultPort
		{
			get
			{
				return nDefaultPort;
			}
		}



		static Network _RegTest;
		public static Network RegTest
		{
			get
			{
				if(_RegTest == null)
				{
					var instance = new Network();
					instance.InitReg();
					_RegTest = instance;
				}
				return _RegTest;
			}
		}

		private void InitReg()
		{
			InitTest();
			magic = 0xDAB5BFFA;
			name = "RegTest";
			nSubsidyHalvingInterval = 150;
			_ProofOfLimit = new Target(~new uint256(0) >> 1);
			genesis.Header.BlockTime = Utils.UnixTimeToDateTime(1296688602);
			genesis.Header.Bits = 0x207fffff;
			genesis.Header.Nonce = 2;
			hashGenesisBlock = genesis.GetHash();
			nDefaultPort = 18444;
			//strDataDir = "regtest";
			assert(hashGenesisBlock == new uint256("0x0f9188f13cb7b2c71f2a335e3a4fc328bf5beb436012afca590b1a11466e2206"));

#if !PORTABLE
			vSeeds.Clear();  // Regtest mode doesn't have any DNS seeds.
#endif
		}


		static Network _Main;
		private Target _ProofOfLimit;
		private int nSubsidyHalvingInterval;
		private string name;

		public string Name
		{
			get
			{
				return name;
			}
		}

		public static Network Main
		{
			get
			{
				if(_Main == null)
				{
					var instance = new Network();
					instance.InitMain();
					_Main = instance;
				}
				return _Main;
			}
		}

		private void InitMain()
		{
			SpendableCoinbaseDepth = 100;
			name = "Main";
			// The message start string is designed to be unlikely to occur in normal data.
			// The characters are rarely used upper ASCII, not valid as UTF-8, and produce
			// a large 4-byte int at any alignment.
			magic = 0xD9B4BEF9;
			vAlertPubKey = DataEncoders.Encoders.Hex.DecodeData("04fc9702847840aaf195de8442ebecedf5b095cdbb9bc716bda9110971b28a49e0ead8564ff0db22209e0374782c093bb899692d524e9d6a6956e7c5ecbcd68284");
			nDefaultPort = 8333;
			nRPCPort = 8332;
			_ProofOfLimit = new Target(~new uint256(0) >> 32);
			nSubsidyHalvingInterval = 210000;

			Transaction txNew = new Transaction();
			txNew.Version = 1;
			txNew.Inputs.Add(new TxIn());
			txNew.Outputs.Add(new TxOut());
			txNew.Inputs[0].ScriptSig = new Script(DataEncoders.Encoders.Hex.DecodeData("04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b73"));
			txNew.Outputs[0].Value = 50 * Money.COIN;
			txNew.Outputs[0].ScriptPubKey = new Script() + DataEncoders.Encoders.Hex.DecodeData("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5f") + OpcodeType.OP_CHECKSIG;
			genesis.Transactions.Add(txNew);
			genesis.Header.HashPrevBlock = 0;
			genesis.UpdateMerkleRoot();
			genesis.Header.Version = 1;
			genesis.Header.BlockTime = Utils.UnixTimeToDateTime(1231006505);
			genesis.Header.Bits = 0x1d00ffff;
			genesis.Header.Nonce = 2083236893;

			hashGenesisBlock = genesis.GetHash();
			assert(hashGenesisBlock == new uint256("0x000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"));
			assert(genesis.Header.HashMerkleRoot == new uint256("0x4a5e1e4baab89f3a32518a88c31bc87f618f76673e2cc77ab2127b7afdeda33b"));
#if !PORTABLE
			vSeeds.Add(new DNSSeedData("bitcoin.sipa.be", "seed.bitcoin.sipa.be"));
			vSeeds.Add(new DNSSeedData("bluematt.me", "dnsseed.bluematt.me"));
			vSeeds.Add(new DNSSeedData("dashjr.org", "dnsseed.bitcoin.dashjr.org"));
			vSeeds.Add(new DNSSeedData("bitcoinstats.com", "seed.bitcoinstats.com"));
			vSeeds.Add(new DNSSeedData("bitnodes.io", "seed.bitnodes.io"));
			vSeeds.Add(new DNSSeedData("xf2.org", "bitseed.xf2.org"));
#endif
			base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (0) };
			base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (5) };
			base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (128) };
			base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
			base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
			base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
			base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
			base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
			base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
			base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
			base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
			base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };

			// Convert the pnSeeds array into usable address objects.
			Random rand = new Random();
			TimeSpan nOneWeek = TimeSpan.FromDays(7);
#if !PORTABLE
			for(int i = 0 ; i < pnSeed.Length ; i++)
			{
				// It'll only connect to one or two seed nodes because once it connects,
				// it'll get a pile of addresses with newer timestamps.
				IPAddress ip = IPAddress.Parse(pnSeed[i]);
				NetworkAddress addr = new NetworkAddress();
				// Seed nodes are given a random 'last seen time' of between one and two
				// weeks ago.
				addr.Time = DateTime.UtcNow - (TimeSpan.FromSeconds(rand.NextDouble() * nOneWeek.TotalSeconds)) - nOneWeek;
				addr.Endpoint = new IPEndPoint(ip, DefaultPort);
				vFixedSeeds.Add(addr);
			}
#endif
		}


		static Network _TestNet;
		public static Network TestNet
		{
			get
			{
				if(_TestNet == null)
				{
					var instance = new Network();
					instance.InitTest();
					_TestNet = instance;
				}
				return _TestNet;
			}
		}
		private void InitTest()
		{
			InitMain();
			name = "TestNet";
			magic = 0x0709110B;

			vAlertPubKey = DataEncoders.Encoders.Hex.DecodeData("04302390343f91cc401d56d68b123028bf52e5fca1939df127f63c6467cdf9c8e2c14b61104cf817d0b780da337893ecc4aaff1309e536162dabbdb45200ca2b0a");
			nDefaultPort = 18333;
			nRPCPort = 18332;
			//strDataDir = "testnet3";

			// Modify the testnet genesis block so the timestamp is valid for a later start.
			genesis.Header.BlockTime = Utils.UnixTimeToDateTime(1296688602);
			genesis.Header.Nonce = 414098458;
			hashGenesisBlock = genesis.GetHash();
			assert(hashGenesisBlock == new uint256("0x000000000933ea01ad0ee984209779baaec3ced90fa3f408719526f8d77f4943"));

#if !PORTABLE
			vFixedSeeds.Clear();
			vSeeds.Clear();
			vSeeds.Add(new DNSSeedData("bitcoin.petertodd.org", "testnet-seed.bitcoin.petertodd.org"));
			vSeeds.Add(new DNSSeedData("bluematt.me", "testnet-seed.bluematt.me"));
#endif
			base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (111) };
			base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
			base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (239) };
			base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
			base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
			base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2b };
			base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 115 };
		}

		private static void assert(bool v)
		{
			if(!v)
				throw new InvalidOperationException("Invalid network");
		}

		public BitcoinSecret CreateBitcoinSecret(string base58)
		{
			return new BitcoinSecret(base58, this);
		}

		/// <summary>
		/// Create a bitcoin address from base58 data, return a BitcoinAddress or BitcoinScriptAddress
		/// </summary>
		/// <param name="base58">base58 address</param>
		/// <exception cref="System.FormatException">Invalid base58 address</exception>
		/// <returns>BitcoinScriptAddress, BitcoinAddress</returns>
		public BitcoinAddress CreateBitcoinAddress(string base58)
		{
			var type = GetBase58Type(base58);
			if(!type.HasValue)
				throw new FormatException("Invalid Base58 version");
			if(type == Base58Type.PUBKEY_ADDRESS)
				return new BitcoinAddress(base58, this);
			if(type == Base58Type.SCRIPT_ADDRESS)
				return new BitcoinScriptAddress(base58, this);
			throw new FormatException("Invalid Base58 version");
		}

		public BitcoinScriptAddress CreateBitcoinScriptAddress(string base58)
		{
			return new BitcoinScriptAddress(base58, this);
		}

		private Base58Type? GetBase58Type(string base58)
		{
			var bytes = Encoders.Base58Check.DecodeData(base58);
			for(int i = 0 ; i < base58Prefixes.Length ; i++)
			{
				var prefix = base58Prefixes[i];
				if(bytes.Length < prefix.Length)
					continue;
				if(Utils.ArrayEqual(bytes, 0, prefix, 0, prefix.Length))
					return (Base58Type)i;
			}
			return null;
		}


		public static Network GetNetworkFromBase58Data(string base58)
		{
			foreach(var network in GetNetworks())
			{
				var type = network.GetBase58Type(base58);
				if(type.HasValue)
				{
					if(type.Value == Base58Type.COLORED_ADDRESS)
					{
						var raw = Encoders.Base58Check.DecodeData(base58);
						var version = network.GetVersionBytes(type.Value);
						raw = raw.Skip(version.Length).ToArray();
						base58 = Encoders.Base58Check.EncodeData(raw);
						return GetNetworkFromBase58Data(base58);
					}
					return network;
				}
			}
			return null;
		}

		/// <summary>
		/// Find automatically the data type and the network to which belong the base58 data
		/// </summary>
		/// <param name="base58">base58 data</param>
		/// <exception cref="System.FormatException">Invalid base58 data</exception>
		public static Base58Data CreateFromBase58Data(string base58, Network expectedNetwork = null)
		{
			bool invalidNetwork = false;
			foreach(var network in GetNetworks())
			{
				var type = network.GetBase58Type(base58);
				if(type.HasValue)
				{
					if(type.Value == Base58Type.COLORED_ADDRESS)
					{
						var inner = BitcoinAddress.Create(BitcoinColoredAddress.GetWrappedBase58(base58, network));
						if(inner.Network != network)
							continue;
					}
					if(expectedNetwork != null && network != expectedNetwork)
					{
						invalidNetwork = true;
						continue;
					}
					return network.CreateBase58Data(type.Value, base58);
				}
			}
			if(invalidNetwork)
				throw new FormatException("Invalid network");
			throw new FormatException("Invalid base58 data");
		}

		public static T CreateFromBase58Data<T>(string base58, Network expectedNetwork = null) where T : Base58Data
		{
			var result = CreateFromBase58Data(base58, expectedNetwork) as T;
			if(result == null)
				throw new FormatException("Invalid base58 data");
			return result;
		}

		public T Parse<T>(string base58) where T : Base58Data
		{
			var type = GetBase58Type(base58);
			if(type.HasValue)
			{
				var result = CreateBase58Data(type.Value, base58) as T;
				if(result == null)
					throw new FormatException("Invalid base58 data");
				return result;
			}
			throw new FormatException("Invalid base58 data");
		}

		public Base58Data CreateBase58Data(Base58Type type, string base58)
		{
			if(type == Base58Type.EXT_PUBLIC_KEY)
				return CreateBitcoinExtPubKey(base58);
			if(type == Base58Type.EXT_SECRET_KEY)
				return CreateBitcoinExtKey(base58);
			if(type == Base58Type.PUBKEY_ADDRESS)
				return CreateBitcoinAddress(base58);
			if(type == Base58Type.SCRIPT_ADDRESS)
				return CreateBitcoinScriptAddress(base58);
			if(type == Base58Type.SECRET_KEY)
				return CreateBitcoinSecret(base58);
			if(type == Base58Type.CONFIRMATION_CODE)
				return CreateConfirmationCode(base58);
			if(type == Base58Type.ENCRYPTED_SECRET_KEY_EC)
				return CreateEncryptedKeyEC(base58);
			if(type == Base58Type.ENCRYPTED_SECRET_KEY_NO_EC)
				return CreateEncryptedKeyNoEC(base58);
			if(type == Base58Type.PASSPHRASE_CODE)
				return CreatePassphraseCode(base58);
			if(type == Base58Type.STEALTH_ADDRESS)
				return CreateStealthAddress(base58);
			if(type == Base58Type.ASSET_ID)
				return CreateAssetId(base58);
			if(type == Base58Type.COLORED_ADDRESS)
				return CreateColoredAddress(base58);
			throw new NotSupportedException("Invalid Base58Data type : " + type.ToString());
		}

		private BitcoinColoredAddress CreateColoredAddress(string base58)
		{
			return new BitcoinColoredAddress(base58, this);
		}

		public NBitcoin.OpenAsset.BitcoinAssetId CreateAssetId(string base58)
		{
			return new NBitcoin.OpenAsset.BitcoinAssetId(base58, this);
		}

		public BitcoinStealthAddress CreateStealthAddress(string base58)
		{
			return new BitcoinStealthAddress(base58, this);
		}

		private BitcoinPassphraseCode CreatePassphraseCode(string base58)
		{
			return new BitcoinPassphraseCode(base58, this);
		}

		private BitcoinEncryptedSecretNoEC CreateEncryptedKeyNoEC(string base58)
		{
			return new BitcoinEncryptedSecretNoEC(base58, this);
		}

		private BitcoinEncryptedSecretEC CreateEncryptedKeyEC(string base58)
		{
			return new BitcoinEncryptedSecretEC(base58, this);
		}

		private Base58Data CreateConfirmationCode(string base58)
		{
			return new BitcoinConfirmationCode(base58, this);
		}

		private Base58Data CreateBitcoinExtPubKey(string base58)
		{
			return new BitcoinExtPubKey(base58, this);
		}


		public BitcoinExtKey CreateBitcoinExtKey(ExtKey key)
		{
			return new BitcoinExtKey(key, this);
		}

		public BitcoinExtPubKey CreateBitcoinExtPubKey(ExtPubKey pubkey)
		{
			return new BitcoinExtPubKey(pubkey, this);
		}

		public BitcoinExtKey CreateBitcoinExtKey(string base58)
		{
			return new BitcoinExtKey(base58, this);
		}

		public byte[] GetVersionBytes(Base58Type type)
		{
			return base58Prefixes[(int)type].ToArray();
		}

		public ValidationState CreateValidationState()
		{
			return new ValidationState(this);
		}

		public override string ToString()
		{
			return name;
		}

		public Target ProofOfWorkLimit
		{
			get
			{
				return _ProofOfLimit;
			}
		}

		public Block GetGenesis()
		{
			var block = new Block();
			block.ReadWrite(genesis.ToBytes());
			return block;
		}

		public static IEnumerable<Network> GetNetworks()
		{
			yield return Main;
			yield return TestNet;
			yield return RegTest;
		}

		public static Network GetNetwork(uint magic)
		{
			return GetNetworks().FirstOrDefault(r => r.Magic == magic);
		}

		public static Network GetNetwork(string name)
		{
			name = name.ToLowerInvariant();
			switch(name)
			{
				case "main":
					return Network.Main;
				case "testnet":
				case "testnet3":
					return Network.TestNet;
				case "reg":
				case "regtest":
					return Network.RegTest;
				default:
					throw new ArgumentException(String.Format("Invalid network name '{0}'", name));
			}
		}

		public BitcoinSecret CreateBitcoinSecret(Key key)
		{
			return new BitcoinSecret(key, this);
		}

		public BitcoinAddress CreateBitcoinAddress(TxDestination dest)
		{
			if(dest == null)
				throw new ArgumentNullException("dest");
			if(dest is ScriptId)
				return CreateBitcoinScriptAddress((ScriptId)dest);
			if(dest is KeyId)
				return new BitcoinAddress((KeyId)dest, this);
			throw new ArgumentException("Invalid dest type", "dest");
		}

		private BitcoinAddress CreateBitcoinScriptAddress(ScriptId scriptId)
		{
			return new BitcoinScriptAddress(scriptId, this);
		}
#if !PORTABLE
		public Message ParseMessage(byte[] bytes, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			BitcoinStream bstream = new BitcoinStream(bytes);
			Message message = new Message();
			using(bstream.ProtocolVersionScope(version))
			{
				bstream.ReadWrite(ref message);
			}
			if(message.Magic != magic)
				throw new FormatException("Unexpected magic field in the message");
			return message;
		}

		public IEnumerable<NetworkAddress> SeedNodes
		{
			get
			{
				return this.vFixedSeeds;
			}
		}
		public IEnumerable<DNSSeedData> DNSSeeds
		{
			get
			{
				return this.vSeeds;
			}
		}
#endif
		public byte[] _MagicBytes;
		public byte[] MagicBytes
		{
			get
			{
				if(_MagicBytes == null)
				{
					var bytes = new byte[] 
					{ 
						(byte)Magic,
						(byte)(Magic >> 8),
						(byte)(Magic >> 16),
						(byte)(Magic >> 24)
					};
					_MagicBytes = bytes;
				}
				return _MagicBytes;
			}
		}
		public uint Magic
		{
			get
			{
				return magic;
			}
		}

		public Money GetReward(int nHeight)
		{
			long nSubsidy = new Money(50 * Money.COIN);
			int halvings = nHeight / nSubsidyHalvingInterval;

			// Force block reward to zero when right shift is undefined.
			if(halvings >= 64)
				return Money.Zero;

			// Subsidy is cut in half every 210,000 blocks which will occur approximately every 4 years.
			nSubsidy >>= halvings;

			return new Money(nSubsidy);
		}

		public bool ReadMagic(Stream stream, CancellationToken cancellation)
		{
			byte[] bytes = new byte[1];
			for(int i = 0 ; i < MagicBytes.Length ; i++)
			{
				i = Math.Max(0, i);
				cancellation.ThrowIfCancellationRequested();

				var read = stream.ReadEx(bytes, 0, bytes.Length, cancellation);
				if(read == -1)
					return false;
				if(read != 1)
					i--;
				else if(_MagicBytes[i] != bytes[0])
					i = -1;
			}
			return true;
		}

		public int SpendableCoinbaseDepth
		{
			get;
			private set;
		}
	}
}
