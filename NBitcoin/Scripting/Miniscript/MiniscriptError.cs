using System;

namespace NBitcoin.Scripting.Miniscript
{
	public class MiniscriptError : Exception
	{
		public class ErrorKind
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
			public static ErrorKind UnexpectedStart = new ErrorKind(Tags.UnexpectedStart);

			/// <summary>
			/// Fragment was an `and_v(_, true)` which should be written as `t:`
			/// </summary>
			public static ErrorKind NonCanonicalTrue = new ErrorKind(Tags.NonCanonicalTrue);

			/// <summary>
			/// NFragment was an `or_i(_, false)` or `or_i(false, _)` which should be written as `u:` or `l:`
			/// </summary>
			public static ErrorKind NonCanonicalFalse = new ErrorKind(Tags.NonCanonicalFalse);

			/// <summary>
			/// Encountered a `l:0` which is syntactically equal to `u:0` except stupid
			/// </summary>
			public static ErrorKind LikelyFalse = new ErrorKind(Tags.LikelyFalse);

			/// <summary>
			/// General failure to satisfy
			/// </summary>
			public static ErrorKind CouldNotSatisfy = new ErrorKind(Tags.CouldNotSatisfy);

			/// <summary>
			/// General error in creating descriptor
			/// </summary>
			public static ErrorKind BadDescriptor = new ErrorKind(Tags.BadDescriptor);

			/// <summary>
			/// Bad Script Sig. As per standardness rules, only pushes are allowed in scriptSig.
			/// This error is invoked when opc_codes are pushed onto the stack As per the current
			/// implementation, pushing an integer apart from 0 or 1will also trigger this.
			/// This is because, Miniscript only expects push bytes for pk, sig, preimage etc or 1 or 0
			/// for `StackElement:Satisfied` or `StackElement::Dissatisfied`
			/// </summary>
			public static ErrorKind BadScriptSig = new ErrorKind(Tags.BadScriptSig);

			/// <summary>
			/// Witness must be empty for pre-segwit transactions
			/// </summary>
			public static ErrorKind NonEmptyWitness = new ErrorKind(Tags.NonEmptyWitness);

			/// <summary>
			/// ScriptSig must be empty for pure segwit transactions.
			/// </summary>
			public static ErrorKind NonEmptyScriptSig = new ErrorKind(Tags.NonEmptyScriptSig);

			/// <summary>
			/// Incorrect Script Pubkey Hash for the descriptor. This is used for both
			/// `PkH` and `Wpkh` descriptors.
			/// </summary>
			public static ErrorKind IncorrectPubkeyHash = new ErrorKind(Tags.IncorrectPubkeyHash);

			/// <summary>
			/// Incorrect Script pubkey Hash for the descriptor. This is used for both
			/// `Sh` and `Wsh` descriptors.
			/// </summary>
			public static ErrorKind IncorrectScriptHash = new ErrorKind(Tags.IncorrectScriptHash);

			private ErrorKind(int tag) => Tag = tag;

			/// <summary>
			/// Opcode appeared which is not part of the script subset.
			/// </summary>
			internal class InvalidOpcode : ErrorKind
			{
				public OpcodeType Item;
				public InvalidOpcode(OpcodeType item) : base(Tags.InvalidOpcode) => Item = item;
			}

			/// <summary>
			/// Some opcode occured followed by `OP_VERIFY` when it had
			/// a `VERIFY` version that should have been used instead.
			/// </summary>
			internal class NonMinimalVerify : ErrorKind
			{
				public ScriptToken Item;
				public NonMinimalVerify(ScriptToken item) : base(Tags.NonMinimalVerify) => Item = item;
			}

			/// <summary>
			/// Push was illegal in some context
			/// </summary>
			internal class InvalidPush : ErrorKind
			{
				public byte[] Item;
				public InvalidPush(byte[] item) : base(Tags.InvalidPush) => Item = item;
			}

			/// <summary>
			///  PSBT related error
			/// </summary>
			internal class PSBT : ErrorKind
			{
				public PSBTError Item;
				public PSBT(PSBTError item) : base(Tags.PSBT) => Item = item;
			}

			internal class Script : ErrorKind
			{
				public ScriptError Item;
				public Script(ScriptError item) : base(Tags.Script) => Item = item;
			}

			internal class CmsTooManyKeys : ErrorKind
			{
				public uint Item;

				public CmsTooManyKeys(uint item) : base(Tags.CmsTooManyKeys) => Item = item;
			}

			internal class Unprintable : ErrorKind
			{
				public byte Item;

				public Unprintable(byte item) : base (Tags.Unprintable) => Item = item;
			}

			internal class ExpectedChar : ErrorKind
			{
				public char Item;

				public ExpectedChar(char item) : base(Tags.ExpectedChar) => Item = item;
			}

			internal class Unexpected : ErrorKind
			{
				public string Item;

				public Unexpected(string item) : base(Tags.Unexpected) => Item = item;
			}

			internal class MultiColon : ErrorKind
			{
				public string Item;

				public MultiColon(string item) : base(Tags.MultiColon) => Item = item;
			}

			internal class MultiAt : ErrorKind
			{
				public string Item;
				internal MultiAt(string item) : base(Tags.MultiAt) => Item = item;
			}

			internal class AtOutsideOr : ErrorKind
			{
				public string Item;
				internal AtOutsideOr(string item) : base(Tags.AtOutsideOr) => Item = item;
			}

			internal class UnknownWrapper : ErrorKind
			{
				public char Item;
				public UnknownWrapper(char item) : base(Tags.UnknownWrapper) => Item = item;
			}

			internal class NonTopLevel : ErrorKind
			{
				public string Item;
				public NonTopLevel(string item) : base(Tags.NonTopLevel) => Item = item;
			}

			internal class Trailing : ErrorKind
			{
				public string Item;
				public Trailing(string item) : base(Tags.Trailing) => Item = item;
			}

			internal class BadPubKey : ErrorKind
			{
				public FormatException Item;
				public BadPubKey(FormatException item) : base(Tags.BadPubKey) => Item = item;
			}

			internal class MissingHash : ErrorKind
			{
				public uint256 Item;

				public MissingHash(uint256 item) : base(Tags.MissingHash) => Item = item;
			}

			internal class MissingSig : ErrorKind
			{
				public PubKey Item;

				public MissingSig(PubKey item) : base(Tags.MissingSig) => Item = item;
			}

			internal class RelativeLockTimeNotMet : ErrorKind
			{
				public uint Item;
				public RelativeLockTimeNotMet(uint item) : base(Tags.RelativeLocktimeNotMet) => Item = item;
			}

			internal class AbsoluteLockTimeNotMet : ErrorKind
			{
				public uint Item;
				public AbsoluteLockTimeNotMet(uint item) : base(Tags.AbsoluteLockTimeNotMet) => Item = item;
			}

			internal class TypeCheck : ErrorKind
			{
				public string Item;
				public TypeCheck(string item) : base(Tags.TypeCheck) => Item = item;
			}

			internal class CompileError : ErrorKind
			{
				public Policy.CompilerException Item;

				public CompileError(Policy.CompilerException item) : base (Tags.CompileError) => Item = item;
			}

			internal class InterpreterError : ErrorKind
			{
				public InterpreterError Item;

				public InterpreterError(InterpreterError item) : base(Tags.InterpreterError) => Item = item;
			}

		}

	}
}