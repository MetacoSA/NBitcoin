using System;
using System.Collections;
using System.Linq;
using NBitcoin.Miniscript.Parser;

namespace NBitcoin.Miniscript
{
	internal partial class MiniscriptDSLParser : CharParsers<string>
	{
		private static Parser<string> SurroundedByBrackets(Func<string, >)
		{
			var res =
				from x in Char('(')
				from cin Char()
			return res;
		}
	}
}