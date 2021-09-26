using System;
using System.Linq;
using System.Reflection;

namespace NBitcoin.Tests
{
	public partial class NodeDownloadData
	{
		public bool SupportCookieFile { get; set; } = true;

		public class NodeDownloadDataBase
		{
			/// <summary>
			/// Get the specific version from string
			/// </summary>
			/// <param name="version">The version number, null or 'latest' if fetching the latest version.</param>
			/// <returns></returns>
			public NodeDownloadData FromVersion(string version)
			{
				var nodes = this.GetType().GetTypeInfo().DeclaredFields
					.Where(v => v.FieldType == typeof(NodeDownloadData))
					.Select(v => (NodeDownloadData)v.GetValue(this));
				if (version is "latest" || version is null)
					return nodes.OrderBy(v => v.Version).Last();
				return nodes.First(n => VersionMach(n.Version, version));
			}

			private bool VersionMach(string v1, string v2)
			{
				v1 = v1.Replace("v", "").Replace("_", ".");
				v2 = v2.Replace("v", "").Replace("_", ".");
				return v1.Equals(v2);
			}
		}
		public class BitcoinNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_13_1 = new NodeDownloadData()
			{
				Version = "0.13.1",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "2293de5682375b8edfde612d9e152b42344d25d3852663ba36f7f472b27954a4"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "499be4f48c933d92c43468ee2853dddaba4af7e1a17f767a85023b69a21b6e77"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win32.zip",
					Archive = "bitcoin-{0}-win32.zip",
					Hash = "fcf6089fc013b175e3c5e32580afb3cb4310c62d2e133e992b8a9d2e0cbbafaa"
				}
			};

			public NodeDownloadData v0_16_3 = new NodeDownloadData()
			{
				Version = "0.16.3",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "5d422a9d544742bc0df12427383f9c2517433ce7b58cf672b9a9b17c2ef51e4f"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "78c3bff3b619a19aed575961ea43cc9e142959218835cf51aede7f0b764fc25d"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "52469c56222c1b5344065ef2d3ce6fc58ae42939a7b80643a7e3ee75ec237da9"
				}
			};

			public NodeDownloadData v0_17_0 = new NodeDownloadData()
			{
				Version = "0.17.0",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "9d6b472dc2aceedb1a974b93a3003a81b7e0265963bd2aa0acdcb17598215a4f"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "e4210edfff313e4e00169e9170369537bb45024c318f5b339623d5fd08715d61"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "d6312ef594fa701d6bc863415baeccd3a140f200259fcfac56dde81a73d50799"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v0_18_0 = new NodeDownloadData()
			{
				Version = "0.18.0",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "5146ac5310133fbb01439666131588006543ab5364435b748ddfc95a8cb8d63f"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "12ae4dfc7566d08116509a592bb3ab5036b50405cba75f7d52105cee98ba47b0"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "29f449e2d1986a924b512e043893f932170830a45981323d8943ba6410848153"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v0_18_1 = new NodeDownloadData()
			{
				Version = "0.18.1",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "600d1db5e751fa85903e935a01a74f5cc57e1e7473c15fd3e17ed21e202cfe5a"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "b7bbcee7a7540f711b171d6981f939ca8482005fde22689bc016596d80548bb1"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "b0f94ab43c068bac9c10a59cb3f1b595817256a00b84f0b724f8504b44e1314f"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v0_19_0_1 = new NodeDownloadData()
			{
				Version = "0.19.0.1",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "732cc96ae2e5e25603edf76b8c8af976fe518dd925f7e674710c6c8ee5189204"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "a64e4174e400f3a389abd76f4d6b1853788730013ab1dedc0e64b0a0025a0923"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "7706593de727d893e4b1e750dc296ea682ccee79acdd08bbc81eaacf3b3173cf"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v0_20_0 = new NodeDownloadData()
			{
				Version = "0.20.0",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "35ec10f87b6bc1e44fd9cd1157e5dfa483eaf14d7d9a9c274774539e7824c427"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "34f377fee2c7adf59981dde7e41215765d47b466f773cf2673137d30495b2675"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "3e9ddfa05b7967e43fb502b735b6c4d716ec06f63ab7183df2e006ed4a6a431f"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v0_20_1 = new NodeDownloadData()
			{
				Version = "0.20.1",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "376194f06596ecfa40331167c39bc70c355f960280bd2a645fdbf18f66527397"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "b9024dde373ea7dad707363e07ec7e265383204127539ae0c234bff3a61da0d1"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "e59fba67afce011d32b5d723a3a0be12da1b8a34f5d7966e504520c48d64716d"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v0_21_0 = new NodeDownloadData()
			{
				Version = "0.21.0",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "da7766775e3f9c98d7a9145429f2be8297c2672fe5b118fd3dc2411fb48e0032"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "695fb624fa6423f5da4f443b60763dd1d77488bfe5ef63760904a7b54e91298d"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "1d0052c4ce80227fb6d0bc1c4e673ba21033e219c1f935d25f130ef7f43360d4"
				},
				UseSectionInConfigFile = true,
				CreateWallet = true
			};

			public NodeDownloadData v0_21_1 = new NodeDownloadData()
			{
				Version = "0.21.1",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "366eb44a7a0aa5bd342deea215ec19a184a11f2ca22220304ebb20b9c8917e2b"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "1ea5cedb64318e9868a66d3ab65de14516f9ada53143e460d50af428b5aec3c7"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "94c80f90184cdc7e7e75988a55b38384de262336abd80b1b30121c6e965dc74e"
				},
				UseSectionInConfigFile = true,
				CreateWallet = true
			};

			public NodeDownloadData v22_0 = new NodeDownloadData()
			{
				Version = "22.0",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "59ebd25dd82a51638b7a6bb914586201e67db67b919b2a1ff08925a7936d1b16"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "2744d199c3343b2d94faffdfb2c94d75a630ba27301a70e47b0ad30a7e0155e9"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoincore.org/bin/bitcoin-core-{0}/bitcoin-{0}-win64.zip",
					Archive = "bitcoin-{0}-win64.zip",
					Hash = "9485e4b52ed6cebfe474ab4d7d0c1be6d0bb879ba7246a8239326b2230a77eb1"
				},
				UseSectionInConfigFile = true,
				CreateWallet = true
			};
		}

		public class LitecoinNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_14_2 = new NodeDownloadData()
			{
				Version = "0.14.2",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/win/litecoin-{0}-win64.zip",
					Archive = "litecoin-{0}-win64.zip",
					Executable = "litecoin-{0}/bin/litecoind.exe",
					Hash = "c47b196a45f64dbfc9d13b66b50d4da82a263d95b36577e64b31c37590f718b2"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/linux/litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "05f409ee57ce83124f2463a3277dc8d46fca18637052d1021130e4deaca07b3c"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/osx/litecoin-{0}-osx64.tar.gz",
					Archive = "litecoin-{0}-osx64.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind"
				}
			};

			public NodeDownloadData v0_15_1 = new NodeDownloadData()
			{
				Version = "0.15.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/win/litecoin-{0}-win64.zip",
					Archive = "litecoin-{0}-win64.zip",
					Executable = "litecoin-{0}/bin/litecoind.exe",
					Hash = "eae66242ef66ee22f403ade0c2795ff74f6654bf3fc546e99bde2e6e4c9e148f"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/linux/litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "77062f7bad781dd6667854b3c094dbf51094b33405c6cd25c36d07e0dd5e92e5"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/osx/litecoin-{0}-osx64.tar.gz",
					Archive = "litecoin-{0}-osx64.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "2bb565a77779be4ed5b186c93891bc0a12352c94316a1fc44388898f7afb7bc2"
				}
			};

			public NodeDownloadData v0_16_3 = new NodeDownloadData()
			{
				Version = "0.16.3",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/win/litecoin-{0}-win64.zip",
					Archive = "litecoin-{0}-win64.zip",
					Executable = "litecoin-{0}/bin/litecoind.exe",
					Hash = "1958608b52056d0489451cdba4f631b3010419ea85edc9271a9efe4341870b4d"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/linux/litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "686d99d1746528648c2c54a1363d046436fd172beadaceea80bdc93043805994"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/osx/litecoin-{0}-osx64.tar.gz",
					Archive = "litecoin-{0}-osx64.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "fe1a593ffb10fec817157903a49d8965c49594dda4021fb76cf7d341e5300e17"
				}
			};

			public NodeDownloadData v0_17_1 = new NodeDownloadData()
			{
				Version = "0.17.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/win/litecoin-{0}-win64.zip",
					Archive = "litecoin-{0}-win64.zip",
					Executable = "litecoin-{0}/bin/litecoind.exe",
					Hash = "8060e9bface9bbdc22c74a2687b211c8b4e32fe03c0e6c537c12de0ff6f0813b"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/linux/litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "9cab11ba75ea4fb64474d4fea5c5b6851f9a25fe9b1d4f7fc9c12b9f190fed07"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/osx/litecoin-{0}-osx64.tar.gz",
					Archive = "litecoin-{0}-osx64.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "b93fa415c84bea1676d0b0ea819dd6e8e4f7b136167d89b18b63240b50757d4f"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v0_18_1 = new NodeDownloadData()
			{
				Version = "0.18.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/win/litecoin-{0}-win64.zip",
					Archive = "litecoin-{0}-win64.zip",
					Executable = "litecoin-{0}/bin/litecoind.exe",
					Hash = "9ff1f552f8ed3f058a56332defa7c61cfb345848428d7b419182096eca2ac99a"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/linux/litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "litecoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "ca50936299e2c5a66b954c266dcaaeef9e91b2f5307069b9894048acf3eb5751"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.litecoin.org/litecoin-{0}/osx/litecoin-{0}-osx64.tar.gz",
					Archive = "litecoin-{0}-osx64.tar.gz",
					Executable = "litecoin-{0}/bin/litecoind",
					Hash = "4ce590ecbaecaced7253473bc574a2b70527c9aeb3a3ab33a843ea1c9caf0c86"
				},
				UseSectionInConfigFile = true
			};
		}

		public class ViacoinNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_15_2 = new NodeDownloadData()
			{
				Version = "0.15.2",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/viacoin/viacoin/releases/download/v{0}/viacoin-{0}-win64.zip",
					Archive = "viacoin-{0}-win64.zip",
					Executable = "viacoin-{0}/bin/viacoind.exe",
					Hash = "79e1d052890dae7531b782046ee4af4851778099121442b219d0605bee486789"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/viacoin/viacoin/releases/download/v{0}/viacoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "viacoin-{0}-linux64.tar.gz",
					Executable = "viacoin-{0}/bin/viacoind",
					Hash = "bdbd432645a8b4baadddb7169ea4bef3d03f80dc2ce53dce5783d8582ac63bab"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/viacoin/viacoin/releases/download/v{0}/viacoin-{0}-osx64.tar.gz",
					Archive = "viacoin-{0}-osx64.tar.gz",
					Executable = "viacoin-{0}/bin/viacoind",
					Hash = "b2b0ac9cfb354a017df4271a312f604a67d9e7bc4450f796a20cebd15425c052"
				}
			};
		}

		public class BCashNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_16_2 = new NodeDownloadData()
			{
				Version = "0.16.2",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.bitcoinabc.org/{0}/win/bitcoin-abc-{0}-win64.zip",
					Archive = "bitcoin-abc-{0}-win64.zip",
					Executable = "bitcoin-abc-{0}/bin/bitcoind.exe",
					Hash = "af022ccdb7d55fdffd1ddddabc2bcde9d72614a4c8412a74456954bacac0e729"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.bitcoinabc.org/{0}/linux/bitcoin-abc-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "bitcoin-abc-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-abc-{0}/bin/bitcoind",
					Hash = "5eeadea9c23069e08d18e0743f4a86a9774db7574197440c6d795fad5cad2084"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.bitcoinabc.org/{0}/osx/bitcoin-abc-{0}-osx64.tar.gz",
					Archive = "bitcoin-abc-{0}-osx64.tar.gz",
					Executable = "bitcoin-abc-{0}/bin/bitcoind",
					Hash = "5a655ddd8eb6b869b902780efe4ec12de24bbede3f6bf2edc3922048928053e5"
				},
			};

			public NodeDownloadData v0_19_8 = new NodeDownloadData()
			{
				Version = "0.19.8",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.bitcoinabc.org/{0}/win/bitcoin-abc-{0}-win64.zip",
					Archive = "bitcoin-abc-{0}-win64.zip",
					Executable = "bitcoin-abc-{0}/bin/bitcoind.exe",
					Hash = "2080955c7cc2af0a84efa4375ea44a581b0e86b8c7cea735edd9ed5e23866069"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.bitcoinabc.org/{0}/linux/bitcoin-abc-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "bitcoin-abc-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-abc-{0}/bin/bitcoind",
					Hash = "cc40101ffe44340dcc82c6de2bc92040368a857548da133d7f1eb7a45a4c63f5"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://download.bitcoinabc.org/{0}/osx/bitcoin-abc-{0}-osx64.tar.gz",
					Archive = "bitcoin-abc-{0}-osx64.tar.gz",
					Executable = "bitcoin-abc-{0}/bin/bitcoind",
					Hash = "7ecb69387fdea63ad87cda726b56da72fc2cf5d2af94393634b776bcda70a906"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v22_1_0 = new NodeDownloadData()
			{
				Version = "22.1.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcoin-cash-node/bitcoin-cash-node/releases/download/v{0}/bitcoin-cash-node-{0}-win64.zip",
					Archive = "bitcoin-cash-node-{0}-win64.zip",
					Executable = "bitcoin-cash-node-{0}/bin/bitcoind.exe",
					Hash = "03ef21a905667e4482889cd98ecd77c26746b192b97f2bf20cea6327fbdb0f6e"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcoin-cash-node/bitcoin-cash-node/releases/download/v{0}/bitcoin-cash-node-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "bitcoin-cash-node-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-cash-node-{0}/bin/bitcoind",
					Hash = "aa1002d51833b0de44084bde09951223be4f9c455427aef277f91dacd2f0f657"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcoin-cash-node/bitcoin-cash-node/releases/download/v{0}/bitcoin-cash-node-{0}-osx64.tar.gz",
					Archive = "bitcoin-cash-node-{0}-osx64.tar.gz",
					Executable = "bitcoin-cash-node-{0}/bin/bitcoind",
					Hash = "5e5fff474495d3211c3d299dee6224a9fc231f85ca19fa69c7a25d29f92ba745"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v23_0_0 = new NodeDownloadData()
			{
				Version = "23.0.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcoin-cash-node/bitcoin-cash-node/releases/download/v{0}/bitcoin-cash-node-{0}-win64.zip",
					Archive = "bitcoin-cash-node-{0}-win64.zip",
					Executable = "bitcoin-cash-node-{0}/bin/bitcoind.exe",
					Hash = "c58ad8b5f972059e55aa1553bf4950a574a0579af2d010a62f5f4371f10ef3c2"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcoin-cash-node/bitcoin-cash-node/releases/download/v{0}/bitcoin-cash-node-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "bitcoin-cash-node-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-cash-node-{0}/bin/bitcoind",
					Hash = "474d53ba3dc10cee20da4c1e8d77e31a6b3c54c805f72eab7d705c9211c879bd"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcoin-cash-node/bitcoin-cash-node/releases/download/v{0}/bitcoin-cash-node-{0}-osx64.tar.gz",
					Archive = "bitcoin-cash-node-{0}-osx64.tar.gz",
					Executable = "bitcoin-cash-node-{0}/bin/bitcoind",
					Hash = "b3e51705241352c0f86a36c627aaa33dc74fae4a63e904316c03447550a78ad7"
				},
				UseSectionInConfigFile = true
			};
		}

		public class FeathercoinNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_16_0 = new NodeDownloadData()
			{
				Version = "0.16.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://downloads.feathercoin.com/feathercoin-{0}.zip",
					Archive = "feathercoin-{0}-win64.zip",
					Executable = "feathercoin-{0}/bin/feathercoind.exe",
					Hash = "5BA572C4283E8C4C0332A8072C82B4C8FD6CADD0D15E6400BA1C0C2991575155"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "http://downloads.feathercoin.com/feathercoin-0.16.0-x86_64-linux-gnu.tar.gz",
					Archive = "feathercoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "feathercoin-{0}/bin/feathercoind",
					Hash = "5673DA0CE1141D5417D6EE502DAD8741F36100CDF89B4F67A525475E9EB435DE"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "http://downloads.feathercoin.com/feathercoin-{0}-osx64.tar.gz",
					Archive = "feathercoin-{0}-osx64.tar.gz",
					Executable = "feathercoin-{0}/bin/feathercoind",
					Hash = "E6ECE15424DDD83E3FAC64F9A0786AD40F8D89A24ECDC6285353435CD46EEBB1"
				}
			};
		}

		public class DogecoinNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v1_10_0 = new NodeDownloadData()
			{
				Version = "1.10.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dogecoin/dogecoin/releases/download/v{0}/dogecoin-{0}-win64.zip",
					Archive = "dogecoin-{0}-win64.zip",
					Executable = "dogecoin-{0}/bin/dogecoind.exe",
					Hash = "e3a2aa652cb35465d9727b51d1b91094881e6c099883955e9d275add2e26f0ce"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dogecoin/dogecoin/releases/download/v{0}/dogecoin-{0}-linux64.tar.gz",
					Archive = "dogecoin-{0}-linux64.tar.gz",
					Executable = "dogecoin-{0}/bin/dogecoind",
					Hash = "2e5b61842695d74ebcd30f21014cf74b6265f0f7756e9f140f031259bb3cd656"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dogecoin/dogecoin/releases/download/v{0}/dogecoin-{0}-osx-signed.dmg",
					Archive = "dogecoin-{0}-osx64.tar.gz",
					Executable = "dogecoin-{0}/bin/dogecoind",
					Hash = "be854af97efecf30ee18ed846a3bf3a780a0eb0e459a49377d7a8261c212b322"
				},
				SupportCookieFile = false
			};
		}

		public class DashNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_13_0 = new NodeDownloadData()
			{
				Version = "0.13.0.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-win64.zip",
					Archive = "dashcore-{0}-win64.zip",
					Executable = "dashcore-0.13.0/bin/dashd.exe",
					Hash = "89d2e06701f948cfecea612fb6b1a0175227108990a29849fc6fcc8a28fb62fd"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "dashcore-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "dashcore-0.13.0/bin/dashd",
					Hash = "99b4309c7f53b2a93d4b60a45885000b88947af2f329e24ca757ff8cf882ab18"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-osx-unsigned.dmg",
					Archive = "dashcore-{0}-osx-unsigned.dmg",
					Executable = "dashcore-0.13.0/bin/dashd",
					Hash = "6f97f502732e5b63a431d0edb5a9d14e95ff8afb8e7eb94463566a75e7589a70"
				}
			};

			// Note that Dash have DISABLED mining in their offical Windows and Mac binaries as per
			// https://github.com/dashpay/dash/pull/2778 and https://github.com/dashpay/dash/issues/2998.
			// Without generate or generatetoaddress RPC calls the ability to run automated tests is very limited.
			//public NodeDownloadData v0_14_0_1 = new NodeDownloadData()
			//{
			//	Version = "0.14.0.1",
			//	Windows = new NodeOSDownloadData()
			//	{
			//		DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-win64.zip",
			//		Archive = "dashcore-{0}-win64.zip",
			//		Executable = "dashcore-0.14.0/bin/dashd.exe",
			//		Hash = "8d9a0d25cafb166dd49b75b63e059d2896d0162b3e32168c5dddb40c8ac3853b"
			//	},
			//	Linux = new NodeOSDownloadData()
			//	{
			//		DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-x86_64-linux-gnu.tar.gz",
			//		Archive = "dashcore-{0}-x86_64-linux-gnu.tar.gz",
			//		Executable = "dashcore-0.14.0/bin/dashd",
			//		Hash = "c28881104ef7b3bdede7eb2b231b076a6e69213948695b4ec79ccb5621c04d97"
			//	},
			//	Mac = new NodeOSDownloadData()
			//	{
			//		DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-osx-unsigned.dmg",
			//		Archive = "dashcore-{0}-osx-unsigned.dmg",
			//		Executable = "dashcore-0.14.0/bin/dashd",
			//		Hash = "51faffb422fbd3c659ef4b34e7e708174389d8493f2368db4d6c909b52db9115"
			//	}
			//};
		}

		public class TerracoinNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_12_2 = new NodeDownloadData()
			{
				Version = "0.12.2.5",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://terracoin.io/bin/terracoin-core-{0}/terracoin-0.12.2-win64.zip",
					Archive = "terracoin-0.12.2-win64.zip",
					Executable = "terracoin-0.12.2/bin/terracoind.exe",
					Hash = "5d87ede8097557aa02380c6d0b1f15af7e9b3edb0ba31ff6809a33b41051bbef"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://terracoin.io/bin/terracoin-core-{0}/terracoin-0.12.2-x86_64-linux-gnu.tar.gz",
					Archive = "terracoin-0.12.2-x86_64-linux-gnu.tar.gz",
					Executable = "terracoin-0.12.2/bin/terracoind",
					Hash = "a983cb9ca990b77566017fbccfaf70b42cf8947a6f82f247bace19a332ce18e3"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://terracoin.io/bin/terracoin-core-{0}/terracoin-0.12.2-osx64.tar.gz",
					Archive = "terracoin-0.12.2-osx64.tar.gz",
					Executable = "terracoin-0.12.2/bin/terracoind",
					Hash = "51ae932f276be131c5b938e4d7dd710e8a0af3ea8a5ca46aaac8366eafc22c49"
				}
			};
		}

		public class VergeNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v6_0_2 = new NodeDownloadData()
			{
				Version = "6.0.2",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/vergecurrency/verge/releases/download/v6.0.2/verge-6.0.2-win64.zip",
					Archive = "verge-6.0.2-win64.zip",
					Executable = "verge-6.0.2/bin/verged.exe",
					Hash = "6334d5222309337271b47ccf6129a282144c81682c691d9624c35219769e5fb4"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/vergecurrency/verge/releases/download/v6.0.2/verge-6.0.2-x86_64-linux-gnu.tar.gz",
					Archive = "verge-6.0.2-x86_64-linux-gnu.tar.gz",
					Executable = "verge-6.0.2/bin/verged",
					Hash = "cdcb797d9bb11e9fe8062acd9ca46c5bafb02c4868f3c02fd417037584efd721"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/vergecurrency/verge/releases/download/v6.0.2/verge-6.0.2-osx64.tar.gz",
					Archive = "verge-6.0.2-osx64.tar.gz",
					Executable = "verge-6.0.2/bin/verged",
					Hash = "bb3ef22d6e589162c3ff8a3de72a6bd0a80ec0000aa2573960865d3d0f8703c2"
				}
			};
		}

		public class DystemNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v1_0_9_9 = new NodeDownloadData()
			{
				Version = "1.0.9.9",
				Windows = new NodeOSDownloadData()
				{//
					DownloadLink = "https://github.com/Dystem/dystem-core/releases/download/v{0}/dystem-qt-v{0}.exe",
					Archive = "",
					Executable = "dystemd.exe",
					Hash = "1cf1f317aaae6e8edf520d2439f9c950aafb01bd5b46c399c8582524c59273dc"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Dystem/dystem-core/releases/download/v{0}/dystemd.tar.gz",
					Archive = "dystemd.tar.gz",
					Executable = "dystemd",
					//Hash = "8b7c72197f87be1f5d988c274cac06f6539ddb4591a578bfb852a412022378f2"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Dystem/dystem-core/releases/download/v{0}/DYSTEM-Qt.dmg",
					Archive = "DYSTEM-Qt.dmg",
					Executable = "dystemd",
					//Hash = "90ca27d6733df6fc69b0fc8220f2315623fe5b0cbd1fe31f247684d51808cb81"
				}
			};
		}

		public class MogwaiNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_12_2 = new NodeDownloadData()
			{
				Version = "0.12.2.4",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/mogwaicoin/mogwai/releases/download/v{0}/mogwaicore-{0}-win64.zip",
					Archive = "mogwaicore-{0}-win64.zip",
					Executable = "mogwaicore-0.12.2/bin/mogwaid.exe",
					Hash = "af830999026809416cf5b93d840e6e90ce8af0dc61738bd9bf1c5f059439b0a6"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/mogwaicoin/mogwai/releases/download/v{0}/mogwaicore-{0}-linux64.tar.gz",
					Archive = "mogwaicore-{0}-linux64.tar.gz",
					Executable = "mogwaicore-0.12.2/bin/mogwaid",
					Hash = "8b7c72197f87be1f5d988c274cac06f6539ddb4591a578bfb852a412022378f2"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/mogwaicoin/mogwai/releases/download/v{0}/mogwaicore-{0}-osx.dmg",
					Archive = "mogwaicore-{0}-osx.dmg",
					Executable = "mogwaicore-0.12.2/bin/mogwaid",
					Hash = "90ca27d6733df6fc69b0fc8220f2315623fe5b0cbd1fe31f247684d51808cb81"
				}
			};
		}
		public class BGoldNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_15_0 = new NodeDownloadData()
			{
				Version = "0.15.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/BTCGPU/BTCGPU/releases/download/v{0}.2/bitcoin-gold-{0}-win64.zip",
					Archive = "bitcoin-gold-{0}-win64.zip",
					Executable = "bitcoin-gold-{0}/bin/bgoldd.exe",
					Hash = "497dba65c2047bc374532d83f91bf38bc7b44eae2eca36b9a375b59abfe9e6fc"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/BTCGPU/BTCGPU/releases/download/v{0}.2/bitcoin-gold-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "bitcoin-gold-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-gold-{0}/bin/bgoldd",
					Hash = "c49fa0874333837526cf1b4fce5b58abe6437b48e64dcf095654e6317e1f66a3"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/BTCGPU/BTCGPU/releases/download/v{0}.2/bitcoin-gold-{0}-osx64.tar.gz",
					Archive = "bitcoin-gold-{0}-osx64.tar.gz",
					Executable = "bitcoin-gold-{0}/bin/bgoldd",
					Hash = "87bb6dd288ffa3d0cd753a8013a177a2e48b63ddf10f3593634388b59a60c45b"
				},
			};
		}

		public class PolisNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v1_4_3 = new NodeDownloadData()
			{
				Version = "1.4.3",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/polispay/polis/releases/download/v{0}/poliscore-{0}-win64.zip",
					Archive = "poliscore-{0}-win64.zip",
					Executable = "poliscore-{0}/bin/polisd.exe",
					Hash = "ca470f2c4fcee527019f08406d26a469fc84e3118f87b1f4ac1e1f05dcee284e"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/polispay/polis/releases/download/v{0}/poliscore-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "poliscore-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "poliscore-{0}/bin/polisd",
					Hash = "9b49c912b154c4584b7e77ba7665f60cc78cc1c1321f3ca08b36efca016d359f"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/polispay/polis/releases/download/v{0}/poliscore-{0}-osx64.tar.gz",
					Archive = "poliscore-{0}-osx64.tar.gz",
					Executable = "poliscore-{0}/bin/polisd",
					Hash = "9d7ae6cdc6afdecfbf6425e4e652baeb7c6b440c90dc8e7ac1cb30a7f7e0574e"
				}
			};
		}

		public class BitcoreNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_90_9_10 = new NodeDownloadData()
			{
				Version = "0.90.9.10",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcore-btx/BitCore/releases/download/{0}/bitcore-win64-daemon.zip",
					Archive = "bitcore-win64-daemon.zip",
					Executable = "bitcored.exe",
					Hash = "d50c75d11b88b15f42b8e59919684f61c585009f3a2465ea0fc2890f448d5bb2"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/bitcore-btx/BitCore/releases/download/{0}/bitcore-x86_64-linux-gnu_no-wallet.tar.gz",
					Archive = "bitcore-x86_64-linux-gnu_no-wallet.tar.gz",
					Executable = "bin/bitcored",
					Hash = "1980b85c229e00f58a474858a2da9fe9fc41f0fba06416337ea9f5e4aa197a1b"
				}
			};
		}

		public class MonacoinNodeDownloadData : NodeDownloadDataBase
		{
			public NodeDownloadData v0_15_1 = new NodeDownloadData()
			{
				Version = "0.15.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/monacoinproject/monacoin/releases/download/monacoin-{0}/monacoin-{0}-win64.zip",
					Archive = "monacoin-{0}-win64.zip",
					Executable = "monacoin-{0}/bin/monacoind.exe",
					Hash = "420cba3c5e70cc913c2cacab9162e8fd1408fc2aaa345b04d3f44615c63d7b17"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/monacoinproject/monacoin/releases/download/monacoin-{0}/monacoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "monacoin-{0}-linux64.tar.gz",
					Executable = "monacoin-{0}/bin/monacoind",
					Hash = "8199f92d4296ea99891db34f5d779d7e95a2338425544b82b04fd8b427dae905"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/monacoinproject/monacoin/releases/download/monacoin-{0}/monacoin-{0}-osx-unsigned.dmg",
					Archive = "monacoin-{0}-osx.dmg",
					Executable = "monacoin-{0}/bin/monacoind",
					Hash = "d19cc2cc12732c49351add23075c4f7a4ec92ee04874ec7037429dc4f9f1c058"
				}
			};
		}

		public class UfoNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v0_16_0 = new NodeDownloadData()
			{
				Version = "0.16.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://downloads.ufobject.com/ufo-0.16.0.zip",
					Archive = "UFO-{0}-win64.zip",
					Executable = "UFO-{0}/bin/ufod.exe",
					Hash = "B06D8564CF2BF95EDD4AECEB3F725C12FB18A31398E59B48A242AED210261FAE"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://downloads.ufobject.com/ufo-0.16.0-x86_64-linux-gnu.tar.gz",
					Archive = "UFO-{0}-linux64.tar.gz",
					Executable = "UFO-{0}/bin/ufod",
					Hash = "2A0F4ED78EA58C232CCEA6DDD4EB36F766C72663D1DF9B6FDA0CB39143FE0F60"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://downloads.ufobject.com/ufo-0.16.0-osx64.tar.gz",
					Archive = "UFO-{0}-osx.dmg",
					Executable = "UFO-{0}/bin/ufod",
					Hash = "5CC7E5F742584BAD0CADD516B09C93566D38B42C352F21D521C84C9490088ACB"
				}
			};
		}

		public class GroestlcoinNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v2_16_0 = new NodeDownloadData()
			{
				Version = "2.16.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Archive = "groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Executable = "GRS-{0}\\groestlcoind.exe",
					Hash = "5a53dcaaf209d9871be0c297bbdc9b6a03275d3b2f252d03a813043cd98cf892",
					CreateFolder = "GRS-{0}",
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "GRS-{0}/groestlcoind",
					Hash = "9a57add934d8715090fe6a77867271884495cf53be6fc7ca986b3ddc626772f1",
					CreateFolder = "GRS-{0}",
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Executable = "GRS-{0}/groestlcoind",
					Hash = "4cd9c96a54849204002e986458ac35e2f437a187179c6a5e7f518e7bac44b75b",
					CreateFolder = "GRS-{0}",
				}
			};

			public NodeDownloadData v2_16_3 = new NodeDownloadData()
			{
				Version = "2.16.3",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Archive = "groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Executable = "GRS-{0}\\groestlcoind.exe",
					Hash = "9617e7ec61a1f8850d11613ff3d4f4e1d8caa29e118ec1c29e07ef323b16557d",
					CreateFolder = "GRS-{0}"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "GRS-{0}/groestlcoind",
					Hash = "f15bd5e38b25a103821f1563cd0e1b2cf7146ec9f9835493a30bd57313d3b86f",
					CreateFolder = "GRS-{0}"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Executable = "GRS-{0}/groestlcoind",
					Hash = "4976c8f60105a32bb0d8e230577f60438d5bed45a9aa92c51f0dd79a13c6b89e",
					CreateFolder = "GRS-{0}"
				}
			};

			public NodeDownloadData v2_17_2 = new NodeDownloadData()
			{
				Version = "2.17.2",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Archive = "groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Executable = "groestlcoin-{0}/bin/groestlcoind.exe",
					Hash = "2378209ef954d50cd82ae70f04b4fc5e07d16e11f13c7183a6647f8d60de1f85"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "e90f6ceb56fbc86ae17ee3c5d6d3913c422b7d98aa605226adb669acdf292e9e"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "b3fe245752a445ce56cac265af7ed63906c7c1c8e2c932891369be72c290307d"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v2_18_2 = new NodeDownloadData()
			{
				Version = "2.18.2",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-win64.zip",
					Archive = "groestlcoin-{0}-win64.zip",
					Executable = "groestlcoin-{0}/bin/groestlcoind.exe",
					Hash = "44ab9b896db1c8492facc1f6353ea6b59e72328a38859a419a08113520e9c0b8"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "9ee26e1cd7967d0dc88670dbbdb99f95236ebc218f75977efb23f03ad8b74250"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-osx64.tar.gz",
					Archive = "groestlcoin-{0}-osx64.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "fdb722b326433501b179a33ac20e88b5fd587a249878eb94a9981da2097c42a5"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v2_19_1 = new NodeDownloadData()
			{
				Version = "2.19.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-win64.zip",
					Archive = "groestlcoin-{0}-win64.zip",
					Executable = "groestlcoin-{0}/bin/groestlcoind.exe",
					Hash = "27e1518b80d6212bc7dcb45fd20d4b12553f8872600996aedd8bf3dd33783e48"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "0646cae023a0be0821f357d33bdbf81fc05fc9a9e3e9d4e5936d5053f1a988d4"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-osx64.tar.gz",
					Archive = "groestlcoin-{0}-osx64.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "902d38bea03fded2762acd1855cddd4a7b210acac9921ea56d816e622c4244ba"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v2_20_1 = new NodeDownloadData()
			{
				Version = "2.20.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-win64.zip",
					Archive = "groestlcoin-{0}-win64.zip",
					Executable = "groestlcoin-{0}/bin/groestlcoind.exe",
					Hash = "d7b506074aa0fe66c77106c4cc7123923be169b17ee015bc0433d6a3edb9278c"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "0a877be9dac14f4d9aab95d6bfd51547275acbcc3e6553f0cb82c5c9f35f333c"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-osx64.tar.gz",
					Archive = "groestlcoin-{0}-osx64.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "16564cf6df5f4edead0f8a807f285e34e9f20b2770c2f66ab803de5152e38d3b"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v2_21_0 = new NodeDownloadData()
			{
				Version = "2.21.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-win64.zip",
					Archive = "groestlcoin-{0}-win64.zip",
					Executable = "groestlcoin-{0}/bin/groestlcoind.exe",
					Hash = "811933f361dbfacda35d8408c333c1777b546dd06e7b5f2884b558f76026cff2"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "cc01c2d8a3f5e6730e931fdc579658aff74d9618adadee095bea06da54e75d5b"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-osx64.tar.gz",
					Archive = "groestlcoin-{0}-osx64.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "2c59d56c4a77087f2e3c91423fd444140e200395c1cb9cb35c65e47cf8befc3d"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v2_21_1 = new NodeDownloadData()
			{
				Version = "2.21.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-win64.zip",
					Archive = "groestlcoin-{0}-win64.zip",
					Executable = "groestlcoin-{0}/bin/groestlcoind.exe",
					Hash = "2d53e696979283c79c9b3a310a0c408dba3732f66c51781179e10da77176aa98"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "b03bd4211f9473b39a12f6e57e64fe7bc9f2f54f27491f46f5b1bff5b96db7a5"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-osx64.tar.gz",
					Archive = "groestlcoin-{0}-osx64.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "d3b36a30f2a9a624087414820237df0ec5289a52fc24c3a1a38f67be7698073c"
				},
				UseSectionInConfigFile = true
			};

			public NodeDownloadData v22_0 = new NodeDownloadData()
			{
				Version = "22.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-win64.zip",
					Archive = "groestlcoin-{0}-win64.zip",
					Executable = "groestlcoin-{0}/bin/groestlcoind.exe",
					Hash = "a4cad64a4c47796d3a6c2d3f28e1088a6ae39ca8a66d6b06dc453bfd234a4275"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "b30c5353dd3d9cfd7e8b31f29eac125925751165f690bacff57effd76560dddd"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-osx64.tar.gz",
					Archive = "groestlcoin-{0}-osx64.tar.gz",
					Executable = "groestlcoin-{0}/bin/groestlcoind",
					Hash = "bdcdfac563eb54bc3de185c9b92200a36ccbd10d018aebd665e0bbe65a4480db"
				},
				UseSectionInConfigFile = true
			};
		}

		public class ZclassicNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v1_0_14 = new NodeDownloadData()
			{
				Version = "1.0.14",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/z-classic/zclassic/releases/download/v{0}/zclassic-v{0}-win.zip",
					Archive = "zclassic-v{0}-win.zip",
					Executable = "zclassic-{0}/bin/zcld.exe",
					Hash = "99923ACC9D45609FDD4098AF8033542A34E41840091C9121C326571889811A2A"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/z-classic/zclassic/releases/download/v{0}/zclassic-v{0}-linux.tar.gz",
					Archive = "zclassic-v{0}-linux.tar.gz",
					Executable = "zclassic-{0}/bin/zcld",
					Hash = "51e49a81f8493923c08e3cdd72b253bbcc10fe582e97f6926e6267a4f337b696"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/z-classic/zclassic/releases/download/v{0}/zclassic-v{0}-mac.zip",
					Archive = "zclassic-v{0}-mac.zip",
					Executable = "zclassic-{0}/bin/zcld",
					Hash = ""
				},
			};
		}

		public class ElementsNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v0_14_1 = new NodeDownloadData()
			{
				Version = "0.14.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://aois.blob.core.windows.net/public/ElementsBinaries/elements-{0}-win64.zip",
					Archive = "elements-{0}-win64.zip",
					Executable = "elements-{0}/bin/elementsd.exe",
					Hash = "d0d2e2a26d1fb64979e3050aa6b0e5e619d80f0f40552b39c62d07fdb889df90"
				},
				RegtestFolderName = "elementsregtest",
				AdditionalRegtestConfig = "initialfreecoins=210000000000000"
			};

			public NodeDownloadData v0_18_1_1 = new NodeDownloadData()
			{
				Version = "0.18.1.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/ElementsProject/elements/releases/download/elements-{0}/elements-{0}-win64.zip",
					Archive = "elements-{0}-win64.zip",
					Executable = "elements-0.18.1.1/bin/elementsd.exe",
					Hash = "f6ca18e3f4fe4fb4aadb70b7b447466afb68eec11d9cd5e2cac613414a28a1a5"
				},
				RegtestFolderName = "elementsregtest",
				Chain = "elementsregtest",
				AdditionalRegtestConfig = "initialfreecoins=210000000000000\nvalidatepegin=0\n\ncon_dyna_deploy_start=99999999999999999",
				UseSectionInConfigFile = true,
			};

			public NodeDownloadData v0_18_1_12 = new NodeDownloadData()
			{
				Version = "0.18.1.12",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/ElementsProject/elements/releases/download/elements-0.18.1.12/elements-0.18.1.12-win64.zip",
					Archive = "elements-{0}-win64.zip",
					Executable = "elements-0.18.1.12/bin/elementsd.exe",
					Hash = "ac3eabc603f1c91462b79ea10e4d7ad5f090ac2f2136d43078c12d1e5bdee831"
				},
				AdditionalRegtestConfig = "initialfreecoins=210000000000000\nvalidatepegin=0\n\ncon_dyna_deploy_start=0\n\ncon_dyna_deploy_signal=1\ncon_nminerconfirmationwindow=1\ncon_nrulechangeactivationthreshold=1",
				RegtestFolderName = "elementsregtest",
				Chain = "elementsregtest",
				UseSectionInConfigFile = true,
			};

		}
		public class LiquidNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v3_14_1_21 = new NodeDownloadData()
			{
				Version = "3.14.1.21",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://aois.blob.core.windows.net/public/LiquidBinaries/liquid-{0}-win64.zip",
					Archive = "liquid-{0}-win64.zip",
					Executable = "liquid-{0}/bin/liquidd.exe",
					Hash = "cedab6e7d3f5b6eac4ce8cf81c480dc49599ac34a2d7ede1d15bb9547f800a8a"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Blockstream/liquid/releases/download/liquid.{0}/liquid-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "liquid-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "liquid-{0}/bin/liquidd",
					Hash = "ea2836aa267b32b29e890acdd5e724b4be225c34891fd26426ce741c12c1e166"
				},
				RegtestFolderName = "liquidregtest",
				AdditionalRegtestConfig = "initialfreecoins=210000000000000\nvalidatepegin=0"
			};

			public NodeDownloadData v3_14_1_23 = new NodeDownloadData()
			{
				Version = "3.14.1.23",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Blockstream/liquid/releases/download/liquid.{0}/liquid-{0}-win64.zip",
					Archive = "liquid-{0}-win64.zip",
					Executable = "liquid-{0}/bin/liquidd.exe",
					Hash = "8b18aebbbf8092b052db648e34adf52342a02923d758181cfb8bc0894c90dfb5"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Blockstream/liquid/releases/download/liquid.{0}/liquid-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "liquid-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "liquid-{0}/bin/liquidd",
					Hash = "cb135d60407fd4fcd04d1f021cd314e9f8f50a8f0a660551f5ea251b0fea3ffc"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Blockstream/liquid/releases/download/liquid.{0}/liquid-{0}-osx64.tar.gz",
					Archive = "liquid-{0}-x86_64-osx64.tar.gz",
					Executable = "liquid-{0}/bin/liquidd",
					Hash = "91f5859414d6bce99695c2de01317ec1454d3d99615f81f301b85f767b5e2cf2  "
				},
				RegtestFolderName = "liquidregtest",
				AdditionalRegtestConfig = "initialfreecoins=210000000000000\nvalidatepegin=0"
			};
		}
		public class MonoeciNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v0_12_2_3 = new NodeDownloadData()
			{
				Version = "0.12.2.3",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/monacocoin-net/monoeci-core/releases/download/v{0}/monoeciCore-{0}-win32.zip",
					Archive = "monoeciCore-{0}-win32.zip",
					Executable = "monoeciCore-{0}/bin/monoecid.exe",
					Hash = "19172ed041227ce0eaebaa67fd6cd36ea5a1c753013c035da34e7817a30c5c35",
					CreateFolder = "monoeciCore-{0}",
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/monacocoin-net/monoeci-core/releases/download/v{0}/monoeciCore-{0}-linux64.tar.gz",
					Archive = "monoeciCore-{0}-linux64.tar.gz",
					Executable = "monoeciCore-{0}/bin/monoecid",
					Hash = "8cab56a02a2b7f5d41af6dd9e09208be13ded20a06b29c5e2e95bb19db3694f1",
					CreateFolder = "monoeciCore-{0}",
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/monacocoin-net/monoeci-core/releases/download/v{0}/monoeciCore-{0}-osx.tar.gz",
					Archive = "monoeciCore-{0}-osx.tar.gz",
					Executable = "monoeciCore-{0}/bin/monoecid",
					Hash = "60a2414e01950e8f2f91da56334116866261e12240f4da2a698ed142c7c68d4a",
					CreateFolder = "monoeciCore-{0}",
				}
			};
		}
		public class GoByteNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v0_12_2_4 = new NodeDownloadData()
			{
				Version = "0.12.2.4",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/gobytecoin/gobyte/releases/download/v{0}/GoByte_{0}_Windows32.zip",
					Archive = "GoByte_{0}_Windows32.zip",
					Executable = "GoByte_{0}_Windows32/gobyted.exe",
					Hash = "333144de13cb5b1a5e1d81890ed8e91dbc9e52bb63eecd10f397c879f5725de1",
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/gobytecoin/gobyte/releases/download/v{0}/GoByteCore-{0}_Linux64.tar.gz",
					Archive = "GoByteCore-{0}_Linux64.tar.gz",
					Executable = "GoByteCore-{0}_Linux64/gobyted",
					Hash = "d2419274d1234b80c5756247775ace04abc85a8f74b91760c8c25f65212e4e57",
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/gobytecoin/gobyte/releases/download/v0.12.2.4/GoByte_0.12.2.4_MacOS.dmg",
					Archive = "GoByte_0.12.2.4_MacOS.dmg",
					Executable = "gobyted",
					Hash = "de8fa9bd6aa4dbab2c93627b94185eb58b24cd05d5628ede1086f305362f1b0f",
				}
			};
		}
		public class ColossusNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v1_1_1 = new NodeDownloadData()
			{
				Version = "1.1.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/ColossusCoinXT/ColossusCoinXT/releases/download/v{0}/colx-v{0}-win32.zip",
					Archive = "colx-v{0}-win64.zip",
					Executable = "colx-v{0}/bin/colxd.exe",
					Hash = "d4ec16815d85a122f57a6a1a1fe9ca19487a1aac3294dc041315bce2e76772bd",
					CreateFolder = "colx-v{0}",
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/ColossusCoinXT/ColossusCoinXT/releases/download/v{0}/colx-v{0}-x86_64-linux-gnu.tar.gz",
					Archive = "colx-v{0}-x86_64-linux-gnu.tar.gz",
					Executable = "colx-v{0}/bin/colxd",
					Hash = "4812cd2296467b0524625a13c205832039d03990eddf7e31e180f6cbdb9f8917",
					CreateFolder = "colx-v{0}",
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/ColossusCoinXT/ColossusCoinXT/releases/download/v{0}/colx-{0}-osx64.tar.gz",
					Archive = "colx-{0}-osx64.tar.gz",
					Executable = "colx-{0}/bin/colxd",
					Hash = "6cb3411ea02d2e7dc17824dffece1ba1e61ea9842eb1f14f15ae78b99bb8493a",
					CreateFolder = "colx-v{0}",
				}
			};
		}

		public class GincoinNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v1_1_0_0 = new NodeDownloadData()
			{
				Version = "1.1.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink =
						"https://github.com/gincoin-dev/gincoin-core/releases/download/{0}.0/gincoincore-{0}-windows-64bit.exe",
					Archive = "gincoincore-{0}-windows-64bit.exe",
					Executable = "gincoincore-{0}-windows-64bit.exe",
					Hash = "B64E4C334D3597FC7A094DE4CA1955DBE06E9C5856598D9A2B3D8E5907D71EF9"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink =
						"https://github.com/gincoin-dev/gincoin-core/releases/download/{0}.0/gincoin-binaries-linux-64bit.tar.gz",
					Archive = "gincoin-binaries-linux-64bit.tar.gz",
					Executable = "gincoin-binaries/gincoind",
					Hash = "1C249AEC8CD3D66F8D9D49CF3AD1526736216C76200D4BB83E89657879D55F92"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/gincoin-dev/gincoin-core/releases/download/{0}.0/gincoin-binaries-mac.zip",
					Archive = "gincoin-binaries-mac.zip",
					Executable = "gincoin-binaries-mac/gincoind",
					Hash = "CFFE613A18AB3ABB0200EC5E100036DDF710C4EC832FBB67B3D5196CDBF541EA"
				}
			};
		}
		public class KotoNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v2_0_0 = new NodeDownloadData()
			{
				Version = "2.0.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Harumaki-cryptocoin/koto-releases.github.io/releases/download/{0}/koto-{0}-win64.zip",
					Archive = "koto-{0}-win64.zip",
					Executable = "koto-{0}-win64/daemon/kotod.exe",
					Hash = "6bda95dc3f4597d06ecd5c5cbc450d1e0822b126de99d31e8cab1395b8eeac0a"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Harumaki-cryptocoin/koto-releases.github.io/releases/download/{0}/koto-{0}-linux64.zip",
					Archive = "koto-{0}-linux64.zip",
					Executable = "koto-{0}-linux64/bin/kotod",
					Hash = "77168affc53533833f9f904bba5667629537ec43aedd54845f51096212fe7bcc"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "hhttps://github.com/Harumaki-cryptocoin/koto-releases.github.io/releases/download/{0}/koto-{0}-darwin.dmg",
					Archive = "koto-{0}-darwin.dmg",
					Executable = "koto-{0}/bin/kotod",
					Hash = "6c23e649972b6c9fb828a554c1a170379a7792ad6f1ecc01210d5207d034aaa2"
				}
			};
		}

		public class BitcoinplusNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v2_7_0 = new NodeDownloadData()
			{
				Version = "2.7.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://downloads.bitcoinplus.org/bitcoinplus-2.7.0.zip",
					Archive = "bitcoinplus-{0}-win64.zip",
					Executable = "bitcoinplus-{0}/bin/bitcoinplusd.exe",
					Hash = "3eb8fc8c57eba865c4818653f1745adbca7ee5c9065e622311907eb4d5c34273"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://downloads.bitcoinplus.org/bitcoinplus-2.7.0-x86_64.tar.gz",
					Archive = "bitcoinplus-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoinplus-{0}/bin/bitcoinplusd",
					Hash = "753547b23988124defbf9bee029b4902d6277ce467c63f2ad6588c53817b2251"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://downloads.bitcoinplus.org/bitcoinplus-2.7.0-osx64.tar.gz",
					Archive = "Bitcoinplus-Core.dmg",
					Executable = "bitcoinplusd",
					Hash = "09d381ed0082fccd6e3af4792b975fee177cffc546fd449181a4c37b4907cff8"
				}
			};
		}

		public class ChaincoinNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v0_16_4 = new NodeDownloadData()
			{
				Version = "0.16.4",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/fellowserf/chaincoin/releases/download/v{0}/chaincoincore-{0}-win64.zip",
					Archive = "chaincoincore-{0}-win64.zip",
					Executable = "chaincoincore-0.16.4/bin/chaincoind.exe",
					Hash = "58dc6cc513fadcd9216062d332a5214fcb28a51a80883a32b3b9534093cdda2c"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/chaincoin/chaincoin/releases/download/v{0}/chaincoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "chaincoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "chaincoin-0.16.4/bin/chaincoind",
					Hash = "b841f9c2098e973217a32a6213fe2a5bfe2987dd7b5c851a38082ce191b65283"
				}
			};
		}

		public class ZCoinNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v0_13_8_3 = new NodeDownloadData()
			{
				Version = "0.13.8.3",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-win64.zip",
					Archive = "zcoin-{0}-win64.zip",
					Executable = "zcoin-{0}/bin/zcoind.exe",
					Hash = "f0cca1fca157c8549cdfdbd2587d2dfad9234a63df193f666d8a9d77df5a8eb3"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "zcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "zcoin-{0}/bin/zcoind",
					Hash = "364ea09583b46866a7d84b924355e41cf5d8f2f1a54f8abb6c3f10b63d1933f1"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/zcoinofficial/zcoin/releases/download/v{0}/zcoin-{0}-osx64.tar.gz",
					Archive = "zcoin-{0}-osx64.tar.gz",
					Executable = "zcoin-{0}/bin/zcoind",
					Hash = "9d7ae6cdc6afdecfbf6425e4e652baeb7c6b440c90dc8e7ac1cb30a7f7e0574e"
				}
			};
		}

		/// <summary>
		/// Using Stratis C# full node.
		/// Should be updated to use official release once it is deployed.
		/// </summary>
		public class StratisNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v3_0_0 = new NodeDownloadData()
			{
				Version = "3.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/mikedennis/StratisBitcoinFullNode/releases/download/{0}/stratis-{0}-win64.zip",
					Archive = "stratis-{0}-win64.zip",
					Executable = "stratis-{0}-win64/Stratis.StratisD.exe",
					Hash = "7B0ABEA75B032D8FAF3BEE071A892E2864A31A8ECC42F7AA4300CC51B1CA1D5A"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/mikedennis/StratisBitcoinFullNode/releases/download/{0}/stratis-{0}-linux64.tgz",
					Archive = "stratis-{0}-linux64.tgz",
					Executable = "stratis-{0}-linux64/Stratis.StratisD",
					Hash = "57965AA4034468ED4C9FDD6CDBAD8A1F722DDE6A6BCAE6291D79BF76C5FC644B"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/mikedennis/StratisBitcoinFullNode/releases/download/{0}/stratis-{0}-osx64.tgz",
					Archive = "stratis-{0}-osx64.tgz",
					Executable = "stratis-{0}-osx64/Stratis.StratisD",
					Hash = "04C4F4EBCA494ABD6C29D834D86AFEA559BB8893A1E74BE849D050FEFC164C72"
				},
				SupportCookieFile = false,
				AdditionalRegtestConfig = "defaultwalletname=default" + Environment.NewLine + "maxtipage=2147483647" + Environment.NewLine + "unlockdefaultwallet=1"
			};
		}

		public class DogeCashNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v5_1_1 = new NodeDownloadData()
			{
				Version = "5.2.2",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dogecash/dogecash/releases/download/v5.2.2/DogeCash-5.2.2-win32.zip",
					Archive = "DogeCash-5.2.2-win32.zip",
					Executable = "dogecashd.exe",
					Hash = "ca9bb0d8e8dc9733b839ba71e626f8e1d20f4869d7105e2145193a793294acdf"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dogecash/dogecash/releases/download/v5.2.2/DogeCash-5.2.2-x86_64-linux-gnu.tar.gz",
					Archive = "DogeCash-5.2.2-x86_64-linux-gnu.tar.gz",
					Executable = "dogecashd",
					Hash = "95b5bd3155f5bed6f91aa8a240f525f8f75ff7a6e7e276aa0a1acfa016ddfaae"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dogecash/dogecash/releases/download/v5.2.2/DogeCash-5.2.2-osx-unsigned.dmg",
					Archive = "DogeCash-5.2.2-osx-unsigned.dmg",
					Executable = "DogeCash-5.2.2-osx-unsigned.dmg",
					Hash = "559ce13c257ed4378ae017d8b9b862d1e5a4a83e8fbe33ce1c51eea2fc088d56"
				}
			};
		}

		public class ArgoneumNodeDownloadData
 : NodeDownloadDataBase
		{
			// Note that Argoneum has mining disabled by default in offical Windows and Mac binaries as per
			// https://github.com/dashpay/dash/pull/2778 and https://github.com/dashpay/dash/issues/2998.
			// Without generate or generatetoaddress RPC calls the ability to run automated tests is very limited.
			public NodeDownloadData v1_4_1 = new NodeDownloadData()
			{
				Version = "1.4.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Argoneum/argoneum/releases/download/v{0}.0/argoneum-{0}-win64.zip",
					Archive = "argoneum-{0}-win64.zip",
					Executable = "argoneum-1.4.1/bin/argoneumd.exe",
					Hash = "06ed74f14135b7fc5d7c6618723cf7e385ac36303e5f83c632241dba9a095248"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Argoneum/argoneum/releases/download/v{0}.0/argoneum-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "argoneum-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "argoneum-1.4.1/bin/argoneumd",
					Hash = "821e98b2af5c8f12ca39dd399925bdffed400ff702940f45ddf5ad375987d3f6"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Argoneum/argoneum/releases/download/v{0}.0/argoneum-{0}-osx64.tar.gz",
					Archive = "argoneum-{0}-osx64.tar.gz",
					Executable = "argoneum-1.4.1/bin/argoneumd",
					Hash = "7c6dd15bd87042d57ce73c6b124586d33f014ae74b66299e29b926f7a361e5be"
				}
			};
		}

		public class QtumNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v0_18_3 = new NodeDownloadData()
			{
				Version = "0.18.3",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/qtumproject/qtum/releases/download/mainnet-ignition-v{0}/qtum-{0}-win64.zip",
					Archive = "qtum-{0}-win64.zip",
					Executable = "qtum-{0}/bin/qtumd.exe",
					Hash = "e328fb5768d573ccca52c8021497f356781c08af80ed87d478627ff311d8996e"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/qtumproject/qtum/releases/download/mainnet-ignition-v{0}/qtum-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "qtum-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "qtum-{0}/bin/qtumd",
					Hash = "f70b21da2ff3e0e7aecfe3a9861df20c6be8d67e5be758f70e6b05c3c9afc951"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/qtumproject/qtum/releases/download/mainnet-ignition-v{0}/qtum-{0}-osx64.tar.gz",
					Archive = "qtum-{0}-osx64.tar.gz",
					Executable = "qtum-{0}/bin/qtumd",
					Hash = "91f5e07fae24c282cb74babb158ce7fe70d6b80ce58f134722d5e7d70f835886"
				},
				UseSectionInConfigFile = true
			};
		}

		public class MonetaryUnitNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v2_1_6 = new NodeDownloadData()
			{
				Version = "2.1.6",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/muecoin/MUE/releases/download/v2.1.6/mon-2.1.6-win64.zip",
					Archive = "mon-2.1.6-win64.zip",
					Executable = "mon/bin/monetaryunitd.exe",
					Hash = "32ff392d34396e3b5c123ee629a72aef1bc9380afd9630bb98c0d8a3eaa9ea22"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/muecoin/MUE/releases/download/v2.1.6/mon-2.1.6-x86_64-linux-gnu.tar.gz",
					Archive = "mon-2.1.6-x86_64-linux-gnu.tar.gz",
					Executable = "mon/bin/monetaryunitd",
					Hash = "87aee9fd607af80fded5c8495fbb978f646cd7e7020be071ca3868840c42d62f"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/muecoin/MUE/releases/download/v2.1.6/mon-2.1.6-osx64.tar.gz",
					Archive = "mon-2.1.6-osx64.tar.gz",
					Executable = "mon/bin/monetaryunitd",
					Hash = "af3712f4d6a526d8003198bb8c80d8fbbb97d97249ed9737a829527f8fab1e74"
				}
			};
		}


		public class LBRYCreditsNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v2_1_6 = new NodeDownloadData()
			{
				Version = "2.1.6",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/lbryio/lbrycrd/releases/download/v0.17.3.2/lbrycrd-windows-1732.zip",
					Archive = "lbrycrd-windows-1732.zip",
					Executable = "lbrycrdd.exe",
					Hash = "272bed01b1d62a51f8586c600c2be59f052236760b8f08c9b012ecf64edd726b"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/lbryio/lbrycrd/releases/download/v0.17.3.2/lbrycrd-linux-1732.zip",
					Archive = "lbrycrd-windows-1732.zip",
					Executable = "lbrycrdd",
					Hash = "7d0de93a178553a5832b6c560ceb1c270047a124b834d875a772d4dcebac9056"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/lbryio/lbrycrd/releases/download/v0.17.3.2/lbrycrd-darwin-1732.zip",
					Archive = "lbrycrd-darwin-1732.zip",
					Executable = "lbrycrdd",
					Hash = "65cd5c79c51758def40c723b532760381662c5439ae07f313761446475b73afc"
				}
			};
		}

		public class AlthashNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v2_5_1 = new NodeDownloadData()
			{
				Version = "2.5.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/HTMLCOIN/HTMLCOIN/releases/download/v{0}/htmlcoin-{0}-win64-setup.exe",
					Archive = "htmlcoin-{0}-win64-setup.exe",
					Executable = "HTMLCOIN/bin/htmlcoind.exe",
					Hash = "13262996832e0f32916f5188ab7b80aff08e0920a35d0f317816e46113036c7e"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/HTMLCOIN/HTMLCOIN/releases/download/v{0}/htmlcoin-{0}-linux64.tar.gz",
					Archive = "htmlcoin-{0}-linux64.tar.gz",
					Executable = "HTMLCOIN/bin/htmlcoind",
					Hash = "0863c4f67fc237f563d44d7fa9e6ca4d0e50e5e89172b56034a2c65f7e908c0a"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/HTMLCOIN/HTMLCOIN/releases/download/v{0}/htmlcoin-{0}-mac.dmg",
					Archive = "htmlcoin-{0}-mac.dmg",
					Executable = "HTMLCOIN/bin/htmlcoind",
					Hash = "9dc520cc0068984d825fe0051a33ee717bc04bda9cd4063d2de20897a86bb1d7"
				},

				UseSectionInConfigFile = true
			};
		}

		public class NeblioNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v3_2_0 = new NodeDownloadData()
			{
				Version = "3.2.0",
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/NeblioTeam/neblio/releases/download/v3.2.0/2021-02-01---v3.2.0-5f11d65---nebliod---ubuntu16.04.tar.gz",
					Archive = "2021-02-01---v3.2.0-5f11d65---nebliod---ubuntu16.04.tar.gz",
					Executable = "nebliod",
					Hash = "afc52eeb0726dbb222222debb51e8b8bb333e4987eabae2b12cd95faa3214ec1"
				},

				UseSectionInConfigFile = true
			};
		}


		/// <summary>
		/// Using Stratis C# full node.
		/// Should be updated to use official release once it is deployed.
		/// </summary>
		public class XDSNodeDownloadData
 : NodeDownloadDataBase
		{
			public NodeDownloadData v1_0_16 = new NodeDownloadData()
			{
				Version = "1.0.16",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/xds-playgrounds/xds-blockcore-experimental/releases/download/v{0}/xds-blockcore-v{0}-win-x64.zip",
					Archive = "xds-blockcore-v{0}-win-x64.zip",
					Executable = "xds-blockcore-v{0}-win-x64/blockcore.xdsd.exe",
					Hash = "dd5661dcac7dcba6a78dd1e706623b65ddb1ad15252b286d692e99dd0b653e02"
				},
			};
		}

		public class TriptourcoinNodeDownloadData : NodeDownloadDataBase
		{

			public NodeDownloadData v0_21_0 = new NodeDownloadData()
			{
				Version = "0.21.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "http://node1.triptourcoin.com/triptourcoin-{0}/triptourcoin-{0}-windows.zip",
					Archive = "triptourcoin-{0}-windows.zip",
					Executable = "triptourcoind.exe",
					Hash = "eb1a3c46f31c8d43757b8ad9435e754d685a1e8a"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "http://node1.triptourcoin.com/triptourcoin-{0}/triptourcoin-{0}-linux.zip",
					Archive = "Linux/bin/triptourcoin-{0}-linux.zip",
					Executable = "Linux/bin/triptourcoind",
					Hash = "299f8a8c2da5f0772bd8adc4f329d61bfd4dfca9"
				},
				UseSectionInConfigFile = true
			};
		}

		public static LBRYCreditsNodeDownloadData LBRYCredits
		{
			get; set;
		} = new LBRYCreditsNodeDownloadData();

		public static GoByteNodeDownloadData GoByte
		{
			get; set;
		} = new GoByteNodeDownloadData();

		public static ColossusNodeDownloadData Colossus
		{
			get; set;
		} = new ColossusNodeDownloadData();

		public static MonoeciNodeDownloadData Monoeci
		{
			get; set;
		} = new MonoeciNodeDownloadData();

		public static GroestlcoinNodeDownloadData Groestlcoin
		{
			get; set;
		} = new GroestlcoinNodeDownloadData();

		public static MogwaiNodeDownloadData Mogwai
		{
			get; set;
		} = new MogwaiNodeDownloadData();
		public static BitcoinNodeDownloadData Bitcoin
		{
			get; set;
		} = new BitcoinNodeDownloadData();

		public static LitecoinNodeDownloadData Litecoin
		{
			get; set;
		} = new LitecoinNodeDownloadData();

		public static ViacoinNodeDownloadData Viacoin
		{
			get; set;
		} = new ViacoinNodeDownloadData();

		public static BCashNodeDownloadData BCash
		{
			get; set;
		} = new BCashNodeDownloadData();

		public static FeathercoinNodeDownloadData Feathercoin
		{
			get; set;
		} = new FeathercoinNodeDownloadData();

		public static DogecoinNodeDownloadData Dogecoin
		{
			get; set;
		} = new DogecoinNodeDownloadData();

		public static DashNodeDownloadData Dash
		{
			get; set;
		} = new DashNodeDownloadData();

		public static TerracoinNodeDownloadData Terracoin
		{
			get; set;
		} = new TerracoinNodeDownloadData();

		public static BGoldNodeDownloadData BGold
		{
			get; set;
		} = new BGoldNodeDownloadData();

		public static PolisNodeDownloadData Polis
		{
			get; set;
		} = new PolisNodeDownloadData();

		public static MonacoinNodeDownloadData Monacoin
		{
			get; set;
		} = new MonacoinNodeDownloadData();

		public static UfoNodeDownloadData Ufo
		{
			get; set;
		} = new UfoNodeDownloadData();

		public static BitcoreNodeDownloadData Bitcore
		{
			get; set;
		} = new BitcoreNodeDownloadData();

		public static ZclassicNodeDownloadData Zclassic
		{
			get; set;
		} = new ZclassicNodeDownloadData();

		public static ElementsNodeDownloadData Elements
		{
			get; set;
		} = new ElementsNodeDownloadData();

		public static LiquidNodeDownloadData Liquid
		{
			get; set;
		} = new LiquidNodeDownloadData();

		public static GincoinNodeDownloadData Gincoin
		{
			get; set;
		} = new GincoinNodeDownloadData();

		public static KotoNodeDownloadData Koto
		{
			get; set;
		} = new KotoNodeDownloadData();

		public static BitcoinplusNodeDownloadData Bitcoinplus
		{
			get; set;
		} = new BitcoinplusNodeDownloadData();

		public static ChaincoinNodeDownloadData Chaincoin
		{
			get; set;
		} = new ChaincoinNodeDownloadData();

		public static StratisNodeDownloadData Stratis
		{
			get; set;
		} = new StratisNodeDownloadData();

		public static ZCoinNodeDownloadData ZCoin
		{
			get; set;
		} = new ZCoinNodeDownloadData();

		public static DogeCashNodeDownloadData DogeCash
		{
			get; set;
		} = new DogeCashNodeDownloadData();

		public static ArgoneumNodeDownloadData Argoneum
		{
			get; set;
		} = new ArgoneumNodeDownloadData();

		public static QtumNodeDownloadData Qtum
		{
			get; set;
		} = new QtumNodeDownloadData();

		public static MonetaryUnitNodeDownloadData MonetaryUnit
		{
			get; set;
		} = new MonetaryUnitNodeDownloadData();

		public static XDSNodeDownloadData XDS
		{
			get; set;
		} = new XDSNodeDownloadData();

		public static AlthashNodeDownloadData Althash
		{
			get; set;
		} = new AlthashNodeDownloadData();

		public static NeblioNodeDownloadData Neblio
		{
			get; set;
		} = new NeblioNodeDownloadData();

		public static TriptourcoinNodeDownloadData Triptourcoin
		{
			get; set;
		} = new TriptourcoinNodeDownloadData();

		public bool UseSectionInConfigFile { get; private set; }
		public string AdditionalRegtestConfig { get; private set; }
	}
}
