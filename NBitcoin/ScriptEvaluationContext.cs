using System.Diagnostics;
using NBitcoin.Crypto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NBitcoin
{
	public enum ScriptError
	{
		OK = 0,
		UnknownError,
		EvalFalse,
		OpReturn,

		/* Max sizes */
		ScriptSize,
		PushSize,
		OpCount,
		StackSize,
		SigCount,
		PubkeyCount,

		/* Failed verify operations */
		Verify,
		EqualVerify,
		CheckMultiSigVerify,
		CheckSigVerify,
		NumEqualVerify,

		/* Logical/Format/Canonical errors */
		BadOpCode,
		DisabledOpCode,
		InvalidStackOperation,
		InvalidAltStackOperation,
		UnbalancedConditional,

		/* BIP62 */
		SigHashType,
		SigDer,
		MinimalData,
		SigPushOnly,
		SigHighS,
		SigNullDummy,
		PubKeyType,
		CleanStack,

		/* softfork safeness */
		DiscourageUpgradableNops,
	}
	public class ScriptEvaluationContext
	{
		class CScriptNum
		{
			const long nMaxNumSize = 4;
			/**
			 * Numeric opcodes (OP_1ADD, etc) are restricted to operating on 4-byte integers.
			 * The semantics are subtle, though: operands must be in the range [-2^31 +1...2^31 -1],
			 * but results may overflow (and are valid as long as they are not used in a subsequent
			 * numeric operation). CScriptNum enforces those semantics by storing results as
			 * an int64 and allowing out-of-range values to be returned as a vector of bytes but
			 * throwing an exception if arithmetic is done or the result is interpreted as an integer.
			 */

			public CScriptNum(long n)
			{
				m_value = n;
			}
			private long m_value;

			public CScriptNum(byte[] vch, bool fRequireMinimal)
			{
				if(vch.Length > nMaxNumSize)
				{
					throw new ArgumentException("script number overflow", "vch");
				}
				if(fRequireMinimal && vch.Length > 0)
				{
					// Check that the number is encoded with the minimum possible
					// number of bytes.
					//
					// If the most-significant-byte - excluding the sign bit - is zero
					// then we're not minimal. Note how this test also rejects the
					// negative-zero encoding, 0x80.
					if((vch[vch.Length - 1] & 0x7f) == 0)
					{
						// One exception: if there's more than one byte and the most
						// significant bit of the second-most-significant-byte is set
						// it would conflict with the sign bit. An example of this case
						// is +-255, which encode to 0xff00 and 0xff80 respectively.
						// (big-endian).
						if(vch.Length <= 1 || (vch[vch.Length - 2] & 0x80) == 0)
						{
							throw new ArgumentException("non-minimally encoded script number", "vch");
						}
					}
				}
				m_value = set_vch(vch);
			}

			public override int GetHashCode()
			{
				return getint();
			}
			public override bool Equals(object obj)
			{
				if(obj == null || !(obj is CScriptNum))
					return false;
				CScriptNum item = (CScriptNum)obj;
				return m_value == item.m_value;
			}
			public static bool operator ==(CScriptNum num, long rhs)
			{
				return num.m_value == rhs;
			}
			public static bool operator !=(CScriptNum num, long rhs)
			{
				return num.m_value != rhs;
			}
			public static bool operator <=(CScriptNum num, long rhs)
			{
				return num.m_value <= rhs;
			}
			public static bool operator <(CScriptNum num, long rhs)
			{
				return num.m_value < rhs;
			}
			public static bool operator >=(CScriptNum num, long rhs)
			{
				return num.m_value >= rhs;
			}
			public static bool operator >(CScriptNum num, long rhs)
			{
				return num.m_value > rhs;
			}

			public static bool operator ==(CScriptNum a, CScriptNum b)
			{
				return a.m_value == b.m_value;
			}
			public static bool operator !=(CScriptNum a, CScriptNum b)
			{
				return a.m_value != b.m_value;
			}
			public static bool operator <=(CScriptNum a, CScriptNum b)
			{
				return a.m_value <= b.m_value;
			}
			public static bool operator <(CScriptNum a, CScriptNum b)
			{
				return a.m_value < b.m_value;
			}
			public static bool operator >=(CScriptNum a, CScriptNum b)
			{
				return a.m_value >= b.m_value;
			}
			public static bool operator >(CScriptNum a, CScriptNum b)
			{
				return a.m_value > b.m_value;
			}

			public static CScriptNum operator +(CScriptNum num, long rhs)
			{
				return new CScriptNum(num.m_value + rhs);
			}
			public static CScriptNum operator -(CScriptNum num, long rhs)
			{
				return new CScriptNum(num.m_value - rhs);
			}
			public static CScriptNum operator +(CScriptNum a, CScriptNum b)
			{
				return new CScriptNum(a.m_value + b.m_value);
			}
			public static CScriptNum operator -(CScriptNum a, CScriptNum b)
			{
				return new CScriptNum(a.m_value - b.m_value);
			}


			public static CScriptNum operator -(CScriptNum num)
			{
				assert(num.m_value != Int64.MinValue);
				return new CScriptNum(-num.m_value);
			}

			private static void assert(bool result)
			{
				if(!result)
					throw new InvalidOperationException("Assertion fail for CScriptNum");
			}

			public static implicit operator CScriptNum(long rhs)
			{
				return new CScriptNum(rhs);
			}




			public int getint()
			{
				if(m_value > int.MaxValue)
					return int.MaxValue;
				else if(m_value < int.MinValue)
					return int.MinValue;
				return (int)m_value;
			}

			public byte[] getvch()
			{
				return serialize(m_value);
			}

			static byte[] serialize(long value)
			{
				if(value == 0)
					return new byte[0];

				var result = new List<byte>();
				bool neg = value < 0;
				long absvalue = neg ? -value : value;

				while(absvalue != 0)
				{
					result.Add((byte)(absvalue & 0xff));
					absvalue >>= 8;
				}

				//    - If the most significant byte is >= 0x80 and the value is positive, push a
				//    new zero-byte to make the significant byte < 0x80 again.

				//    - If the most significant byte is >= 0x80 and the value is negative, push a
				//    new 0x80 byte that will be popped off when converting to an integral.

				//    - If the most significant byte is < 0x80 and the value is negative, add
				//    0x80 to it, since it will be subtracted and interpreted as a negative when
				//    converting to an integral.

				if((result[result.Count - 1] & 0x80) != 0)
					result.Add((byte)(neg ? 0x80 : 0));
				else if(neg)
					result[result.Count - 1] |= 0x80;

				return result.ToArray();
			}

			static long set_vch(byte[] vch)
			{
				if(vch.Length == 0)
					return 0;

				long result = 0;
				for(int i = 0 ; i != vch.Length ; ++i)
					result |= ((long)(vch[i])) << 8 * i;

				// If the input vector's most significant byte is 0x80, remove it from
				// the result's msb and return a negative.
				if((vch[vch.Length - 1] & 0x80) != 0)
				{
					var temp = ~(0x80UL << (8 * (vch.Length - 1)));
					return -((long)((ulong)result & temp));
				}

				return result;
			}
		}

		ContextStack<byte[]> _stack = new ContextStack<byte[]>();

		public ContextStack<byte[]> Stack
		{
			get
			{
				return _stack;
			}
		}

		public ScriptEvaluationContext()
		{
			ScriptVerify = NBitcoin.ScriptVerify.Standard;
			SigHash = NBitcoin.SigHash.Undefined;
			Error = ScriptError.UnknownError;
		}
		public ScriptVerify ScriptVerify
		{
			get;
			set;
		}
		public SigHash SigHash
		{
			get;
			set;
		}

		public bool VerifyScript(Script scriptSig, Script scriptPubKey, Transaction txTo, int nIn)
		{
			SetError(ScriptError.UnknownError);
			if((ScriptVerify & ScriptVerify.SigPushOnly) != 0 && !scriptSig.IsPushOnly)
				return SetError(ScriptError.SigPushOnly);

			ScriptEvaluationContext evaluationCopy = null;

			if(!EvalScript(scriptSig, txTo, nIn))
				return false;
			if((ScriptVerify & ScriptVerify.P2SH) != 0)
			{
				evaluationCopy = Clone();
			}
			if(!EvalScript(scriptPubKey, txTo, nIn))
				return false;

			if(Result == null || Result.Value == false)
				return SetError(ScriptError.EvalFalse);

			// Additional validation for spend-to-script-hash transactions:
			if(((ScriptVerify & ScriptVerify.P2SH) != 0) && scriptPubKey.IsPayToScriptHash)
			{
				Load(evaluationCopy);
				evaluationCopy = this;
				if(!scriptSig.IsPushOnly)
					return SetError(ScriptError.SigPushOnly);

				// stackCopy cannot be empty here, because if it was the
				// P2SH  HASH <> EQUAL  scriptPubKey would be evaluated with
				// an empty stack and the EvalScript above would return false.
				if(evaluationCopy.Stack.Count == 0)
					throw new InvalidOperationException("stackCopy cannot be empty here");

				var redeem = new Script(evaluationCopy.Stack.Pop());

				if(!evaluationCopy.EvalScript(redeem, txTo, nIn))
					return false;

				if(evaluationCopy.Result == null)
					return SetError(ScriptError.EvalFalse);

				if(!evaluationCopy.Result.Value)
					return SetError(ScriptError.EvalFalse);
			}

			// The CLEANSTACK check is only performed after potential P2SH evaluation,
			// as the non-P2SH evaluation of a P2SH script will obviously not result in
			// a clean stack (the P2SH inputs remain).
			if((ScriptVerify & ScriptVerify.CleanStack) != 0)
			{
				// Disallow CLEANSTACK without P2SH, as otherwise a switch CLEANSTACK->P2SH+CLEANSTACK
				// would be possible, which is not a softfork (and P2SH should be one).
				if((ScriptVerify & ScriptVerify.P2SH) == 0)
					throw new InvalidOperationException("ScriptVerify : CleanStack without P2SH is not allowed");
				if(Stack.Count != 1)
					return SetError(ScriptError.CleanStack);
			}


			return true;
		}


		static readonly byte[] vchFalse = new byte[0];
		static readonly byte[] vchZero = new byte[0];
		static readonly byte[] vchTrue = new byte[] { 1 };

		private const int MAX_SCRIPT_ELEMENT_SIZE = 520;

		public bool EvalScript(Script s, Transaction txTo, int nIn)
		{
			if (s.Length > 10000)
				return SetError(ScriptError.ScriptSize);

			SetError(ScriptError.UnknownError);

			var script = s.CreateReader();
			var pbegincodehash = 0;

			var vfExec = new Stack<bool>();
			var altstack = new ContextStack<byte[]>();
	
			var nOpCount = 0;
			var fRequireMinimal = (ScriptVerify & ScriptVerify.MinimalData) != 0;

			try
			{
				Op opcode;
				while((opcode = script.Read()) != null)
				{
					//
					// Read instruction
					//
					if(opcode.PushData != null && opcode.PushData.Length > MAX_SCRIPT_ELEMENT_SIZE)
						return SetError(ScriptError.PushSize);

					// Note how OP_RESERVED does not count towards the opcode limit.
					if(opcode.Code > OpcodeType.OP_16 && ++nOpCount > 201)
						return SetError(ScriptError.OpCount);

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
					{
						return SetError(ScriptError.DisabledOpCode);
					}

					bool fExec = vfExec.All(o => o); //!count(vfExec.begin(), vfExec.end(), false);
					if (fExec && opcode.IsInvalid)
						return SetError(ScriptError.UnknownError);

					if(fExec && 0 <= (int)opcode.Code && (int)opcode.Code <= (int)OpcodeType.OP_PUSHDATA4)
					{
						if(fRequireMinimal && !CheckMinimalPush(opcode.PushData, opcode.Code))
							return SetError(ScriptError.MinimalData);

						_stack.Push(opcode.PushData);
					}

					//if(fExec && opcode.PushData != null)
					//	_Stack.Push(opcode.PushData);
					else if(fExec || (OpcodeType.OP_IF <= opcode.Code && opcode.Code <= OpcodeType.OP_ENDIF))
					{
						switch (opcode.Code)
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
							case OpcodeType.OP_16: {
								// ( -- value)
								var num = new CScriptNum((int) opcode.Code - (int) (OpcodeType.OP_1 - 1));
								_stack.Push(num.getvch());
								break;
							}
							//
							// Control
							//
							case OpcodeType.OP_NOP:
								break;
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
								if ((ScriptVerify & ScriptVerify.DiscourageUpgradableNops) != 0)
								{
									return SetError(ScriptError.DiscourageUpgradableNops);
								}
								break;

							case OpcodeType.OP_IF:
							case OpcodeType.OP_NOTIF: {
								// <expression> if [statements] [else [statements]] endif
								var bValue = false;
								if (fExec)
								{
									if (_stack.Count < 1)
										return SetError(ScriptError.UnbalancedConditional);

									var vch = _stack.Top(-1);
									bValue = CastToBool(vch);
									if (opcode.Code == OpcodeType.OP_NOTIF)
										bValue = !bValue;
									_stack.Pop();
								}
								vfExec.Push(bValue);
								break;
							}
							case OpcodeType.OP_ELSE: {
								if (vfExec.Count == 0)
									return SetError(ScriptError.UnbalancedConditional);

								var v = vfExec.Pop();
								vfExec.Push(!v);
								break;
							}
							case OpcodeType.OP_ENDIF:{
								if (vfExec.Count == 0)
									return SetError(ScriptError.UnbalancedConditional);

								vfExec.Pop();
								break;
							}
							case OpcodeType.OP_VERIFY: {
								// (true -- ) or
								// (false -- false) and return
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								if (!CastToBool(_stack.Top(-1)))
									return SetError(ScriptError.Verify);

								_stack.Pop();
								break;
							}
							case OpcodeType.OP_RETURN: {
								return SetError(ScriptError.OpReturn);
							}
							//
							// Stack ops
							//
							case OpcodeType.OP_TOALTSTACK: {
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidAltStackOperation);

								altstack.Push(_stack.Top(-1));
								_stack.Pop();
								break;
							}
							case OpcodeType.OP_FROMALTSTACK: {
								if (altstack.Count < 1)
									return SetError(ScriptError.InvalidAltStackOperation);

								_stack.Push(altstack.Top(-1));
								altstack.Pop();
								break;
							}
							case OpcodeType.OP_2DROP: {
								// (x1 x2 -- )
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								_stack.Pop();
								_stack.Pop();
								break;
							}
							case OpcodeType.OP_2DUP: {
								// (x1 x2 -- x1 x2 x1 x2)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								var vch1 = _stack.Top(-2);
								var vch2 = _stack.Top(-1);
								_stack.Push(vch1);
								_stack.Push(vch2);
								break;
							}
							case OpcodeType.OP_3DUP: {
								// (x1 x2 x3 -- x1 x2 x3 x1 x2 x3)
								if (_stack.Count < 3)
									return SetError(ScriptError.InvalidStackOperation);
								
								var vch1 = _stack.Top(-3);
								var vch2 = _stack.Top(-2);
								var vch3 = _stack.Top(-1);
								_stack.Push(vch1);
								_stack.Push(vch2);
								_stack.Push(vch3);
								break;
							}
							case OpcodeType.OP_2OVER: {
								// (x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2)
								if (_stack.Count < 4)
									return SetError(ScriptError.InvalidStackOperation);
								
								var vch1 = _stack.Top(-4);
								var vch2 = _stack.Top(-3);
								_stack.Push(vch1);
								_stack.Push(vch2);
								break;
							}
							case OpcodeType.OP_2ROT: {
								// (x1 x2 x3 x4 x5 x6 -- x3 x4 x5 x6 x1 x2)
								if (_stack.Count < 6)
									return SetError(ScriptError.InvalidStackOperation);
								
								var vch1 = _stack.Top(-6);
								var vch2 = _stack.Top(-5);
								_stack.Remove(-6, -4);
								_stack.Push(vch1);
								_stack.Push(vch2);
								break;
							}
							case OpcodeType.OP_2SWAP: {
								// (x1 x2 x3 x4 -- x3 x4 x1 x2)
								if (_stack.Count < 4)
									return SetError(ScriptError.InvalidStackOperation);

								_stack.Swap(-4, -2);
								_stack.Swap(-3, -1);
								break;
							}
							case OpcodeType.OP_IFDUP: {
								// (x - 0 | x x)
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								var vch = _stack.Top(-1);
								if (CastToBool(vch))
									_stack.Push(vch);
								break;
							}
							case OpcodeType.OP_DEPTH: {
								// -- stacksize
								var bn = new CScriptNum(_stack.Count);
								_stack.Push(bn.getvch());
								break;
							}
							case OpcodeType.OP_DROP: {
								// (x -- )
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								_stack.Pop();
								break;
							}
							case OpcodeType.OP_DUP: {
								// (x -- x x)
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								var vch = _stack.Top(-1);
								_stack.Push(vch);
								break;
							}
							case OpcodeType.OP_NIP: {
								// (x1 x2 -- x2)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								_stack.Remove(-2);
								break;
							}
							case OpcodeType.OP_OVER: {
								// (x1 x2 -- x1 x2 x1)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								var vch = _stack.Top(-2);
								_stack.Push(vch);
								break;
							}
							case OpcodeType.OP_PICK:
							case OpcodeType.OP_ROLL: {
								// (xn ... x2 x1 x0 n - xn ... x2 x1 x0 xn)
								// (xn ... x2 x1 x0 n - ... x2 x1 x0 xn)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								int n = new CScriptNum(_stack.Top(-1), fRequireMinimal).getint();
								_stack.Pop();
								if (n < 0 || n >= _stack.Count)
									return SetError(ScriptError.InvalidStackOperation);

								var vch = _stack.Top(-n - 1);
								if (opcode.Code == OpcodeType.OP_ROLL)
									_stack.Remove(-n - 1);
								_stack.Push(vch);
								break;
							}
							case OpcodeType.OP_ROT: {
								// (x1 x2 x3 -- x2 x3 x1)
								//  x2 x1 x3  after first swap
								//  x2 x3 x1  after second swap
								if (_stack.Count < 3)
									return SetError(ScriptError.InvalidStackOperation);

								_stack.Swap(-3, -2);
								_stack.Swap(-2, -1);
								break;
							}
							case OpcodeType.OP_SWAP: {
								// (x1 x2 -- x2 x1)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								_stack.Swap(-2, -1);
								break;
							}
							case OpcodeType.OP_TUCK: {
								// (x1 x2 -- x2 x1 x2)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								var vch = _stack.Top(-1);
								_stack.Insert(0, vch);
								break;
							}
							case OpcodeType.OP_SIZE: {
								// (in -- in size)
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								var bn = new CScriptNum(_stack.Top(-1).Length);
								_stack.Push(bn.getvch());
								break;
							}
							//
							// Bitwise logic
							//
							case OpcodeType.OP_EQUAL:
							case OpcodeType.OP_EQUALVERIFY: {
								//case OpcodeType.OP_NOTEQUAL: // use OpcodeType.OP_NUMNOTEQUAL
								// (x1 x2 - bool)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								var vch1 = _stack.Top(-2);
								var vch2 = _stack.Top(-1);
								bool fEqual = Utils.ArrayEqual(vch1, vch2);
								// OpcodeType.OP_NOTEQUAL is disabled because it would be too easy to say
								// something like n != 1 and have some wiseguy pass in 1 with extra
								// zero bytes after it (numerically, 0x01 == 0x0001 == 0x000001)
								//if (opcode == OpcodeType.OP_NOTEQUAL)
								//    fEqual = !fEqual;
								_stack.Pop();
								_stack.Pop();
								_stack.Push(fEqual ? vchTrue : vchFalse);
								if (opcode.Code == OpcodeType.OP_EQUALVERIFY)
								{
									if (!fEqual)
										return SetError(ScriptError.EqualVerify);

									_stack.Pop();
								}
								break;
							}
							//
							// Numeric
							//
							case OpcodeType.OP_1ADD:
							case OpcodeType.OP_1SUB:
							case OpcodeType.OP_NEGATE:
							case OpcodeType.OP_ABS:
							case OpcodeType.OP_NOT:
							case OpcodeType.OP_0NOTEQUAL: {
								// (in -- out)
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								var bn = new CScriptNum(_stack.Top(-1), fRequireMinimal);
								switch (opcode.Code)
								{
									case OpcodeType.OP_1ADD:
										bn += 1;
										break;
									case OpcodeType.OP_1SUB:
										bn -= 1;
										break;
									case OpcodeType.OP_NEGATE:
										bn = -bn;
										break;
									case OpcodeType.OP_ABS:
										if (bn < 0)
											bn = -bn;
										break;
									case OpcodeType.OP_NOT:
										bn = bn == 0 ? 1 : 0;
										break;
									case OpcodeType.OP_0NOTEQUAL:
										bn = bn != 0 ? 1 : 0;
										break;
									default:
										throw new NotSupportedException("invalid opcode");
								}
								_stack.Pop();
								_stack.Push(bn.getvch());
								break;
							}
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
							case OpcodeType.OP_MAX: {
								// (x1 x2 -- out)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								var bn1 = new CScriptNum(_stack.Top(-2), fRequireMinimal);
								var bn2 = new CScriptNum(_stack.Top(-1), fRequireMinimal);
								var bn = new CScriptNum(0);
								switch (opcode.Code)
								{
									case OpcodeType.OP_ADD:
										bn = bn1 + bn2;
										break;

									case OpcodeType.OP_SUB:
										bn = bn1 - bn2;
										break;

									case OpcodeType.OP_BOOLAND:
										bn = bn1 != 0 && bn2 != 0 ? 1 : 0;
										break;
									case OpcodeType.OP_BOOLOR:
										bn = bn1 != 0 || bn2 != 0 ? 1 : 0;
										break;
									case OpcodeType.OP_NUMEQUAL:
										bn = (bn1 == bn2) ? 1 : 0;
										break;
									case OpcodeType.OP_NUMEQUALVERIFY:
										bn = (bn1 == bn2) ? 1 : 0;
										break;
									case OpcodeType.OP_NUMNOTEQUAL:
										bn = (bn1 != bn2) ? 1 : 0;
										break;
									case OpcodeType.OP_LESSTHAN:
										bn = (bn1 < bn2) ? 1 : 0;
										break;
									case OpcodeType.OP_GREATERTHAN:
										bn = (bn1 > bn2) ? 1 : 0;
										break;
									case OpcodeType.OP_LESSTHANOREQUAL:
										bn = (bn1 <= bn2) ? 1 : 0;
										break;
									case OpcodeType.OP_GREATERTHANOREQUAL:
										bn = (bn1 >= bn2) ? 1 : 0;
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
								_stack.Pop();
								_stack.Pop();
								_stack.Push(bn.getvch());

								if (opcode.Code == OpcodeType.OP_NUMEQUALVERIFY)
								{
									if (!CastToBool(_stack.Top(-1)))
										return SetError(ScriptError.NumEqualVerify);
									_stack.Pop();
								}
								break;
							}
							case OpcodeType.OP_WITHIN: {
								// (x min max -- out)
								if (_stack.Count < 3)
									return SetError(ScriptError.InvalidStackOperation);

								var bn1 = new CScriptNum(_stack.Top(-3), fRequireMinimal);
								var bn2 = new CScriptNum(_stack.Top(-2), fRequireMinimal);
								var bn3 = new CScriptNum(_stack.Top(-1), fRequireMinimal);
								bool fValue = (bn2 <= bn1 && bn1 < bn3);
								_stack.Pop();
								_stack.Pop();
								_stack.Pop();
								_stack.Push(fValue ? vchTrue : vchFalse);
								break;
							}
							//
							// Crypto
							//
							case OpcodeType.OP_RIPEMD160:
							case OpcodeType.OP_SHA1:
							case OpcodeType.OP_SHA256:
							case OpcodeType.OP_HASH160:
							case OpcodeType.OP_HASH256: {
								// (in -- hash)
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								var vch = _stack.Top(-1);
								byte[] vchHash = null; //((opcode == OpcodeType.OP_RIPEMD160 || opcode == OpcodeType.OP_SHA1 || opcode == OpcodeType.OP_HASH160) ? 20 : 32);
								if (opcode.Code == OpcodeType.OP_RIPEMD160)
									vchHash = Hashes.RIPEMD160(vch, 0, vch.Length);
								else if (opcode.Code == OpcodeType.OP_SHA1)
									vchHash = Hashes.SHA1(vch, 0, vch.Length);
								else if (opcode.Code == OpcodeType.OP_SHA256)
									vchHash = Hashes.SHA256(vch, 0, vch.Length);
								else if (opcode.Code == OpcodeType.OP_HASH160)
									vchHash = Hashes.Hash160(vch, 0, vch.Length).ToBytes();
								else if (opcode.Code == OpcodeType.OP_HASH256)
									vchHash = Hashes.Hash256(vch, 0, vch.Length).ToBytes();
								_stack.Pop();
								_stack.Push(vchHash);
								break;
							}
							case OpcodeType.OP_CODESEPARATOR: {
								// Hash starts after the code separator
								pbegincodehash = (int) script.Inner.Position;
								break;
							}
							case OpcodeType.OP_CHECKSIG:
							case OpcodeType.OP_CHECKSIGVERIFY: {
								// (sig pubkey -- bool)
								if (_stack.Count < 2)
									return SetError(ScriptError.InvalidStackOperation);

								var vchSig = _stack.Top(-2);
								var vchPubKey = _stack.Top(-1);

								////// debug print
								//PrintHex(vchSig.begin(), vchSig.end(), "sig: %s\n");
								//PrintHex(vchPubKey.begin(), vchPubKey.end(), "pubkey: %s\n");

								// Subset of script starting at the most recent codeseparator
								var scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
								// Drop the signature, since there's no way for a signature to sign itself
								scriptCode.FindAndDelete(vchSig);

								if (!CheckSignatureEncoding(vchSig) || !CheckPubKeyEncoding(vchPubKey))
								{
									//serror is set
									return false;
								}

								bool fSuccess = CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn);

								_stack.Pop();
								_stack.Pop();
								_stack.Push(fSuccess ? vchTrue : vchFalse);
								if (opcode.Code == OpcodeType.OP_CHECKSIGVERIFY)
								{
									if (!fSuccess)
										return SetError(ScriptError.CheckSigVerify);

									_stack.Pop();
								}
								break;
							}
							case OpcodeType.OP_CHECKMULTISIG:
							case OpcodeType.OP_CHECKMULTISIGVERIFY: {
								// ([sig ...] num_of_signatures [pubkey ...] num_of_pubkeys -- bool)

								int i = 1;
								if (_stack.Count < i)
									return SetError(ScriptError.InvalidStackOperation);

								int nKeysCount = new CScriptNum(_stack.Top(-i), fRequireMinimal).getint();
								if (nKeysCount < 0 || nKeysCount > 20)
									return SetError(ScriptError.PubkeyCount);

								nOpCount += nKeysCount;
								if (nOpCount > 201)
									return SetError(ScriptError.OpCount);

								int ikey = ++i;
								i += nKeysCount;
								if (_stack.Count < i)
									return SetError(ScriptError.InvalidStackOperation);

								int nSigsCount = new CScriptNum(_stack.Top(-i), fRequireMinimal).getint();
								if (nSigsCount < 0 || nSigsCount > nKeysCount)
									return SetError(ScriptError.SigCount);

								int isig = ++i;
								i += nSigsCount;
								if (_stack.Count < i)
									return SetError(ScriptError.InvalidStackOperation);

								// Subset of script starting at the most recent codeseparator
								Script scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
								// Drop the signatures, since there's no way for a signature to sign itself
								for (int k = 0; k < nSigsCount; k++)
								{
									var vchSig = _stack.Top(-isig - k);
									scriptCode.FindAndDelete(vchSig);
								}

								bool fSuccess = true;
								while (fSuccess && nSigsCount > 0)
								{
									var vchSig = _stack.Top(-isig);
									var vchPubKey = _stack.Top(-ikey);

									// Note how this makes the exact order of pubkey/signature evaluation
									// distinguishable by CHECKMULTISIG NOT if the STRICTENC flag is set.
									// See the script_(in)valid tests for details.
									if (!CheckSignatureEncoding(vchSig) || !CheckPubKeyEncoding(vchPubKey))
									{
										// serror is set
										return false;
									}

									bool fOk = CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn);

									if (fOk)
									{
										isig++;
										nSigsCount--;
									}
									ikey++;
									nKeysCount--;

									// If there are more signatures left than keys left,
									// then too many signatures have failed
									if (nSigsCount > nKeysCount)
										fSuccess = false;
								}

								_stack.Clear(i-1);

								// A bug causes CHECKMULTISIG to consume one extra argument
								// whose contents were not checked in any way.
								//
								// Unfortunately this is a potential source of mutability,
								// so optionally verify it is exactly equal to zero prior
								// to removing it from the stack.
								if (_stack.Count < 1)
									return SetError(ScriptError.InvalidStackOperation);

								if (((ScriptVerify & ScriptVerify.NullDummy) != 0) && _stack.Top(-1).Length != 0)
									return SetError(ScriptError.SigNullDummy);

								_stack.Pop();

								_stack.Push(fSuccess ? vchTrue : vchFalse);

								if (opcode.Code == OpcodeType.OP_CHECKMULTISIGVERIFY)
								{
									if (!fSuccess)
										return SetError(ScriptError.CheckMultiSigVerify);

									_stack.Pop();
								}
								break;
							}
							default:
								return SetError(ScriptError.BadOpCode);
						}
					}
					// Size limits
					if(_stack.Count + altstack.Count > 1000)
						return SetError(ScriptError.StackSize);
				}
			}
			catch(Exception ex)
			{
				ThrownException = ex;
				return SetError(ScriptError.UnknownError);
			}

			if(vfExec.Count != 0)
				return SetError(ScriptError.UnbalancedConditional);

			return SetSuccess(ScriptError.OK);
		}

		private bool SetSuccess(ScriptError scriptError)
		{
			Error = ScriptError.OK;
			return true;
		}

		private bool SetError(ScriptError scriptError)
		{
			Error = scriptError;
			return false;
		}

		private bool IsCompressedOrUncompressedPubKey(byte[] vchPubKey)
		{
			if(vchPubKey.Length < 33)
			{
				//  Non-canonical public key: too short
				return false;
			}
			if(vchPubKey[0] == 0x04)
			{
				if(vchPubKey.Length != 65)
				{
					//  Non-canonical public key: invalid length for uncompressed key
					return false;
				}
			}
			else if(vchPubKey[0] == 0x02 || vchPubKey[0] == 0x03)
			{
				if(vchPubKey.Length != 33)
				{
					//  Non-canonical public key: invalid length for compressed key
					return false;
				}
			}
			else
			{
				//  Non-canonical public key: neither compressed nor uncompressed
				return false;
			}
			return true;
		}

		private bool CheckSignatureEncoding(byte[] vchSig)
		{
			// Empty signature. Not strictly DER encoded, but allowed to provide a
			// compact way to provide an invalid signature for use with CHECK(MULTI)SIG
			if(vchSig.Length == 0)
			{
				return true;
			}
			if((ScriptVerify & (ScriptVerify.DerSig | ScriptVerify.LowS | ScriptVerify.StrictEnc)) != 0 && !IsDERSignature(vchSig))
			{
				Error = ScriptError.SigDer;
				return false;
			}
			else if((ScriptVerify & ScriptVerify.LowS) != 0 && !IsLowDERSignature(vchSig))
			{
				// serror is set
				return false;
			}
			else if((ScriptVerify & ScriptVerify.StrictEnc) != 0 && !IsDefinedHashtypeSignature(vchSig))
			{
				Error = ScriptError.SigHashType;
				return false;
			}
			return true;
		}

		private bool CheckPubKeyEncoding(byte[] vchPubKey)
		{
			if((ScriptVerify & ScriptVerify.StrictEnc) != 0 && !IsCompressedOrUncompressedPubKey(vchPubKey))
			{
				Error = ScriptError.PubKeyType;
				return false;
			}
			return true;
		}

		private bool IsDefinedHashtypeSignature(byte[] vchSig)
		{
			if(vchSig.Length == 0)
			{
				return false;
			}

			var temp = ~(SigHash.AnyoneCanPay);
			byte nHashType = (byte)(vchSig[vchSig.Length - 1] & (byte)temp);
			if(nHashType < (byte)SigHash.All || nHashType > (byte)SigHash.Single)
				return false;

			return true;
		}

		private bool IsLowDERSignature(byte[] vchSig)
		{
			if(!IsDERSignature(vchSig))
			{
				Error = ScriptError.SigDer;
				return false;
			}
			int nLenR = vchSig[3];
			int nLenS = vchSig[5 + nLenR];
			var S = 6 + nLenR;
			// If the S value is above the order of the curve divided by two, its
			// complement modulo the order could have been used instead, which is
			// one byte shorter when encoded correctly.
			if(!CheckSignatureElement(vchSig, S, nLenS, true))
			{
				Error = ScriptError.SigHighS;
				return false;
			}

			return true;
		}

		public ScriptError Error
		{
			get;
			set;
		}

		static byte[] vchMaxModOrder = new byte[]{
0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
 0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFE,
 0xBA,0xAE,0xDC,0xE6,0xAF,0x48,0xA0,0x3B,
0xBF,0xD2,0x5E,0x8C,0xD0,0x36,0x41,0x40
};

		static byte[] vchMaxModHalfOrder = new byte[]{
 0x7F,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
 0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
 0x5D,0x57,0x6E,0x73,0x57,0xA4,0x50,0x1D,
0xDF,0xE9,0x2F,0x46,0x68,0x1B,0x20,0xA0
};

		private bool CheckSignatureElement(byte[] vchSig, int i, int len, bool half)
		{
			return vchSig != null
						&&
						 CompareBigEndian(vchSig, i, len, vchZero, 0) > 0 &&
						 CompareBigEndian(vchSig, i, len, half ? vchMaxModHalfOrder : vchMaxModOrder, 32) <= 0;
		}

		private int CompareBigEndian(byte[] c1, int ic1, int c1len, byte[] c2, int c2len)
		{
			int ic2 = 0;
			while(c1len > c2len)
			{
				if(c1[ic1] != 0)
					return 1;
				ic1++;
				c1len--;
			}
			while(c2len > c1len)
			{
				if(c2[ic2] != 0)
					return -1;
				ic2++;
				c2len--;
			}
			while(c1len > 0)
			{
				if(c1[ic1] > c2[ic2])
					return 1;
				if(c2[ic2] > c1[ic1])
					return -1;
				ic1++;
				ic2++;
				c1len--;
			}
			return 0;
		}



		private bool IsDERSignature(byte[] vchSig)
		{
			if(vchSig.Length < 9)
			{
				//  Non-canonical signature: too short
				return false;
			}
			if(vchSig.Length > 73)
			{
				// Non-canonical signature: too long
				return false;
			}
			if(vchSig[0] != 0x30)
			{
				//  Non-canonical signature: wrong type
				return false;
			}
			if(vchSig[1] != vchSig.Length - 3)
			{
				//  Non-canonical signature: wrong length marker
				return false;
			}
			uint nLenR = vchSig[3];
			if(5 + nLenR >= vchSig.Length)
			{
				//  Non-canonical signature: S length misplaced
				return false;
			}
			uint nLenS = vchSig[5 + nLenR];
			if((ulong)(nLenR + nLenS + 7) != (ulong)vchSig.Length)
			{
				//  Non-canonical signature: R+S length mismatch
				return false;
			}

			var R = 4;
			if(vchSig[R + -2] != 0x02)
			{
				//  Non-canonical signature: R value type mismatch
				return false;
			}
			if(nLenR == 0)
			{
				//  Non-canonical signature: R length is zero
				return false;
			}
			if((vchSig[R] & 0x80) != 0)
			{
				//  Non-canonical signature: R value negative
				return false;
			}
			if(nLenR > 1 && (vchSig[R] == 0x00) && (vchSig[R + 1] & 0x80) == 0)
			{
				//  Non-canonical signature: R value excessively padded
				return false;
			}

			var S = 6 + nLenR;
			if(vchSig[S + -2] != 0x02)
			{
				//  Non-canonical signature: S value type mismatch
				return false;
			}
			if(nLenS == 0)
			{
				//  Non-canonical signature: S length is zero
				return false;
			}
			if((vchSig[S] & 0x80) != 0)
			{
				//  Non-canonical signature: S value negative
				return false;
			}
			if(nLenS > 1 && (vchSig[S] == 0x00) && !((vchSig[S + 1] & 0x80) != 0))
			{
				//  Non-canonical signature: S value excessively padded
				return false;
			}
			return true;
		}

		bool CheckMinimalPush(byte[] data, OpcodeType opcode)
		{
			if(data.Length == 0)
			{
				// Could have used OP_0.
				return opcode == OpcodeType.OP_0;
			}
			else if(data.Length == 1 && data[0] >= 1 && data[0] <= 16)
			{
				// Could have used OP_1 .. OP_16.
				return (int)opcode == ((int)OpcodeType.OP_1) + (data[0] - 1);
			}
			else if(data.Length == 1 && data[0] == 0x81)
			{
				// Could have used OP_1NEGATE.
				return opcode == OpcodeType.OP_1NEGATE;
			}
			else if(data.Length <= 75)
			{
				// Could have used a direct push (opcode indicating number of bytes pushed + those bytes).
				return (int)opcode == data.Length;
			}
			else if(data.Length <= 255)
			{
				// Could have used OP_PUSHDATA.
				return opcode == OpcodeType.OP_PUSHDATA1;
			}
			else if(data.Length <= 65535)
			{
				// Could have used OP_PUSHDATA2.
				return opcode == OpcodeType.OP_PUSHDATA2;
			}
			return true;
		}

		private BigInteger CastToBigNum(bool v)
		{
			return new BigInteger(v ? 1 : 0);
		}

		private static bool CastToBool(byte[] vch)
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


		public bool CheckSig(TransactionSignature signature, PubKey pubKey, Script scriptPubKey, IndexedTxIn txIn)
		{
			return CheckSig(signature, pubKey, scriptPubKey, txIn.Transaction, txIn.N);
		}
		public bool CheckSig(TransactionSignature signature, PubKey pubKey, Script scriptPubKey, Transaction txTo, uint nIn)
		{
			return CheckSig(signature.ToBytes(), pubKey.ToBytes(), scriptPubKey, txTo, (int)nIn);
		}

		public bool CheckSig(byte[] vchSig, byte[] vchPubKey, Script scriptCode, Transaction txTo, int nIn)
		{
			PubKey pubkey = null;
			try
			{
				pubkey = new PubKey(vchPubKey);
			}
			catch(Exception)
			{
				return false;
			}


			// Hash type is one byte tacked on to the end of the signature
			if(vchSig.Length == 0)
				return false;

			TransactionSignature scriptSig = null;
			try
			{
				scriptSig = new TransactionSignature(vchSig);
			}
			catch(Exception)
			{
				if((ScriptVerify.DerSig & ScriptVerify) != 0)
					throw;
				return false;
			}

			if(!IsAllowedSignature(scriptSig.SigHash))
				return false;

			uint256 sighash = scriptCode.SignatureHash(txTo, nIn, scriptSig.SigHash);

			if(!pubkey.Verify(sighash, scriptSig.Signature))
			{
				if((ScriptVerify & ScriptVerify.StrictEnc) != 0)
					return false;

				//Replicate OpenSSL bug on 23b397edccd3740a74adb603c9756370fafcde9bcc4483eb271ecad09a94dd63 (http://r6.ca/blog/20111119T211504Z.html)
				var nLenR = vchSig[3];
				var nLenS = vchSig[5 + nLenR];
				var R = 4;
				var S = 6 + nLenR;
				var newS = new NBitcoin.BouncyCastle.Math.BigInteger(1, vchSig, S, nLenS);
				var newR = new NBitcoin.BouncyCastle.Math.BigInteger(1, vchSig, R, nLenR);
				var sig2 = new ECDSASignature(newR, newS);
				if(sig2.R != scriptSig.Signature.R || sig2.S != scriptSig.Signature.S)
				{
					if(!pubkey.Verify(sighash, sig2))
						return false;
				}
			}

			return true;
		}


		public bool IsAllowedSignature(SigHash sigHash)
		{
			if(SigHash == NBitcoin.SigHash.Undefined)
				return true;
			return SigHash == sigHash;
		}


		private void Load(ScriptEvaluationContext other)
		{
			_stack = new ContextStack<byte[]>(other._stack);
			ScriptVerify = other.ScriptVerify;
			SigHash = other.SigHash;
		}

		public ScriptEvaluationContext Clone()
		{
			return new ScriptEvaluationContext()
			{
				_stack = new ContextStack<byte[]>(_stack),
				ScriptVerify = ScriptVerify,
				SigHash = SigHash
			};
		}

		public bool? Result
		{
			get
			{
				if(Stack.Count == 0)
					return null;
				return CastToBool(_stack.Top(-1));
			}
		}

		public Exception ThrownException
		{
			get;
			set;
		}
	}


	/// <summary>
	/// ContextStack is used internally by the bitcoin script evaluator. This class contains
	/// operations not tipically available in a "pure" Stack class, as example:
	/// Insert, Swap, Erase and Top (Peek w/index)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(ContextStackDebugView<>))]
	public class ContextStack<T> : IEnumerable<T>
	{
		private T[] _array;
		private int _position;

		/// <summary>
		/// Initializes a new instance of the <see cref="ContextStack{T}"/> class.
		/// </summary>
		public ContextStack()
		{
			_position = -1;
			_array = new T[16];
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ContextStack{T}"/> 
		/// base on another stack. This is for copy/clone. 
		/// </summary>
		/// <param name="stack">The stack.</param>
		public ContextStack(ContextStack<T> stack)
		{
			_position = stack._position;
			_array = new T[stack._array.Length];
			stack._array.CopyTo(_array, 0);
		}

		/// <summary>
		/// Gets the number of items in the stack.
		/// </summary>
		public int Count
		{
			get { return _position + 1; }
		}

		/// <summary>
		/// Pushes the specified item on the stack.
		/// </summary>
		/// <param name="item">The item to by pushed.</param>
		public void Push(T item)
		{
			EnsureSize();
			_array[++_position] = item;
		}

		/// <summary>
		/// Pops this element in top of the stack.
		/// </summary>
		/// <returns>The element in top of the stack</returns>
		public T Pop()
		{
			return _array[_position--];
		}

		/// <summary>
		/// Pops as many items as specified.
		/// </summary>
		/// <param name="n">The number of items to be poped</param>
		/// <exception cref="System.ArgumentOutOfRangeException">Cannot remove more elements</exception>
		public void Clear(int n)
		{
			if(n > Count)
				throw new ArgumentOutOfRangeException("n", "Cannot remove more elements");
			_position -= n;
		}

		/// <summary>
		/// Returns the i-th element from the top of the stack.
		/// </summary>
		/// <param name="i">The i-th index.</param>
		/// <returns>the i-th element from the top of the stack</returns>
		/// <exception cref="System.IndexOutOfRangeException">topIndex</exception>
		public T Top(int i)
		{
			if (i > 0 || -i > Count) 
				throw new IndexOutOfRangeException("topIndex");
			return _array[Count + i];
		}

		/// <summary>
		/// Swaps the specified i and j elements in the stack.
		/// </summary>
		/// <param name="i">The i-th index.</param>
		/// <param name="j">The j-th index.</param>
		/// <exception cref="System.IndexOutOfRangeException">
		/// i or  j
		/// </exception>
		public void Swap(int i, int j)
		{
			if (i > 0 || -i > Count) 
				throw new IndexOutOfRangeException("i");
			if (i > 0 || -j > Count) 
				throw new IndexOutOfRangeException("j");

			var t = _array[Count + i];
			_array[Count + i] = _array[Count + j];
			_array[Count + j] = t;
		}

		/// <summary>
		/// Inserts an item in the specified position.
		/// </summary>
		/// <param name="position">The position.</param>
		/// <param name="value">The value.</param>
		public void Insert(int position, T value)
		{
			EnsureSize();
			var newArray = new T[_array.Length];
			Array.Copy(_array, position, newArray, position + 1, _position - position + 1);
			_array = newArray;
			_array[position] = value;
			_position++;
		}

		/// <summary>
		/// Removes the i-th item.
		/// </summary>
		/// <param name="from">The item position</param>
		public void Remove(int from)
		{
			Remove(from, from + 1);
		}

		/// <summary>
		/// Removes items from the i-th position to the j-th position.
		/// </summary>
		/// <param name="from">The item position</param>
		/// <param name="to">The item position</param>
		public void Remove(int from, int to)
		{
			var dest = new T[_array.Length];
			var f = Count + from;
			var t = Count + to;
			var diff = t-f;
			Array.Copy(_array, 0, dest, 0, f);
			Array.Copy(_array, t, dest, f, _position - diff +1);
			_array = dest;
			_position -= diff;
		}

		private void EnsureSize()
		{
			if(_position < _array.Length-1) return;
			Array.Resize(ref _array, 2 * _array.Length );
		}

		/// <summary>
		/// Returns a copy of the internal array.
		/// </summary>
		/// <returns>A copy of the internal array</returns>
		public T[] AsInternalArray()
		{
			var array = new T[Count];
			Array.Copy(_array, 0, array, 0, Count);
			return array;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}

		#region Reverse order enumerator (for Stacks)

		/// <summary>
		/// Implements a reverse enumerator for the ContextStack
		/// </summary>
		public struct Enumerator : IEnumerator<T>
		{
			private ContextStack<T> _stack; 
			private int _index;

			public Enumerator(ContextStack<T> stack )
			{
				_stack = stack;
				_index = stack._position + 1;
			}

			public T Current
			{
				get
				{
					if (_index == -1)
					{
						throw new InvalidOperationException("Enumeration has ended");
					}
					return _stack._array[_index];
				}
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

			public bool MoveNext()
			{
				return --_index >= 0;
			}

			public void Reset()
			{
				_index = _stack._position +1;
			}

			public void Dispose()
			{
			}
		}
		#endregion
	}

	/// <summary>
	/// DebuggerTypeProxy's View for debugging. It displays the information in 
	/// ContextStack in the same way that the System.Collections.Generic.Stack class does. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal sealed class ContextStackDebugView<T>
	{
		private ContextStack<T> _stack;
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return _stack.ToArray();
			}
		}
		public ContextStackDebugView(ContextStack<T> stack)
		{
			if (stack == null)
			{
				throw new ArgumentNullException("stack");
			}
			_stack = stack;
		}
	}
}
