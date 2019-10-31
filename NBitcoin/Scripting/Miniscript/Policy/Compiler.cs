using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using NBitcoin.Scripting.Miniscript.Types;

namespace NBitcoin.Scripting.Miniscript.Policy
{
	public class CompilerException : Exception
	{
		public static CompilerException TopLevelNonSafe =
			new CompilerException("Top level element of compilation result was not safe");
		public static CompilerException ImpossibleNonMalleableCompilation =
			new CompilerException("Top level of compilation result was malleable!");

		private CompilerException(string msg) : base(msg) {}
	}

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

		public CompilerExtData(double? branchProb, double satisfyCost, double? dissatCost)
		{
			BranchProb = branchProb;
			SatisfyCost = satisfyCost;
			DissatCost = dissatCost;
		}

		public CompilerExtData() {}

		public void SanityChecks()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromTrue()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromFalse()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromPk()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromPkH()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromMulti(int k, int pkLength)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromAfter(uint time)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromOlder(uint time)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromHash()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromSha256()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromHash256()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromRipemd160()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData FromHash160()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastAlt()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastSwap()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastCheck()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastDupIf()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastVerify()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastNonZero()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastZeroNotEqual()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastTrue()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastOrIFalse()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastUnLikely()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData CastLikely()
		{
			throw new NotImplementedException();
		}

		public CompilerExtData AndB(CompilerExtData left, CompilerExtData right)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData AndV(CompilerExtData left, CompilerExtData right)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData AndN(CompilerExtData left, CompilerExtData right)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData OrB(CompilerExtData left, CompilerExtData right)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData OrD(CompilerExtData left, CompilerExtData right)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData OrC(CompilerExtData left, CompilerExtData right)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData OrI(CompilerExtData left, CompilerExtData right)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData AndOr(CompilerExtData a, CompilerExtData b, CompilerExtData c)
		{
			throw new NotImplementedException();
		}

		public CompilerExtData Threshold(int k, int n, Func<uint, CompilerExtData> subCk)
		{
			throw new NotImplementedException();
		}
	}

	internal class AstElemExt<TPk, TPKh>
		where TPk : IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
	{
		internal readonly Miniscript<TPk, TPKh> Ms;
		internal readonly CompilerExtData CompExtData;

		public AstElemExt(CompilerExtData compilerExtData, Miniscript<TPk, TPKh> ms)
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

		internal static AstElemExt<TPk, TPKh> Terminal(Terminal<TPk, TPKh> ast)
		{
			return new AstElemExt<TPk, TPKh>(Property<CompilerExtData, TPk, TPKh>.TypeCheck(ast), Miniscript<TPk, TPKh>.FromAst(ast));
		}

		internal static AstElemExt<TPk, TPKh> Binary(Terminal<TPk, TPKh> ast, AstElemExt<TPk, TPKh> l, AstElemExt<TPk, TPKh> r)
		{
			var ty = Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(ast);
			var ext = Property<ExtData, TPk, TPKh>.TypeCheck(ast);

			Func<int, CompilerExtData> lookupExt =
				(int n) => n == 0 ? l.CompExtData : n == 1 ? r.CompExtData : throw new Exception("Unreachable");
			var compExtData = Property<CompilerExtData, TPk, TPKh>.TypeCheck(ast, lookupExt);

			return new AstElemExt<TPk, TPKh>(compExtData, new Miniscript<TPk, TPKh>(ty, ast, ext));
		}

		internal static AstElemExt<TPk, TPKh> Ternary(
			Terminal<TPk, TPKh> ast,
			AstElemExt<TPk, TPKh> a,
			AstElemExt<TPk, TPKh> b,
			AstElemExt<TPk, TPKh> c
		)
		{
			Func<int, CompilerExtData> lookupExt =
				n =>
					n == 0 ? a.CompExtData :
					n == 1 ? b.CompExtData :
					n == 2 ? c.CompExtData :
					throw new Exception("Unreachable");

			var ty = Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(ast);
			var ext = Property<ExtData, TPk, TPKh>.TypeCheck(ast);
			var compilerExtData = Property<CompilerExtData, TPk, TPKh>.TypeCheck(ast, lookupExt);

			return new AstElemExt<TPk, TPKh>(compilerExtData, new Miniscript<TPk, TPKh>(ty, ast, ext));
		}
	}

	internal class Cast<TPk, TPKh>
		where TPk : IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
	{
		public Func<Terminal<TPk, TPKh>> Node;
		public Func<MiniscriptFragmentType> AstType;
		public Func<ExtData> ExtData;
		public Func<CompilerExtData> CompilerExtData;

		public Cast(Func<ExtData> extData, Func<MiniscriptFragmentType> astType, Func<Terminal<TPk, TPKh>> node, Func<CompilerExtData> compilerExtData)
		{
			ExtData = extData;
			AstType = astType;
			Node = node;
			CompilerExtData = compilerExtData;
		}

		public AstElemExt<TPk, TPKh> InvokeCast()
			=>
				new AstElemExt<TPk, TPKh>(
					compilerExtData: this.CompilerExtData.Invoke(),
					ms: new Miniscript<TPk, TPKh>(
						this.AstType.Invoke(),
						this.Node.Invoke(),
						this.ExtData.Invoke()

					));

		public static List<Cast<TPk, TPKh>> GetAllCasts(AstElemExt<TPk, TPKh> ast)
			=> new List<Cast<TPk, TPKh>>()
			{
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastCheck,
					ast.Ms.Type.CastCheck,
					() => Terminal<TPk, TPKh>.NewCheck(ast.Ms),
					ast.CompExtData.CastCheck
				),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastDupIf,
					ast.Ms.Type.CastDupIf,
					() => Terminal<TPk, TPKh>.NewDupIf(ast.Ms),
					ast.CompExtData.CastDupIf
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastLikely,
					ast.Ms.Type.CastLikely,
					() => Terminal<TPk, TPKh>.NewOrI(Miniscript<TPk, TPKh>.FromAst(Terminal<TPk, TPKh>.False), ast.Ms),
					ast.CompExtData.CastLikely
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastUnLikely,
					ast.Ms.Type.CastUnLikely,
					() => Terminal<TPk, TPKh>.NewOrI(Miniscript<TPk, TPKh>.FromAst(Terminal<TPk, TPKh>.False), ast.Ms),
					ast.CompExtData.CastUnLikely
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastVerify,
					ast.Ms.Type.CastVerify,
					() => Terminal<TPk, TPKh>.NewVerify(ast.Ms),
					ast.CompExtData.CastVerify
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastNonZero,
					ast.Ms.Type.CastNonZero,
					() => Terminal<TPk, TPKh>.NewNonZero(ast.Ms),
					ast.CompExtData.CastNonZero
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastTrue,
					ast.Ms.Type.CastTrue,
					() => Terminal<TPk, TPKh>.NewAndV(ast.Ms, Miniscript<TPk, TPKh>.FromAst(Terminal<TPk, TPKh>.True)),
					ast.CompExtData.CastTrue
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastSwap,
					ast.Ms.Type.CastSwap,
					() => Terminal<TPk, TPKh>.NewSwap(ast.Ms),
					ast.CompExtData.CastSwap
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastAlt,
					ast.Ms.Type.CastAlt,
					() => Terminal<TPk, TPKh>.NewAlt(ast.Ms),
					ast.CompExtData.CastAlt
					),
				new Cast<TPk, TPKh>(
					ast.Ms.Ext.CastZeroNotEqual,
					ast.Ms.Type.CastZeroNotEqual,
					() => Terminal<TPk, TPKh>.NewZeroNotEqual(ast.Ms),
					ast.CompExtData.CastZeroNotEqual
					),
			};
	}

	public static class Compiler<TPk, TPKh>
		where TPk : IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
	{
		private static void InsertElem(
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> map,
			AstElemExt<TPk, TPKh> astElemExt,
			double SatProb,
			double? dissatProb
		)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get teh best compilations of a policy with a given sat and dissat probabilities. This functions
		/// caches the results into a global policy cache.
		/// </summary>
		/// <param name="policyCache"></param>
		/// <param name="policy"></param>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		/// <returns></returns>
		private static IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> BestCompilation(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, float, float>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			double satProb,
			double? dissatProb
		)
		{
			throw new NotImplementedException();
		}

		private static void CompileBinary(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, float, float>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> ret,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> leftComp,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> rightComp,
			Tuple<double, double> weights,
			double satProb,
			double? dissatProb,
			Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> binFunc
		)
		{
			throw new NotImplementedException();
		}

		private static void CompileTernary(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, float, float>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> ret,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> aComp,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> bComp,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> cComp,
			Tuple<double, double> weights,
			double satProb,
			double? dissatProb,
			Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> binFunc
			)
		{
		}

		public static Miniscript<TPk, TPKh> BestCompilation(ConcretePolicy<TPk, TPKh> policy)
		{
			var policyCache =
				new Dictionary<Tuple<ConcretePolicy<TPk, TPKh>, float, float>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>();
			var x = BestT(policyCache, policy, 1.0, null).Ms;
			if (!x.Type.Malleability.Safe)
				throw CompilerException.TopLevelNonSafe;
			if (!x.Type.Malleability.NonMalleable)
				throw CompilerException.ImpossibleNonMalleableCompilation;

			return x;
		}

		private static AstElemExt<TPk, TPKh> BestT(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, float, float>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			double satProb,
			double? dissatProb
			)
		{
			throw new NotImplementedException();
		}

		private static AstElemExt<TPk, TPKh> BestE(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, float, float>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
				ConcretePolicy<TPk, TPKh> policy,
				double satProb,
				double? dissatProb
			)
		{
			throw new NotImplementedException();
		}

		private static AstElemExt<TPk, TPKh> BestW(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, float, float>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
				ConcretePolicy<TPk, TPKh> policy,
				double satProb,
				double? dissatProb
			)
		{
			throw new NotImplementedException();
		}
	}
}
