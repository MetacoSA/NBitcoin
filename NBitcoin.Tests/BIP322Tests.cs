using System;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	[Trait("UnitTest", "UnitTest")]
	public class BIP322Tests
	{
		//from https://github.com/ACken2/bip322-js/tree/main/test && https://github.com/bitcoin/bitcoin/pull/24058/files#diff-2bd57d7fbec4bb262834d155c304ebe15d26f73fea87c75ff273df3529a15510
		[Fact]
		public async Task CanHandleLegacyBIP322Message()
		{
			var address = BitcoinAddress.Create("1F3sAm6ZtwLAUnj7d38pGFxtP3RVEvtsbV", Network.Main);
			var addressTestnet = BitcoinAddress.Create("muZpTpBYhxmRFuCjLc7C6BBDF32C8XVJUi", Network.TestNet);
			var addressRegtest = BitcoinAddress.Create("muZpTpBYhxmRFuCjLc7C6BBDF32C8XVJUi", Network.RegTest);
			var addressWrong = BitcoinAddress.Create("1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2", Network.Main);
			var addressWrongTestnet = BitcoinAddress.Create("mipcBbFg9gMiCh81Kj8tqqdgoZub1ZJRfn", Network.TestNet);
			var addressWrongRegtest = BitcoinAddress.Create("mipcBbFg9gMiCh81Kj8tqqdgoZub1ZJRfn", Network.RegTest);
			var message = "This is an example of a signed message.";
			var messageWrong = "";
			var signature = BIP322.BIP322Signature.Parse("H9L5yLFjti0QTHhPyFrZCT1V/MMnBtXKmoiKDZ78NDBjERki6ZTQZdSMCtkgoNmp17By9ItJr8o7ChX0XxY91nk=", address.Network);
			Assert.True(await BIP322o.Verify(message, address, signature));
			Assert.True(await BIP322o.Verify(message, addressTestnet, signature));
			Assert.True(await BIP322o.Verify(message, addressRegtest, signature));
			Assert.False(await BIP322o.Verify(messageWrong, address, signature));
			Assert.False(await BIP322o.Verify(messageWrong, addressTestnet, signature));
			Assert.False(await BIP322o.Verify(messageWrong, addressRegtest, signature));
			Assert.False(await BIP322o.Verify(message, addressWrong, signature));
			Assert.False(await BIP322o.Verify(message, addressWrongTestnet, signature));
			Assert.False(await BIP322o.Verify(message, addressWrongRegtest, signature));


			var privateKey = new BitcoinSecret("L3VFeEujGtevx9w18HD1fhRbCH67Az2dpCymeRE1SoPK6XQtaN2k", Network.Main)
				.PrivateKey;
			address = privateKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);
			addressTestnet = privateKey.GetAddress(ScriptPubKeyType.Legacy, Network.TestNet);
			addressRegtest = privateKey.GetAddress(ScriptPubKeyType.Legacy, Network.RegTest);
			message = "Hello World";
			signature = await BIP322o.SignEncoded(address, message, BIP322o.SignatureType.Legacy, privateKey);
			var signatureTestnet =
				await BIP322o.SignEncoded(addressTestnet, message, BIP322o.SignatureType.Legacy, privateKey);
			var signatureRegtest =
				await BIP322o.SignEncoded(addressRegtest, message, BIP322o.SignatureType.Legacy, privateKey);

			Assert.True(await BIP322o.Verify(message, address, signature));
			Assert.True(await BIP322o.Verify(message, addressTestnet, signatureTestnet));
			Assert.True(await BIP322o.Verify(message, addressRegtest, signatureRegtest));
		}

		[Fact]
		public async Task CanSign()
		{
			var k = new Key();
			var p = k.GetAddress(ScriptPubKeyType.SegwitP2SH, Network.Main);

			var privateKey = new BitcoinSecret("KwTbAxmBXjoZM3bzbXixEr9nxLhyYSM4vp2swet58i19bw9sqk5z", Network.Main)
				.PrivateKey; // Private key of "3HSVzEhCFuH9Z3wvoWTexy7BMVVp3PjS6f"
			var privateKeyTestnet =
				new BitcoinSecret("cMpadsm2xoVpWV5FywY5cAeraa1PCtSkzrBM45Ladpf9rgDu6cMz", Network.TestNet)
					.PrivateKey; // Equivalent to "KwTbAxmBXjoZM3bzbXixEr9nxLhyYSM4vp2swet58i19bw9sqk5z"
			var address = BitcoinAddress.Create("3HSVzEhCFuH9Z3wvoWTexy7BMVVp3PjS6f", Network.Main);
			var addressTestnet = BitcoinAddress.Create("2N8zi3ydDsMnVkqaUUe5Xav6SZqhyqEduap", Network.TestNet);
			var addressRegtest = BitcoinAddress.Create("2N8zi3ydDsMnVkqaUUe5Xav6SZqhyqEduap", Network.RegTest);
			var message = "Hello World";

			var signature = await BIP322o.SignEncoded(address, message, BIP322o.SignatureType.Simple, privateKey);
			var signatureTestnet =
				await BIP322o.SignEncoded(addressTestnet, message, BIP322o.SignatureType.Simple, privateKeyTestnet);
			var signatureRegtest =
				await BIP322o.SignEncoded(addressRegtest, message, BIP322o.SignatureType.Simple, privateKeyTestnet);

			Assert.Equal(signatureTestnet, signature);
			Assert.Equal(signatureRegtest, signature);

			var k1 = new BitcoinSecret("L4DksdGZ4KQJfcLHD5Dv25fu8Rxyv7hHi2RjZR4TYzr8c6h9VNrp", Network.Main).PrivateKey;
			var k2 = new BitcoinSecret("KzSRqnCVwjzY8id2X5oHEJWXkSHwKUYaAXusjwgkES8BuQPJnPNu", Network.Main).PrivateKey;
			var k3 = new BitcoinSecret("L1zt9Rw7HrU7jaguMbVzhiX8ffuVkmMis5wLHddXYuHWYf8u8uRj", Network.Main).PrivateKey;


			var redeem =
				PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, true,
					new PubKey[] {k1.PubKey, k2.PubKey, k3.PubKey});
			var p2wshScriptPubKey = redeem.WitHash.ScriptPubKey;
			var p2ShAddressScriptPubKey = redeem.Hash.ScriptPubKey;
			var p2shAddress = p2ShAddressScriptPubKey.GetDestinationAddress(Network.Main);
			var p2wshAddress = p2wshScriptPubKey.GetDestinationAddress(Network.Main);

			var message2of3 = "This will be a 2-of-3 multisig BIP 322 signed message";
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await BIP322o.SignEncoded(p2shAddress, message2of3, BIP322o.SignatureType.Simple, redeem, null,k2, k3);
			});
			var p2sh_signature2of3_k2_k3 =
				await BIP322o.SignEncoded(p2shAddress, message2of3, BIP322o.SignatureType.Full, redeem, null,k2, k3);
			var p2sh_signature2of3_k1_k3 =
				await BIP322o.SignEncoded(p2shAddress, message2of3, BIP322o.SignatureType.Full, redeem, null,k1, k3);
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await BIP322o.SignEncoded(p2shAddress, message2of3, BIP322o.SignatureType.Full, redeem, null,k1);
			});
			Assert.True(await BIP322o.Verify(message2of3, p2shAddress, p2sh_signature2of3_k2_k3));
			Assert.True(await BIP322o.Verify(message2of3, p2shAddress, p2sh_signature2of3_k1_k3));

			var p2wsh_signature2of3_k2_k3 =
				await BIP322o.SignEncoded(p2wshAddress, message2of3, BIP322o.SignatureType.Simple, redeem, null,k2, k3);
			var p2wsh_signature2of3_k1_k3 =
				await BIP322o.SignEncoded(p2wshAddress, message2of3, BIP322o.SignatureType.Simple, redeem, null,k1, k3);
			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await BIP322o.SignEncoded(p2wshAddress, message2of3, BIP322o.SignatureType.Simple, redeem, null,k1);
			});
			Assert.True(await BIP322o.Verify(message2of3, p2wshAddress, p2wsh_signature2of3_k2_k3));
			Assert.True(await BIP322o.Verify(message2of3, p2wshAddress, p2wsh_signature2of3_k1_k3));
		}


		[Fact]
		public async Task CanVerifyBIP322Message()
		{
			var emptyStringHash = BIP322o.CreateMessageHash("");
			var helloWorldHash = BIP322o.CreateMessageHash("Hello World");

			Assert.Equal(new uint256("c90c269c4f8fcbe6880f72a721ddfbf1914268a794cbb21cfafee13770ae19f1"),
				emptyStringHash);
			Assert.Equal(new uint256("f0eb03b1a75ac6d9847f55c624a99169b5dccba2a31f5b23bea77ba270de0a7a"),
				helloWorldHash);

			var key = new BitcoinSecret("L3VFeEujGtevx9w18HD1fhRbCH67Az2dpCymeRE1SoPK6XQtaN2k", Network.Main)
				.PrivateKey;

			var segwitAddress = key.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
			Assert.Equal("bc1q9vza2e8x573nczrlzms0wvx3gsqjx7vavgkx0l", segwitAddress.ToString());


			var emptyStringToSpendTx =
				BIP322o.CreateToSpendTransaction(Network.Main, emptyStringHash, segwitAddress.ScriptPubKey);
			var helloWorldToSpendTx =
				BIP322o.CreateToSpendTransaction(Network.Main, helloWorldHash, segwitAddress.ScriptPubKey);
			Assert.Equal("c5680aa69bb8d860bf82d4e9cd3504b55dde018de765a91bb566283c545a99a7",
				emptyStringToSpendTx.GetHash().ToString());
			Assert.Equal("b79d196740ad5217771c1098fc4a4b51e0535c32236c71f1ea4d61a2d603352b",
				helloWorldToSpendTx.GetHash().ToString());

			var emptyStringToSignTx = BIP322o.CreateToSignTransaction(Network.Main, emptyStringToSpendTx.GetHash());
			var helloWorldToSignTx = BIP322o.CreateToSignTransaction(Network.Main, helloWorldToSpendTx.GetHash());

			Assert.Equal("1e9654e951a5ba44c8604c4de6c67fd78a27e81dcadcfe1edf638ba3aaebaed6",
				emptyStringToSignTx.GetHash().ToString());
			Assert.Equal("88737ae86f2077145f93cc4b153ae9a1cb8d56afa511988c149c5c8c9d93bddf",
				helloWorldToSignTx.GetHash().ToString());

			var simpleHelloWorldSignature = await
				BIP322o.SignEncoded(segwitAddress, "Hello World", BIP322o.SignatureType.Simple, key);
			var simpleEmptySignature = await
				BIP322o.SignEncoded(segwitAddress, "", BIP322o.SignatureType.Simple, key);

			var fullHelloWorldSignature = await
				BIP322o.SignEncoded(segwitAddress, "Hello World", BIP322o.SignatureType.Full, key);
			var fullEmptySignature = await BIP322o.SignEncoded(segwitAddress, "", BIP322o.SignatureType.Full, key);


			Assert.True(await BIP322o.Verify("Hello World", segwitAddress, simpleHelloWorldSignature));
			Assert.True(await BIP322o.Verify("", segwitAddress, simpleEmptySignature));
			Assert.True(await BIP322o.Verify("Hello World", segwitAddress, fullHelloWorldSignature));
			Assert.True(await BIP322o.Verify("", segwitAddress, fullEmptySignature));

			Assert.False(await BIP322o.Verify("nuhuh", segwitAddress, simpleHelloWorldSignature));
			Assert.False(await BIP322o.Verify("nuhuh", segwitAddress, simpleEmptySignature));
			Assert.False(await BIP322o.Verify("nuhuh", segwitAddress, fullHelloWorldSignature));
			Assert.False(await BIP322o.Verify("nuhuh", segwitAddress, fullEmptySignature));

			foreach (var t in new[]
			{
				("", "AkcwRAIgM2gBAQqvZX15ZiysmKmQpDrG83avLIT492QBzLnQIxYCIBaTpOaD20qRlEylyxFSeEA2ba9YOixpX8z46TSDtS40ASECx/EgAxlkQpQ9hYjgGu6EBCPMVPwVIVJqO4XCsMvViHI="),
				("", "AkgwRQIhAPkJ1Q4oYS0htvyuSFHLxRQpFAY56b70UvE7Dxazen0ZAiAtZfFz1S6T6I23MWI2lK/pcNTWncuyL8UL+oMdydVgzAEhAsfxIAMZZEKUPYWI4BruhAQjzFT8FSFSajuFwrDL1Yhy"),
				("Hello World", "AkcwRAIgZRfIY3p7/DoVTty6YZbWS71bc5Vct9p9Fia83eRmw2QCICK/ENGfwLtptFluMGs2KsqoNSk89pO7F29zJLUx9a/sASECx/EgAxlkQpQ9hYjgGu6EBCPMVPwVIVJqO4XCsMvViHI="),
				("Hello World", "AkgwRQIhAOzyynlqt93lOKJr+wmmxIens//zPzl9tqIOua93wO6MAiBi5n5EyAcPScOjf1lAqIUIQtr3zKNeavYabHyR8eGhowEhAsfxIAMZZEKUPYWI4BruhAQjzFT8FSFSajuFwrDL1Yhy")
			})
			{
				(var message, var sig) = t;
				Assert.True(await BIP322o.Verify(message, segwitAddress, sig));
			}			

			// // 2-of-3 p2sh multisig BIP322 signature (created with the buidl-python library)
			// // Keys are defined as (HDRootWIF, bip322_path)
			// // Key1 (L4DksdGZ4KQJfcLHD5Dv25fu8Rxyv7hHi2RjZR4TYzr8c6h9VNrp, m/45'/0/0/1)
			// // Key2 (KzSRqnCVwjzY8id2X5oHEJWXkSHwKUYaAXusjwgkES8BuQPJnPNu, m/45'/0/0/3)
			// // Key3 (L1zt9Rw7HrU7jaguMbVzhiX8ffuVkmMis5wLHddXYuHWYf8u8uRj, m/45'/0/0/6)
			// // BIP322 includes signs from Key2 and Key3
			// BOOST_CHECK_EQUAL(
			// 	MessageVerify(
			// 		"3LnYoUkFrhyYP3V7rq3mhpwALz1XbCY9Uq",
			// 		"AAAAAAHNcfHaNfl8f/+ZC2gTr8aF+0KgppYjKM94egaNm/u1ZAAAAAD8AEcwRAIhAJ6hdj61vLDP+aFa30qUZQmrbBfE0kiOObYvt5nqPSxsAh9IrOKFwflfPRUcQ/5e0REkdFHVP2GGdUsMgDet+sNlAUcwRAIgH3eW/VyFDoXvCasd8qxgwj5NDVo0weXvM6qyGXLCR5YCIEwjbEV6fS6RWP6QsKOcMwvlGr1/SgdCC6pW4eH87/YgAUxpUiECKJfGy28imLcuAeNBLHCNv3NRP5jnJwFDNRXCYNY/vJ4hAv1RQtaZs7+vKqQeWl2rb/jd/gMxkEjUnjZdDGPDZkMLIQL65cH2X5O7LujjTLDL2l8Pxy0Y2UUR99u1qCfjdz7dklOuAAAAAAEAAAAAAAAAAAFqAAAAAA==",
			// 		"This will be a p2sh 2-of-3 multisig BIP 322 signed message"),
			// 	MessageVerificationResult::OK);
			Assert.True(await BIP322o.Verify("This will be a p2sh 2-of-3 multisig BIP 322 signed message",
				BitcoinAddress.Create("3LnYoUkFrhyYP3V7rq3mhpwALz1XbCY9Uq", Network.Main),
				"AAAAAAHNcfHaNfl8f/+ZC2gTr8aF+0KgppYjKM94egaNm/u1ZAAAAAD8AEcwRAIhAJ6hdj61vLDP+aFa30qUZQmrbBfE0kiOObYvt5nqPSxsAh9IrOKFwflfPRUcQ/5e0REkdFHVP2GGdUsMgDet+sNlAUcwRAIgH3eW/VyFDoXvCasd8qxgwj5NDVo0weXvM6qyGXLCR5YCIEwjbEV6fS6RWP6QsKOcMwvlGr1/SgdCC6pW4eH87/YgAUxpUiECKJfGy28imLcuAeNBLHCNv3NRP5jnJwFDNRXCYNY/vJ4hAv1RQtaZs7+vKqQeWl2rb/jd/gMxkEjUnjZdDGPDZkMLIQL65cH2X5O7LujjTLDL2l8Pxy0Y2UUR99u1qCfjdz7dklOuAAAAAAEAAAAAAAAAAAFqAAAAAA=="));

			// // 3-of-3 p2wsh multisig BIP322 signature (created with the buidl-python library)
			// // Keys are defined as (HDRootWIF, bip322_path)
			// // Key1 (L4DksdGZ4KQJfcLHD5Dv25fu8Rxyv7hHi2RjZR4TYzr8c6h9VNrp, m/45'/0/0/6)
			// // Key2 (KzSRqnCVwjzY8id2X5oHEJWXkSHwKUYaAXusjwgkES8BuQPJnPNu, m/45'/0/0/9)
			// // Key3 (L1zt9Rw7HrU7jaguMbVzhiX8ffuVkmMis5wLHddXYuHWYf8u8uRj, m/45'/0/0/11)
			// BOOST_CHECK_EQUAL(
			// 	MessageVerify(
			// 		"bc1qlqtuzpmazp2xmcutlwv0qvggdvem8vahkc333usey4gskug8nutsz53msw",    "BQBIMEUCIQDQoXvGKLH58exuujBOta+7+GN7vi0lKwiQxzBpuNuXuAIgIE0XYQlFDOfxbegGYYzlf+tqegleAKE6SXYIa1U+uCcBRzBEAiATegywVl6GWrG9jJuPpNwtgHKyVYCX2yfuSSDRFATAaQIgTLlU6reLQsSIrQSF21z3PtUO2yAUseUWGZqRUIE7VKoBSDBFAiEAgxtpidsU0Z4u/+5RB9cyeQtoCW5NcreLJmWXZ8kXCZMCIBR1sXoEinhZE4CF9P9STGIcMvCuZjY6F5F0XTVLj9SjAWlTIQP3dyWvTZjUENWJowMWBsQrrXCUs20Gu5YF79CG5Ga0XSEDwqI5GVBOuFkFzQOGH5eTExSAj2Z/LDV/hbcvAPQdlJMhA17FuuJd+4wGuj+ZbVxEsFapTKAOwyhfw9qpch52JKxbU64=",
			// 		"This will be a p2wsh 3-of-3 multisig BIP 322 signed message"),
			// 	MessageVerificationResult::OK);
			Assert.True(await BIP322o.Verify("This will be a p2wsh 3-of-3 multisig BIP 322 signed message",
				BitcoinAddress.Create("bc1qlqtuzpmazp2xmcutlwv0qvggdvem8vahkc333usey4gskug8nutsz53msw", Network.Main),
				"BQBIMEUCIQDQoXvGKLH58exuujBOta+7+GN7vi0lKwiQxzBpuNuXuAIgIE0XYQlFDOfxbegGYYzlf+tqegleAKE6SXYIa1U+uCcBRzBEAiATegywVl6GWrG9jJuPpNwtgHKyVYCX2yfuSSDRFATAaQIgTLlU6reLQsSIrQSF21z3PtUO2yAUseUWGZqRUIE7VKoBSDBFAiEAgxtpidsU0Z4u/+5RB9cyeQtoCW5NcreLJmWXZ8kXCZMCIBR1sXoEinhZE4CF9P9STGIcMvCuZjY6F5F0XTVLj9SjAWlTIQP3dyWvTZjUENWJowMWBsQrrXCUs20Gu5YF79CG5Ga0XSEDwqI5GVBOuFkFzQOGH5eTExSAj2Z/LDV/hbcvAPQdlJMhA17FuuJd+4wGuj+ZbVxEsFapTKAOwyhfw9qpch52JKxbU64="));

#if HAS_SPAN
			// // Single key p2tr BIP322 signature (created with the buidl-python library)
			// // PrivateKeyWIF L3VFeEujGtevx9w18HD1fhRbCH67Az2dpCymeRE1SoPK6XQtaN2k
			// BOOST_CHECK_EQUAL(
			// 	MessageVerify(
			// 		"bc1ppv609nr0vr25u07u95waq5lucwfm6tde4nydujnu8npg4q75mr5sxq8lt3",
			// 		"AUHd69PrJQEv+oKTfZ8l+WROBHuy9HKrbFCJu7U1iK2iiEy1vMU5EfMtjc+VSHM7aU0SDbak5IUZRVno2P5mjSafAQ==",
			// 		"Hello World"),
			// 	MessageVerificationResult::OK);
			Assert.True(await BIP322o.Verify("Hello World",
				BitcoinAddress.Create("bc1ppv609nr0vr25u07u95waq5lucwfm6tde4nydujnu8npg4q75mr5sxq8lt3", Network.Main),
				"AUHd69PrJQEv+oKTfZ8l+WROBHuy9HKrbFCJu7U1iK2iiEy1vMU5EfMtjc+VSHM7aU0SDbak5IUZRVno2P5mjSafAQ=="));
#endif
		}


		[Fact]
		public async Task CanDoProofOfFunds()
		{
			var key = new Key();
			var key2 = new Key();
			var addr = key.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
			var addr2 = key2.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
			var addr3 = key2.GetAddress(ScriptPubKeyType.SegwitP2SH, Network.Main);

			var multisigRedeem =
				PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new[] {key.PubKey, key2.PubKey});
			var p2wshScriptPubKey = multisigRedeem.WitHash.ScriptPubKey;
			var p2ShAddressScriptPubKey = multisigRedeem.Hash.ScriptPubKey;

			var p2shAddress = p2ShAddressScriptPubKey.GetDestinationAddress(Network.Main);
			var p2wshAddress = p2wshScriptPubKey.GetDestinationAddress(Network.Main);

			var coins = new Coin[]
			{
				new Coin(OutPoint.Zero, new TxOut(Money.Coins(1), addr2)),
				new Coin(new OutPoint(uint256.One, 1), new TxOut(Money.Coins(1), addr3)),
				new ScriptCoin(new OutPoint(uint256.One, 2), new TxOut(Money.Coins(1), p2shAddress), multisigRedeem),
				new ScriptCoin(new OutPoint(uint256.One, 4), new TxOut(Money.Coins(1), p2wshAddress), multisigRedeem)

			};

			var message = "I own these coins";

			var signature = await BIP322o.SignEncoded(addr, message, BIP322o.SignatureType.Full, null, coins, key, key2);




		}
	}
}
