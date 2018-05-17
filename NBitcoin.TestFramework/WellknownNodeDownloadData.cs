using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Tests
{
	public partial class NodeDownloadData
	{
		public class BitcoinNodeDownloadData
		{
			public NodeDownloadData v0_13_1 = new NodeDownloadData()
			{
				Version = "0.13.1",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoin.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "2293de5682375b8edfde612d9e152b42344d25d3852663ba36f7f472b27954a4"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoin.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "499be4f48c933d92c43468ee2853dddaba4af7e1a17f767a85023b69a21b6e77"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoin.org/bin/bitcoin-core-{0}/bitcoin-{0}-win32.zip",
					Archive = "bitcoin-{0}-win32.zip",
					Hash = "fcf6089fc013b175e3c5e32580afb3cb4310c62d2e133e992b8a9d2e0cbbafaa"
				}
			};

			public NodeDownloadData v0_16_0 = new NodeDownloadData()
			{
				Version = "0.16.0",
				Linux = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					DownloadLink = "https://bitcoin.org/bin/bitcoin-core-{0}/bitcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "e6322c69bcc974a29e6a715e0ecb8799d2d21691d683eeb8fef65fc5f6a66477"
				},
				Mac = new NodeOSDownloadData()
				{
					Archive = "bitcoin-{0}-osx64.tar.gz",
					DownloadLink = "https://bitcoin.org/bin/bitcoin-core-{0}/bitcoin-{0}-osx64.tar.gz",
					Executable = "bitcoin-{0}/bin/bitcoind",
					Hash = "ade85a8e39de8c36a134721c3da9853a80f29a8625048e0c2a5295ca8b23a88c"
				},
				Windows = new NodeOSDownloadData()
				{
					Executable = "bitcoin-{0}/bin/bitcoind.exe",
					DownloadLink = "https://bitcoin.org/bin/bitcoin-core-{0}/bitcoin-{0}-win32.zip",
					Archive = "bitcoin-{0}-win32.zip",
					Hash = "60d65d6e57f42164e1c04bb5bb65156d87f0433825a1c1f1f5f6aebf5c8df424"
				}
			};
		}

		public class LitecoinNodeDownloadData
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
		}

		public class ViacoinNodeDownloadData
		{
			public NodeDownloadData v0_15_1 = new NodeDownloadData()
			{
				Version = "0.15.1",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/viacoin/viacoin/releases/download/v{0}/viacoin-{0}-win64.zip",
					Archive = "viacoin-{0}-win64.zip",
					Executable = "viacoin-{0}/bin/viacoind.exe",
					Hash = "408d270db88e345fb5d4e93b5ec0f7761c676e4d795458ebaffce6de6cde65af"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/viacoin/viacoin/releases/download/v{0}.0/viacoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "viacoin-{0}-linux64.tar.gz",
					Executable = "viacoin-{0}/bin/viacoind",
					Hash = "673bfd17194ca4fe8408450e1871447d461ce26925e71ea55eebd89c379f5775"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/viacoin/viacoin/releases/download/v{0}.0/viacoin-{0}-osx-unsigned.dmg",
					Archive = "viacoin-{0}-osx64.tar.gz",
					Executable = "viacoin-{0}/bin/viacoind",
					Hash = "673bfd17194ca4fe8408450e1871447d461ce26925e71ea55eebd89c379f5775"
				}
			};
		}

		public class BCashNodeDownloadData
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
		}

		public class FeathercoinNodeDownloadData
		{
			public NodeDownloadData v0_16_0 = new NodeDownloadData()
			{
				Version = "0.16.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/FeatherCoin/Feathercoin/releases/download/v{0}.0/feathercoin-{0}-win-setup.exe",
					Archive = "feathercoin-{0}-win64.zip",
					Executable = "feathercoin-{0}/bin/feathercoind.exe",
					Hash = "7eb76875e38bf3c2ed35afe06d2b133780b935b81a285f8de5522ebb6e99523c"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/FeatherCoin/Feathercoin/releases/download/v{0}.0/feathercoin-{0}-linux64.tar.gz",
					Archive = "feathercoin-{0}-linux64.tar.gz",
					Executable = "feathercoin-{0}/bin/feathercoind",
					Hash = "a24ec110cc45c935028f64198e054e1a7b096caf7671614f288f38ec516e1fd9"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/FeatherCoin/Feathercoin/releases/download/v{0}.0/feathercoin-{0}-mac.dmg",
					Archive = "feathercoin-{0}-osx64.tar.gz",
					Executable = "feathercoin-{0}/bin/feathercoind",
					Hash = "19d243507d8e1ad5de22b82363f5fad069037f9b419f7c01ed56af5150060737"
				}
			};
		}

		public class DogecoinNodeDownloadData
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
				}
			};
		}

		public class DashNodeDownloadData
		{
			public NodeDownloadData v0_12_2 = new NodeDownloadData()
			{
				Version = "0.12.2.3",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-win64.zip",
					Archive = "dashcore-{0}-win64.zip",
					Executable = "dashcore-0.12.2/bin/dashd.exe",
					Hash = "04e95d11443d785ad9d98b04fd2313ca96d937e424be80f639b73846304d154c"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-linux64.tar.gz",
					Archive = "dashcore-{0}-linux64.tar.gz",
					Executable = "dashcore-0.12.2/bin/dashd",
					Hash = "8b7c72197f87be1f5d988c274cac06f6539ddb4591a578bfb852a412022378f2"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/dashpay/dash/releases/download/v{0}/dashcore-{0}-osx.dmg",
					Archive = "dashcore-{0}-osx.dmg",
					Executable = "dashcore-0.12.2/bin/dashd",
					Hash = "90ca27d6733df6fc69b0fc8220f2315623fe5b0cbd1fe31f247684d51808cb81"
				}
			};
		}
		
		public class BGoldNodeDownloadData
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

		public class PolisNodeDownloadData
		{
			public NodeDownloadData v1_3_0 = new NodeDownloadData()
			{
				Version = "1.3.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/polispay/polis/releases/download/v{0}/poliscore-{0}-win64.zip",
					Archive = "poliscore-{0}-win64.zip",
					Executable = "poliscore-1.3.0/bin/polisd.exe",
					Hash = "eec3d9b0c721d690139bc9ac11344ba370245c4ade5d6ec6750eda27493b2390"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/polispay/polis/releases/download/v{0}/poliscore-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "poliscore-{0}-linux64.tar.gz",
					Executable = "poliscore-1.3.0/bin/polisd",
					Hash = "50c3599645fbcfdfa35f4704ed742bbb5fa1ca432067f9b2368deea9784ec771"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/polispay/polis/releases/download/v{0}/poliscore-{0}-osx.dmg",
					Archive = "poliscore-{0}-osx.dmg",
					Executable = "poliscore-1.3.0/bin/polisd",
					Hash = "2d67048a8e51d6c1384752cfde6a3562b1b1ba250fce28020e8afe894a9b5afe"
				}
			};
		}

		public class MonacoinNodeDownloadData
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
	}
}
