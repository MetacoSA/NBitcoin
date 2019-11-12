using System;

namespace NBitcoin.Scripting.Miniscript
{
	public class MiniscriptException : Exception
	{
		internal static class Tags
		{

			internal const int InvalidOpcode = 0;
			internal const int NonMinimalVerify = 1;
			internal const int InvalidPush = 2;
			internal const int PSBT = 3;
			internal const int Script = 4;
			internal const int CmsTooManyKeys = 5;
			internal const int Unprintable = 6;
			internal const int ExpectedChar = 7;
			internal const int UnexpectedStart = 8;
			internal const int Unexpected = 9;
			internal const int MultiColon = 10;
			internal const int MultiAt = 11;
			internal const int AtOutsideOr = 12;
			internal const int NonCanonicalTrue = 13;
			internal const int NonCanonicalFalse = 14;
			internal const int LikelyFalse = 15;
			internal const int UnknownWrapper = 16;
			internal const int NonTopLevel = 17;
			internal const int Trailing = 18;
			internal const int BadPubKey = 19;
			internal const int MissingHash = 20;
			internal const int MissingSig = 21;
			internal const int RelativeLocktimeNotMet = 22;
			internal const int AbsoluteLockTimeNotMet = 23;
			internal const int CouldNotSatisfy = 24;
			internal const int TypeCheck = 25;
			internal const int BadDescriptor = 26;
			internal const int CompileError = 27;
			internal const int InterpreterError = 28;
			internal const int BadScriptSig = 29;
			internal const int NonEmptyWitness = 30;
			internal const int NonEmptyScriptSig = 31;
			internal const int IncorrectPubkeyHash = 32;
			internal const int IncorrectScriptHash = 33;
		}

		internal int Tag;

		/// <summary>
		/// While parsing backward, hit beginning of the script.
		/// </summary>
		public static MiniscriptException UnexpectedStart = new MiniscriptException(Tags.UnexpectedStart);

		/// <summary>
		/// Fragment was an `and_v(_, true)` which should be written as `t:`
		/// </summary>
		public static MiniscriptException NonCanonicalTrue = new MiniscriptException(Tags.NonCanonicalTrue);

		/// <summary>
		/// NFragment was an `or_i(_, false)` or `or_i(false, _)` which should be written as `u:` or `l:`
		/// </summary>
		public static MiniscriptException NonCanonicalFalse = new MiniscriptException(Tags.NonCanonicalFalse);

		/// <summary>
		/// Encountered a `l:0` which is syntactically equal to `u:0` except stupid
		/// </summary>
		public static MiniscriptException LikelyFalse = new MiniscriptException(Tags.LikelyFalse);

		/// <summary>
		/// General failure to satisfy
		/// </summary>
		public static MiniscriptException CouldNotSatisfy = new MiniscriptException(Tags.CouldNotSatisfy);

		/// <summary>
		/// General error in creating descriptor
		/// </summary>
		public static MiniscriptException BadDescriptor = new MiniscriptException(Tags.BadDescriptor);

		/// <summary>
		/// Bad Script Sig. As per standardness rules, only pushes are allowed in scriptSig.
		/// This error is invoked when opc_codes are pushed onto the stack As per the current
		/// implementation, pushing an integer apart from 0 or 1will also trigger this.
		/// This is because, Miniscript only expects push bytes for pk, sig, preimage etc or 1 or 0
		/// for `StackElement:Satisfied` or `StackElement::Dissatisfied`
		/// </summary>
		public static MiniscriptException BadScriptSig = new MiniscriptException(Tags.BadScriptSig);

		/// <summary>
		/// Witness must be empty for pre-segwit transactions
		/// </summary>
		public static MiniscriptException NonEmptyWitness = new MiniscriptException(Tags.NonEmptyWitness);

		/// <summary>
		/// ScriptSig must be empty for pure segwit transactions.
		/// </summary>
		public static MiniscriptException NonEmptyScriptSig = new MiniscriptException(Tags.NonEmptyScriptSig);

		/// <summary>
		/// Incorrect Script Pubkey Hash for the descriptor. This is used for both
		/// `PkH` and `Wpkh` descriptors.
		/// </summary>
		public static MiniscriptException IncorrectPubkeyHash = new MiniscriptException(Tags.IncorrectPubkeyHash);

		/// <summary>
		/// Incorrect Script pubkey Hash for the descriptor. This is used for both
		/// `Sh` and `Wsh` descriptors.
		/// </summary>
		public static MiniscriptException IncorrectScriptHash = new MiniscriptException(Tags.IncorrectScriptHash);

		private MiniscriptException(int tag) => Tag = tag;

		/// <summary>
		/// Opcode appeared which is not part of the script subset.
		/// </summary>
		internal class InvalidOpcode : MiniscriptException
		{
			public OpcodeType Item;
			public InvalidOpcode(OpcodeType item) : base(Tags.InvalidOpcode) => Item = item;
		}

		/// <summary>
		/// Some opcode occured followed by `OP_VERIFY` when it had
		/// a `VERIFY` version that should have been used instead.
		/// </summary>
		internal class NonMinimalVerify : MiniscriptException
		{
			public ScriptToken Item;
			public NonMinimalVerify(ScriptToken item) : base(Tags.NonMinimalVerify) => Item = item;
		}

		/// <summary>
		/// Push was illegal in some context
		/// </summary>
		internal class InvalidPush : MiniscriptException
		{
			public byte[] Item;
			public InvalidPush(byte[] item) : base(Tags.InvalidPush) => Item = item;
		}

		/// <summary>
		///  PSBT related error
		/// </summary>
		internal class PSBT : MiniscriptException
		{
			public PSBTError Item;
			public PSBT(PSBTError item) : base(Tags.PSBT) => Item = item;
		}

		internal class Script : MiniscriptException
		{
			public ScriptError Item;
			public Script(ScriptError item) : base(Tags.Script) => Item = item;
		}

		internal class CmsTooManyKeys : MiniscriptException
		{
			public uint Item;

			public CmsTooManyKeys(uint item) : base(Tags.CmsTooManyKeys) => Item = item;
		}

		internal class Unprintable : MiniscriptException
		{
			public byte Item;

			public Unprintable(byte item) : base (Tags.Unprintable) => Item = item;
		}

		internal class ExpectedChar : MiniscriptException
		{
			public char Item;

			public ExpectedChar(char item) : base(Tags.ExpectedChar) => Item = item;
		}

		internal class Unexpected : MiniscriptException
		{
			public string Item;

			public Unexpected(string item) : base(Tags.Unexpected) => Item = item;
		}

		internal class MultiColon : MiniscriptException
		{
			public string Item;

			public MultiColon(string item) : base(Tags.MultiColon) => Item = item;
		}

		internal class MultiAt : MiniscriptException
		{
			public string Item;
			internal MultiAt(string item) : base(Tags.MultiAt) => Item = item;
		}

		internal class AtOutsideOr : MiniscriptException
		{
			public string Item;
			internal AtOutsideOr(string item) : base(Tags.AtOutsideOr) => Item = item;
		}

		internal class UnknownWrapper : MiniscriptException
		{
			public char Item;
			public UnknownWrapper(char item) : base(Tags.UnknownWrapper) => Item = item;
		}

		internal class NonTopLevel : MiniscriptException
		{
			public string Item;
			public NonTopLevel(string item) : base(Tags.NonTopLevel) => Item = item;
		}

		internal class Trailing : MiniscriptException
		{
			public string Item;
			public Trailing(string item) : base(Tags.Trailing) => Item = item;
		}

		internal class BadPubKey : MiniscriptException
		{
			public FormatException Item;
			public BadPubKey(FormatException item) : base(Tags.BadPubKey) => Item = item;
		}

		internal class MissingHash : MiniscriptException
		{
			public uint256 Item;

			public MissingHash(uint256 item) : base(Tags.MissingHash) => Item = item;
		}

		internal class MissingSig : MiniscriptException
		{
			public PubKey Item;

			public MissingSig(PubKey item) : base(Tags.MissingSig) => Item = item;
		}

		internal class RelativeLockTimeNotMet : MiniscriptException
		{
			public uint Item;
			public RelativeLockTimeNotMet(uint item) : base(Tags.RelativeLocktimeNotMet) => Item = item;
		}

		internal class AbsoluteLockTimeNotMet : MiniscriptException
		{
			public uint Item;
			public AbsoluteLockTimeNotMet(uint item) : base(Tags.AbsoluteLockTimeNotMet) => Item = item;
		}

		internal class TypeCheck : MiniscriptException
		{
			public string Item;
			public TypeCheck(string item) : base(Tags.TypeCheck) => Item = item;
		}

		internal class CompileError : MiniscriptException
		{
			public Policy.CompilerException Item;

			public CompileError(Policy.CompilerException item) : base (Tags.CompileError) => Item = item;
		}

		internal class InterpreterError : MiniscriptException
		{
			public InterpreterError Item;

			public InterpreterError(InterpreterError item) : base(Tags.InterpreterError) => Item = item;
		}
	}
}
