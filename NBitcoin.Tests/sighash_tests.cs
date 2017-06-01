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

		static Random rand = new Random();

		static Script RandomScript()
		{
			OpcodeType[] oplist = { OpcodeType.OP_FALSE, OpcodeType.OP_1, OpcodeType.OP_2, OpcodeType.OP_3, OpcodeType.OP_CHECKSIG, OpcodeType.OP_IF, OpcodeType.OP_VERIF, OpcodeType.OP_RETURN, OpcodeType.OP_CODESEPARATOR };
			var script = new Script();
			int ops = (rand.Next() % 10);
			for(int i = 0; i < ops; i++)
				script += oplist[rand.Next() % oplist.Length];

			return script;
		}


		//Compare between new old implementation of signature in reference bitcoin. But NBitcoin is like the old one, so we don't care about this test
		//[Fact]
		//public void sighash_test()
		//{

		//	int nRandomTests = 50000;


		//	for(int i = 0 ; i < nRandomTests ; i++)
		//	{
		//		int nHashType = rand.Next();
		//		Transaction txTo = RandomTransaction((nHashType & 0x1f) == SigHash.Single);
		//		Script scriptCode = RandomScript();
		//		int nIn = rand.Next() % txTo.VIn.Length;

		//		var sho = SignatureHashOld(scriptCode, txTo, nIn, nHashType);
		//		var sh = scriptCode.SignatureHash(txTo, nIn, (SigHash)nHashType);

		//		Assert.True(sh == sho);
		//	}
		//}

		// Goal: check that SignatureHash generates correct hash
		[Fact]
		[Trait("Core", "Core")]
		public void sighash_from_data()
		{
			var tests = TestCase.read_json("Data/sighash.json");

			foreach(var test in tests)
			{
				var strTest = test.ToString();
				if(test.Count < 1) // Allow for extra stuff (useful for comments)
				{
					Assert.True(false, "Bad test: " + strTest);
					continue;
				}
				if(test.Count == 1)
					continue; // comment

				string raw_tx, raw_script, sigHashHex;
				int nIn, nHashType;
				Transaction tx = new Transaction();
				Script scriptCode = new Script();


				// deserialize test data
				raw_tx = (string)test[0];
				raw_script = (string)test[1];
				nIn = (int)(long)test[2];
				nHashType = (int)(long)test[3];
				sigHashHex = (string)test[4];


				tx.ReadWrite(ParseHex(raw_tx));				

				var raw = ParseHex(raw_script);
				scriptCode = new Script(raw);

				var sh = Script.SignatureHash(scriptCode, tx, nIn, (SigHash)nHashType);
				Assert.True(sh.ToString() == sigHashHex, strTest);
			}
		}
		private byte[] ParseHex(string data)
		{
			return Encoders.Hex.DecodeData(data);
		}
	}
}
