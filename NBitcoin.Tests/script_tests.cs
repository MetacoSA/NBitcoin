using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if !NOCONSENSUSLIB
using System.Net.Http;
#endif
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace NBitcoin.Tests
{
	public class script_tests
	{

		static Dictionary<string, OpcodeType> mapOpNames = new Dictionary<string, OpcodeType>();
		public static Script ParseScript(string s)
		{
			MemoryStream result = new MemoryStream();
			if (mapOpNames.Count == 0)
			{
				mapOpNames = new Dictionary<string, OpcodeType>(Op._OpcodeByName);
				foreach (var kv in mapOpNames.ToArray())
				{
					if (kv.Key.StartsWith("OP_", StringComparison.Ordinal))
					{
						var name = kv.Key.Substring(3, kv.Key.Length - 3);
						mapOpNames.AddOrReplace(name, kv.Value);
					}
				}
			}

			var words = s.Split(' ', '\t', '\n');

			foreach (string w in words)
			{
				if (w == "")
					continue;
				if (w.All(l => l.IsDigit()) ||
					(w.StartsWith("-") && w.Substring(1).All(l => l.IsDigit())))
				{

					// Number
					long n = long.Parse(w);
					Op.GetPushOp(n).WriteTo(result);
				}
				else if (w.StartsWith("0x") && HexEncoder.IsWellFormed(w.Substring(2)))
				{
					// Raw hex data, inserted NOT pushed onto stack:
					var raw = Encoders.Hex.DecodeData(w.Substring(2));
					result.Write(raw, 0, raw.Length);
				}
				else if (w.Length >= 2 && w.StartsWith("'") && w.EndsWith("'"))
				{
					// Single-quoted string, pushed as data. NOTE: this is poor-man's
					// parsing, spaces/tabs/newlines in single-quoted strings won't work.
					var b = TestUtils.ToBytes(w.Substring(1, w.Length - 2));
					Op.GetPushOp(b).WriteTo(result);
				}
				else if (mapOpNames.ContainsKey(w))
				{
					// opcode, e.g. OP_ADD or ADD:
					result.WriteByte((byte)mapOpNames[w]);
				}
				else
				{
					Assert.True(false, "Invalid test");
					return null;
				}
			}

			return new Script(result.ToArray());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseNOPs()
		{
			new Script("OP_NOP1 OP_NOP2 OP_NOP3 OP_NOP4 OP_NOP5 OP_NOP6 OP_NOP7 OP_NOP8 OP_NOP9");
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BIP65_tests()
		{
			BIP65_testsCore(
				Utils.UnixTimeToDateTime(510000000),
				Utils.UnixTimeToDateTime(509999999),
				false);
			BIP65_testsCore(
				Utils.UnixTimeToDateTime(510000000),
				Utils.UnixTimeToDateTime(510000000),
				true);
			BIP65_testsCore(
				Utils.UnixTimeToDateTime(510000000),
				Utils.UnixTimeToDateTime(510000001),
				true);

			BIP65_testsCore(
				1000,
				999,
				false);
			BIP65_testsCore(
				1000,
				1000,
				true);
			BIP65_testsCore(
				1000,
				1001,
				true);

			//Bad comparison
			BIP65_testsCore(
				1000,
				Utils.UnixTimeToDateTime(510000001),
				false);
			BIP65_testsCore(
				Utils.UnixTimeToDateTime(510000001),
				1000,
				false);

			Script s = new Script(OpcodeType.OP_CHECKLOCKTIMEVERIFY);
			Assert.Equal("OP_CLTV", s.ToString());
			s = new Script("OP_CHECKLOCKTIMEVERIFY");
			Assert.Equal("OP_CLTV", s.ToString());

			s = new Script("OP_NOP2");
			Assert.Equal("OP_CLTV", s.ToString());

			s = new Script("OP_HODL");
			Assert.Equal("OP_CLTV", s.ToString());
		}

		private void BIP65_testsCore(LockTime target, LockTime now, bool expectedResult)
		{
			Transaction tx = Network.CreateTransaction();
			tx.Outputs.Add(new TxOut()
			{
				ScriptPubKey = new Script(Op.GetPushOp(target.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY)
			});

			Transaction spending = Network.CreateTransaction();
			spending.LockTime = now;
			spending.Inputs.Add(new TxIn(tx.Outputs.AsCoins().First().Outpoint, new Script()));
			spending.Inputs[0].Sequence = 1;

			Assert.Equal(expectedResult, spending.Inputs.AsIndexedInputs().First().VerifyScript(tx.Outputs[0]));

			spending.Inputs[0].Sequence = uint.MaxValue;
			Assert.False(spending.Inputs.AsIndexedInputs().First().VerifyScript(tx.Outputs[0]));
		}



		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanUseCompactVarInt()
		{
			var tests = new[]{
				new object[]{0UL, new byte[]{0}},
				new object[]{1UL, new byte[]{1}},
				new object[]{127UL, new byte[]{0x7F}},
				new object[]{128UL, new byte[]{0x80, 0x00}},
				new object[]{255UL, new byte[]{0x80, 0x7F}},
				new object[]{256UL, new byte[]{0x81, 0x00}},
				new object[]{16383UL, new byte[]{0xFE, 0x7F}},
				//new object[]{16384UL, new byte[]{0xFF, 0x00}},
				//new object[]{16511UL, new byte[]{0x80, 0xFF, 0x7F}},
				//new object[]{65535UL, new byte[]{0x82, 0xFD, 0x7F}},
				new object[]{(ulong)1 << 32, new byte[]{0x8E, 0xFE, 0xFE, 0xFF, 0x00}},
			};
			foreach (var test in tests)
			{
				ulong val = (ulong)test[0];
				byte[] expectedBytes = (byte[])test[1];

				AssertEx.CollectionEquals(new CompactVarInt(val, sizeof(ulong)).ToBytes(), expectedBytes);
				AssertEx.CollectionEquals(new CompactVarInt(val, sizeof(uint)).ToBytes(), expectedBytes);

				var compact = new CompactVarInt(sizeof(ulong));
				compact.ReadWrite(expectedBytes, Network.Main);
				Assert.Equal(val, compact.ToLong());

				compact = new CompactVarInt(sizeof(uint));
				compact.ReadWrite(expectedBytes, Network.Main);
				Assert.Equal(val, compact.ToLong());
			}

			foreach (var i in Enumerable.Range(0, 65535 * 4))
			{
				var compact = new CompactVarInt((ulong)i, sizeof(ulong));
				var bytes = compact.ToBytes();
				compact = new CompactVarInt(sizeof(ulong));
				compact.ReadWrite(bytes, Network.Main);
				Assert.Equal((ulong)i, compact.ToLong());
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanExtractScriptCode()
		{
			var script = new Script("022b1300040df7414c0251433b7a2516e81689b02e33299c87ae870b5c9407b761 OP_DEPTH 3 OP_EQUAL OP_IF OP_SWAP 020524b8de0a1b57478f2d0e07aa9ea375b736f072281b3749fea044392bccfc52 OP_CHECKSIGVERIFY OP_CODESEPARATOR OP_CHECKSIG OP_ELSE 0 OP_CLTV OP_DROP OP_CHECKSIG OP_ENDIF");

			Assert.Throws<ArgumentOutOfRangeException>(() => script.ExtractScriptCode(-2));
			Assert.Throws<ArgumentOutOfRangeException>(() => script.ExtractScriptCode(1));

			Assert.Equal("OP_CHECKSIG OP_ELSE 0 OP_CLTV OP_DROP OP_CHECKSIG OP_ENDIF", script.ExtractScriptCode(0).ToString());

			Assert.Equal(script, script.ExtractScriptCode(-1));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCompressScript2()
		{
			var key = new Key(true);
			var script = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(key.PubKey.Hash);
			var compressed = script.ToCompressedBytes();
			Assert.Equal(21, compressed.Length);

			Assert.Equal(script.ToString(), new Script(compressed, true).ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void PayToMultiSigTemplateShouldAcceptNonKeyParameters()
		{
			var tx = Transaction.Parse("0100000002f9cbafc519425637ba4227f8d0a0b7160b4e65168193d5af39747891de98b5b5000000006b4830450221008dd619c563e527c47d9bd53534a770b102e40faa87f61433580e04e271ef2f960220029886434e18122b53d5decd25f1f4acb2480659fea20aabd856987ba3c3907e0121022b78b756e2258af13779c1a1f37ea6800259716ca4b7f0b87610e0bf3ab52a01ffffffff42e7988254800876b69f24676b3e0205b77be476512ca4d970707dd5c60598ab00000000fd260100483045022015bd0139bcccf990a6af6ec5c1c52ed8222e03a0d51c334df139968525d2fcd20221009f9efe325476eb64c3958e4713e9eefe49bf1d820ed58d2112721b134e2a1a53034930460221008431bdfa72bc67f9d41fe72e94c88fb8f359ffa30b33c72c121c5a877d922e1002210089ef5fc22dd8bfc6bf9ffdb01a9862d27687d424d1fefbab9e9c7176844a187a014c9052483045022015bd0139bcccf990a6af6ec5c1c52ed8222e03a0d51c334df139968525d2fcd20221009f9efe325476eb64c3958e4713e9eefe49bf1d820ed58d2112721b134e2a1a5303210378d430274f8c5ec1321338151e9f27f4c676a008bdf8638d07c0b6be9ab35c71210378d430274f8c5ec1321338151e9f27f4c676a008bdf8638d07c0b6be9ab35c7153aeffffffff01a08601000000000017a914d8dacdadb7462ae15cd906f1878706d0da8660e68700000000", Network.Main);
			var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(tx.Inputs[1].ScriptSig).RedeemScript;
			var result = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeemScript);
			Assert.Equal(2, result.PubKeys.Length);
			Assert.Equal(2, result.SignatureCount);
			Assert.Single(result.InvalidPubKeys);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void PayToPubkeyHashTemplateDoNotCrashOnInvalidSig()
		{
			var data = Encoders.Hex.DecodeData("035c030441ef8fa580553f149a5422ba4b0038d160b07a28e6fe2e1041b940fe95b1553c040000000000000050db680300000000000002b0466f722050696572636520616e64205061756c");

			PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(new Script(data));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCompressScript()
		{
			var key = new Key(true);

			//Pay to pubkey hash (encoded as 21 bytes)
			var script = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(key.PubKey.Hash);
			AssertCompressed(script, 21);
			script = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(key.PubKey.Decompress().Hash);
			AssertCompressed(script, 21);

			//Pay to script hash (encoded as 21 bytes)
			script = PayToScriptHashTemplate.Instance.GenerateScriptPubKey(script);
			AssertCompressed(script, 21);

			//Pay to pubkey starting with 0x02, 0x03 or 0x04 (encoded as 33 bytes)
			script = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(key.PubKey);
			script = AssertCompressed(script, 33);
			var readenKey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(script);
			AssertEx.CollectionEquals(readenKey.ToBytes(), key.PubKey.ToBytes());

			script = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(key.PubKey.Decompress());
			script = AssertCompressed(script, 33);
			readenKey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(script);
			AssertEx.CollectionEquals(readenKey.ToBytes(), key.PubKey.Decompress().ToBytes());


			//Other scripts up to 121 bytes require 1 byte + script length.
			script = new Script(Enumerable.Range(0, 60).Select(_ => (Op)OpcodeType.OP_RETURN).ToArray());
			AssertCompressed(script, 61);
			script = new Script(Enumerable.Range(0, 120).Select(_ => (Op)OpcodeType.OP_RETURN).ToArray());
			AssertCompressed(script, 121);

			//Above that, scripts up to 16505 bytes require 2 bytes + script length.
			script = new Script(Enumerable.Range(0, 122).Select(_ => (Op)OpcodeType.OP_RETURN).ToArray());
			AssertCompressed(script, 124);
		}

		private Script AssertCompressed(Script script, int expectedSize)
		{
			var compressor = new ScriptCompressor(script);
			var compressed = compressor.ToBytes();
			Assert.Equal(expectedSize, compressed.Length);

			compressor = new ScriptCompressor();
			compressor.ReadWrite(compressed, Network);
			AssertEx.CollectionEquals(compressor.GetScript().ToBytes(), script.ToBytes());

			var compressed2 = compressor.ToBytes();
			AssertEx.CollectionEquals(compressed, compressed2);
			return compressor.GetScript();
		}

		[Fact]
		[Trait("Core", "Core")]
		public void sig_validinvalid()
		{
			Assert.False(TransactionSignature.IsValid(new byte[0]));
			var sigs = JArray.Parse(File.ReadAllText("data/sig_canonical.json"));
			foreach (var sig in sigs)
			{
				Assert.True(TransactionSignature.IsValid(Encoders.Hex.DecodeData(sig.ToString())));
			}

			sigs = JArray.Parse(File.ReadAllText("data/sig_noncanonical.json"));
			foreach (var sig in sigs)
			{
				if (((HexEncoder)Encoders.Hex).IsValid(sig.ToString()))
				{
					Assert.False(TransactionSignature.IsValid(Encoders.Hex.DecodeData(sig.ToString())));
				}
			}
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_json_tests()
		{
			EnsureHasLibConsensus();
			var tests = TestCase.read_json("data/script_tests.json");
			foreach (var test in tests)
			{
				if (test.Count == 1)
					continue;
				int i = 0;

				Script wit = null;
				Money amount = Money.Zero;
				if (test[i] is JArray)
				{
					var array = (JArray)test[i];
					for (int ii = 0; ii < array.Count - 1; ii++)
					{
						wit += Encoders.Hex.DecodeData(array[ii].ToString());
					}
					amount = Money.Coins(((JValue)(array[array.Count - 1])).Value<decimal>());
					i++;
				}
				var scriptSig = ParseScript((string)test[i++]);
				var scriptPubKey = ParseScript((string)test[i++]);
				var flag = ParseFlag((string)test[i++]);
				var expectedError = ParseScriptError((string)test[i++]);
				var comment = i < test.Count ? (string)test[i++] : "no comment";

				Assert.Equal(scriptSig.ToString(), new Script(scriptSig.ToString()).ToString());
				Assert.Equal(scriptPubKey.ToString(), new Script(scriptPubKey.ToString()).ToString());

				AssertVerifyScript(wit, amount, scriptSig, scriptPubKey, flag, test.Index, comment, expectedError);
			}
		}

		private void AssertVerifyScript(WitScript wit, Money amount, Script scriptSig, Script scriptPubKey, ScriptVerify flags, int testIndex, string comment, ScriptError expectedError)
		{
			if (flags.HasFlag(ScriptVerify.CleanStack))
			{
				flags |= ScriptVerify.Witness;
				flags |= ScriptVerify.P2SH;
			}

			var creditingTransaction = CreateCreditingTransaction(scriptPubKey, amount);
			var spendingTransaction = CreateSpendingTransaction(wit, scriptSig, creditingTransaction);
			ScriptError actual;
			Script.VerifyScript(scriptSig, spendingTransaction, 0, new TxOut(amount, scriptPubKey), flags, SigHash.Undefined, out actual);
			Assert.True(expectedError == actual, "Test : " + testIndex + " " + comment);
#if !NOCONSENSUSLIB
			var ok = Script.VerifyScriptConsensus(scriptPubKey, spendingTransaction, 0, amount, flags);

			// If the spendingTransaction correctly spends the scriptPubKey but the expected error is not okay
			// because of a policy flags then, we ignore the test; otherwise assert everything the expected result
			// is the expected one.
			if (ok && (expectedError != ScriptError.OK) && (flags & ~ScriptVerify.Consensus) != 0)
				return;
			Assert.True(ok == (expectedError == ScriptError.OK), "[ConsensusLib] Test : " + testIndex + " " + comment);
#endif
		}


		private void EnsureHasLibConsensus()
		{
#if !NOCONSENSUSLIB
			var bitcoinPath = NodeBuilder.EnsureDownloaded(NodeDownloadData.Bitcoin.v0_17_0);

			string libConsensusDll = null;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				libConsensusDll = "libbitcoinconsensus-0.dll";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				libConsensusDll = "libbitcoinconsensus.0.dylib";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				libConsensusDll = "libbitcoinconsensus.so";
			}
			else
			{
				throw new NotSupportedException("Unknown operating system");
			}

			bitcoinPath = Path.GetDirectoryName(bitcoinPath);
			var libConsensusPath = Path.Combine(bitcoinPath, "..", "lib", libConsensusDll);
			libConsensusPath = Path.GetFullPath(libConsensusPath);
			try
			{
				File.Copy(libConsensusPath, $"./{libConsensusDll}", overwrite: false);
			}
			catch (IOException)
			{

			}
#endif
		}

		private static Transaction CreateSpendingTransaction(WitScript wit, Script scriptSig, Transaction creditingTransaction)
		{
			var spendingTransaction = Network.CreateTransaction();
			spendingTransaction.Inputs.Add(new OutPoint(creditingTransaction, 0), scriptSig, wit ?? WitScript.Empty);
			spendingTransaction.Outputs.Add(creditingTransaction.Outputs[0].Value, new Script());
			return spendingTransaction;
		}

		private static Transaction CreateCreditingTransaction(Script scriptPubKey, Money amount = null)
		{
			amount = amount ?? Money.Zero;
			var creditingTransaction = Network.CreateTransaction();
			creditingTransaction.Version = 1;
			creditingTransaction.LockTime = LockTime.Zero;
			creditingTransaction.Inputs.Add(scriptSig: new Script(OpcodeType.OP_0, OpcodeType.OP_0), sequence: Sequence.Final);
			creditingTransaction.Outputs.Add(amount, scriptPubKey);
			return creditingTransaction;
		}

		private ScriptError ParseScriptError(string str)
		{
			if (str == "OK")
				return ScriptError.OK;
			if (str == "EVAL_FALSE")
				return ScriptError.EvalFalse;
			if (str == "BAD_OPCODE")
				return ScriptError.BadOpCode;
			if (str == "UNBALANCED_CONDITIONAL")
				return ScriptError.UnbalancedConditional;
			if (str == "OP_RETURN")
				return ScriptError.OpReturn;
			if (str == "VERIFY")
				return ScriptError.Verify;
			if (str == "INVALID_ALTSTACK_OPERATION")
				return ScriptError.InvalidAltStackOperation;
			if (str == "INVALID_STACK_OPERATION")
				return ScriptError.InvalidStackOperation;
			if (str == "EQUALVERIFY")
				return ScriptError.EqualVerify;
			if (str == "DISABLED_OPCODE")
				return ScriptError.DisabledOpCode;
			if (str == "UNKNOWN_ERROR")
				return ScriptError.UnknownError;
			if (str == "DISCOURAGE_UPGRADABLE_NOPS")
				return ScriptError.DiscourageUpgradableNops;
			if (str == "PUSH_SIZE")
				return ScriptError.PushSize;
			if (str == "OP_COUNT")
				return ScriptError.OpCount;
			if (str == "STACK_SIZE")
				return ScriptError.StackSize;
			if (str == "SCRIPT_SIZE")
				return ScriptError.ScriptSize;
			if (str == "PUBKEY_COUNT")
				return ScriptError.PubkeyCount;
			if (str == "SIG_COUNT")
				return ScriptError.SigCount;
			if (str == "SIG_PUSHONLY")
				return ScriptError.SigPushOnly;
			if (str == "MINIMALDATA")
				return ScriptError.MinimalData;
			if (str == "PUBKEYTYPE")
				return ScriptError.PubKeyType;
			if (str == "SIG_DER")
				return ScriptError.SigDer;
			if (str == "WITNESS_MALLEATED")
				return ScriptError.WitnessMalleated;
			if (str == "WITNESS_MALLEATED_P2SH")
				return ScriptError.WitnessMalleatedP2SH;
			if (str == "WITNESS_PROGRAM_WITNESS_EMPTY")
				return ScriptError.WitnessProgramEmpty;
			if (str == "WITNESS_PROGRAM_MISMATCH")
				return ScriptError.WitnessProgramMissmatch;
			if (str == "WITNESS_PROGRAM_WRONG_LENGTH")
				return ScriptError.WitnessProgramWrongLength;
			if (str == "WITNESS_UNEXPECTED")
				return ScriptError.WitnessUnexpected;
			if (str == "SIG_HIGH_S")
				return ScriptError.SigHighS;
			if (str == "SIG_HASHTYPE")
				return ScriptError.SigHashType;
			if (str == "SIG_NULLDUMMY")
				return ScriptError.SigNullDummy;
			if (str == "CLEANSTACK")
				return ScriptError.CleanStack;
			if (str == "DISCOURAGE_UPGRADABLE_WITNESS_PROGRAM")
				return ScriptError.DiscourageUpgradableWitnessProgram;
			if (str == "NULLFAIL")
				return ScriptError.NullFail;
			if (str == "NEGATIVE_LOCKTIME")
				return ScriptError.NegativeLockTime;
			if (str == "UNSATISFIED_LOCKTIME")
				return ScriptError.UnsatisfiedLockTime;
			if (str == "MINIMALIF")
				return ScriptError.MinimalIf;
			if (str == "WITNESS_PUBKEYTYPE")
				return ScriptError.WitnessPubkeyType;
			throw new NotSupportedException(str);
		}

		private ScriptVerify ParseFlag(string flag)
		{
			ScriptVerify result = ScriptVerify.None;
			foreach (var p in flag.Split(',', '|').Select(p => p.Trim().ToUpperInvariant()))
			{
				if (p == "P2SH")
					result |= ScriptVerify.P2SH;
				else if (p == "STRICTENC")
					result |= ScriptVerify.StrictEnc;
				else if (p == "MINIMALDATA")
				{
					result |= ScriptVerify.MinimalData;
				}
				else if (p == "DERSIG")
				{
					result |= ScriptVerify.DerSig;
				}
				else if (p == "SIGPUSHONLY")
				{
					result |= ScriptVerify.SigPushOnly;
				}
				else if (p == "NULLDUMMY")
				{
					result |= ScriptVerify.NullDummy;
				}
				else if (p == "LOW_S")
				{
					result |= ScriptVerify.LowS;
				}
				else if (p == "")
				{
				}
				else if (p == "DISCOURAGE_UPGRADABLE_NOPS")
				{
					result |= ScriptVerify.DiscourageUpgradableNops;
				}
				else if (p == "CLEANSTACK")
				{
					result |= ScriptVerify.CleanStack;
				}
				else if (p == "WITNESS")
				{
					result |= ScriptVerify.Witness;
				}
				else if (p == "DISCOURAGE_UPGRADABLE_WITNESS_PROGRAM")
				{
					result |= ScriptVerify.DiscourageUpgradableWitnessProgram;
				}
				else if (p == "CHECKSEQUENCEVERIFY")
				{
					result |= ScriptVerify.CheckSequenceVerify;
				}
				else if (p == "NULLFAIL")
				{
					result |= ScriptVerify.NullFail;
				}
				else if (p == "MINIMALIF")
				{
					result |= ScriptVerify.MinimalIf;
				}
				else if (p == "WITNESS_PUBKEYTYPE")
				{
					result |= ScriptVerify.WitnessPubkeyType;
				}
				else
					throw new NotSupportedException(p);
			}
			return result;
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_standard_push()
		{
			for (int i = -1; i < 1000; i++)
			{
				Script script = new Script(Op.GetPushOp(i).ToBytes());
				Assert.True(script.IsPushOnly, "Number " + i + " is not pure push.");
				Assert.True(script.HasCanonicalPushes, "Number " + i + " push is not canonical.");
			}

			for (int i = 0; i < 1000; i++)
			{
				var data = Enumerable.Range(0, i).Select(_ => (byte)0x49).ToArray();
				Script script = new Script(Op.GetPushOp(data).ToBytes());
				Assert.True(script.IsPushOnly, "Length " + i + " is not pure push.");
				Assert.True(script.HasCanonicalPushes, "Length " + i + " push is not canonical.");
			}
		}


		Script sign_multisig(Script scriptPubKey, Key[] keys, Transaction transaction)
		{
			uint256 hash = transaction.GetSignatureHash(scriptPubKey, 0, SigHash.All);

			List<Op> ops = new List<Op>();
			//CScript result;
			//
			// NOTE: CHECKMULTISIG has an unfortunate bug; it requires
			// one extra item on the stack, before the signatures.
			// Putting OP_0 on the stack is the workaround;
			// fixing the bug would mean splitting the block chain (old
			// clients would not accept new CHECKMULTISIG transactions,
			// and vice-versa)
			//
			ops.Add(OpcodeType.OP_0);
			foreach (Key key in keys)
			{
				var vchSig = key.Sign(hash).ToDER().ToList();
				vchSig.Add((byte)SigHash.All);
				ops.Add(Op.GetPushOp(vchSig.ToArray()));
			}
			return new Script(ops.ToArray());
		}

		Script sign_multisig(Script scriptPubKey, Key key, Transaction transaction)
		{
			return sign_multisig(scriptPubKey, new Key[] { key }, transaction);
		}
		ScriptVerify flags = ScriptVerify.P2SH | ScriptVerify.StrictEnc;
		[Fact]
		[Trait("Core", "Core")]
		public void script_CHECKMULTISIG12()
		{
			EnsureHasLibConsensus();
			Key key1 = new Key(true);
			Key key2 = new Key(false);
			Key key3 = new Key(true);

			Script scriptPubKey12 = new Script(
					OpcodeType.OP_1,
					Op.GetPushOp(key1.PubKey.ToBytes()),
					Op.GetPushOp(key2.PubKey.ToBytes()),
					OpcodeType.OP_2,
					OpcodeType.OP_CHECKMULTISIG
				);

			Transaction txFrom12 = Network.CreateTransaction();
			txFrom12.Outputs.Add(new TxOut());
			txFrom12.Outputs[0].ScriptPubKey = scriptPubKey12;


			Transaction txTo12 = Network.CreateTransaction();
			txTo12.Inputs.Add(new TxIn());
			txTo12.Outputs.Add(new TxOut());
			txTo12.Inputs[0].PrevOut.N = 0;
			txTo12.Inputs[0].PrevOut.Hash = txFrom12.GetHash();
			txTo12.Outputs[0].Value = 1UL;
			txTo12.Inputs[0].ScriptSig = sign_multisig(scriptPubKey12, key1, txTo12);

			AssertValidScript(txFrom12.Outputs[0], txTo12, 0, flags);
			txTo12.Outputs[0].Value = 2UL;
			AssertInvalidScript(txFrom12.Outputs[0], txTo12, 0, flags);

			txTo12.Inputs[0].ScriptSig = sign_multisig(scriptPubKey12, key2, txTo12);
			AssertValidScript(txFrom12.Outputs[0], txTo12, 0, flags);

			txTo12.Inputs[0].ScriptSig = sign_multisig(scriptPubKey12, key3, txTo12);
			AssertInvalidScript(txFrom12.Outputs[0], txTo12, 0, flags);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_CHECKMULTISIG23()
		{
			EnsureHasLibConsensus();
			Key key1 = new Key(true);
			Key key2 = new Key(false);
			Key key3 = new Key(true);
			Key key4 = new Key(false);

			Script scriptPubKey23 = new Script(
					OpcodeType.OP_2,
					Op.GetPushOp(key1.PubKey.ToBytes()),
					Op.GetPushOp(key2.PubKey.ToBytes()),
					Op.GetPushOp(key3.PubKey.ToBytes()),
					OpcodeType.OP_3,
					OpcodeType.OP_CHECKMULTISIG
				);


			var txFrom23 = Network.CreateTransaction();
			txFrom23.Outputs.Add(new TxOut());
			txFrom23.Outputs[0].ScriptPubKey = scriptPubKey23;

			var txTo23 = Network.CreateTransaction();
			txTo23.Inputs.Add(new TxIn());
			txTo23.Outputs.Add(new TxOut());
			txTo23.Inputs[0].PrevOut.N = 0;
			txTo23.Inputs[0].PrevOut.Hash = txFrom23.GetHash();
			txTo23.Outputs[0].Value = 1UL;

			var keys = new Key[] { key1, key2 };
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertValidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[] { key1, key3 };
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertValidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[] { key2, key3 };
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertValidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[] { key2, key2 }; // Can't re-use sig
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertInvalidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[] { key2, key1 }; // sigs must be in correct order
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertInvalidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[] { key3, key2 }; // sigs must be in correct order
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertInvalidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[] { key4, key2 };// sigs must match pubkeys
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertInvalidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[] { key1, key4 };// sigs must match pubkeys
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertInvalidScript(txFrom23.Outputs[0], txTo23, 0, flags);

			keys = new Key[0]; // Must have signatures
			txTo23.Inputs[0].ScriptSig = sign_multisig(scriptPubKey23, keys, txTo23);
			AssertInvalidScript(txFrom23.Outputs[0], txTo23, 0, flags);
		}

		private void AssertInvalidScript(TxOut txOut, Transaction tx, int n, ScriptVerify verify)
		{
			Assert.False(Script.VerifyScript(tx, n, txOut, flags));
#if !NOCONSENSUSLIB
			Assert.False(Script.VerifyScriptConsensus(txOut.ScriptPubKey, tx, (uint)n, flags));
#endif
		}

		private void AssertValidScript(TxOut txOut, Transaction tx, int n, ScriptVerify verify)
		{
			Assert.True(Script.VerifyScript(tx, n, txOut, flags));
#if !NOCONSENSUSLIB
			Assert.True(Script.VerifyScriptConsensus(txOut.ScriptPubKey, tx, (uint)n, flags & ScriptVerify.Consensus));
#endif
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_single_hashtype()
		{
			var tx = Transaction.Parse("010000000390d31c6107013d754529d8818eff285fe40a3e7635f6930fec5d12eb02107a43010000006b483045022100f40815ae3c81a0dd851cc8d376d6fd226c88416671346a9033468cca2cdcc6c202204f764623903e6c4bed1b734b75d82c40f1725e4471a55ad4f51218f86130ac038321033d710ab45bb54ac99618ad23b3c1da661631aa25f23bfe9d22b41876f1d46e4effffffff3ff04a68e22bdd52e7c8cb848156d2d158bd5515b3c50adabc87d0ca2cd3482d010000006a4730440220598d263c107004008e9e26baa1e770be30fd31ee55ded1898f7c00da05a75977022045536bead322ca246779698b9c3df3003377090f41afeca7fb2ce9e328ec4af2832102b738b531def73020bd637f32935924cc88549c8206976226d968edd3a42fc2d7ffffffff46a8dc8970eb96622f27a516adcf40e0fcec5731e7556e174f2a271aef6861c7010000006b483045022100c5b90a777a9fdc90c208dbef7290d1fc1be651f47151ee4ccff646872a454cf90220640cfbc4550446968fbbe9d12528f3adf7d87b31541569c59e790db8a220482583210391332546e22bbe8fe3af54addfad6f8b83d05fa4f5e047593d4c07ae938795beffffffff028036be26000000001976a914ddfb29efad43a667465ac59ff14dc6442a1adfca88ac3d5cba01000000001976a914b64dde7a505a13ca986c40e86e984a8dc81368b688ac00000000", Network.Main);
			var scriptPubKey = new Script("OP_DUP OP_HASH160 34fea2c5a75414fd945273ae2d029ce1f28dafcf OP_EQUALVERIFY OP_CHECKSIG");
			var txout = tx.Outputs.CreateNewTxOut(Money.Zero, scriptPubKey);
			Assert.True(tx.Inputs.AsIndexedInputs().ToArray()[2].VerifyScript(txout, out ScriptError error));
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_combineSigs()
		{
			Key[] keys = new[] { new Key(), new Key(), new Key() };
			var txFrom = CreateCreditingTransaction(keys[0].PubKey.Hash.ScriptPubKey);
			var txTo = CreateSpendingTransaction(null, new Script(), txFrom);

			Script scriptPubKey = txFrom.Outputs[0].ScriptPubKey;
			Script scriptSig = txTo.Inputs[0].ScriptSig;

			Script empty = new Script();
			Script combined = Script.CombineSignatures(scriptPubKey, txTo, 0, empty, empty);
			Assert.True(combined.ToBytes().Length == 0);

			// Single signature case:
			SignSignature(keys, txFrom, txTo, 0); // changes scriptSig
			scriptSig = txTo.Inputs[0].ScriptSig;
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, scriptSig, empty);
			Assert.True(combined == scriptSig);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, empty, scriptSig);
			Assert.True(combined == scriptSig);
			Script scriptSigCopy = scriptSig.Clone();
			// Signing again will give a different, valid signature:
			SignSignature(keys, txFrom, txTo, 0);
			scriptSig = txTo.Inputs[0].ScriptSig;

			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, scriptSigCopy, scriptSig);
			Assert.True(combined == scriptSigCopy || combined == scriptSig);


			// P2SH, single-signature case:
			Script pkSingle = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(keys[0].PubKey);
			scriptPubKey = pkSingle.Hash.ScriptPubKey;
			txFrom.Outputs[0].ScriptPubKey = scriptPubKey;
			txTo.Inputs[0].PrevOut = new OutPoint(txFrom, 0);

			SignSignature(keys, txFrom, txTo, 0, pkSingle);
			scriptSig = txTo.Inputs[0].ScriptSig;

			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, scriptSig, empty);
			Assert.True(combined == scriptSig);

			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, empty, scriptSig);
			scriptSig = txTo.Inputs[0].ScriptSig;
			Assert.True(combined == scriptSig);
			scriptSigCopy = scriptSig.Clone();

			SignSignature(keys, txFrom, txTo, 0);
			scriptSig = txTo.Inputs[0].ScriptSig;

			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, scriptSigCopy, scriptSig);
			Assert.True(combined == scriptSigCopy || combined == scriptSig);
			// dummy scriptSigCopy with placeholder, should always choose non-placeholder:
			scriptSigCopy = new Script(OpcodeType.OP_0, Op.GetPushOp(pkSingle.ToBytes()));
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, scriptSigCopy, scriptSig);
			Assert.True(combined == scriptSig);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, scriptSig, scriptSigCopy);
			Assert.True(combined == scriptSig);

			// Hardest case:  Multisig 2-of-3
			scriptPubKey = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, keys.Select(k => k.PubKey).ToArray());
			txFrom.Outputs[0].ScriptPubKey = scriptPubKey;
			txTo.Inputs[0].PrevOut = new OutPoint(txFrom, 0);

			SignSignature(keys, txFrom, txTo, 0);
			scriptSig = txTo.Inputs[0].ScriptSig;

			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, scriptSig, empty);
			Assert.True(combined == scriptSig);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, empty, scriptSig);
			Assert.True(combined == scriptSig);

			// A couple of partially-signed versions:
			uint256 hash1 = txTo.GetSignatureHash(scriptPubKey, 0, SigHash.All);
			var sig1 = new TransactionSignature(keys[0].Sign(hash1), SigHash.All);

			uint256 hash2 = txTo.GetSignatureHash(scriptPubKey, 0, SigHash.None);
			var sig2 = new TransactionSignature(keys[1].Sign(hash2), SigHash.None);


			uint256 hash3 = txTo.GetSignatureHash(scriptPubKey, 0, SigHash.Single);
			var sig3 = new TransactionSignature(keys[2].Sign(hash3), SigHash.Single);


			// Not fussy about order (or even existence) of placeholders or signatures:
			Script partial1a = new Script() + OpcodeType.OP_0 + Op.GetPushOp(sig1.ToBytes()) + OpcodeType.OP_0;
			Script partial1b = new Script() + OpcodeType.OP_0 + OpcodeType.OP_0 + Op.GetPushOp(sig1.ToBytes());
			Script partial2a = new Script() + OpcodeType.OP_0 + Op.GetPushOp(sig2.ToBytes());
			Script partial2b = new Script() + Op.GetPushOp(sig2.ToBytes()) + OpcodeType.OP_0;
			Script partial3a = new Script() + Op.GetPushOp(sig3.ToBytes());
			Script partial3b = new Script() + OpcodeType.OP_0 + OpcodeType.OP_0 + Op.GetPushOp(sig3.ToBytes());
			Script partial3c = new Script() + OpcodeType.OP_0 + Op.GetPushOp(sig3.ToBytes()) + OpcodeType.OP_0;
			Script complete12 = new Script() + OpcodeType.OP_0 + Op.GetPushOp(sig1.ToBytes()) + Op.GetPushOp(sig2.ToBytes());
			Script complete13 = new Script() + OpcodeType.OP_0 + Op.GetPushOp(sig1.ToBytes()) + Op.GetPushOp(sig3.ToBytes());
			Script complete23 = new Script() + OpcodeType.OP_0 + Op.GetPushOp(sig2.ToBytes()) + Op.GetPushOp(sig3.ToBytes());

			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial1a, partial1b);
			Assert.True(combined == partial1a);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial1a, partial2a);
			Assert.True(combined == complete12);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial2a, partial1a);
			Assert.True(combined == complete12);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial1b, partial2b);
			Assert.True(combined == complete12);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial3b, partial1b);
			Assert.True(combined == complete13);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial2a, partial3a);
			Assert.True(combined == complete23);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial3b, partial2b);
			Assert.True(combined == complete23);
			combined = Script.CombineSignatures(scriptPubKey, txTo, 0, partial3b, partial3a);
			Assert.True(combined == partial3c);
		}

		private void SignSignature(Key[] keys, Transaction txFrom, Transaction txTo, int n, params Script[] knownRedeems)
		{
			Network.Main.CreateTransactionBuilder()
				.AddKeys(keys)
				.AddKnownRedeems(knownRedeems)
				.AddCoins(txFrom)
				.SignTransactionInPlace(txTo);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_PushData()
		{
			// Check that PUSHDATA1, PUSHDATA2, and PUSHDATA4 create the same value on
			// the stack as the 1-75 opcodes do.
			var direct = new Script(new byte[] { 1, 0x5a });
			var pushdata1 = new Script(new byte[] { (byte)OpcodeType.OP_PUSHDATA1, 1, 0x5a });
			var pushdata2 = new Script(new byte[] { (byte)OpcodeType.OP_PUSHDATA2, 1, 0, 0x5a });
			var pushdata4 = new Script(new byte[] { (byte)OpcodeType.OP_PUSHDATA4, 1, 0, 0, 0, 0x5a });

			var context = new ScriptEvaluationContext()
			{
				ScriptVerify = ScriptVerify.P2SH,
				SigHash = 0
			};
			var directStack = context.Clone();
			Assert.True(directStack.EvalScript(direct, Network.CreateTransaction(), 0));

			var pushdata1Stack = context.Clone();
			Assert.True(pushdata1Stack.EvalScript(pushdata1, Network.CreateTransaction(), 0));
			AssertEx.StackEquals(pushdata1Stack.Stack, directStack.Stack);


			var pushdata2Stack = context.Clone();
			Assert.True(pushdata2Stack.EvalScript(pushdata2, Network.CreateTransaction(), 0));
			AssertEx.StackEquals(pushdata2Stack.Stack, directStack.Stack);

			var pushdata4Stack = context.Clone();
			Assert.True(pushdata4Stack.EvalScript(pushdata4, Network.CreateTransaction(), 0));
			AssertEx.StackEquals(pushdata4Stack.Stack, directStack.Stack);
		}

		static Network Network => Network.Main;

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseAndGeneratePayToPubKeyScript()
		{
			var scriptPubKey = new Script("OP_DUP OP_HASH160 b72a6481ec2c2e65aa6bd9b42e213dce16fc6217 OP_EQUALVERIFY OP_CHECKSIG");
			var pubKey = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			Assert.Equal("b72a6481ec2c2e65aa6bd9b42e213dce16fc6217", pubKey.ToString());
			var scriptSig = new Script("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c");

			var sigResult = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			Assert.Equal("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301", Encoders.Hex.EncodeData(sigResult.TransactionSignature.ToBytes()));
			Assert.Equal("0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c", sigResult.PublicKey.ToString());

			Assert.Equal(PayToPubkeyHashTemplate.Instance.GenerateScriptSig(sigResult.TransactionSignature, sigResult.PublicKey).ToString(), scriptSig.ToString());
			Assert.Equal(PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(pubKey).ToString(), scriptPubKey.ToString());

			scriptSig = new Script("0 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c");

			sigResult = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(scriptSig);
			Assert.Null(sigResult.TransactionSignature);

			var scriptSig2 = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(sigResult);
			Assert.Equal(scriptSig.ToString(), scriptSig2.ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseAndGeneratePayToPubkey()
		{
			string scriptPubKey = "0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c OP_CHECKSIG";
			var pub = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(new Script(scriptPubKey));
			Assert.Equal("0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c", pub.ToHex());

			scriptPubKey = "0464bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27cff45c67d5f7be479215e9a27cea37afe1a00fa968ae3cbad128c9cee403844b7 OP_CHECKSIG";
			pub = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(new Script(scriptPubKey));
			Assert.Equal("0464bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27cff45c67d5f7be479215e9a27cea37afe1a00fa968ae3cbad128c9cee403844b7", pub.ToHex());

			scriptPubKey = "9964bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c OP_CHECKSIG";
			pub = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(new Script(scriptPubKey));
			Assert.Null(pub);

			string scriptSig = "3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301";
			var sig = PayToPubkeyTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig));
			Assert.NotNull(sig);
			Assert.True(PayToPubkeyTemplate.Instance.CheckScriptSig(new Script(scriptSig), null));

			scriptSig = "0044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301";
			sig = PayToPubkeyTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig));
			Assert.Null(sig);
			Assert.False(PayToPubkeyTemplate.Instance.CheckScriptSig(new Script(scriptSig), null));

			scriptSig = Encoders.Hex.EncodeData(TransactionSignature.Empty.ToBytes());
			sig = PayToPubkeyTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig));
			Assert.NotNull(sig);
			Assert.True(PayToPubkeyTemplate.Instance.CheckScriptSig(new Script(scriptSig), null));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeDeserializeVarInt()
		{
			var testCases = new (byte[] Bytes, ulong Value)[]
			{
				(Bytes: new byte[] { 0 }, Value: 0),
				(Bytes: new byte[] { 1 }, Value: 1),
				(Bytes: new byte[] { 0xFD-1  }, Value: 0xFD-1),
				(Bytes: new byte[] { 0xFD, 0xFD, 0x00 }, Value: 0xFD),
				(Bytes: new byte[] { 0xFD, 0xFF, 0xFF }, Value: 0xFFFF),
				(Bytes: new byte[] { 0xFE, 0x00, 0x00, 0x01, 0x00 }, Value: 0x00010000),
				(Bytes: new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF }, Value: 0xFFFFFFFF),
				(Bytes: new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }, Value: 0x0000000100000000),
				(Bytes: new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, Value: 0xFFFFFFFFFFFFFFFF)
			};
			foreach (var testCase in testCases)
			{
				var stream = new BitcoinStream(testCase.Bytes);
				Assert.Equal(testCase.Value, VarInt.StaticRead(stream));
				var ms = new MemoryStream();
				stream = new BitcoinStream(ms, true);
				VarInt.StaticWrite(stream, testCase.Value);
				var actualBytes = ms.ToArray();
				Assert.Equal(Encoders.Hex.EncodeData(testCase.Bytes), Encoders.Hex.EncodeData(actualBytes));
			}

			// Test on non canonic values
			testCases = new (byte[] Bytes, ulong Value)[]
			{
				(Bytes: new byte[] { 0xFD, 0x01, 0x00 }, Value: 0x01),
				(Bytes: new byte[] { 0xFE, 0x01, 0x00, 0x00, 0x00 }, Value: 0x01),
				(Bytes: new byte[] { 0xFF, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, Value: 0x01)
			};
			foreach (var testCase in testCases)
			{
				var stream = new BitcoinStream(testCase.Bytes);
				Assert.Equal(testCase.Value, VarInt.StaticRead(stream));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseAndGenerateSegwitScripts()
		{
			var pubkey = new PubKey("03a65786c1a48d4167aca08cf6eb8eed081e13f45c02dc6000fd8f3bb16242579a");
			var scriptPubKey = new Script("0 05481b7f1d90c5a167a15b00e8af76eb6984ea59");
			Assert.Equal(scriptPubKey, PayToWitPubKeyHashTemplate.Instance.GenerateScriptPubKey(pubkey));
			Assert.Equal(scriptPubKey, PayToWitPubKeyHashTemplate.Instance.GenerateScriptPubKey(pubkey.WitHash));
			Assert.Equal(scriptPubKey, PayToWitPubKeyHashTemplate.Instance.GenerateScriptPubKey((BitcoinWitPubKeyAddress)pubkey.WitHash.GetAddress(Network.Main)));
			var expected = new WitScript("304402206104c335e4adbb920184957f9f710b09de17d015329fde6807b9d321fd2142db02200b24ad996b4aa4ff103000348b5ad690abfd9fddae546af9e568394ed4a8311301 03a65786c1a48d4167aca08cf6eb8eed081e13f45c02dc6000fd8f3bb16242579a");
			var actual = PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(new PayToWitPubkeyHashScriptSigParameters()
			{
				PublicKey = pubkey,
				TransactionSignature = new TransactionSignature(Encoders.Hex.DecodeData("304402206104c335e4adbb920184957f9f710b09de17d015329fde6807b9d321fd2142db02200b24ad996b4aa4ff103000348b5ad690abfd9fddae546af9e568394ed4a8311301"))
			});
			Assert.Equal(expected, actual);

			// should be able to parse if signature is empty in p2wpkh.
			var witScriptWithoutSig = PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(null, pubkey);
			var actualParam = PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(witScriptWithoutSig);
			Assert.NotNull(actualParam);
			Assert.Equal(pubkey, actualParam.PublicKey);

			var script = new Script("0 03a65786c1a48d4167aca08cf6eb8eed081e13f45c02dc6000fd8f3bb16242579a").ToWitScript();
			var actualParam2 = PayToWitPubKeyHashTemplate.Instance.ExtractWitScriptParameters(script);
			Assert.NotNull(actualParam2);
			Assert.Equal(pubkey, actualParam2.PublicKey);

			var scriptSig = new Script("304402206b782f095f52f12133a96c078b558458b84c925afdb620d96c5f5bbf483e28d502206206796ff45d80216b83c77bafc4e7951fdb10a5bf3e4041c0e6c0938079b22b01 2103");
			var redeem = new Script(Encoders.Hex.DecodeData("2103a65786c1a48d4167aca08cf6eb8eed081e13f45c02dc6000fd8f3bb16242579aac"));
			expected = new WitScript(new Script("304402206b782f095f52f12133a96c078b558458b84c925afdb620d96c5f5bbf483e28d502206206796ff45d80216b83c77bafc4e7951fdb10a5bf3e4041c0e6c0938079b22b01 2103 2103a65786c1a48d4167aca08cf6eb8eed081e13f45c02dc6000fd8f3bb16242579aac"));
			actual = PayToWitScriptHashTemplate.Instance.GenerateWitScript(scriptSig, redeem);
			Assert.Equal(expected, actual);
			actual = PayToWitScriptHashTemplate.Instance.GenerateWitScript(scriptSig.ToOps().ToArray(), redeem);
			Assert.Equal(expected, actual);
			var extract = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(actual, null);
			Assert.Equal(extract, redeem);
			extract = PayToWitScriptHashTemplate.Instance.ExtractWitScriptParameters(actual, new Key().ScriptPubKey.WitHash);
			Assert.Null(extract);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseAndGeneratePayToMultiSig()
		{
			string scriptPubKey = "1 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d 2 OP_CHECKMULTISIG";
			var scriptPubKeyResult = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(new Script(scriptPubKey));
			Assert.Equal("0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c", scriptPubKeyResult.PubKeys[0].ToString());
			Assert.Equal("0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d", scriptPubKeyResult.PubKeys[1].ToString());
			Assert.Equal(1, scriptPubKeyResult.SignatureCount);
			Assert.Equal(scriptPubKey, PayToMultiSigTemplate.Instance.GenerateScriptPubKey(1, scriptPubKeyResult.PubKeys).ToString());

			var scriptSig = "0 3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301 3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9302";

			var result = PayToMultiSigTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig));
			Assert.Equal("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301", Encoders.Hex.EncodeData(result[0].ToBytes()));
			Assert.Equal("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9302", Encoders.Hex.EncodeData(result[1].ToBytes()));

			Assert.Equal(scriptSig, PayToMultiSigTemplate.Instance.GenerateScriptSig(result).ToString());

			scriptSig = "0 0 3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9302";
			result = PayToMultiSigTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig));
			Assert.Null(result[0]);
			Assert.Equal("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9302", Encoders.Hex.EncodeData(result[1].ToBytes()));

			var scriptSig2 = PayToMultiSigTemplate.Instance.GenerateScriptSig(result);
			Assert.Equal(scriptSig, scriptSig2.ToString());


			var sig = new TransactionSignature(Encoders.Hex.DecodeData("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301"));
			var actual = PayToScriptHashTemplate.Instance.GenerateScriptSig(new[] { sig, sig }, new Script(scriptPubKey));
			var expected = new Script("0 3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301 3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301 " + new Script(scriptPubKey).ToHex());
			Assert.Equal(expected, actual);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanExtractAddressesFromScript()
		{
			var payToMultiSig = new Script("1 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d 2 OP_CHECKMULTISIG");

			Assert.Null(payToMultiSig.GetSigner());
			var destinations = payToMultiSig.GetDestinationPublicKeys();
			Assert.Equal(2, destinations.Length);
			Assert.Equal("0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c", destinations[0].ToHex());
			Assert.Equal("0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d", destinations[1].ToHex());

			var payToScriptHash = new Script("OP_HASH160 b5b88dd9befc9236915fcdbb7fd50052df50c855 OP_EQUAL");
			Assert.NotNull(payToScriptHash.GetDestination());
			Assert.IsType<ScriptId>(payToScriptHash.GetDestination());
			Assert.Equal("b5b88dd9befc9236915fcdbb7fd50052df50c855", payToScriptHash.GetDestination().ToString());
			Assert.True(payToScriptHash.GetDestination().GetAddress(Network.Main).GetType() == typeof(BitcoinScriptAddress));

			var payToPubKeyHash = new Script("OP_DUP OP_HASH160 356facdac5f5bcae995d13e667bb5864fd1e7d59 OP_EQUALVERIFY OP_CHECKSIG");
			Assert.NotNull(payToPubKeyHash.GetDestination());
			Assert.IsType<KeyId>(payToPubKeyHash.GetDestination());
			Assert.Equal("356facdac5f5bcae995d13e667bb5864fd1e7d59", payToPubKeyHash.GetDestination().ToString());
			Assert.True(payToPubKeyHash.GetDestination().GetAddress(Network.Main).GetType() == typeof(BitcoinPubKeyAddress));

			var p2shScriptSig = new Script("0 3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301 51210364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c210364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d52ae");

			Assert.NotNull(p2shScriptSig.GetSigner());
			Assert.IsType<ScriptId>(p2shScriptSig.GetSigner());
			Assert.Equal("b5b88dd9befc9236915fcdbb7fd50052df50c855", p2shScriptSig.GetSigner().ToString());

			var p2phScriptSig = new Script("3045022100af878a48aab5a71397d518ee1ae3c35267cb559240bc4a06926d65d575090e7f02202a9208e1f13683b4e450b349ae3e7bd4498d5d808f06c4b8059ea41595447af401 02a71e88db4924c7620f3b27fa748817444b6ad02cd8cea32ed3cf2deb8b5ccae7");

			Assert.NotNull(p2phScriptSig.GetSigner());
			Assert.IsType<KeyId>(p2phScriptSig.GetSigner());
			Assert.Equal("352183abbcc80a0cd7c051a28df0abbf1e80ac3e", p2phScriptSig.GetSigner().ToString());
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void P2PKHScriptSigShouldNotBeMistakenForP2SHScriptSig()
		{
			var p2pkhScriptSig = new Script("304402206e3f2f829644ffe78b56ec8d0ea3715aee66e533a8195220bdea1526dc6ed3b202205eabcae791abfea55d54f8ec4e6de1bad1f7aa90e91687e81150b411e457025701 029f4485fddb359aeed82d71dc8df2fb0e83e31601c749d468ea92c99c13c5558b");
			p2pkhScriptSig.ToString();
			var result = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(p2pkhScriptSig);

			Assert.Null(result);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseAndGeneratePayToScript()
		{
			var redeem = "1 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c 0364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d 2 OP_CHECKMULTISIG";

			var scriptPubkey = "OP_HASH160 b5b88dd9befc9236915fcdbb7fd50052df50c855 OP_EQUAL";

			var scriptSig = "0 3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301 51210364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c210364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d52ae";

			var pubParams = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(new Script(scriptPubkey));
			Assert.Equal("b5b88dd9befc9236915fcdbb7fd50052df50c855", pubParams.ToString());
			Assert.Equal(scriptPubkey, PayToScriptHashTemplate.Instance.GenerateScriptPubKey(pubParams).ToString());
			new ScriptId(new Script());
			var sigParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig));
			Assert.Equal("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301", Encoders.Hex.EncodeData(sigParams.GetMultisigSignatures()[0].ToBytes()));
			Assert.Equal(redeem, sigParams.RedeemScript.ToString());
			Assert.Equal(scriptSig, PayToScriptHashTemplate.Instance.GenerateScriptSig(sigParams).ToString());

			//If scriptPubKey is provided, is it verifying the provided scriptSig is coherent with it ?
			sigParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig), sigParams.RedeemScript.PaymentScript);
			Assert.NotNull(sigParams);
			sigParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig), new Script("OP_HASH160 b5b88dd9befc9236915fcdbb7fd50052df50c853 OP_EQUAL"));
			Assert.Null(sigParams);
			sigParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig), new Script("OP_HASH160 OP_EQUAL"));
			Assert.Null(sigParams);
			///

			scriptSig = "0 0 51210364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27c210364bd4b02a752798342ed91c681a48793bb1c0853cbcd0b978c55e53485b8e27d52ae";

			sigParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(new Script(scriptSig));
			Assert.Null(sigParams.GetMultisigSignatures()[0]);
			var scriptSig2 = PayToScriptHashTemplate.Instance.GenerateScriptSig(sigParams);
			Assert.Equal(scriptSig2.ToString(), scriptSig);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void MultisigScriptCanFollowBIP67()
		{
			// test vector 1
			var pk1_1 = new PubKey("02ff12471208c14bd580709cb2358d98975247d8765f92bc25eab3b2763ed605f8");
			var pk1_2 = new PubKey("02fe6f0a5a297eb38c391581c4413e084773ea23954d93f7753db7dc0adc188b2f");
			var script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, true, new PubKey[] { pk1_1, pk1_2 });
			var pksInScript = script.ToOps().Skip(1).Take(2).ToArray();
			Assert.Equal(pksInScript[0].PushData, pk1_2.ToBytes());
			Assert.Equal(pksInScript[1].PushData, pk1_1.ToBytes());
			var expected = Script.FromBytesUnsafe(Encoders.Hex.DecodeData("522102fe6f0a5a297eb38c391581c4413e084773ea23954d93f7753db7dc0adc188b2f2102ff12471208c14bd580709cb2358d98975247d8765f92bc25eab3b2763ed605f852ae"));
			Assert.Equal(expected, script);

			// test vector 2 (Already sorted)
			var pk2_1 = new PubKey("02632b12f4ac5b1d1b72b2a3b508c19172de44f6f46bcee50ba33f3f9291e47ed0");
			var pk2_2 = new PubKey("027735a29bae7780a9755fae7a1c4374c656ac6a69ea9f3697fda61bb99a4f3e77");
			var pk2_3 = new PubKey("02e2cc6bd5f45edd43bebe7cb9b675f0ce9ed3efe613b177588290ad188d11b404");
			script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, true, new PubKey[] { pk2_1, pk2_2, pk2_3 });
			expected = Script.FromBytesUnsafe(Encoders.Hex.DecodeData("522102632b12f4ac5b1d1b72b2a3b508c19172de44f6f46bcee50ba33f3f9291e47ed021027735a29bae7780a9755fae7a1c4374c656ac6a69ea9f3697fda61bb99a4f3e772102e2cc6bd5f45edd43bebe7cb9b675f0ce9ed3efe613b177588290ad188d11b40453ae"));
			Assert.Equal(expected, script);
			var scriptWithoutSort = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, false, new PubKey[] { pk2_1, pk2_2, pk2_3 });
			Assert.Equal(script, scriptWithoutSort);
			scriptWithoutSort = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { pk2_1, pk2_2, pk2_3 });
			Assert.Equal(script, scriptWithoutSort);

			// test vector 3
			var pk3_1 = new PubKey("030000000000000000000000000000000000004141414141414141414141414141");
			var pk3_2 = new PubKey("020000000000000000000000000000000000004141414141414141414141414141");
			var pk3_3 = new PubKey("020000000000000000000000000000000000004141414141414141414141414140");
			var pk3_4 = new PubKey("030000000000000000000000000000000000004141414141414141414141414140");
			script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, true, new PubKey[] { pk3_1, pk3_2, pk3_3, pk3_4 });
			expected = Script.FromBytesUnsafe(Encoders.Hex.DecodeData("522102000000000000000000000000000000000000414141414141414141414141414021020000000000000000000000000000000000004141414141414141414141414141210300000000000000000000000000000000000041414141414141414141414141402103000000000000000000000000000000000000414141414141414141414141414154ae"));
			Assert.Equal(expected, script);

			// test vector 4
			var pk4_1 = new PubKey("022df8750480ad5b26950b25c7ba79d3e37d75f640f8e5d9bcd5b150a0f85014da");
			var pk4_2 = new PubKey("03e3818b65bcc73a7d64064106a859cc1a5a728c4345ff0b641209fba0d90de6e9");
			var pk4_3 = new PubKey("021f2f6e1e50cb6a953935c3601284925decd3fd21bc445712576873fb8c6ebc18");
			script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, true, new PubKey[] { pk4_1, pk4_2, pk4_3 });
			expected = Script.FromBytesUnsafe(Encoders.Hex.DecodeData("5221021f2f6e1e50cb6a953935c3601284925decd3fd21bc445712576873fb8c6ebc1821022df8750480ad5b26950b25c7ba79d3e37d75f640f8e5d9bcd5b150a0f85014da2103e3818b65bcc73a7d64064106a859cc1a5a728c4345ff0b641209fba0d90de6e953ae"));
			Assert.Equal(expected, script);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIdentifyP2PKHScript()
		{
			var script = Script.FromHex("76a914c398efa9c392ba6013c5e04ee729755ef7f58b3288ac");

			Assert.False(script.IsScriptType(ScriptType.Witness));
			Assert.True(script.IsScriptType(ScriptType.P2PKH));
			Assert.False(script.IsScriptType(ScriptType.P2SH));
			Assert.False(script.IsScriptType(ScriptType.P2PK));
			Assert.False(script.IsScriptType(ScriptType.P2WPKH));
			Assert.False(script.IsScriptType(ScriptType.P2WSH));
			Assert.False(script.IsScriptType(ScriptType.MultiSig));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIdentifyP2SHScript()
		{
			var script = Script.FromHex("a914e9c3dd0c07aac76179ebc76a6c78d4d67c6c160a87");

			Assert.False(script.IsScriptType(ScriptType.Witness));
			Assert.False(script.IsScriptType(ScriptType.P2PKH));
			Assert.True(script.IsScriptType(ScriptType.P2SH));
			Assert.False(script.IsScriptType(ScriptType.P2PK));
			Assert.False(script.IsScriptType(ScriptType.P2WPKH));
			Assert.False(script.IsScriptType(ScriptType.P2WSH));
			Assert.False(script.IsScriptType(ScriptType.MultiSig));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIdentifyP2PKScript()
		{
			var script = Script.FromHex("410496b538e853519c726a2c91e61ec11600ae1390813a627c66fb8be7947be63c52da7589379515d4e0a604f8141781e62294721166bf621e73a82cbf2342c858eeac");

			Assert.False(script.IsScriptType(ScriptType.Witness));
			Assert.False(script.IsScriptType(ScriptType.P2PKH));
			Assert.False(script.IsScriptType(ScriptType.P2SH));
			Assert.True(script.IsScriptType(ScriptType.P2PK));
			Assert.False(script.IsScriptType(ScriptType.P2WPKH));
			Assert.False(script.IsScriptType(ScriptType.P2WSH));
			Assert.False(script.IsScriptType(ScriptType.MultiSig));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIdentifyMultiSigScript()
		{
			var script = Script.FromHex("514104cc71eb30d653c0c3163990c47b976f3fb3f37cccdcbedb169a1dfef58bbfbfaff7d8a473e7e2e6d317b87bafe8bde97e3cf8f065dec022b51d11fcdd0d348ac4410461cbdcc5409fb4b4d42b51d33381354d80e550078cb532a34bfa2fcfdeb7d76519aecc62770f5b0e4ef8551946d8a540911abe3e7854a26f39f58b25c15342af52ae");

			Assert.False(script.IsScriptType(ScriptType.Witness));
			Assert.False(script.IsScriptType(ScriptType.P2PKH));
			Assert.False(script.IsScriptType(ScriptType.P2SH));
			Assert.False(script.IsScriptType(ScriptType.P2PK));
			Assert.False(script.IsScriptType(ScriptType.P2WPKH));
			Assert.False(script.IsScriptType(ScriptType.P2WSH));
			Assert.True(script.IsScriptType(ScriptType.MultiSig));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIdentifyP2WPKHScript()
		{
			var script = Script.FromHex("0014c4e2db37553d3e55c23da2ef5f2f41eb79935849");

			Assert.True(script.IsScriptType(ScriptType.Witness));
			Assert.False(script.IsScriptType(ScriptType.P2PKH));
			Assert.False(script.IsScriptType(ScriptType.P2SH));
			Assert.False(script.IsScriptType(ScriptType.P2PK));
			Assert.True(script.IsScriptType(ScriptType.P2WPKH));
			Assert.False(script.IsScriptType(ScriptType.P2WSH));
			Assert.False(script.IsScriptType(ScriptType.MultiSig));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanIdentifyP2WSHScript()
		{
			var script = Script.FromHex("00201965d4c2e7e7b1973b00931ed042d0bacd03c12b7dc5e06b550c31d15fec7cc1");

			Assert.True(script.IsScriptType(ScriptType.Witness));
			Assert.False(script.IsScriptType(ScriptType.P2PKH));
			Assert.False(script.IsScriptType(ScriptType.P2SH));
			Assert.False(script.IsScriptType(ScriptType.P2PK));
			Assert.False(script.IsScriptType(ScriptType.P2WPKH));
			Assert.True(script.IsScriptType(ScriptType.P2WSH));
			Assert.False(script.IsScriptType(ScriptType.MultiSig));
		}
	}
}
