using System;

namespace NBitcoin.Tests
{
	public partial class NodeDownloadData
	{
		public bool SupportCookieFile { get; set; } = true;

		public class BitcoinNodeDownloadData
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
		}

		public class ViacoinNodeDownloadData
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
				},
				SupportCookieFile = false
			};
		}

		public class DashNodeDownloadData
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
		}

		public class DystemNodeDownloadData
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

		public class MogwaiNodeDownloadData
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

		public class BitcoreNodeDownloadData
		{
			public NodeDownloadData v0_15_2 = new NodeDownloadData()
			{
				Version = "0.15.2.0.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/LIMXTEC/BitCore/releases/download/{0}/windows.zip",
					Archive = "windows.zip",
					Executable = "bitcored.exe",
					Hash = "96b70ff0828af1a147c0be9326a941d541c6c82d96767d79378289d3e6a80b9a"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/LIMXTEC/BitCore/releases/download/{0}/linux.Ubuntu.16.04.LTS-static-libstdc.tar.gz",
					Archive = "linux.Ubuntu.16.04.LTS-static-libstdc.tar.gz",
					Executable = "bitcored",
					Hash = "b9092c1ad8e814b95f1d2199c535f24a02174af342399fe9b7f457d9d182f5a4"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/LIMXTEC/BitCore/releases/download/{0}/BitCore-Qt.dmg",
					Archive = "BitCore-Qt.dmg",
					Executable = "bitcored",
					Hash = "74efb6069278ef99fa361d70368a15da8cfc7bc92b33ead4af0b06277e16ef25"
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

        public class UfoNodeDownloadData
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
		{
			public NodeDownloadData v2_16_0 = new NodeDownloadData()
			{
				Version = "2.16.0",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Archive = "groestlcoin-{0}-x86_64-w64-mingw32.zip",
					Executable = "GRS-{0}\\groestlcoind.exe",
					Hash = "327aaee189255f2722736a426732a0f38fef90bae6495f42fd148138523c586c",
					CreateFolder = "GRS-{0}",
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "GRS-{0}/groestlcoind",
					Hash = "4e7683bbc6f3b7899761d1360f52a91f417e2b7e6c56b75b522d95b86ca46628",
					CreateFolder = "GRS-{0}",
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/Groestlcoin/groestlcoin/releases/download/v{0}/groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Archive = "groestlcoin-{0}-x86_64-apple-darwin11.tar.gz",
					Executable = "GRS-{0}/groestlcoind",
					Hash = "5ff6e5a509e0c69f4a832bd3c40a1a93f80a68bc5f55a0b5d517716fb123164e",
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
		}

		public class ZclassicNodeDownloadData
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
		}
		public class LiquidNodeDownloadData
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
		}
		public class MonoeciNodeDownloadData
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

		/// <summary>
		/// Using Stratis C# full node.
		/// Should be updated to use official release once it is deployed.
		/// </summary>
		public class StratisNodeDownloadData
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

		public class ParticlNodeDownloadData
		{
			public NodeDownloadData v0_17_1_4 = new NodeDownloadData()
			{
				Version = "0.17.1.4",
				Windows = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/particl/particl-core/releases/download/v{0}/particl-{0}-win64.zip",
					Archive = "particl-{0}-win64.zip",
					Executable = "particl-0.16.4/bin/particld.exe",
					Hash = "ed69ff8be8f4ce76d16f56b7643cd7b8f2b6c9590e15a164f99bc9f4b5d50e75"
				},
				Linux = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/particl/particl-core/releases/download/v{0}/particl-{0}-x86_64-linux-gnu.tar.gz",
					Archive = "particl-{0}-x86_64-linux-gnu.tar.gz",
					Executable = "particl-{0}/bin/particld",
					Hash = "ad198188c9350520d9408ccfc0eaa3c5f8c1f99574d78aac2dff0fce0b9bdadd"
				},
				Mac = new NodeOSDownloadData()
				{
					DownloadLink = "https://github.com/particl/particl-core/releases/download/v{0}/particl-{0}-osx64.tar.gz",
					Archive = "particl-{0}-osx64.tar.gz",
					Executable = "particl-{0}/bin/particld",
					Hash = "c16385df1698e241b518d03511a65f1567b7edf337265c38ad598c097bd3f2f4"
				},
				UseSectionInConfigFile = true,
			};
		}

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

		public static ParticlNodeDownloadData Particl
		{
			get; set;
		} = new ParticlNodeDownloadData();

		public bool UseSectionInConfigFile { get; private set; }
		public string AdditionalRegtestConfig { get; private set; }
	}
}
