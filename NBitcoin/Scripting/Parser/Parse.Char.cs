using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NBitcoin.Scripting.Parser
{
	internal static partial class Parse
	{
		private const string ValidHex = "0123456789abcdef";
		private const string ValidBase58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
		private readonly static char[] ValidHexChars;
		private readonly static char[] ValidBase58Chars;

		static Parse()
		{
			ValidHexChars = ValidHex.ToCharArray();
			ValidBase58Chars = ValidBase58.ToCharArray();
		}
		public static Parser<char, char> Char(Func<char, bool> predicate, string expected)
		{
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return (i, n) =>
			{
				if (i.AtEnd)
				{
					return ParserResult<char, char>.Failure(i, new[] { expected }, "Unexpected end of input");
				}

				if (predicate(i.GetCurrent()))
					return ParserResult<char, char>.Success(i.Advance(), i.GetCurrent());

				return ParserResult<char, char>.Failure(i, new[] { expected }, $"Unexpected '{i.GetCurrent()}'");
			};
		}
		public static Parser<char, char> CharExcept(Func<char, bool> predicate, string description)
			=> Char(c => !predicate(c), $"any character except {description}");

		public static Parser<char, char> Char(char c)
			=> Char(ch => c == ch, char.ToString(c));

		public static Parser<char, char> Chars(params char[] c)
			=> Char(c.Contains, string.Join("|", c));

		public static Parser<char, char> Chars(string c)
			=> Char(c.ToCharArray().Contains, string.Join("|", c));

		public static Parser<char, char> CharExcept(char c)
			=> CharExcept(ch => c == ch, c.ToString());

		public static readonly Parser<char, char> AnyChar = Char(c => true, "any charactor");
		public static readonly Parser<char, char> WhiteSpace = Char(char.IsWhiteSpace, "whitespace");
		public static readonly Parser<char, char> Digit = Char(char.IsDigit, "digit");
		public static readonly Parser<char, char> Letter = Char(char.IsLetter, "letter");
		public static readonly Parser<char, char> LetterOrDigit = Char(char.IsLetterOrDigit, "letter or digit");
		public static readonly Parser<char, char> Hex =
			Char(c => ValidHexChars.Any(ca => c == ca), "hex");
		public static readonly Parser<char, char> Base58 =
			Char(c => ValidBase58Chars.Any(ca => c == ca), "hex");

		public static readonly Parser<char, char> Numeric = Char(char.IsNumber, "numeric character");

		/// <summary>
		/// Parse a string of characters.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static Parser<char, IEnumerable<char>> String(string s)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));

			return s
				.ToCharArray()
				.Select(Char)
				.Sequence();
		}

		public static Parser<char, T> Token<T>(this Parser<char, T> parser)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));

			return from leading in WhiteSpace.Many()
				   from item in parser
				   from trailing in WhiteSpace.Many()
				   select item;
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

		/// <summary>
		/// Parse a number.
		/// </summary>
		public static readonly Parser<char, string> Number = Numeric.AtLeastOnce().Text();

		static Parser<char, string> DecimalWithoutLeadingDigits(CultureInfo ci = null)
		{
			return from nothing in Return<char, string>("")
					   // dummy so that CultureInfo.CurrentCulture is evaluated later
				   from dot in String((ci ?? CultureInfo.InvariantCulture).NumberFormat.NumberDecimalSeparator).Text()
				   from fraction in Number
				   select dot + fraction;
		}

		static Parser<char, string> DecimalWithLeadingDigits(CultureInfo ci = null)
		{
			return Number.Then(n => DecimalWithoutLeadingDigits(ci).XOr(Return<char, string>("")).Select(f => n + f));
		}

		/// <summary>
		/// Parse a decimal number with separator '.'.
		/// </summary>
		public static readonly Parser<char, string> Decimal = DecimalWithLeadingDigits()
																	 .XOr(DecimalWithoutLeadingDigits());
	}
}
