using System;
using NBitcoin.Scripting.Miniscript.Types;

namespace NBitcoin.Scripting.Miniscript
{
	public partial class Miniscript<TPk, TPKh>
		where TPk : IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
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
			MiniscriptFragmentType ty = default;
			ExtData ext = default;

			return
				new Miniscript<TPk, TPKh>(
					ty.TypeCheck(t, (_) => null),
					t,
					ext.TypeCheck(t, (_) => null)
				);
		}

		public static Miniscript<TPk, TPKh> Parse(string str)
		{
			var inner = MiniscriptDSLParser<TPk, TPKh>.ParseTerminal(str);
			MiniscriptFragmentType ty = default;
			ExtData ext = default;
			return new Miniscript<TPk, TPKh>(
				ty.TypeCheck(inner),
				inner,
				ext.TypeCheck(inner)
				);
		}

		public Script ToScript() =>
			Node.ToScript();

	}
}
