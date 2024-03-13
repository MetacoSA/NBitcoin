#nullable enable
using System.Runtime.InteropServices;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using NBitcoin.Crypto;

namespace NBitcoin
{
	/// <summary>
	/// Script verification flags
	/// </summary>
	[Flags]
	public enum ScriptVerify : uint
	{
		None = 0,

		/// <summary>
		/// Evaluate P2SH subscripts (softfork safe, BIP16).
		/// </summary>
		P2SH = (1U << 0),

		/// <summary>
		/// Passing a non-strict-DER signature or one with undefined hashtype to a checksig operation causes script failure.
		/// Passing a pubkey that is not (0x04 + 64 bytes) or (0x02 or 0x03 + 32 bytes) to checksig causes that pubkey to be
		/// +
		/// skipped (not softfork safe: this flag can widen the validity of OP_CHECKSIG OP_NOT).
		/// </summary>
		StrictEnc = (1U << 1),

		/// <summary>
		/// Passing a non-strict-DER signature to a checksig operation causes script failure (softfork safe, BIP62 rule 1)
		/// </summary>
		DerSig = (1U << 2),

		/// <summary>
		/// Passing a non-strict-DER signature or one with S > order/2 to a checksig operation causes script failure
		/// (softfork safe, BIP62 rule 5).
		/// </summary>
		LowS = (1U << 3),

		/// <summary>
		/// verify dummy stack item consumed by CHECKMULTISIG is of zero-length (softfork safe, BIP62 rule 7).
		/// </summary>
		NullDummy = (1U << 4),

		/// <summary>
		/// Using a non-push operator in the scriptSig causes script failure (softfork safe, BIP62 rule 2).
		/// </summary>
		SigPushOnly = (1U << 5),

		/// <summary>
		/// Require minimal encodings for all push operations (OP_0... OP_16, OP_1NEGATE where possible, direct
		/// pushes up to 75 bytes, OP_PUSHDATA up to 255 bytes, OP_PUSHDATA2 for anything larger). Evaluating
		/// any other push causes the script to fail (BIP62 rule 3).
		/// In addition, whenever a stack element is interpreted as a number, it must be of minimal length (BIP62 rule 4).
		/// (softfork safe)
		/// </summary>
		MinimalData = (1U << 6),

		/// <summary>
		/// Discourage use of NOPs reserved for upgrades (NOP1-10)
		///
		/// Provided so that nodes can avoid accepting or mining transactions
		/// containing executed NOP's whose meaning may change after a soft-fork,
		/// thus rendering the script invalid; with this flag set executing
		/// discouraged NOPs fails the script. This verification flag will never be
		/// a mandatory flag applied to scripts in a block. NOPs that are not
		/// executed, e.g.  within an unexecuted IF ENDIF block, are *not* rejected.
		/// </summary>
		DiscourageUpgradableNops = (1U << 7),

		/// <summary>
		/// Require that only a single stack element remains after evaluation. This changes the success criterion from
		/// "At least one stack element must remain, and when interpreted as a boolean, it must be true" to
		/// "Exactly one stack element must remain, and when interpreted as a boolean, it must be true".
		/// (softfork safe, BIP62 rule 6)
		/// Note: CLEANSTACK should never be used without P2SH.
		/// </summary>
		CleanStack = (1U << 8),

		/// <summary>
		/// Verify CHECKLOCKTIMEVERIFY
		///
		/// See BIP65 for details.
		/// </summary>
		CheckLockTimeVerify = (1U << 9),

		/// <summary>
		/// See BIP68 for details.
		/// </summary>
		CheckSequenceVerify = (1U << 10),

		/// <summary>
		/// Support segregated witness
		/// </summary>
		Witness = (1U << 11),

		/// <summary>
		/// Making v2-v16 witness program non-standard
		/// </summary>
		DiscourageUpgradableWitnessProgram = (1U << 12),

		/// <summary>
		/// Segwit script only: Require the argument of OP_IF/NOTIF to be exactly 0x01 or empty vector
		/// </summary>
		MinimalIf = (1U << 13),

		/// <summary>
		/// Signature(s) must be empty vector if an CHECK(MULTI)SIG operation failed
		/// </summary>
		NullFail = (1U << 14),

		/// <summary>
		/// Public keys in segregated witness scripts must be compressed
		/// </summary>
		WitnessPubkeyType = (1U << 15),

		/// <summary>
		/// Some altcoins like BCash and BGold requires ForkId inside the sigHash
		/// </summary>
		ForkId = (1U << 29),

		/// <summary>
		/// Taproot/Tapscript validation (BIPs 341 & 342)
		/// </summary>
		Taproot = (1U << 17),
		/// <summary>
		/// Making unknown Taproot leaf versions non-standard
		/// </summary>
		DiscourageUpgradableTaprootVersion = (1U << 18),
		// Making unknown OP_SUCCESS non-standard
		DiscourageOpSuccess = (1U << 19),
		// Making unknown public key versions (in BIP 342 scripts) non-standard
		DiscourageUpgradablePubKeyType = (1U << 20),

		/// <summary>
		/// Mandatory script verification flags that all new blocks must comply with for
		/// them to be valid. (but old blocks may not comply with) Currently just P2SH,
		/// but in the future other flags may be added, such as a soft-fork to enforce
		/// strict DER encoding.
		///
		/// Failing one of these tests may trigger a DoS ban - see CheckInputs() for
		/// details.
		/// </summary>
		Mandatory = P2SH,

		/// <summary>
		/// Standard script verification flags that standard transactions will comply
		/// with. However scripts violating these flags may still be present in valid
		/// blocks and we must accept those blocks.
		/// </summary>
		Standard =
			  Mandatory
			| DerSig
			| StrictEnc
			| MinimalData
			| NullDummy
			| DiscourageUpgradableNops
			| CleanStack
			| CheckLockTimeVerify
			| CheckSequenceVerify
			| LowS
			| Witness
			| DiscourageUpgradableWitnessProgram
			| NullFail
			| MinimalIf
			| Taproot
			| DiscourageUpgradableTaprootVersion
			| DiscourageOpSuccess
			| DiscourageUpgradablePubKeyType,


		/// <summary>
		/// Strict consensus script verification flags that are checked by the Bitcoin Core
		/// consensus library before validating the transaction.
		/// </summary>
		Consensus =
			  ScriptVerify.DerSig
			| ScriptVerify.P2SH
			| ScriptVerify.NullDummy
			| ScriptVerify.CheckLockTimeVerify
			| ScriptVerify.CheckSequenceVerify
			| ScriptVerify.Witness
	}

	/// <summary>
	/// Signature hash types/flags
	/// </summary>
	[Flags]
	public enum SigHash : uint
	{
		/// <summary>
		/// All outputs are signed
		/// </summary>
		All = 1,
		/// <summary>
		/// No outputs as signed
		/// </summary>
		None = 2,
		/// <summary>
		/// Only the output with the same index as this input is signed
		/// </summary>
		Single = 3,
		/// <summary>
		/// If set, no inputs, except this, are part of the signature
		/// </summary>
		AnyoneCanPay = 0x80
	};

	/// <summary>
	/// Signature hash types/flags for taproot transactions
	/// </summary>
	[Flags]
	public enum TaprootSigHash : uint
	{
		Default = 0,
		/// <summary>
		/// All outputs are signed
		/// </summary>
		All = 1,
		/// <summary>
		/// No outputs as signed
		/// </summary>
		None = 2,
		/// <summary>
		/// Only the output with the same index as this input is signed
		/// </summary>
		Single = 3,
		/// <summary>
		/// If set, no inputs, except this, are part of the signature
		/// </summary>
		AnyoneCanPay = 0x80
	};

	/// <summary>
	/// Script opcodes
	/// </summary>
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

		OP_CHECKLOCKTIMEVERIFY = 0xb1,
		OP_CHECKSEQUENCEVERIFY = 0xb2,

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
		OP_CHECKSIGADD = 0xba
	}
	public enum HashVersion
	{
		Original = 0,
		WitnessV0 = 1,
		/// <summary>
		/// Key spend
		/// </summary>
		Taproot = 2,
		/// <summary>
		/// Script spend
		/// </summary>
		Tapscript = 3,
	}

	public enum ScriptType
	{
		Witness,
		P2PKH,
		P2SH,
		P2PK,
		P2WPKH,
		P2WSH,
		MultiSig,
		Taproot,
	}

	public class ScriptSigs
	{
		public ScriptSigs()
		{
			WitSig = WitScript.Empty;
			ScriptSig = Script.Empty;
		}
		public Script ScriptSig
		{
			get;
			set;
		}
		public WitScript WitSig
		{
			get;
			set;
		}
	}

	public class Script : IEquatable<Script>
	{
		static readonly Script _Empty = new Script();
		public static Script Empty
		{
			get
			{
				return _Empty;
			}
		}

		internal readonly byte[] _Script;
		public Script()
		{
#if NO_ARRAY_EMPTY
			_Script = new byte[0];
#else
			_Script = Array.Empty<byte>();
#endif
		}
		public Script(params Op[] ops)
			: this((IEnumerable<Op>)ops)
		{
		}

		public Script(IEnumerable<Op> ops)
		{
			MemoryStream ms = new MemoryStream();
			foreach (var op in ops)
			{
				op.WriteTo(ms);
			}
			_Script = ms.ToArray();
		}

		public Script(string script)
		{
			_Script = Parse(script);
		}

		private static byte[] Parse(string script)
		{
			var reader = new StringReader(script.Trim());
			MemoryStream result = new MemoryStream();
			while (reader.Peek() != -1)
			{
				Op.Read(reader).WriteTo(result);
			}
			return result.ToArray();
		}

		public static Script FromBytesUnsafe(byte[] data)
		{
			return new Script(data, true, true);
		}

		public static Script FromHex(string hex)
		{
			return FromBytesUnsafe(Encoders.Hex.DecodeData(hex));
		}

		public Script(byte[] data)
			: this((IEnumerable<byte>)data)
		{
		}


		private Script(byte[] data, bool @unsafe, bool unused)
		{
			_Script = @unsafe ? data : data.ToArray();
		}

		public Script(IEnumerable<byte> data)
		{
			_Script = data.ToArray();
		}

		public Script(byte[] data, bool compressed)
		{
			if (!compressed)
				_Script = data.ToArray();
			else
			{
				ScriptCompressor compressor = new ScriptCompressor();
				compressor.ReadWrite(new BitcoinStream(data));
				_Script = compressor.GetScript()._Script;
			}
		}

		public int Length
		{
			get
			{
				return _Script.Length;
			}
		}

#if HAS_SPAN
		private uint256? _leafHash;
		public uint256 TaprootV1LeafHash
		{
			get
			{
				if (_leafHash is not null)
					return _leafHash;
				_leafHash = TaprootLeafHash((byte)TaprootConstants.TAPROOT_LEAF_TAPSCRIPT);
				return _leafHash;
			}
		}

		internal uint256 TaprootLeafHash(byte version)
		{
				var hash = new HashStream { SingleSHA256 = true };
				hash.InitializeTagged("TapLeaf");
				hash.WriteByte(version);
				var bs = new BitcoinStream(hash, true);
				bs.ReadWrite(this);
				return hash.GetHash();
		}
#endif
		/// <summary>
		/// Extract the ScriptCode delimited by the codeSeparatorIndex th OP_CODESEPARATOR.
		/// </summary>
		/// <param name="codeSeparatorIndex">Index of the OP_CODESEPARATOR, or -1 for fetching the whole script</param>
		/// <returns></returns>
		public Script ExtractScriptCode(int codeSeparatorIndex)
		{
			if (codeSeparatorIndex == -1)
				return this;
			if (codeSeparatorIndex < -1)
				throw new ArgumentOutOfRangeException("codeSeparatorIndex");
			var separatorIndex = -1;
			List<Op> ops = new List<Op>();
			foreach (var op in ToOps())
			{
				if (op.Code == OpcodeType.OP_CODESEPARATOR)
					separatorIndex++;
				if (separatorIndex >= codeSeparatorIndex && !(separatorIndex == codeSeparatorIndex && op.Code == OpcodeType.OP_CODESEPARATOR))
					ops.Add(op);
			}
			if (separatorIndex < codeSeparatorIndex)
				throw new ArgumentOutOfRangeException("codeSeparatorIndex");
			return new Script(ops.ToArray());
		}

		public ScriptReader CreateReader()
		{
			return new ScriptReader(_Script);
		}

		private Script FindAndDelete(Op op)
		{
			return op == null ? this : FindAndDelete(o => o.Code == op.Code && Utils.ArrayEqual(o.PushData, op.PushData));
		}

		internal Script FindAndDelete(byte[] pushedData)
		{
			if (pushedData.Length == 0)
				return this;
			var standardOp = Op.GetPushOp(pushedData);
			return FindAndDelete(op =>
							op.Code == standardOp.Code &&
							op.PushData != null && Utils.ArrayEqual(op.PushData, pushedData));
		}
		internal Script FindAndDelete(OpcodeType op)
		{
			return FindAndDelete(new Op()
			{
				Code = op
			});
		}

		private Script FindAndDelete(Func<Op, bool> predicate)
		{
			int nFound = 0;
			List<Op> operations = new List<Op>();
			foreach (var op in ToOps())
			{
				var shouldDelete = predicate(op);
				if (!shouldDelete)
				{
					operations.Add(op);
				}
				else
					nFound++;
			}
			if (nFound == 0)
				return this;
			return new Script(operations);
		}

		public string ToHex()
		{
			return Encoders.Hex.EncodeData(_Script);
		}

		Script? _PaymentScript;

		/// <summary>
		/// Get the P2SH scriptPubKey of this script
		/// </summary>
		public Script PaymentScript
		{
			get
			{
				return _PaymentScript ?? (_PaymentScript = PayToScriptHashTemplate.Instance.GenerateScriptPubKey(Hash));
			}
		}

		public override string ToString()
		{
			// by default StringBuilder capacity is 16 (too small)
			// 300 is enough for P2PKH
			var builder = new StringBuilder(300);
			var reader = new ScriptReader(_Script);

			Op op;
			while ((op = reader.Read()) != null)
			{
				builder.Append(" ");
				builder.Append(op);
			}

			return builder.ToString().Trim();
		}

		public bool IsPushOnly
		{
			get
			{
				foreach (var script in CreateReader().ToEnumerable())
				{
					if (script.PushData == null)
						return false;
				}
				return true;
			}
		}

		public bool HasCanonicalPushes
		{
			get
			{
				foreach (var op in CreateReader().ToEnumerable())
				{
					if (op.IsInvalid)
						return false;
					if (op.Code > OpcodeType.OP_16)
						continue;
					if (op.Code < OpcodeType.OP_PUSHDATA1 && op.Code > OpcodeType.OP_0 && (op.PushData.Length == 1 && op.PushData[0] <= 16))
						// Could have used an OP_n code, rather than a 1-byte push.
						return false;
					if (op.Code == OpcodeType.OP_PUSHDATA1 && op.PushData.Length < (byte)OpcodeType.OP_PUSHDATA1)
						// Could have used a normal n-byte push, rather than OP_PUSHDATA1.
						return false;
					if (op.Code == OpcodeType.OP_PUSHDATA2 && op.PushData.Length <= 0xFF)
						// Could have used an OP_PUSHDATA1.
						return false;
					if (op.Code == OpcodeType.OP_PUSHDATA4 && op.PushData.Length <= 0xFFFF)
						// Could have used an OP_PUSHDATA2.
						return false;
				}
				return true;
			}
		}

		public static Script operator +(Script? a, IEnumerable<byte> bytes)
		{
			if (a is null)
				return new Script(Op.GetPushOp(bytes.ToArray()));
			return a + Op.GetPushOp(bytes.ToArray());
		}
		public static Script operator +(Script? a, Op op)
		{
			if (op is null)
				throw new ArgumentNullException(nameof(op));
			return a is null ? new Script(op) : new Script(a._Script.Concat(op.ToBytes()));
		}

		public static Script operator +(Script? a, IEnumerable<Op> ops)
		{
			return a is null ? new Script(ops) : new Script(a._Script.Concat(new Script(ops)._Script));
		}

		public IEnumerable<Op> ToOps()
		{
			ScriptReader reader = new ScriptReader(_Script);
			return reader.ToEnumerable();
		}

		public uint GetSigOpCount(bool fAccurate)
		{
			uint n = 0;
			Op? lastOpcode = null;
			foreach (var op in ToOps())
			{
				if (op.Code == OpcodeType.OP_CHECKSIG || op.Code == OpcodeType.OP_CHECKSIGVERIFY)
					n++;
				else if (op.Code == OpcodeType.OP_CHECKMULTISIG || op.Code == OpcodeType.OP_CHECKMULTISIGVERIFY)
				{
					if (fAccurate && lastOpcode != null && lastOpcode.Code >= OpcodeType.OP_1 && lastOpcode.Code <= OpcodeType.OP_16)
						n += (lastOpcode.PushData == null || lastOpcode.PushData.Length == 0) ? 0U : (uint)lastOpcode.PushData[0];
					else
						n += 20;
				}
				lastOpcode = op;
			}
			return n;
		}

		ScriptId? _Hash;
		public ScriptId Hash
		{
			get
			{
				return _Hash ?? (_Hash = new ScriptId(this));
			}
		}
		WitScriptId? _WitHash;
		public WitScriptId WitHash
		{
			get
			{
				return _WitHash ?? (_WitHash = new WitScriptId(this));
			}
		}

		public uint GetSigOpCount(Script scriptSig)
		{
			if (!IsScriptType(ScriptType.P2SH))
				return GetSigOpCount(true);
			// This is a pay-to-script-hash scriptPubKey;
			// get the last item that the scriptSig
			// pushes onto the stack:
			var validSig = new PayToScriptHashTemplate().CheckScriptSig(scriptSig, this);
			return !validSig ? 0 : new Script(scriptSig.ToOps().Last().PushData).GetSigOpCount(true);
			// ... and return its opcount:
		}

		public ScriptTemplate? FindTemplate()
		{
			return StandardScripts.GetTemplateFromScriptPubKey(this);
		}

		public bool IsMalleable
		{
			get
			{
				return !IsScriptType(ScriptType.Witness);
			}
		}
		public bool IsScriptType(ScriptType type)
		{
			switch (type)
			{
				case ScriptType.Witness:
					return PayToWitTemplate.Instance.CheckScriptPubKey(this);
				case ScriptType.P2PKH:
					return PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(this);
				case ScriptType.P2SH:
					return PayToScriptHashTemplate.Instance.CheckScriptPubKey(this);
				case ScriptType.P2PK:
					return PayToPubkeyTemplate.Instance.CheckScriptPubKey(this);
				case ScriptType.P2WPKH:
					return PayToWitPubKeyHashTemplate.Instance.CheckScriptPubKey(this);
				case ScriptType.P2WSH:
					return PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(this);
				case ScriptType.MultiSig:
					return PayToMultiSigTemplate.Instance.CheckScriptPubKey(this);
				case ScriptType.Taproot:
					return PayToTaprootTemplate.Instance.CheckScriptPubKey(this);
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, "The value is not a valid script type");
			}
		}

		/// <summary>
		/// Extract P2SH or P2PKH address from scriptSig
		/// </summary>
		/// <param name="network">The network</param>
		/// <returns></returns>
		[Obsolete("Do not use this, it isn't possible to get a signer's address from a script without taking heuristic which can be gamed by a malicious actor")]
		public BitcoinAddress? GetSignerAddress(Network network)
		{
			var sig = GetSigner();
			return sig == null ? null : sig.GetAddress(network);
		}

		/// <summary>
		/// Extract P2SH or P2PKH id from scriptSig
		/// </summary>
		/// <returns>The network</returns>
		[Obsolete("Do not use this, it isn't possible to get a signer's address from a script without taking heuristic which can be gamed by a malicious actor")]
		public IAddressableDestination? GetSigner()
		{
			var pubKey = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(this);
			if (pubKey != null)
			{
				return pubKey.PublicKey.Hash;
			}
			var p2sh = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(this);
			return p2sh != null ? p2sh.RedeemScript.Hash : null;
		}

		/// <summary>
		/// Extract P2SH/P2PKH/P2WSH/P2WPKH/P2TR address from scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public BitcoinAddress? GetDestinationAddress(Network network)
		{
			var dest = GetDestination();
			return dest == null ? null : dest.GetAddress(network);
		}

		/// <summary>
		/// Extract P2SH/P2PKH/P2WSH/P2WPKH/P2TR id from scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public IAddressableDestination? GetDestination()
		{
			var pubKeyHashParams = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if (pubKeyHashParams != null)
				return pubKeyHashParams;
			var scriptHashParams = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if (scriptHashParams != null)
				return scriptHashParams;
			return PayToWitTemplate.Instance.ExtractScriptPubKeyParameters(this);
		}

		/// <summary>
		/// Extract public keys if this script is a multi sig or pay to pub key scriptPubKey
		/// </summary>
		/// <param name="network"></param>
		/// <returns></returns>
		public PubKey[] GetDestinationPublicKeys()
		{
			List<PubKey> result = new List<PubKey>();
			var single = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(this);
			if (single != null)
			{
				result.Add(single);
			}
			else
			{
				var multiSig = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(this);
				if (multiSig != null)
				{
					result.AddRange(multiSig.PubKeys);
				}
			}
			return result.ToArray();
		}

		public PubKey[] GetAllPubKeys() =>
			ToOps().Where(op => op.PushData != null).Select(op =>
			{
				PubKey.TryCreatePubKey(op.PushData, out var pk);
				return pk;
			}).Where(pk => pk != null).ToArray()!;

		/// <summary>
		/// Get script byte array
		/// </summary>
		/// <returns></returns>
		public byte[] ToBytes()
		{
			return ToBytes(false);
		}

		/// <summary>
		/// Get script byte array
		/// </summary>
		/// <param name="unsafe">if false, returns a copy of the internal byte array</param>
		/// <returns></returns>
		public byte[] ToBytes(bool @unsafe)
		{
			return @unsafe ? _Script : _Script.ToArray();
		}

		public WitScript ToWitScript() => new WitScript(this);

		public byte[] ToCompressedBytes()
		{
			var compressor = new ScriptCompressor(this);
			return compressor.ToBytes();
		}


#if !NOCONSENSUSLIB

		public enum BitcoinConsensusError
		{
			ERR_OK = 0,
			ERR_TX_INDEX,
			ERR_TX_SIZE_MISMATCH,
			ERR_TX_DESERIALIZE,
			ERR_AMOUNT_REQUIRED
		}

		/// Returns 1 if the input nIn of the serialized transaction pointed to by
		/// txTo correctly spends the scriptPubKey pointed to by scriptPubKey under
		/// the additional constraints specified by flags.
		/// If not NULL, err will contain an error/success code for the operation
		[DefaultDllImportSearchPathsAttribute(DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.AssemblyDirectory)]
		[DllImport("libbitcoinconsensus", EntryPoint = "bitcoinconsensus_verify_script", CallingConvention = CallingConvention.Cdecl)]
		private static extern int VerifyScriptConsensus(byte[] scriptPubKey, uint scriptPubKeyLen, byte[] txTo, uint txToLen, uint nIn, ScriptVerify flags, ref BitcoinConsensusError err);

		[DefaultDllImportSearchPathsAttribute(DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.AssemblyDirectory)]
		[DllImport("libbitcoinconsensus", EntryPoint = "bitcoinconsensus_verify_script_with_amount", CallingConvention = CallingConvention.Cdecl)]
		static extern int VerifyScriptConsensusWithAmount(byte[] scriptPubKey, uint scriptPubKeyLen, long amount, byte[] txTo, uint txToLen, uint nIn, ScriptVerify flags, ref BitcoinConsensusError err);

		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, ScriptVerify flags)
		{
			var err = BitcoinConsensusError.ERR_OK;
			return VerifyScriptConsensus(scriptPubKey, tx, nIn, flags, out err);
		}
		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, Money amount, ScriptVerify flags)
		{
			var err = BitcoinConsensusError.ERR_OK;
			return VerifyScriptConsensus(scriptPubKey, tx, nIn, amount, flags, out err);
		}

		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, ScriptVerify flags, out BitcoinConsensusError err)
		{
			var scriptPubKeyBytes = scriptPubKey.ToBytes();
			var txToBytes = tx.ToBytes();
			err = BitcoinConsensusError.ERR_OK;
			var valid = VerifyScriptConsensus(scriptPubKeyBytes, (uint)scriptPubKeyBytes.Length, txToBytes, (uint)txToBytes.Length, nIn, flags, ref err);
			return valid == 1;
		}

		public static bool VerifyScriptConsensus(Script scriptPubKey, Transaction tx, uint nIn, Money amount, ScriptVerify flags, out BitcoinConsensusError err)
		{
			var scriptPubKeyBytes = scriptPubKey.ToBytes();
			var txToBytes = tx.ToBytes();
			err = BitcoinConsensusError.ERR_OK;

			int valid = VerifyScriptConsensusWithAmount(scriptPubKeyBytes, (uint)scriptPubKeyBytes.Length, amount.Satoshi, txToBytes, (uint)txToBytes.Length, 0, flags & ScriptVerify.Consensus, ref err);
			return valid == 1;
		}
#endif

		public bool IsUnspendable
		{
			get
			{
				return _Script.Length > 0 && _Script[0] == (byte)OpcodeType.OP_RETURN;
			}
		}

		public static bool IsNullOrEmpty([NotNullWhen(false)] Script? script)
		{
			return script is null || script._Script.Length == 0;
		}

		public override bool Equals(object? obj)
		{
			Script? item = obj as Script;
			return Equals(item);
		}

		public bool Equals(Script? item) => item is not null && Utils.ArrayEqual(item._Script, _Script);
		public static bool operator ==(Script? a, Script? b)
		{
			if (a is null && b is null)
				return true;
			if (a is null)
				return false;
			if (b is null)
				return false;
			return Utils.ArrayEqual(a._Script, b._Script);
		}

		public static bool operator !=(Script? a, Script? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Utils.GetHashCode(_Script);
		}

		public Script Clone()
		{
			return new Script(_Script);
		}

		public static Script CombineSignatures(Script scriptPubKey, Transaction transaction, int n, Script scriptSig1, Script scriptSig2)
		{
			return CombineSignatures(scriptPubKey, new TransactionChecker(transaction, n), new ScriptSigs()
			{
				ScriptSig = scriptSig1,
			}, new ScriptSigs()
			{
				ScriptSig = scriptSig2,
			}).ScriptSig;
		}
		public static ScriptSigs CombineSignatures(Script scriptPubKey, TransactionChecker checker, ScriptSigs input1, ScriptSigs input2)
		{
			if (scriptPubKey == null)
				scriptPubKey = new Script();

			var scriptSig1 = input1.ScriptSig;
			var scriptSig2 = input2.ScriptSig;
			HashVersion hashVersion = HashVersion.Original;
			var isWitness = input1.WitSig != WitScript.Empty || input2.WitSig != WitScript.Empty;
			if (isWitness)
			{
				scriptSig1 = input1.WitSig.ToScript();
				scriptSig2 = input2.WitSig.ToScript();
				hashVersion = HashVersion.WitnessV0;
			}

			var context = new ScriptEvaluationContext();
			context.ScriptVerify = ScriptVerify.StrictEnc;
			context.EvalScript(scriptSig1, checker, hashVersion);

			var stack1 = context.Stack.AsInternalArray();
			context = new ScriptEvaluationContext();
			context.ScriptVerify = ScriptVerify.StrictEnc;
			context.EvalScript(scriptSig2, checker, hashVersion);

			var stack2 = context.Stack.AsInternalArray();
			var result = CombineSignatures(scriptPubKey, checker, stack1, stack2, hashVersion);
			if (result == null)
				return scriptSig1.Length < scriptSig2.Length ? input2 : input1;
			if (!isWitness)
				return new ScriptSigs()
				{
					ScriptSig = result,
					WitSig = WitScript.Empty
				};
			else
			{
				return new ScriptSigs()
				{
					ScriptSig = input1.ScriptSig.Length < input2.ScriptSig.Length ? input2.ScriptSig : input1.ScriptSig,
					WitSig = new WitScript(result)
				};
			}
		}

		private static Script? CombineSignatures(Script scriptPubKey, TransactionChecker checker, byte[][] sigs1, byte[][] sigs2, HashVersion hashVersion)
		{
			var template = StandardScripts.GetTemplateFromScriptPubKey(scriptPubKey);

			if (template is PayToWitPubKeyHashTemplate)
			{
				scriptPubKey = new KeyId(scriptPubKey.ToBytes(true).SafeSubarray(1, 20)).ScriptPubKey;
				template = StandardScripts.GetTemplateFromScriptPubKey(scriptPubKey);
			}
			if (template == null || template is TxNullDataTemplate)
				return PushAll(Max(sigs1, sigs2));

			if (template is PayToPubkeyTemplate || template is PayToPubkeyHashTemplate)
				if (sigs1.Length == 0 || sigs1[0].Length == 0)
					return PushAll(sigs2);
				else
					return PushAll(sigs1);
			if (template is PayToScriptHashTemplate || template is PayToWitTemplate)
			{
				if (sigs1.Length == 0 || sigs1[sigs1.Length - 1].Length == 0)
					return PushAll(sigs2);

				if (sigs2.Length == 0 || sigs2[sigs2.Length - 1].Length == 0)
					return PushAll(sigs1);

				var redeemBytes = sigs1[sigs1.Length - 1];
				var redeem = new Script(redeemBytes);
				sigs1 = sigs1.Take(sigs1.Length - 1).ToArray();
				sigs2 = sigs2.Take(sigs2.Length - 1).ToArray();
				var result = CombineSignatures(redeem, checker, sigs1, sigs2, hashVersion);
				result += Op.GetPushOp(redeemBytes);
				return result;
			}

			if (template is PayToMultiSigTemplate)
			{
				return CombineMultisig(scriptPubKey, checker, sigs1, sigs2, hashVersion);
			}

			return null;
		}

		private static Script CombineMultisig(Script scriptPubKey, TransactionChecker checker, byte[][] sigs1, byte[][] sigs2, HashVersion hashVersion)
		{
			// Combine all the signatures we've got:
			List<TransactionSignature> allsigs = new List<TransactionSignature>();
			foreach (var v in sigs1)
			{
				if (TransactionSignature.IsValid(v))
				{
					allsigs.Add(new TransactionSignature(v));
				}
			}


			foreach (var v in sigs2)
			{
				if (TransactionSignature.IsValid(v))
				{
					allsigs.Add(new TransactionSignature(v));
				}
			}

			var multiSigParams = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey);
			if (multiSigParams == null)
				throw new InvalidOperationException("The scriptPubKey is not a valid multi sig");

			Dictionary<PubKey, TransactionSignature> sigs = new Dictionary<PubKey, TransactionSignature>();

			foreach (var sig in allsigs)
			{
				foreach (var pubkey in multiSigParams.PubKeys)
				{
					if (sigs.ContainsKey(pubkey))
						continue; // Already got a sig for this pubkey

					uint256 sighash = checker.Transaction.GetSignatureHash(scriptPubKey, checker.Index, sig.SigHash, checker.SpentOutput, hashVersion, checker.PrecomputedTransactionData);
					if (pubkey.Verify(sighash, sig.Signature))
					{
						sigs.AddOrReplace(pubkey, sig);
					}
				}
			}


			// Now build a merged CScript:
			int nSigsHave = 0;
			Script result = new Script(OpcodeType.OP_0); // pop-one-too-many workaround
			foreach (var pubkey in multiSigParams.PubKeys)
			{
				if (sigs.ContainsKey(pubkey))
				{
					result += Op.GetPushOp(sigs[pubkey].ToBytes());
					nSigsHave++;
				}
				if (nSigsHave >= multiSigParams.SignatureCount)
					break;
			}

			// Fill any missing with OP_0:
			for (int i = nSigsHave; i < multiSigParams.SignatureCount; i++)
				result += OpcodeType.OP_0;

			return result;
		}

		private static Script PushAll(byte[][] stack)
		{
			Script s = new Script();
			foreach (var push in stack)
			{
				s += Op.GetPushOp(push);
			}
			return s;
		}

		[return: NotNullIfNotNull("script")]
		public static implicit operator WitScript?(Script? script)
		{
			if (script is null)
				return null;
			return new WitScript(script);
		}

		private static byte[][] Max(byte[][] scriptSig1, byte[][] scriptSig2)
		{
			return scriptSig1.Length >= scriptSig2.Length ? scriptSig1 : scriptSig2;
		}

		public bool IsValid
		{
			get
			{
				return ToOps().All(o => !o.IsInvalid);
			}
		}
	}
}
