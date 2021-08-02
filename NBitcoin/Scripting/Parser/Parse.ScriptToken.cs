using System;

namespace NBitcoin.Scripting.Parser
{
	internal static partial class Parse
	{
		public static Parser<ScriptToken, ScriptToken> ScriptToken(Func<int, bool> predicate, string expected)
		{
			return (i, n) =>
			{
				if (i.AtEnd)
				{
					return ParserResult<ScriptToken, ScriptToken>.Failure(i, new[] { expected }, "Unexpected end of input");
				}

				if (predicate(i.GetCurrent().Tag))
					return ParserResult<ScriptToken, ScriptToken>.Success(i.Advance(), i.GetCurrent());
				return ParserResult<ScriptToken, ScriptToken>.Failure(i, $"Unexpected {i.GetCurrent()}");
			};
		}

		public static Parser<ScriptToken, ScriptToken> ScriptToken(ScriptToken sct, string expected)
			=> ScriptToken((tag) => tag == sct.Tag, expected);

		public static Parser<ScriptToken, ScriptToken> ScriptToken(int tag)
			=> ScriptToken(actualTag => actualTag == tag, $"ScriptToken for tag: {tag}");
		public static Parser<ScriptToken, ScriptToken> ScriptToken(ScriptToken sct)
			=> ScriptToken(sct, sct.ToString());
	}
}
