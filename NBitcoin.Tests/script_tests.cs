using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class script_tests
	{

		static Dictionary<string, OpcodeType> mapOpNames = new Dictionary<string, OpcodeType>();
		public static Script ParseScript(string s)
		{
			MemoryStream result = new MemoryStream();
			if(mapOpNames.Count == 0)
			{
				for(int op = 0 ; op <= (byte)OpcodeType.OP_NOP10 ; op++)
				{
					// Allow OP_RESERVED to get into mapOpNames
					if(op < (byte)OpcodeType.OP_NOP && op != (byte)OpcodeType.OP_RESERVED)
						continue;

					var name = Op.GetOpName((OpcodeType)op);
					if(name == "OP_UNKNOWN")
						continue;
					string strName = name;
					mapOpNames[strName] = (OpcodeType)op;
					// Convenience: OP_ADD and just ADD are both recognized:
					strName = strName.Replace("OP_", "");
					mapOpNames[strName] = (OpcodeType)op;
				}
			}

			var words = s.Split(' ', '\t', '\n');

			foreach(string w in words)
			{
				if(w == "")
					continue;
				if(w.All(l => Money.isdigit(l)) ||
					(w.StartsWith("-") && w.Substring(1).All(l => Money.isdigit(l))))
				{

					// Number
					long n = long.Parse(w);
					Op.GetPushOp(new BigInteger(n)).WriteTo(result);
				}
				else if(w.StartsWith("0x") && HexEncoder.IsWellFormed(w.Substring(2)))
				{
					// Raw hex data, inserted NOT pushed onto stack:
					var raw = Encoders.Hex.DecodeData(w.Substring(2));
					result.Write(raw, 0, raw.Length);
				}
				else if(w.Length >= 2 && w.StartsWith("'") && w.EndsWith("'"))
				{
					// Single-quoted string, pushed as data. NOTE: this is poor-man's
					// parsing, spaces/tabs/newlines in single-quoted strings won't work.
					var b = TestUtils.ToBytes(w.Substring(1, w.Length - 2));
					Op.GetPushOp(b).WriteTo(result);
				}
				else if(mapOpNames.ContainsKey(w))
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
			foreach(var test in tests)
			{
				ulong val = (ulong)test[0];
				byte[] expectedBytes = (byte[])test[1];

				AssertEx.CollectionEquals(new CompactVarInt(val, sizeof(ulong)).ToBytes(), expectedBytes);
				AssertEx.CollectionEquals(new CompactVarInt(val, sizeof(uint)).ToBytes(), expectedBytes);

				var compact = new CompactVarInt(sizeof(ulong));
				compact.ReadWrite(expectedBytes);
				Assert.Equal(val, compact.ToLong());

				compact = new CompactVarInt(sizeof(uint));
				compact.ReadWrite(expectedBytes);
				Assert.Equal(val, compact.ToLong());
			}

			foreach(var i in Enumerable.Range(0, 65535 * 4))
			{
				var compact = new CompactVarInt((ulong)i, sizeof(ulong));
				var bytes = compact.ToBytes();
				compact = new CompactVarInt(sizeof(ulong));
				compact.ReadWrite(bytes);
				Assert.Equal((ulong)i, compact.ToLong());
			}
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
			var tx = new Transaction("0100000002f9cbafc519425637ba4227f8d0a0b7160b4e65168193d5af39747891de98b5b5000000006b4830450221008dd619c563e527c47d9bd53534a770b102e40faa87f61433580e04e271ef2f960220029886434e18122b53d5decd25f1f4acb2480659fea20aabd856987ba3c3907e0121022b78b756e2258af13779c1a1f37ea6800259716ca4b7f0b87610e0bf3ab52a01ffffffff42e7988254800876b69f24676b3e0205b77be476512ca4d970707dd5c60598ab00000000fd260100483045022015bd0139bcccf990a6af6ec5c1c52ed8222e03a0d51c334df139968525d2fcd20221009f9efe325476eb64c3958e4713e9eefe49bf1d820ed58d2112721b134e2a1a53034930460221008431bdfa72bc67f9d41fe72e94c88fb8f359ffa30b33c72c121c5a877d922e1002210089ef5fc22dd8bfc6bf9ffdb01a9862d27687d424d1fefbab9e9c7176844a187a014c9052483045022015bd0139bcccf990a6af6ec5c1c52ed8222e03a0d51c334df139968525d2fcd20221009f9efe325476eb64c3958e4713e9eefe49bf1d820ed58d2112721b134e2a1a5303210378d430274f8c5ec1321338151e9f27f4c676a008bdf8638d07c0b6be9ab35c71210378d430274f8c5ec1321338151e9f27f4c676a008bdf8638d07c0b6be9ab35c7153aeffffffff01a08601000000000017a914d8dacdadb7462ae15cd906f1878706d0da8660e68700000000");
			var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(tx.Inputs[1].ScriptSig).RedeemScript;
			var result = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeemScript);
			Assert.Equal(2, result.PubKeys.Length);
			Assert.Equal(2, result.SignatureCount);
			Assert.Equal(1, result.InvalidPubKeys.Length);
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
			compressor.ReadWrite(compressed);
			AssertEx.CollectionEquals(compressor.GetScript().ToBytes(), script.ToBytes());

			var compressed2 = compressor.ToBytes();
			AssertEx.CollectionEquals(compressed, compressed2);
			return compressor.GetScript();
		}


		[Fact]
		[Trait("Core", "Core")]
		public void script_valid()
		{
			var tests = TestCase.read_json("data/script_valid.json");
			foreach(var test in tests)
			{
				if(test.Count == 1)
					continue;
				var comment = test.Count == 4 ? (string)test[3] : "no comment";
				var flag = ParseFlag((string)test[2]);
				var scriptSig = ParseScript((string)test[0]);
				var scriptPubKey = ParseScript((string)test[1]);

				Assert.Equal(scriptSig.ToString(), new Script(scriptSig.ToString()).ToString());
				Assert.Equal(scriptPubKey.ToString(), new Script(scriptPubKey.ToString()).ToString());

				AssertVerifyScript(scriptSig, scriptPubKey, flag, test.Index, comment, true);
			}
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_invalid()
		{
			var tests = TestCase.read_json("data/script_invalid.json");
			foreach(var test in tests)
			{
				if(test.Count == 1)
					continue;
				var comment = test.Count == 4 ? (string)test[3] : "no comment";
				var flag = ParseFlag((string)test[2]);
				var scriptSig = ParseScript((string)test[0]);
				var scriptPubKey = ParseScript((string)test[1]);

				Assert.Equal(scriptSig.ToString(), new Script(scriptSig.ToString()).ToString());
				Assert.Equal(scriptPubKey.ToString(), new Script(scriptPubKey.ToString()).ToString());


				AssertVerifyScript(scriptSig, scriptPubKey, flag, test.Index, comment, false);
			}
		}

		private void AssertVerifyScript(Script scriptSig, Script scriptPubKey, ScriptVerify flags, int testIndex, string comment, bool expected)
		{
			var creditingTransaction = CreateCreditingTransaction(scriptPubKey);
			var spendingTransaction = CreateSpendingTransaction(scriptSig, creditingTransaction);

			var actual = Script.VerifyScript(scriptSig, scriptPubKey, spendingTransaction, 0, flags, SigHash.Undefined);
			Assert.True(expected == actual, "Test : " + testIndex + " " + comment);
		}

		private static Transaction CreateSpendingTransaction(Script scriptSig, Transaction creditingTransaction)
		{
			var spendingTransaction = new Transaction();
			spendingTransaction.AddInput(new TxIn(new OutPoint(creditingTransaction, 0))
			{
				ScriptSig = scriptSig
			});
			spendingTransaction.AddOutput(new TxOut()
			{
				ScriptPubKey = new Script(),
				Value = Money.Zero
			});
			return spendingTransaction;
		}

		private static Transaction CreateCreditingTransaction(Script scriptPubKey)
		{
			var creditingTransaction = new Transaction();
			creditingTransaction.AddInput(new TxIn()
			{
				ScriptSig = new Script(OpcodeType.OP_0, OpcodeType.OP_0)
			});
			creditingTransaction.AddOutput(Money.Zero, scriptPubKey);
			return creditingTransaction;
		}

		private ScriptVerify ParseFlag(string flag)
		{
			ScriptVerify result = ScriptVerify.None;
			foreach(var p in flag.Split(',', '|').Select(p => p.Trim().ToUpperInvariant()))
			{
				if(p == "P2SH")
					result |= ScriptVerify.P2SH;
				else if(p == "STRICTENC")
					result |= ScriptVerify.StrictEnc;
				else if(p == "MINIMALDATA")
				{
					result |= ScriptVerify.MinimalData;
				}
				else if(p == "DERSIG")
				{
					result |= ScriptVerify.DerSig;
				}
				else if(p == "SIGPUSHONLY")
				{
					result |= ScriptVerify.SigPushOnly;
				}
				else if(p == "NULLDUMMY")
				{
					result |= ScriptVerify.NullDummy;
				}
				else if(p == "LOW_S")
				{
					result |= ScriptVerify.LowS;
				}
				else if(p == "")
				{
				}
				else if(p == "DISCOURAGE_UPGRADABLE_NOPS")
				{
					result |= ScriptVerify.DiscourageUpgradableNops;
				}
				else if(p == "CLEANSTACK")
				{
					result |= ScriptVerify.CleanStack;
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
			for(int i = -1 ; i < 1000 ; i++)
			{
				Script script = new Script(Op.GetPushOp(i).ToBytes());
				Assert.True(script.IsPushOnly, "Number " + i + " is not pure push.");
				Assert.True(script.HasCanonicalPushes, "Number " + i + " push is not canonical.");
			}

			for(int i = 0 ; i < 1000 ; i++)
			{
				var data = Enumerable.Range(0, i).Select(_ => (byte)0x49).ToArray();
				Script script = new Script(Op.GetPushOp(data).ToBytes());
				Assert.True(script.IsPushOnly, "Length " + i + " is not pure push.");
				Assert.True(script.HasCanonicalPushes, "Length " + i + " push is not canonical.");
			}
		}


		Script sign_multisig(Script scriptPubKey, Key[] keys, Transaction transaction)
		{
			uint256 hash = scriptPubKey.SignatureHash(transaction, 0, SigHash.All);

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
			foreach(Key key in keys)
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

			Transaction txFrom12 = new Transaction();
			txFrom12.Outputs.Add(new TxOut());
			txFrom12.Outputs[0].ScriptPubKey = scriptPubKey12;


			Transaction txTo12 = new Transaction();
			txTo12.Inputs.Add(new TxIn());
			txTo12.Outputs.Add(new TxOut());
			txTo12.Inputs[0].PrevOut.N = 0;
			txTo12.Inputs[0].PrevOut.Hash = txFrom12.GetHash();
			txTo12.Outputs[0].Value = 1;

			Script goodsig1 = sign_multisig(scriptPubKey12, key1, txTo12);
			Assert.True(Script.VerifyScript(goodsig1, scriptPubKey12, txTo12, 0, flags, 0));
			txTo12.Outputs[0].Value = 2;
			Assert.True(!Script.VerifyScript(goodsig1, scriptPubKey12, txTo12, 0, flags, 0));

			Script goodsig2 = sign_multisig(scriptPubKey12, key2, txTo12);
			Assert.True(Script.VerifyScript(goodsig2, scriptPubKey12, txTo12, 0, flags, 0));

			Script badsig1 = sign_multisig(scriptPubKey12, key3, txTo12);
			Assert.True(!Script.VerifyScript(badsig1, scriptPubKey12, txTo12, 0, flags, 0));
		}

		[Fact]
		[Trait("Core", "Core")]
		public void script_CHECKMULTISIG23()
		{
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


			Transaction txFrom23 = new Transaction();
			txFrom23.Outputs.Add(new TxOut());
			txFrom23.Outputs[0].ScriptPubKey = scriptPubKey23;

			Transaction txTo23 = new Transaction();
			txTo23.Inputs.Add(new TxIn());
			txTo23.Outputs.Add(new TxOut());
			txTo23.Inputs[0].PrevOut.N = 0;
			txTo23.Inputs[0].PrevOut.Hash = txFrom23.GetHash();
			txTo23.Outputs[0].Value = 1;

			Key[] keys = new Key[] { key1, key2 };
			Script goodsig1 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(Script.VerifyScript(goodsig1, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[] { key1, key3 };
			Script goodsig2 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(Script.VerifyScript(goodsig2, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[] { key2, key3 };
			Script goodsig3 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(Script.VerifyScript(goodsig3, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[] { key2, key2 }; // Can't re-use sig
			Script badsig1 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(!Script.VerifyScript(badsig1, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[] { key2, key1 }; // sigs must be in correct order
			Script badsig2 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(!Script.VerifyScript(badsig2, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[] { key3, key2 }; // sigs must be in correct order
			Script badsig3 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(!Script.VerifyScript(badsig3, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[] { key4, key2 };// sigs must match pubkeys
			Script badsig4 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(!Script.VerifyScript(badsig4, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[] { key1, key4 };// sigs must match pubkeys
			Script badsig5 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(!Script.VerifyScript(badsig5, scriptPubKey23, txTo23, 0, flags, 0));

			keys = new Key[0]; // Must have signatures
			Script badsig6 = sign_multisig(scriptPubKey23, keys, txTo23);
			Assert.True(!Script.VerifyScript(badsig6, scriptPubKey23, txTo23, 0, flags, 0));
		}


		[Fact]
		[Trait("Core", "Core")]
		public void script_combineSigs()
		{
			Key[] keys = new[] { new Key(), new Key(), new Key() };
			var txFrom = CreateCreditingTransaction(keys[0].PubKey.Hash.ScriptPubKey);
			var txTo = CreateSpendingTransaction(new Script(), txFrom);

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
			uint256 hash1 = scriptPubKey.SignatureHash(txTo, 0, SigHash.All);
			var sig1 = new TransactionSignature(keys[0].Sign(hash1), SigHash.All);

			uint256 hash2 = scriptPubKey.SignatureHash(txTo, 0, SigHash.None);
			var sig2 = new TransactionSignature(keys[1].Sign(hash2), SigHash.None);


			uint256 hash3 = scriptPubKey.SignatureHash(txTo, 0, SigHash.Single);
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
			new TransactionBuilder()
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
			Assert.True(directStack.EvalScript(direct, new Transaction(), 0));

			var pushdata1Stack = context.Clone();
			Assert.True(pushdata1Stack.EvalScript(pushdata1, new Transaction(), 0));
			AssertEx.StackEquals(pushdata1Stack.Stack, directStack.Stack);


			var pushdata2Stack = context.Clone();
			Assert.True(pushdata2Stack.EvalScript(pushdata2, new Transaction(), 0));
			AssertEx.StackEquals(pushdata2Stack.Stack, directStack.Stack);

			var pushdata4Stack = context.Clone();
			Assert.True(pushdata4Stack.EvalScript(pushdata4, new Transaction(), 0));
			AssertEx.StackEquals(pushdata4Stack.Stack, directStack.Stack);
		}

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
			Assert.True(payToPubKeyHash.GetDestination().GetAddress(Network.Main).GetType() == typeof(BitcoinAddress));

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
			Assert.Equal("3044022064f45a382a15d3eb5e7fe72076eec4ef0f56fde1adfd710866e729b9e5f3383d02202720a895914c69ab49359087364f06d337a2138305fbc19e20d18da78415ea9301", Encoders.Hex.EncodeData(sigParams.Signatures[0].ToBytes()));
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
			Assert.Null(sigParams.Signatures[0]);

			var scriptSig2 = PayToScriptHashTemplate.Instance.GenerateScriptSig(sigParams);
			Assert.Equal(scriptSig2.ToString(), scriptSig);
		}
	}
}
