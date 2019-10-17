using System;

namespace NBitcoin.Scripting.Miniscript.Policy
{
	public class CompilerException : Exception
	{}

	internal class CompilationKey
	{
		/// <summary>
		/// The type of the compilation result
		/// </summary>
		internal readonly MiniscriptFragmentType Type;

		/// <summary>
		/// Whether the result cannot be easily converted into verify form.
		/// This is exactly the opposite of has_verify_from in the data-types.
		/// This is required in cases where it is important to distinguish between
		/// two Compilation of the same-type: one of which is expensive to verify and
		/// the other is not.
		/// </summary>
		internal readonly bool ExpensiveVerify;
		/// <summary>
		/// The probability of dissatisfaction of the compilation of the policy.
		/// Note that all possible compilations of a (sub)policy have the same sat-prob
		/// and only differ in DissatProb.
		/// </summary>
		internal readonly double? DissatProb;

		public bool IsSubtype(CompilationKey other) =>
			this.Type.IsSubtype(other.Type);

		private CompilationKey(MiniscriptFragmentType type, double? dissatProb, bool expensiveVerify)
		{
			Type = type;
			DissatProb = dissatProb;
			ExpensiveVerify = expensiveVerify;
		}

	}

	internal class CompilerExtData
	{
		/// <summary>
		/// If this node is the direct child of a disjunction, this field must have
		/// the probability of its branch being taken. Otherwise it is ignored.
		/// All functions initialize it to `None` .
		/// </summary>
		internal double? BranchProb;

		/// <summary>
		/// The number of bytes needed to satisfy the fragment in segwit format
		/// (total length of all witness pushes, plus their own length prefixes)
		/// </summary>
		internal double SatisfyCost;
		internal double? DissatCost;
	}

	internal class AstElemExt
	{
		internal Miniscript Ms;
		internal CompilerExtData CompilerExtData;

		internal double CostLd(double satProb, double? dissatProb)
		{
			return this.Ms
		}
	}

	public class Compiler
	{

	}
}
