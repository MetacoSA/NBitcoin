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
		Script ParseScript(string s)
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
