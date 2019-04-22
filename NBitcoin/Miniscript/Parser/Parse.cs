using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NBitcoin.Miniscript.Parser
{
	internal static class Parse
	{


		# region char parsers
		public static Parser<char> Char(Func<char, bool> predicate, string expected)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return i =>
			{
				if (i.AtEnd)
				{
					return ParserResult<char>.Failure(i, new [] {expected}, "Unexpected end of input");
				}

				if (predicate(i.Current))
					return ParserResult<char>.Success(i.Advance(), i.Current);

				return ParserResult<char>.Failure(i, new [] {expected}, $"Unexpected '{i.Current}'");
			};
		}
		public static Parser<char> CharExcept(Func<char, bool> predicate, string description)
			=> Char(c => !predicate(c), $"any character except {description}");

		public static Parser<char> Char(char c)
			=> Char(ch => c == ch, char.ToString(c));

		public static Parser<char> Chars(params char[] c)
			=> Char(c.Contains, string.Join("|", c));

		public static Parser<char> Chars(string c)
			=> Char(c.AsEnumerable().Contains, string.Join("|", c));

		public static Parser<char> CharExcept(char c)
			=> CharExcept(ch => c == ch, c.ToString());

		public static readonly Parser<char> AnyChar = Char(c => true, "any charactor");
		public static readonly Parser<char> WhiteSpace = Char(char.IsWhiteSpace, "whitespace");
		public static readonly Parser<char> Digit = Char(char.IsDigit, "digit");
		public static readonly Parser<char> Letter = Char(char.IsLetter, "letter");
		public static readonly Parser<char> LetterOrDigit = Char(char.IsLetterOrDigit, "letter or digit");

		public static readonly Parser<char> Numeric = Char(char.IsNumber, "numeric character");

		/// <summary>
		/// Parse a string of characters.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static Parser<IEnumerable<char>> String(string s)
		{
			if (s == null) throw new ArgumentNullException(nameof(s));

			return s
				.AsEnumerable()
				.Select(Char)
				.Sequence();
		}

		public static Parser<IEnumerable<T>> Sequence<T>(this IEnumerable<Parser<T>> parserList)
		{
			return
				parserList
					.Aggregate(Return(Enumerable.Empty<T>()), (a, p) => a.Concat(p.Once()));
		}


		#endregion


		#region generic utilities

		public static Parser<U> Then<T, U>(this Parser<T> first, Func<T, Parser<U>> second)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));
			if (second == null)
				throw new ArgumentNullException(nameof(second));

			return i => first(i).IfSuccess(s => second(s.Value)(s.Rest));
		}

		public static Parser<IEnumerable<T>> Many<T>(this Parser<T> parser)
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

				return ParserResult<IEnumerable<T>>.Success(rest, result);
			};
		}

		/// <summary>
		/// Parse a stream of elements, failing if any element is only partially parsed.
		/// </summary>
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
		public static Parser<IEnumerable<T>> XMany<T>(this Parser<T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));

			return parser.Many().Then(m => parser.Once().XOr(Return(m)));
		}

		/// <summary>
		/// TryParse a stream of elements with at least one item.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parser"></param>
		/// <returns></returns>
		public static Parser<IEnumerable<T>> AtLeastOnce<T>(this Parser<T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));

			return parser.Once().Then(t1 => parser.Many().Select(ts => t1.Concat(ts)));
		}

		public static Parser<IEnumerable<T>> Once<T>(this Parser<T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			return parser.Select(r => (IEnumerable<T>)new[] { r });
		}

		public static Parser<IEnumerable<T>> AtLeastOne<T> (this Parser<T> parser)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));

			return parser.Once().Then(t1 => parser.Many().Select(ts => t1.Concat(ts)));
		}

		/// <summary>
		/// TryParse a stream of elements with at least one item. Except the first
		/// item, all other items will be matched with the <code>XMany</code> operator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parser"></param>
		/// <returns></returns>
		public static Parser<IEnumerable<T>> XAtLeastOnce<T>(this Parser<T> parser)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));

			return parser.Once().Then(t1 => parser.XMany().Select(ts => t1.Concat(ts)));
		}

		/// <summary>
		/// Lift to a parser monad world
		/// </summary>
		/// <param name="v"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Parser<T> Return<T>(T v)
			=> i => ParserResult<T>.Success(i, v);

		/// <summary>
		/// Take the result of parsing, and project it onto a different domain.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="convert"></param>
		/// <returns></returns>
		public static Parser<U> Select<T, U>(this Parser<T> parser, Func<T, U> convert)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (convert == null) throw new ArgumentNullException(nameof(convert));

			return parser.Then(t => Return(convert(t)));
		}

		public static Parser<T> Token<T>(this Parser<T> parser)
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
		/// <typeparam name="T"></typeparam>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static Parser<T> Ref<T>(Func<Parser<T>> reference)
		{
			if (reference == null) throw new ArgumentNullException(nameof(reference));

			Parser<T> p = null;

			return i =>
				{
					if (p == null)
						p = reference();

					if (i.Memos.ContainsKey(p))
						throw new ParseException(i.Memos[p].ToString());

					i.Memos[p] = ParserResult<T>.Failure(i,
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
		public static Parser<string> Text(this Parser<IEnumerable<char>> characters)
		{
			return characters.Select(chs => new string(chs.ToArray()));
		}

		public static Parser<T> Or<T>(this Parser<T> first, Parser<T> second) 
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
		/// <typeparam name="T"></typeparam>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public static Parser<T> XOr<T> (this Parser<T> first, Parser<T> second)
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
		/// <typeparam name="T"></typeparam>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public static Parser<IEnumerable<T>> Concat<T>(this Parser<IEnumerable<T>> first, Parser<IEnumerable<T>> second)
		{
			if (first == null) throw new ArgumentNullException(nameof(first));
			if (second == null) throw new ArgumentNullException(nameof(second));

			return first.Then(f => second.Select(f.Concat));
		}

		public static ParserResult<T> DetermineBestError<T>(ParserResult<T> firstF, ParserResult<T> secondF)
		{
			if (secondF.Rest.Position > firstF.Rest.Position)
				return secondF;

			if (secondF.Rest.Position == firstF.Rest.Position)
				return ParserResult<T>.Failure(
					firstF.Rest,
					firstF.Expected.Union(secondF.Expected),
					firstF.Description
				);

			return firstF;
		}


		/// <summary>
		/// Version of Return with simpler inline syntax.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Parser<U> Return<T, U>(this Parser<T> parser, U value)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			return parser.Select(t => value);
		}


		/// <summary>
		/// Attempt parsing only if the <paramref name="except"/> parser fails.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="except"></param>
		/// <returns></returns>
		public static Parser<T> Except<T, U>(this Parser<T> parser, Parser<U> except)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (except == null) throw new ArgumentNullException(nameof(except));

			// Could be more like: except.Then(s => s.Fail("..")).XOr(parser)
			return i =>
				{
					var r = except(i);
					if (r.IsSuccess)
						return ParserResult<T>.Failure(i, new[] { "other than the excepted input"} , "Excepted parser succeeded." );
					return parser(i);
				};
		}

		/// <summary>
		/// Parse a sequence of items until a terminator is reached.
		/// Returns the sequence, discarding the terminator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="parser"></param>
		/// <param name="until"></param>
		/// <returns></returns>
		public static Parser<IEnumerable<T>> Until<T, U>(this Parser<T> parser, Parser<U> until)
		{
			return parser.Except(until).Many().Then(r => until.Return(r));
		}


		/// <summary>
		/// Succeed if the parsed value matches predicate.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parser"></param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public static Parser<T> Where<T>(this Parser<T> parser, Func<T, bool> predicate)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			return i => parser(i).IfSuccess(s =>
				predicate(s.Value) ? s : ParserResult<T>.Failure(i,
					new string[0],
					string.Format("Unexpected {0}.", s.Value)
					)
				);
		}


		/// <summary>
		/// Monadic combinator Then, adapted for Linq comprehension syntax.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <typeparam name="V"></typeparam>
		/// <param name="parser"></param>
		/// <param name="selector"></param>
		/// <param name="projector"></param>
		/// <returns></returns>
		public static Parser<V> SelectMany<T, U, V>(
			this Parser<T> parser,
			Func<T, Parser<U>> selector,
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
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<T> ChainOperator<T, TOp>(
			Parser<TOp> op,
			Parser<T> operand,
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
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<T> XChainOperator<T, TOp>(
			Parser<TOp> op,
			Parser<T> operand,
			Func<TOp, T, T, T> apply)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return operand.Then(first => ChainOperatorRest(first, op, operand, apply, XOr));
		}

		static Parser<T> ChainOperatorRest<T, TOp>(
			T firstOperand,
			Parser<TOp> op,
			Parser<T> operand,
			Func<TOp, T, T, T> apply,
			Func<Parser<T>, Parser<T>, Parser<T>> or)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return or(op.Then(opvalue =>
						  operand.Then(operandValue =>
							  ChainOperatorRest(apply(opvalue, firstOperand, operandValue), op, operand, apply, or))),
					  Return(firstOperand));
		}

		/// <summary>
		/// Chain a right-associative operator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<T> ChainRightOperator<T, TOp>(
			Parser<TOp> op,
			Parser<T> operand,
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
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TOp"></typeparam>
		/// <param name="op"></param>
		/// <param name="operand"></param>
		/// <param name="apply"></param>
		/// <returns></returns>
		public static Parser<T> XChainRightOperator<T, TOp>(
			Parser<TOp> op,
			Parser<T> operand,
			Func<TOp, T, T, T> apply)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return operand.Then(first => ChainRightOperatorRest(first, op, operand, apply, XOr));
		}

		static Parser<T> ChainRightOperatorRest<T, TOp>(
			T lastOperand,
			Parser<TOp> op,
			Parser<T> operand,
			Func<TOp, T, T, T> apply,
			Func<Parser<T>, Parser<T>, Parser<T>> or)
		{
			if (op == null) throw new ArgumentNullException(nameof(op));
			if (operand == null) throw new ArgumentNullException(nameof(operand));
			if (apply == null) throw new ArgumentNullException(nameof(apply));
			return or(op.Then(opvalue =>
						operand.Then(operandValue =>
							ChainRightOperatorRest(operandValue, op, operand, apply, or)).Then(r =>
								Return(apply(opvalue, lastOperand, r)))),
					  Return(lastOperand));
		}

		/// <summary>
		/// Parse a number.
		/// </summary>
		public static readonly Parser<string> Number = Numeric.AtLeastOnce().Text();

		static Parser<string> DecimalWithoutLeadingDigits(CultureInfo ci = null)
		{
			return from nothing in Return("")
					   // dummy so that CultureInfo.CurrentCulture is evaluated later
				   from dot in String((ci ?? CultureInfo.CurrentCulture).NumberFormat.NumberDecimalSeparator).Text()
				   from fraction in Number
				   select dot + fraction;
		}

		static Parser<string> DecimalWithLeadingDigits(CultureInfo ci = null)
		{
			return Number.Then(n => DecimalWithoutLeadingDigits(ci).XOr(Return("")).Select(f => n + f));
		}

		/// <summary>
		/// Parse a decimal number using the current culture's separator character.
		/// </summary>
		public static readonly Parser<string> Decimal = DecimalWithLeadingDigits().XOr(DecimalWithoutLeadingDigits());

		/// <summary>
		/// Parse a decimal number with separator '.'.
		/// </summary>
		public static readonly Parser<string> DecimalInvariant = DecimalWithLeadingDigits(CultureInfo.InvariantCulture)
																	 .XOr(DecimalWithoutLeadingDigits(CultureInfo.InvariantCulture));

		#endregion

		# region sequence
		public static Parser<IEnumerable<T>> DelimitedBy<T, U> (this Parser<T> parser, Parser<U> delimiter)
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

		public static Parser<IEnumerable<T>> XDelimitedBy<T, U>(this Parser<T> itemParser, Parser<U> delimiter)
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

		public static Parser<IEnumerable<T>> Repeat<T>(this Parser<T> parser, int count)
			=> Repeat<T>(parser, count, count);

		public static Parser<IEnumerable<T>> Repeat<T> (this Parser<T> parser, int minimumCount, int maximumCount)
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
								: r.Rest.Current.ToString();

							var msg = $"Unexpected '{what}'";
							var exp = minimumCount == maximumCount
								? $"'{string.Join(", ", r.Expected)}' {minimumCount} times, but found {n}"
								: $"'{string.Join(", ", r.Expected)}' between {minimumCount} and {maximumCount} times, but found {n}";

							return ParserResult<IEnumerable<T>>.Failure(i, new[] { exp }, msg);
						}

						if (!ReferenceEquals(remainder, r.Rest))
						{
							result.Add(r.Value);
						}

						remainder = r.Rest;
					}

					return ParserResult<IEnumerable<T>>.Success(remainder, result);
				};
		}

		#endregion
	}

}