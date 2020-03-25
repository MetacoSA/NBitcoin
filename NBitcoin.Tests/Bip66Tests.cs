using System;
using NBitcoin.Crypto;
using Xunit;
using System.Linq;

namespace NBitcoin.Tests
{
	public class Bip66Tests
	{
		// We want to ensure DER signatures from ECDSASignature are strict
		// BIP 66 DER. Strict sigs are a consensus rule. Imported non-strict
		// ought to be coerced to strict sigs, as well.

		// The interpreter is bip66 tested here: https://github.com/MetacoSA/NBitcoin/blob/d21f31311180041a15524588b443767cf95951ec/NBitcoin.Tests/data/script_tests.json#L702
		// We test the internal object so NBitcoin never exports non-BIP66.

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ECDSASignatureIsOnlyBip66DER()
		{
			var invalidBIP66DER = HexToByteArray("302402107777777777777777777777777777777702108777777777777777777777777777777701");
			// Encodes:
			// SEQUENCE {
			//	 INTEGER 0x77777777777777777777777777777777
			//   INTEGER 0x87777777777777777777777777777777 // -1 exponent :(
			// }
			// BOOLEAN FALSE
			var coercedSignature = new ECDSASignature(invalidBIP66DER);

			var validBIP66DER = HexToByteArray("302502107777777777777777777777777777777702110087777777777777777777777777777777");
			// Should coerce to this valid DER encoding:
			// SEQUENCE {
			//	 INTEGER 0x77777777777777777777777777777777
			//   INTEGER 0x0087777777777777777777777777777777 // +1 exponent :)
			// }
			Assert.True(coercedSignature.ToDER().SequenceEqual(validBIP66DER));
		}

		private static byte[] HexToByteArray(string hex)
		{
			byte[] bytes = new byte[hex.Length / 2];
			for (int i = 0; i < hex.Length; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}
	}
}
