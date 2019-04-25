using System;
using System.Collections.Generic;

namespace NBitcoin.Miniscript.Parser
{
	internal delegate ParserResult<TToken, TValue> Parser<TToken, TValue>(IInput<TToken> input);

	internal delegate ParserResult<char, TValue> Parser<TValue>(IInput<char> input);
	public static class ParserExtension
	{
		/// <summary>
		/// Tries to parse the input without throwing an exception.
		/// </summary>
		/// <typeparam name="T">The type of the result.</typeparam>
		/// <param name="parser">The parser.</param>
		/// <param name="input">The input.</param>
		/// <returns>The result of the parser</returns>
		internal static ParserResult<char, T> TryParse<T>(this Parser<char, T> parser, string input)
		{
			if (parser == null)
				throw new System.ArgumentNullException(nameof(parser));

			if (input == null)
				throw new System.ArgumentNullException(nameof(input));

			return parser(new StringInput(input));
		}

		/// <summary>
		/// Parses the specified input string.
		/// </summary>
		/// <typeparam name="T">The type of the result.</typeparam>
		/// <param name="parser">The parser.</param>
		/// <param name="input">The input.</param>
		/// <returns>The result of the parser.</returns>
		/// <exception cref="ParseException">It contains the details of the parsing error.</exception>
		internal static T Parse<T>(this Parser<char, T> parser, string input)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (input == null) throw new ArgumentNullException(nameof(input));

			var result = parser.TryParse(input);

			if (!result.IsSuccess)
				throw new ParseException(result.ToString(), result.Rest.Position);
			return result.Value;
		}

		internal static ParserResult<ScriptToken, T> TryParse<T>(this Parser<ScriptToken, T> parser, Script input)
		{
			if (parser == null)
				throw new System.ArgumentNullException(nameof(parser));

			if (input == null)
				throw new System.ArgumentNullException(nameof(input));

			return parser(new ScriptInput(input));
		}

		internal static T Parse<T>(this Parser<ScriptToken, T> parser, Script input)
		{
			if (parser == null) throw new ArgumentNullException(nameof(parser));
			if (input == null) throw new ArgumentNullException(nameof(input));

			var result = parser.TryParse(input);

			if (!result.IsSuccess)
				throw new ParseException(result.ToString(), result.Rest.Position);
			return result.Value;
		}
	}
}