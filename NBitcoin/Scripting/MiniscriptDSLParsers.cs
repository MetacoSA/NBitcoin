using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NBitcoin.Scripting.Parser;
using NBitcoin.DataEncoders;
using System.Collections.Generic;

namespace NBitcoin.Scripting
{
	internal static class MiniscriptDSLParser
	{
		internal static readonly Parser<char, string> SurroundedByBrackets  =
				from leftB in Parse.Char('(').Token()
				from x in Parse.CharExcept(')').Many().Text()
				from rightB in Parse.Char(')').Token()
				select x;

		private static string[] SafeSplit(string s)
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

		private static Parser<char, string[]> ExprPMany(string name)
			=>
				from x in ExprP(name)
				select SafeSplit(x);

		private static readonly Parser<char, AbstractPolicy> PPubKeyExpr =
				from pk in ExprP("pk").Then(s => Parse.TryConvert(s, c => new PubKey(c)))
				select AbstractPolicy.NewCheckSig(pk);

		private static readonly Parser<char, AbstractPolicy> PMultisigExpr =
				from contents in ExprPMany("multi")
				from m in Parse.TryConvert(contents.First(), UInt32.Parse)
				from pks in contents.Skip(1)
					.Select(pk => Parse.TryConvert(pk, c => new PubKey(c)))
					.Sequence()
				select AbstractPolicy.NewMulti(m, pks.ToArray());

		private static readonly Parser<char, AbstractPolicy> PHashExpr =
				from hash in ExprP("hash").Then(s => Parse.TryConvert(s, uint256.Parse))
				select AbstractPolicy.NewHash(hash);

		private static readonly Parser<char, AbstractPolicy> PTimeExpr =
				from t in ExprP("time").Then(s => Parse.TryConvert(s, UInt32.Parse))
				where t <= 65535
				select AbstractPolicy.NewTime(t);

		private static Parser<char, IEnumerable<AbstractPolicy>> PSubExprs(string name) =>
				from _n in Parse.String(name)
				from _left in Parse.Char('(')
				from x in Parse
					.Ref(() => DSLParser)
					.DelimitedBy(Parse.Char(',').Token()).Token()
				from _right in Parse.Char(')')
				select x;
		private static readonly Parser<char, AbstractPolicy> PAndExpr =
				from x in PSubExprs("and")
				select AbstractPolicy.NewAnd(x.ElementAt(0), x.ElementAt(1));

		private static readonly Parser<char, AbstractPolicy> POrExpr =
				from x in PSubExprs("or")
				select AbstractPolicy.NewOr(x.ElementAt(0), x.ElementAt(1));
		private static readonly Parser<char, AbstractPolicy> PAOrExpr =
				from x in PSubExprs("aor")
				select AbstractPolicy.NewAsymmetricOr(x.ElementAt(0), x.ElementAt(1));

		internal static readonly Parser<char, AbstractPolicy> PThresholdExpr =
				from _n in Parse.String("thres")
				from _left in Parse.Char('(')
				from numStr in Parse.Digit.AtLeastOnce().Text()
				from _sep in Parse.Char(',')
				from num in Parse.TryConvert(numStr, UInt32.Parse)
				from x in Parse
					.Ref(() => DSLParser)
					.DelimitedBy(Parse.Char(',').Token()).Token()
				from _right in Parse.Char(')')
				where num <= x.Count()
				select AbstractPolicy.NewThreshold(num, x.ToArray());
		internal static readonly Parser<char, AbstractPolicy> DSLParser =
				(PPubKeyExpr
					.Or(PMultisigExpr)
					.Or(PTimeExpr)
					.Or(PHashExpr)
					.Or(PAndExpr)
					.Or(POrExpr)
					.Or(PAOrExpr)
					.Or(PThresholdExpr)).Token();


		public static AbstractPolicy ParseDSL(string input)
			=> DSLParser.Parse(input);
	}
}