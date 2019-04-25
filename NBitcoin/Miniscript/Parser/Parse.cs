using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NBitcoin.Miniscript.Parser
{
	internal static class Parse
	{


		# region char parsers
		public static Parser<char, char> Char(Func<char, bool> predicate, string expected)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return i =>
			{
				if (i.AtEnd)
				{
					return ParserResult<char, char>.Failure(i, new [] {expected}, "Unexpected end of input");
				}

				if (predicate(i.GetCurrent()))
					return ParserResult<char, char>.Success(i.Advance(), i.GetCurrent());

				return ParserResult<char, char>.Failure(i, new [] {expected}, $"Unexpected '{i.GetCurrent()}'");
			};
		}
		public static Parser<char, char> CharExcept(Func<char, bool> predicate, string description)
			=> Char(c => !predicate(c), $"any character except {description}");

		public static Parser<char, char> Char(char c)
			=> Char(ch => c == ch, char.ToString(c));

		public static Parser<char, char> Chars(params char[] c)
			=> Char(c.Contains, string.Join("|", c));

		public static Parser<char, char> Chars(string c)
			=> Char(c.AsEnumerable().Contains, string.Join("|", c));

		public static Parser<char, char> CharExcept(char c)
			=> CharExcept(ch => c == ch, c.ToString());

		public static readonly Parser<char, char> AnyChar = Char(c => true, "any charactor");
		public static readonly Parser<char, char> WhiteSpace = Char(char.IsWhiteSpace, "whitespace");
		public static readonly Parser<char, char> Digit = Char(char.IsDigit, "digit");
		public static readonly Parser<char, char> Letter = Char(char.IsLetter, "letter");
		public static readonly Parser<char, char> LetterOrDigit = Char(char.IsLetterOrDigit, "letter or digit");

		public static readonly Parser<char, char> Numeric = Char(char.IsNumber, "numeric character");

		/// <summary>
		/// Parse a string of characters.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static Parser<char, IEnumerable<char>> String(string s)
		{
			if (s == null) throw new ArgumentNullException(nameof(s));

			return s
				.AsEnumerable()
				.Select(Char)
				.Sequence();
		}

		public static Parser<TToken, IEnumerable<T>> Sequence<TToken, T>(this IEnumerable<Parser<TToken, T>> parserList)
		{
			return
				parserList
					.Aggregate(Return<TToken, IEnumerable<T>>(Enumerable.Empty<T>()), (a, p) => a.Concat(p.Once()));
		}


		#endregion


		#region generic utilities

		public static Parser<TToken, U> Then<TToken, T, U>(this Parser<TToken, T> first, Func<T, Parser<TToken, U>> second)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));
			if (second == null)
				throw new ArgumentNullException(nameof(second));

			return i => first(i).IfSuccess(s => second(s.Value)(s.Rest));
		}

		public static Parser<TToken, IEnumerable<T>> Many<TToken, T>(this Parser<TToken, T> parser)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));

			return i =>
			{
				var rest = i;
				var result = new List<T>();
				var r = parser(i);
				while (r.IsSuccess)
				{
					if (rest.Equals(r.Rest))
						break;
					result.Add(r.Value);
					rest = r.Rest;
					r = parser(rest);
				}

				return ParserResult<TToken, IEnumerable<T>>.Success(rest, result);
			};
		}

		/// <summary>
		/// Parse a stream of elements, failing if any element is only partially parsed.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T">The type of element to parse.</typeparam>
		/// <param name="parser">A parser that matches a single element.</param>
		/// <returns>A <see cref="Parser{T}"/> that matches the sequence.</returns>
		/// <remarks>
		/// <para>
		/// Using <seealso cref="XMany{T}(Parser{T})"/> may be preferable to <seealso cref="Many{T}(Parser{T})"/>
		/// where the first character of each match identified by <paramref name="parser"/>
		/// is sufficient to determine whether the entire match should succeed. The X*
		/// methods typically give more helpful errors and are easier to debug than their
		/// unqualified counterparts.
		/// </para>
		/// </remarks>
		/// <seealso cref="XOr"/>
		public static Parser<TToken, IEnumerable<T>> XMany<TToken, T>(this Parser<TToken, T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));

			return parser.Many<TToken, T>().Then(m => parser.Once().XOr(Return<TToken, IEnumerable<T>>(m)));
		}

		/// <summary>
		/// TryParse a stream of elements with at least one item.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <param name="parser"></param>
		/// <returns></returns>
		public static Parser<TToken, IEnumerable<T>> AtLeastOnce<TToken, T>(this Parser<TToken, T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));

			return parser.Once().Then(t1 => parser.Many().Select(ts => t1.Concat(ts)));
		}

		public static Parser<TToken, IEnumerable<T>> Once<TToken,T>(this Parser<TToken, T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			return parser.Select(r => (IEnumerable<T>)new[] { r });
		}

		public static Parser<TToken, IEnumerable<T>> AtLeastOne<TToken, T> (this Parser<TToken, T> parser)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));

			return parser.Once().Then(t1 => parser.Many().Select(ts => t1.Concat(ts)));
		}

		/// <summary>
		/// TryParse a stream of elements with at least one item. Except the first
		/// item, all other items will be matched with the <code>XMany</code> operator.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <param name="parser"></param>
		/// <returns></returns>
		public static Parser<TToken, IEnumerable<T>> XAtLeastOnce<TToken, T>(this Parser<TToken, T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));

			return parser.Once().Then(t1 => parser.XMany().Select(ts => t1.Concat(ts)));
		}

		/// <summary>
		/// Lift to a parser monad world
		/// </summary>
		/// <param name="v"></param>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Parser<TToken, T> Return<TToken, T>(T v)
			=> i => ParserResult<TToken, T>.Success(i, v);

		/// <summary>
		/// Take the result of parsing, and project it onto a different domain.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="convert"></param>
		/// <returns></returns>
		public static Parser<TToken, U> Select<TToken, T, U>(this Parser<TToken, T> parser, Func<T, U> convert)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (convert == null) throw new ArgumentNullException(nameof(convert));

			return parser.Then(t => Return<TToken, U>(convert(t)));
		}

		public static Parser<char, T> Token<T>(this Parser<char, T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));

			return from leading in WhiteSpace.Many()
				   from item in parser
				   from trailing in WhiteSpace.Many()
				   select item;
		}

		/// <summary>
		/// Refer to another parser indirectly. This allows circular compile-time dependency between parsers.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static Parser<TToken, T> Ref<TToken, T>(Func<Parser<TToken, T>> reference)
		{
			if (reference == null) throw new ArgumentNullException(nameof(reference));

			Parser<TToken, T> p = null;

			return i =>
				{
					if (p == null)
						p = reference();

					if (i.Memos.ContainsKey(p))
						throw new ParseException(i.Memos[p].ToString());

					i.Memos[p] = ParserResult<TToken, T>.Failure(i,
						new string[0],
						"Left recursion in the grammar."
					);
					var result = p(i);
					i.Memos[p] = result;
					return result;
				};
		}

		/// <summary>
		/// Convert a stream of characters to a string.
		/// </summary>
		/// <param name="characters"></param>
		/// <returns></returns>
		public static Parser<char, string> Text(this Parser<char, IEnumerable<char>> characters)
		{
			return characters.Select(chs => new string(chs.ToArray()));
		}

		public static Parser<TToken, T> Or<TToken, T>(this Parser<TToken, T> first, Parser<TToken, T> second) 
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));
			if (second == null)
				throw new ArgumentNullException(nameof(second));

			return i =>
			{
				var fr = first(i);
				if (fr.IsSuccess)
					return second(i).IfFailure<T>(sf => DetermineBestError(fr, sf));

				if (fr.Rest.Equals(i))
					return second(i).IfFailure<T>(sf => fr);

				return fr;
			};
		}


		/// <summary>
		/// Parse first, if it succeeds, return first, otherwise try second.
		/// Assumes that the first parsed character will determine the parser chosen (see Try).
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public static Parser<TToken, T> XOr<TToken, T> (this Parser<TToken, T> first, Parser<TToken, T> second)
		{
			if (first == null) throw new ArgumentNullException(nameof(first));
			if (second == null) throw new ArgumentNullException(nameof(second));

			return i =>
			{
				var fr = first(i);
				if (!fr.IsSuccess)
				{
					// The 'X' part
					if (!fr.Rest.Equals(i))
						return fr;

					return second(i).IfFailure<T>(sf => DetermineBestError(fr, sf));
				}

				// This handles a zero-length successful application of first.
				if (fr.Rest.Equals(i))
					return second(i).IfFailure<T>(sf => fr);

				return fr;
			};
		}

		/// <summary>
		/// Concatenate two streams of elements.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public static Parser<TToken, IEnumerable<T>> Concat<TToken, T>(this Parser<TToken, IEnumerable<T>> first, Parser<TToken, IEnumerable<T>> second)
		{
			if (first == null) throw new ArgumentNullException(nameof(first));
			if (second == null) throw new ArgumentNullException(nameof(second));

			return first.Then(f => second.Select(f.Concat));
		}

		public static ParserResult<TToken, T> DetermineBestError<TToken, T>(
			ParserResult<TToken, T> firstF,
			ParserResult<TToken, T> secondF
			)
		{
			if (secondF.Rest.Position > firstF.Rest.Position)
				return secondF;

			if (secondF.Rest.Position == firstF.Rest.Position)
				return ParserResult<TToken, T>.Failure(
					firstF.Rest,
					firstF.Expected.Union(secondF.Expected),
					firstF.Description
				);

			return firstF;
		}


		/// <summary>
		/// Version of Return with simpler inline syntax.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Parser<TToken, U> Return<TToken, T, U>(this Parser<TToken, T> parser, U value)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			return parser.Select(t => value);
		}


		/// <summary>
		/// Attempt parsing only if the <paramref name="except"/> parser fails.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="except"></param>
		/// <returns></returns>
		public static Parser<TToken, T> Except<TToken, T, U>(this Parser<TToken, T> parser, Parser<TToken, U> except)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (except == null) throw new ArgumentNullException(nameof(except));

			// Could be more like: except.Then(s => s.Fail("..")).XOr(parser)
			return i =>
				{
					var r = except(i);
					if (r.IsSuccess)
						return ParserResult<TToken, T>.Failure(i, new[] { "other than the excepted input"} , "Excepted parser succeeded." );
					return parser(i);
				};
		}

		/// <summary>
		/// Parse a sequence of items until a terminator is reached.
		/// Returns the sequence, discarding the terminator.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="until"></param>
		/// <returns></returns>
		public static Parser<TToken, IEnumerable<T>> Until<TToken, T, U>(this Parser<TToken, T> parser, Parser<TToken, U> until)
		{
			return parser.Except(until).Many().Then(r => until.Return(r));
		}


		/// <summary>
		/// Succeed if the parsed value matches predicate.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <param name="parser"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public static Parser<TToken, T> Where<TToken, T>(this Parser<TToken, T> parser, Func<T, bool> predicate)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			return i => parser(i).IfSuccess(s =>
				predicate(s.Value) ? s : ParserResult<TToken, T>.Failure(i,
					new string[0],
					string.Format("Unexpected {0}.", s.Value)
					)
				);
		}


		/// <summary>
		/// Monadic combinator Then, adapted for Linq comprehension syntax.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="parser"></param>
		/// <param name="selector"></param>
		/// <param name="projector"></param>
		/// <returns></returns>
		public static Parser<TToken, V> SelectMany<TToken, T, U, V>(
			this Parser<TToken, T> parser,
			Func<T, Parser<TToken, U>> selector,
			Func<T, U, V> projector)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (selector == null) throw new ArgumentNullException(nameof(selector));
			if (projector == null) throw new ArgumentNullException(nameof(projector));

			return parser.Then(t => selector(t).Select(u => projector(t, u)));
		}

		/// <summary>
		/// Chain a left-associative operator.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<TToken, T> ChainOperator<TToken,T, TOp>(
			Parser<TToken, TOp> op,
			Parser<TToken, T> operand,
			Func<TOp, T, T, T> apply)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return operand.Then(first => ChainOperatorRest(first, op, operand, apply, Or));
		}

		/// <summary>
		/// Chain a left-associative operator.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<TToken, T> XChainOperator<TToken, T, TOp>(
			Parser<TToken, TOp> op,
			Parser<TToken, T> operand,
			Func<TOp, T, T, T> apply)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return operand.Then(first => ChainOperatorRest(first, op, operand, apply, XOr));
		}

		static Parser<TToken, T> ChainOperatorRest<TToken, T, TOp>(
			T firstOperand,
			Parser<TToken, TOp> op,
			Parser<TToken, T> operand,
			Func<TOp, T, T, T> apply,
			Func<Parser<TToken, T>, Parser<TToken, T>, Parser<TToken, T>> or)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return or(op.Then(opvalue =>
						  operand.Then(operandValue =>
							  ChainOperatorRest(apply(opvalue, firstOperand, operandValue), op, operand, apply, or))),
					  Return<TToken, T>(firstOperand));
		}

		/// <summary>
		/// Chain a right-associative operator.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<TToken, T> ChainRightOperator<TToken, T, TOp>(
			Parser<TToken, TOp> op,
			Parser<TToken, T> operand,
			Func<TOp, T, T, T> apply)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return operand.Then(first => ChainRightOperatorRest(first, op, operand, apply, Or));
		}

		/// <summary>
		/// Chain a right-associative operator.
		/// </summary>
		/// <typeparam name="TToken"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<TToken, T> XChainRightOperator<TToken, T, TOp>(
			Parser<TToken, TOp> op,
			Parser<TToken, T> operand,
			Func<TOp, T, T, T> apply)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return operand.Then(first => ChainRightOperatorRest(first, op, operand, apply, XOr));
		}

		static Parser<TToken, T> ChainRightOperatorRest<TToken, T, TOp>(
			T lastOperand,
			Parser<TToken, TOp> op,
			Parser<TToken, T> operand,
			Func<TOp, T, T, T> apply,
			Func<Parser<TToken, T>, Parser<TToken, T>, Parser<TToken, T>> or)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return or(op.Then(opvalue =>
						operand.Then(operandValue =>
							ChainRightOperatorRest(operandValue, op, operand, apply, or)).Then(r =>
								Return<TToken, T>(apply(opvalue, lastOperand, r)))),
					  Return<TToken, T>(lastOperand));
		}

		/// <summary>
		/// Parse a number.
		/// </summary>
		public static readonly Parser<char, string> Number = Numeric.AtLeastOnce().Text();

		static Parser<char, string> DecimalWithoutLeadingDigits(CultureInfo ci = null)
		{
			return from nothing in Return<char, string>("")
					   // dummy so that CultureInfo.CurrentCulture is evaluated later
				   from dot in String((ci ?? CultureInfo.CurrentCulture).NumberFormat.NumberDecimalSeparator).Text()
				   from fraction in Number
				   select dot + fraction;
		}

		static Parser<char, string> DecimalWithLeadingDigits(CultureInfo ci = null)
		{
			return Number.Then(n => DecimalWithoutLeadingDigits(ci).XOr(Return<char, string>("")).Select(f => n + f));
		}

		/// <summary>
		/// Parse a decimal number using the current culture's separator character.
		/// </summary>
		public static readonly Parser<char, string> Decimal = DecimalWithLeadingDigits().XOr(DecimalWithoutLeadingDigits());

		/// <summary>
		/// Parse a decimal number with separator '.'.
		/// </summary>
		public static readonly Parser<char, string> DecimalInvariant = DecimalWithLeadingDigits(CultureInfo.InvariantCulture)
																	 .XOr(DecimalWithoutLeadingDigits(CultureInfo.InvariantCulture));

		#endregion

		# region sequence
		public static Parser<TToken, IEnumerable<T>> DelimitedBy<TToken, T, U> (
			this Parser<TToken, T> parser,
			Parser<TToken, U> delimiter
			)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));
			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));

			return
				from head in parser.Once()
				from tail in
					(
						from seprattor in delimiter
						from item in parser
						select item
					).Many()
				select head.Concat(tail);
		}

		public static Parser<TToken, IEnumerable<T>> XDelimitedBy<TToken, T, U>(
			this Parser<TToken, T> itemParser,
			Parser<TToken, U> delimiter
			)
		{
			if (itemParser == null)
				throw new ArgumentNullException(nameof(itemParser));

			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));

			return
				from head in itemParser.Once()
				from tail in
					(
						from seprator in delimiter
						from item in itemParser
						select item
					).XMany()
				select head.Concat(tail);
		}

		public static Parser<TToken, IEnumerable<T>> Repeat<TToken, T>(
			this Parser<TToken, T> parser,
			int count
			)
			=> Repeat<TToken, T>(parser, count, count);

		public static Parser<TToken, IEnumerable<T>> Repeat<TToken, T> (
			this Parser<TToken, T> parser,
			int minimumCount,
			int maximumCount
			)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));

			return i =>
				{
					var remainder = i;
					var result = new List<T>();

					for (var n = 0; n < maximumCount; ++n)
					{
						var r = parser(remainder);

						if (!r.IsSuccess && n < minimumCount)
						{
							var what = r.Rest.AtEnd
								? "end of input"
								: r.Rest.GetCurrent().ToString();

							var msg = $"Unexpected '{what}'";
							var exp = minimumCount == maximumCount
								? $"'{string.Join(", ", r.Expected)}' {minimumCount} times, but found {n}"
								: $"'{string.Join(", ", r.Expected)}' between {minimumCount} and {maximumCount} times, but found {n}";

							return ParserResult<TToken, IEnumerable<T>>.Failure(i, new[] { exp }, msg);
						}

						if (!ReferenceEquals(remainder, r.Rest))
						{
							result.Add(r.Value);
						}

						remainder = r.Rest;
					}

					return ParserResult<TToken, IEnumerable<T>>.Success(remainder, result);
				};
		}

		#endregion
	}

}