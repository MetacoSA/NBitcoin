using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//https://github.com/bitcoin/bips/blob/master/bip-0038.mediawiki
	public class BIP38Tests
	{
		[Fact]
		public void EncryptedSecretNoECmultiply()
		{
			var tests = new[]
			{
				new {
				Passphrase= "TestingOneTwoThree",
				Encrypted = "6PRVWUbkzzsbcVac2qwfssoUJAN1Xhrg6bNk8J7Nzm5H7kxEbn2Nh2ZoGg",
				Unencrypted = "5KN7MzqK5wt2TP1fQCYyHBtDrXdJuXbUzm4A9rKAteGu3Qi5CVR",
				Compressed = false
				},
				new {
				Passphrase= "Satoshi",
				Encrypted = "6PRNFFkZc2NZ6dJqFfhRoFNMR9Lnyj7dYGrzdgXXVMXcxoKTePPX1dWByq",
				Unencrypted = "5HtasZ6ofTHP6HCwTqTkLDuLQisYPah7aUnSKfC7h4hMUVw2gi5",
				Compressed = false
				},
				new {
				Passphrase= "TestingOneTwoThree",
				Encrypted = "6PYNKZ1EAgYgmQfmNVamxyXVWHzK5s6DGhwP4J5o44cvXdoY7sRzhtpUeo",
				Unencrypted = "L44B5gGEpqEDRS9vVPz7QT35jcBG2r3CZwSwQ4fCewXAhAhqGVpP",
				Compressed = true
				},
				new {
				Passphrase= "Satoshi",
				Encrypted = "6PYLtMnXvfG3oJde97zRyLYFZCYizPU5T3LwgdYJz1fRhh16bU7u6PPmY7",
				Unencrypted = "KwYgW8gcxj1JWJXhPSu4Fqwzfhp5Yfi42mdYmMa4XqK7NJxXUSK7",
				Compressed = true
				}
			};

			//Slow test, run in parallel
			Parallel.ForEach(tests, test =>
			{
				var secret = new BitcoinSecret(test.Unencrypted, Network.Main);
				var encryptedKey = secret.Key.GetEncryptedBitcoinSecret(test.Passphrase, Network.Main);
				Assert.Equal(test.Encrypted, encryptedKey.ToString());

				var actualSecret = encryptedKey.GetKey(test.Passphrase);
				Assert.Equal(test.Unencrypted, actualSecret.GetBitcoinSecret(Network.Main).ToString());

				Assert.Equal(test.Compressed, actualSecret.IsCompressed);
			});
		}

		[Fact]
		public void EncryptedSecretECmultiplyNoLot()
		{
			var tests = new[]
			{
				new {
				Passphrase= "TestingOneTwoThree",
				PassphraseCode= "passphrasepxFy57B9v8HtUsszJYKReoNDV6VHjUSGt8EVJmux9n1J3Ltf1gRxyDGXqnf9qm",
				Encrypted = "6PfQu77ygVyJLZjfvMLyhLMQbYnu5uguoJJ4kMCLqWwPEdfpwANVS76gTX",
				Address = "1PE6TQi6HTVNz5DLwB1LcpMBALubfuN2z2",
				Unencrypted = "5K4caxezwjGCGfnoPTZ8tMcJBLB7Jvyjv4xxeacadhq8nLisLR2",
				Compressed = false
				},
				new {
				Passphrase= "Satoshi",
				PassphraseCode= "passphraseoRDGAXTWzbp72eVbtUDdn1rwpgPUGjNZEc6CGBo8i5EC1FPW8wcnLdq4ThKzAS",
				Encrypted = "6PfLGnQs6VZnrNpmVKfjotbnQuaJK4KZoPFrAjx1JMJUa1Ft8gnf5WxfKd",
				Address = "1CqzrtZC6mXSAhoxtFwVjz8LtwLJjDYU3V",
				Unencrypted = "5KJ51SgxWaAYR13zd9ReMhJpwrcX47xTJh2D3fGPG9CM8vkv5sH",
				Compressed = false
				}
			};
			foreach(var test in tests)
			{
				//Can generate unencrypted key with password and encrypted key
				var encryptedKey = new BitcoinEncryptedSecretEC(test.Encrypted, Network.Main);
				var actualKey = encryptedKey.GetKey(test.Passphrase);
				Assert.Equal(test.Unencrypted, actualKey.GetBitcoinSecret(Network.Main).ToString());
				Assert.Equal(test.Address, actualKey.PubKey.GetAddress(Network.Main).ToString());
				Assert.Equal(test.Compressed, actualKey.IsCompressed);


				//Can generate same BitcoinPassphraseCode with by using same ownerentropy
				var passCode = new BitcoinPassphraseCode(test.PassphraseCode, Network.Main);
				var actualPassCode = BitcoinPassphraseCode.Generate(test.Passphrase, Network.Main, ownersalt: passCode.OwnerEntropy);
				Assert.Equal(passCode.ToString(), actualPassCode.ToString());

				//Can generate encrypted key from passcode
				var generatedEncryptedKey = passCode.GenerateEncryptedSecret(test.Compressed).EncryptedKey;
				AssertEx.CollectionEquals(passCode.OwnerEntropy, generatedEncryptedKey.OwnerEntropy);
				Assert.Equal(test.Compressed, generatedEncryptedKey.IsCompressed);
			}
		}

		

		[Fact]
		public void EncryptedSecretECmultiplyNoLotSimple()
		{
			var compressedValues = new[] { false, true };
			foreach(var compressed in compressedValues)
			{
				var code = BitcoinPassphraseCode.Generate("test", Network.Main);
				var result = code.GenerateEncryptedSecret(compressed);

				var decryptedKey = result.EncryptedKey.GetKey("test");
				Assert.Equal(result.GeneratedAddress.ToString(), decryptedKey.PubKey.GetAddress(Network.Main).ToString());

				Assert.Throws<SecurityException>(() => result.EncryptedKey.GetKey("wrong"));

				//Can regenerate same result with same seed
				var result2 = code.GenerateEncryptedSecret(compressed, seedb: result.Seed);
				var decryptedKey2 = result.EncryptedKey.GetKey("test");
				AssertEx.CollectionEquals(decryptedKey2.ToBytes(), decryptedKey.ToBytes());
			}
		}

		[Fact]
		public void CanRoundTripSeedEncryption()
		{
			//Test easily debuggable
			var seed = new byte[24];
			var derived = new byte[64];
			var encrypted = BitcoinEncryptedSecret.EncryptSeed(seed, derived);
			var actualSeed = BitcoinEncryptedSecret.DecryptSeed(encrypted, derived);
			Assert.Equal(seed, actualSeed);

			//The real deal
			for(int i = 0 ; i < 5 ; i++)
			{
				seed = RandomUtils.GetBytes(24);
				derived = RandomUtils.GetBytes(64);
				encrypted = BitcoinEncryptedSecret.EncryptSeed(seed, derived);

				var encryptedBefore = encrypted.ToArray();
				for(int u = 8 ; u < 16 ; u++)
				{
					encrypted[u] = 0;
				}
				actualSeed = BitcoinEncryptedSecret.DecryptSeed(encrypted, derived);
				//Restore old encrypted
				AssertEx.CollectionEquals(encrypted, encryptedBefore);
				Assert.Equal(seed, actualSeed);
			}
		}
		[Fact]
		public void CanRoundTripKeyEncryption()
		{
			//Test easily debuggable
			var key = new byte[32];
			var derived = new byte[64];
			var encrypted = BitcoinEncryptedSecret.EncryptKey(key, derived);
			var actualSeed = BitcoinEncryptedSecret.DecryptKey(encrypted, derived);
			Assert.Equal(key, actualSeed);

			//The real deal
			for(int i = 0 ; i < 5 ; i++)
			{
				key = RandomUtils.GetBytes(32);
				derived = RandomUtils.GetBytes(64);
				encrypted = BitcoinEncryptedSecret.EncryptKey(key, derived);
				actualSeed = BitcoinEncryptedSecret.DecryptKey(encrypted, derived);
				Assert.Equal(key, actualSeed);
			}
		}
	}
}
