using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Crypto;

namespace NBitcoin.Altcoins.Elements
{
	public enum ElementsOpcodeType : byte
	{
		OP_CHECKSIGFROMSTACK = 0xc1,
		OP_CHECKSIGFROMSTACKVERIFY = 0xc2
	}

	public class ElementsScriptEvaluationContext : ScriptEvaluationContext
	{
		protected override bool IsDisabledOpCode(OpcodeType type)
		{
			return type != OpcodeType.OP_CAT && base.IsDisabledOpCode(type);
		}

		protected override bool ExecuteOpCode(Script s, TransactionChecker checker, int hashversion, Op opcode, bool fRequireMinimal,
			bool fExec, Stack<bool> vfExec, ContextStack<byte[]> altstack, ScriptReader script, ref int pbegincodehash, ref int nOpCount,
			out bool evalScript)
		{
			switch (((byte)opcode.Code))
			{
				case (byte) ElementsOpcodeType.OP_CHECKSIGFROMSTACK:
				case (byte)ElementsOpcodeType.OP_CHECKSIGFROMSTACKVERIFY:
					if (_stack.Count < 3)
					{
						evalScript = SetError(ScriptError.InvalidStackOperation);
						return true;
					}

					var vchSig = _stack.Top(-3);
					var vchData = _stack.Top(-2);
					var vchPubKey = _stack.Top(-1);

					////// debug print
					//PrintHex(vchSig.begin(), vchSig.end(), "sig: %s\n");
					//PrintHex(vchPubKey.begin(), vchPubKey.end(), "pubkey: %s\n");

					// Subset of script starting at the most recent codeseparator
					var scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
					// Drop the signature, since there's no way for a signature to sign itself
					if (hashversion == (int) HashVersion.Original)
						scriptCode = scriptCode.FindAndDelete(vchSig);

					if (!CheckSignatureEncoding(vchSig) || !CheckPubKeyEncoding(vchPubKey, hashversion))
					{
						//serror is set
						{
							evalScript = false;
							return true;
						}
					}

					var publicKey = new PubKey(vchPubKey);
					var vchHash = new uint256(Hashes.SHA256(vchData));
					var sig = new ECDSASignature(vchSig);
					bool fSuccess = publicKey.Verify(vchHash, sig);
					if (!fSuccess && (ScriptVerify & ScriptVerify.NullFail) != 0 && vchSig.Length != 0)
					{
						evalScript = SetError(ScriptError.NullFail);
						return true;
					}

					_stack.Pop();
					_stack.Pop();
					_stack.Pop();
					_stack.Push(fSuccess ? vchTrue : vchFalse);
					if (((byte)opcode.Code) == (byte)ElementsOpcodeType.OP_CHECKSIGFROMSTACKVERIFY)
					{
						_stack.Pop();
					}
					if (!fSuccess)
					{
						evalScript = SetError(ScriptError.CheckSigVerify);
						return true;
					}


					break;

			}
			return base.ExecuteOpCode(s, checker, hashversion, opcode, fRequireMinimal, fExec, vfExec, altstack, script, ref pbegincodehash, ref nOpCount, out evalScript);
		}
	}

	public class ElementsOp : Op
	{
		public static string GetOpName(ElementsOpcodeType opcode)
		{
			switch (opcode)
			{
				case ElementsOpcodeType.OP_CHECKSIGFROMSTACK:
					return "OP_CHECKSIGFROMSTACK";
				case ElementsOpcodeType.OP_CHECKSIGFROMSTACKVERIFY:
					return "OP_CHECKSIGFROMSTACKVERIFY";
				default:
					return Op.GetOpName((OpcodeType) (byte) opcode);
			}
		}

		private static bool[] GetValidOpCode()
		{
			var valid = new bool[256];
			foreach (var val in Enum.GetValues(typeof(ElementsOpcodeType)))
			{
				valid[(byte)val] = true;
			}
			for (byte i = 0; ; i++)
			{
				if (IsPushCode((OpcodeType)i))
					valid[i] = true;
				if (i == 255)
					break;
			}
			return valid;
		}

	}
}
