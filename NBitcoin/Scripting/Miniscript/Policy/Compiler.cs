using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

		public static CompilerException MaxOpCountExceeded =
			new CompilerException(
				$"one path in the optimal miniscript has opcodes more than {Constants.MAX_OPS_PER_SCRIPT}. consider using more simple script");

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
			Type.IsSubtype(other.Type) &&
			ExpensiveVerify == other.ExpensiveVerify &&
			DissatProb == other.DissatProb;

		internal CompilationKey(MiniscriptFragmentType type, bool expensiveVerify, double? dissatProb)
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
		internal double? BranchProb;

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

		public override CompilerExtData FromTrue()
		{
			// only used in casts. should never be computed directly
			throw new Exception("Unreachable");
		}

		public override CompilerExtData FromFalse() =>
			new CompilerExtData(
				null,
				double.MaxValue,
				0.0
				);

		public override CompilerExtData FromPk() =>
			new CompilerExtData(
				null,
				73.0,
				1.0
				);

		public override CompilerExtData FromPkH() =>
			new CompilerExtData(null, 73.0 + 34.0, 1.0 + 34.0);

		public override CompilerExtData FromMulti(int k, int pkLength) =>
			new CompilerExtData(null, 1.0 + 73.0 * k, (1.0 * (k + 1)));

		public override CompilerExtData FromHash()
			=> new CompilerExtData(null, 33.0, 33.0);

		public override CompilerExtData FromTime(uint time) =>
			new CompilerExtData(null, 0.0, null);

		public override CompilerExtData CastAlt()
			=> new CompilerExtData(null, SatisfyCost, DissatCost);

		public override CompilerExtData CastSwap() =>
			new CompilerExtData(null, SatisfyCost, DissatCost);

		public override CompilerExtData CastCheck()
			=> new CompilerExtData(null, SatisfyCost, DissatCost);

		public override CompilerExtData CastDupIf()
			=> new CompilerExtData(null, 2.0 + SatisfyCost, 1.0);

		public override CompilerExtData CastVerify()
			=> new CompilerExtData(null, SatisfyCost, null);

		public override CompilerExtData CastNonZero()
			=> new CompilerExtData(null, SatisfyCost, 1.0);

		public override CompilerExtData CastZeroNotEqual()
			=> new CompilerExtData(null, SatisfyCost, DissatCost);

		public override CompilerExtData CastTrue()
			=> new CompilerExtData(null, SatisfyCost, null);

		public override CompilerExtData CastOrIFalse()
			=> new CompilerExtData(null, SatisfyCost, null);

		public override CompilerExtData CastUnLikely()
			=> new CompilerExtData(null, 2.0 + SatisfyCost, 1.0);

		public override CompilerExtData CastLikely()
			=> new CompilerExtData(null, 1.0 + SatisfyCost, 2.0);

		public override CompilerExtData AndB(CompilerExtData l, CompilerExtData r)
			=> new CompilerExtData(null, l.SatisfyCost + r.SatisfyCost,
				l.DissatCost + r.DissatCost
				);

		public override CompilerExtData AndV(CompilerExtData l, CompilerExtData r)
			=> new CompilerExtData(null, l.SatisfyCost + r.SatisfyCost, null);

		public override CompilerExtData OrB(CompilerExtData left, CompilerExtData right)
		{
			var lProb = left.BranchProb.Value; // left branch prob must always be set for disjunctions
			var rProb = right.BranchProb.Value; // and for right branch.
			return new CompilerExtData(
				null,
				lProb * (left.SatisfyCost + right.DissatCost.Value) + rProb * (right.SatisfyCost + left.DissatCost.Value),
				left.DissatCost + right.DissatCost
				);
		}

		public override CompilerExtData OrD(CompilerExtData left, CompilerExtData right)
		{
			var lProb = left.BranchProb.Value; // left branch prob must always be set for disjunctions
			var rProb = left.BranchProb.Value; // and for right branch.
			return new CompilerExtData(
				null,
				(lProb * left.SatisfyCost + rProb * (right.SatisfyCost + left.DissatCost.Value)),
				right.DissatCost + left.DissatCost
				);
		}

		public override CompilerExtData OrC(CompilerExtData left, CompilerExtData right)
		{
			var lProb = left.BranchProb.Value; // left branch prob must always be set for disjunctions
			var rProb = right.BranchProb.Value; // and for right branch.
			return new CompilerExtData(
				null,
				lProb * (left.SatisfyCost + right.DissatCost.Value) +
				rProb * (right.SatisfyCost + left.DissatCost.Value),
				null
			);
		}

		public override CompilerExtData OrI(CompilerExtData left, CompilerExtData right)
		{
			var lProb = left.BranchProb.Value; // left branch prob must always be set for disjunctions
			var rProb = right.BranchProb.Value; // and for right branch.
			return new CompilerExtData(
				null,
				lProb * (2.0 + left.SatisfyCost) + rProb * (1.0 + right.SatisfyCost),
				new[] {(2.0 + left.DissatCost), (1.0 + right.DissatCost)}.Min()
				);
		}

		public override CompilerExtData AndOr(CompilerExtData a, CompilerExtData b, CompilerExtData c)
		{
			if (!a.DissatCost.HasValue) throw FragmentPropertyException.LeftNotDissatisfiable();
			var aProb = a.BranchProb.Value;
			var bProb = b.BranchProb.Value;
			var cProb = c.BranchProb.Value;
			var aDis = a.DissatCost.Value;
			return new CompilerExtData(
				null,
				aProb * (a.SatisfyCost + b.SatisfyCost)
				+ cProb * (aDis + c.SatisfyCost),
				aDis + c.DissatCost
				);
		}

		public override CompilerExtData AndN(CompilerExtData l, CompilerExtData r)
			=> new CompilerExtData(null, l.SatisfyCost + r.SatisfyCost, l.DissatCost);

		public override CompilerExtData Threshold(int k, int n, Func<int, CompilerExtData> subCk)
		{
			var kOverN = k;
			var satisfyCost = 0.0;
			var dissatisfyCost = 0.0;
			for (int i = 0; i < n; i++)
			{
				var sub = subCk(i);
				satisfyCost += sub.SatisfyCost;
				dissatisfyCost += sub.DissatCost.Value;
			}

			return new CompilerExtData(
				null,
				satisfyCost * kOverN + dissatisfyCost * (1.0 - kOverN),
				dissatisfyCost);
		}
	}

	internal class AstElemExt<TPk, TPKh>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
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
				Ms.Ext.PkCost +
				(CompExtData.SatisfyCost * satProb) +
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
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
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
					compilerExtData: CompilerExtData.Invoke(),
					ms: new Miniscript<TPk, TPKh>(
						AstType.Invoke(),
						Node.Invoke(),
						ExtData.Invoke()

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
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		private static bool InsertElem(
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> map,
			AstElemExt<TPk, TPKh> astElemExt,
			double satProb,
			double? dissatProb
		)
		{
			if (!astElemExt.Ms.Type.Malleability.NonMalleable)
				return false;

			if (astElemExt.Ms.Ext.OpsCountSat > Constants.MAX_OPS_PER_SCRIPT)
				return false;

			var elemCost = astElemExt.Cost1d(satProb, dissatProb);
			var elemKey = new CompilationKey(astElemExt.Ms.Type, astElemExt.Ms.Ext.HasVerifyForm, dissatProb);

			// Check whether the new element is worse than any existing element. If there
			//
			var isWorse = map.Select((kv) =>
			{
				var existingElementCost = kv.Value.Cost1d(satProb, dissatProb);
				return kv.Key.IsSubtype(elemKey) && existingElementCost <= elemCost;
			}).Aggregate(false, (acc, x) => acc || x);
			if (!isWorse)
			{
				// if the element is not worse any element in the map, remove elements
				// whose subtype is the current element and have worse cost.
				map = map.Where(kv =>
				{
					var existingElementsCost = kv.Value.Cost1d(satProb, dissatProb);
					return !((elemKey.IsSubtype(kv.Key)) && existingElementsCost >= elemCost);
				}).ToDictionary(kv => kv.Key, kv => kv.Value);
				map.Add(elemKey, astElemExt);
			}

			return !isWorse;
		}

		/// <summary>
		/// Insert the cast-closure of in the `astElemExt`. The castStack
		/// has all the elements whose closure is yet to inserted in the map.
		/// A cast-closure refers to trying all possible casts on a particular element
		/// if they are better than the current elements
		/// </summary>
		/// <param name="map"></param>
		/// <param name="astElemExt"></param>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		/// <exception cref="NotImplementedException"></exception>
		private static void InsertElemClosure(
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> map,
			AstElemExt<TPk, TPKh> astElemExt,
			double satProb,
			double? dissatProb
		)
		{
			var castStack = new Stack<AstElemExt<TPk, TPKh>>();
			if (InsertElem(map, astElemExt, satProb, dissatProb))
				castStack.Push(astElemExt);

			while (castStack.Count != 0)
			{
				var casts = Cast<TPk, TPKh>.GetAllCasts(castStack.Pop());
				foreach (var c in casts)
				{
					try
					{
						var newExt = c.InvokeCast();
						if (InsertElem(map, newExt, satProb, dissatProb))
							castStack.Push(newExt);
					} catch (FragmentPropertyException e) {}
				}
			}
		}

		/// <summary>
		/// Insert the best wrapped compilations of a particular Terminal. If the
		/// dissat probability is None, then we directly get the closure of the element.
		/// Otherwise, some wrappers require the compilation of the policy with dissat
		/// `null` because they convert it into a dissat around it.
		/// For example, `l` wrapper should it argument it dissat. `null` because it can
		/// always dissatisfy the policy outside and it find the better inner compilation
		/// given that it may be not be necessary to dissatisfy. For these elements, we
		/// apply the wrappers around the element once and bring them into the same
		/// dissat probability map and get their closure.
		/// </summary>
		/// <param name="policyCache"></param>
		/// <param name="policy"></param>
		/// <param name="map"></param>
		/// <param name="data"></param>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		private static void InsertBestWrapped(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> map,
			AstElemExt<TPk, TPKh> data,
			double satProb,
			double? dissatProb
		)
		{
			InsertElemClosure(map, data, satProb, dissatProb);

			if (dissatProb.HasValue)
			{
				foreach (var x in BestCompilations(policyCache, policy, satProb, null).Values)
				{
					var casts = Cast<TPk, TPKh>.GetAllCasts(x);
					foreach (var c in casts) {
						try
						{
							var newExt = c.InvokeCast();
							InsertElemClosure(map, newExt, satProb, dissatProb);
						}
						catch (FragmentPropertyException _)
						{
						}
					}
				}
			}
		}

		/// <summary>
		/// Get the best compilations of a policy with a given sat and dissat probabilities. This functions
		/// caches the results into a global policy cache.
		/// </summary>
		/// <param name="policyCache"></param>
		/// <param name="policy"></param>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		/// <returns></returns>
		private static IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> BestCompilations(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			double satProb,
			double? dissatProb
		)
		{
			if (policyCache.TryGetValue(Tuple.Create(policy, satProb, dissatProb), out var result))
				return result;
			var ret = new Dictionary<CompilationKey, AstElemExt<TPk, TPKh>>();

			// ---- define helpers ----
			Action<AstElemExt<TPk, TPKh>> insertWrap = (x) =>
				InsertBestWrapped(policyCache, policy, ret, x, satProb, dissatProb);

			Action<
					IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>,
					IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>,
					Tuple<double, double>,
					Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>>>
				compileBinary = (l, right, w, f) =>
					CompileBinary(policyCache, policy, ret,  l, right, w, satProb, dissatProb, f);

			Action<
					IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>,
					IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>,
					IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>,
					Tuple<double, double>
				>
				compileTernary = (a, b, c, w) =>
					CompileTernary(policyCache, policy, ret, a, b, c, w, satProb, dissatProb);
			// -------

			switch (policy)
			{
				case ConcretePolicy<TPk, TPKh>.Key pol:
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewPkH(pol.Item.ToPubKeyHash())));
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewPk(pol.Item)));
					break;
				case ConcretePolicy<TPk, TPKh>.After pol:
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewAfter(pol.Item)));
					break;
				case ConcretePolicy<TPk, TPKh>.Older pol:
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewOlder(pol.Item)));
					break;
				case ConcretePolicy<TPk, TPKh>.Sha256 pol:
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewSha256(pol.Item)));
					break;
				case ConcretePolicy<TPk, TPKh>.Hash256 pol:
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewHash256(pol.Item)));
					break;
				case ConcretePolicy<TPk, TPKh>.Ripemd160 pol:
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewRipemd160(pol.Item)));
					break;
				case ConcretePolicy<TPk, TPKh>.Hash160 pol:
					insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewHash160(pol.Item)));
					break;
				case ConcretePolicy<TPk, TPKh>.And pol:
					Debug.Assert(pol.Item.Count == 2);
					var l = BestCompilations(policyCache, pol.Item[0], satProb, dissatProb);
					var r = BestCompilations(policyCache, pol.Item[1], satProb, dissatProb);
					var q_zero_l = BestCompilations(policyCache, pol.Item[0], satProb, null);
					var q_zero_r = BestCompilations(policyCache, pol.Item[1], satProb, null);
					compileBinary(l, r, Tuple.Create(1.0, 1.0), Terminal<TPk, TPKh>.NewAndB);
					compileBinary(r, l, Tuple.Create(1.0, 1.0), Terminal<TPk, TPKh>.NewAndB);
					compileBinary(l, r, Tuple.Create(1.0, 1.0), Terminal<TPk, TPKh>.NewAndV);
					compileBinary(r, l, Tuple.Create(1.0, 1.0), Terminal<TPk, TPKh>.NewAndV);
					var zeroComp = new Dictionary<CompilationKey, AstElemExt<TPk, TPKh>>();
					zeroComp.Add(
						new CompilationKey(
							new MiniscriptFragmentType().FromFalse(),
							new ExtData().FromFalse().HasVerifyForm,
							dissatProb
							),
						AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewFalse())
						);
					compileTernary(l, q_zero_r, zeroComp, Tuple.Create(1.0, 0.0));
					compileTernary(r, q_zero_l, zeroComp, Tuple.Create(1.0, 0.0));
					break;
				case ConcretePolicy<TPk, TPKh>.Or pol:
					var total = (double) (pol.Item[0].Item1 + pol.Item[1].Item1);
					var lw = pol.Item[0].Item1 / total;
					var rw = pol.Item[1].Item1 / total;
					if (pol.Item[0].Item2 is ConcretePolicy<TPk, TPKh>.And a)
					{
						var a1 =
							BestCompilations(
								policyCache,
								a.Item[0],
								lw * satProb,
								(dissatProb ?? 0.0) + rw * satProb);
						var a2 =
							BestCompilations(
								policyCache,
								a.Item[0],
								lw * satProb, null);
						var b1 =
							BestCompilations(
								policyCache,
								a.Item[1],
								lw * satProb,
								(dissatProb ?? 0) + rw * satProb
								);
						var b2 =
							BestCompilations(
								policyCache,
								a.Item[1],
								lw * satProb,
								null
								);
						var c = BestCompilations(policyCache, pol.Item[1].Item2, rw * satProb, dissatProb);
						compileTernary(a1, b2, c, Tuple.Create(lw, rw));
						compileTernary(b1, a2, c, Tuple.Create(lw, rw));
					}

					if (pol.Item[1].Item2 is ConcretePolicy<TPk, TPKh>.And x)
					{
						var a1 = BestCompilations(policyCache, x.Item[0], rw * satProb, (dissatProb ?? 0.0) + lw * satProb);
						var a2 = BestCompilations(policyCache, x.Item[0], rw * satProb, null);
						var b1 = BestCompilations(policyCache, x.Item[1], rw * satProb, (dissatProb ?? 0.0) + lw * satProb);
						var b2 = BestCompilations(policyCache, x.Item[1], rw * satProb, null);
						var c = BestCompilations(policyCache, pol.Item[0].Item2, lw * satProb, dissatProb);
						compileTernary(a1, b2, c, Tuple.Create(rw, lw));
						compileTernary(b1, a2, c, Tuple.Create(rw, lw));
					}

					Func<double, List<double?>> dissatProbs =
						(w) =>
						{
							var dissatSet = new List<double?>();
							dissatSet.Add((dissatProb ?? 0.0) + w * satProb);
							dissatSet.Add(w * satProb);
							dissatSet.Add(dissatProb);
							dissatSet.Add(null);
							return dissatSet;
						};

					var lComp = new List<IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>();
					var rComp = new List<IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>();
					foreach (var dissatProbRight in dissatProbs(rw))
						lComp.Add(BestCompilations(policyCache, pol.Item[0].Item2, lw * satProb, dissatProbRight));
					foreach (var dissatProbLeft in dissatProbs(lw))
						rComp.Add(BestCompilations(policyCache, pol.Item[1].Item2, rw * satProb, dissatProbLeft));
					compileBinary(lComp[0], rComp[0], Tuple.Create(lw, rw), Terminal<TPk, TPKh>.NewOrB);
					compileBinary(rComp[0], lComp[0], Tuple.Create(rw, lw), Terminal<TPk, TPKh>.NewOrB);

					compileBinary(lComp[0], rComp[2], Tuple.Create<double, double>(lw, rw), Terminal<TPk, TPKh>.NewOrD);
					compileBinary(rComp[0], lComp[2], Tuple.Create<double, double>(rw, lw), Terminal<TPk, TPKh>.NewOrD);

					compileBinary(lComp[1], rComp[3], Tuple.Create(lw, rw), Terminal<TPk, TPKh>.NewOrC);
					compileBinary(rComp[1], lComp[3], Tuple.Create(rw, lw), Terminal<TPk, TPKh>.NewOrC);

					compileBinary(lComp[2], rComp[3], Tuple.Create(lw, rw), Terminal<TPk, TPKh>.NewOrI);
					compileBinary(rComp[2], lComp[3], Tuple.Create(rw, lw), Terminal<TPk, TPKh>.NewOrI);

					compileBinary(lComp[3], rComp[2], Tuple.Create(lw, rw), Terminal<TPk, TPKh>.NewOrI);
					compileBinary(rComp[3], lComp[2], Tuple.Create(rw, lw), Terminal<TPk, TPKh>.NewOrI);
					break;

				case ConcretePolicy<TPk, TPKh>.Threshold pol:
					var n = pol.Item2.Count;
					var kOverN = ((double) pol.Item1) / n;
					var subAst = new List<Miniscript<TPk, TPKh>>();
					var subExtData  = new List<CompilerExtData>();
					var bestEs = new List<Tuple<CompilerExtData, AstElemExt<TPk, TPKh>>>();
					var bestWs = new List<Tuple<CompilerExtData, AstElemExt<TPk, TPKh>>>();

					var minValue = new { Item1 = 0, Item2 = double.PositiveInfinity };

					for (int i = 0; i < pol.Item2.Count; i++)
					{
						var ast = pol.Item2[i];
						var sp = satProb * kOverN;
						var dp = (dissatProb ?? 0.0) + (1.0 - kOverN) * satProb;
						var be = BestE(policyCache, ast, sp, dp);
						var bw = BestW(policyCache, ast, sp, dp);
						var diff = be.Cost1d(sp, dp) - bw.Cost1d(sp, dp);
						bestEs.Add(Tuple.Create(be.CompExtData, be));
						bestWs.Add(Tuple.Create(bw.CompExtData, bw));
						if (diff < minValue.Item2)
							minValue = new {Item1 = i, Item2 = diff};
						subExtData.Add(bestEs[minValue.Item1].Item1);
						subAst.Add(bestEs[minValue.Item1].Item2.Ms);
					}
					for (int j = 0; j < pol.Item2.Count; j++)
					{
						if (j != minValue.Item1)
						{
							subExtData.Add(bestWs[j].Item1);
							subAst.Add(bestWs[j].Item2.Ms);
						}
					}

					var astResult = Terminal<TPk, TPKh>.NewThresh(pol.Item1, subAst);
					var astExtResult = new AstElemExt<TPk, TPKh>(
						new CompilerExtData().Threshold((int)pol.Item1, n, index => subExtData[index]),
							Miniscript<TPk, TPKh>.FromAst(astResult));
					insertWrap(astExtResult);
					var keyVec =
						pol.Item2
							.Select(s => (s is ConcretePolicy<TPk, TPKh>.Key pk) ? pk.Item : null)
							.Where(pk => pk != null)
							.ToList();
					if (keyVec.Count == pol.Item2.Count && pol.Item2.Count <= 20)
						insertWrap(AstElemExt<TPk, TPKh>.Terminal(Terminal<TPk, TPKh>.NewThreshM(pol.Item1, keyVec)));
					break;
				default:
					throw new Exception("Unreachable!");
			}

			foreach (var k in ret.Keys)
				Debug.Assert(k.DissatProb == dissatProb);

			if (ret.Count == 0)
				// The only reason we are discarding elements out of compiler is because
				// compilations exceed opcount or are non-malleable. If there is no possible
				// compilations for any policies regardless of dissat probability then it must
				// have all compilations exceeded the Max opcount because we already checked
				// that policy must have non-malleable compilations before calling this compile function
				throw CompilerException.MaxOpCountExceeded;
			policyCache.Add(Tuple.Create(policy, satProb, dissatProb), ret);
			return ret;
		}
		/// <summary>
		/// Helper function to compile different types of binary fragments.
		/// `sat_prob` and `dissat_prob` represent the sat and dissat probabilities
		/// of root or. `weights` represents the odds for taking each sub branch.
		/// </summary>
		/// <param name="policyCache"></param>
		/// <param name="policy"></param>
		/// <param name="ret"></param>
		/// <param name="leftComp"></param>
		/// <param name="rightComp"></param>
		/// <param name="weights"></param>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		/// <param name="binFunc"></param>
		/// <exception cref="NotImplementedException"></exception>

		private static void CompileBinary(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
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
			foreach (var l in leftComp.Values)
			{
				foreach (var r in rightComp.Values)
				{
					var ast = binFunc(l.Ms, r.Ms);
					l.CompExtData.BranchProb = weights.Item1;
					r.CompExtData.BranchProb = weights.Item2;
					try
					{
						var newExt = AstElemExt<TPk, TPKh>.Binary(ast, l, r);
						InsertBestWrapped(policyCache, policy, ret, newExt, satProb, dissatProb);
					} catch (FragmentPropertyException ex) {}
				}
			}
			throw new NotImplementedException();
		}

		private static void CompileTernary(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> ret,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> aComp,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> bComp,
			IDictionary<CompilationKey, AstElemExt<TPk, TPKh>> cComp,
			Tuple<double, double> weights,
			double satProb,
			double? dissatProb
			)
		{
			foreach (var a in aComp.Values)
			{
				foreach (var b in bComp.Values)
				{
					foreach (var c in cComp.Values)
					{
						a.CompExtData.BranchProb = weights.Item1;
						b.CompExtData.BranchProb = weights.Item1;
						c.CompExtData.BranchProb = weights.Item2;
						var ast = Terminal<TPk, TPKh>.NewAndOr(a.Ms, b.Ms, c.Ms);
						try
						{
							var newExt = AstElemExt<TPk, TPKh>.Ternary(ast, a, b, c);
							InsertBestWrapped(policyCache, policy, ret, newExt, satProb, dissatProb);
						} catch (FragmentPropertyException ex)
						{}
					}
				}
			}
		}

		public static Miniscript<TPk, TPKh> BestCompilation(ConcretePolicy<TPk, TPKh> policy)
		{
			var policyCache =
				new Dictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>();
			var x = BestT(policyCache, policy, 1.0, null).Ms;
			if (!x.Type.Malleability.Safe)
				throw CompilerException.TopLevelNonSafe;
			if (!x.Type.Malleability.NonMalleable)
				throw CompilerException.ImpossibleNonMalleableCompilation;

			return x;
		}

		/// <summary>
		/// Obtain the best B expression with given sat and dissat.
		/// </summary>
		/// <param name="policyCache"></param>
		/// <param name="policy"></param>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private static AstElemExt<TPk, TPKh> BestT(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
			ConcretePolicy<TPk, TPKh> policy,
			double satProb,
			double? dissatProb
			)
		{
			var result =
				BestCompilations(policyCache, policy, satProb, dissatProb)
					.Where(kv => kv.Key.Type.Correctness.Base == Base.B && kv.Key.DissatProb == dissatProb)
					.Select(kv => kv.Value);

			if (!result.Any())
				throw CompilerException.MaxOpCountExceeded;
			return result.Aggregate( // get where the cost is minimum
				(champion, challenger) =>
				{
					 var challengerValue = challenger.Cost1d(satProb, dissatProb);
					 var championValue = champion.Cost1d(satProb, dissatProb);
					 return (challengerValue < championValue) ? challenger : champion;
				});
		}

		/// <summary>
		/// Obtain the B.deu with the given sat and dissat.
		/// </summary>
		/// <param name="policyCache"></param>
		/// <param name="policy"></param>
		/// <param name="satProb"></param>
		/// <param name="dissatProb"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private static AstElemExt<TPk, TPKh> BestE(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
				ConcretePolicy<TPk, TPKh> policy,
				double satProb,
				double? dissatProb
			)
		{
			var result =
				BestCompilations(policyCache, policy, satProb, dissatProb)
					.Where(kv =>
						kv.Key.Type.Correctness.Base == Base.B
						&& kv.Key.Type.Correctness.Unit
						&& kv.Value.Ms.Type.Malleability.Dissat == Dissat.Unique
						&& kv.Key.DissatProb == dissatProb);
			if (!result.Any())
				throw CompilerException.MaxOpCountExceeded;
			return result.Select(kv => kv.Value)
				.Aggregate( // get where cost is minimum
					(champion, challenger) =>
						{
							var challengerValue = challenger.Cost1d(satProb, dissatProb);
							var championValue = champion.Cost1d(satProb, dissatProb);
							return (challengerValue < championValue) ? challenger : champion;
						}
					);
		}

		private static AstElemExt<TPk, TPKh> BestW(
			IDictionary<Tuple<ConcretePolicy<TPk, TPKh>, double, double?>, IDictionary<CompilationKey, AstElemExt<TPk, TPKh>>>
				policyCache,
				ConcretePolicy<TPk, TPKh> policy,
				double satProb,
				double? dissatProb
			)
		{
			var result =
				BestCompilations(policyCache, policy, satProb, dissatProb)
					.Where(kv =>
						kv.Key.Type.Correctness.Unit
						&& kv.Value.Ms.Type.Malleability.Dissat == Dissat.Unique
						&& kv.Key.DissatProb == dissatProb
					);
			if (!result.Any())
				throw CompilerException.MaxOpCountExceeded;
			return result.Select(kv => kv.Value)
				.Aggregate( // get where cost is minimum
					(champion, challenger) =>
						{
							var challengerValue = challenger.Cost1d(satProb, dissatProb);
							var championValue = champion.Cost1d(satProb, dissatProb);
							return (challengerValue < championValue) ? challenger : champion;
						}
					);
		}
	}
}