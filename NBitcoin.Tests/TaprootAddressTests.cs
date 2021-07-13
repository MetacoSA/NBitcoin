using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace NBitcoin.Tests
{
	public class TaprootAddressTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseTaprootAddress()
		{
			var a = BitcoinAddress.Create("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", Network.Main);
			var address = Assert.IsType<TaprootAddress>(a);
			Assert.Equal("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", address.ToString());
			var addressOriginal = (TaprootAddress)address;
			a = address.ScriptPubKey.GetDestinationAddress(Network.Main);
			address = Assert.IsType<TaprootAddress>(a);
			Assert.Equal("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", address.ToString());
			Assert.Equal(address, addressOriginal);
			Assert.Equal(address.PubKey, addressOriginal.PubKey);
			Assert.True(address.PubKey == addressOriginal.PubKey);
			Assert.True(address.PubKey.GetHashCode() == addressOriginal.PubKey.GetHashCode());

			var address2 = new TaprootPubKey(new byte[32] {
		0x58, 0x84, 0xb3, 0xa2, 0x4b, 0x97, 0x37, 0x88, 0x92, 0x38, 0xa6, 0x26, 0x62, 0x52, 0x35, 0x11,
		0xd0, 0x9a, 0xa1, 0x1b, 0x80, 0x0b, 0x5e, 0x93, 0x80, 0x26, 0x11, 0xef, 0x67, 0x4b, 0xd9, 0x23
	}).GetAddress(Network.Main);
			Assert.NotEqual(address2, address);
			Assert.NotEqual(address2.PubKey, address.PubKey);
			Assert.False(address2.PubKey == address.PubKey);
			Assert.True(address2.PubKey != address.PubKey);
			Assert.False(address2.PubKey.GetHashCode() == address.PubKey.GetHashCode());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanExtractTaprootScript()
		{
			var a = BitcoinAddress.Create("bc1p0xlxvlhemja6c4dqv22uapctqupfhlxm9h8z3k2e72q4k9hcz7vqzk5jj0", Network.Main);
			var res = PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(a.ScriptPubKey);
			Assert.Equal(OpcodeType.OP_1, res.Version);
		}

#if HAS_SPAN
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGenerateTaprootPubKey()
		{
			var mnemo = new Mnemonic("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about");
			var root = mnemo.DeriveExtKey();

			// From BIP86
			foreach (var v in new[]
			{
				("bc1p5cyxnuxmeuwuvkwfem96lqzszd02n6xdcjrs20cac6yqjjwudpxqkedrcr", "m/86'/0'/0'/0/0") ,
				("bc1p4qhjn9zdvkux4e44uhx8tc55attvtyu358kutcqkudyccelu0was9fqzwh", "m/86'/0'/0'/0/1"),
				("bc1p3qkhfews2uk44qtvauqyr2ttdsw7svhkl9nkm9s9c3x4ax5h60wqwruhk7", "m/86'/0'/0'/1/0")
			})
			{
				var privKey = root.Derive(KeyPath.Parse(v.Item2));
				var address = privKey.GetPublicKey().GetAddress(ScriptPubKeyType.TaprootBIP86, Network.Main);
				Assert.Equal(v.Item1, address.ToString());
			}
		}
#endif
	}
}
