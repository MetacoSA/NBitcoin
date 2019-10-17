
using System;
using NBitcoin.Scripting.Miniscript.Types;

namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// Basic type representing where the fragment can go
	/// </summary>
	public enum Base
	{
		/// <summary>
		/// Takes its inputs from the top of the stack.
		/// Pushes nonzero if the condition is statisfied. If not, if it
		/// does not abort, then 0 is pushed.
		/// </summary>
		B,
		/// <summary>
		/// Takes its inputs from the top of the stack. Pushes a
		/// public key, regardless of satisfaction, onto the stack.
		/// Must be wrapped in `c:` to turn into any other type.
		/// </summary>
		K,
		/// <summary>
		/// Takes its inputs from the top of the stack, which
		/// must satisfy the condition (will abort otherwise).
		/// Does not push anything onto the stack.
		/// </summary>
		V,
		/// <summary>
		/// Takes from the stack its inputs + element X at the top.
		/// If the inputs satisfy the condition, [nonzero X] or
		/// [X nonzero] is pushed. If not, if it does not abort,
		/// then [0 X] or [X 0] is pushed.
		/// </summary>
		W
	}

	public enum Input
	{
		Zero,
		One,
		Any,
		OneNoneZero,
		AnyNonZero
	}

	public static partial class InputExtensions
	{
		public static bool IsSubtype(this Input self, Input other)
		{
			if (self == other)
				return true;

			if (self == Input.OneNoneZero && other == Input.One)
				return true;

			if (self == Input.OneNoneZero && other == Input.AnyNonZero)
				return true;

			if (other == Input.Any)
				return true;

			return false;
		}
	}

	public class Correctness
	{
		public Base Base;
		public Input Input;
		public bool DisSatisfiable;
		/// <summary>
		/// Whether the fragment's "nonzero' output on satisfaction is
		/// always the constant 1.
		/// </summary>
		public bool Unit;

		public bool IsSubtype(Correctness other) =>
			(this.Base == other.Base) &&
			(this.Input.IsSubtype(other.Input)) &&
			(!(!(this.DisSatisfiable) && other.DisSatisfiable)) &&
			(!(!(this.Unit) && other.Unit));
	}

	public class MiniscriptFragmentType
	{
		public Correctness Correctness;

		internal Malleability Malleability;

		public bool IsSubtype(MiniscriptFragmentType other) =>
			this.Correctness.IsSubtype(other.Correctness) &&
			this.Malleability.IsSubtype(other.Malleability);

		public MiniscriptFragmentType TypeCheck(Terminal fragment, Func<UInt64, MiniscriptFragmentType> child)
		{
			throw new Exception(("TODO"));
		}

	}

	public class Miniscript
	{
		internal readonly Terminal Node;
		public readonly MiniscriptFragmentType Type;

		/// <summary>
		/// Additional information helpful for extra analysis.
		/// </summary>
		/// <returns></returns>
		internal readonly ExtData Ext;

		private Miniscript(MiniscriptFragmentType type, Terminal node, ExtData ext)
		{
			Type = type;
			Node = node;
			Ext = ext;
		}

		public static Miniscript FromAst(Terminal t) =>
			new Miniscript(
				MiniscriptFragmentType.TypeCheck(t, (_) => null),
				t,
				ExtData.TypeCheck(t, (_) => null)
			);
	}
}
