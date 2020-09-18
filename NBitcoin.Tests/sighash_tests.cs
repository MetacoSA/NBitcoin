using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class sighash_tests
	{
		// Goal: check that SignatureHash generates correct hash
		[Fact]
		[Trait("Core", "Core")]
		public void sighash_from_data()
		{
			var tests = TestCase.read_json("data/sighash.json");

			foreach (var test in tests)
			{
				var strTest = test.ToString();
				if (test.Count < 1) // Allow for extra stuff (useful for comments)
				{
					Assert.True(false, "Bad test: " + strTest);
					continue;
				}
				if (test.Count == 1)
					continue; // comment

				string raw_tx, raw_script, sigHashHex;
				int nIn, nHashType;
				Transaction tx = Network.Main.CreateTransaction();
				Script scriptCode = new Script();


				// deserialize test data
				raw_tx = (string)test[0];
				raw_script = (string)test[1];
				nIn = (int)(long)test[2];
				nHashType = (int)(long)test[3];
				sigHashHex = (string)test[4];


				tx.ReadWrite(ParseHex(raw_tx), Network.Main);

				var raw = ParseHex(raw_script);
				scriptCode = new Script(raw);

				var sh = tx.GetSignatureHash(scriptCode, nIn, (SigHash)nHashType);
				Assert.True(sh.ToString() == sigHashHex, strTest);
			}
		}
		private byte[] ParseHex(string data)
		{
			return Encoders.Hex.DecodeData(data);
		}
	}
}
