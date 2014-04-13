using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
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



		ScriptVerify flags = ScriptVerify.P2SH | ScriptVerify.StrictEnc;
		[Fact]
		public void script_valid()
		{
			var tests = TestCase.read_json("data/script_valid.json");
			foreach(var test in tests)
			{
				var comment = test.Count == 3 ? (string)test[2] : "no comment";
				var scriptSig = ParseScript((string)test[0]);
				var scriptPubKey = ParseScript((string)test[1]);

				Assert.Equal(scriptSig.ToString(), new Script(scriptSig.ToString()).ToString());
				Assert.Equal(scriptPubKey.ToString(), new Script(scriptPubKey.ToString()).ToString());

				var transaction = new Transaction();
				Assert.True(Script.VerifyScript(scriptSig, scriptPubKey, transaction, 0, flags, SigHash.None), "Test : " + test.Index + " " + comment);
			}
		}
		[Fact]
		public void script_invalid()
		{
			var tests = TestCase.read_json("data/script_invalid.json");
			foreach(var test in tests)
			{
				var comment = test.Count == 3 ? (string)test[2] : "no comment";
				var scriptSig = ParseScript((string)test[0]);
				var scriptPubKey = ParseScript((string)test[1]);

				Assert.Equal(scriptSig.ToString(), new Script(scriptSig.ToString()).ToString());
				Assert.Equal(scriptPubKey.ToString(), new Script(scriptPubKey.ToString()).ToString());

				var transaction = new Transaction();
				Assert.True(!Script.VerifyScript(scriptSig, scriptPubKey, transaction, 0, flags, SigHash.None), "Test : " + test.Index + " " + comment);
			}
		}

		[Fact]
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
			uint256 hash = Script.SignatureHash(scriptPubKey, transaction, 0, SigHash.All);

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
				var vchSig = key.Sign(hash).ToList();
				vchSig.Add((byte)SigHash.All);
				ops.Add(Op.GetPushOp(vchSig.ToArray()));
			}
			return new Script(ops.ToArray());
		}

		Script sign_multisig(Script scriptPubKey, Key key, Transaction transaction)
		{
			return sign_multisig(scriptPubKey, new Key[] { key }, transaction);
		}

		[Fact]
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
			txFrom12.VOut = new TxOut[] { new TxOut() };
			txFrom12.VOut[0].PublicKey = scriptPubKey12;


			Transaction txTo12 = new Transaction();
			txTo12.VIn = new TxIn[] { new TxIn() };
			txTo12.VOut = new TxOut[] { new TxOut() };
			txTo12.VIn[0].PrevOut.N = 0;
			txTo12.VIn[0].PrevOut.Hash = txFrom12.GetHash();
			txTo12.VOut[0].Value = 1;

			Script goodsig1 = sign_multisig(scriptPubKey12, key1, txTo12);
			Assert.True(Script.VerifyScript(goodsig1, scriptPubKey12, txTo12, 0, flags, 0));
			txTo12.VOut[0].Value = 2;
			Assert.True(!Script.VerifyScript(goodsig1, scriptPubKey12, txTo12, 0, flags, 0));

			Script goodsig2 = sign_multisig(scriptPubKey12, key2, txTo12);
			Assert.True(Script.VerifyScript(goodsig2, scriptPubKey12, txTo12, 0, flags, 0));

			Script badsig1 = sign_multisig(scriptPubKey12, key3, txTo12);
			Assert.True(!Script.VerifyScript(badsig1, scriptPubKey12, txTo12, 0, flags, 0));
		}

		[Fact]
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
			txFrom23.VOut = new TxOut[] { new TxOut() };
			txFrom23.VOut[0].PublicKey = scriptPubKey23;

			Transaction txTo23 = new Transaction();
			txTo23.VIn = new TxIn[] { new TxIn() };
			txTo23.VOut = new TxOut[] { new TxOut() };
			txTo23.VIn[0].PrevOut.N = 0;
			txTo23.VIn[0].PrevOut.Hash = txFrom23.GetHash();
			txTo23.VOut[0].Value = 1;

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
		public void script_PushData()
		{
			// Check that PUSHDATA1, PUSHDATA2, and PUSHDATA4 create the same value on
			// the stack as the 1-75 opcodes do.
			var direct = new Script(new byte[] { 1, 0x5a });
			var pushdata1 = new Script(new byte[] { (byte)OpcodeType.OP_PUSHDATA1, 1, 0x5a });
			var pushdata2 = new Script(new byte[] { (byte)OpcodeType.OP_PUSHDATA2, 1, 0, 0x5a });
			var pushdata4 = new Script(new byte[] { (byte)OpcodeType.OP_PUSHDATA4, 1, 0, 0, 0, 0x5a });

			Stack<byte[]> directStack = new Stack<byte[]>();
			Assert.True(direct.EvalScript(ref directStack, new Transaction(), 0, ScriptVerify.P2SH, 0));

			Stack<byte[]> pushdata1Stack = new Stack<byte[]>();
			Assert.True(pushdata1.EvalScript(ref pushdata1Stack, new Transaction(), 0, ScriptVerify.P2SH, 0));
			AssertEx.CollectionEquals(pushdata1Stack.SelectMany(o => o).ToArray(), directStack.SelectMany(o => o).ToArray());


			Stack<byte[]> pushdata2Stack = new Stack<byte[]>();
			Assert.True(pushdata2.EvalScript(ref pushdata2Stack, new Transaction(), 0, ScriptVerify.P2SH, 0));
			AssertEx.CollectionEquals(pushdata2Stack.SelectMany(o => o).ToArray(), directStack.SelectMany(o => o).ToArray());

			Stack<byte[]> pushdata4Stack = new Stack<byte[]>();
			Assert.True(pushdata4.EvalScript(ref pushdata4Stack, new Transaction(), 0, ScriptVerify.P2SH, 0));
			AssertEx.CollectionEquals(pushdata4Stack.SelectMany(o => o).ToArray(), directStack.SelectMany(o => o).ToArray());
		}
	}
}
