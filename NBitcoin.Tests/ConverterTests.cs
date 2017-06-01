using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class ConverterTests
	{
		//http://brainwallet.org/#converter
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanConvertText()
		{
			string testPhrase = "é ^ç hello \"12345\"  wooorld";
			var tests = new[]
			{
				new
				{
					Encoder = Encoders.Hex,
					Input = testPhrase,
					Expected = "c3a9205ec3a72068656c6c6f20223132333435222020776f6f6f726c64",
				},
				new
				{
					Encoder = Encoders.Base58,
					Input = testPhrase,
					Expected = "9tBRc991GhmZNsV5qSyynUsnRCNvxdvvWDmj3nAP"
				},
				new
				{
					Encoder = Encoders.Base58Check,
					Input = testPhrase,
					//Different from brainwallet, because brainwallet code convert the data to bitcoin address instead of directely formating in base58check (ie : the data followed be the 4 hash bytes)
					Expected = "2189xoVGsHC6VbVPUrKeH3fhT429VDruzdgUJFk37PNskG"
				},
				new
				{
					Encoder = Encoders.Base64,
					Input = testPhrase,
					Expected = "w6kgXsOnIGhlbGxvICIxMjM0NSIgIHdvb29ybGQ="
				},
				//Not yet implemented
				//new 
				//{
				//	Encoder = Encoders.Bin,
				//	Input = testPhrase,
				//	Expected = "11000011 10101001 00100000 01011110 11000011 10100111 00100000 01101000 01100101 01101100 01101100 01101111 00100000 00100010 00110001 00110010 00110011 00110100 00110101 00100010 00100000 00100000 01110111 01101111 01101111 01101111 01110010 01101100 01100100"
				//},
				//Not yet implemented
				//new 
				//{
				//	Encoder = Encoders.Dec,
				//	Input = testPhrase,
				//	Expected = "5275000693703128425041367611933003709099386868005962673424426230508644"
				//},
				//Useless for bitcoin
				//new 
				//{
				//	Encoder = Encoders.RFC1751,
				//	Input = testPhrase,
				//	Expected = "A A OWE BANG BAN BUST KITE ARK HAT SEEN OBOE GRIM KIN GASH GLOB COAT BANE DUN JO MILL SIGH SLID MAD PAR"
				//},
				//Useless for bitcoin
				//new 
				//{
				//	Encoder = Encoders.Poetry,
				//	Input = testPhrase,
				//	Expected = "perfect perfect perfect soul stone royal fault companion sharp cross build leap possess possibly yet bone magic beam illuminate moonlight foul juice darkness universe"
				//},
				//Useless for bitcoin
				//new 
				//{
				//	Encoder = Encoders.Rot13,
				//	Input = testPhrase,
				//	Expected = "é ^ç uryyb \"12345\"  jbbbeyq"
				//},
				//Useless for bitcoin
				//new 
				//{
				//	Encoder = Encoders.Easy16,
				//	Input = testPhrase,
				//	Expected = "aaaa aauf reda houf rkda jwjh juju jnda eriu\r\nddfs fdff fgfh ddda dakk jnjn jnkd jujg euhs"
				//},
			};

			foreach(var test in tests)
			{
				var input = Encoding.UTF8.GetBytes(test.Input);
				var encoded = test.Encoder.EncodeData(input);
				Assert.Equal(test.Expected, encoded);

				try
				{
					var decoded = test.Encoder.DecodeData(encoded);
					AssertEx.CollectionEquals(input, decoded);
				}
				catch(NotSupportedException)
				{
				}
			}

			var expectedText = "2189xoVGsHC6VbVPUrKeH3fhT429VDruzdgUJFk37PNskG";
			var input1 = Encoding.UTF8.GetBytes("---é ^ç hello \"12345\"  wooorld---");
			var encoded1 = Encoders.Base58Check.EncodeData(input1, 3, input1.Length - 6);
			Assert.Equal(expectedText, encoded1);

			var decoded1 = Encoders.Base58Check.DecodeData(encoded1);
			byte[] arr = new byte[input1.Length - 6];
			Array.Copy(input1,3, arr, 0, arr.Length);
			AssertEx.CollectionEquals(input1.SafeSubarray(3, input1.Length - 6), decoded1);
		}
	}
}
