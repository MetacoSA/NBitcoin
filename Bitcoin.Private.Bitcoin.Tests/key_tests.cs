using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bitcoin.Private.Bitcoin.Tests
{
	public class key_tests
	{
		const string strSecret1 = ("5HxWvvfubhXpYYpS3tJkw6fq9jE9j18THftkZjHHfmFiWtmAbrj");
		const string strSecret2 = ("5KC4ejrDjv152FGwP386VD1i2NYc5KkfSMyv1nGy1VGDxGHqVY3");
		const string strSecret1C = ("Kwr371tjA9u2rFSMZjTNun2PXXP3WPZu2afRHTcta6KxEUdm1vEw");
		const string strSecret2C = ("L3Hq7a8FEQwJkW1M2GNKDW28546Vp5miewcCzSqUD9kCAXrJdS3g");
		const string strAddressBad = ("1HV9Lc3sNHZxwj4Zk6fB38tEmBryq2cBiF");

		BitcoinAddress addr1 = new BitcoinAddress("1QFqqMUD55ZV3PJEJZtaKCsQmjLT6JkjvJ");
		BitcoinAddress addr2 = new BitcoinAddress("1F5y5E5FMc5YzdJtB9hLaUe43GDxEKXENJ");
		BitcoinAddress addr1C = new BitcoinAddress("1NoJrossxPBKfCHuJXT4HadJrXRE9Fxiqs");
		BitcoinAddress addr2C = new BitcoinAddress("1CRj2HyM1CXWzHAXLQtiGLyggNT9WQqsDs");


		BitcoinAddress addrLocal = new BitcoinAddress("1Q1wVsNNiUo68caU7BfyFFQ8fVBqxC2DSc");
		uint256 msgLocal = Utils.Hash(TestUtils.ToBytes("Localbitcoins.com will change the world"));
		byte[] signatureLocal = Convert.FromBase64String("IJ/17TjGGUqmEppAliYBUesKHoHzfY4gR4DW0Yg7QzrHUB5FwX1uTJ/H21CF8ncY8HHNB5/lh8kPAOeD5QxV8Xc=");


		[Fact]
		public void CanVerifySignature()
		{
			var tests = new[]
			{
				new
				{
					Address = "15jZVzLc9cXz5PUFFda5A4Z7kZDYPg2NnL",
					PrivateKey = "L3TiCqmvPkXJpzCCZJuhy6wQtJZWDkR1AuqFY4Utib5J5XLuvLdZ",
					Message = "This is an example of a signed message.",
					Signature = "H6sliOnVrD9r+J8boZAKHZwBIW2zLiD72IfTIF94bfZhBI0JdMu9AM9rrF7P6eH+866YvM4H9xWGVN4jMJZycFU="
				},
				new
				{
					Address = "1QFqqMUD55ZV3PJEJZtaKCsQmjLT6JkjvJ",
					PrivateKey = "5HxWvvfubhXpYYpS3tJkw6fq9jE9j18THftkZjHHfmFiWtmAbrj",
					Message = "hello world",
					Signature = "G+dnSEywl3v1ijlWXvpY6zpu+AKNNXJcVmrdE35m0mMlzwFzXDiNg+uZrG9k8mpQL6sjHKrlBoDNSA+yaPW7PEA="
				},
				new
				{
					Address = "1Q1wVsNNiUo68caU7BfyFFQ8fVBqxC2DSc",
					PrivateKey = null as string,
					Message = "Localbitcoins.com will change the world",
					Signature = "IJ/17TjGGUqmEppAliYBUesKHoHzfY4gR4DW0Yg7QzrHUB5FwX1uTJ/H21CF8ncY8HHNB5/lh8kPAOeD5QxV8Xc="
				},
				new
				{
					Address = "1GvPJp7H8UYsYDvE4GFoV4f2gSCNZzGF48",
					PrivateKey = "5JEeah4w29axvf5Yg9v9PKv86zcCN9qVbizJDMHmiSUxBqDFoUT",
					Message = "This is an example of a signed message2",
					Signature = "G8YNwlo+I36Ct+hZKGSBFl3q8Kbx1pxPpwQmwdsG85io76+DUOHXqh/DfBq+Cn2R3C3dI//g3koSjxy7yNxJ9m8="
				},
			};


			foreach(var test in tests)
			{
				if(test.PrivateKey != null)
				{
					var secret = new BitcoinSecret(test.PrivateKey);
					var signature = secret.Key.SignMessage(test.Message);
					Assert.True(new BitcoinAddress(test.Address).VerifyMessage(test.Message, signature));
				}
				BitcoinAddress address = new BitcoinAddress(test.Address);
				Assert.True(address.VerifyMessage(test.Message,test.Signature));
				Assert.True(!address.VerifyMessage("bad message", test.Signature));
			}
		}

		[Fact]
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

			foreach(var test in tests)
			{
				BitcoinSecret secret = new BitcoinSecret(test.PrivateKeyWIF);
				Assert.Equal(test.PubKey, secret.Key.PubKey.ToHex());

				var address = new BitcoinAddress(test.Address);
				Assert.Equal(new KeyId(test.Hash160), address.ID);
				Assert.Equal(new KeyId(test.Hash160), secret.Key.PubKey.ID);
				Assert.Equal(address.ID, secret.Key.PubKey.Address.ID);

				var compressedSec = secret.Copy(true);

				var a = secret.Key.PubKey;
				var b = compressedSec.Key.PubKey;

				Assert.Equal(test.CompressedPrivateKeyWIF, compressedSec.ToWif());
				Assert.Equal(test.CompressedPubKey, compressedSec.Key.PubKey.ToHex());
				Assert.True(compressedSec.Key.PubKey.IsCompressed);

				var compressedAddr = new BitcoinAddress(test.CompressedAddress);
				Assert.Equal(new KeyId(test.CompressedHash160), compressedAddr.ID);
				Assert.Equal(new KeyId(test.CompressedHash160), compressedSec.Key.PubKey.ID);
				//Assert.True(compressedAddr.PubKey.IsCompressed);
			}
		}

		[Fact]
		public void key_test1()
		{
			BitcoinSecret bsecret1 = new BitcoinSecret(strSecret1);
			BitcoinSecret bsecret2 = new BitcoinSecret(strSecret2);
			BitcoinSecret bsecret1C = new BitcoinSecret(strSecret1C);
			BitcoinSecret bsecret2C = new BitcoinSecret(strSecret2C);
			Assert.Throws<FormatException>(() => new BitcoinSecret(strAddressBad));

			Key key1 = bsecret1.Key;
			Assert.True(key1.IsCompressed == false);
			Assert.True(bsecret1.Copy(true).Key.IsCompressed == true);
			Assert.True(bsecret1.Copy(true).Copy(false).IsCompressed == false);
			Assert.True(bsecret1.Copy(true).Copy(false).ToString() == bsecret1.ToString());
			Key key2 = bsecret2.Key;
			Assert.True(key2.IsCompressed == false);
			Key key1C = bsecret1C.Key;
			Assert.True(key1C.IsCompressed == true);
			Key key2C = bsecret2C.Key;
			Assert.True(key1C.IsCompressed == true);

			PubKey pubkey1 = key1.PubKey;
			PubKey pubkey2 = key2.PubKey;
			PubKey pubkey1C = key1C.PubKey;
			PubKey pubkey2C = key2C.PubKey;

			Assert.True(addr1.ID == pubkey1.ID);
			Assert.True(addr2.ID == pubkey2.ID);
			Assert.True(addr1C.ID == pubkey1C.ID);
			Assert.True(addr2C.ID == pubkey2C.ID);



			for(int n = 0 ; n < 16 ; n++)
			{
				string strMsg = String.Format("Very secret message {0}: 11", n);
				uint256 hashMsg = Utils.Hash(TestUtils.ToBytes(strMsg));

				// normal signatures

				ECDSASignature sign1, sign2, sign1C, sign2C;

				sign1 = key1.Sign(hashMsg);
				sign2 = key2.Sign(hashMsg);
				sign1C = key1C.Sign(hashMsg);
				sign2C = key2C.Sign(hashMsg);

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

				// compact signatures (with key recovery)

				ECDSASignature csign1, csign2, csign1C, csign2C;

				csign1 = key1.SignCompact(hashMsg);
				csign2 = key2.SignCompact(hashMsg);
				csign1C = key1C.SignCompact(hashMsg);
				csign2C = key2C.SignCompact(hashMsg);


				//PubKey rkey1, rkey2, rkey1C, rkey2C;



				//Assert.True(rkey1.RecoverCompact(hashMsg, csign1));
				//Assert.True(rkey2.RecoverCompact(hashMsg, csign2));
				//Assert.True(rkey1C.RecoverCompact(hashMsg, csign1C));
				//Assert.True(rkey2C.RecoverCompact(hashMsg, csign2C));

				//Assert.True(rkey1  == pubkey1);
				//Assert.True(rkey2  == pubkey2);
				//Assert.True(rkey1C == pubkey1C);
				//Assert.True(rkey2C == pubkey2C);
			}
		}
	}
}
