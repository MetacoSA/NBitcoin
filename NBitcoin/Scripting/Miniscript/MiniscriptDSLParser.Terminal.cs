using System;
using System.Linq;
using NBitcoin.Scripting.Miniscript.Policy;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting.Miniscript
{
	internal static partial class MiniscriptDSLParser<TPk, TPKh>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		# region Leaf
		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalPk
			=
			from pk in ExprP("pk").Then(s => TryParseMiniscriptKey(s))
			select Terminal<TPk, TPKh>.NewPk(pk);

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalPkH
			=
			from pkH in ExprP("pk_h").Then(TryParseMiniscriptKeyHash)
			select Terminal<TPk, TPKh>.NewPkH(pkH);

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalTrue =
			from _t in Parse.Char('1').Token()
			select Terminal<TPk, TPKh>.NewTrue();

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalFalse =
			from _t in Parse.Char('0').Token()
			select Terminal<TPk, TPKh>.NewFalse();

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalAfter =
			from t in ExprP("after").Then(s => TryConvert(s, UInt32.Parse))
			select Terminal<TPk, TPKh>.NewAfter(t);

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalOlder =
			from t in ExprP("older").Then(s => TryConvert(s, UInt32.Parse))
			select Terminal<TPk, TPKh>.NewOlder(t);

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalSha256 =
			from t in ExprP("sha256").Then(s => TryConvert(s, uint256.Parse))
			select Terminal<TPk, TPKh>.NewSha256(t);

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalHash256 =
			from t in ExprP("hash256").Then(s => TryConvert(s, uint256.Parse))
			select Terminal<TPk, TPKh>.NewHash256(t);

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalRipemd160 =
			from t in ExprP("ripemd160").Then(s => TryConvert(s, uint160.Parse))
			select Terminal<TPk, TPKh>.NewRipemd160(t);

		private static readonly Parser<char, Terminal<TPk, TPKh>> PTerminalHash160 =
			from t in ExprP("hash160").Then(s => TryConvert(s, uint160.Parse))
			select Terminal<TPk, TPKh>.NewHash160(t);

		#endregion

		private static Parser<char, Terminal<TPk, TPKh>> PWrapperNested(char identifier,
			Func<Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> construct)
			=>
			(
				from _t in Parse.Char(identifier)
				from x in Parse.Ref(() => PWrappersNested).Except(PWrapperNested(identifier, construct))
				select x
			);

			private static Parser<char, Terminal<TPk, TPKh>> PWrapperInnerMost(char identifier,
			Func<Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> construct)
				=>
			(
				from _t in Parse.Char(identifier).Then(_ => Parse.Char(':'))
				from inner in Parse.Ref(PNonWrappers)
				select construct(Miniscript<TPk, TPKh>.FromAst(inner))
			);

		private static Parser<char, Terminal<TPk, TPKh>> PBinary(
			string identifier,
			Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> constructor
		) =>
			from s in PSubExprs(identifier, TerminalDSLParser)
			where s.Count() == 2
			select constructor(Miniscript<TPk, TPKh>.FromAst(s.ElementAt(0)),
				Miniscript<TPk, TPKh>.FromAst(s.ElementAt(1)));

		private static Parser<char, Terminal<TPk, TPKh>> PTernary(
			string identifier,
			Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> constructor
		) =>
			from s in PSubExprs(identifier, TerminalDSLParser)
			where s.Count() == 3
			select constructor(
				Miniscript<TPk, TPKh>.FromAst(s.ElementAt(0)),
				Miniscript<TPk, TPKh>.FromAst(s.ElementAt(1)),
				Miniscript<TPk, TPKh>.FromAst(s.ElementAt(2)));

		private static Parser<char, Terminal<TPk, TPKh>> PTerminalThresh =
			PThresh(
				"thresh",
				Terminal<TPk, TPKh>.NewThresh,
				() => TerminalDSLParser().Select(t => Miniscript<TPk, TPKh>.FromAst(t)));

		private static Parser<char, Terminal<TPk, TPKh>> PTerminalThreshM =
			PThresh("thresh_m", Terminal<TPk, TPKh>.NewThreshM,
			() => (from pk in ExprP("pk").Then(s => TryParseMiniscriptKey(s))
			select pk));

		private static readonly Parser<char, Terminal<TPk, TPKh>> PWrappersNested =
				PWrapperNested('a', Terminal<TPk, TPKh>.NewAlt)
				.Or(PWrapperNested('c', Terminal<TPk, TPKh>.NewCheck))
				.Or(PWrapperNested('s', Terminal<TPk, TPKh>.NewSwap))
				.Or(PWrapperNested('d', Terminal<TPk, TPKh>.NewDupIf))
				.Or(PWrapperNested('v', Terminal<TPk, TPKh>.NewVerify))
				.Or(PWrapperNested('j', Terminal<TPk, TPKh>.NewZeroNotEqual));

		private static readonly Parser<char, Terminal<TPk, TPKh>> PWrappersInnerMost =
			PWrapperInnerMost('a', Terminal<TPk, TPKh>.NewAlt)
			.Or(PWrapperInnerMost('c', Terminal<TPk, TPKh>.NewCheck))
			.Or(PWrapperInnerMost('s', Terminal<TPk, TPKh>.NewSwap))
			.Or(PWrapperInnerMost('d', Terminal<TPk, TPKh>.NewDupIf))
			.Or(PWrapperInnerMost('v', Terminal<TPk, TPKh>.NewVerify))
			.Or(PWrapperInnerMost('j', Terminal<TPk, TPKh>.NewZeroNotEqual));


		private static Parser<char, Terminal<TPk, TPKh>> PNonWrappers() =>
			// ------ leafs ------
			PTerminalPk
				.Or(PTerminalPkH)
				.Or(PTerminalAfter)
				.Or(PTerminalOlder)
				.Or(PTerminalSha256)
				.Or(PTerminalHash256)
				.Or(PTerminalRipemd160)
				.Or(PTerminalHash160)
				.Or(PTerminalTrue)
				.Or(PTerminalFalse)
				// ------- Conjunctions -----
				.Or(PBinary("and_v", Terminal<TPk, TPKh>.NewAndV))
				.Or(PBinary("and_b", Terminal<TPk, TPKh>.NewAndB))
				.Or(PTernary("and_or", Terminal<TPk, TPKh>.NewAndOr))
				// ------- Disjunctions  -----
				.Or(PBinary("or_b", Terminal<TPk, TPKh>.NewOrB))
				.Or(PBinary("or_d", Terminal<TPk, TPKh>.NewOrD))
				.Or(PBinary("or_c", Terminal<TPk, TPKh>.NewOrC))
				.Or(PBinary("or_i", Terminal<TPk, TPKh>.NewOrI))
				// ------- Thresholds ------
				.Or(Parse.Ref(() => PTerminalThresh))
				.Or(Parse.Ref(() => PTerminalThreshM));
		private static Parser<char, Terminal<TPk, TPKh>> TerminalDSLParser() =>
				Parse.Ref(() => PWrappersNested)
					.Or(Parse.Ref(() => PWrappersInnerMost))
					.Or(PNonWrappers());
				// ------- wrappers --------

		public static Terminal<TPk, TPKh> ParseTerminal(string input)
			=> TerminalDSLParser().Parse(input);
	}
}
