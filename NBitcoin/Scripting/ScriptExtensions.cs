using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting
{
	public static class ScriptExtensions
	{
		internal static ScriptToken[] ToTokens(this Script sc)
		{
			var result = new List<ScriptToken>();
			foreach (Op op in sc.ToOps())
			{
				switch (op.Code)
				{
					case OpcodeType.OP_BOOLAND:
						result.Add(ScriptToken.BoolAnd);
						break;
					case OpcodeType.OP_BOOLOR:
						result.Add(ScriptToken.BoolOr);
						break;
					case OpcodeType.OP_EQUAL:
						result.Add(ScriptToken.Equal);
						break;
					case OpcodeType.OP_EQUALVERIFY:
						result.Add(ScriptToken.EqualVerify);
						break;
					case OpcodeType.OP_CHECKSIG:
						result.Add(ScriptToken.CheckSig);
						break;
					case OpcodeType.OP_CHECKSIGVERIFY:
						result.Add(ScriptToken.CheckSigVerify);
						break;
					case OpcodeType.OP_CHECKMULTISIG:
						result.Add(ScriptToken.CheckMultiSig);
						break;
					case OpcodeType.OP_CHECKMULTISIGVERIFY:
						result.Add(ScriptToken.CheckMultiSigVerify);
						break;
					case OpcodeType.OP_CHECKSEQUENCEVERIFY:
						result.Add(ScriptToken.CheckSequenceVerify);
						break;
					case OpcodeType.OP_FROMALTSTACK:
						result.Add(ScriptToken.FromAltStack);
						break;
					case OpcodeType.OP_TOALTSTACK:
						result.Add(ScriptToken.ToAltStack);
						break;
					case OpcodeType.OP_DROP:
						result.Add(ScriptToken.Drop);
						break;
					case OpcodeType.OP_DUP:
						result.Add(ScriptToken.Dup);
						break;
					case OpcodeType.OP_IF:
						result.Add(ScriptToken.If);
						break;
					case OpcodeType.OP_IFDUP:
						result.Add(ScriptToken.IfDup);
						break;
					case OpcodeType.OP_NOTIF:
						result.Add(ScriptToken.NotIf);
						break;
					case OpcodeType.OP_ELSE:
						result.Add(ScriptToken.Else);
						break;
					case OpcodeType.OP_ENDIF:
						result.Add(ScriptToken.EndIf);
						break;
					case OpcodeType.OP_0NOTEQUAL:
						result.Add(ScriptToken.ZeroNotEqual);
						break;
					case OpcodeType.OP_SIZE:
						result.Add(ScriptToken.Size);
						break;
					case OpcodeType.OP_SWAP:
						result.Add(ScriptToken.Swap);
						break;
					case OpcodeType.OP_VERIFY:
						result.Add(ScriptToken.Verify);
						break;
					case OpcodeType.OP_HASH160:
						result.Add(ScriptToken.Hash160);
						break;
					case OpcodeType.OP_SHA256:
						result.Add(ScriptToken.Sha256);
						break;
					case OpcodeType.OP_ADD:
						result.Add(ScriptToken.Add);
						break;
					case OpcodeType.OP_0:
						result.Add(new ScriptToken.Number(0u));
						break;
					case OpcodeType.OP_1:
						result.Add(new ScriptToken.Number(1u));
						break;
					case OpcodeType.OP_2:
						result.Add(new ScriptToken.Number(2u));
						break;
					case OpcodeType.OP_3:
						result.Add(new ScriptToken.Number(3u));
						break;
					case OpcodeType.OP_4:
						result.Add(new ScriptToken.Number(4u));
						break;
					case OpcodeType.OP_5:
						result.Add(new ScriptToken.Number(5u));
						break;
					case OpcodeType.OP_6:
						result.Add(new ScriptToken.Number(6u));
						break;
					case OpcodeType.OP_7:
						result.Add(new ScriptToken.Number(7u));
						break;
					case OpcodeType.OP_8:
						result.Add(new ScriptToken.Number(8u));
						break;
					case OpcodeType.OP_9:
						result.Add(new ScriptToken.Number(9u));
						break;
					case OpcodeType.OP_10:
						result.Add(new ScriptToken.Number(10u));
						break;
					case OpcodeType.OP_11:
						result.Add(new ScriptToken.Number(11u));
						break;
					case OpcodeType.OP_12:
						result.Add(new ScriptToken.Number(12u));
						break;
					case OpcodeType.OP_13:
						result.Add(new ScriptToken.Number(13u));
						break;
					case OpcodeType.OP_14:
						result.Add(new ScriptToken.Number(14u));
						break;
					case OpcodeType.OP_15:
						result.Add(new ScriptToken.Number(15u));
						break;
					case OpcodeType.OP_16:
						result.Add(new ScriptToken.Number(16u));
						break;
					default:
						if ((byte)0x01 <= (byte)op.Code && (byte)op.Code < (byte)0x48)
							result.Add(GetItem(op));
						else if ((byte)0x48 <= (byte)op.Code)
							throw new ParsingException($"Miniscript does not support pushdata bigger than 33. Got {op}");
						else
							throw new ParsingException($"Unknown Opcode to Miniscript {op.Name}");
						break;
				}
			}
			result.Reverse();
			return result.ToArray();
		}
		private static ScriptToken GetItem(Op op)
		{
			if (op.PushData.Length == 20)
			{
				return new ScriptToken.Hash160Hash(new uint160(op.PushData, false));
			}
			if (op.PushData.Length == 32)
			{
				return new ScriptToken.Sha256Hash(new uint256(op.PushData, false));
			}
			if (op.PushData.Length == 33)
			{
				try
				{
					return new ScriptToken.Pk(new PubKey(op.PushData));
				}
				catch (FormatException ex)
				{
					throw new ParsingException("Invalid Public Key", ex);
				}
			}
			var i = op.GetInt();
			if (i.HasValue)
			{
				return new ScriptToken.Number((UInt32)i.Value);
			}
			else
			{
				throw new ParsingException($"Invalid push with Opcode {op}");
			}
		}
	}
}