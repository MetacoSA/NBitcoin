using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using NBitcoin.Scripting.Miniscript.Types;

namespace NBitcoin.Scripting.Miniscript.Policy
{
	public class CompilerException : Exception
	{}

	/// <summary>
	/// This represents the state of the best possible compilation
	/// of a given policy(implicitly keyed).
	/// </summary>
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
			this.Type.IsSubtype(other.Type) &&
			this.ExpensiveVerify == other.ExpensiveVerify &&
			this.DissatProb == other.DissatProb;

		private CompilationKey(MiniscriptFragmentType type, double? dissatProb, bool expensiveVerify)
		{
			Type = type;
			DissatProb = dissatProb;
			ExpensiveVerify = expensiveVerify;
		}

	}

	internal class CompilerExtData : IProperty<CompilerExtData>
	{
		/// <summary>
		/// If this node is the direct child of a disjunction, this field must have
		/// the probability of its branch being taken. Otherwise it is ignored.
		/// All functions initialize it to `None` .
		/// </summary>
		internal readonly double? BranchProb;

		/// <summary>
		/// The number of bytes needed to satisfy the fragment in segwit format
		/// (total length of all witness pushes, plus their own length prefixes)
		/// </summary>
		internal readonly double SatisfyCost;
		internal readonly double? DissatCost;
	}

	internal class AstElemExt<TPk> where TPk : IMiniscriptKey
	{
		internal readonly Miniscript<TPk> Ms;
		internal readonly CompilerExtData CompExtData;

		public AstElemExt(CompilerExtData compilerExtData, Miniscript<TPk> ms)
		{
			CompExtData = compilerExtData;
			Ms = ms;
		}

		/// <summary>
		/// Compute a one dimensional cost, given a probability of satisfaction and
		/// a probability of dissatisfaction. if `dissatProb` is `null` then it is
		/// assumed that dissatisfaction never occurs
		/// </summary>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		/// <returns></returns>
		internal double Cost1d(double satProb, double? dissatProb)
		{
			var dissatCost = 0.0;
			if (dissatProb.HasValue && CompExtData.DissatCost.HasValue)
				dissatCost = dissatProb.Value * CompExtData.DissatCost.Value;
			else if (dissatProb.HasValue && !CompExtData.DissatCost.HasValue)
				dissatCost = double.PositiveInfinity;
			return
				(double)this.Ms.Ext.PkCost +
				(this.CompExtData.SatisfyCost * satProb) +
				dissatCost;

		}

		internal static AstElemExt<TPk> Terminal(Terminal<TPk> ast)
		{
			CompilerExtData compExtData = default;
			Miniscript<TPk> ms = default;
			return new AstElemExt<TPk>(compExtData.TypeCheck(ast), Miniscript<TPk>.FromAst(ast));
		}

		internal static AstElemExt<TPk> Binary(Terminal<TPk> ast, AstElemExt<TPk> l, AstElemExt<TPk> r)
		{
			MiniscriptFragmentType ty = default;
			ty = ty.TypeCheck(ast);
			ExtData ext = default;
			ext = ext.TypeCheck(ast);

			Func<int, CompilerExtData> lookupExt =
				(int n) => n == 0 ? l.CompExtData : n == 1 ? r.CompExtData : throw new Exception("Unreachable");
			CompilerExtData compExtData = default;
			compExtData = compExtData.TypeCheck(ast, lookupExt);

			return new AstElemExt<TPk>(compExtData, new Miniscript<TPk>(ty, ast, ext));
		}

		internal static AstElemExt<TPk> Ternary(
			Terminal<TPk> ast,
			AstElemExt<TPk> a,
			AstElemExt<TPk> b,
			AstElemExt<TPk> c
		)
		{
			Func<int, CompilerExtData> lookupExt =
				n =>
					n == 0 ? a.CompExtData :
					n == 1 ? b.CompExtData :
					n == 2 ? c.CompExtData :
					throw new Exception("Unreachable");

			MiniscriptFragmentType ty = default;
			ty = ty.TypeCheck(ast);
			ExtData ext = default;
			ext = ext.TypeCheck(ast);
			CompilerExtData compilerExtData = default;
			compilerExtData = compilerExtData.TypeCheck(ast, lookupExt);

			return new AstElemExt<TPk>(compilerExtData, new Miniscript<TPk>(ty, ast, ext));
		}
	}

	internal class Cast<TPk> where TPk : IMiniscriptKey
	{
		public Func<Miniscript<TPk>, Terminal<TPk>> Node;
		public Func<MiniscriptFragmentType> AstType;
		public Func<ExtData> ExtData;
		public Func<CompilerExtData> CompilerExtData;

		public Cast(Func<ExtData> extData, Func<MiniscriptFragmentType> astType, Func<Miniscript<TPk>, Terminal<TPk>> node, Func<CompilerExtData> compilerExtData)
		{
			ExtData = extData;
			AstType = astType;
			Node = node;
			CompilerExtData = compilerExtData;
		}

		public AstElemExt<TPk> InvokeCast(AstElemExt<TPk> ast)
		=>
			new AstElemExt<TPk>(
				ms: new Miniscript<TPk>(
					this.AstType.Invoke(ast.Ms.Type),
					this.Node.Invoke(ast.Ms),
					this.ExtData.Invoke(ast.Ms.Ext)

				),
				compilerExtData:this.CompilerExtData.Invoke(ast.CompExtData));

		public List<Cast<TPk>> AllCasts()
		{
			ExtData extD = default;
			Terminal<TPk> term = default;
			MiniscriptFragmentType astType = default;
			CompilerExtData compilerExtData = default;
			return
				new List<Cast<TPk>>()
				{
					new Cast<TPk>(
						extD.CastCheck,
						astType.TypeCheck,
						Terminal<TPk>.NewCheck,
						compilerExtData.CastCheck
					),
					new Cast<TPk>()
				};
		}
	}

	public static class Compiler
	{

	}
}
