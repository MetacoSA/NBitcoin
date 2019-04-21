using System;

namespace NBitcoin.Miniscript
{
	public class EndOfSourceException : InvalidOperationException
	{
		public EndOfSourceException(Source s) : base($"Unexpected end of source") { }
	}
	public struct Source
	{
		private readonly string _source;
		private readonly int _pos;

		private Source(string source, int pos)
		{
			_source = source;
			_pos = pos;
		}

		public static Source Create(string source)
		=> new Source(source, 0);

		public Tuple<char, Source> Read()
		{
			if (_source.Length <= _pos)
				throw new EndOfSourceException(this);
			return Tuple.Create(_source[_pos], new Source(_source, _pos + 1));
		}
	}
	internal static class ParserCombinatorExtensions
	{
		public static Parser<TIn, TValue> Or<TIn, TValue>(this Parser<TIn, TValue> parser1, Parser<TIn, TValue> parser2) =>
			input => parser1(input) ?? parser2(input);

		public static Parser<TIn, TValue2> And<TIn, TValue, TValue2>(this Parser<TIn, TValue> parser1, Parser<TIn, TValue2> parser2) =>
			input => parser2(parser1(input).Rest);

		public static Parser<TIn, TValue> Where<TIn, TValue>(this Parser<TIn, TValue> parser, Func<TValue, bool> predicate) =>
			input =>
				{
					var res = parser(input);
					if (res == null || !predicate(res.Value)) return null;
					return res;
				};

		public static Parser<TIn, TValue2> Select<TIn, TValue, TValue2>(this Parser<TIn, TValue> parser, Func<TValue, TValue2> mapFn) =>
			input =>
				{
					var res = parser(input);
					return res == null ? null : new ParserResult<TIn, TValue2>(mapFn(res.Value), res.Rest);
				};

		public static Parser<TIn, TValue2> SelectMany<TIn, TValue, TIntermediate, TValue2>(
			this Parser<TIn, TValue> parser,
			Func<TValue, Parser<TIn, TIntermediate>> selector,
			Func<TValue, TIntermediate, TValue2> projector) =>
			input =>
				{
					var res = parser(input);
					if (res == null) return null;
					var val = res.Value;
					var res2 = selector(val)(res.Rest);
					if (res2 == null) return null;
					return new ParserResult<TIn, TValue2>(projector(val, res2.Value), res2.Rest);
				};

	}
}