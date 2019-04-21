namespace NBitcoin.Miniscript
{
	internal class ParserResult<TIn, TValue>
	{
		public readonly TValue Value;
		public readonly TIn Rest;

		public ParserResult(TValue value, TIn rest)
		{
			Value = value;
			Rest = rest;
		}
	}

	internal delegate ParserResult<TIn, TValue> Parser<TIn, TValue>(TIn input);
}