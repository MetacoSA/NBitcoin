using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Miniscript
{
	public enum SatisfyErrorCode
	{

		NoSignatureProvider,
		NoPreimageProvider,
		NoAgeProvided,
		CanNotProvideSignature,
		CanNotProvideEnoughSignatureForMulti,
		CanNotProvidePreimage,
		LockTimeNotMet,
		ThresholdNotMet,
		OrExpressionBothNotMet
	}
	/// <summary>
	/// Represents the error that occurs during satisfying Miniscript AST.
	/// </summary>
	public class SatisfyException : Exception
	{
		public SatisfyException(IEnumerable<SatisfyError> errors) : base(ToMessage(errors))
		{
			ErrorCodes = errors.ToList();
		}

		public IReadOnlyList<SatisfyError> ErrorCodes { get; }
		private static string ToMessage(IEnumerable<SatisfyError> errors)
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));
			return String.Join(Environment.NewLine, errors.Select(e => e.ToString()).ToArray());
		}
	}

	public class SatisfyError
	{
		public SatisfyError(SatisfyErrorCode code, AstElem ast, IEnumerable<SatisfyError> inner = null)
		{
			Code = code;
			Ast = ast ?? throw new ArgumentNullException(nameof(ast));
			Inner = inner;
		}

		public override string ToString() =>
			Inner == null ?
				$"Failed to satisfy AST: {Ast}. Code: {Code}" :
				$"Failed to satisfy AST: {Ast}. Code: {Code} \n InnerErrors: " + String.Join(Environment.NewLine, Inner.Select(i => i.ToString()));

		public IEnumerable<SatisfyError> Inner { get; }
		public AstElem Ast { get; }

		public SatisfyErrorCode Code { get; }
	}

}