using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	/** Script verification flags */
	[Flags]
	public enum ScriptVerify
	{
		None = 0,
		P2SH = 1, // evaluate P2SH (BIP16) subscripts
		StrictEnc = 2, // enforce strict conformance to DER and SEC2 for signatures and pubkeys
		EvenS = 4, // enforce even S values in signatures (depends on STRICTENC)
		NoCache = 8, // do not store results in signature cache (but do query it)
	};

	/** Signature hash types/flags */
	public enum SigHash : byte
	{
		All = 1,
		None = 2,
		Single = 3,
		AnyoneCanPay = 0x80,
	};

	/** Script opcodes */
	public enum OpcodeType : byte
	{
		// push value
		OP_0 = 0x00,
		OP_FALSE = OP_0,
		OP_PUSHDATA1 = 0x4c,
		OP_PUSHDATA2 = 0x4d,
		OP_PUSHDATA4 = 0x4e,
		OP_1NEGATE = 0x4f,
		OP_RESERVED = 0x50,
		OP_1 = 0x51,
		OP_TRUE = OP_1,
		OP_2 = 0x52,
		OP_3 = 0x53,
		OP_4 = 0x54,
		OP_5 = 0x55,
		OP_6 = 0x56,
		OP_7 = 0x57,
		OP_8 = 0x58,
		OP_9 = 0x59,
		OP_10 = 0x5a,
		OP_11 = 0x5b,
		OP_12 = 0x5c,
		OP_13 = 0x5d,
		OP_14 = 0x5e,
		OP_15 = 0x5f,
		OP_16 = 0x60,

		// control
		OP_NOP = 0x61,
		OP_VER = 0x62,
		OP_IF = 0x63,
		OP_NOTIF = 0x64,
		OP_VERIF = 0x65,
		OP_VERNOTIF = 0x66,
		OP_ELSE = 0x67,
		OP_ENDIF = 0x68,
		OP_VERIFY = 0x69,
		OP_RETURN = 0x6a,

		// stack ops
		OP_TOALTSTACK = 0x6b,
		OP_FROMALTSTACK = 0x6c,
		OP_2DROP = 0x6d,
		OP_2DUP = 0x6e,
		OP_3DUP = 0x6f,
		OP_2OVER = 0x70,
		OP_2ROT = 0x71,
		OP_2SWAP = 0x72,
		OP_IFDUP = 0x73,
		OP_DEPTH = 0x74,
		OP_DROP = 0x75,
		OP_DUP = 0x76,
		OP_NIP = 0x77,
		OP_OVER = 0x78,
		OP_PICK = 0x79,
		OP_ROLL = 0x7a,
		OP_ROT = 0x7b,
		OP_SWAP = 0x7c,
		OP_TUCK = 0x7d,

		// splice ops
		OP_CAT = 0x7e,
		OP_SUBSTR = 0x7f,
		OP_LEFT = 0x80,
		OP_RIGHT = 0x81,
		OP_SIZE = 0x82,

		// bit logic
		OP_INVERT = 0x83,
		OP_AND = 0x84,
		OP_OR = 0x85,
		OP_XOR = 0x86,
		OP_EQUAL = 0x87,
		OP_EQUALVERIFY = 0x88,
		OP_RESERVED1 = 0x89,
		OP_RESERVED2 = 0x8a,

		// numeric
		OP_1ADD = 0x8b,
		OP_1SUB = 0x8c,
		OP_2MUL = 0x8d,
		OP_2DIV = 0x8e,
		OP_NEGATE = 0x8f,
		OP_ABS = 0x90,
		OP_NOT = 0x91,
		OP_0NOTEQUAL = 0x92,

		OP_ADD = 0x93,
		OP_SUB = 0x94,
		OP_MUL = 0x95,
		OP_DIV = 0x96,
		OP_MOD = 0x97,
		OP_LSHIFT = 0x98,
		OP_RSHIFT = 0x99,

		OP_BOOLAND = 0x9a,
		OP_BOOLOR = 0x9b,
		OP_NUMEQUAL = 0x9c,
		OP_NUMEQUALVERIFY = 0x9d,
		OP_NUMNOTEQUAL = 0x9e,
		OP_LESSTHAN = 0x9f,
		OP_GREATERTHAN = 0xa0,
		OP_LESSTHANOREQUAL = 0xa1,
		OP_GREATERTHANOREQUAL = 0xa2,
		OP_MIN = 0xa3,
		OP_MAX = 0xa4,

		OP_WITHIN = 0xa5,

		// crypto
		OP_RIPEMD160 = 0xa6,
		OP_SHA1 = 0xa7,
		OP_SHA256 = 0xa8,
		OP_HASH160 = 0xa9,
		OP_HASH256 = 0xaa,
		OP_CODESEPARATOR = 0xab,
		OP_CHECKSIG = 0xac,
		OP_CHECKSIGVERIFY = 0xad,
		OP_CHECKMULTISIG = 0xae,
		OP_CHECKMULTISIGVERIFY = 0xaf,

		// expansion
		OP_NOP1 = 0xb0,
		OP_NOP2 = 0xb1,
		OP_NOP3 = 0xb2,
		OP_NOP4 = 0xb3,
		OP_NOP5 = 0xb4,
		OP_NOP6 = 0xb5,
		OP_NOP7 = 0xb6,
		OP_NOP8 = 0xb7,
		OP_NOP9 = 0xb8,
		OP_NOP10 = 0xb9,



		// template matching params
		OP_SMALLDATA = 0xf9,
		OP_SMALLINTEGER = 0xfa,
		OP_PUBKEYS = 0xfb,
		OP_PUBKEYHASH = 0xfd,
		OP_PUBKEY = 0xfe,

		OP_INVALIDOPCODE = 0xff,
	};


	public class Script : IBitcoinSerializable
	{

		byte[] _Script = new byte[0];
		public Script()
		{

		}
		public Script(string script)
		{
			_Script = Parse(script);
		}

		private static byte[] Parse(string script)
		{
			var reader = new StringReader(script);
			MemoryStream result = new MemoryStream();
			while(reader.Peek() != -1)
			{
				Op.Read(reader).WriteTo(result);
			}
			return result.ToArray();
		}

		public Script(byte[] data)
		{
			_Script = data;
		}

		static bool CastToBool(byte[] vch)
		{
			for(uint i = 0 ; i < vch.Length ; i++)
			{
				if(vch[i] != 0)
				{

					if(i == vch.Length - 1 && vch[i] == 0x80)
						return false;
					return true;
				}
			}
			return false;
		}

		public static bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction txTo, int nIn, ScriptVerify flags, SigHash nHashType)
		{
			Stack<byte[]> stack = new Stack<byte[]>();
			Stack<byte[]> stackCopy = null;
			if(!scriptSig.EvalScript(ref stack, txTo, nIn, flags, nHashType))
				return false;
			if((flags & ScriptVerify.P2SH) != 0)
			{
				var copy = stack.ToArray();
				Array.Reverse(copy);
				stackCopy = new Stack<byte[]>(copy);
			}
			if(!scriptPubKey.EvalScript(ref stack, txTo, nIn, flags, nHashType))
				return false;
			if(stack.Count == 0)
				return false;
			if(CastToBool(stack.Peek()) == false)
				return false;

			// Additional validation for spend-to-script-hash transactions:
			if(((flags & ScriptVerify.P2SH) != 0) && scriptPubKey.IsPayToScriptHash)
			{
				foreach(var script in scriptSig.CreateReader().ToEnumerable())
				{
					if(script.PushData == null)
						return false;
				}

				// stackCopy cannot be empty here, because if it was the
				// P2SH  HASH <> EQUAL  scriptPubKey would be evaluated with
				// an empty stack and the EvalScript above would return false.
				if(stackCopy.Count == 0)
					throw new InvalidProgramException("stackCopy cannot be empty here");

				var pubKeySerialized = stackCopy.Peek();
				Script pubKey2 = new Script(pubKeySerialized);
				stackCopy.Pop();

				if(!pubKey2.EvalScript(ref stackCopy, txTo, nIn, flags, nHashType))
					return false;
				if(stackCopy.Count == 0)
					return false;
				return CastToBool(stackCopy.Peek());
			}
			return true;
		}






		static readonly byte[] vchFalse = new byte[] { 0 };
		static readonly byte[] vchZero = new byte[] { 0 };
		static readonly byte[] vchTrue = new byte[] { 1, 1 };

		private bool EvalScript(ref Stack<byte[]> stack, Transaction txTo, int nIn, ScriptVerify flags, SigHash nHashType)
		{
			var script = CreateReader();
			int pend = (int)script.Inner.Length;

			int pbegincodehash = 0;
			Stack<bool> vfExec = new Stack<bool>();
			Stack<byte[]> altstack = new Stack<byte[]>();
			Op opcode;
			if(_Script.Length > 10000)
				return false;
			int nOpCount = 0;

			try
			{
				while((opcode = script.Read()) != null)
				{
					bool fExec = vfExec.All(o => o); //!count(vfExec.begin(), vfExec.end(), false);

					//
					// Read instruction
					//

					if(opcode.PushData != null && opcode.PushData.Length > 520)
						return false;

					// Note how OP_RESERVED does not count towards the opcode limit.
					if(opcode.Code > OpcodeType.OP_16 && ++nOpCount > 201)
						return false;

					if(opcode.Code == OpcodeType.OP_CAT ||
						opcode.Code == OpcodeType.OP_SUBSTR ||
						opcode.Code == OpcodeType.OP_LEFT ||
						opcode.Code == OpcodeType.OP_RIGHT ||
						opcode.Code == OpcodeType.OP_INVERT ||
						opcode.Code == OpcodeType.OP_AND ||
						opcode.Code == OpcodeType.OP_OR ||
						opcode.Code == OpcodeType.OP_XOR ||
						opcode.Code == OpcodeType.OP_2MUL ||
						opcode.Code == OpcodeType.OP_2DIV ||
						opcode.Code == OpcodeType.OP_MUL ||
						opcode.Code == OpcodeType.OP_DIV ||
						opcode.Code == OpcodeType.OP_MOD ||
						opcode.Code == OpcodeType.OP_LSHIFT ||
						opcode.Code == OpcodeType.OP_RSHIFT)
						return false; // Disabled opcodes.

					if(fExec && opcode.PushData != null)
						stack.Push(opcode.PushData);
					else if(fExec || (OpcodeType.OP_IF <= opcode.Code && opcode.Code <= OpcodeType.OP_ENDIF))
						switch(opcode.Code)
						{
							//
							// Push value
							//
							case OpcodeType.OP_1NEGATE:
							case OpcodeType.OP_1:
							case OpcodeType.OP_2:
							case OpcodeType.OP_3:
							case OpcodeType.OP_4:
							case OpcodeType.OP_5:
							case OpcodeType.OP_6:
							case OpcodeType.OP_7:
							case OpcodeType.OP_8:
							case OpcodeType.OP_9:
							case OpcodeType.OP_10:
							case OpcodeType.OP_11:
							case OpcodeType.OP_12:
							case OpcodeType.OP_13:
							case OpcodeType.OP_14:
							case OpcodeType.OP_15:
							case OpcodeType.OP_16:
								{
									// ( -- value)
									BigInteger bn = new BigInteger((int)opcode.Code - (int)(OpcodeType.OP_1 - 1));
									stack.Push(Utils.BigIntegerToBytes(bn));
								}
								break;


							//
							// Control
							//
							case OpcodeType.OP_NOP:
							case OpcodeType.OP_NOP1:
							case OpcodeType.OP_NOP2:
							case OpcodeType.OP_NOP3:
							case OpcodeType.OP_NOP4:
							case OpcodeType.OP_NOP5:
							case OpcodeType.OP_NOP6:
							case OpcodeType.OP_NOP7:
							case OpcodeType.OP_NOP8:
							case OpcodeType.OP_NOP9:
							case OpcodeType.OP_NOP10:
								break;

							case OpcodeType.OP_IF:
							case OpcodeType.OP_NOTIF:
								{
									// <expression> if [statements] [else [statements]] endif
									bool fValue = false;
									if(fExec)
									{
										if(stack.Count < 1)
											return false;
										var vch = top(stack, -1);
										fValue = CastToBool(vch);
										if(opcode.Code == OpcodeType.OP_NOTIF)
											fValue = !fValue;
										stack.Pop();
									}
									vfExec.Push(fValue);
								}
								break;

							case OpcodeType.OP_ELSE:
								{
									if(vfExec.Count == 0)
										return false;
									var v = vfExec.Pop();
									vfExec.Push(!v);
									//vfExec.Peek() = !vfExec.Peek();
								}
								break;

							case OpcodeType.OP_ENDIF:
								{
									if(vfExec.Count == 0)
										return false;
									vfExec.Pop();
								}
								break;

							case OpcodeType.OP_VERIFY:
								{
									// (true -- ) or
									// (false -- false) and return
									if(stack.Count < 1)
										return false;
									bool fValue = CastToBool(top(stack, -1));
									if(fValue)
										stack.Pop();
									else
										return false;
								}
								break;

							case OpcodeType.OP_RETURN:
								{
									return false;
								}


							//
							// Stack ops
							//
							case OpcodeType.OP_TOALTSTACK:
								{
									if(stack.Count < 1)
										return false;
									altstack.Push(top(stack, -1));
									stack.Pop();
								}
								break;

							case OpcodeType.OP_FROMALTSTACK:
								{
									if(altstack.Count < 1)
										return false;
									stack.Push(top(altstack, -1));
									altstack.Pop();
								}
								break;

							case OpcodeType.OP_2DROP:
								{
									// (x1 x2 -- )
									if(stack.Count < 2)
										return false;
									stack.Pop();
									stack.Pop();
								}
								break;

							case OpcodeType.OP_2DUP:
								{
									// (x1 x2 -- x1 x2 x1 x2)
									if(stack.Count < 2)
										return false;
									var vch1 = top(stack, -2);
									var vch2 = top(stack, -1);
									stack.Push(vch1);
									stack.Push(vch2);
								}
								break;

							case OpcodeType.OP_3DUP:
								{
									// (x1 x2 x3 -- x1 x2 x3 x1 x2 x3)
									if(stack.Count < 3)
										return false;
									var vch1 = top(stack, -3);
									var vch2 = top(stack, -2);
									var vch3 = top(stack, -1);
									stack.Push(vch1);
									stack.Push(vch2);
									stack.Push(vch3);
								}
								break;

							case OpcodeType.OP_2OVER:
								{
									// (x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2)
									if(stack.Count < 4)
										return false;
									var vch1 = top(stack, -4);
									var vch2 = top(stack, -3);
									stack.Push(vch1);
									stack.Push(vch2);
								}
								break;

							case OpcodeType.OP_2ROT:
								{
									// (x1 x2 x3 x4 x5 x6 -- x3 x4 x5 x6 x1 x2)
									if(stack.Count < 6)
										return false;
									var vch1 = top(stack, -6);
									var vch2 = top(stack, -5);
									erase(ref stack, stack.Count - 6, stack.Count - 4);
									stack.Push(vch1);
									stack.Push(vch2);
								}
								break;

							case OpcodeType.OP_2SWAP:
								{
									// (x1 x2 x3 x4 -- x3 x4 x1 x2)
									if(stack.Count < 4)
										return false;
									swap(ref stack, -4, -2);
									swap(ref stack, -3, -1);
								}
								break;

							case OpcodeType.OP_IFDUP:
								{
									// (x - 0 | x x)
									if(stack.Count < 1)
										return false;
									var vch = top(stack, -1);
									if(CastToBool(vch))
										stack.Push(vch);
								}
								break;

							case OpcodeType.OP_DEPTH:
								{
									// -- stacksize
									BigInteger bn = new BigInteger(stack.Count);
									stack.Push(Utils.BigIntegerToBytes(bn));
								}
								break;

							case OpcodeType.OP_DROP:
								{
									// (x -- )
									if(stack.Count < 1)
										return false;
									stack.Pop();
								}
								break;

							case OpcodeType.OP_DUP:
								{
									// (x -- x x)
									if(stack.Count < 1)
										return false;
									var vch = top(stack, -1);
									stack.Push(vch);
								}
								break;

							case OpcodeType.OP_NIP:
								{
									// (x1 x2 -- x2)
									if(stack.Count < 2)
										return false;
									erase(ref stack, stack.Count - 2);
								}
								break;

							case OpcodeType.OP_OVER:
								{
									// (x1 x2 -- x1 x2 x1)
									if(stack.Count < 2)
										return false;
									var vch = top(stack, -2);
									stack.Push(vch);
								}
								break;

							case OpcodeType.OP_PICK:
							case OpcodeType.OP_ROLL:
								{
									// (xn ... x2 x1 x0 n - xn ... x2 x1 x0 xn)
									// (xn ... x2 x1 x0 n - ... x2 x1 x0 xn)
									if(stack.Count < 2)
										return false;
									int n = (int)CastToBigNum(top(stack, -1));
									stack.Pop();
									if(n < 0 || n >= stack.Count)
										return false;
									var vch = top(stack, -n - 1);
									if(opcode.Code == OpcodeType.OP_ROLL)
										erase(ref stack, stack.Count - n - 1);
									stack.Push(vch);
								}
								break;

							case OpcodeType.OP_ROT:
								{
									// (x1 x2 x3 -- x2 x3 x1)
									//  x2 x1 x3  after first swap
									//  x2 x3 x1  after second swap
									if(stack.Count < 3)
										return false;
									swap(ref stack, -3, -2);
									swap(ref stack, -2, -1);
								}
								break;

							case OpcodeType.OP_SWAP:
								{
									// (x1 x2 -- x2 x1)
									if(stack.Count < 2)
										return false;
									swap(ref stack, -2, -1);
								}
								break;

							case OpcodeType.OP_TUCK:
								{
									// (x1 x2 -- x2 x1 x2)
									if(stack.Count < 2)
										return false;
									var vch = top(stack, -1);
									insert(ref stack, stack.Count - 2, vch);
								}
								break;


							case OpcodeType.OP_SIZE:
								{
									// (in -- in size)
									if(stack.Count < 1)
										return false;
									BigInteger bn = new BigInteger(top(stack, -1).Length);
									stack.Push(Utils.BigIntegerToBytes(bn));
								}
								break;


							//
							// Bitwise logic
							//
							case OpcodeType.OP_EQUAL:
							case OpcodeType.OP_EQUALVERIFY:
								//case OpcodeType.OP_NOTEQUAL: // use OpcodeType.OP_NUMNOTEQUAL
								{
									// (x1 x2 - bool)
									if(stack.Count < 2)
										return false;
									var vch1 = top(stack, -2);
									var vch2 = top(stack, -1);
									bool fEqual = Utils.ArrayEqual(vch1, vch2);
									// OpcodeType.OP_NOTEQUAL is disabled because it would be too easy to say
									// something like n != 1 and have some wiseguy pass in 1 with extra
									// zero bytes after it (numerically, 0x01 == 0x0001 == 0x000001)
									//if (opcode == OpcodeType.OP_NOTEQUAL)
									//    fEqual = !fEqual;
									stack.Pop();
									stack.Pop();
									stack.Push(fEqual ? vchTrue : vchFalse);
									if(opcode.Code == OpcodeType.OP_EQUALVERIFY)
									{
										if(fEqual)
											stack.Pop();
										else
											return false;
									}
								}
								break;


							//
							// Numeric
							//
							case OpcodeType.OP_1ADD:
							case OpcodeType.OP_1SUB:
							case OpcodeType.OP_NEGATE:
							case OpcodeType.OP_ABS:
							case OpcodeType.OP_NOT:
							case OpcodeType.OP_0NOTEQUAL:
								{
									// (in -- out)
									if(stack.Count < 1)
										return false;
									var bn = CastToBigNum(top(stack, -1));
									switch(opcode.Code)
									{
										case OpcodeType.OP_1ADD:
											bn += BigInteger.One;
											break;
										case OpcodeType.OP_1SUB:
											bn -= BigInteger.One;
											break;
										case OpcodeType.OP_NEGATE:
											bn = -bn;
											break;
										case OpcodeType.OP_ABS:
											if(bn < BigInteger.Zero)
												bn = -bn;
											break;
										case OpcodeType.OP_NOT:
											bn = CastToBigNum(bn == BigInteger.Zero);
											break;
										case OpcodeType.OP_0NOTEQUAL:
											bn = CastToBigNum(bn != BigInteger.Zero);
											break;
										default:
											throw new NotSupportedException("invalid opcode");
									}
									stack.Pop();
									stack.Push(Utils.BigIntegerToBytes(bn));
								}
								break;

							case OpcodeType.OP_ADD:
							case OpcodeType.OP_SUB:
							case OpcodeType.OP_BOOLAND:
							case OpcodeType.OP_BOOLOR:
							case OpcodeType.OP_NUMEQUAL:
							case OpcodeType.OP_NUMEQUALVERIFY:
							case OpcodeType.OP_NUMNOTEQUAL:
							case OpcodeType.OP_LESSTHAN:
							case OpcodeType.OP_GREATERTHAN:
							case OpcodeType.OP_LESSTHANOREQUAL:
							case OpcodeType.OP_GREATERTHANOREQUAL:
							case OpcodeType.OP_MIN:
							case OpcodeType.OP_MAX:
								{
									// (x1 x2 -- out)
									if(stack.Count < 2)
										return false;
									var bn1 = CastToBigNum(top(stack, -2));
									var bn2 = CastToBigNum(top(stack, -1));
									BigInteger bn;
									switch(opcode.Code)
									{
										case OpcodeType.OP_ADD:
											bn = bn1 + bn2;
											break;

										case OpcodeType.OP_SUB:
											bn = bn1 - bn2;
											break;

										case OpcodeType.OP_BOOLAND:
											bn = CastToBigNum(bn1 != BigInteger.Zero && bn2 != BigInteger.Zero);
											break;
										case OpcodeType.OP_BOOLOR:
											bn = CastToBigNum(bn1 != BigInteger.Zero || bn2 != BigInteger.Zero);
											break;
										case OpcodeType.OP_NUMEQUAL:
											bn = CastToBigNum(bn1 == bn2);
											break;
										case OpcodeType.OP_NUMEQUALVERIFY:
											bn = CastToBigNum(bn1 == bn2);
											break;
										case OpcodeType.OP_NUMNOTEQUAL:
											bn = CastToBigNum(bn1 != bn2);
											break;
										case OpcodeType.OP_LESSTHAN:
											bn = CastToBigNum(bn1 < bn2);
											break;
										case OpcodeType.OP_GREATERTHAN:
											bn = CastToBigNum(bn1 > bn2);
											break;
										case OpcodeType.OP_LESSTHANOREQUAL:
											bn = CastToBigNum(bn1 <= bn2);
											break;
										case OpcodeType.OP_GREATERTHANOREQUAL:
											bn = CastToBigNum(bn1 >= bn2);
											break;
										case OpcodeType.OP_MIN:
											bn = (bn1 < bn2 ? bn1 : bn2);
											break;
										case OpcodeType.OP_MAX:
											bn = (bn1 > bn2 ? bn1 : bn2);
											break;
										default:
											throw new NotSupportedException("invalid opcode");
									}
									stack.Pop();
									stack.Pop();
									stack.Push(Utils.BigIntegerToBytes(bn));

									if(opcode.Code == OpcodeType.OP_NUMEQUALVERIFY)
									{
										if(CastToBool(top(stack, -1)))
											stack.Pop();
										else
											return false;
									}
								}
								break;

							case OpcodeType.OP_WITHIN:
								{
									// (x min max -- out)
									if(stack.Count < 3)
										return false;
									var bn1 = CastToBigNum(top(stack, -3));
									var bn2 = CastToBigNum(top(stack, -2));
									var bn3 = CastToBigNum(top(stack, -1));
									bool fValue = (bn2 <= bn1 && bn1 < bn3);
									stack.Pop();
									stack.Pop();
									stack.Pop();
									stack.Push(fValue ? vchTrue : vchFalse);
								}
								break;


							//
							// Crypto
							//
							case OpcodeType.OP_RIPEMD160:
							case OpcodeType.OP_SHA1:
							case OpcodeType.OP_SHA256:
							case OpcodeType.OP_HASH160:
							case OpcodeType.OP_HASH256:
								{
									// (in -- hash)
									if(stack.Count < 1)
										return false;
									var vch = top(stack, -1);
									byte[] vchHash = null;//((opcode == OpcodeType.OP_RIPEMD160 || opcode == OpcodeType.OP_SHA1 || opcode == OpcodeType.OP_HASH160) ? 20 : 32);
									if(opcode.Code == OpcodeType.OP_RIPEMD160)
										vchHash = Hashes.RIPEMD160(vch, vch.Length);
									else if(opcode.Code == OpcodeType.OP_SHA1)
										vchHash = Hashes.SHA1(vch, vch.Length);
									else if(opcode.Code == OpcodeType.OP_SHA256)
										vchHash = Hashes.SHA256(vch, vch.Length);
									else if(opcode.Code == OpcodeType.OP_HASH160)
										vchHash = Hashes.Hash160(vch, vch.Length).ToBytes();
									else if(opcode.Code == OpcodeType.OP_HASH256)
										vchHash = Hashes.Hash256(vch, vch.Length).ToBytes();
									stack.Pop();
									stack.Push(vchHash);
								}
								break;

							case OpcodeType.OP_CODESEPARATOR:
								{
									// Hash starts after the code separator
									pbegincodehash = (int)script.Inner.Position;
								}
								break;

							case OpcodeType.OP_CHECKSIG:
							case OpcodeType.OP_CHECKSIGVERIFY:
								{
									// (sig pubkey -- bool)
									if(stack.Count < 2)
										return false;

									var vchSig = top(stack, -2);
									var vchPubKey = top(stack, -1);

									////// debug print
									//PrintHex(vchSig.begin(), vchSig.end(), "sig: %s\n");
									//PrintHex(vchPubKey.begin(), vchPubKey.end(), "pubkey: %s\n");

									// Subset of script starting at the most recent codeseparator
									var scriptCode = new Script(_Script.Skip(pbegincodehash).ToArray());

									// Drop the signature, since there's no way for a signature to sign itself
									scriptCode.FindAndDelete(new Script(vchSig));

									bool fSuccess = IsCanonicalSignature(vchSig, flags) && IsCanonicalPubKey(vchPubKey, flags) &&
										CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn, nHashType, flags);

									stack.Pop();
									stack.Pop();
									stack.Push(fSuccess ? vchTrue : vchFalse);
									if(opcode.Code == OpcodeType.OP_CHECKSIGVERIFY)
									{
										if(fSuccess)
											stack.Pop();
										else
											return false;
									}
								}
								break;

							case OpcodeType.OP_CHECKMULTISIG:
							case OpcodeType.OP_CHECKMULTISIGVERIFY:
								{
									// ([sig ...] num_of_signatures [pubkey ...] num_of_pubkeys -- bool)

									int i = 1;
									if((int)stack.Count < i)
										return false;

									int nKeysCount = (int)CastToBigNum(top(stack, -i));
									if(nKeysCount < 0 || nKeysCount > 20)
										return false;
									nOpCount += nKeysCount;
									if(nOpCount > 201)
										return false;
									int ikey = ++i;
									i += nKeysCount;
									if((int)stack.Count < i)
										return false;

									int nSigsCount = (int)CastToBigNum(top(stack, -i));
									if(nSigsCount < 0 || nSigsCount > nKeysCount)
										return false;
									int isig = ++i;
									i += nSigsCount;
									if((int)stack.Count < i)
										return false;

									// Subset of script starting at the most recent codeseparator
									Script scriptCode = new Script(this._Script.Skip(pbegincodehash).ToArray());

									// Drop the signatures, since there's no way for a signature to sign itself
									for(int k = 0 ; k < nSigsCount ; k++)
									{
										var vchSig = top(stack, -isig - k);
										scriptCode.FindAndDelete(new Script(vchSig));
									}

									bool fSuccess = true;
									while(fSuccess && nSigsCount > 0)
									{
										var vchSig = top(stack, -isig);
										var vchPubKey = top(stack, -ikey);

										// Check signature
										bool fOk = IsCanonicalSignature(vchSig, flags) && IsCanonicalPubKey(vchPubKey, flags) &&
											CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn, nHashType, flags);

										if(fOk)
										{
											isig++;
											nSigsCount--;
										}
										ikey++;
										nKeysCount--;

										// If there are more signatures left than keys left,
										// then too many signatures have failed
										if(nSigsCount > nKeysCount)
											fSuccess = false;
									}

									while(i-- > 0)
										stack.Pop();
									stack.Push(fSuccess ? vchTrue : vchFalse);

									if(opcode.Code == OpcodeType.OP_CHECKMULTISIGVERIFY)
									{
										if(fSuccess)
											stack.Pop();
										else
											return false;
									}
								}
								break;

							default:
								return false;
						}

					// Size limits
					if(stack.Count + altstack.Count > 1000)
						return false;

				}
			}
			catch(Exception)
			{
				return false;
			}


			if(vfExec.Count != 0)
				return false;

			return true;
		}

		private ScriptReader CreateReader()
		{
			return new ScriptReader(_Script);
		}

		private bool CheckSig(byte[] vchSig, byte[] vchPubKey, Script scriptCode, Transaction txTo, int nIn, SigHash nHashType, ScriptVerify flags)
		{
			throw new NotImplementedException();
		}

		private bool IsCanonicalPubKey(byte[] vchPubKey, ScriptVerify flags)
		{
			throw new NotImplementedException();
		}

		private bool IsCanonicalSignature(byte[] vchSig, ScriptVerify flags)
		{
			throw new NotImplementedException();
		}

		private int FindAndDelete(Script script)
		{
			int nFound = 0;
			if(script._Script.Length == 0)
				return nFound;

			bool[] suppressed = new bool[_Script.Length];
			for(int i = 0 ; i < this._Script.Length - script._Script.Length + 1 ; i++)
			{
				if(Utils.ArrayEqual(_Script, i, script._Script, 0, script._Script.Length))
				{
					i += script._Script.Length;
					suppressed[i] = true;
					nFound++;
				}
			}

			_Script = _Script.Where((b, i) => !suppressed[i]).ToArray();
			return nFound;
		}

		private void insert(ref Stack<byte[]> stack, int i, byte[] vch)
		{
			var newStack = new Stack<byte[]>();
			var count = stack.Count;
			stack = new Stack<byte[]>(stack); //Reverse the stack
			for(int y = 0 ; y < count + 1 ; y++)
			{
				if(y == i)
					newStack.Push(vch);
				else
					newStack.Push(stack.Pop());
			}
			stack = newStack;
		}

		private BigInteger CastToBigNum(bool v)
		{
			return new BigInteger(v ? 1 : 0);
		}
		private BigInteger CastToBigNum(byte[] b)
		{
			if(b.Length > 4)
				throw new InvalidOperationException("CastToBigNum() : overflow");
			return Utils.BytesToBigInteger(b);
		}

		static void swap<T>(ref Stack<T> stack, int i, int i2)
		{
			var values = stack.ToArray();
			Array.Reverse(values);
			var temp = values[values.Length + i];
			values[values.Length + i] = values[values.Length + i2];
			values[values.Length + i2] = temp;
			stack = new Stack<T>(values);
		}

		private void erase(ref Stack<byte[]> stack, int from, int to)
		{
			var values = stack.ToArray();
			Array.Reverse(values);
			stack = new Stack<byte[]>();
			for(int i = 0 ; i < values.Length ; i++)
			{
				if(from <= i && i < to)
					continue;
				stack.Push(values[i]);
			}
		}
		private void erase(ref Stack<byte[]> stack, int i)
		{
			erase(ref stack, i, i + 1);
		}
		static T top<T>(Stack<T> stack, int i)
		{
			var array = stack.ToArray();
			Array.Reverse(array);
			return array[stack.Count + i];
			//stacktop(i)  (altstack.at(altstack.size()+(i)))
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			VarString str = new VarString(_Script);
			stream.ReadWrite(ref str);
			if(!stream.Serializing)
				_Script = str.GetString();
		}

		#endregion

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_Script);
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			ScriptReader reader = new ScriptReader(_Script)
			{
				IgnoreIncoherentPushData = true
			};

			Op op = null;
			while((op = reader.Read()) != null)
			{
				builder.Append(" ");
				builder.Append(op.ToString());
			}

			return builder == null ? "" : builder.ToString().Trim();
		}


		public bool IsPayToScriptHash
		{
			get
			{
				return (_Script.Length == 23 &&
						_Script[0] == (byte)OpcodeType.OP_HASH160 &&
						_Script[1] == 0x14 &&
						_Script[22] == (byte)OpcodeType.OP_EQUAL);
			}
		}
	}
}
