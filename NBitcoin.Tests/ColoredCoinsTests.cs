using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
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
		//https://www.coinprism.info/tx/b4399a545c4ddd640920d63af75e7367fe4d94b2d7f7a3423105e25ac5f165a6
		string Tx1 = "0100000002e68f7cca21a220b8a02b01d4d4e791eee3b7a14ae61db46dbde5900e91ac2c13020000006a47304402205fe2ffdd84a93e92ff6da1d35bfc72e60b3a01a8ef00d7fc706f9aa4d0f1d3cf02202e2ee316918411e2850003625949e854f233f2b2649d89499c6ca4a4f69d315301210335cf4bc6ce5905ea028fd9bbe7f9c23941a222a846201e628f75920fd8858a29ffffffffe68f7cca21a220b8a02b01d4d4e791eee3b7a14ae61db46dbde5900e91ac2c13030000006a47304402200254f50976742069dff75597fa6930b79edd2ae1b1ee5dd5afead0d67b4f802702205b2e0e5e6beca31c31d6f239f155ac61d6517c551a913e7bc2993f0d0076c99c01210335cf4bc6ce5905ea028fd9bbe7f9c23941a222a846201e628f75920fd8858a29ffffffff0400000000000000000e6a0c4f41010002e419fffa9d040058020000000000001976a914501dee91d85c096c57bd8d6abea0a1718560a71888ac58020000000000001976a91477e3e6acdeca221685d0d23a12989b96335a463988ac00384a00000000001976a91477e3e6acdeca221685d0d23a12989b96335a463988ac00000000";


		//Data in the marker output      Description
		//-----------------------------  -------------------------------------------------------------------
		//0x6a                           The OP_RETURN opcode.
		//0x10                           The PUSHDATA opcode for a 16 bytes payload.
		//0x4f 0x41                      The Open Assets Protocol tag.
		//0x01 0x00                      Version 1 of the protocol.
		//0x03                           There are 3 items in the asset quantity list.
		//0xac 0x02 0x00 0xe5 0x8e 0x26  The asset quantity list:
		//							   - '0xac 0x02' means output 0 has an asset quantity of 300.
		//							   - Output 1 is skipped and has an asset quantity of 0
		//								 because it is the marker output.
		//							   - '0x00' means output 2 has an asset quantity of 0.
		//							   - '0xe5 0x8e 0x26' means output 3 has an asset quantity of 624,485.
		//							   - Outputs after output 3 (if any) have an asset quantity of 0.
		//0x04                           The metadata is 4 bytes long.
		//0x12 0x34 0x56 0x78            Some arbitrary metadata.
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseOpenAssetPayload()
		{
			var script = new Script(Encoders.Hex.DecodeData("6a104f41010003ac0200e58e260412345678"));
			var payload = OpenAssetPayload.TryParse(script);
			Assert.NotNull(payload);
			Assert.Equal(1, payload.Version);
			Assert.Equal(3, payload.Quantities.Length);
			Assert.True(payload.Quantities.SequenceEqual(new ulong[] { 300, 0, 624485 }));
			Assert.True(payload.Metadata.SequenceEqual(new byte[] { 0x12, 0x34, 0x56, 0x78 }));

			Assert.Equal(script.ToString(), payload.GetScript().ToString());
		}

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


			Assert.Equal("36hBrMeUfevFPZdY2iYSHVaP9jdLd9Np4R", key.PubKey.Decompress().GetAddress(Network.Main).GetScriptAddress().ToString());
		}

	}
}
