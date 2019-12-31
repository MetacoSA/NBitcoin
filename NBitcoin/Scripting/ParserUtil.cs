using System.Collections.Generic;
using NBitcoin.Scripting.Parser;
using System;

namespace NBitcoin.Scripting
{
	internal static class ParserUtil
	{
		internal static readonly Parser<char, string> SurroundedByBrackets =
				from leftB in Parse.Char('(').Token()
				from x in Parse.CharExcept(')').Many().Text()
				from rightB in Parse.Char(')').Token()
				select x;

		internal static string[] SafeSplit(string s)
		{
			var parenthCount = 0;
			var items = new List<string>();
			var charSoFar = new List<char>();
			var length = s.Length;
			for (int i = 0; i < length; i++)
			{
				var c = s[i];
				if (c == '(')
				{
					parenthCount++;
					charSoFar.Add(c);
				}
				else if (c == ')')
				{
					parenthCount--;
					charSoFar.Add(c);
				}
				else if (parenthCount != 0)
				{
					charSoFar.Add(c);
				}

				if (parenthCount == 0)
				{
					if (i == length - 1)
					{
						charSoFar.Add(c);
					}
					if (c == ',' || i == length - 1)
					{
						var charsCopy = new List<char>(charSoFar);
						charSoFar = new List<char>();
						var item = new String(charsCopy.ToArray()).Trim();
						items.Add(item);
					}
					else
					{
						charSoFar.Add(c);
					}
				}
			}
			return items.ToArray();
		}

		internal static Parser<char, string> ExprP(string name)
			=>
				from identifier in Parse.String(name)
				from x in SurroundedByBrackets
				select x;

		internal static Parser<char, string[]> ExprPMany(string name)
			=>
				from x in ExprP(name)
				select SafeSplit(x);
	}
}