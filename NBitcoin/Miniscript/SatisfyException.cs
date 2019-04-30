using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Miniscript
{
	public enum SatisfyErrorCode
	{
		LockTimeNotMet
	}
	/// <summary>
	/// Represents the error that occurs during satisfying Miniscript AST.
	/// </summary>
	public class SatisfyException : Exception
	{
		public SatisfyException(IEnumerable<SatisfyError> errors) : base(ToMessage(errors))
		{
			ErrorCodes = ErrorCodes.ToList();
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
		public SatisfyError(SatisfyErrorCode code, AstElem ast)
		{
			Code = code;
			Ast = ast ?? throw new ArgumentNullException(nameof(ast));
		}

		public override string ToString() => $"Failed to satisfy AST: {Ast}. Code: {Code}";

		public AstElem Ast { get; }

		public SatisfyErrorCode Code { get; }
	}

}