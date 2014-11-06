using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class ScriptEvaluationContext
	{
		Stack<byte[]> _Stack = new Stack<byte[]>();
		public Stack<byte[]> Stack
		{
			get
			{
				return _Stack;
			}
		}

		public ScriptEvaluationContext()
		{
			ScriptVerify = NBitcoin.ScriptVerify.P2SH | NBitcoin.ScriptVerify.StrictEnc;
			SigHash = NBitcoin.SigHash.Undefined;
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
				return false;

			// Additional validation for spend-to-script-hash transactions:
			if(((ScriptVerify & ScriptVerify.P2SH) != 0) && scriptPubKey.IsPayToScriptHash)
			{
				this.Load(evaluationCopy);
				evaluationCopy = this;
				if(!scriptSig.IsPushOnly)
					return false;

				// stackCopy cannot be empty here, because if it was the
				// P2SH  HASH <> EQUAL  scriptPubKey would be evaluated with
				// an empty stack and the EvalScript above would return false.
				if(evaluationCopy.Stack.Count == 0)
					throw new InvalidProgramException("stackCopy cannot be empty here");

				Script redeem = new Script(evaluationCopy.Stack.Pop());

				if(!evaluationCopy.EvalScript(redeem, txTo, nIn))
					return false;

				return evaluationCopy.Result != null && evaluationCopy.Result.Value;
			}
			return true;
		}

		
		static readonly byte[] vchFalse = new byte[] { 0 };
		static readonly byte[] vchZero = new byte[] { 0 };
		static readonly byte[] vchTrue = new byte[] { 1 };

		public bool EvalScript(Script s, Transaction txTo, int nIn)
		{
			var script = s.CreateReader();
			int pend = (int)script.Inner.Length;

			int pbegincodehash = 0;
			Stack<bool> vfExec = new Stack<bool>();
			Stack<byte[]> altstack = new Stack<byte[]>();
			Op opcode = null;
			if(s.Length > 10000)
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
						_Stack.Push(opcode.PushData);
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
									_Stack.Push(Utils.BigIntegerToBytes(bn));
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
										if(_Stack.Count < 1)
											return false;
										var vch = top(_Stack, -1);
										fValue = CastToBool(vch);
										if(opcode.Code == OpcodeType.OP_NOTIF)
											fValue = !fValue;
										_Stack.Pop();
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
									if(_Stack.Count < 1)
										return false;
									bool fValue = CastToBool(top(_Stack, -1));
									if(fValue)
										_Stack.Pop();
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
									if(_Stack.Count < 1)
										return false;
									altstack.Push(top(_Stack, -1));
									_Stack.Pop();
								}
								break;

							case OpcodeType.OP_FROMALTSTACK:
								{
									if(altstack.Count < 1)
										return false;
									_Stack.Push(top(altstack, -1));
									altstack.Pop();
								}
								break;

							case OpcodeType.OP_2DROP:
								{
									// (x1 x2 -- )
									if(_Stack.Count < 2)
										return false;
									_Stack.Pop();
									_Stack.Pop();
								}
								break;

							case OpcodeType.OP_2DUP:
								{
									// (x1 x2 -- x1 x2 x1 x2)
									if(_Stack.Count < 2)
										return false;
									var vch1 = top(_Stack, -2);
									var vch2 = top(_Stack, -1);
									_Stack.Push(vch1);
									_Stack.Push(vch2);
								}
								break;

							case OpcodeType.OP_3DUP:
								{
									// (x1 x2 x3 -- x1 x2 x3 x1 x2 x3)
									if(_Stack.Count < 3)
										return false;
									var vch1 = top(_Stack, -3);
									var vch2 = top(_Stack, -2);
									var vch3 = top(_Stack, -1);
									_Stack.Push(vch1);
									_Stack.Push(vch2);
									_Stack.Push(vch3);
								}
								break;

							case OpcodeType.OP_2OVER:
								{
									// (x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2)
									if(_Stack.Count < 4)
										return false;
									var vch1 = top(_Stack, -4);
									var vch2 = top(_Stack, -3);
									_Stack.Push(vch1);
									_Stack.Push(vch2);
								}
								break;

							case OpcodeType.OP_2ROT:
								{
									// (x1 x2 x3 x4 x5 x6 -- x3 x4 x5 x6 x1 x2)
									if(_Stack.Count < 6)
										return false;
									var vch1 = top(_Stack, -6);
									var vch2 = top(_Stack, -5);
									erase(ref _Stack, _Stack.Count - 6, _Stack.Count - 4);
									_Stack.Push(vch1);
									_Stack.Push(vch2);
								}
								break;

							case OpcodeType.OP_2SWAP:
								{
									// (x1 x2 x3 x4 -- x3 x4 x1 x2)
									if(_Stack.Count < 4)
										return false;
									swap(ref _Stack, -4, -2);
									swap(ref _Stack, -3, -1);
								}
								break;

							case OpcodeType.OP_IFDUP:
								{
									// (x - 0 | x x)
									if(_Stack.Count < 1)
										return false;
									var vch = top(_Stack, -1);
									if(CastToBool(vch))
										_Stack.Push(vch);
								}
								break;

							case OpcodeType.OP_DEPTH:
								{
									// -- stacksize
									BigInteger bn = new BigInteger(_Stack.Count);
									_Stack.Push(Utils.BigIntegerToBytes(bn));
								}
								break;

							case OpcodeType.OP_DROP:
								{
									// (x -- )
									if(_Stack.Count < 1)
										return false;
									_Stack.Pop();
								}
								break;

							case OpcodeType.OP_DUP:
								{
									// (x -- x x)
									if(_Stack.Count < 1)
										return false;
									var vch = top(_Stack, -1);
									_Stack.Push(vch);
								}
								break;

							case OpcodeType.OP_NIP:
								{
									// (x1 x2 -- x2)
									if(_Stack.Count < 2)
										return false;
									erase(ref _Stack, _Stack.Count - 2);
								}
								break;

							case OpcodeType.OP_OVER:
								{
									// (x1 x2 -- x1 x2 x1)
									if(_Stack.Count < 2)
										return false;
									var vch = top(_Stack, -2);
									_Stack.Push(vch);
								}
								break;

							case OpcodeType.OP_PICK:
							case OpcodeType.OP_ROLL:
								{
									// (xn ... x2 x1 x0 n - xn ... x2 x1 x0 xn)
									// (xn ... x2 x1 x0 n - ... x2 x1 x0 xn)
									if(_Stack.Count < 2)
										return false;
									int n = (int)CastToBigNum(top(_Stack, -1));
									_Stack.Pop();
									if(n < 0 || n >= _Stack.Count)
										return false;
									var vch = top(_Stack, -n - 1);
									if(opcode.Code == OpcodeType.OP_ROLL)
										erase(ref _Stack, _Stack.Count - n - 1);
									_Stack.Push(vch);
								}
								break;

							case OpcodeType.OP_ROT:
								{
									// (x1 x2 x3 -- x2 x3 x1)
									//  x2 x1 x3  after first swap
									//  x2 x3 x1  after second swap
									if(_Stack.Count < 3)
										return false;
									swap(ref _Stack, -3, -2);
									swap(ref _Stack, -2, -1);
								}
								break;

							case OpcodeType.OP_SWAP:
								{
									// (x1 x2 -- x2 x1)
									if(_Stack.Count < 2)
										return false;
									swap(ref _Stack, -2, -1);
								}
								break;

							case OpcodeType.OP_TUCK:
								{
									// (x1 x2 -- x2 x1 x2)
									if(_Stack.Count < 2)
										return false;
									var vch = top(_Stack, -1);
									insert(ref _Stack, _Stack.Count - 2, vch);
								}
								break;


							case OpcodeType.OP_SIZE:
								{
									// (in -- in size)
									if(_Stack.Count < 1)
										return false;
									BigInteger bn = new BigInteger(top(_Stack, -1).Length);
									_Stack.Push(Utils.BigIntegerToBytes(bn));
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
									if(_Stack.Count < 2)
										return false;
									var vch1 = top(_Stack, -2);
									var vch2 = top(_Stack, -1);
									bool fEqual = Utils.ArrayEqual(vch1, vch2);
									// OpcodeType.OP_NOTEQUAL is disabled because it would be too easy to say
									// something like n != 1 and have some wiseguy pass in 1 with extra
									// zero bytes after it (numerically, 0x01 == 0x0001 == 0x000001)
									//if (opcode == OpcodeType.OP_NOTEQUAL)
									//    fEqual = !fEqual;
									_Stack.Pop();
									_Stack.Pop();
									_Stack.Push(fEqual ? vchTrue : vchFalse);
									if(opcode.Code == OpcodeType.OP_EQUALVERIFY)
									{
										if(fEqual)
											_Stack.Pop();
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
									if(_Stack.Count < 1)
										return false;
									var bn = CastToBigNum(top(_Stack, -1));
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
									_Stack.Pop();
									_Stack.Push(Utils.BigIntegerToBytes(bn));
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
									if(_Stack.Count < 2)
										return false;
									var bn1 = CastToBigNum(top(_Stack, -2));
									var bn2 = CastToBigNum(top(_Stack, -1));
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
									_Stack.Pop();
									_Stack.Pop();
									_Stack.Push(Utils.BigIntegerToBytes(bn));

									if(opcode.Code == OpcodeType.OP_NUMEQUALVERIFY)
									{
										if(CastToBool(top(_Stack, -1)))
											_Stack.Pop();
										else
											return false;
									}
								}
								break;

							case OpcodeType.OP_WITHIN:
								{
									// (x min max -- out)
									if(_Stack.Count < 3)
										return false;
									var bn1 = CastToBigNum(top(_Stack, -3));
									var bn2 = CastToBigNum(top(_Stack, -2));
									var bn3 = CastToBigNum(top(_Stack, -1));
									bool fValue = (bn2 <= bn1 && bn1 < bn3);
									_Stack.Pop();
									_Stack.Pop();
									_Stack.Pop();
									_Stack.Push(fValue ? vchTrue : vchFalse);
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
									if(_Stack.Count < 1)
										return false;
									var vch = top(_Stack, -1);
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
									_Stack.Pop();
									_Stack.Push(vchHash);
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
									if(_Stack.Count < 2)
										return false;

									var vchSig = top(_Stack, -2);
									var vchPubKey = top(_Stack, -1);

									////// debug print
									//PrintHex(vchSig.begin(), vchSig.end(), "sig: %s\n");
									//PrintHex(vchPubKey.begin(), vchPubKey.end(), "pubkey: %s\n");

									// Subset of script starting at the most recent codeseparator
									var scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
									// Drop the signature, since there's no way for a signature to sign itself
									scriptCode.FindAndDelete(vchSig);


									bool fSuccess = IsCanonicalSignature(vchSig) && IsCanonicalPubKey(vchPubKey) &&
										CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn);

									_Stack.Pop();
									_Stack.Pop();
									_Stack.Push(fSuccess ? vchTrue : vchFalse);
									if(opcode.Code == OpcodeType.OP_CHECKSIGVERIFY)
									{
										if(fSuccess)
											_Stack.Pop();
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
									if((int)_Stack.Count < i)
										return false;

									int nKeysCount = (int)CastToBigNum(top(_Stack, -i));
									if(nKeysCount < 0 || nKeysCount > 20)
										return false;
									nOpCount += nKeysCount;
									if(nOpCount > 201)
										return false;
									int ikey = ++i;
									i += nKeysCount;
									if((int)_Stack.Count < i)
										return false;

									int nSigsCount = (int)CastToBigNum(top(_Stack, -i));
									if(nSigsCount < 0 || nSigsCount > nKeysCount)
										return false;
									int isig = ++i;
									i += nSigsCount;
									if((int)_Stack.Count < i)
										return false;

									// Subset of script starting at the most recent codeseparator
									Script scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
									// Drop the signatures, since there's no way for a signature to sign itself
									for(int k = 0 ; k < nSigsCount ; k++)
									{
										var vchSig = top(_Stack, -isig - k);
										scriptCode.FindAndDelete(vchSig);
									}

									bool fSuccess = true;
									while(fSuccess && nSigsCount > 0)
									{
										var vchSig = top(_Stack, -isig);
										var vchPubKey = top(_Stack, -ikey);

										// Check signature
										bool fOk = IsCanonicalSignature(vchSig) && IsCanonicalPubKey(vchPubKey) &&
											CheckSig(vchSig, vchPubKey, scriptCode, txTo, nIn);

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

									while(i-- > 1)
										_Stack.Pop();

									// A bug causes CHECKMULTISIG to consume one extra argument
									// whose contents were not checked in any way.
									//
									// Unfortunately this is a potential source of mutability,
									// so optionally verify it is exactly equal to zero prior
									// to removing it from the stack.
									if(_Stack.Count < 1)
										return false;
									if(((ScriptVerify & ScriptVerify.NullDummy) != 0) && top(_Stack, -1).Length != 0)
										return Utils.error("CHECKMULTISIG dummy argument not null");
									_Stack.Pop();

									_Stack.Push(fSuccess ? vchTrue : vchFalse);

									if(opcode.Code == OpcodeType.OP_CHECKMULTISIGVERIFY)
									{
										if(fSuccess)
											_Stack.Pop();
										else
											return false;
									}
								}
								break;

							default:
								return false;
						}

					// Size limits
					if(_Stack.Count + altstack.Count > 1000)
						return false;

				}
			}
			catch(Exception ex)
			{
				Utils.error("Error in EvalScript " + ex.Message + " on opcode " + opcode);
				return false;
			}


			if(vfExec.Count != 0)
				return false;

			return true;
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

		public bool IsCanonicalPubKey(byte[] vchPubKey)
		{
			if(!((ScriptVerify & ScriptVerify.StrictEnc) != 0))
				return true;

			if(vchPubKey.Length < 33)
				return false; //error("Non-canonical public key: too short");
			if(vchPubKey[0] == 0x04)
			{
				if(vchPubKey.Length != 65)
					return false; //error("Non-canonical public key: invalid length for uncompressed key");
			}
			else if(vchPubKey[0] == 0x02 || vchPubKey[0] == 0x03)
			{
				if(vchPubKey.Length != 33)
					return false; //error("Non-canonical public key: invalid length for compressed key");
			}
			else
			{
				return false; //error("Non-canonical public key: compressed nor uncompressed");
			}
			return true;
		}

		public bool IsCanonicalSignature(byte[] vchSig)
		{
			if(!((ScriptVerify & ScriptVerify.StrictEnc) != 0))
				return true;

			// See https://bitcointalk.org/index.php?topic=8392.msg127623#msg127623
			// A canonical signature exists of: <30> <total len> <02> <len R> <R> <02> <len S> <S> <hashtype>
			// Where R and S are not negative (their first byte has its highest bit not set), and not
			// excessively padded (do not start with a 0 byte, unless an otherwise negative number follows,
			// in which case a single 0 byte is necessary and even required).
			if(vchSig.Length < 9)
				return Utils.error("Non-canonical signature: too short");
			if(vchSig.Length > 73)
				return Utils.error("Non-canonical signature: too long");
			var nHashType = vchSig[vchSig.Length - 1] & (~((byte)SigHash.AnyoneCanPay));
			if(nHashType < (byte)SigHash.All || nHashType > (byte)SigHash.Single)
				return Utils.error("Non-canonical signature: unknown hashtype byte");
			if(vchSig[0] != 0x30)
				return Utils.error("Non-canonical signature: wrong type");
			if(vchSig[1] != vchSig.Length - 3)
				return Utils.error("Non-canonical signature: wrong length marker");
			var nLenR = vchSig[3];
			if(5 + nLenR >= vchSig.Length)
				return Utils.error("Non-canonical signature: S length misplaced");
			var nLenS = vchSig[5 + nLenR];
			if(((int)nLenR + nLenS + 7) != vchSig.Length)
				return Utils.error("Non-canonical signature: R+S length mismatch");

			var R = 4;
			if(vchSig[R - 2] != 0x02)
				return Utils.error("Non-canonical signature: R value type mismatch");
			if(nLenR == 0)
				return Utils.error("Non-canonical signature: R length is zero");
			if((vchSig[R + 0] & (byte)0x80) != 0)
				return Utils.error("Non-canonical signature: R value negative");
			if(nLenR > 1 && (vchSig[R + 0] == 0x00) && !((vchSig[R + 1] & 0x80) != 0))
				return Utils.error("Non-canonical signature: R value excessively padded");

			var S = 6 + nLenR;
			if(vchSig[S - 2] != 0x02)
				return Utils.error("Non-canonical signature: S value type mismatch");
			if(nLenS == 0)
				return Utils.error("Non-canonical signature: S length is zero");
			if((vchSig[S + 0] & 0x80) != 0)
				return Utils.error("Non-canonical signature: S value negative");
			if(nLenS > 1 && (vchSig[S + 0] == 0x00) && !((vchSig[S + 1] & 0x80) != 0))
				return Utils.error("Non-canonical signature: S value excessively padded");

			if((ScriptVerify & ScriptVerify.LowS) != 0)
			{
				if((vchSig[S + nLenS - 1] & 1) != 0)
					return Utils.error("Non-canonical signature: S value is unnecessarily high");
			}

			return true;
		}

		private bool CheckSig(byte[] vchSig, byte[] vchPubKey, Script scriptCode, Transaction txTo, int nIn)
		{
			//static CSignatureCache signatureCache;
			if(!PubKey.IsValidSize(vchPubKey.Length))
				return false;
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

			var scriptSig = new TransactionSignature(vchSig);

			if(!IsAllowedSignature(scriptSig.SigHash))
				return false;

			uint256 sighash = scriptCode.SignatureHash(txTo, nIn, scriptSig.SigHash);

			//if (signatureCache.Get(sighash, vchSig, pubkey))
			//	return true;

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

			//if (!(flags & SCRIPT_VERIFY_NOCACHE))
			//	signatureCache.Set(sighash, vchSig, pubkey);

			return true;
		}


		public bool IsAllowedSignature(SigHash sigHash)
		{
			if(SigHash == NBitcoin.SigHash.Undefined)
				return true;
			else
				return SigHash == sigHash;
		}


		private void Load(ScriptEvaluationContext other)
		{
			_Stack = Clone(other._Stack);
			ScriptVerify = other.ScriptVerify;
			SigHash = other.SigHash;
		}

		public ScriptEvaluationContext Clone()
		{
			return new ScriptEvaluationContext()
			{
				_Stack = Clone(_Stack),
				ScriptVerify = ScriptVerify,
				SigHash = SigHash
			};
		}

		private Stack<byte[]> Clone(Stack<byte[]> stack)
		{
			var elements = stack.ToArray();
			Array.Reverse(elements);
			return new Stack<byte[]>(elements.Select(s => s.ToArray()));
		}

		public bool? Result
		{
			get
			{
				if(Stack.Count == 0)
					return null;
				return CastToBool(Stack.Peek());
			}
		}
	}
}
