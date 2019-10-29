using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NBitcoin.Scripting.Parser;
using NBitcoin.DataEncoders;
using System.Collections.Generic;
using NBitcoin.Scripting.Miniscript;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting
{
	internal static partial class MiniscriptDSLParser
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

		internal static Parser<char, T> TryConvert<T>(string str, Func<string, T> converter)
		{
			return i =>
			{
				try
				{
					return ParserResult<char, T>.Success(i, converter(str));
				}
				catch (FormatException)
				{
					return ParserResult<char, T>.Failure(i, $"Failed to parse {str}");
				}
			};
		}

		internal static Parser<char, TPk> TryParseMiniscriptKey<TPk>(string str)
			where TPk : IMiniscriptKey
			=> i =>
			{
				TPk t = default;
				if (!t.TryParse(str))
					return ParserResult<char, TPk>.Failure(i, $"Failed to parse MiniscriptKey {i}");
				return ParserResult<char, TPk>.Success(i, t);
			};

		internal static Parser<char, string> ExprP(string name)
			=>
			from identifier in Parse.String(name)
			from x in SurroundedByBrackets
			select x;

		private static Parser<char, string[]> ExprPMany(string name)
			=>
			from x in ExprP(name)
			select SafeSplit(x);

		private static Parser<char, ConcretePolicy<TPk>> PPubKeyExpr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from pk in ExprP("pk").Then(s => TryParseMiniscriptKey<TPk>(s))
			select ConcretePolicy<TPk>.NewKey(pk);

		private static Parser<char, ConcretePolicy<TPk>> PSha256Expr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from hash in ExprP("sha256").Then(s => TryConvert(s, uint256.Parse))
			select ConcretePolicy<TPk>.NewSha256(hash);

		private static Parser<char, ConcretePolicy<TPk>> PHash256Expr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from hash in ExprP("hash256").Then(s => TryConvert(s, uint256.Parse))
			select ConcretePolicy<TPk>.NewHash256(hash);

		private static Parser<char, ConcretePolicy<TPk>> PRipemd160Expr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from hash in ExprP("ripemd160").Then(s => TryConvert(s, uint160.Parse))
			select ConcretePolicy<TPk>.NewRipemd160(hash);

		private static Parser<char, ConcretePolicy<TPk>> PHash160Expr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from hash in ExprP("hash160").Then(s => TryConvert(s, uint160.Parse))
			select ConcretePolicy<TPk>.NewHash160(hash);

		private static Parser<char, ConcretePolicy<TPk>> PAfterExpr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from t in ExprP("after").Then(s => TryConvert(s, UInt32.Parse))
			select ConcretePolicy<TPk>.NewAfter(t);

		private static Parser<char, ConcretePolicy<TPk>> POlderExpr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from t in ExprP("older").Then(s => TryConvert(s, UInt32.Parse))
			select ConcretePolicy<TPk>.NewOlder(t);

		private static Parser<char, IEnumerable<TSub>> PSubExprs<TSub>(string name, Func<Parser<char, TSub>> subP) =>
			from _n in Parse.String(name)
			from _left in Parse.Char('(')
			from x in Parse
				.Ref(subP)
				.DelimitedBy(Parse.Char(',').Token()).Token()
			from _right in Parse.Char(')')
			select x;

		private static Parser<char, ConcretePolicy<TPk>> PAndExpr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from x in PSubExprs("and", DSLParser<TPk>)
			select ConcretePolicy<TPk>.NewAnd(x);

		private static Parser<char, Tuple<uint, ConcretePolicy<TPk>>> POrWithProb<TPk>()
			where TPk : IMiniscriptKey
			=>
			from x in Parse.Digit.AtLeastOnce().Text()
				.Then(digitStr => TryConvert(digitStr, UInt32.Parse))
			from _a in Parse.Char('@')
			from sub in Parse.Ref(DSLParser<TPk>)
			select Tuple.Create(x, sub);

		private static Parser<char, Tuple<uint, ConcretePolicy<TPk>>> POrWithoutProb<TPk>()
			where TPk : IMiniscriptKey
			=>
			from sub in Parse.Ref(DSLParser<TPk>)
			select Tuple.Create(1U, sub);

		private static Parser<char, ConcretePolicy<TPk>> POrExpr<TPk>()
			where TPk : IMiniscriptKey
			=>
			from x in
				PSubExprs("or", POrWithProb<TPk>)
				.Or(PSubExprs("or", POrWithoutProb<TPk>))
			select ConcretePolicy<TPk>.NewOr(x);

		internal static Parser<char, ConcretePolicy<TPk>> PThresholdExpr<TPk>()
			where TPk : IMiniscriptKey
			=>
				from _n in Parse.String("thres")
				from _left in Parse.Char('(')
				from numStr in Parse.Digit.AtLeastOnce().Text()
				from _sep in Parse.Char(',')
				from num in TryConvert(numStr, UInt32.Parse)
				from x in Parse
					.Ref(DSLParser<TPk>)
					.DelimitedBy(Parse.Char(',').Token()).Token()
				from _right in Parse.Char(')')
				where num <= x.Count()
				select ConcretePolicy<TPk>.NewThreshold(num, x.ToArray());

		internal static Parser<char, ConcretePolicy<TPk>> DSLParser<TPk>()
				where TPk : IMiniscriptKey
				=>
				(PPubKeyExpr<TPk>()
					.Or(PAfterExpr<TPk>())
					.Or(POlderExpr<TPk>())
					.Or(PSha256Expr<TPk>())
					.Or(PHash256Expr<TPk>())
					.Or(PRipemd160Expr<TPk>())
					.Or(PHash160Expr<TPk>())
					.Or(PAndExpr<TPk>())
					.Or(POrExpr<TPk>())
					.Or(PThresholdExpr<TPk>())).Token();

		public static ConcretePolicy<TPk> ParseDSL<TPk>(string input)
			where TPk : IMiniscriptKey
			=> DSLParser<TPk>().Parse(input);
	}
}
