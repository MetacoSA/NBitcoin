using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NBitcoin.Miniscript.Parser;
using NBitcoin.DataEncoders;
using System.Collections.Generic;

namespace NBitcoin.Miniscript
{
	internal static class MiniscriptDSLParser
	{
		private static Parser<string> SurroundedByBrackets()
		{
			var res =
				from leftB in Parse.Char('(').Token()
				from x in Parse.CharExcept(')').Many().Text()
				from rightB in Parse.Char(')').Token()
				select x;
			return res;
		}

		private static string[] SafeSplit(string s)
		{
			var parenthCount = 0;
			var items = new List<string>();
			var charSoFar = new List<char>();
			var length = s.Count();
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
						var item = new String(charsCopy.ToArray());
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

		private static Parser<T> TryConvert<T>(string str, Func<string, T> converter)
		{
			return i =>
			{
				try
				{
					return ParserResult<T>.Success(i, converter(str));
				}
				catch (FormatException)
				{
					return ParserResult<T>.Failure(i, $"Failed to parse {str}");
				}
			};
		}

		private static Parser<string> ExprP(string name)
			=>
				from identifier in Parse.String(name)
				from x in SurroundedByBrackets()
				select x;

		private static Parser<string[]> ExprPMany(string name)
			=>
				from x in ExprP(name)
				select SafeSplit(x);

		private static Parser<AbstractPolicy> PubKeyExpr()
			=>
				from pk in ExprP("pk").Then(s => TryConvert(s, c => new PubKey(c)))
				select AbstractPolicy.NewCheckSig(pk);

		private static Parser<AbstractPolicy> MultisigExpr()
			=>
				from contents in ExprPMany("multi")
				from m in TryConvert(contents.First(), UInt32.Parse)
				from pks in contents.Skip(1)
					.Select(pk => TryConvert(pk, c => new PubKey(c)))
					.Sequence()
				select AbstractPolicy.NewMulti(m, pks.ToArray());

		private static Parser<AbstractPolicy> HashExpr()
			=>
				from hash in ExprP("hash").Then(s => TryConvert(s, uint256.Parse))
				select AbstractPolicy.NewHash(hash);

		private static Parser<AbstractPolicy> TimeExpr()
			=>
				from t in ExprP("time").Then(s => TryConvert(s, UInt32.Parse))
				select AbstractPolicy.NewTime(t);

		private static Parser<IEnumerable<AbstractPolicy>> SubExprs(string name) =>
				from _n in Parse.String(name)
				from _left in Parse.Char('(')
				from x in Parse
					.Ref(() => GetDSLParser())
					.DelimitedBy(Parse.Char(',')).Token()
				from _right in Parse.Char(')')
				select x;
		private static Parser<AbstractPolicy> AndExpr()
			=>
				from x in SubExprs("and")
				select AbstractPolicy.NewAnd(x.ElementAt(0), x.ElementAt(1));

		private static Parser<AbstractPolicy> OrExpr()
			=>
				from x in SubExprs("or")
				select AbstractPolicy.NewOr(x.ElementAt(0), x.ElementAt(1));
		private static Parser<AbstractPolicy> AOrExpr()
			=>
				from x in SubExprs("aor")
				select AbstractPolicy.NewAsymmetricOr(x.ElementAt(0), x.ElementAt(1));

		internal static Parser<AbstractPolicy> ThresholdExpr()
			=>
				from _n in Parse.String("thres")
				from _left in Parse.Char('(')
				from numStr in Parse.Digit.AtLeastOnce().Text()
				from _sep in Parse.Char(',')
				from num in TryConvert(numStr, UInt32.Parse)
				from x in Parse
					.Ref(() => GetDSLParser())
					.DelimitedBy(Parse.Char(',')).Token()
				from _right in Parse.Char(')')
				where num <= x.Count()
				select AbstractPolicy.NewThreshold(num, x.ToArray());
		private static Parser<AbstractPolicy> GetDSLParser()
			=>
				(PubKeyExpr()
					.Or(MultisigExpr())
					.Or(TimeExpr())
					.Or(HashExpr())
					.Or(AndExpr())
					.Or(OrExpr())
					.Or(AOrExpr())
					.Or(ThresholdExpr())).Token();

		public static AbstractPolicy ParseDSL(string input)
			=> GetDSLParser().Parse(input);
	}
}