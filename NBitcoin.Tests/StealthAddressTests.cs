using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Stealth;
using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//https://en.bitcoin.it/wiki/Sx/Stealth
	public class StealthAddressTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://wiki.unsystem.net/index.php/DarkWallet/Stealth#Bitfield_value
		public void BitFieldCorrectlyInterpreted()
		{

			var tests = new[]
			{
				new
				{
					Encoded = (uint)1763505291,
					BitCount = 32,
					Raw = "8bf41c69"
				},
				new
				{
					Encoded = (uint)1,
					BitCount = 8,
					Raw = "0100"
				},
				new
				{
					Encoded = (uint)1,
					BitCount = 7,
					Raw = "01"
				}
			};

			foreach(var test in tests)
			{
				var bitField = new BitField(test.Encoded, test.BitCount);
				Assert.Equal(TestUtils.ParseHex(test.Raw), bitField.GetRawForm());
				Assert.Equal(test.Encoded, bitField.GetEncodedForm());

				bitField = new BitField(TestUtils.ParseHex(test.Raw), test.BitCount);
				Assert.Equal(test.Encoded, bitField.GetEncodedForm());
				Assert.Equal(TestUtils.ParseHex(test.Raw), bitField.GetRawForm());
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://github.com/libbitcoin/libbitcoin/blob/master/test/stealth.cpp
		public void BitFieldCorrectlyMatchData()
		{
			var bit = (uint)1358086238;
			var o = new BitField(bit, 32);
			var tests = new[]
			{
				new
				{
					Encoded = (uint)0x691cf48b,
					BitCount = 27,
					Data = "8bf42c79",
					Match = false
				},
				new
				{
					Encoded = (uint)0x691cf48b,
					BitCount = 27,
					//Data miss match (1 instead of 2)
					Data = "8bf41c79",
					Match = true
				}
			};
			foreach(var test in tests)
			{
				BitField field = new BitField(test.Encoded, test.BitCount);
				Assert.Equal(test.Match, field.Match(Utils.ToUInt32(TestUtils.ParseHex(test.Data), true)));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://github.com/libbitcoin/libbitcoin/blob/master/test/stealth.cpp
		public void BitFieldCanFetchTransaction()
		{
			var tests = new[]
			{
				new
				{
					Encoded = "deadbeef",
					BitCount = 5,
					Transaction = "0100000001bd365d65c6eeee2d3ae29c6e1b0fb22f70f0135220eac1273c92a13b6039ecdf000000006b48304502202551626a52a088ea585f2aaa8afe1c1b7bc52ea8e577e149f20f0f5f2fc54485022100913643c6a1e54b5284d1c108519d9243213373256ff95ab6c7e3d162175c9e28012102881c1427e826f230246197f7f693f1c923ad2afea1dad901d1c870f7e310c895ffffffff020000000000000000286a2606ab3099ae0359222b4a2dd9a21ebd70331794fb05b0bd605bd9660fdf895906b1a227edcbec306c3402000000001976a9142f62432f367dc3cf67e68f814cc2a3ed5b2e8cec88ac00000000",
				}
				,new
				{
					Encoded = "deadbeef",
					BitCount = 5,
					Transaction = "01000000028d8d968818e464db9dc876eb4edd59e62913d849603ba10afe033159b55652dc010000006b483045022100992e7ce4516cdabffae823557f5854c6ca7a424cc2fe7a9b94f439ecca839f84022025b871cf499f7b0eebcf751a83e7ffe9473b6c5227f95d9989ef8e98937112760121038795ff1d07ef7092e03d612f187d1281705b36d0c11ecf4b11167beb7efc3437ffffffff723372d920d8eaa6bd1fe6ec5adaf913b6d8a48655acded49a3f09191cdf9f3a010000006a47304402206f6ca054242fe5b410d743ae238049799eccdd88637d555ad48d8da1a854bd0802201b13217d0a6a7cf3467074aa8964f3f365541304142503d91813baa0117d1a8101210258cedc6e590493838c612a394d53ffbb283638f9d1d49e1c571b15a885f405d9ffffffff05f0874b00000000001976a914cd08dcd94ba88488b4c02c82fc11fc63fee4e30c88acc0e1e400000000001976a9148784cbfe99e86135d40d276c58fcbbf37af1b48e88ac0000000000000000286a260689d0267d021cc402cf764f2c88138028b6d5c0f06d5caa34edffc1d2286048f0162756ebf1f0874b00000000001976a914923d77bbdd3d29dd4844a51629514bac9e8a2c4d88aca30c0000000000001976a9143aaa9db7a3fbc5dcc9d121a157bf63aaaee9d8f588ac00000000",
				},
				new
				{
					Encoded = "deadbeef",
					BitCount = 5,
					Transaction = "010000000288b6952f8357b5a8271f6c0608ea779549bf0a9ae72213ce433f7a9c90190be6000000006b4830450221008b1b7369d7bf8a1dd99e6a260ef969840d633ba81fc379bf4f5469afd3b288e2022077e21dd12c1d8b2a4ab2e58fb0113b9e3bb77db4439483f7a8311cac3ad6ce9a012102b0c2eb0cc505a4c9fb62df8a7fcdcc00ddab43ff5752ebc51a0dae11fbfb0648ffffffff4ca2f8960e17b56e690739158994ed23427ffc08445ba3223edacb322db12d4b000000006b483045022100f94d50d846d85a545693574f9e1a680a858f085257c827cd8b211df8d27c558202201bca0407ad67960937999a88c95e52417442703a16036bbe55f071cdff804f69012102884d0c845bff3158e88033bce236cdbfeadba0a7e7381258379612f355681d6dffffffff052f750000000000001976a914a34a179dc97cbb84992d2960ee240b56f1b2ae3088acbca15400000000001976a914139e314f94a8e4fc2864ef2544e7640ba033725b88ac0000000000000000286a26061d6b629c024c7daf682da4afd61aadf401fae316a265c3244d912b375a75b55551d52a583f2f750000000000001976a9142888cb9f0b0f489106bc13f56fdd2ceec1b6883788ac51c30000000000001976a91404316e7db781481d1419feda2c8d7fa9f7b68a6e88ac00000000",
				},

			};



			foreach(var test in tests)
			{
				var field = new BitField(TestUtils.ParseHex(test.Encoded), test.BitCount);
				Transaction transaction = new Transaction();
				transaction.FromBytes(TestUtils.ParseHex(test.Transaction));

				var stealthOutput = field.GetPayments(transaction).FirstOrDefault();
				Assert.NotNull(stealthOutput);

				Assert.True(field.Match(stealthOutput.Metadata.BitField));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseStealthAddress()
		{
			var tests = new[]
			{ 
				//Test vector created with sx
				//sx stealth-newkey -> ScanSecret,SpendSecret,StealthAddress
				//sx stealth-show-addr StealthAddress -> ScanPubKey,SpendPubKey,RequiredSignature...
				new
				{
					ScanSecret = "9ac9fdee7c2c19611bcbed8959e1c61d00cdc27cf17bb50a1f4d29db7f953632",
					SpendSecrets = new[]{
											"4e2fa767cc241c3fa4c512d572b2758a3960a06d374f2c819fe409b161d72ad4"
										},
					StealthAddress = "vJmsmwE8cVt9ytJxBuY2jayh8RAfvpG42CyNVYpeVZAkHaiwASobUEzskpXMwbH1TZNBLoxWWYem5WuZewTL8xz3upJ75zKcdVmTfg",
					ScanPubKey = "021ce89be99a229d123e8bc13ffbcb66722d6200bbeb1d90ddddbf97df82ed2672",
					SpendPubKeys = new[]
										{
											"03c197525241d3d70bbf33bb2b54d41e6b9595a92a2c6b7bf7157727c017f0154a"
										},
					RequiredSignature = 1,
					Options = 0,
					PrefixLength = 0,
					PrefixValue = "",
				}
			};
			foreach(var test in tests)
			{
				var scanSecret = new Key(TestUtils.ParseHex(test.ScanSecret));
				AssertEx.CollectionEquals(scanSecret.PubKey.ToBytes(), TestUtils.ParseHex(test.ScanPubKey));

				var stealth = new BitcoinStealthAddress(test.StealthAddress, Network.Main);
				Assert.Equal(test.RequiredSignature, stealth.SignatureCount);
				Assert.Equal(test.PrefixLength, stealth.Prefix.BitCount);
				AssertEx.CollectionEquals(stealth.Prefix.GetRawForm(), TestUtils.ParseHex(test.PrefixValue));
				Assert.Equal(test.Options, stealth.Options);

				AssertEx.CollectionEquals(stealth.ScanPubKey.ToBytes(),
											  TestUtils.ParseHex(test.ScanPubKey));
				for(int i = 0; i < test.SpendPubKeys.Length; i++)
				{
					AssertEx.CollectionEquals(stealth.SpendPubKeys[i].ToBytes(),
											  TestUtils.ParseHex(test.SpendPubKeys[i]));

					var spendSecret = new Key(TestUtils.ParseHex(test.SpendSecrets[i]));
					AssertEx.CollectionEquals(stealth.SpendPubKeys[i].ToBytes(), TestUtils.ParseHex(test.SpendPubKeys[i]));
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreatePayment()
		{
			var tests = new[]
			{
				new CanCreatePaymentData
				{
					//sx stealth-newkey
					StealthAddress = "vJmtjxSDxNPXL4RNapp9ARdqKz3uJyf1EDGjr1Fgqs9c8mYsVH82h8wvnA4i5rtJ57mr3kor1EVJrd4e5upACJd588xe52yXtzumxj",

					ScanSecret = "3e49e7257cb31db997edb1cf8299af0f37e2663e2260e4b8033e49d39a6d02f2",
					ScanPubKey = "025e58a31122b38c86abc119b9379fe247410aee87a533f9c07b189aef6c3c1f52",

					SpendSecret = "aa3db0cfb3edc94de4d10f873f8190843f2a17484f6021a95a7742302c744748",
					SpendPubKey = "03616562c98e7d7b74be409a787cec3a912122f3fb331a9bee9b0b73ce7b9f50af",

					//sx newkey | sx wif-to-secret
					EphemSecret = "9e63abaf8dcd5ea3919e6de0b6c544e00bf51bf92496113a01d6e369944dc091",
					EphemPubKey = "03403d306ec35238384c7e340393335f9bc9bb4a2e574eb4e419452c4ea19f14b0",

					//sx steatlh-uncover-secret [EphemPubKey] [ScanSecret] [SpendSecret] 
					StealthSecret = "4e422fb1e5e1db6c1f6ab32a7706d368ceb385e7fab098e633c5c5949c3b97cd",
					//sx stealth-initiate [EphemSecret] [ScanPubKey] [SpendPubKey] (for sender)
					//or 
					//sx stealth-uncover [EphemPubKey] [ScanSecret] [SpendPubKey]  (for receiver)
					StealthPubKey = "02726112ad39cb6bf848b1b1ef30b88e35286bf99f746c2be575f96c0e02a9357c",
				},

				//Need padding for to find the stealth secret
				new CanCreatePaymentData{
					StealthAddress = "vJmyTEybwCKz7W8y6vP62jo7RoyfLneiANcPLBBNYwn98EXzQRStMKqKGRiZhqscuQ6WKy2J3U3zfx72V3b2J6YvxxBcxUj4XMDsw7",
					ScanSecret = "2f517d81cf30e47dbf4809321275bbfd92192af81a6141a17aa53e40bd28fe36",
					ScanPubKey = "039d91ae0eebea6dc500fb57b704abce3d3fa700cc762a52bc5dcaee27770a8402",
					SpendSecret = "71e33219884fc27011f8da9adcc730f0c2e940759bdb1b615764492bce04fcea",
					SpendPubKey = "021a3d5b40ec83fc58b5a23207eb9c99b741d8f0e9f8b80f04f49cec915b540c40",
					EphemSecret = "578ffe42c0fbfb324a31f41dbbcd8b1f910ce2f4d803444a83b18ae9f8ccd97e",
					EphemPubKey = "03c190be0a1c6e50577b3dd637b1fff9344de31c2544ff3d815535c0515711150f",
					StealthSecret = "006d138b4bcef0f09c8784c0cc68f2be4497a1a822d8d7b0519c5c0378b5cb45",
					StealthPubKey = "0223a99278a5279ea93718503a42377067e72960eb808d8bff6defdd95d4feff76"
				}
			};

			foreach(var test in tests)
			{
				var scan = AssertKeys(test.ScanSecret, test.ScanPubKey);
				var spend = AssertKeys(test.SpendSecret, test.SpendPubKey);
				var ephem = AssertKeys(test.EphemSecret, test.EphemPubKey);
				var stealth = AssertKeys(test.StealthSecret, test.StealthPubKey);

				var address = spend.PubKey.CreateStealthAddress(scan.PubKey, Network.Main);
				Assert.Equal(test.StealthAddress, address.ToString());
				//Try roundtrip
				address = new BitcoinStealthAddress(address.ToBytes(), Network.Main);
				Assert.Equal(test.StealthAddress, address.ToString());

				var payment = address.CreatePayment(ephem);
				var generatedKey = spend.Uncover(scan, payment.Metadata.EphemKey);
				if(stealth != null)
				{
					Assert.Equal(stealth.PubKey.Hash, payment.StealthKeys[0].ID);
					Assert.Equal(stealth.ToBytes(), generatedKey.ToBytes());
				}
				var uncoveredSender = spend.PubKey.UncoverSender(ephem, scan.PubKey);
				var uncovertedReceiver = spend.PubKey.UncoverReceiver(scan, ephem.PubKey);

				AssertEx.CollectionEquals(uncoveredSender.ToBytes(), uncovertedReceiver.ToBytes());
				AssertEx.CollectionEquals(generatedKey.PubKey.ToBytes(), uncovertedReceiver.ToBytes());

				var transaction = new Transaction();
				payment.AddToTransaction(transaction, 100);
			}
		}

		private Key AssertKeys(string key, string pub)
		{
			if(key == null)
				return null;
			Key k = new Key(TestUtils.ParseHex(key));
			if(pub != null)
			{
				PubKey p = new PubKey(TestUtils.ParseHex(pub));
				AssertEx.Equal(k.PubKey.ToBytes(), p.ToBytes());
			}
			return k;
		}

	}

	public class CanCreatePaymentData
	{
		public string StealthAddress
		{
			get;
			set;
		}

		public string ScanSecret
		{
			get;
			set;
		}

		public string ScanPubKey
		{
			get;
			set;
		}

		public string SpendSecret
		{
			get;
			set;
		}

		public string SpendPubKey
		{
			get;
			set;
		}

		public string EphemSecret
		{
			get;
			set;
		}

		public string EphemPubKey
		{
			get;
			set;
		}

		public string StealthPubKey
		{
			get;
			set;
		}

		public string StealthSecret
		{
			get;
			set;
		}

		internal static IEnumerable<CanCreatePaymentData> GenerateRandoms(int count)
		{
			for(int i = 0; i < count; i++)
			{
				CanCreatePaymentData data = new CanCreatePaymentData();
				var spend = new Key();
				var scan = new Key();
				var ephem = new Key();
				data.SpendSecret = ToString(spend);
				data.SpendPubKey = ToString(spend.PubKey);
				data.ScanSecret = ToString(scan);
				data.ScanPubKey = ToString(scan.PubKey);
				data.EphemSecret = ToString(ephem);
				data.EphemPubKey = ToString(ephem.PubKey);
				data.StealthAddress = spend.PubKey.CreateStealthAddress(scan.PubKey, Network.Main).ToString();
				yield return data;
			}
		}

		private static string ToString(IBitcoinSerializable spend)
		{
			return Encoders.Hex.EncodeData(spend.ToBytes());
		}

		public override string ToString()
		{
			return
					"StealthAddress = \"" + StealthAddress + "\",\r\n" +

					"ScanSecret = \"" + ScanSecret + "\",\r\n" +
					"ScanPubKey = \"" + ScanPubKey + "\",\r\n" +

					"SpendSecret = \"" + SpendSecret + "\",\r\n" +
					"SpendPubKey = \"" + SpendPubKey + "\",\r\n" +

					"EphemSecret = \"" + EphemSecret + "\",\r\n" +
					"EphemPubKey = \"" + EphemPubKey + "\",\r\n" +

					"StealthSecret = \"" + StealthSecret + "\",\r\n" +
					"StealthPubKey = \"" + StealthPubKey + "\",\r\n";
		}
	}
}
