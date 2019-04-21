using System.Linq;
using System;

namespace NBitcoin.Miniscript
{
	internal abstract class Parsers<TIn>
	{
		public Parser<TIn, TValue> Return<TValue>(TValue value) =>
			input => new ParserResult<TIn, TValue>(value, input);

		public Parser<TIn, TValue[]> Many<TValue>(Parser<TIn, TValue> parser) =>
			Many1(parser).Or(Return(new TValue[0]));

		public Parser<TIn, TValue[]> Many1<TValue>(Parser<TIn, TValue> parser) =>
			from x in parser
			from xs in Many(parser)
			select (new[] { x }).Concat(xs).ToArray();
	}
	internal abstract class CharParsers<TIn> : Parsers<TIn>
	{
		public Parser<TIn, char> AnyChar { get; }
		public Parser<TIn, char> Char(char ch) =>
			from c in AnyChar where c == ch select c;

		public Parser<TIn, char> Char(Func<char, bool> predicate) =>
			from c in AnyChar where predicate(c) select c;
	}
}