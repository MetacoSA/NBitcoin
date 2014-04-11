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

		Script ParseScript(string s)
		{
			MemoryStream result = new MemoryStream();

			Dictionary<string, OpcodeType> mapOpNames = new Dictionary<string, OpcodeType>();

			if(mapOpNames.Count == 0)
			{
				for(int op = 0 ; op <= (byte)OpcodeType.OP_NOP10 ; op++)
				{
					// Allow OP_RESERVED to get into mapOpNames
					if(op < (byte)OpcodeType.OP_NOP && op != (byte)OpcodeType.OP_RESERVED)
						continue;

					var name = Script.GetOpName((OpcodeType)op);
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
				if(w.All(l => Money.isdigit(l)) ||
					(w.StartsWith("-") && w.Substring(1).All(l => Money.isdigit(l))))
				{

					// Number
					long n = long.Parse(w);
					Script.PushData(new BigInteger(n), result);
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
					Script.PushData(new BigInteger(b), result);
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
				Assert.True(Script.VerifyScript(scriptSig, scriptPubKey, transaction, 0, flags, SigHash.None), comment);
			}
		}
	}
}
