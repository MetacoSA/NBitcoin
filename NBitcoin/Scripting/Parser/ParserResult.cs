using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Scripting.Parser
{
	internal class ParserResult<TToken, TValue>
	{
		public readonly TValue Value;
		public readonly IInput<TToken> Rest;

		private ParserResult(IInput<TToken> rest, TValue value)
		{
			Rest = rest;
			Value = value;
		}

		public static ParserResult<TToken, TValue> Success(IInput<TToken> rest, TValue v)
			=> new ParserResult<TToken, TValue>(rest, v) { IsSuccess = true };

		public static ParserResult<TToken, TValue> Failure(IInput<TToken> rest, string description) =>
			Failure(rest, new string[] { }, description);
		public static ParserResult<TToken, TValue> Failure(IInput<TToken> rest, IEnumerable<string> expected, string description) =>
			new ParserResult<TToken, TValue>(rest, default(TValue))
			{
				IsSuccess = false,
				Description = description,
				Expected = expected
			};

		public ParserResult<TToken, U> IfSuccess<U>(Func<ParserResult<TToken, TValue>, ParserResult<TToken, U>> next)
		{
			if (next == null)
				throw new ArgumentNullException(nameof(next));

			if (this.IsSuccess)
				return next(this);

			return ParserResult<TToken, U>.Failure(this.Rest, this.Expected, this.Description);
		}

		public ParserResult<TToken, TValue> IfFailure<U>(Func<ParserResult<TToken, TValue>, ParserResult<TToken, TValue>> next)
		{
			if (next == null)
				throw new ArgumentNullException(nameof(next));

			return this.IsSuccess ? this : next(this);
		}
		public IEnumerable<string> Expected { get; private set; } = null;
		public string Description { get; private set; }

		public bool IsSuccess { get; private set; }

		public override string ToString()
		{
			if (IsSuccess)
				return string.Format("Successful parsing of {0}.", Value);

			var expMsg = "";

			if (Expected.Any())
				expMsg = " expected " + Expected.Aggregate((e1, e2) => e1 + " or " + e2);

			return string.Format("Parsing failure: {0};{1} ({2});", Description, expMsg, Rest);
		}
	}
}