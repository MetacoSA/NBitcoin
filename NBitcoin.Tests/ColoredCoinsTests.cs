using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//https://github.com/OpenAssets/open-assets-protocol/blob/master/specification.mediawiki
	public class ColoredCoinsTests
	{
		[Fact]
		public void CanCreateAssetAddress()
		{
			//The issuer first generates a private key: 18E14A7B6A307F426A94F8114701E7C8E774E7F9A47E2C2035DB29A206321725.
			var key = new Key(TestUtils.ParseHex("18E14A7B6A307F426A94F8114701E7C8E774E7F9A47E2C2035DB29A206321725"));
			//He calculates the corresponding address: 16UwLL9Risc3QfPqBUvKofHmBQ7wMtjvM.
			var address = key.PubKey.Decompress().GetAddress(Network.Main);
			Assert.Equal("16UwLL9Risc3QfPqBUvKofHmBQ7wMtjvM", address.ToString());

			//Next, he builds the Pay-to-PubKey-Hash script associated to that address: OP_DUP OP_HASH160 010966776006953D5567439E5E39F86A0D273BEE OP_EQUALVERIFY OP_CHECKSIG
			Script script = Script.CreateFromDestinationAddress(address);
			Assert.Equal("OP_DUP OP_HASH160 010966776006953D5567439E5E39F86A0D273BEE OP_EQUALVERIFY OP_CHECKSIG", script.ToString().ToUpper());

			//36hBrMeUfevFPZdY2iYSHVaP9jdLd9Np4R.
			var scriptAddress = script.GetScriptAddress(Network.Main);
			Assert.Equal("36hBrMeUfevFPZdY2iYSHVaP9jdLd9Np4R", scriptAddress.ToString());


			Assert.Equal("36hBrMeUfevFPZdY2iYSHVaP9jdLd9Np4R",key.PubKey.Decompress().GetScriptAddress(Network.Main).ToString());
		}

	}
}
