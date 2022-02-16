using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class key_tests
	{
		const string strSecret1 = ("5HxWvvfubhXpYYpS3tJkw6fq9jE9j18THftkZjHHfmFiWtmAbrj");
		const string strSecret2 = ("5KC4ejrDjv152FGwP386VD1i2NYc5KkfSMyv1nGy1VGDxGHqVY3");
		const string strSecret1C = ("Kwr371tjA9u2rFSMZjTNun2PXXP3WPZu2afRHTcta6KxEUdm1vEw");
		const string strSecret2C = ("L3Hq7a8FEQwJkW1M2GNKDW28546Vp5miewcCzSqUD9kCAXrJdS3g");
		const string strAddressBad = ("1HV9Lc3sNHZxwj4Zk6fB38tEmBryq2cBiF");

		BitcoinPubKeyAddress addr1 = (BitcoinPubKeyAddress)Network.Main.CreateBitcoinAddress("1QFqqMUD55ZV3PJEJZtaKCsQmjLT6JkjvJ");
		BitcoinPubKeyAddress addr2 = (BitcoinPubKeyAddress)Network.Main.CreateBitcoinAddress("1F5y5E5FMc5YzdJtB9hLaUe43GDxEKXENJ");
		BitcoinPubKeyAddress addr1C = (BitcoinPubKeyAddress)Network.Main.CreateBitcoinAddress("1NoJrossxPBKfCHuJXT4HadJrXRE9Fxiqs");
		BitcoinPubKeyAddress addr2C = (BitcoinPubKeyAddress)Network.Main.CreateBitcoinAddress("1CRj2HyM1CXWzHAXLQtiGLyggNT9WQqsDs");

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGeneratePubKeysAndAddress()
		{
			//Took from http://brainwallet.org/ and http://procbits.com/2013/08/27/generating-a-bitcoin-address-with-javascript
			var tests = new[]
			{
				new
				{
					PrivateKeyWIF = "5Hx15HFGyep2CfPxsJKe2fXJsCVn5DEiyoeGGF6JZjGbTRnqfiD",
					CompressedPrivateKeyWIF = "KwomKti1X3tYJUUMb1TGSM2mrZk1wb1aHisUNHCQXTZq5auC2qc3",
					PubKey = "04d0988bfa799f7d7ef9ab3de97ef481cd0f75d2367ad456607647edde665d6f6fbdd594388756a7beaf73b4822bc22d36e9bda7db82df2b8b623673eefc0b7495",
					CompressedPubKey = "03d0988bfa799f7d7ef9ab3de97ef481cd0f75d2367ad456607647edde665d6f6f",
					Address =           "16UjcYNBG9GTK4uq2f7yYEbuifqCzoLMGS",
					CompressedAddress = "1FkKMsKNJqWSDvTvETqcCeHcUQQ64kSC6s",
					Hash160 = "3c176e659bea0f29a3e9bf7880c112b1b31b4dc8",
					CompressedHash160 = "a1c2f92a9dacbd2991c3897724a93f338e44bdc1"
				},
				new
				{
					PrivateKeyWIF = "5J7WTMRn1vjZ9udUxNCLq7F9DYEJiqRCjstiBrY6mDjnaomd6kZ",
					CompressedPrivateKeyWIF = "KxXj1KAMh6ApvKJ2PNZ4XLZRGLqjDehppFdEnueGSBDrC2Hfe7vt",
					PubKey = "0493e5d305cad2588d5fb254065fe48ce446028ba380e6ee663baea9cd105500897eb030c033cdab160f31c36df0ea38330fdd69677df49cd14826902022d17f3f",
					CompressedPubKey = "0393e5d305cad2588d5fb254065fe48ce446028ba380e6ee663baea9cd10550089",
					Address =           "1MZmwgyMyjM11uA6ZSpgn1uK3LBWCzvV6e",
					CompressedAddress = "1AECNr2TDye8dpC1TeDH3eJpGoZ7dNPy4g",
					Hash160 = "e19557c8f8fb53a964c5dc7bfde86d806709f7c5",
					CompressedHash160 = "6538094af65453ea279f14d1a04b408e3adfebd7"
				}
			};

			foreach (var test in tests)
			{
				BitcoinSecret secret = Network.Main.CreateBitcoinSecret(test.PrivateKeyWIF);
				Assert.Equal(test.PubKey, secret.PrivateKey.PubKey.ToHex());

				var address = (BitcoinPubKeyAddress)Network.Main.CreateBitcoinAddress(test.Address);
				Assert.Equal(new KeyId(test.Hash160), address.Hash);
				Assert.Equal(new KeyId(test.Hash160), secret.PrivateKey.PubKey.Hash);
				Assert.Equal(new KeyId(test.Hash160).ToString(), test.Hash160);
				Assert.Equal(address, secret.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main));

				Assert.Equal(new WitKeyId(test.Hash160).ToString(), address.Hash.ToString());
				Assert.Equal(new WitKeyId(test.Hash160).ToString(), secret.PrivateKey.PubKey.Hash.ToString());
				Assert.Equal(new WitKeyId(test.Hash160).ToString(), test.Hash160.ToString());

				var compressedSec = secret.Copy(true);

				var a = secret.PrivateKey.PubKey;
				var b = compressedSec.PrivateKey.PubKey;

				Assert.Equal(test.CompressedPrivateKeyWIF, compressedSec.ToWif());
				Assert.Equal(test.CompressedPubKey, compressedSec.PrivateKey.PubKey.ToHex());
				Assert.True(compressedSec.PrivateKey.PubKey.IsCompressed);

				var compressedAddr = (BitcoinPubKeyAddress)Network.Main.CreateBitcoinAddress(test.CompressedAddress);
				Assert.Equal(new KeyId(test.CompressedHash160), compressedAddr.Hash);
				Assert.Equal(new KeyId(test.CompressedHash160), compressedSec.PrivateKey.PubKey.Hash);
			}
		}

		[Fact]
		[Trait("Core", "Core")]
		public void QuickTestsOnKeyIdBytes()
		{
			var a = new KeyId("93e5d305cad2588d5fb254065fe48ce446028ba3");
			var b = new ScriptId("93e5d305cad2588d5fb254065fe48ce446028ba3");
			var b2 = new WitKeyId("93e5d305cad2588d5fb254065fe48ce446028ba3");
			Assert.Equal(a.ToString(), b.ToString());
			Assert.Equal(a.ToString(), b2.ToString());
			Assert.Equal("93e5d305cad2588d5fb254065fe48ce446028ba3", a.ToString());
			var bytes = Encoders.Hex.DecodeData("93e5d305cad2588d5fb254065fe48ce446028ba3");
			Assert.True(StructuralComparisons.StructuralComparer.Compare(a.ToBytes(), b.ToBytes()) == 0);
			Assert.True(StructuralComparisons.StructuralComparer.Compare(b2.ToBytes(), b.ToBytes()) == 0);

			var c = new KeyId(bytes);
			var d = new ScriptId(bytes);
			var d2 = new WitKeyId(bytes);
			Assert.Equal(a, c);
			Assert.Equal(d, b);
			Assert.Equal(b2, d2);
			Assert.Equal(c.ToString(), d.ToString());
			Assert.Equal(c.ToString(), d2.ToString());
			Assert.Equal("93e5d305cad2588d5fb254065fe48ce446028ba3", c.ToString());
			Assert.True(StructuralComparisons.StructuralComparer.Compare(c.ToBytes(), d.ToBytes()) == 0);
			Assert.True(StructuralComparisons.StructuralComparer.Compare(c.ToBytes(), d2.ToBytes()) == 0);

			var e = new WitScriptId("93e5d305cad2588d5fb254065fe48ce446028ba380e6ee663baea9cd10550089");
			Assert.Equal("93e5d305cad2588d5fb254065fe48ce446028ba380e6ee663baea9cd10550089", e.ToString());
			bytes = Encoders.Hex.DecodeData("93e5d305cad2588d5fb254065fe48ce446028ba380e6ee663baea9cd10550089");
			var e2 = new WitScriptId(bytes);
			Assert.Equal(e, e2);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void key_test1()
		{
			BitcoinSecret bsecret1 = Network.Main.CreateBitcoinSecret(strSecret1);
			BitcoinSecret bsecret2 = Network.Main.CreateBitcoinSecret(strSecret2);
			BitcoinSecret bsecret1C = Network.Main.CreateBitcoinSecret(strSecret1C);
			BitcoinSecret bsecret2C = Network.Main.CreateBitcoinSecret(strSecret2C);
			Assert.Throws<FormatException>(() => Network.Main.CreateBitcoinSecret(strAddressBad));

			Key key1 = bsecret1.PrivateKey;
			Assert.True(key1.IsCompressed == false);
			Assert.True(bsecret1.Copy(true).PrivateKey.IsCompressed == true);
			Assert.True(bsecret1.Copy(true).Copy(false).IsCompressed == false);
			Assert.True(bsecret1.Copy(true).Copy(false).ToString() == bsecret1.ToString());
			Key key2 = bsecret2.PrivateKey;
			Assert.True(key2.IsCompressed == false);
			Key key1C = bsecret1C.PrivateKey;
			Assert.True(key1C.IsCompressed == true);
			Key key2C = bsecret2C.PrivateKey;
			Assert.True(key1C.IsCompressed == true);

			PubKey pubkey1 = key1.PubKey;
			PubKey pubkey2 = key2.PubKey;
			PubKey pubkey1C = key1C.PubKey;
			PubKey pubkey2C = key2C.PubKey;

			Assert.True(addr1.Hash == pubkey1.Hash);
			Assert.True(addr2.Hash == pubkey2.Hash);
			Assert.True(addr1C.Hash == pubkey1C.Hash);
			Assert.True(addr2C.Hash == pubkey2C.Hash);


			for (int n = 0; n < 16; n++)
			{
				string strMsg = String.Format("Very secret message {0}: 11", n);
				if (n == 10)
				{
					//Test one long message
					strMsg = String.Join(",", Enumerable.Range(0, 2000).Select(i => i.ToString()).ToArray());
				}
				uint256 hashMsg = Hashes.DoubleSHA256(TestUtils.ToBytes(strMsg));

				// normal signatures

				ECDSASignature sign1 = null, sign2 = null, sign1C = null, sign2C = null;
				List<Task> tasks = new List<Task>();
				sign1 = key1.Sign(hashMsg);
				sign2 = key2.Sign(hashMsg);
				sign1C = key1C.Sign(hashMsg);
				sign2C = key2C.Sign(hashMsg);

				for (int i = 0; i < 30; i++)
				{
					Assert.True(pubkey1.Verify(hashMsg, sign1));
					Assert.True(pubkey2.Verify(hashMsg, sign2));
					Assert.True(pubkey1C.Verify(hashMsg, sign1C));
					Assert.True(pubkey2C.Verify(hashMsg, sign2C));

					Assert.True(pubkey1.Verify(hashMsg, sign1));
					Assert.True(!pubkey1.Verify(hashMsg, sign2));
					Assert.True(pubkey1.Verify(hashMsg, sign1C));
					Assert.True(!pubkey1.Verify(hashMsg, sign2C));

					Assert.True(!pubkey2.Verify(hashMsg, sign1));
					Assert.True(pubkey2.Verify(hashMsg, sign2));
					Assert.True(!pubkey2.Verify(hashMsg, sign1C));
					Assert.True(pubkey2.Verify(hashMsg, sign2C));

					Assert.True(pubkey1C.Verify(hashMsg, sign1));
					Assert.True(!pubkey1C.Verify(hashMsg, sign2));
					Assert.True(pubkey1C.Verify(hashMsg, sign1C));
					Assert.True(!pubkey1C.Verify(hashMsg, sign2C));

					Assert.True(!pubkey2C.Verify(hashMsg, sign1));
					Assert.True(pubkey2C.Verify(hashMsg, sign2));
					Assert.True(!pubkey2C.Verify(hashMsg, sign1C));
					Assert.True(pubkey2C.Verify(hashMsg, sign2C));
				}
				// compact signatures (with key recovery)

				CompactSignature csign1 = null, csign2 = null, csign1C = null, csign2C = null;

				if (key1.IsCompressed)
				{
					csign1 = key1.SignCompact(hashMsg);
					csign2 = key2.SignCompact(hashMsg);
					csign1C = key1C.SignCompact(hashMsg);
					csign2C = key2C.SignCompact(hashMsg);

					PubKey rkey1 = null, rkey2 = null, rkey1C = null, rkey2C = null;
					rkey1 = PubKey.RecoverCompact(hashMsg, csign1);
					rkey2 = PubKey.RecoverCompact(hashMsg, csign2);
					rkey1C = PubKey.RecoverCompact(hashMsg, csign1C);
					rkey2C = PubKey.RecoverCompact(hashMsg, csign2C);

					Assert.True(rkey1.ToHex() == pubkey1.ToHex());
					Assert.True(rkey2.ToHex() == pubkey2.ToHex());
					Assert.True(rkey1C.ToHex() == pubkey1C.ToHex());
					Assert.True(rkey2C.ToHex() == pubkey2C.ToHex());

					Assert.True(sign1.IsLowR && sign1.ToDER().Length <= 70);
					Assert.True(sign2.IsLowR && sign2.ToDER().Length <= 70);
					Assert.True(sign1C.IsLowR && sign1C.ToDER().Length <= 70);
					Assert.True(sign2C.IsLowR && sign2C.ToDER().Length <= 70);
				}
			}
		}

		[Fact]
		[Trait("Core", "Core")]
		public void key_test_from_bytes()
		{
			//Example private key taken from https://en.bitcoin.it/wiki/Private_key
			Byte[] privateKey = new Byte[32] { 0xE9, 0x87, 0x3D, 0x79, 0xC6, 0xD8, 0x7D, 0xC0, 0xFB, 0x6A, 0x57, 0x78, 0x63, 0x33, 0x89, 0xF4, 0x45, 0x32, 0x13, 0x30, 0x3D, 0xA6, 0x1F, 0x20, 0xBD, 0x67, 0xFC, 0x23, 0x3A, 0xA3, 0x32, 0x62 };
			Key key1 = new Key(privateKey, -1, false);

			ISecret wifKey = key1.GetWif(NBitcoin.Network.Main);

			//Example wif private key taken from https://en.bitcoin.it/wiki/Private_key
			const String expected = "5Kb8kLf9zgWQnogidDA76MzPL6TsZZY36hWXMssSzNydYXYB9KF";
			Assert.True(wifKey.ToString() == expected);
		}
	}
}
