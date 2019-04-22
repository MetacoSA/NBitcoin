using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Miniscript.Parser
{
	internal class ParserResult<TValue>
	{
		public readonly TValue Value;
		public readonly IInput Rest;

		private ParserResult(IInput rest, TValue value)
		{
			Rest = rest;
			Value = value;
		}

		public static ParserResult<TValue> Success(IInput rest, TValue v)
			=> new ParserResult<TValue>(rest, v) { IsSuccess = true };

		public static ParserResult<TValue> Failure(IInput rest, string description) =>
			Failure(rest, null, description);
		public static ParserResult<TValue> Failure(IInput rest, IEnumerable<string> expected, string description) =>
			new ParserResult<TValue>(rest, default(TValue))
			{
				IsSuccess = false,
				Description = description,
				Expected = expected
			};

		public ParserResult<U> IfSuccess<U>(Func<ParserResult<TValue>, ParserResult<U>> next)
		{
			if (next == null)
				throw new ArgumentNullException(nameof(next));

			if (this.IsSuccess)
				return next(this);

			return ParserResult<U>.Failure(this.Rest, this.Expected, this.Description);
		}

		public ParserResult<TValue> IfFailure<U>(Func<ParserResult<TValue>, ParserResult<TValue>> next)
		{
			if (next == null)
				throw new ArgumentNullException(nameof(next));

			return this.IsSuccess ? this : next(this);
		}
		public IEnumerable<string> Expected { get; private set; } = null;
		public string Description { get; private set; }

		public bool IsSuccess { get;  private set; }

		public override string ToString()
		{
			if (IsSuccess)
				return string.Format("Successful parsing of {0}.", Value);

			var expMsg = "";

			if (Expected.Any())
				expMsg = " expected " + Expected.Aggregate((e1, e2) => e1 + " or " + e2);

			var recentlyConsumed = CalculateRecentlyConsumed();

			return string.Format("Parsing failure: {0};{1} ({2}); recently consumed: {3}", Description, expMsg, Rest, recentlyConsumed);
		}

		private string CalculateRecentlyConsumed()
		{
			const int windowSize = 10;

			var totalConsumedChars = Rest.Position;
			var windowStart = totalConsumedChars - windowSize;
			windowStart = windowStart < 0 ? 0 : windowStart;

			var numberOfRecentlyConsumedChars = totalConsumedChars - windowStart;

			return Rest.Source.Substring(windowStart, numberOfRecentlyConsumedChars);
		}
	}
}