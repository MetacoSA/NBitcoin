using NBitcoin.DataEncoders;
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
		[Trait("UnitTest", "UnitTest")]
		public void CanSignTransaction()
		{
			var key = new Key();
			PayToPubkeyHashTemplate template = new PayToPubkeyHashTemplate();
			var scriptPubKey = template.GenerateScriptPubKey(key.PubKey);

			Transaction tx = new Transaction();
			tx.AddInput(new TxIn(new OutPoint(tx.GetHash(), 0)));
			tx.AddInput(new TxIn(new OutPoint(tx.GetHash(), 1)));
			tx.AddOutput(new TxOut("21", key.PubKey.ID));
			tx.SignAll(key);

			AssertCorrectlySigned(tx, scriptPubKey);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://gist.github.com/gavinandresen/3966071
		public void CanPartiallySignTransaction()
		{
			var privKeys = new[]{"5JaTXbAUmfPYZFRwrYaALK48fN6sFJp4rHqq2QSXs8ucfpE4yQU",
						"5Jb7fCeh1Wtm4yBBg3q3XbT6B525i17kVhy3vMC9AqfR6FH2qGk",
						"5JFjmGo5Fww9p8gvx48qBYDJNAzR9pmH5S389axMtDyPT8ddqmw"}
						.Select(k => new BitcoinSecret(k).Key).ToArray();

			//First: combine the three keys into a multisig address
			var multiSigTemplate = new PayToMultiSigTemplate();
			var redeem = multiSigTemplate.GenerateScriptPubKey(2, privKeys.Select(k => k.PubKey).ToArray());
			var scriptAddress = redeem.ID.GetAddress(Network.Main);
			Assert.Equal("3QJmV3qfvL9SuYo34YihAf3sRCW3qSinyC", scriptAddress.ToString());

			// Next, create a transaction to send funds into that multisig. Transaction d6f72... is
			// an unspent transaction in my wallet (which I got from the 'listunspent' RPC call):
			// Taken from example
			var fundingTransaction = new Transaction("010000000189632848f99722915727c5c75da8db2dbf194342a0429828f66ff88fab2af7d6000000008b483045022100abbc8a73fe2054480bda3f3281da2d0c51e2841391abd4c09f4f908a2034c18d02205bc9e4d68eafb918f3e9662338647a4419c0de1a650ab8983f1d216e2a31d8e30141046f55d7adeff6011c7eac294fe540c57830be80e9355c83869c9260a4b8bf4767a66bacbd70b804dc63d5beeb14180292ad7f3b083372b1d02d7a37dd97ff5c9effffffff0140420f000000000017a914f815b036d9bbbce5e9f2a00abd1bf3dc91e955108700000000");

			// Create the spend-from-multisig transaction. Since the fund-the-multisig transaction
			// hasn't been sent yet, I need to give txid, scriptPubKey and redeemScript:
			var spendTransaction = new Transaction();
			spendTransaction.Inputs.Add(new TxIn()
			{
				PrevOut = new OutPoint(fundingTransaction.GetHash(), 0),
			});
			spendTransaction.Outputs.Add(new TxOut()
			{
				Value = "0.01000000",
				ScriptPubKey = new Script("OP_DUP OP_HASH160 ae56b4db13554d321c402db3961187aed1bbed5b OP_EQUALVERIFY OP_CHECKSIG")
			});

			//... Now I can partially sign it using one private key:

			var partiallySigned = spendTransaction.CreatePartiallySignedTransaction(redeem);
			partiallySigned.SignAll(privKeys[0]);

			//the other private keys (note the "hex" result getting longer):
			partiallySigned.SignAll(privKeys[1]);

			var signed = partiallySigned.BuildSignedTransaction();
			AssertCorrectlySigned(signed, fundingTransaction.Outputs[0].ScriptPubKey);

			//Verify the transaction from the gist is also correctly signed
			var gistTransaction = new Transaction("0100000001aca7f3b45654c230e0886a57fb988c3044ef5e8f7f39726d305c61d5e818903c00000000fd5d010048304502200187af928e9d155c4b1ac9c1c9118153239aba76774f775d7c1f9c3e106ff33c0221008822b0f658edec22274d0b6ae9de10ebf2da06b1bbdaaba4e50eb078f39e3d78014730440220795f0f4f5941a77ae032ecb9e33753788d7eb5cb0c78d805575d6b00a1d9bfed02203e1f4ad9332d1416ae01e27038e945bc9db59c732728a383a6f1ed2fb99da7a4014cc952410491bba2510912a5bd37da1fb5b1673010e43d2c6d812c514e91bfa9f2eb129e1c183329db55bd868e209aac2fbc02cb33d98fe74bf23f0c235d6126b1d8334f864104865c40293a680cb9c020e7b1e106d8c1916d3cef99aa431a56d253e69256dac09ef122b1a986818a7cb624532f062c1d1f8722084861c5c3291ccffef4ec687441048d2455d2403e08708fc1f556002f1b6cd83f992d085097f9974ab08a28838f07896fbab08f39495e15fa6fad6edbfb1e754e35fa1c7844c41f322a1863d4621353aeffffffff0140420f00000000001976a914ae56b4db13554d321c402db3961187aed1bbed5b88ac00000000");
			AssertCorrectlySigned(gistTransaction, fundingTransaction.Outputs[0].ScriptPubKey);

			//Can sign out of order
			partiallySigned = spendTransaction.CreatePartiallySignedTransaction(redeem);
			partiallySigned.SignAll(privKeys[2]);
			partiallySigned.SignAll(privKeys[0]);
			signed = partiallySigned.BuildSignedTransaction();
			AssertCorrectlySigned(signed, fundingTransaction.Outputs[0].ScriptPubKey);

			//Can clone partially signed transaction
			partiallySigned = partiallySigned.Clone();
			signed = partiallySigned.BuildSignedTransaction();
			AssertCorrectlySigned(signed, fundingTransaction.Outputs[0].ScriptPubKey);

			//Can sign multiple inputs
			spendTransaction.Inputs.Add(new TxIn()
			{
				PrevOut = new OutPoint(fundingTransaction.GetHash(), 1),
			});
			partiallySigned = spendTransaction.CreatePartiallySignedTransaction(redeem);
			partiallySigned.SignAll(privKeys[2]);
			partiallySigned.SignAll(privKeys[0]);
			signed = partiallySigned.BuildSignedTransaction();
			AssertCorrectlySigned(signed, fundingTransaction.Outputs[0].ScriptPubKey);
		}

		private void AssertCorrectlySigned(Transaction tx, Script scriptPubKey)
		{
			for(int i = 0 ; i < tx.Inputs.Count ; i++)
			{
				Assert.True(Script.VerifyScript(tx.Inputs[i].ScriptSig, scriptPubKey, tx, i));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//http://brainwallet.org/#tx
		public void CanParseTransaction()
		{
			var tests = TestCase.read_json("data/can_parse_transaction.json");

			foreach(var test in tests.Select(t => t.GetDynamic(0)))
			{
				string raw = test.Raw;
				Transaction tx = new Transaction(raw);
				Assert.Equal((int)test.JSON.vin_sz, tx.Inputs.Count);
				Assert.Equal((int)test.JSON.vout_sz, tx.Outputs.Count);
				Assert.Equal((uint)test.JSON.lock_time, tx.LockTime);

				for(int i = 0 ; i < tx.Inputs.Count ; i++)
				{
					var actualVIn = tx.Inputs[i];
					var expectedVIn = test.JSON.@in[i];
					Assert.Equal(new uint256((string)expectedVIn.prev_out.hash), actualVIn.PrevOut.Hash);
					Assert.Equal((uint)expectedVIn.prev_out.n, actualVIn.PrevOut.N);
					if(expectedVIn.sequence != null)
						Assert.Equal((uint)expectedVIn.sequence, actualVIn.Sequence);
					Assert.Equal((string)expectedVIn.scriptSig, actualVIn.ScriptSig.ToString());
					//Can parse the string
					Assert.Equal((string)expectedVIn.scriptSig, (string)expectedVIn.scriptSig.ToString());
				}

				for(int i = 0 ; i < tx.Outputs.Count ; i++)
				{
					var actualVOut = tx.Outputs[i];
					var expectedVOut = test.JSON.@out[i];
					Assert.Equal((string)expectedVOut.scriptPubKey, actualVOut.ScriptPubKey.ToString());
					Assert.Equal(Money.Parse((string)expectedVOut.value), actualVOut.Value);
				}
				var hash = (string)test.JSON.hash;
				var expectedHash = new uint256(Encoders.Hex.DecodeData(hash), false);
				Assert.Equal(expectedHash, tx.GetHash());
			}
		}


		[Fact]
		[Trait("Core", "Core")]
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
				if(test.Count != 3 || !(test[1] is string) || !(test[2] is string))
				{
					Assert.False(true, "Bad test: " + strTest);
					continue;
				}

				Dictionary<OutPoint, Script> mapprevOutScriptPubKeys = new Dictionary<OutPoint, Script>();
				foreach(var vinput in inputs)
				{
					mapprevOutScriptPubKeys[new OutPoint(new uint256(vinput[0].ToString()), int.Parse(vinput[1].ToString()))] = script_tests.ParseScript(vinput[2].ToString());
				}

				Transaction tx = new Transaction((string)test[1]);
				ValidationState state = Network.Main.CreateValidationState();
				Assert.True(state.CheckTransaction(tx), strTest);
				Assert.True(state.IsValid);


				for(int i = 0 ; i < tx.Inputs.Count ; i++)
				{
					if(!mapprevOutScriptPubKeys.ContainsKey(tx.Inputs[i].PrevOut))
					{
						Assert.False(true, "Bad test: " + strTest);
						continue;
					}

					var valid = Script.VerifyScript(
						tx.Inputs[i].ScriptSig,
						mapprevOutScriptPubKeys[tx.Inputs[i].PrevOut],
						tx,
						i,
						ParseFlags(test[2].ToString())
						, 0);
					Assert.True(valid, strTest + " failed");
				}
			}


		}

		ScriptVerify ParseFlags(string strFlags)
		{
			ScriptVerify flags = 0;
			var words = strFlags.Split(',');


			// Note how NOCACHE is not included as it is a runtime-only flag.
			Dictionary<string, ScriptVerify> mapFlagNames = new Dictionary<string, ScriptVerify>();
			if(mapFlagNames.Count == 0)
			{
				mapFlagNames["NONE"] = ScriptVerify.None;
				mapFlagNames["P2SH"] = ScriptVerify.P2SH;
				mapFlagNames["STRICTENC"] = ScriptVerify.StrictEnc;
				mapFlagNames["LOW_S"] = ScriptVerify.LowS;
				mapFlagNames["NULLDUMMY"] = ScriptVerify.NullDummy;
			}

			foreach(string word in words)
			{
				if(!mapFlagNames.ContainsKey(word))
					Assert.False(true, "Bad test: unknown verification flag '" + word + "'");
				flags |= mapFlagNames[word];
			}

			return flags;
		}

		[Fact]
		[Trait("Core", "Core")]
		public void tx_invalid()
		{
			// Read tests from test/data/tx_valid.json
			// Format is an array of arrays
			// Inner arrays are either [ "comment" ]
			// or [[[prevout hash, prevout index, prevout scriptPubKey], [input 2], ...],"], serializedTransaction, enforceP2SH
			// ... where all scripts are stringified scripts.
			var tests = TestCase.read_json("data/tx_invalid.json");
			foreach(var test in tests)
			{
				string strTest = test.ToString();
				//Skip comments
				if(!(test[0] is JArray))
					continue;
				JArray inputs = (JArray)test[0];
				if(test.Count != 3 || !(test[1] is string) || !(test[2] is string))
				{
					Assert.False(true, "Bad test: " + strTest);
					continue;
				}

				Dictionary<OutPoint, Script> mapprevOutScriptPubKeys = new Dictionary<OutPoint, Script>();
				foreach(var vinput in inputs)
				{
					mapprevOutScriptPubKeys[new OutPoint(new uint256(vinput[0].ToString()), int.Parse(vinput[1].ToString()))] = script_tests.ParseScript(vinput[2].ToString());
				}

				Transaction tx = new Transaction((string)test[1]);

				ValidationState state = Network.Main.CreateValidationState();
				var fValid = state.CheckTransaction(tx) && state.IsValid;

				for(int i = 0 ; i < tx.Inputs.Count && fValid ; i++)
				{
					if(!mapprevOutScriptPubKeys.ContainsKey(tx.Inputs[i].PrevOut))
					{
						Assert.False(true, "Bad test: " + strTest);
						continue;
					}

					fValid = Script.VerifyScript(
					   tx.Inputs[i].ScriptSig,
					   mapprevOutScriptPubKeys[tx.Inputs[i].PrevOut],
					   tx,
					   i,
					   ParseFlags(test[2].ToString())
					   , 0);
				}
				Assert.True(!fValid, strTest + " failed");
			}


		}

		[Fact]
		[Trait("Core", "Core")]
		public void basic_transaction_tests()
		{
			// Random real transaction (e2769b09e784f32f62ef849763d4f45b98e07ba658647343b915ff832b110436)
			var ch = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x01, 0x6b, 0xff, 0x7f, 0xcd, 0x4f, 0x85, 0x65, 0xef, 0x40, 0x6d, 0xd5, 0xd6, 0x3d, 0x4f, 0xf9, 0x4f, 0x31, 0x8f, 0xe8, 0x20, 0x27, 0xfd, 0x4d, 0xc4, 0x51, 0xb0, 0x44, 0x74, 0x01, 0x9f, 0x74, 0xb4, 0x00, 0x00, 0x00, 0x00, 0x8c, 0x49, 0x30, 0x46, 0x02, 0x21, 0x00, 0xda, 0x0d, 0xc6, 0xae, 0xce, 0xfe, 0x1e, 0x06, 0xef, 0xdf, 0x05, 0x77, 0x37, 0x57, 0xde, 0xb1, 0x68, 0x82, 0x09, 0x30, 0xe3, 0xb0, 0xd0, 0x3f, 0x46, 0xf5, 0xfc, 0xf1, 0x50, 0xbf, 0x99, 0x0c, 0x02, 0x21, 0x00, 0xd2, 0x5b, 0x5c, 0x87, 0x04, 0x00, 0x76, 0xe4, 0xf2, 0x53, 0xf8, 0x26, 0x2e, 0x76, 0x3e, 0x2d, 0xd5, 0x1e, 0x7f, 0xf0, 0xbe, 0x15, 0x77, 0x27, 0xc4, 0xbc, 0x42, 0x80, 0x7f, 0x17, 0xbd, 0x39, 0x01, 0x41, 0x04, 0xe6, 0xc2, 0x6e, 0xf6, 0x7d, 0xc6, 0x10, 0xd2, 0xcd, 0x19, 0x24, 0x84, 0x78, 0x9a, 0x6c, 0xf9, 0xae, 0xa9, 0x93, 0x0b, 0x94, 0x4b, 0x7e, 0x2d, 0xb5, 0x34, 0x2b, 0x9d, 0x9e, 0x5b, 0x9f, 0xf7, 0x9a, 0xff, 0x9a, 0x2e, 0xe1, 0x97, 0x8d, 0xd7, 0xfd, 0x01, 0xdf, 0xc5, 0x22, 0xee, 0x02, 0x28, 0x3d, 0x3b, 0x06, 0xa9, 0xd0, 0x3a, 0xcf, 0x80, 0x96, 0x96, 0x8d, 0x7d, 0xbb, 0x0f, 0x91, 0x78, 0xff, 0xff, 0xff, 0xff, 0x02, 0x8b, 0xa7, 0x94, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x19, 0x76, 0xa9, 0x14, 0xba, 0xde, 0xec, 0xfd, 0xef, 0x05, 0x07, 0x24, 0x7f, 0xc8, 0xf7, 0x42, 0x41, 0xd7, 0x3b, 0xc0, 0x39, 0x97, 0x2d, 0x7b, 0x88, 0xac, 0x40, 0x94, 0xa8, 0x02, 0x00, 0x00, 0x00, 0x00, 0x19, 0x76, 0xa9, 0x14, 0xc1, 0x09, 0x32, 0x48, 0x3f, 0xec, 0x93, 0xed, 0x51, 0xf5, 0xfe, 0x95, 0xe7, 0x25, 0x59, 0xf2, 0xcc, 0x70, 0x43, 0xf9, 0x88, 0xac, 0x00, 0x00, 0x00, 0x00, 0x00 };
			var vch = ch.Take(ch.Length - 1).ToArray();

			Transaction tx = new Transaction(vch);
			ValidationState state = Network.Main.CreateValidationState();
			Assert.True(state.CheckTransaction(tx) && state.IsValid, "Simple deserialized transaction should be valid.");

			// Check that duplicate txins fail
			tx.Inputs.Add(tx.Inputs[0]);
			Assert.True(!state.CheckTransaction(tx) || !state.IsValid, "Transaction with duplicate txins should be invalid.");
		}

		[Fact]
		[Trait("Core", "Core")]
		public void test_Get()
		{
			//CBasicKeyStore keystore;
			//CCoinsView coinsDummy;
			CoinsView coins = new CoinsView();//(coinsDummy);
			Transaction[] dummyTransactions = SetupDummyInputs(coins);//(keystore, coins);

			Transaction t1 = new Transaction();
			t1.Inputs.AddRange(Enumerable.Range(0, 3).Select(_ => new TxIn()));
			t1.Inputs[0].PrevOut.Hash = dummyTransactions[0].GetHash();
			t1.Inputs[0].PrevOut.N = 1;
			t1.Inputs[0].ScriptSig += new byte[65];
			t1.Inputs[1].PrevOut.Hash = dummyTransactions[1].GetHash();
			t1.Inputs[1].PrevOut.N = 0;
			t1.Inputs[1].ScriptSig = t1.Inputs[1].ScriptSig + new byte[65] + Enumerable.Range(0, 33).Select(_ => (byte)4);
			t1.Inputs[2].PrevOut.Hash = dummyTransactions[1].GetHash();
			t1.Inputs[2].PrevOut.N = 1;
			t1.Inputs[2].ScriptSig = t1.Inputs[2].ScriptSig + new byte[65] + Enumerable.Range(0, 33).Select(_ => (byte)4);
			t1.Outputs.AddRange(Enumerable.Range(0, 2).Select(_ => new TxOut()));
			t1.Outputs[0].Value = 90 * Money.CENT;
			t1.Outputs[0].ScriptPubKey += OpcodeType.OP_1;

			Assert.True(StandardScripts.AreInputsStandard(t1, coins));
			//Assert.Equal(coins.GetValueIn(t1), (50+21+22)*Money.CENT);

			//// Adding extra junk to the scriptSig should make it non-standard:
			t1.Inputs[0].ScriptSig += OpcodeType.OP_11;
			Assert.True(!StandardScripts.AreInputsStandard(t1, coins));

			//// ... as should not having enough:
			t1.Inputs[0].ScriptSig = new Script();
			Assert.True(!StandardScripts.AreInputsStandard(t1, coins));
		}

		private Transaction[] SetupDummyInputs(CoinsView coinsRet)
		{
			Transaction[] dummyTransactions = Enumerable.Range(0, 2).Select(_ => new Transaction()).ToArray();

			// Add some keys to the keystore:
			Key[] key = Enumerable.Range(0, 4).Select((_, i) => new Key(i % 2 != 0)).ToArray();


			// Create some dummy input transactions
			dummyTransactions[0].Outputs.AddRange(Enumerable.Range(0, 2).Select(_ => new TxOut()));
			dummyTransactions[0].Outputs[0].Value = 11 * Money.CENT;
			dummyTransactions[0].Outputs[0].ScriptPubKey = dummyTransactions[0].Outputs[0].ScriptPubKey + key[0].PubKey.ToBytes() + OpcodeType.OP_CHECKSIG;
			dummyTransactions[0].Outputs[1].Value = 50 * Money.CENT;
			dummyTransactions[0].Outputs[1].ScriptPubKey = dummyTransactions[0].Outputs[1].ScriptPubKey + key[1].PubKey.ToBytes() + OpcodeType.OP_CHECKSIG;
			coinsRet.AddTransaction(dummyTransactions[0], 0);


			dummyTransactions[1].Outputs.AddRange(Enumerable.Range(0, 2).Select(_ => new TxOut()));
			dummyTransactions[1].Outputs[0].Value = 21 * Money.CENT;
			dummyTransactions[1].Outputs[0].ScriptPubKey = StandardScripts.PayToAddress(key[2].PubKey.GetAddress(Network.Main));
			dummyTransactions[1].Outputs[1].Value = 22 * Money.CENT;
			dummyTransactions[1].Outputs[1].ScriptPubKey = StandardScripts.PayToAddress(key[3].PubKey.GetAddress(Network.Main));
			coinsRet.AddTransaction(dummyTransactions[1], 0);


			return dummyTransactions;
		}


		[Fact]
		[Trait("Core", "Core")]
		public void test_IsStandard()
		{
			var coins = new CoinsView();
			Transaction[] dummyTransactions = SetupDummyInputs(coins);

			Transaction t = new Transaction();
			t.Inputs.Add(new TxIn());
			t.Inputs[0].PrevOut.Hash = dummyTransactions[0].GetHash();
			t.Inputs[0].PrevOut.N = 1;
			t.Inputs[0].ScriptSig = new Script(Op.GetPushOp(new byte[65]));
			t.Outputs.Add(new TxOut());
			t.Outputs[0].Value = 90 * Money.CENT;
			Key key = new Key(true);
			var payToHash = new PayToPubkeyHashTemplate();
			t.Outputs[0].ScriptPubKey = payToHash.GenerateScriptPubKey(key.PubKey.ID);

			Assert.True(StandardScripts.IsStandardTransaction(t));

			t.Outputs[0].Value = 501; //dust
			Assert.True(!StandardScripts.IsStandardTransaction(t));

			t.Outputs[0].Value = 601; // not dust
			Assert.True(StandardScripts.IsStandardTransaction(t));

			t.Outputs[0].ScriptPubKey = new Script() + OpcodeType.OP_1;
			Assert.True(!StandardScripts.IsStandardTransaction(t));

			// 40-byte TX_NULL_DATA (standard)
			t.Outputs[0].ScriptPubKey = new Script() + OpcodeType.OP_RETURN + ParseHex("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38");
			Assert.True(StandardScripts.IsStandardTransaction(t));

			// 41-byte TX_NULL_DATA (non-standard)
			t.Outputs[0].ScriptPubKey = new Script() + OpcodeType.OP_RETURN + ParseHex("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef3800");
			Assert.True(!StandardScripts.IsStandardTransaction(t));

			// TX_NULL_DATA w/o PUSHDATA
			t.Outputs.Clear();
			t.Outputs.Add(new TxOut());
			t.Outputs[0].ScriptPubKey = new Script() + OpcodeType.OP_RETURN;
			Assert.True(StandardScripts.IsStandardTransaction(t));

			// Only one TX_NULL_DATA permitted in all cases
			t.Outputs.Add(new TxOut());
			t.Outputs.Add(new TxOut());
			t.Outputs[0].ScriptPubKey = new Script() + OpcodeType.OP_RETURN + ParseHex("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38");
			t.Outputs[1].ScriptPubKey = new Script() + OpcodeType.OP_RETURN + ParseHex("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38");
			Assert.True(!StandardScripts.IsStandardTransaction(t));

			t.Outputs[0].ScriptPubKey = new Script() + OpcodeType.OP_RETURN + ParseHex("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38");
			t.Outputs[1].ScriptPubKey = new Script() + OpcodeType.OP_RETURN;
			Assert.True(!StandardScripts.IsStandardTransaction(t));

			t.Outputs[0].ScriptPubKey = new Script() + OpcodeType.OP_RETURN;
			t.Outputs[1].ScriptPubKey = new Script() + OpcodeType.OP_RETURN;
			Assert.True(!StandardScripts.IsStandardTransaction(t));
		}

		private byte[] ParseHex(string data)
		{
			return Encoders.Hex.DecodeData(data);
		}
	}
}
