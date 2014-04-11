using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class transaction_tests
	{
		[Fact]
		//http://brainwallet.org/#tx
		public void CanParseTransaction()
		{
			var tests = TestCase.read_json("data/can_parse_transaction.json");

			foreach(var test in tests.Select(t=>t.GetDynamic(0)))
			{
				string raw  = test.Raw;
				Transaction tx = new Transaction(raw);
				Assert.Equal((int)test.JSON.vin_sz, tx.VIn.Length);
				Assert.Equal((int)test.JSON.vout_sz, tx.VOut.Length);
				Assert.Equal((uint)test.JSON.lock_time, tx.LockTime);

				for(int i = 0 ; i < tx.VIn.Length ; i++)
				{
					var actualVIn = tx.VIn[i];
					var expectedVIn = test.JSON.@in[i];
					Assert.Equal(new uint256((string)expectedVIn.prev_out.hash), actualVIn.PrevOut.Hash);
					Assert.Equal((uint)expectedVIn.prev_out.n, actualVIn.PrevOut.N);
					Assert.Equal((uint)expectedVIn.sequence, actualVIn.Sequence);
					Assert.Equal((string)expectedVIn.scriptSig, actualVIn.ScriptSig.ToString());
					//Can parse the string
					Assert.Equal((string)expectedVIn.scriptSig, new Script((string)expectedVIn.scriptSig).ToString());
				}

				for(int i = 0 ; i < tx.VOut.Length ; i++)
				{
					var actualVOut = tx.VOut[i];
					var expectedVOut = test.JSON.@out[i];
					Assert.Equal((string)expectedVOut.scriptPubKey, actualVOut.PublicKey.ToString());
					Assert.Equal(Money.Parse((string)expectedVOut.value), actualVOut.Value);
				}
			}
		}


		[Fact]
		public void tx_valid()
		{
			// Read tests from test/data/tx_valid.json
			// Format is an array of arrays
			// Inner arrays are either [ "comment" ]
			// or [[[prevout hash, prevout index, prevout scriptPubKey], [input 2], ...],"], serializedTransaction, enforceP2SH
			// ... where all scripts are stringified scripts.
			var tests = TestCase.read_json("data/tx_valid.json");
			foreach(var test in tests)
			{
				string strTest = test.ToString();
				//Skip comments
				if(!(test[0] is JArray))
					continue;
				JArray inputs = (JArray)test[0];
				if(test.Count != 3 || !(test[1] is string) || !(test[2] is bool))
				{
					Assert.False(true, "Bad test: " + strTest);
					continue;
				}

				Dictionary<OutPoint, Script> mapprevOutScriptPubKeys = new Dictionary<OutPoint, Script>();
				foreach(var vinput in inputs)
				{
					mapprevOutScriptPubKeys[new OutPoint(new uint256(vinput[0].ToString()), int.Parse(vinput[1].ToString()))] = new Script(vinput[2].ToString());
				}

				Transaction tx = new Transaction((string)test[1]);

				for(int i = 0 ; i < tx.VIn.Length ; i++)
				{
					if(!mapprevOutScriptPubKeys.ContainsKey(tx.VIn[i].PrevOut))
					{
						Assert.False(true, "Bad test: " + strTest);
						continue;
					}

					var valid = Script.VerifyScript(
						tx.VIn[i].ScriptSig,
						mapprevOutScriptPubKeys[tx.VIn[i].PrevOut],
						tx,
						i,
						bool.Parse(test[2].ToString()) ? ScriptVerify.P2SH : ScriptVerify.None
						, 0);
					Assert.True(valid, strTest + " failed");
				}
			}


		}
	}
}
