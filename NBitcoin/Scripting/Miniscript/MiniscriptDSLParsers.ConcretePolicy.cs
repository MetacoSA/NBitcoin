using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NBitcoin.Scripting.Parser;
using NBitcoin.DataEncoders;
using System.Collections.Generic;
using NBitcoin.Scripting.Miniscript;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript
{
	internal static partial class MiniscriptDSLParser<TPk, TPKh>
			where TPk : class, IMiniscriptKey<TPKh>, new()
			where TPKh : class, IMiniscriptKeyHash, new()
	{
		internal static Parser<char, string> SurroundedByBrackets()  =>
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

		internal static Parser<char, TPk> TryParseMiniscriptKey(string str)
			=> i =>
			{
				TPk t = default;
				try
				{
					var k = MiniscriptKeyParser<TPk, TPKh>.TryParse(str);
					return ParserResult<char, TPk>.Success(i, k);
				}
				catch (Exception ex)
				{
					return ParserResult<char, TPk>.Failure(i, $"Failed to parse MiniscriptKey {str}. {ex}");
				}
			};

		internal static Parser<char, TPKh> TryParseMiniscriptKeyHash(string str)
			=> i =>
			{
				TPKh t = default;
				try
				{
					var k = MiniscriptKeyParser<TPk, TPKh>.TryParseHash(str);
					return ParserResult<char, TPKh>.Success(i, t);
				}
				catch
				{
					return ParserResult<char, TPKh>.Failure(i, "");
				}
			};

		internal static Parser<char, string> ExprP(string name)
			=>
			from identifier in Parse.String(name)
			from x in SurroundedByBrackets()
			select x;

		private static Parser<char, ConcretePolicy<TPk, TPKh>> PPubKeyExpr()
			=>
			from pk in ExprP("pk").Then(s => TryParseMiniscriptKey(s))
			select ConcretePolicy<TPk, TPKh>.NewKey(pk);

		private static Parser<char, ConcretePolicy<TPk, TPKh>> PSha256Expr()
			=>
			from hash in ExprP("sha256").Then(s => TryConvert(s, uint256.Parse))
			select ConcretePolicy<TPk, TPKh>.NewSha256(hash);

		private static Parser<char, ConcretePolicy<TPk, TPKh>> PHash256Expr()
			=>
			from hash in ExprP("hash256").Then(s => TryConvert(s, uint256.Parse))
			select ConcretePolicy<TPk, TPKh>.NewHash256(hash);

		private static Parser<char, ConcretePolicy<TPk, TPKh>> PRipemd160Expr()
			=>
			from hash in ExprP("ripemd160").Then(s => TryConvert(s, uint160.Parse))
			select ConcretePolicy<TPk, TPKh>.NewRipemd160(hash);

		private static Parser<char, ConcretePolicy<TPk, TPKh>> PHash160Expr()
			=>
			from hash in ExprP("hash160").Then(s => TryConvert(s, uint160.Parse))
			select ConcretePolicy<TPk, TPKh>.NewHash160(hash);

		private static Parser<char, ConcretePolicy<TPk, TPKh>> PAfterExpr()
			=>
			from t in ExprP("after").Then(s => TryConvert(s, UInt32.Parse))
			select ConcretePolicy<TPk, TPKh>.NewAfter(t);

		private static Parser<char, ConcretePolicy<TPk, TPKh>> POlderExpr()
			=>
			from t in ExprP("older").Then(s => TryConvert(s, UInt32.Parse))
			select ConcretePolicy<TPk, TPKh>.NewOlder(t);

		internal static Parser<char, IEnumerable<TSub>> PSubExprs<TSub>(string name, Func<Parser<char, TSub>> subP) =>
			from _n in Parse.String(name)
			from _left in Parse.Char('(')
			from x in Parse
				.Ref(subP)
				.DelimitedBy(Parse.Char(',').Token()).Token()
			from _right in Parse.Char(')')
			select x;

		private static Parser<char, ConcretePolicy<TPk, TPKh>> PAndExpr()
			=>
			from x in PSubExprs("and", DSLParser)
			select ConcretePolicy<TPk, TPKh>.NewAnd(x);

		internal static Parser<char, Tuple<uint, ConcretePolicy<TPk, TPKh>>> POrWithProb()
			=>
			from x in Parse.Digit.AtLeastOnce().Text()
				.Then(digitStr => TryConvert(digitStr, UInt32.Parse))
			from _a in Parse.Char('@')
			from sub in Parse.Ref( DSLParser)
			select Tuple.Create(x, sub);

		private static Parser<char, Tuple<uint, ConcretePolicy<TPk, TPKh>>> POrWithoutProb()
			=>
			from sub in Parse.Ref(DSLParser)
			select Tuple.Create(1U, sub);

		internal static Parser<char, ConcretePolicy<TPk, TPKh>> POrExpr()
			=>
			from x in
				PSubExprs("or", POrWithProb)
				.Or(PSubExprs("or", POrWithoutProb))
			select ConcretePolicy<TPk, TPKh>.NewOr(x);

		private static Parser<char, T> PThresh<T, TIn>(
			string identifier,
			Func<uint, IEnumerable<TIn>, T> constructor,
			Func<Parser<char, TIn>> subP
			) =>
			from _n in Parse.String(identifier)
			from _left in Parse.Char('(')
			from numStr in Parse.Digit.AtLeastOnce().Text()
			from _sep in Parse.Char(',')
			from num in TryConvert(numStr, UInt32.Parse)
			from x in Parse
				.Ref(subP)
				.DelimitedBy(Parse.Char(',').Token()).Token()
			from _right in Parse.Char(')')
			where num <= x.Count()
			select constructor(num, x);

		internal static Parser<char, ConcretePolicy<TPk, TPKh>> PThresholdExpr() =>
			PThresh("thresh", ConcretePolicy<TPk, TPKh>.NewThreshold, DSLParser);

		internal static Parser<char, ConcretePolicy<TPk, TPKh>> DSLParser() =>
			(PPubKeyExpr())
			.Or(PAfterExpr())
			.Or(POlderExpr()
				.Or(PSha256Expr())
				.Or(PHash256Expr())
				.Or(PRipemd160Expr())
				.Or(PHash160Expr())
				.Or(PAndExpr())
				.Or(POrExpr())
				.Or(PThresholdExpr())).Token();

		public static ConcretePolicy<TPk, TPKh> ParseConcretePolicy(string input)
			=> DSLParser().Parse(input);
	}
}
