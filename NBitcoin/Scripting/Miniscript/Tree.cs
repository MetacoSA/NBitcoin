using System;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// A token of the from `x()` or `x`
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Tree<T>
	{
		public readonly string Name;
		public readonly Tree<T>[] Args;

		/// <summary>
		/// Attempts to parse a terminal expression.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static T Terminal(Tree<T> term, Func<string, T> converter)
		{
			if (term.Args.Length == 0)
				converter.Invoke(term.Name);
			throw new ParsingException(term.Name);
		}

		public static Tree<T> Parse(string str)
		{
			var topAndRemaining = FromSlice(str);
			var top = topAndRemaining.Item1;
			var remaining = topAndRemaining.Item2;
			if (remaining.Length == 0)
				throw new NotImplementedException();
			else
				throw new ParsingException(remaining);
		}

		private static Tuple<Tree<T>, string> FromSlice(string str)
		{
			throw new NotImplementedException();
		}
	}
}
