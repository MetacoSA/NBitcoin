using System;
using NBitcoin.Scripting.Miniscript.Types;

namespace NBitcoin.Scripting.Miniscript
{
	public partial class Miniscript<TPk> where TPk : IMiniscriptKey
	{
		internal readonly Terminal<TPk> Node;
		public readonly MiniscriptFragmentType Type;

		/// <summary>
		/// Additional information helpful for extra analysis.
		/// </summary>
		/// <returns></returns>
		internal readonly ExtData Ext;

		internal Miniscript(MiniscriptFragmentType type, Terminal<TPk> node, ExtData ext)
		{
			Type = type;
			Node = node;
			Ext = ext;
		}

		public static Miniscript<TPk> FromAst(Terminal<TPk> t)
		{
			MiniscriptFragmentType ty = default;
			ExtData ext = default;

			return
				new Miniscript<TPk>(
					ty.TypeCheck(t, (_) => null),
					t,
					ext.TypeCheck(t, (_) => null)
				);
		}

		public static Miniscript<TPk> Parse(string str)
		{
			var inner = MiniscriptDSLParser.ParseDSL<TPk>(str);
			MiniscriptFragmentType ty = default;
			ExtData ext = default;
			new Miniscript<TPk>(ty.TypeCheck<MiniscriptFragmentType, TPk>(inner));
		}

		public Script Encode() =>
			Node.ToScript();

	}
}
