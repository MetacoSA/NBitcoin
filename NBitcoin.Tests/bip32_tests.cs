using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	public class bip32_tests
	{


		class TestDerivation
		{
			public string pub;
			public string prv;
			public uint nChild;
		};

		class TestVector
		{
			public string strHexMaster;
			public List<TestDerivation> vDerive = new List<TestDerivation>();

			public TestVector(string strHexMasterIn)
			{
				strHexMaster = strHexMasterIn;
			}

			public TestVector Add(string pub, string prv, uint nChild)
			{
				vDerive.Add(new TestDerivation());
				TestDerivation der = vDerive.Last();
				der.pub = pub;
				der.prv = prv;
				der.nChild = nChild;
				return this;
			}
		};

		TestVector test1 =
		  new TestVector("000102030405060708090a0b0c0d0e0f")
			.Add("xpub661MyMwAqRbcFtXgS5sYJABqqG9YLmC4Q1Rdap9gSE8NqtwybGhePY2gZ29ESFjqJoCu1Rupje8YtGqsefD265TMg7usUDFdp6W1EGMcet8",
			 "xprv9s21ZrQH143K3QTDL4LXw2F7HEK3wJUD2nW2nRk4stbPy6cq3jPPqjiChkVvvNKmPGJxWUtg6LnF5kejMRNNU3TGtRBeJgk33yuGBxrMPHi",
			 0x80000000)
			.Add("xpub68Gmy5EdvgibQVfPdqkBBCHxA5htiqg55crXYuXoQRKfDBFA1WEjWgP6LHhwBZeNK1VTsfTFUHCdrfp1bgwQ9xv5ski8PX9rL2dZXvgGDnw",
			 "xprv9uHRZZhk6KAJC1avXpDAp4MDc3sQKNxDiPvvkX8Br5ngLNv1TxvUxt4cV1rGL5hj6KCesnDYUhd7oWgT11eZG7XnxHrnYeSvkzY7d2bhkJ7",
			 1)
			.Add("xpub6ASuArnXKPbfEwhqN6e3mwBcDTgzisQN1wXN9BJcM47sSikHjJf3UFHKkNAWbWMiGj7Wf5uMash7SyYq527Hqck2AxYysAA7xmALppuCkwQ",
			 "xprv9wTYmMFdV23N2TdNG573QoEsfRrWKQgWeibmLntzniatZvR9BmLnvSxqu53Kw1UmYPxLgboyZQaXwTCg8MSY3H2EU4pWcQDnRnrVA1xe8fs",
			 0x80000002)
			.Add("xpub6D4BDPcP2GT577Vvch3R8wDkScZWzQzMMUm3PWbmWvVJrZwQY4VUNgqFJPMM3No2dFDFGTsxxpG5uJh7n7epu4trkrX7x7DogT5Uv6fcLW5",
			 "xprv9z4pot5VBttmtdRTWfWQmoH1taj2axGVzFqSb8C9xaxKymcFzXBDptWmT7FwuEzG3ryjH4ktypQSAewRiNMjANTtpgP4mLTj34bhnZX7UiM",
			 2)
			.Add("xpub6FHa3pjLCk84BayeJxFW2SP4XRrFd1JYnxeLeU8EqN3vDfZmbqBqaGJAyiLjTAwm6ZLRQUMv1ZACTj37sR62cfN7fe5JnJ7dh8zL4fiyLHV",
			 "xprvA2JDeKCSNNZky6uBCviVfJSKyQ1mDYahRjijr5idH2WwLsEd4Hsb2Tyh8RfQMuPh7f7RtyzTtdrbdqqsunu5Mm3wDvUAKRHSC34sJ7in334",
			 1000000000)
			.Add("xpub6H1LXWLaKsWFhvm6RVpEL9P4KfRZSW7abD2ttkWP3SSQvnyA8FSVqNTEcYFgJS2UaFcxupHiYkro49S8yGasTvXEYBVPamhGW6cFJodrTHy",
			 "xprvA41z7zogVVwxVSgdKUHDy1SKmdb533PjDz7J6N6mV6uS3ze1ai8FHa8kmHScGpWmj4WggLyQjgPie1rFSruoUihUZREPSL39UNdE3BBDu76",
			 0);

		TestVector test2 =
		  new TestVector("fffcf9f6f3f0edeae7e4e1dedbd8d5d2cfccc9c6c3c0bdbab7b4b1aeaba8a5a29f9c999693908d8a8784817e7b7875726f6c696663605d5a5754514e4b484542")
			.Add("xpub661MyMwAqRbcFW31YEwpkMuc5THy2PSt5bDMsktWQcFF8syAmRUapSCGu8ED9W6oDMSgv6Zz8idoc4a6mr8BDzTJY47LJhkJ8UB7WEGuduB",
			 "xprv9s21ZrQH143K31xYSDQpPDxsXRTUcvj2iNHm5NUtrGiGG5e2DtALGdso3pGz6ssrdK4PFmM8NSpSBHNqPqm55Qn3LqFtT2emdEXVYsCzC2U",
			 0)
			.Add("xpub69H7F5d8KSRgmmdJg2KhpAK8SR3DjMwAdkxj3ZuxV27CprR9LgpeyGmXUbC6wb7ERfvrnKZjXoUmmDznezpbZb7ap6r1D3tgFxHmwMkQTPH",
			 "xprv9vHkqa6EV4sPZHYqZznhT2NPtPCjKuDKGY38FBWLvgaDx45zo9WQRUT3dKYnjwih2yJD9mkrocEZXo1ex8G81dwSM1fwqWpWkeS3v86pgKt",
			 0xFFFFFFFF)
			.Add("xpub6ASAVgeehLbnwdqV6UKMHVzgqAG8Gr6riv3Fxxpj8ksbH9ebxaEyBLZ85ySDhKiLDBrQSARLq1uNRts8RuJiHjaDMBU4Zn9h8LZNnBC5y4a",
			 "xprv9wSp6B7kry3Vj9m1zSnLvN3xH8RdsPP1Mh7fAaR7aRLcQMKTR2vidYEeEg2mUCTAwCd6vnxVrcjfy2kRgVsFawNzmjuHc2YmYRmagcEPdU9",
			 1)
			.Add("xpub6DF8uhdarytz3FWdA8TvFSvvAh8dP3283MY7p2V4SeE2wyWmG5mg5EwVvmdMVCQcoNJxGoWaU9DCWh89LojfZ537wTfunKau47EL2dhHKon",
			 "xprv9zFnWC6h2cLgpmSA46vutJzBcfJ8yaJGg8cX1e5StJh45BBciYTRXSd25UEPVuesF9yog62tGAQtHjXajPPdbRCHuWS6T8XA2ECKADdw4Ef",
			 0xFFFFFFFE)
			.Add("xpub6ERApfZwUNrhLCkDtcHTcxd75RbzS1ed54G1LkBUHQVHQKqhMkhgbmJbZRkrgZw4koxb5JaHWkY4ALHY2grBGRjaDMzQLcgJvLJuZZvRcEL",
			 "xprvA1RpRA33e1JQ7ifknakTFpgNXPmW2YvmhqLQYMmrj4xJXXWYpDPS3xz7iAxn8L39njGVyuoseXzU6rcxFLJ8HFsTjSyQbLYnMpCqE2VbFWc",
			 2)
			.Add("xpub6FnCn6nSzZAw5Tw7cgR9bi15UV96gLZhjDstkXXxvCLsUXBGXPdSnLFbdpq8p9HmGsApME5hQTZ3emM2rnY5agb9rXpVGyy3bdW6EEgAtqt",
			 "xprvA2nrNbFZABcdryreWet9Ea4LvTJcGsqrMzxHx98MMrotbir7yrKCEXw7nadnHM8Dq38EGfSh6dqA9QWTyefMLEcBYJUuekgW4BYPJcr9E7j",
			 0);

		[Fact]
		[Trait("Core", "Core")]
		public void bip32_test1()
		{
			RunTest(test1);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void bip32_test2()
		{
			RunTest(test2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CheckBIP32Constructors()
		{
			var key = new ExtKey();
			Assert.Equal(key.GetWif(Network.Main), new ExtKey(key.PrivateKey, key.ChainCode).GetWif(Network.Main));
			Assert.Equal(key.Neuter().GetWif(Network.Main), new ExtPubKey(key.PrivateKey.PubKey, key.ChainCode).GetWif(Network.Main));

			key = key.Derive(1);
			Assert.Equal(key.GetWif(Network.Main), new ExtKey(key.PrivateKey, key.ChainCode, key.Depth, key.ParentFingerprint, key.Child).GetWif(Network.Main));
			Assert.Equal(key.Neuter().GetWif(Network.Main), new ExtPubKey(key.PrivateKey.PubKey, key.ChainCode, key.Depth, key.ParentFingerprint, key.Child).GetWif(Network.Main));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRecoverExtKeyFromExtPubKeyAndOneChildExtKey()
		{
			ExtKey key = ExtKey.Parse("xprv9s21ZrQH143K3Z9EwCXrA5VbypnvWGiE9z22S1cLLPi7r8DVUkTabBvMjeirS8KCyppw24KoD4sFmja8UDU4VL32SBdip78LY6sz3X2GPju", Network.Main)
				.Derive(1);
			var pubkey = key.Neuter();
			var childKey = key.Derive(1);

			ExtKey recovered = childKey.GetParentExtKey(pubkey);
			Assert.Equal(recovered.ToString(Network.Main), key.ToString(Network.Main));

			childKey = key.Derive(1, true);
			Assert.Throws<InvalidOperationException>(() => childKey.GetParentExtKey(pubkey));

			childKey = key.Derive(1).Derive(1);
			Assert.Throws<ArgumentException>(() => childKey.GetParentExtKey(pubkey));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRecoverExtKeyFromExtPubKeyAndOneChildExtKey2()
		{
			for (int i = 0; i < 255; i++)
			{
				ExtKey key = new ExtKey().Derive((uint)i);
				var childKey = key.Derive((uint)i);
				var pubKey = key.Neuter();
				ExtKey recovered = childKey.GetParentExtKey(pubKey);
				Assert.Equal(recovered.ToString(Network.Main), key.ToString(Network.Main));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRecoverExtKeyFromExtPubKeyAndSecret()
		{
			ExtKey key = new ExtKey().Derive(1);
			var underlying = key.PrivateKey.GetBitcoinSecret(Network.Main);
			var pubKey = key.Neuter().GetWif(Network.Main);
			ExtKey key2 = new ExtKey(pubKey, underlying);
			Assert.Equal(key.ToString(Network.Main), key2.ToString(Network.Main));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUseKeyPath()
		{
			var keyPath = KeyPath.Parse("0/1/2/3");
			Assert.Equal("0/1/2/3", keyPath.ToString());
			var key = new ExtKey();
			Assert.Equal(key
							.Derive(0)
							.Derive(1)
							.Derive(2)
							.Derive(3)
							.ToString(Network.Main), key.Derive(keyPath).ToString(Network.Main));

			var neuter = key.Neuter();
			Assert.Equal(neuter
							.Derive(0)
							.Derive(1)
							.Derive(2)
							.Derive(3)
							.ToString(Network.Main), neuter.Derive(keyPath).ToString(Network.Main));

			Assert.Equal(neuter.Derive(keyPath).ToString(Network.Main), key.Derive(keyPath).Neuter().ToString(Network.Main));

			keyPath = new KeyPath(new uint[] { 0x8000002Cu, 1u });
			Assert.Equal("44'/1", keyPath.ToString());

			keyPath = KeyPath.Parse("44'/1");
			Assert.False(keyPath.IsHardened);
			Assert.True(KeyPath.Parse("44'/1'").IsHardened);
			Assert.Equal(0x8000002Cu, keyPath[0]);
			Assert.Equal(1u, keyPath[1]);

			key = new ExtKey();
			Assert.Equal(key.Derive(keyPath).ToString(Network.Main), key.Derive(44, true).Derive(1, false).ToString(Network.Main));

			keyPath = KeyPath.Parse("");
			keyPath = keyPath.Derive(44, true).Derive(1, false);
			Assert.Equal("44'/1", keyPath.ToString());
			Assert.Equal("44'/2", keyPath.Increment().ToString());
			Assert.Equal("44'/1/2'", keyPath.Derive(1, true).Increment().ToString());
			Assert.Equal("44'", keyPath.Parent.ToString());
			Assert.Equal("", keyPath.Parent.Parent.ToString());
			Assert.Null(keyPath.Parent.Parent.Parent);
			Assert.Throws<InvalidOperationException>(() => keyPath.Parent.Parent.Increment());
			Assert.Equal(key.Derive(keyPath).ToString(Network.Main), key.Derive(44, true).Derive(1, false).ToString(Network.Main));

			Assert.True(key.Derive(44, true).IsHardened);
			Assert.False(key.Derive(44, false).IsHardened);

			neuter = key.Derive(44, true).Neuter();
			Assert.True(neuter.IsHardened);
			neuter = key.Derive(44, false).Neuter();
			Assert.False(neuter.IsHardened);

			KeyPath a = new KeyPath("3/2");
			KeyPath b = new KeyPath("5/6");
			Assert.Equal(new KeyPath("3/2/5/6"), a + b);
			b = null;
			Assert.Equal(a, a + b);
			a = null;
			Assert.Null(a + b);
			b = new KeyPath("5/6");
			Assert.Equal(new KeyPath("5/6"), a + b);
			a += new KeyPath("3/2");
			a += new KeyPath("5/6");
			Assert.Equal(new KeyPath("3/2/5/6"), a);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanInstantiateFromRawBytes()
		{
			var data = new byte[] { 0x01, 0x00, 0x00, 0x80 };
			var keyPath = KeyPath.FromBytes(data);
			Assert.Equal("1'", keyPath.ToString());
			AssertEx.CollectionEquals(data, keyPath.ToBytes());

			data = new byte[] { 0x01, 0x00, 0x00, 0x80, 0x02, 0x00, 0x00, 0x00 };
			keyPath = KeyPath.FromBytes(data);
			Assert.Equal("1'/2", keyPath.ToString());
			AssertEx.CollectionEquals(data, keyPath.ToBytes());

			data = new byte[0];
			keyPath = KeyPath.FromBytes(data);
			Assert.Equal("", keyPath.ToString());
			AssertEx.CollectionEquals(data, keyPath.ToBytes());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRoundTripExtKeyBase58Data()
		{
			var key = new ExtKey();
			var pubkey = key.Neuter();
			Assert.Equal(ExtKey.Parse(key.ToString(Network.Main), Network.Main).ToString(Network.Main), key.ToString(Network.Main));
			Assert.Equal(ExtPubKey.Parse(pubkey.ToString(Network.Main), Network.Main).ToString(Network.Main), pubkey.ToString(Network.Main));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanRecoverExtPubKeyFromHexString()
		{
			var key = new ExtKey();
			var pubkeyBytes = key.Neuter().ToBytes();
			var pubkeyHexString = Encoders.Hex.EncodeData(pubkeyBytes);


			var pubKeyFromBytes = new ExtPubKey(pubkeyBytes);
			var pubKeyFromHexString = new ExtPubKey(pubkeyHexString);

			Assert.True(pubKeyFromBytes.Equals(pubKeyFromHexString));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCheckChildKey()
		{
			var parent = new ExtKey();
			var child = parent.Derive(1);
			var notchild = new ExtKey();

			Assert.True(child.IsChildOf(parent));
			Assert.True(parent.IsParentOf(child));
			Assert.False(notchild.IsChildOf(parent));
			Assert.False(parent.IsParentOf(notchild));

			Assert.True(child.Neuter().IsChildOf(parent.Neuter()));
			Assert.True(parent.Neuter().IsParentOf(child.Neuter()));
			Assert.False(notchild.Neuter().IsChildOf(parent.Neuter()));
			Assert.False(parent.Neuter().IsParentOf(notchild.Neuter()));

			var keyA = parent.Neuter();
			var keyB = new ExtPubKey(keyA.ToBytes());
			AssertEx.CollectionEquals(keyA.ToBytes(), keyB.ToBytes());
		}
		private void RunTest(TestVector test)
		{
			var seed = TestUtils.ParseHex(test.strHexMaster);
			ExtKey key = ExtKey.CreateFromSeed(seed);
			ExtPubKey pubkey = key.Neuter();
			foreach (TestDerivation derive in test.vDerive)
			{
				byte[] data = key.ToBytes();
				Assert.Equal(74, data.Length);
				data = pubkey.ToBytes();
				Assert.Equal(74, data.Length);
				// Test private key
				BitcoinExtKey b58key = Network.Main.CreateBitcoinExtKey(key);
				var a = Encoders.Hex.EncodeData(Encoders.Base58Check.DecodeData(b58key.ToString()));
				var expected = Encoders.Hex.EncodeData(Encoders.Base58Check.DecodeData(derive.prv));
				Assert.True(b58key.ToString() == derive.prv);
				// Test public key
				BitcoinExtPubKey b58pubkey = Network.Main.CreateBitcoinExtPubKey(pubkey);
				Assert.True(b58pubkey.ToString() == derive.pub);
				// Derive new keys
				ExtKey keyNew = key.Derive(derive.nChild);
				ExtPubKey pubkeyNew = keyNew.Neuter();
				if (!((derive.nChild & 0x80000000) != 0))
				{
					// Compare with public derivation
					ExtPubKey pubkeyNew2 = pubkey.Derive(derive.nChild);
					Assert.True(pubkeyNew == pubkeyNew2);
				}
				key = keyNew;
				pubkey = pubkeyNew;
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanHandleEmptyKeyPath()
		{
			var rKeyPath = RootedKeyPath.Parse("01234567");
			Assert.Equal(rKeyPath.KeyPath, KeyPath.Empty);
			Assert.Equal("", KeyPath.Empty.ToString());
			Assert.Equal(new byte[0], KeyPath.Empty.ToBytes());
			Assert.Equal("01234567", rKeyPath.ToStringWithEmptyKeyPathAware());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void KeyPathShouldNotParseBIP32Overflow()
		{
			Assert.Equal(0x80000000U, uint.Parse("2147483648"));
			Assert.Throws<FormatException>(() => KeyPath.Parse("/2147483648"));
		}
	}
}
