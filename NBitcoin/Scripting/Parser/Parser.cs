using System;
using System.Collections.Generic;

namespace NBitcoin.Scripting.Parser
{
	internal delegate ParserResult<TToken, TValue> Parser<TToken, TValue>(IInput<TToken> input, Network network);

	internal delegate ParserResult<char, TValue> Parser<TValue>(IInput<char> input);
	internal static class ParserExtension
	{
		/// <summary>
		/// Tries to parse the input without throwing an exception.
		/// </summary>
		/// <typeparam name="T">The type of the result.</typeparam>
		/// <param name="parser">The parser.</param>
		/// <param name="input">The input.</param>
		/// <returns>The result of the parser</returns>
		internal static ParserResult<char, T> TryParse<T>(this Parser<char, T> parser, string input, Network network)
		{
			if (parser == null)
				throw new System.ArgumentNullException(nameof(parser));

			if (input == null)
				throw new System.ArgumentNullException(nameof(input));

			return parser(new StringInput(input), network);
		}

		/// <summary>
		/// Parses the specified input string.
		/// </summary>
		/// <typeparam name="T">The type of the result.</typeparam>
		/// <param name="parser">The parser.</param>
		/// <param name="input">The input.</param>
		/// <returns>The result of the parser.</returns>
		/// <exception cref="ParsingException">It contains the details of the parsing error.</exception>
		internal static T Parse<T>(this Parser<char, T> parser, string input, Network network)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			var result = parser.TryParse(input, network);

			if (!result.IsSuccess)
				throw new ParsingException(result.ToString(), result.Rest.Position);
			return result.Value;
		}

		internal static ParserResult<ScriptToken, T> TryParse<T>(this Parser<ScriptToken, T> parser, Network network, Script input)
		{
			if (parser == null)
				throw new System.ArgumentNullException(nameof(parser));

			if (input == null)
				throw new System.ArgumentNullException(nameof(input));

			try
			{
				return parser(new ScriptInput(input), network);
			}
			// Catching exception here is bit ugly, but converting `Script` to `ScriptToken` is itself unsafe
			// so this is good for assuring purity of this method.
			catch (ParsingException e)
			{
				return ParserResult<ScriptToken, T>.Failure(new ScriptInput(new ScriptToken[] { }), e.Message);
			}
		}

		internal static T Parse<T>(this Parser<ScriptToken, T> parser, Script input, Network network)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			var result = parser.TryParse(network, input);

			if (!result.IsSuccess)
				throw new ParsingException(result.ToString(), result.Rest.Position);
			return result.Value;
		}
	}
}
