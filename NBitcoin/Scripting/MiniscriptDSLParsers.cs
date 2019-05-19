using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NBitcoin.Scripting.Parser;
using NBitcoin.DataEncoders;
using System.Collections.Generic;
using static NBitcoin.Scripting.ParserUtil;

namespace NBitcoin.Scripting
{
	internal static class MiniscriptDSLParser
	{
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