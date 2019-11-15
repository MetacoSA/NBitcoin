using System;
using System.Collections.Generic;
using System.Diagnostics;
using NBitcoin.Scripting.Miniscript.Types;
using NBitcoin.Scripting.Parser;

namespace NBitcoin.Scripting.Miniscript
{
	[DebuggerDisplay("{" + nameof(ToDebugString) + "()}")]
	public partial class Miniscript<TPk, TPKh> : IEquatable<Miniscript<TPk, TPKh>>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		internal readonly Terminal<TPk, TPKh> Node;
		internal readonly MiniscriptFragmentType Type;

		/// <summary>
		/// Additional information helpful for extra analysis.
		/// </summary>
		/// <returns></returns>
		internal readonly ExtData Ext;

		internal Miniscript(MiniscriptFragmentType type, Terminal<TPk, TPKh> node, ExtData ext)
		{
			Type = type;
			Node = node;
			Ext = ext;
		}

		internal static Miniscript<TPk, TPKh> FromAst(Terminal<TPk, TPKh> t)
		{
			var errors = new List<FragmentPropertyException>();
			if (Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(t, out var fragmentType, errors)
				&& Property<ExtData, TPk, TPKh>.TypeCheck(t, out var extData, errors))
				return
					new Miniscript<TPk, TPKh>(
						fragmentType,
						t,
						extData
					);
			throw errors.Flatten();
		}

		internal static Miniscript<TPk, TPKh> FromTree(Tree t)
		{
			var inner = Terminal<TPk, TPKh>.FromTree(t);
			return FromAst(inner);
		}
		public static Miniscript<TPk, TPKh> Parse(string str)
		{
			try
			{
				var ms = FromTree(Tree.Parse(str));
				if (ms.Type.Correctness.Base != Base.B)
					throw new MiniscriptException.NonTopLevel(ms.Type.Correctness.Base.ToString("G"));
				return ms;
			} catch(FragmentPropertyException ex) {
				throw new ParsingException("Failed to parse miniscript from ast", ex);
			}
		}

		public Script ToScript() =>
			Node.ToScript();

		public override string ToString()
			=> this.Node.ToString();

		public string ToDebugString() =>
			Node.ToDebugString();

		public bool Equals(Miniscript<TPk, TPKh> other)
			=> this.Node.Equals(other.Node);
	}
}
