using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Miniscript
{
	using CostCalculationInfo = Tuple<CalcType, int, Cost, Cost>;
	internal enum CalcType
	{
		Base = 0,
		BaseSwap,
		Cond,
		CondSwap,
		True,
		TrueSwap,
		Key,
		KeyCond

	}
	internal class CompiledNode
	{
		internal Dictionary<Tuple<UInt64, UInt64>, Cost> BestEMap { get; }
		internal Dictionary<Tuple<UInt64, UInt64>, Cost> BestQMap { get; }
		internal Dictionary<Tuple<UInt64, UInt64>, Cost> BestWMap { get; }
		internal Dictionary<Tuple<UInt64, UInt64>, Cost> BestFMap { get; }
		internal Dictionary<Tuple<UInt64, UInt64>, Cost> BestVMap { get; }
		internal Dictionary<Tuple<UInt64, UInt64>, Cost> BestTMap { get; }

		internal CompiledNodeContent Content { get; }


		private CompiledNode(AbstractPolicy policy)
		{
			BestEMap = new Dictionary<Tuple<UInt64, UInt64>, Cost>();
			BestWMap = new Dictionary<Tuple<UInt64, UInt64>, Cost>();
			BestQMap = new Dictionary<Tuple<UInt64, UInt64>, Cost>();
			BestFMap = new Dictionary<Tuple<UInt64, UInt64>, Cost>();
			BestVMap = new Dictionary<Tuple<UInt64, UInt64>, Cost>();
			BestTMap = new Dictionary<Tuple<UInt64, UInt64>, Cost>();
			Content = CompiledNodeContent.FromPolicy(policy);
		}
		internal static CompiledNode FromPolicy(AbstractPolicy policy)
			=> new CompiledNode(policy);

		private Tuple<ulong, ulong> GetHashKey(double pSat, double pDissat)
			=> Tuple.Create(
				BitConverter.ToUInt64(BitConverter.GetBytes(pSat), 0),
				BitConverter.ToUInt64(BitConverter.GetBytes(pDissat), 0)
				);
		internal Cost BestE(double pSat, double pDissat)
		{
			var hashKey = GetHashKey(pSat, pDissat);
			if (BestEMap.TryGetValue(hashKey, out Cost value))
				return value;
			switch (Content)
			{
				case CompiledNodeContent.Pk c:
					return Cost.FromTerminal(new AstElem.Pk(c.Item1));
				case CompiledNodeContent.Multi c:
					var multiOptions = new List<Cost>();
					multiOptions.Add(Cost.FromTerminal(new AstElem.Multi(c.Item1, c.Item2)));
					if (pDissat > 0.0)
					{
						var fCostMulti = BestF(pSat, pDissat);
						multiOptions.Add(Cost.Likely(fCostMulti));
						multiOptions.Add(Cost.Unlikely(fCostMulti));
					}
					return FoldCostList(multiOptions, pSat, pDissat);
				case CompiledNodeContent.Time c:
					return Cost.FromTerminal(new AstElem.Time(c.Item1));
				case CompiledNodeContent.Hash c:
					var fCostHash = BestF(pSat, 0.0);
					return MinCost(Cost.Likely(fCostHash), Cost.Unlikely(fCostHash), pSat, pDissat);
				case CompiledNodeContent.And c:
					var le = c.Item1.BestE(pSat, pDissat);
					var re = c.Item2.BestE(pSat, pDissat);
					var lw = c.Item1.BestW(pSat, pDissat);
					var rw = c.Item2.BestW(pSat, pDissat);

					var lf = c.Item1.BestF(pSat, pDissat);
					var rf = c.Item2.BestF(pSat, pDissat);
					var andRet = MinCostOf(
						pSat, pDissat, 0.5, 0.5,
						new CostCalculationInfo[]
							{
								Tuple.Create(CalcType.Base, AstElem.Tags.AndBool, le, rw),
								Tuple.Create(CalcType.Base, AstElem.Tags.AndCasc, le, rf),
								Tuple.Create(CalcType.BaseSwap, AstElem.Tags.AndBool, re, lw),
								Tuple.Create(CalcType.BaseSwap, AstElem.Tags.AndCasc, re, lf),
							}
						);
					BestEMap.Add(hashKey, andRet);
					return andRet;
				case CompiledNodeContent.Or c:
					var left = c.Item1;
					var right = c.Item2;
					var lweight = c.Item3;
					var rweight = c.Item4;
					var lePar = left.BestE(pSat * lweight, pDissat + pSat * rweight);
					var rePar = right.BestE(pSat * rweight, pDissat + pSat * lweight);
					var lwPar = left.BestW(pSat * lweight, pDissat + pSat * rweight);
					var rwPar = right.BestW(pSat * rweight, pDissat + pSat * lweight);
					var leCas = left.BestE(pSat * lweight, pDissat);
					var reCas = right.BestE(pSat * rweight, pDissat);

					var leCondPar = left.BestE(pSat * lweight, pSat * rweight);
					var reCondPar = right.BestE(pSat * rweight, pSat * lweight);
					var lv2 = left.BestV(pSat * lweight, 0.0);
					var rv2 = right.BestV(pSat * rweight, 0.0);
					var lf2 = left.BestF(pSat * lweight, 0.0);
					var rf2 = right.BestF(pSat * rweight, 0.0);
					var lq = left.BestQ(pSat * lweight, 0.0);
					var rq = right.BestQ(pSat * rweight, 0.0);
					var orRet = MinCostOf(pSat, pDissat, lweight, rweight, new CostCalculationInfo[]{
							Tuple.Create(CalcType.Base, AstElem.Tags.OrBool, lePar, rwPar),
							Tuple.Create(CalcType.Base, AstElem.Tags.OrCasc, lePar, reCas),
							Tuple.Create(CalcType.Base, AstElem.Tags.OrIf, reCas, lf2),
							Tuple.Create(CalcType.Base, AstElem.Tags.OrNotIf, lf2, reCas),
							Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrBool, rePar, lwPar),
							Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrCasc, rePar, leCas),
							Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrIf, leCas, rf2),
							Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrNotIf, rf2, leCas),
							Tuple.Create(CalcType.Cond, AstElem.Tags.OrCont, leCondPar, rv2),
							Tuple.Create(CalcType.Cond, AstElem.Tags.OrIf, lf2, rf2),
							Tuple.Create(CalcType.Cond, AstElem.Tags.OrIf, lv2, rv2),
							Tuple.Create(CalcType.CondSwap, AstElem.Tags.OrCont, reCondPar, lv2),
							Tuple.Create(CalcType.CondSwap, AstElem.Tags.OrIf, rf2, lf2),
							Tuple.Create(CalcType.CondSwap, AstElem.Tags.OrIf, rv2, lv2),
							Tuple.Create(CalcType.KeyCond, -1, lq, rq),
						});
					BestEMap.Add(hashKey, orRet);
					return orRet;
				case CompiledNodeContent.Thresh c:
					var numCost = Cost.ScriptNumCost(c.Item1);
					var avgCost = (double)c.Item1 / (double)c.Item2.Length;
					var e = c.Item2[0].BestE(pSat * avgCost, pDissat + pSat * (1.0 - avgCost));
					var pkCost = 1 + numCost + e.PkCost;
					var satCost = e.SatCost;
					var dissatCost = e.DissatCost;
					var subAsts = new List<AstElem>();
					subAsts.Add(e.Ast);
					foreach (var expr in c.Item2.Skip(1))
					{
						var w = expr.BestW(pSat * avgCost, pDissat + pSat * (1.0 - avgCost));
						pkCost += w.PkCost + 1;
						satCost += w.SatCost;
						dissatCost += w.DissatCost;
						subAsts.Add(w.Ast);
					}

					var nonCond = new Cost(
						ast: new AstElem.Thresh(c.Item1, subAsts.ToArray()),
						pkCost: pkCost,
						satCost: satCost * avgCost + dissatCost * (1.0 - avgCost),
						dissatCost: dissatCost
					);
					var f = BestF(pSat, 0.0);
					var cond = MinCost(Cost.Likely(f), Cost.Unlikely(f), pSat, pDissat);
					var retThresh = MinCost(cond, nonCond, pSat, pDissat);
					BestEMap.Add(hashKey, retThresh);
					return retThresh;
			}
			throw new Exception("Unreachable!");
		}

		internal Cost BestQ(double pSat, double pDissat)
		{
			var hashKey = GetHashKey(pSat, pDissat);
			if (BestQMap.TryGetValue(hashKey, out Cost value))
				return value;

			switch (Content)
			{
				case CompiledNodeContent.Pk c:
					return Cost.FromTerminal(new AstElem.PkQ(c.Item1));
				case CompiledNodeContent.And c:
					var andQOptions = new List<Cost>();
					var rq = c.Item2.BestQ(pSat, pDissat);
					if (rq != null)
					{
						var lv = c.Item1.BestV(pSat, pDissat);
						andQOptions.Add(new Cost(
								ast: new AstElem.AndCat(lv.Ast, rq.Ast),
								pkCost: lv.PkCost + rq.PkCost,
								satCost: lv.SatCost + rq.SatCost,
								dissatCost: 0.0
							));
					}
					var lq = c.Item1.BestQ(pSat, pDissat);
					if (lq != null)
					{
						var rv = c.Item2.BestV(pSat, pDissat);
						andQOptions.Add(new Cost(
							ast: new AstElem.AndCat(rv.Ast, lq.Ast),
							pkCost: rv.PkCost + lq.PkCost,
							satCost: rv.SatCost + lq.SatCost,
							dissatCost: 0.0
						));
					}

					if (andQOptions.Count == 0)
						return null;
					else
						return FoldCostList(andQOptions, pSat, pDissat);
				case CompiledNodeContent.Or c:
					var lweight = c.Item3;
					var rweight = c.Item4;
					var lqOr = c.Item1.BestQ(pSat * lweight, 0.0);
					var rqOr = c.Item2.BestQ(pSat * rweight, 0.0);
					var orQOptions = new List<Cost>();
					if (lqOr != null && rqOr != null)
					{
						orQOptions.Add(new Cost(
							ast: new AstElem.OrIf(lqOr.Ast, rqOr.Ast),
							pkCost: lqOr.PkCost + rqOr.PkCost + 3,
							satCost: lweight * (2.0 + lqOr.SatCost) + rweight * (1.0 + rqOr.SatCost),
							dissatCost: 0.0
							));
						orQOptions.Add(new Cost(
							ast: new AstElem.OrIf(rqOr.Ast, lqOr.Ast),
							pkCost: rqOr.PkCost + lqOr.PkCost + 3,
							satCost: lweight * (1.0 + lqOr.SatCost) + rweight * (2.0 + rqOr.SatCost),
							dissatCost: 0.0
							));
					}
					if (orQOptions.Count == 0)
						return null;
					else
						return FoldCostList(orQOptions, pSat, pDissat);
				default:
					return null;
			}
			throw new Exception("Unreachable");
		}

		internal Cost BestW(double pSat, double pDissat)
		{
			switch (Content)
			{
				case CompiledNodeContent.Pk c:
					return Cost.FromTerminal(new AstElem.PkW(c.Item1));
				case CompiledNodeContent.Time c:
					return Cost.FromTerminal(new AstElem.TimeW(c.Item1));
				case CompiledNodeContent.Hash c:
					return Cost.FromTerminal(new AstElem.HashW(c.Item1));
				default:
					return Cost.Wrap(BestE(pSat, pDissat));
			}
		}

		internal Cost BestF(double pSat, double pDissat)
		{
			var hashKey = GetHashKey(pSat, pDissat);
			if (BestFMap.TryGetValue(hashKey, out Cost value))
				return value;
			switch (Content)
			{
				case CompiledNodeContent.Pk c:
					return Cost.True(Cost.FromTerminal(new AstElem.PkV(c.Item1)));
				case CompiledNodeContent.Multi c:
					return Cost.True(Cost.FromTerminal(new AstElem.MultiV(c.Item1, c.Item2)));
				case CompiledNodeContent.Time c:
					return Cost.FromTerminal(new AstElem.TimeF(c.Item1));
				case CompiledNodeContent.Hash c:
					return Cost.True(Cost.FromTerminal(new AstElem.HashV(c.Item1)));
				case CompiledNodeContent.And c:
					var vl = c.Item1.BestV(pSat, 0.0);
					var vr = c.Item2.BestV(pSat, 0.0);
					var fl = c.Item1.BestF(pSat, 0.0);
					var fr = c.Item2.BestF(pSat, 0.0);
					var retAnd = MinCostOf(pSat, 0.0, 0.5, 0.5, new CostCalculationInfo[]{
						Tuple.Create(CalcType.Base, AstElem.Tags.AndCat, vl, fr),
						Tuple.Create(CalcType.BaseSwap, AstElem.Tags.AndCat, vr, fl)
					});
					BestFMap.Add(hashKey, retAnd);
					return retAnd;
				case CompiledNodeContent.Or c:
					var left = c.Item1;
					var right = c.Item2;
					var lweight = c.Item3;
					var rweight = c.Item4;
					var lePar = left.BestE(pSat + lweight, pSat + rweight);
					var rePar = right.BestE(pSat * rweight, pSat * lweight);

					var lf = left.BestF(pSat * lweight, 0.0);
					var rf = right.BestF(pSat * rweight, 0.0);
					var lv = left.BestV(pSat * lweight, 0.0);
					var rv = right.BestV(pSat * rweight, 0.0);
					var lq = left.BestQ(pSat * lweight, 0.0);
					var rq = right.BestQ(pSat * rweight, 0.0);

					var retOr = MinCostOf(pSat, 0.0, lweight, rweight, new CostCalculationInfo[]
					{
						Tuple.Create(CalcType.Base, AstElem.Tags.OrIf, lf, rf),
						Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrIf, rf, lf),
						Tuple.Create(CalcType.True, AstElem.Tags.OrCont, lePar, rv),
						Tuple.Create(CalcType.True, AstElem.Tags.OrIf, lv, rv),
						Tuple.Create(CalcType.TrueSwap, AstElem.Tags.OrCont, rePar, lv),
						Tuple.Create(CalcType.TrueSwap, AstElem.Tags.OrIf, rv, lv),
						Tuple.Create(CalcType.KeyCond, -1, lq, rq),
					});

					BestFMap.Add(hashKey, retOr);
					return retOr;
				case CompiledNodeContent.Thresh c:
					var numCost = Cost.ScriptNumCost(c.Item1);
					var avgCost = (double)c.Item1 / (double)c.Item2.Length;
					var e = c.Item2[0].BestE(pSat * avgCost, pDissat + pSat * (1.0 - avgCost));
					var pkCost = 1 + numCost + e.PkCost;
					var satCost = e.SatCost;
					var dissatCost = e.DissatCost;
					var subAsts = new List<AstElem>();
					subAsts.Add(e.Ast);
					foreach (var expr in c.Item2.Skip(1))
					{
						var w = expr.BestW(pSat * avgCost, pDissat + pSat * (1.0 - avgCost));
						pkCost += w.PkCost + 1;
						satCost += w.SatCost;
						dissatCost += w.DissatCost;
						subAsts.Add(w.Ast);
					}

					return Cost.True(new Cost(
						ast: new AstElem.ThreshV(c.Item1, subAsts.ToArray()),
						pkCost: pkCost,
						satCost: satCost,
						dissatCost: 0.0
						));
			}
			throw new Exception("Unreachable");
		}

		internal Cost BestV(double pSat, double pDissat)
		{
			var hashKey = GetHashKey(pSat, pDissat);
			if (BestVMap.TryGetValue(hashKey, out Cost value))
				return value;
			switch (Content)
			{
				case CompiledNodeContent.Pk c:
					return Cost.FromTerminal(new AstElem.PkV(c.Item1));
				case CompiledNodeContent.Multi c:
					return Cost.FromTerminal(new AstElem.MultiV(c.Item1, c.Item2));
				case CompiledNodeContent.Time c:
					return Cost.FromTerminal(new AstElem.TimeV(c.Item1));
				case CompiledNodeContent.Hash c:
					return Cost.FromTerminal(new AstElem.HashV(c.Item1));
				case CompiledNodeContent.And c:
					return Cost.FromPair(c.Item1.BestV(pSat, 0.0), c.Item2.BestV(pSat, 0.0), AstElem.Tags.AndCat, 0.0, 0.0);
				case CompiledNodeContent.Or c:
					var left = c.Item1;
					var right = c.Item2;
					var lweight = c.Item3;
					var rweight = c.Item4;
					var lePar = left.BestE(pSat * lweight, pSat * rweight);
					var rePar = right.BestE(pSat * rweight, pSat * lweight);

					var lt = left.BestT(pSat * lweight, 0.0);
					var rt = right.BestT(pSat * rweight, 0.0);
					var lv = left.BestV(pSat * lweight, 0.0);
					var rv = right.BestV(pSat * rweight, 0.0);
					var lq = left.BestQ(pSat * lweight, 0.0);
					var rq = right.BestQ(pSat * rweight, 0.0);

					var ret = MinCostOf(
						pSat, 0.0, lweight, rweight,
						new CostCalculationInfo[] {
							Tuple.Create(CalcType.Base, AstElem.Tags.OrCont, lePar, rv),
							Tuple.Create(CalcType.Base, AstElem.Tags.OrIf, lv, rv),
							Tuple.Create(CalcType.Base, AstElem.Tags.OrIfV, lt, rt),
							Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrCont, rePar, lv),
							Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrIf, rv, lv),
							Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrIfV, rt, lt),
							Tuple.Create(CalcType.Key, AstElem.Tags.OrKeyV, lq, rq),
						}
					);

					// Memoize and return
					BestVMap.Add(hashKey, ret);
					return ret;
				case CompiledNodeContent.Thresh c:
					var numCost = Cost.ScriptNumCost(c.Item1);
					var avgCost = (double)c.Item1 / (double)c.Item2.Length;
					var e = c.Item2[0].BestE(pSat * avgCost, pSat * (1.0 - avgCost));
					var pkCost = 1 + numCost + e.PkCost;
					var satCost = e.SatCost;
					var dissatCost = e.DissatCost;
					var subAsts = new List<AstElem>();
					subAsts.Add(e.Ast);
					foreach (var subW in c.Item2.Skip(1))
					{
						var w = subW.BestW(pSat * avgCost, pDissat + pSat * (1.0 - avgCost));
						pkCost += w.PkCost + 1;
						satCost += w.SatCost;
						dissatCost += w.DissatCost;
						subAsts.Add(w.Ast);
					}

					return new Cost(
						ast: new AstElem.ThreshV(c.Item1, subAsts.ToArray()),
						pkCost: pkCost,
						satCost: satCost * avgCost + dissatCost * (1 - avgCost),
						dissatCost: 0.0
						);
			}
			throw new Exception("Unreachable");
		}
		internal Cost BestT(double pSat, double pDissat)
		{
			var hashKey = GetHashKey(pSat, pDissat);
			if (BestTMap.TryGetValue(hashKey, out Cost value))
				return value;
			switch (Content)
			{
				case CompiledNodeContent.Pk _:
				case CompiledNodeContent.Multi _:
				case CompiledNodeContent.Thresh _:
					var ret = BestE(pSat, 0.0);
					ret.DissatCost = 0.0;
					return ret;
				case CompiledNodeContent.Time c:
					return Cost.FromTerminal(AstElem.NewTimeT(c.Item1));
				case CompiledNodeContent.Hash c:
					return Cost.FromTerminal(AstElem.NewHashT(c.Item1));
				case CompiledNodeContent.And c:
					var lv = c.Item1.BestV(pSat, 0.0);
					var rv = c.Item2.BestV(pSat, 0.0);
					var lt = c.Item1.BestT(pSat, 0.0);
					var rt = c.Item2.BestT(pSat, 0.0);
					var retAnd = MinCostOf(pSat, 0.0, 0.0, 0.0, new CostCalculationInfo[] {
						Tuple.Create(CalcType.Base, AstElem.Tags.AndCat, lv, rt),
						Tuple.Create(CalcType.BaseSwap, AstElem.Tags.AndCat, rv, lt)
					});
					BestTMap.Add(hashKey, retAnd);
					return retAnd;
				case CompiledNodeContent.Or c:
					var left = c.Item1;
					var right = c.Item2;
					var lweight = c.Item3;
					var rweight = c.Item4;
					var lePar = left.BestE(pSat * lweight, pSat * rweight);
					var rePar = right.BestE(pSat * rweight, pSat * lweight);
					var lwPar = left.BestW(pSat * lweight, pSat * rweight);
					var rwPar = right.BestW(pSat * rweight, pSat * lweight);

					var lt2 = left.BestT(pSat * lweight, 0.0);
					var rt2 = right.BestT(pSat * rweight, 0.0);
					var lv2 = left.BestV(pSat * lweight, 0.0);
					var rv2 = right.BestV(pSat * rweight, 0.0);
					var lq = left.BestQ(pSat * lweight, 0.0);
					var rq = right.BestQ(pSat * rweight, 0.0);

					var ret2 = MinCostOf(
							pSat, 0.0, lweight, rweight,
							new CostCalculationInfo[] {
								Tuple.Create(CalcType.Base, AstElem.Tags.OrBool, lePar, rwPar),
								Tuple.Create(CalcType.Base, AstElem.Tags.OrCasc, lePar, rt2),
								Tuple.Create(CalcType.Base, AstElem.Tags.OrIf, lt2, rt2),
								Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrBool, rePar, lwPar),
								Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrCasc, rePar, lt2),
								Tuple.Create(CalcType.BaseSwap, AstElem.Tags.OrIf, rt2, lt2),
								Tuple.Create(CalcType.True, AstElem.Tags.OrCont, lePar, rv2),
								Tuple.Create(CalcType.True, AstElem.Tags.OrIf, lv2, rv2),
								Tuple.Create(CalcType.TrueSwap, AstElem.Tags.OrCont, rePar, lv2),
								Tuple.Create(CalcType.TrueSwap, AstElem.Tags.OrIf, rv2, lv2),
								Tuple.Create(CalcType.Key, AstElem.Tags.OrKey, lq, rq),
							}
						);
					BestTMap.Add(hashKey, ret2);
					return ret2;

			}
			throw new Exception("Unreachable");
		}
		private Cost MinCostOf(
			double pSat,
			double pDissat,
			double lweight,
			double rweight,
			CostCalculationInfo[] info
			)
		{
			var champion = Cost.Dummy();
			foreach (var i in info)
			{
				// In case for Q expression, item might be null
				if (i.Item3 != null && i.Item4 != null)
				{
					var challenger = GetCost(i.Item1, i.Item2, i.Item3, i.Item4, pSat, pDissat, lweight, rweight);
					champion = MinCost(champion, challenger, pSat, pDissat);
				}
			}
			return champion;
		}

		private Cost GetCost(CalcType t, int parentType, Cost l, Cost r, double pSat, double pDissat, double lweight, double rweight)

		{
			if (t == CalcType.Base)
			{
				return Cost.FromPair(l, r, parentType, lweight, rweight);
			}
			else if (t == CalcType.BaseSwap)
			{
				return Cost.FromPair(l, r, parentType, rweight, lweight);
			}
			else if (t == CalcType.Cond)
			{
				var baseCost = Cost.FromPair(l, r, parentType, lweight, rweight);
				baseCost = baseCost.Ast.IsV() ? Cost.True(baseCost) : baseCost;
				var one = Cost.Likely(baseCost);
				var two = Cost.Unlikely(baseCost);
				return MinCost(one, two, pSat, pDissat);
			}
			else if (t == CalcType.CondSwap)
			{
				var baseCost = Cost.FromPair(l, r, parentType, rweight, lweight);
				baseCost = baseCost.Ast.IsV() ? Cost.True(baseCost) : baseCost;
				var one = Cost.Likely(baseCost);
				var two = Cost.Unlikely(baseCost);
				return MinCost(one, two, pSat, pDissat);
			}
			else if (t == CalcType.True)
			{
				return Cost.True(Cost.FromPair(l, r, parentType, lweight, rweight));
			}
			else if (t == CalcType.TrueSwap)
			{
				return Cost.True(Cost.FromPair(l, r, parentType, rweight, lweight));
			}
			else if (t == CalcType.Key)
			{
				var one = Cost.FromPair(l, r, parentType, lweight, rweight);
				var two = Cost.FromPair(r, l, parentType, rweight, lweight);
				return MinCost(one, two, pSat, pDissat);
			}
			else if (t == CalcType.KeyCond)
			{
				var baseCost = Cost.True(Cost.FromPair(l, r, AstElem.Tags.OrKeyV, lweight, rweight));
				var swapCost = Cost.True(Cost.FromPair(r, l, AstElem.Tags.OrKeyV, rweight, lweight));
				var costs = new Cost[]
				{
					Cost.Unlikely(baseCost),
					Cost.Likely(baseCost),
					Cost.Unlikely(swapCost),
					Cost.Likely(swapCost)
				};
				return costs.Aggregate((acc, a) => MinCost(acc, a, pSat, pDissat));
			}
			throw new Exception("Unreachable");
		}

		private Cost MinCost(Cost one, Cost two, double pSat, double pDissat)
		{
			var WeightOne = one.PkCost + pSat * one.SatCost + pDissat * one.DissatCost;
			var WeightTwo = two.PkCost + pSat * two.SatCost + pDissat * two.DissatCost;
			if (WeightOne < WeightTwo)
			{
				return one;
			}
			else if (WeightTwo < WeightOne)
			{
				return two;
			}
			else
			{
				if (one.SatCost < two.SatCost)
					return one;
				else
					return two;
			}
		}

		internal Cost FoldCostList(List<Cost> cs, double pSat, double pDissat)
			=> cs.Aggregate((acc, a) => MinCost(acc, a, pSat, pDissat));
	}
}