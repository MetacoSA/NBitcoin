using System;
using System.Collections.Generic;
using NBitcoin.Scripting.Parser;
using System.Linq;

namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// A token of the from `x()` or `x`
	/// </summary>
	public class Tree
	{
		public readonly string Name;
		public readonly List<Tree> Args;

		public Tree(string name, List<Tree> args)
		{
			Name = name;
			Args = args;
		}


		public static Tree Parse(string str)
		{
			var topAndRemaining = FromSlice(str);
			var top = topAndRemaining.Item1;
			var remaining = topAndRemaining.Item2;
			if (remaining.Length == 0)
				return top;
			throw new ParsingException($"There was remaining input after parsing. Remaining: {remaining}");
		}

		private enum Found
		{
			LParen,
			Nothing,
			Comma,
			RParen
		}
		private static Tuple<Tree, string> FromSlice(string str)
		{
			var found = new { Found = Found.Nothing, Pos = 0 };
			for (var n = 0; n < str.Length; n++)
			{
				var ch = str[n];
				if (ch == '(') { found = new { Found = Found.LParen, Pos = n}; break; }
				if (ch == ',') { found = new { Found = Found.Comma, Pos = n}; break; }
				if (ch == ')') { found = new { Found = Found.RParen, Pos = n}; break; }
			}

			switch (found.Found)
			{
				case Found.Nothing: return new Tuple<Tree, string>(new Tree(str, new List<Tree>()), "");
				case Found.Comma:
					var strToComma = str.Substring(0, found.Pos);
					var strFromComma = str.Substring(found.Pos);
					return new Tuple<Tree, string>(new Tree(strToComma, new List<Tree>()), strFromComma);
				case Found.RParen:
					var strToRParen = str.Substring(0, found.Pos);
					var strFromRParen = str.Substring(found.Pos);
					return new Tuple<Tree, string>(new Tree(strToRParen, new List<Tree>()), strFromRParen);
				case Found.LParen:
					var strToLParen = str.Substring(0, found.Pos);
					var ret = new Tree(strToLParen, new List<Tree>());
					str = str.Substring(found.Pos + 1);
					while (true)
					{
						var t = Tree.FromSlice(str);
						var remain = t.Item2;
						ret.Args.Add(t.Item1);
						if (remain.Length == 0)
							throw new ParsingException("Miniscript does not terminate with )");
						str = remain.Substring(1);
						if (remain[0] == ',') {}
						else if (remain[0] == ')') break;
						else throw new ParsingException($"expecting char , (comma) . but got {remain[0]}");
					}
					return Tuple.Create(ret, str);
			}
			throw new NotImplementedException();
		}

		internal static T Terminal<T>(Tree term, Func<string, T> convert)
		{
			if (term.Args.Count == 0)
			{
				try
				{
					return convert(term.Name);
				}
				catch (NotSupportedException ex)
				{
					throw new ParsingException(term.Name, ex);
				}
			}
			throw new ParsingException(term.Name);
		}

		internal static Terminal<TPk, TPKh> Binary<TPk, TPKh>(
			Tree term,
			Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> constructor)
			where TPk : class, IMiniscriptKey<TPKh>, new()
			where TPKh : class, IMiniscriptKeyHash, new()
		{
			if (term.Args.Count == 2)
			{
				var left = Miniscript<TPk, TPKh>.FromTree(term.Args[0]);
				var right = Miniscript<TPk, TPKh>.FromTree(term.Args[1]);
				return constructor(left, right);
			}
			throw new ParsingException(term.Name);
		}

		internal static Terminal<TPk, TPKh> Ternary<TPk, TPKh>(
			Tree term,
			Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>,Terminal<TPk, TPKh>> constructor)
			where TPk : class, IMiniscriptKey<TPKh>, new()
			where TPKh : class, IMiniscriptKeyHash, new()
		{
			if (term.Args.Count == 3)
			{
				var a = Miniscript<TPk, TPKh>.FromTree(term.Args[0]);
				var b = Miniscript<TPk, TPKh>.FromTree(term.Args[1]);
				var c = Miniscript<TPk, TPKh>.FromTree(term.Args[2]);
				return constructor(a, b, c);
			}
			throw new ParsingException(term.Name);
		}
	}
}
