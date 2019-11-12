using System;
using System.Diagnostics;
using NBitcoin.Scripting.Miniscript.Types;

namespace NBitcoin.Scripting.Miniscript
{
	[DebuggerDisplay("{" + nameof(ToDebugString) + "()}")]
	public partial class Miniscript<TPk, TPKh>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		internal readonly Terminal<TPk, TPKh> Node;
		public readonly MiniscriptFragmentType Type;

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

		public static Miniscript<TPk, TPKh> FromAst(Terminal<TPk, TPKh> t)
		{
			return
				new Miniscript<TPk, TPKh>(
					Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(t),
					t,
					Property<ExtData, TPk, TPKh>.TypeCheck(t)
				);
		}

		public static Miniscript<TPk, TPKh> FromTree(Tree t)
		{
			var inner = Terminal<TPk, TPKh>.FromTree(t);
			return new Miniscript<TPk, TPKh>(
				Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(inner),
				inner,
				Property<ExtData, TPk, TPKh>.TypeCheck(inner)
				);
		}
		public static Miniscript<TPk, TPKh> Parse(string str)
		{
			var inner = Terminal<TPk, TPKh>.FromTree(Tree.Parse(str));
			var ms = new Miniscript<TPk, TPKh>(
				Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(inner),
				inner,
				Property<ExtData, TPk, TPKh>.TypeCheck(inner)
				);
			if (ms.Type.Correctness.Base != Base.B)
				throw new MiniscriptException.NonTopLevel(ms.Type.Correctness.Base.ToString("G"));
			return ms;
		}

		public Script ToScript() =>
			Node.ToScript();

		public override string ToString()
			=> this.Node.ToString();

		public string ToDebugString() =>
			Node.ToDebugString();
	}
}
