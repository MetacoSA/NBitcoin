using NBitcoin.Crypto;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static NBitcoin.TaprootConstants;

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

		/* OP_CHECKLOCKTIMEVERIFY */
		NegativeLockTime,
		UnsatisfiedLockTime,

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
		WitnessMalleated,
		WitnessMalleatedP2SH,
		WitnessProgramEmpty,
		WitnessProgramMissmatch,
		DiscourageUpgradableWitnessProgram,
		WitnessProgramWrongLength,
		WitnessUnexpected,
		NullFail,
		MinimalIf,
		WitnessPubkeyType,
		SchnorrSigSize,
		SchnorrSigHashType,
		SchnorrSig,
		TaprootWrongControlSize,
		DiscourageUpgradableTaprootVersion,
		TapscriptValidationWeight,
		DiscourageUpgradablePubKeyType,
		DiscourageOpSuccess
	}
#nullable enable
	public class TransactionChecker
	{
		public TransactionChecker(Transaction tx, int index, TxOut? spentOutput, PrecomputedTransactionData? precomputedTransactionData)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			_Transaction = tx;
			_Index = index;
			_SpentOutput = spentOutput;
			_PrecomputedTransactionData = precomputedTransactionData;
		}
		public TransactionChecker(Transaction tx, int index, TxOut? spentOutput = null)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			_Transaction = tx;
			_Index = index;
			_SpentOutput = spentOutput;
		}


		private PrecomputedTransactionData? _PrecomputedTransactionData;
		public PrecomputedTransactionData PrecomputedTransactionData
		{
			get
			{
				return _PrecomputedTransactionData ??= new PrecomputedTransactionData(Transaction);
			}
		}

		private readonly Transaction _Transaction;
		public Transaction Transaction
		{
			get
			{
				return _Transaction;
			}
		}

		public TxIn Input
		{
			get
			{
				return Transaction.Inputs[_Index];
			}
		}

		private readonly int _Index;
		public int Index
		{
			get
			{
				return _Index;
			}
		}

		private readonly TxOut? _SpentOutput;
		public TxOut? SpentOutput
		{
			get
			{
				return _SpentOutput;
			}
		}
#if HAS_SPAN
		public bool CheckSchnorrSignature(byte[] sig, byte[] pubkey_in, HashVersion taproot, ExecutionData executionData, out ScriptError err)
		{
			// Schnorr signatures have 32-byte public keys. The caller is responsible for enforcing this.

			//assert(sigversion == SigVersion::TAPROOT || sigversion == SigVersion::TAPSCRIPT);
			//assert(pubkey_in.size() == 32);

			// Note that in Tapscript evaluation, empty signatures are treated specially (invalid signature that does not
			// abort script execution). This is implemented in EvalChecksigTapscript, which won't invoke
			// CheckSchnorrSignature in that case. In other contexts, they are invalid like every other signature with
			// size different from 64 or 65.
			if (sig.Length != 64 && sig.Length != 65)
			{
				err = ScriptError.SchnorrSigSize;
				return false;
			}

			if (!TaprootPubKey.TryCreate(pubkey_in, out var pubkey))
			{
				// Technically, the pubkey is wrong, but bitcoin core code doesn't check this.
				err = ScriptError.SchnorrSig;
				return false;
			}
			if (!TaprootSignature.TryParse(sig, out var taprootSig))
			{
				err = ScriptError.SchnorrSigHashType;
				return false;
			}

			if (((byte)taprootSig.SigHash & Transaction.SIGHASH_OUTPUT_MASK) == (byte)TaprootSigHash.Single && this.Index >= this.Transaction.Outputs.Count)
			{
				err = ScriptError.SchnorrSigHashType;
				return false;
			}

			var hash = this.Transaction.GetSignatureHashTaproot(PrecomputedTransactionData, new TaprootExecutionData(this.Index, executionData.TapleafHash)
			{
				AnnexHash = executionData.AnnexHash,
				CodeseparatorPosition = executionData.CodeseparatorPosition,
				SigHash = taprootSig.SigHash
			});
			if (!pubkey.VerifySignature(hash, taprootSig.SchnorrSignature))
			{
				err = ScriptError.SchnorrSig;
				return false;
			}
			err = ScriptError.OK;
			return true;
		}
#endif
	}
#nullable restore
	public class SignedHash
	{
		public ITransactionSignature Signature
		{
			get;
			internal set;
		}

		public Script ScriptCode
		{
			get;
			internal set;
		}

		public HashVersion HashVersion
		{
			get;
			internal set;
		}

		public uint256 Hash
		{
			get;
			internal set;
		}
	}

	public class ExecutionData
	{
		public uint256 AnnexHash { get; set; }
		public uint256 TapleafHash { get; internal set; }
		public uint CodeseparatorPosition { get; set; } = 0xffffffff;
		public long ValidationWeightLeft { get; set; }
	}
	public class ScriptEvaluationContext
	{
		internal class CScriptNum
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
				: this(vch, fRequireMinimal, 4)
			{

			}
			public CScriptNum(byte[] vch, bool fRequireMinimal, long nMaxNumSize)
			{
				if (vch.Length > nMaxNumSize)
				{
					throw new ArgumentException("script number overflow", "vch");
				}
				if (fRequireMinimal && vch.Length > 0)
				{
					// Check that the number is encoded with the minimum possible
					// number of bytes.
					//
					// If the most-significant-byte - excluding the sign bit - is zero
					// then we're not minimal. Note how this test also rejects the
					// negative-zero encoding, 0x80.
					if ((vch[vch.Length - 1] & 0x7f) == 0)
					{
						// One exception: if there's more than one byte and the most
						// significant bit of the second-most-significant-byte is set
						// it would conflict with the sign bit. An example of this case
						// is +-255, which encode to 0xff00 and 0xff80 respectively.
						// (big-endian).
						if (vch.Length <= 1 || (vch[vch.Length - 2] & 0x80) == 0)
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
				if (obj == null || !(obj is CScriptNum))
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

			public static CScriptNum operator &(CScriptNum a, long b)
			{
				return new CScriptNum(a.m_value & b);
			}
			public static CScriptNum operator &(CScriptNum a, CScriptNum b)
			{
				return new CScriptNum(a.m_value & b.m_value);
			}



			public static CScriptNum operator -(CScriptNum num)
			{
				assert(num.m_value != Int64.MinValue);
				return new CScriptNum(-num.m_value);
			}

			private static void assert(bool result)
			{
				if (!result)
					throw new InvalidOperationException("Assertion fail for CScriptNum");
			}

			public static implicit operator CScriptNum(long rhs)
			{
				return new CScriptNum(rhs);
			}

			public static explicit operator long(CScriptNum rhs)
			{
				return rhs.m_value;
			}

			public static explicit operator uint(CScriptNum rhs)
			{
				return (uint)rhs.m_value;
			}



			public int getint()
			{
				if (m_value > int.MaxValue)
					return int.MaxValue;
				else if (m_value < int.MinValue)
					return int.MinValue;
				return (int)m_value;
			}

			public byte[] getvch()
			{
				return serialize(m_value);
			}

			internal static byte[] serialize(long value)
			{
				if (value == 0)
					return new byte[0];

				var result = new List<byte>(8);
				bool neg = value < 0;
				long absvalue = neg ? -value : value;

				while (absvalue != 0)
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

				if ((result[result.Count - 1] & 0x80) != 0)
					result.Add((byte)(neg ? 0x80 : 0));
				else if (neg)
					result[result.Count - 1] |= 0x80;

				return result.ToArray();
			}

			static long set_vch(byte[] vch)
			{
				if (vch.Length == 0)
					return 0;

				long result = 0;
				for (int i = 0; i != vch.Length; ++i)
					result |= ((long)(vch[i])) << 8 * i;

				// If the input vector's most significant byte is 0x80, remove it from
				// the result's msb and return a negative.
				if ((vch[vch.Length - 1] & 0x80) != 0)
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
			Error = ScriptError.UnknownError;
		}
		public ScriptVerify ScriptVerify
		{
			get;
			set;
		}
		public ExecutionData ExecutionData { get; set; } = new ExecutionData();

		public bool VerifyScript(Script scriptSig, Transaction txTo, int nIn, TxOut spentOutput)
		{
			return VerifyScript(scriptSig, spentOutput.ScriptPubKey, new TransactionChecker(txTo, nIn, spentOutput));
		}
		public bool VerifyScript(Script scriptSig, Script scriptPubKey, TransactionChecker checker)
		{
			return VerifyScript(scriptSig, checker.Input.WitScript, scriptPubKey, checker);
		}
		public bool VerifyScript(Script scriptSig, WitScript witness, Script scriptPubKey, TransactionChecker checker)
		{
			scriptSig = scriptSig ?? Script.Empty;
			witness = witness ?? WitScript.Empty;
			ExecutionData = new ExecutionData();
			SetError(ScriptError.UnknownError);
			if ((ScriptVerify & ScriptVerify.SigPushOnly) != 0 && !scriptSig.IsPushOnly)
				return SetError(ScriptError.SigPushOnly);

			ScriptEvaluationContext evaluationCopy = null;

			if (!EvalScript(scriptSig, checker, 0))
				return false;
			if ((ScriptVerify & ScriptVerify.P2SH) != 0)
			{
				evaluationCopy = Clone();
			}
			if (!EvalScript(scriptPubKey, checker, 0))
				return false;

			if (!Result)
				return SetError(ScriptError.EvalFalse);

			bool hadWitness = false;
			// Bare witness programs

			if ((ScriptVerify & ScriptVerify.Witness) != 0)
			{
				var wit = PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(scriptPubKey);
				if (wit != null)
				{
					hadWitness = true;
					if (scriptSig.Length != 0)
					{
						// The scriptSig must be _exactly_ CScript(), otherwise we reintroduce malleability.
						return SetError(ScriptError.WitnessMalleated);
					}
					if (!VerifyWitnessProgram(witness, wit, checker, false))
					{
						return false;
					}
					// Bypass the cleanstack check at the end. The actual stack is obviously not clean
					// for witness programs.
					Stack.Clear();
					Stack.Push(new byte[0]);
				}
			}

			// Additional validation for spend-to-script-hash transactions:
			if (((ScriptVerify & ScriptVerify.P2SH) != 0) && scriptPubKey.IsScriptType(ScriptType.P2SH))
			{
				Load(evaluationCopy);
				evaluationCopy = this;
				if (!scriptSig.IsPushOnly)
					return SetError(ScriptError.SigPushOnly);

				// stackCopy cannot be empty here, because if it was the
				// P2SH  HASH <> EQUAL  scriptPubKey would be evaluated with
				// an empty stack and the EvalScript above would return false.
				if (evaluationCopy.Stack.Count == 0)
					throw new InvalidOperationException("stackCopy cannot be empty here");

				var redeem = new Script(evaluationCopy.Stack.Pop());

				if (!evaluationCopy.EvalScript(redeem, checker, 0))
					return false;

				if (!evaluationCopy.Result)
					return SetError(ScriptError.EvalFalse);

				// P2SH witness program
				if ((ScriptVerify & ScriptVerify.Witness) != 0)
				{
					var wit = PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(redeem);
					if (wit != null)
					{
						hadWitness = true;
						if (scriptSig != new Script(Op.GetPushOp(redeem.ToBytes())))
						{
							// The scriptSig must be _exactly_ a single push of the redeemScript. Otherwise we
							// reintroduce malleability.
							return SetError(ScriptError.WitnessMalleatedP2SH);
						}
						if (!VerifyWitnessProgram(witness, wit, checker, true))
						{
							return false;
						}
						// Bypass the cleanstack check at the end. The actual stack is obviously not clean
						// for witness programs.
						Stack.Clear();
						Stack.Push(new byte[0]);
					}
				}
			}

			// The CLEANSTACK check is only performed after potential P2SH evaluation,
			// as the non-P2SH evaluation of a P2SH script will obviously not result in
			// a clean stack (the P2SH inputs remain).
			if ((ScriptVerify & ScriptVerify.CleanStack) != 0)
			{
				// Disallow CLEANSTACK without P2SH, as otherwise a switch CLEANSTACK->P2SH+CLEANSTACK
				// would be possible, which is not a softfork (and P2SH should be one).
				if ((ScriptVerify & ScriptVerify.P2SH) == 0)
					throw new InvalidOperationException("ScriptVerify : CleanStack without P2SH is not allowed");
				if ((ScriptVerify & ScriptVerify.Witness) == 0)
					throw new InvalidOperationException("ScriptVerify : CleanStack without Witness is not allowed");
				if (Stack.Count != 1)
					return SetError(ScriptError.CleanStack);
			}

			if ((ScriptVerify & ScriptVerify.Witness) != 0)
			{
				// We can't check for correct unexpected witness data if P2SH was off, so require
				// that WITNESS implies P2SH. Otherwise, going from WITNESS->P2SH+WITNESS would be
				// possible, which is not a softfork.
				if ((ScriptVerify & ScriptVerify.P2SH) == 0)
					throw new InvalidOperationException("ScriptVerify : Witness without P2SH is not allowed");
				if (!hadWitness && witness.PushCount != 0)
				{
					return SetError(ScriptError.WitnessUnexpected);
				}
			}

			return true;
		}
		const byte ANNEX_TAG = 0x50;
		// How much weight budget is added to the witness size (Tapscript only, see BIP 342).
		private bool VerifyWitnessProgram(WitScript witness, WitProgramParameters wit, TransactionChecker checker, bool isP2SH)
		{
			ContextStack<byte[]> stack = new ContextStack<byte[]>(witness.Pushes);
			Script execScript; //!< Actually executed script (last stack item in P2WSH; implied P2PKH script in P2WPKH; leaf script in P2TR)
			if (wit.Version == 0)
			{
				if (wit.Program.Length == 32)
				{
					// Version 0 segregated witness program: SHA256(CScript) inside the program, CScript + inputs in witness
					if (stack.Count == 0)
					{
						return SetError(ScriptError.WitnessProgramEmpty);
					}
					execScript = Script.FromBytesUnsafe(stack.Pop());
					var hashScriptPubKey = Hashes.SHA256(execScript.ToBytes(true));
					if (!Utils.ArrayEqual(hashScriptPubKey, wit.Program))
					{
						return SetError(ScriptError.WitnessProgramMissmatch);
					}
					return ExecuteWitnessScript(stack, execScript, HashVersion.WitnessV0, checker);
				}
				else if (wit.Program.Length == 20)
				{
					// Special case for pay-to-pubkeyhash; signature + pubkey in witness
					if (stack.Count != 2)
					{
						return SetError(ScriptError.WitnessProgramMissmatch); // 2 items in witness
					}
					execScript = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(new KeyId(wit.Program));
					return ExecuteWitnessScript(stack, execScript, HashVersion.WitnessV0, checker);
				}
				else
				{
					return SetError(ScriptError.WitnessProgramWrongLength);
				}
			}
			else if (wit.Version == OpcodeType.OP_1 && wit.Program.Length == 32 && !isP2SH)
			{
#if HAS_SPAN
				// BIP341 Taproot: 32-byte non-P2SH witness v1 program (which encodes a P2C-tweaked pubkey)
				if (!this.ScriptVerify.HasFlag(ScriptVerify.Taproot))
					return SetSuccess(ScriptError.OK);
				if (stack.Count == 0) return SetError(ScriptError.WitnessProgramEmpty);
				if (stack.Count >= 2 && stack.First() is byte[] b && b.Length > 0 && b[0] == ANNEX_TAG)
				{
					// Drop annex (this is non-standard; see IsWitnessStandard)
					var annex = stack.Pop();
					HashStream h = new HashStream() { SingleSHA256 = true };
					BitcoinStream bs = new BitcoinStream(h, true);
					bs.ReadWriteAsVarString(ref annex);
					ExecutionData.AnnexHash = h.GetHash();
				}
				if (stack.Count == 1)
				{
					// Key path spending (stack size is 1 after removing optional annex)
					if (!checker.CheckSchnorrSignature(stack.First(), wit.Program, HashVersion.Taproot, ExecutionData, out var err))
					{
						return SetError(err);
					}
					return true;
				}
				else
				{
					// Script path spending (stack size is >1 after removing optional annex)
					var control = stack.Pop();
					var scriptBytes = stack.Pop();
					execScript = Script.FromBytesUnsafe(scriptBytes);
					if (control.Length < TAPROOT_CONTROL_BASE_SIZE || control.Length > TAPROOT_CONTROL_MAX_SIZE || ((control.Length - TAPROOT_CONTROL_BASE_SIZE) % TAPROOT_CONTROL_NODE_SIZE) != 0)
					{
						return SetError(ScriptError.TaprootWrongControlSize);
					}
					ExecutionData.TapleafHash = ComputeTapleafHash((byte)(control[0] & TAPROOT_LEAF_MASK), execScript);
					if (!VerifyTaprootCommitment(control, wit.Program, ExecutionData.TapleafHash))
					{
						return SetError(ScriptError.WitnessProgramMissmatch);
					}
					if ((control[0] & TAPROOT_LEAF_MASK) == TAPROOT_LEAF_TAPSCRIPT)
					{
						// Tapscript (leaf version 0xc0)
						ExecutionData.ValidationWeightLeft = witness.GetSerializedSize() + VALIDATION_WEIGHT_OFFSET;
						return ExecuteWitnessScript(stack, execScript, HashVersion.Tapscript, checker);
					}
					if (ScriptVerify.HasFlag(ScriptVerify.DiscourageUpgradableTaprootVersion))
					{
						return SetError(ScriptError.DiscourageUpgradableTaprootVersion);
					}
					return true;
				}

#else
				// We can't support validation of taproot in .net framework
				return SetError(ScriptError.DiscourageUpgradableWitnessProgram);
#endif

			}
			else if ((ScriptVerify & ScriptVerify.DiscourageUpgradableWitnessProgram) != 0)
			{
				return SetError(ScriptError.DiscourageUpgradableWitnessProgram);
			}
			else
			{
				// Higher version witness scripts return true for future softfork compatibility
				return true;
			}
		}

#if HAS_SPAN
		private bool VerifyTaprootCommitment(ReadOnlySpan<byte> control, ReadOnlySpan<byte> program, uint256 tapleafHash)
		{
			//! The internal pubkey (x-only, so no Y coordinate parity).
			if (!TaprootInternalPubKey.TryCreate(control.Slice(1, 32), out var p))
				return false;
			//! The output pubkey (taken from the scriptPubKey).
			if (!TaprootPubKey.TryCreate(program, out var q))
				return false;
			// Compute the Merkle root from the leaf and the provided path.
			uint256 merkle_root = ComputeTaprootMerkleRoot(control, tapleafHash);
			// Verify that the output pubkey matches the tweaked internal pubkey, after correcting for parity.
			return q.CheckTapTweak(p, merkle_root, (control[0] & 1) != 0);
		}

		internal static uint256 ComputeTaprootMerkleRoot(ReadOnlySpan<byte> control, uint256 tapleafHash)
		{
			int path_len = (control.Length - TAPROOT_CONTROL_BASE_SIZE) / TAPROOT_CONTROL_NODE_SIZE;
			uint256 k = tapleafHash;
			Span<byte> buff = stackalloc byte[32];
			for (int i = 0; i < path_len; i++)
			{
				using SHA256 sha = new SHA256();
				sha.InitializeTagged("TapBranch");
				var node = new uint256(control.Slice(TAPROOT_CONTROL_BASE_SIZE + TAPROOT_CONTROL_NODE_SIZE * i, TAPROOT_CONTROL_NODE_SIZE));
				if (CompareLexicographic(k, node))
				{
					k.ToBytes(buff);
					sha.Write(buff);
					node.ToBytes(buff);
					sha.Write(buff);
				}
				else
				{
					node.ToBytes(buff);
					sha.Write(buff);
					k.ToBytes(buff);
					sha.Write(buff);
				}
				sha.GetHash(buff);
				k = new uint256(buff);
			}
			return k;
		}

		static bool CompareLexicographic(uint256 a, uint256 b)
		{
			Span<byte> ab = stackalloc byte[32];
			Span<byte> bb = stackalloc byte[32];
			a.ToBytes(ab);
			b.ToBytes(bb);
			for (int i = 0; i < ab.Length && i < bb.Length; i++)
			{
				if (ab[i] < bb[i])
					return true;
				if (bb[i] < ab[i])
					return false;
			}
			return true;
		}

		private uint256 ComputeTapleafHash(byte leaf_version, Script execScript)
		{
			var hash = new HashStream() { SingleSHA256 = true };
			hash.InitializeTagged("TapLeaf");
			hash.WriteByte(leaf_version);
			var bs = new BitcoinStream(hash, true);
			bs.ReadWrite(execScript);
			return hash.GetHash();
		}
#endif
		// Maximum number of values on script interpreter stack
		const int MAX_STACK_SIZE = 1000;
		private bool ExecuteWitnessScript(ContextStack<byte[]> stack, Script scriptPubKey, HashVersion sigversion, TransactionChecker checker)
		{
			var ctx = this.Clone();
			ctx.Stack.Clear();
			foreach (var item in stack.Reverse())
				ctx.Stack.Push(item);

			if (sigversion == HashVersion.Tapscript)
			{
				// OP_SUCCESSx processing overrides everything, including stack element size limits
				foreach (var op in scriptPubKey.ToOps())
				{
					var opcode = op.Code;
					//if (op.PushData is null)
					//{
					//	// Note how this condition would not be reached if an unknown OP_SUCCESSx was found
					//	return SetError(ScriptError.BadOpCode);
					//}
					// New opcodes will be listed here. May use a different sigversion to modify existing opcodes.
					if (IsOpSuccess(opcode))
					{
						if (ScriptVerify.HasFlag(ScriptVerify.DiscourageOpSuccess))
						{
							return SetError(ScriptError.DiscourageOpSuccess);
						}
						return SetSuccess(ScriptError.OK);
					}
				}

				// Tapscript enforces initial stack size limits (altstack is empty here)
				if (stack.Count > MAX_STACK_SIZE) return SetError(ScriptError.StackSize);
			}

			// Disallow stack item size > MAX_SCRIPT_ELEMENT_SIZE in witness stack
			for (int i = 0; i < ctx.Stack.Count; i++)
			{
				if (ctx.Stack.Top(-(i + 1)).Length > MAX_SCRIPT_ELEMENT_SIZE)
					return SetError(ScriptError.PushSize);
			}
			if (!ctx.EvalScript(scriptPubKey, checker, sigversion))
			{
				return SetError(ctx.Error);
			}
			// Scripts inside witness implicitly require cleanstack behaviour
			if (ctx.Stack.Count != 1)
				return SetError(ScriptError.EvalFalse);
			if (!CastToBool(ctx.Stack.Top(-1)))
				return SetError(ScriptError.EvalFalse);
			return true;
		}

		private bool IsOpSuccess(OpcodeType code)
		{
			var opcode = (byte)code;
			return opcode == 80 || opcode == 98 || (opcode >= 126 && opcode <= 129) ||
					(opcode >= 131 && opcode <= 134) || (opcode >= 137 && opcode <= 138) ||
					(opcode >= 141 && opcode <= 142) || (opcode >= 149 && opcode <= 153) ||
					(opcode >= 187 && opcode <= 254);
		}

		static readonly byte[] vchFalse = new byte[0];
		static readonly byte[] vchZero = new byte[0];
		static readonly byte[] vchTrue = new byte[] { 1 };
		const int MAX_OPS_PER_SCRIPT = 201;

		private const int MAX_SCRIPT_ELEMENT_SIZE = 520;
		const int MAX_SCRIPT_SIZE = 10000;
		internal bool EvalScript(Script s, TransactionChecker checker, HashVersion hashversion)
		{
			if ((hashversion == HashVersion.Original || hashversion == HashVersion.WitnessV0) && s.Length > MAX_SCRIPT_SIZE)
				return SetError(ScriptError.ScriptSize);

			SetError(ScriptError.UnknownError);

			var script = s.CreateReader();
			var pbegincodehash = 0;

			var vfExec = new Stack<bool>();
			var altstack = new ContextStack<byte[]>();
			uint opcode_pos = 0xffffffff; // So the first opcode will bump it to 1
			ExecutionData.CodeseparatorPosition = 0xFFFFFFFFU;
			var nOpCount = 0;
			var fRequireMinimal = (ScriptVerify & ScriptVerify.MinimalData) != 0;

			try
			{
				Op opcode;
				while ((opcode = script.Read()) != null)
				{
					++opcode_pos;
					//
					// Read instruction
					//
					if (opcode.PushData != null && opcode.PushData.Length > MAX_SCRIPT_ELEMENT_SIZE)
						return SetError(ScriptError.PushSize);

					if (hashversion == HashVersion.Original || hashversion == HashVersion.WitnessV0)
					{
						// Note how OP_RESERVED does not count towards the opcode limit.
						if (opcode.Code > OpcodeType.OP_16 && ++nOpCount > MAX_OPS_PER_SCRIPT)
						{
							return SetError(ScriptError.OpCount);
						}
					}

					if (opcode.Code == OpcodeType.OP_CAT ||
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
						return SetError(ScriptError.BadOpCode);

					if (fExec && 0 <= (int)opcode.Code && (int)opcode.Code <= (int)OpcodeType.OP_PUSHDATA4)
					{
						if (fRequireMinimal && !CheckMinimalPush(opcode.PushData, opcode.Code))
							return SetError(ScriptError.MinimalData);

						_stack.Push(opcode.PushData);
					}

					//if(fExec && opcode.PushData != null)
					//	_Stack.Push(opcode.PushData);
					else if (fExec || (OpcodeType.OP_IF <= opcode.Code && opcode.Code <= OpcodeType.OP_ENDIF))
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
							case OpcodeType.OP_16:
								{
									// ( -- value)
									var num = new CScriptNum((int)opcode.Code - (int)(OpcodeType.OP_1 - 1));
									_stack.Push(num.getvch());
									break;
								}
							//
							// Control
							//
							case OpcodeType.OP_NOP:
								break;
							case OpcodeType.OP_NOP1:
							case OpcodeType.OP_CHECKLOCKTIMEVERIFY:
								{
									if ((ScriptVerify & ScriptVerify.CheckLockTimeVerify) == 0)
									{
										// not enabled; treat as a NOP2
										if ((ScriptVerify & ScriptVerify.DiscourageUpgradableNops) != 0)
										{
											return SetError(ScriptError.DiscourageUpgradableNops);
										}
										break;
									}

									if (Stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);

									// Note that elsewhere numeric opcodes are limited to
									// operands in the range -2**31+1 to 2**31-1, however it is
									// legal for opcodes to produce results exceeding that
									// range. This limitation is implemented by CScriptNum's
									// default 4-byte limit.
									//
									// If we kept to that limit we'd have a year 2038 problem,
									// even though the nLockTime field in transactions
									// themselves is uint32 which only becomes meaningless
									// after the year 2106.
									//
									// Thus as a special case we tell CScriptNum to accept up
									// to 5-byte bignums, which are good until 2**39-1, well
									// beyond the 2**32-1 limit of the nLockTime field itself.
									CScriptNum nLockTime = new CScriptNum(_stack.Top(-1), fRequireMinimal, 5);

									// In the rare event that the argument may be < 0 due to
									// some arithmetic being done first, you can always use
									// 0 MAX CHECKLOCKTIMEVERIFY.
									if (nLockTime < 0)
										return SetError(ScriptError.NegativeLockTime);

									// Actually compare the specified lock time with the transaction.
									if (!CheckLockTime(nLockTime, checker))
										return SetError(ScriptError.UnsatisfiedLockTime);

									break;
								}
							case OpcodeType.OP_CHECKSEQUENCEVERIFY:
								{
									if ((ScriptVerify & ScriptVerify.CheckSequenceVerify) == 0)
									{
										// not enabled; treat as a NOP3
										if ((ScriptVerify & ScriptVerify.DiscourageUpgradableNops) != 0)
										{
											return SetError(ScriptError.DiscourageUpgradableNops);
										}
										break;
									}

									if (Stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);

									// nSequence, like nLockTime, is a 32-bit unsigned integer
									// field. See the comment in CHECKLOCKTIMEVERIFY regarding
									// 5-byte numeric operands.
									CScriptNum nSequence = new CScriptNum(Stack.Top(-1), fRequireMinimal, 5);

									// In the rare event that the argument may be < 0 due to
									// some arithmetic being done first, you can always use
									// 0 MAX CHECKSEQUENCEVERIFY.
									if (nSequence < 0)
										return SetError(ScriptError.NegativeLockTime);

									// To provide for future soft-fork extensibility, if the
									// operand has the disabled lock-time flag set,
									// CHECKSEQUENCEVERIFY behaves as a NOP.
									if (((uint)nSequence & Sequence.SEQUENCE_LOCKTIME_DISABLE_FLAG) != 0)
										break;
									// Compare the specified sequence number with the input.
									if (!CheckSequence(nSequence, checker))
										return SetError(ScriptError.UnsatisfiedLockTime);

									break;
								}


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
							case OpcodeType.OP_NOTIF:
								{
									// <expression> if [statements] [else [statements]] endif
									var bValue = false;
									if (fExec)
									{
										if (_stack.Count < 1)
											return SetError(ScriptError.UnbalancedConditional);

										var vch = _stack.Top(-1);

										// Tapscript requires minimal IF/NOTIF inputs as a consensus rule.
										if (hashversion == HashVersion.Tapscript)
										{
											// The input argument to the OP_IF and OP_NOTIF opcodes must be either
											// exactly 0 (the empty vector) or exactly 1 (the one-byte vector with value 1).
											if (vch.Length > 1 || (vch.Length == 1 && vch[0] != 1))
											{
												return SetError(ScriptError.MinimalIf);
											}
										}
										// Under witness v0 rules it is only a policy rule, enabled through SCRIPT_VERIFY_MINIMALIF.
										if (hashversion == HashVersion.WitnessV0 && (ScriptVerify & ScriptVerify.MinimalIf) != 0)
										{
											if (vch.Length > 1)
												return SetError(ScriptError.MinimalIf);
											if (vch.Length == 1 && vch[0] != 1)
												return SetError(ScriptError.MinimalIf);
										}

										bValue = CastToBool(vch);
										if (opcode.Code == OpcodeType.OP_NOTIF)
											bValue = !bValue;
										_stack.Pop();
									}
									vfExec.Push(bValue);
									break;
								}
							case OpcodeType.OP_ELSE:
								{
									if (vfExec.Count == 0)
										return SetError(ScriptError.UnbalancedConditional);

									var v = vfExec.Pop();
									vfExec.Push(!v);
									break;
								}
							case OpcodeType.OP_ENDIF:
								{
									if (vfExec.Count == 0)
										return SetError(ScriptError.UnbalancedConditional);

									vfExec.Pop();
									break;
								}
							case OpcodeType.OP_VERIFY:
								{
									// (true -- ) or
									// (false -- false) and return
									if (_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);

									if (!CastToBool(_stack.Top(-1)))
										return SetError(ScriptError.Verify);

									_stack.Pop();
									break;
								}
							case OpcodeType.OP_RETURN:
								{
									return SetError(ScriptError.OpReturn);
								}
							//
							// Stack ops
							//
							case OpcodeType.OP_TOALTSTACK:
								{
									if (_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);

									altstack.Push(_stack.Top(-1));
									_stack.Pop();
									break;
								}
							case OpcodeType.OP_FROMALTSTACK:
								{
									if (altstack.Count < 1)
										return SetError(ScriptError.InvalidAltStackOperation);

									_stack.Push(altstack.Top(-1));
									altstack.Pop();
									break;
								}
							case OpcodeType.OP_2DROP:
								{
									// (x1 x2 -- )
									if (_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									_stack.Pop();
									_stack.Pop();
									break;
								}
							case OpcodeType.OP_2DUP:
								{
									// (x1 x2 -- x1 x2 x1 x2)
									if (_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									var vch1 = _stack.Top(-2);
									var vch2 = _stack.Top(-1);
									_stack.Push(vch1);
									_stack.Push(vch2);
									break;
								}
							case OpcodeType.OP_3DUP:
								{
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
							case OpcodeType.OP_2OVER:
								{
									// (x1 x2 x3 x4 -- x1 x2 x3 x4 x1 x2)
									if (_stack.Count < 4)
										return SetError(ScriptError.InvalidStackOperation);

									var vch1 = _stack.Top(-4);
									var vch2 = _stack.Top(-3);
									_stack.Push(vch1);
									_stack.Push(vch2);
									break;
								}
							case OpcodeType.OP_2ROT:
								{
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
							case OpcodeType.OP_2SWAP:
								{
									// (x1 x2 x3 x4 -- x3 x4 x1 x2)
									if (_stack.Count < 4)
										return SetError(ScriptError.InvalidStackOperation);

									_stack.Swap(-4, -2);
									_stack.Swap(-3, -1);
									break;
								}
							case OpcodeType.OP_IFDUP:
								{
									// (x - 0 | x x)
									if (_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);

									var vch = _stack.Top(-1);
									if (CastToBool(vch))
										_stack.Push(vch);
									break;
								}
							case OpcodeType.OP_DEPTH:
								{
									// -- stacksize
									var bn = new CScriptNum(_stack.Count);
									_stack.Push(bn.getvch());
									break;
								}
							case OpcodeType.OP_DROP:
								{
									// (x -- )
									if (_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);

									_stack.Pop();
									break;
								}
							case OpcodeType.OP_DUP:
								{
									// (x -- x x)
									if (_stack.Count < 1)
										return SetError(ScriptError.InvalidStackOperation);

									var vch = _stack.Top(-1);
									_stack.Push(vch);
									break;
								}
							case OpcodeType.OP_NIP:
								{
									// (x1 x2 -- x2)
									if (_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									_stack.Remove(-2);
									break;
								}
							case OpcodeType.OP_OVER:
								{
									// (x1 x2 -- x1 x2 x1)
									if (_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									var vch = _stack.Top(-2);
									_stack.Push(vch);
									break;
								}
							case OpcodeType.OP_PICK:
							case OpcodeType.OP_ROLL:
								{
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
							case OpcodeType.OP_ROT:
								{
									// (x1 x2 x3 -- x2 x3 x1)
									//  x2 x1 x3  after first swap
									//  x2 x3 x1  after second swap
									if (_stack.Count < 3)
										return SetError(ScriptError.InvalidStackOperation);

									_stack.Swap(-3, -2);
									_stack.Swap(-2, -1);
									break;
								}
							case OpcodeType.OP_SWAP:
								{
									// (x1 x2 -- x2 x1)
									if (_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									_stack.Swap(-2, -1);
									break;
								}
							case OpcodeType.OP_TUCK:
								{
									// (x1 x2 -- x2 x1 x2)
									if (_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									var vch = _stack.Top(-1);
									_stack.Insert(-3, vch);
									break;
								}
							case OpcodeType.OP_SIZE:
								{
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
							case OpcodeType.OP_EQUALVERIFY:
								{
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
							case OpcodeType.OP_0NOTEQUAL:
								{
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
							case OpcodeType.OP_MAX:
								{
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
							case OpcodeType.OP_WITHIN:
								{
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
							case OpcodeType.OP_HASH256:
								{
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
										vchHash = Hashes.DoubleSHA256(vch, 0, vch.Length).ToBytes();
									_stack.Pop();
									_stack.Push(vchHash);
									break;
								}
							case OpcodeType.OP_CODESEPARATOR:
								{
									// Hash starts after the code separator
									pbegincodehash = (int)script.Inner.Position;
									ExecutionData.CodeseparatorPosition = opcode_pos;
									break;
								}
							case OpcodeType.OP_CHECKSIG:
							case OpcodeType.OP_CHECKSIGVERIFY:
								{
									// (sig pubkey -- bool)
									if (_stack.Count < 2)
										return SetError(ScriptError.InvalidStackOperation);

									var vchSig = _stack.Top(-2);
									var vchPubKey = _stack.Top(-1);
									bool fSuccess = true;

									if (!EvalChecksig(vchSig, vchPubKey, s, pbegincodehash, checker, hashversion, out fSuccess)) return false;

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
#if HAS_SPAN
							case OpcodeType.OP_CHECKSIGADD:
								{
									// OP_CHECKSIGADD is only available in Tapscript
									if (hashversion == HashVersion.Original || hashversion == HashVersion.WitnessV0) return SetError(ScriptError.BadOpCode);

									// (sig num pubkey -- num)
									if (_stack.Count < 3) return SetError(ScriptError.InvalidStackOperation);

									var sig = _stack.Top(-3);
									CScriptNum num = new CScriptNum(_stack.Top(-2), fRequireMinimal);
									var pubkey = _stack.Top(-1);

									bool success = true;
									if (!EvalChecksigTapscript(sig, pubkey, checker, hashversion, out success)) return false;
									_stack.Pop();
									_stack.Pop();
									_stack.Pop();
									_stack.Push((num + (success ? 1 : 0)).getvch());
									break;
								}
#endif
							case OpcodeType.OP_CHECKMULTISIG:
							case OpcodeType.OP_CHECKMULTISIGVERIFY:
								{
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
									// ikey2 is the position of last non-signature item in the stack. Top stack item = 1.
									// With SCRIPT_VERIFY_NULLFAIL, this is used for cleanup if operation fails.
									int ikey2 = nKeysCount + 2;
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
										if (hashversion == (int)HashVersion.Original)
											scriptCode = scriptCode.FindAndDelete(vchSig);
									}

									bool fSuccess = true;
									while (fSuccess && nSigsCount > 0)
									{
										var vchSig = _stack.Top(-isig);
										var vchPubKey = _stack.Top(-ikey);

										// Note how this makes the exact order of pubkey/signature evaluation
										// distinguishable by CHECKMULTISIG NOT if the STRICTENC flag is set.
										// See the script_(in)valid tests for details.
										if (!CheckSignatureEncoding(vchSig) || !CheckPubKeyEncoding(vchPubKey, hashversion))
										{
											// serror is set
											return false;
										}

										bool fOk = CheckSig(vchSig, vchPubKey, scriptCode, checker, hashversion);

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

									// Clean up stack of actual arguments
									while (i-- > 1)
									{
										// If the operation failed, we require that all signatures must be empty vector
										if (!fSuccess && (ScriptVerify & ScriptVerify.NullFail) != 0 && ikey2 == 0 && _stack.Top(-1).Length != 0)
											return SetError(ScriptError.NullFail);
										if (ikey2 > 0)
											ikey2--;
										_stack.Pop();
									}

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
					if (_stack.Count + altstack.Count > 1000)
						return SetError(ScriptError.StackSize);
				}
			}
			catch (Exception ex)
			{
				ThrownException = ex;
				return SetError(ScriptError.UnknownError);
			}

			if (vfExec.Count != 0)
				return SetError(ScriptError.UnbalancedConditional);

			return SetSuccess(ScriptError.OK);
		}
		const long VALIDATION_WEIGHT_PER_SIGOP_PASSED = 50;

		private bool EvalChecksig(byte[] sig, byte[] pubkey, Script s, int pbegincodehash, TransactionChecker checker, HashVersion sigversion, out bool success)
		{
			switch	(sigversion)
			{
				case HashVersion.Original:
				case HashVersion.WitnessV0:
					return EvalChecksigPreTapscript(sig, pubkey, s, pbegincodehash, checker, sigversion, out success);
#if HAS_SPAN
				case HashVersion.Tapscript:
					return EvalChecksigTapscript(sig, pubkey, checker, sigversion, out success);
#endif
				default:
					throw new NotSupportedException("NBitcoin bug 29174: Contact NBitcoin developers, this should never happen");
			}
		}

		private bool EvalChecksigPreTapscript(byte[] vchSig, byte[] vchPubKey, Script s, int pbegincodehash, TransactionChecker checker, HashVersion sigversion, out bool success)
		{
			success = true;
			// Subset of script starting at the most recent codeseparator
			var scriptCode = new Script(s._Script.Skip(pbegincodehash).ToArray());
			// Drop the signature, since there's no way for a signature to sign itself
			if (sigversion == (int)HashVersion.Original)
				scriptCode = scriptCode.FindAndDelete(vchSig);

			if (!CheckSignatureEncoding(vchSig) || !CheckPubKeyEncoding(vchPubKey, sigversion))
			{
				//serror is set
				return false;
			}

			success = CheckSig(vchSig, vchPubKey, scriptCode, checker, sigversion);
			if (!success && (ScriptVerify & ScriptVerify.NullFail) != 0 && vchSig.Length != 0)
				return SetError(ScriptError.NullFail);

			return true;
		}
#if HAS_SPAN
		private bool EvalChecksigTapscript(byte[] sig, byte[] pubkey, TransactionChecker checker, HashVersion sigversion, out bool success)
		{
			/*
     *  The following validation sequence is consensus critical. Please note how --
     *    upgradable public key versions precede other rules;
     *    the script execution fails when using empty signature with invalid public key;
     *    the script execution fails when using non-empty invalid signature.
     */
			success = !(sig.Length is 0);
			if (success)
			{
				// Implement the sigops/witnesssize ratio test.
				// Passing with an upgradable public key version is also counted.
				ExecutionData.ValidationWeightLeft -= VALIDATION_WEIGHT_PER_SIGOP_PASSED;
				if (ExecutionData.ValidationWeightLeft < 0)
					return SetError(ScriptError.TapscriptValidationWeight);
			}

			if (pubkey.Length is 0)
				return SetError(ScriptError.PubKeyType);
			else if (pubkey.Length is 32)
			{
				if (success && !checker.CheckSchnorrSignature(sig, pubkey, sigversion, ExecutionData, out var err))
					return SetError(err);
			}
			else
			{
				/*
	*  New public key version softforks should be defined before this `else` block.
	*  Generally, the new code should not do anything but failing the script execution. To avoid
	*  consensus bugs, it should not modify any existing values (including `success`).
	*/
				if (ScriptVerify.HasFlag(ScriptVerify.DiscourageUpgradablePubKeyType))
				{
					return SetError(ScriptError.DiscourageUpgradablePubKeyType);
				}
			}
			return true;
		}
#endif
		bool CheckSequence(CScriptNum nSequence, TransactionChecker checker)
		{
			var txTo = checker.Transaction;
			var nIn = checker.Index;
			// Relative lock times are supported by comparing the passed
			// in operand to the sequence number of the input.
			long txToSequence = (long)txTo.Inputs[nIn].Sequence;

			// Fail if the transaction's version number is not set high
			// enough to trigger BIP 68 rules.
			if (txTo.Version < 2)
				return false;

			// Sequence numbers with their most significant bit set are not
			// consensus constrained. Testing that the transaction's sequence
			// number do not have this bit set prevents using this property
			// to get around a CHECKSEQUENCEVERIFY check.
			if ((txToSequence & Sequence.SEQUENCE_LOCKTIME_DISABLE_FLAG) != 0)
				return false;

			// Mask off any bits that do not have consensus-enforced meaning
			// before doing the integer comparisons
			var nLockTimeMask = Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG | Sequence.SEQUENCE_LOCKTIME_MASK;
			var txToSequenceMasked = txToSequence & nLockTimeMask;
			CScriptNum nSequenceMasked = nSequence & nLockTimeMask;

			// There are two kinds of nSequence: lock-by-blockheight
			// and lock-by-blocktime, distinguished by whether
			// nSequenceMasked < CTxIn::SEQUENCE_LOCKTIME_TYPE_FLAG.
			//
			// We want to compare apples to apples, so fail the script
			// unless the type of nSequenceMasked being tested is the same as
			// the nSequenceMasked in the transaction.
			if (!(
				(txToSequenceMasked < Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG && nSequenceMasked < Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG) ||
				(txToSequenceMasked >= Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG && nSequenceMasked >= Sequence.SEQUENCE_LOCKTIME_TYPE_FLAG)
			))
			{
				return false;
			}

			// Now that we know we're comparing apples-to-apples, the
			// comparison is a simple numeric one.
			if (nSequenceMasked > txToSequenceMasked)
				return false;

			return true;
		}


		bool CheckLockTime(CScriptNum nLockTime, TransactionChecker checker)
		{
			var txTo = checker.Transaction;
			var nIn = checker.Index;
			// There are two kinds of nLockTime: lock-by-blockheight
			// and lock-by-blocktime, distinguished by whether
			// nLockTime < LOCKTIME_THRESHOLD.
			//
			// We want to compare apples to apples, so fail the script
			// unless the type of nLockTime being tested is the same as
			// the nLockTime in the transaction.
			if (!(
				(txTo.LockTime < LockTime.LOCKTIME_THRESHOLD && nLockTime < LockTime.LOCKTIME_THRESHOLD) ||
				(txTo.LockTime >= LockTime.LOCKTIME_THRESHOLD && nLockTime >= LockTime.LOCKTIME_THRESHOLD)
			))
				return false;

			// Now that we know we're comparing apples-to-apples, the
			// comparison is a simple numeric one.
			if (nLockTime > (long)txTo.LockTime)
				return false;

			// Finally the nLockTime feature can be disabled and thus
			// CHECKLOCKTIMEVERIFY bypassed if every txin has been
			// finalized by setting nSequence to maxint. The
			// transaction would be allowed into the blockchain, making
			// the opcode ineffective.
			//
			// Testing if this vin is not final is sufficient to
			// prevent this condition. Alternatively we could test all
			// inputs, but testing just this input minimizes the data
			// required to prove correct CHECKLOCKTIMEVERIFY execution.
			if (Sequence.SEQUENCE_FINAL == txTo.Inputs[nIn].Sequence)
				return false;

			return true;
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
			if (vchPubKey.Length < 33)
			{
				//  Non-canonical public key: too short
				return false;
			}
			if (vchPubKey[0] == 0x04)
			{
				if (vchPubKey.Length != 65)
				{
					//  Non-canonical public key: invalid length for uncompressed key
					return false;
				}
			}
			else if (vchPubKey[0] == 0x02 || vchPubKey[0] == 0x03)
			{
				if (vchPubKey.Length != 33)
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

		internal bool CheckSignatureEncoding(byte[] vchSig)
		{
			// Empty signature. Not strictly DER encoded, but allowed to provide a
			// compact way to provide an invalid signature for use with CHECK(MULTI)SIG
			if (vchSig.Length == 0)
			{
				return true;
			}
			if ((ScriptVerify & (ScriptVerify.DerSig | ScriptVerify.LowS | ScriptVerify.StrictEnc)) != 0 && !IsValidSignatureEncoding(vchSig))
			{
				Error = ScriptError.SigDer;
				return false;
			}
			if ((ScriptVerify & ScriptVerify.LowS) != 0 && !IsLowDERSignature(vchSig))
			{
				// serror is set
				return false;
			}
			if ((ScriptVerify & ScriptVerify.StrictEnc) != 0 && !IsDefinedHashtypeSignature(vchSig))
			{
				Error = ScriptError.SigHashType;
				return false;
			}
			return true;
		}

		private bool CheckPubKeyEncoding(byte[] vchPubKey, HashVersion sigversion)
		{
			if ((ScriptVerify & ScriptVerify.StrictEnc) != 0 && !IsCompressedOrUncompressedPubKey(vchPubKey))
			{
				Error = ScriptError.PubKeyType;
				return false;
			}
			if ((ScriptVerify & ScriptVerify.WitnessPubkeyType) != 0 && sigversion == HashVersion.WitnessV0 && !IsCompressedPubKey(vchPubKey))
			{
				return SetError(ScriptError.WitnessPubkeyType);
			}
			return true;
		}

		static bool IsCompressedPubKey(byte[] vchPubKey)
		{
			if (vchPubKey.Length != 33)
			{
				//  Non-canonical public key: invalid length for compressed key
				return false;
			}
			if (vchPubKey[0] != 0x02 && vchPubKey[0] != 0x03)
			{
				//  Non-canonical public key: invalid prefix for compressed key
				return false;
			}
			return true;
		}


		bool IsDefinedHashtypeSignature(byte[] vchSig)
		{
			if (vchSig.Length == 0)
			{
				return false;
			}

			var temp = ~(SigHash.AnyoneCanPay);
			if ((ScriptVerify & ScriptVerify.ForkId) != 0)
			{
				temp = (SigHash)((uint)temp & ~(0x40u));
			}
			byte nHashType = (byte)(vchSig[vchSig.Length - 1] & (byte)temp);
			if (nHashType < (byte)SigHash.All || nHashType > (byte)SigHash.Single)
				return false;

			return true;
		}

		private bool IsLowDERSignature(byte[] vchSig)
		{
			if (!IsValidSignatureEncoding(vchSig))
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
			if (!CheckSignatureElement(vchSig, S, nLenS, true))
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
			while (c1len > c2len)
			{
				if (c1[ic1] != 0)
					return 1;
				ic1++;
				c1len--;
			}
			while (c2len > c1len)
			{
				if (c2[ic2] != 0)
					return -1;
				ic2++;
				c2len--;
			}
			while (c1len > 0)
			{
				if (c1[ic1] > c2[ic2])
					return 1;
				if (c2[ic2] > c1[ic1])
					return -1;
				ic1++;
				ic2++;
				c1len--;
			}
			return 0;
		}


		static bool IsValidSignatureEncoding(byte[] sig)
		{
			// Format: 0x30 [total-length] 0x02 [R-length] [R] 0x02 [S-length] [S] [sighash]
			// * total-length: 1-byte length descriptor of everything that follows,
			//   excluding the sighash byte.
			// * R-length: 1-byte length descriptor of the R value that follows.
			// * R: arbitrary-length big-endian encoded R value. It must use the shortest
			//   possible encoding for a positive integers (which means no null bytes at
			//   the start, except a single one when the next byte has its highest bit set).
			// * S-length: 1-byte length descriptor of the S value that follows.
			// * S: arbitrary-length big-endian encoded S value. The same rules apply.
			// * sighash: 1-byte value indicating what data is hashed (not part of the DER
			//   signature)

			var signLen = sig.Length;

			// Minimum and maximum size constraints.
			if (signLen < 9 || signLen > 73)
				return false;

			// A signature is of type 0x30 (compound).
			if (sig[0] != 0x30)
				return false;

			// Make sure the length covers the entire signature.
			if (sig[1] != signLen - 3)
				return false;

			// Extract the length of the R element.
			uint lenR = sig[3];

			// Make sure the length of the S element is still inside the signature.
			if (5 + lenR >= signLen)
				return false;

			// Extract the length of the S element.
			uint lenS = sig[5 + lenR];

			// Verify that the length of the signature matches the sum of the length
			// of the elements.
			if ((lenR + lenS + 7) != signLen)
				return false;

			// Check whether the R element is an integer.
			if (sig[2] != 0x02)
				return false;

			// Zero-length integers are not allowed for R.
			if (lenR == 0)
				return false;

			// Negative numbers are not allowed for R.
			if ((sig[4] & 0x80) != 0)
				return false;

			// Null bytes at the start of R are not allowed, unless R would
			// otherwise be interpreted as a negative number.
			if (lenR > 1 && (sig[4] == 0x00) && (sig[5] & 0x80) == 0)
				return false;

			// Check whether the S element is an integer.
			if (sig[lenR + 4] != 0x02)
				return false;

			// Zero-length integers are not allowed for S.
			if (lenS == 0)
				return false;

			// Negative numbers are not allowed for S.
			if ((sig[lenR + 6] & 0x80) != 0)
				return false;

			// Null bytes at the start of S are not allowed, unless S would otherwise be
			// interpreted as a negative number.
			if (lenS > 1 && (sig[lenR + 6] == 0x00) && (sig[lenR + 7] & 0x80) == 0)
				return false;

			return true;
		}


		bool CheckMinimalPush(byte[] data, OpcodeType opcode)
		{
			if (data.Length == 0)
			{
				// Could have used OP_0.
				return opcode == OpcodeType.OP_0;
			}
			else if (data.Length == 1 && data[0] >= 1 && data[0] <= 16)
			{
				// Could have used OP_1 .. OP_16.
				return (int)opcode == ((int)OpcodeType.OP_1) + (data[0] - 1);
			}
			else if (data.Length == 1 && data[0] == 0x81)
			{
				// Could have used OP_1NEGATE.
				return opcode == OpcodeType.OP_1NEGATE;
			}
			else if (data.Length <= 75)
			{
				// Could have used a direct push (opcode indicating number of bytes pushed + those bytes).
				return (int)opcode == data.Length;
			}
			else if (data.Length <= 255)
			{
				// Could have used OP_PUSHDATA.
				return opcode == OpcodeType.OP_PUSHDATA1;
			}
			else if (data.Length <= 65535)
			{
				// Could have used OP_PUSHDATA2.
				return opcode == OpcodeType.OP_PUSHDATA2;
			}
			return true;
		}

		private static bool CastToBool(byte[] vch)
		{
			for (uint i = 0; i < vch.Length; i++)
			{
				if (vch[i] != 0)
				{

					if (i == vch.Length - 1 && vch[i] == 0x80)
						return false;
					return true;
				}
			}
			return false;
		}

		List<SignedHash> _SignedHashes = new List<SignedHash>();
		public IEnumerable<SignedHash> SignedHashes
		{
			get
			{
				return _SignedHashes;
			}
		}
		private bool CheckSig(byte[] vchSig, byte[] vchPubKey, Script scriptCode, TransactionChecker checker, HashVersion sigversion)
		{
			PubKey pubkey = null;
			try
			{
				pubkey = new PubKey(vchPubKey);
			}
			catch (Exception)
			{
				return false;
			}


			// Hash type is one byte tacked on to the end of the signature
			if (vchSig.Length == 0)
				return false;

			TransactionSignature scriptSig = null;
			try
			{
				scriptSig = new TransactionSignature(vchSig);
			}
			catch (Exception)
			{
				if ((ScriptVerify.DerSig & ScriptVerify) != 0)
					throw;
				return false;
			}

			uint256 sighash = checker.Transaction.GetSignatureHash(scriptCode, checker.Index, scriptSig.SigHash, checker.SpentOutput, (HashVersion)sigversion, checker.PrecomputedTransactionData);
			_SignedHashes.Add(new SignedHash()
			{
				ScriptCode = scriptCode,
				HashVersion = (HashVersion)sigversion,
				Hash = sighash,
				Signature = scriptSig
			});
			if (!pubkey.Verify(sighash, scriptSig.Signature))
			{
				if ((ScriptVerify & ScriptVerify.StrictEnc) != 0)
					return false;
#if HAS_SPAN
				return false;
#else
				//Replicate OpenSSL bug on 23b397edccd3740a74adb603c9756370fafcde9bcc4483eb271ecad09a94dd63 (http://r6.ca/blog/20111119T211504Z.html)
#pragma warning disable 618
				var nLenR = vchSig[3];
				var nLenS = vchSig[5 + nLenR];
				var R = 4;
				var S = 6 + nLenR;
				var newS = new NBitcoin.BouncyCastle.Math.BigInteger(1, vchSig, S, nLenS);
				var newR = new NBitcoin.BouncyCastle.Math.BigInteger(1, vchSig, R, nLenR);
				var sig2 = new ECDSASignature(newR, newS);
				if (sig2.R != scriptSig.Signature.R || sig2.S != scriptSig.Signature.S)
				{
					if (!pubkey.Verify(sighash, sig2))
						return false;
				}
#pragma warning restore 618
#endif
			}

			return true;
		}

		private void Load(ScriptEvaluationContext other)
		{
			_stack = new ContextStack<byte[]>(other._stack);
			ScriptVerify = other.ScriptVerify;
		}

		public ScriptEvaluationContext Clone()
		{
			return new ScriptEvaluationContext()
			{
				_stack = new ContextStack<byte[]>(_stack),
				ScriptVerify = ScriptVerify,
				_SignedHashes = _SignedHashes,
				ExecutionData = ExecutionData,
				Error = Error,
			};
		}

		public bool Result
		{
			get
			{
				if (Stack.Count == 0)
					return false;
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
	/// operations not typically available in a "pure" Stack class, as example:
	/// Insert, Swap, Erase and Top (Peek w/index)
	/// </summary>
	/// <typeparam name="T"></typeparam>
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

		public ContextStack(IEnumerable<T> elements) : this()
		{
			foreach (var el in elements)
				Push(el);
		}

		/// <summary>
		/// Gets the number of items in the stack.
		/// </summary>
		public int Count
		{
			get
			{
				return _position + 1;
			}
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
			if (n > Count)
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
			if (i >= 0 || -i > Count)
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
			if (i >= 0 || -i > Count)
				throw new IndexOutOfRangeException("i");
			if (j >= 0 || -j > Count)
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

			position = Count + position;
			for (int i = _position; i >= position + 1; i--)
			{
				_array[i + 1] = _array[i];
			}
			_array[position + 1] = value;
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
			int toRemove = to - from;
			for (int i = Count + from; i < Count + from + toRemove; i++)
			{
				for (int y = Count + from; y < Count; y++)
					_array[y] = _array[y + 1];
			}
			_position -= toRemove;
		}

		private void EnsureSize()
		{
			if (_position < _array.Length - 1)
				return;
			Array.Resize(ref _array, 2 * _array.Length);
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

			public Enumerator(ContextStack<T> stack)
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
				get
				{
					return Current;
				}
			}

			public bool MoveNext()
			{
				return --_index >= 0;
			}

			public void Reset()
			{
				_index = _stack._position + 1;
			}

			public void Dispose()
			{
			}
		}
		#endregion

		internal void Clear()
		{
			Clear(Count);
		}
	}
}
