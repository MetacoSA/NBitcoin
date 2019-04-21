using System;
using System.Collections;
using System.Linq;

namespace NBitcoin.Miniscript
{
	internal partial class MiniscriptDSLParser : CharParsers<>
	{
		private static Parser<string> SurroundedByBrackets()
		{
			var res = from x in Char<>('(');
			return res;
		}
	}
}