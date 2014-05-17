using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Org.BouncyCastle.Math;
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
		public void CanParseStealthAddress()
		{
			var tests = new[] 
			{ 
				//Test vector from https://en.bitcoin.it/wiki/Sx/Stealth
				new
				{
					ReceiverSecret = "c8edfa93b0475d617de98643673ddbd3f25b08a98261a2b3f913ea29990ef6f6",
					ReceiverAddress = "SxjHZmrj1GrtuyW8dLmtYbNsLTEUGCQzpbk6iFRHEiBiZ5Z8Nq8EVt",
				}
			};

			foreach(var test in tests)
			{
				var receiverKey = new Key(TestUtils.ParseHex(test.ReceiverSecret));
				var expectedReceiverAddress = new BitcoinStealthAddress(test.ReceiverAddress, Network.Main);
				var actualReceiverAddress = receiverKey.PubKey.CreateStealthAddress(Network.Main);
				Assert.Equal(expectedReceiverAddress.ToString(), actualReceiverAddress.ToString());
			}
		}

		[Fact]
		public void CanPayToStealthAddress()
		{
			for(int i = 0 ; i < 5 ; i++)
			{
				var receiverKey = new Key();
				var stealth = receiverKey.PubKey.CreateStealthAddress(Network.Main);

				var senderKey = new Key();
				var senderNonce = stealth.GetNonce(senderKey);
				var receiverNonce = stealth.GetNonce(receiverKey, senderKey.PubKey);
				Assert.Equal(senderNonce.ToString(), receiverNonce.ToString());

				Assert.Equal(senderNonce.DestinationAddress.ToString(), receiverNonce.DestinationAddress.ToString());
				Assert.Equal(senderNonce.StealthKey.ToString(), receiverNonce.StealthKey.ToString());
				Assert.Null(senderNonce.Key);
				Assert.NotNull(receiverNonce.Key);
				Assert.Equal(receiverNonce.Key.PubKey.GetAddress(stealth.Network).ToString(),
							receiverNonce.DestinationAddress.ToString());

				Assert.False(senderNonce.DeriveKey(senderKey));
			}
		}
	}
}
