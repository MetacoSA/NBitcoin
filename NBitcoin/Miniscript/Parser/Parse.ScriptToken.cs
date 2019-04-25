namespace NBitcoin.Miniscript.Parser
{
	internal static partial class Parse
	{
		public static Parser<ScriptToken, ScriptToken> ScriptToken(ScriptToken sct, string expected)
		{
			return i =>
			{
				if (i.AtEnd)
				{
					return ParserResult<ScriptToken, ScriptToken>.Failure(i, new [] {expected}, "Unexpected end of input");
				}

				if (sct.Equals(i.GetCurrent()))
					return ParserResult<ScriptToken, ScriptToken>.Success(i.Advance(), i.GetCurrent());
				return ParserResult<ScriptToken, ScriptToken>.Failure(i.Advance(), $"Unexpected {i.GetCurrent()}");
			};
		}
		public static Parser<ScriptToken, ScriptToken> ScriptToken(ScriptToken sct)
			=> ScriptToken(sct, sct.ToString());
	}
}