using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
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
	}
}
