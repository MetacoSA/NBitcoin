using System;
using System.Linq;
using NBitcoin.Scripting.Parser;
using P = NBitcoin.Scripting.Parser.Parser<NBitcoin.Scripting.ScriptToken, NBitcoin.Scripting.AstElem>;
namespace NBitcoin.Scripting
{
	internal static class MiniscriptScriptParser
	{

		private static readonly Parser<ScriptToken, uint> PNumber =
			from t in Parse.ScriptToken(ScriptToken.Tags.Number)
			select ((ScriptToken.Number)t).Item;

		private static readonly Parser<ScriptToken, uint256> PHash =
			from t in Parse.ScriptToken(ScriptToken.Tags.Sha256Hash)
			select ((ScriptToken.Sha256Hash)t).Item;
		private static readonly Parser<ScriptToken, PubKey> PPubKey =
			from t in Parse.ScriptToken(ScriptToken.Tags.Pk)
			select ((ScriptToken.Pk)t).Item;
		
		private static readonly P PAndBool =
				from _ in Parse.ScriptToken(ScriptToken.BoolAnd)
				from w in Parse.Ref(() => PW)
				from e in ParseShortestE
				select AstElem.NewAndBool(e, w);

		private static readonly P POrBool =
				from _ in Parse.ScriptToken(ScriptToken.BoolOr)
				from w in Parse.Ref(() => PW)
				from e in ParseShortestE
				select AstElem.NewOrBool(e, w);

		internal static readonly P PHashT =
				from _1 in Parse.ScriptToken(ScriptToken.Equal)
				from hash in PHash
				from _2 in Parse.ScriptToken(ScriptToken.Sha256)
				from _3 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from num in PNumber
				where num == 32
				from _4 in Parse.ScriptToken(ScriptToken.Size)
				select AstElem.NewHashT(hash);

		private static readonly Parser<ScriptToken, AstElem[]> ThreshSubExpr =
				from ws in
					(Parse.ScriptToken(ScriptToken.Add).Then(_ => Parse.Ref(() => PW))).AtLeastOnce()
				from e in Parse.Ref(() => ParseShortestE).Once()
				select e.Concat(ws.Reverse()).ToArray();
		internal static readonly P PThresh =
				from _ in Parse.ScriptToken(ScriptToken.Equal)
				from num in PNumber
				from subExprs in Parse.Ref(() => ThreshSubExpr)
				select (AstElem.NewThresh(num, subExprs));

		private static readonly P PHashV =
				from _1 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from hash in PHash
				from _2 in Parse.ScriptToken(ScriptToken.Sha256)
				from _3 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from num in PNumber
				where num == 32
				from _4 in Parse.ScriptToken(ScriptToken.Size)
				select AstElem.NewHashV(hash);

		private static readonly P PThreshV =
				from _1 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from num in PNumber
				from subExprs in Parse.Ref(() => ThreshSubExpr)
				select AstElem.NewThreshV(num, subExprs);

		private static readonly P PPkHelper =
				from _1 in Parse.ScriptToken(ScriptToken.CheckSig)
				from pk in PPubKey
				select AstElem.NewPk(pk);
		private static readonly P PPkW =
				from _1 in Parse.ScriptToken(ScriptToken.CheckSig)
				from pk in PPubKey
				from _2 in Parse.ScriptToken(ScriptToken.Swap)
				select AstElem.NewPkW(pk);

		internal static readonly P PPk =
				from ppk in PPkHelper.Except(Parse.Ref(() => PPkV))
				select ppk;

		private static readonly P POrKey =
				from _1 in Parse.ScriptToken(ScriptToken.CheckSig)
				from _2 in Parse.ScriptToken(ScriptToken.EndIf)
				from qR in Parse.Ref(() => PQ)
				from _3 in Parse.ScriptToken(ScriptToken.Else)
				from qL in Parse.Ref(() => PQ)
				from _4 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrKey(qL, qR);
		private static readonly P PPkV =
				from _1 in Parse.ScriptToken(ScriptToken.CheckSigVerify)
				from pk in PPubKey
				select AstElem.NewPkV(pk);

		private static readonly P POrKeyV =
				from _1 in Parse.ScriptToken(ScriptToken.CheckSigVerify)
				from _2 in Parse.ScriptToken(ScriptToken.EndIf)
				from qR in Parse.Ref(() => PQ)
				from _3 in Parse.ScriptToken(ScriptToken.Else)
				from qL in Parse.Ref(() => PQ)
				from _4 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrKeyV(qL, qR);

		private static readonly Parser<ScriptToken, Tuple<uint, PubKey[]>> PMultiHelper =
				from n in PNumber
				from pksStk in Parse.ScriptToken(ScriptToken.Tags.Pk).Repeat((int)n)
				from m in PNumber
				let pks = pksStk.Select(pkStk => ((ScriptToken.Pk)pkStk).Item).Reverse().ToArray()
				select Tuple.Create(m, pks);
		internal static readonly P PMulti =
				from _1 in Parse.ScriptToken(ScriptToken.CheckMultiSig)
				from t in PMultiHelper
				select AstElem.NewMulti(t.Item1, t.Item2);

		private static readonly P PMultiV =
				from _1 in Parse.ScriptToken(ScriptToken.CheckMultiSigVerify)
				from t in PMultiHelper
				select AstElem.NewMultiV(t.Item1, t.Item2);

		private static readonly P PTimeF =
				from _1 in Parse.ScriptToken(ScriptToken.ZeroNotEqual)
				from _2 in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
				from n in PNumber
				select AstElem.NewTimeF(n);

		internal static readonly P PTimeT =
				from _1 in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
				from n in PNumber
				select AstElem.NewTimeT(n);

		internal static readonly P PWrap =
				from _1 in Parse.ScriptToken(ScriptToken.FromAltStack)
				from e in ParseShortestE
				from _2 in Parse.ScriptToken(ScriptToken.ToAltStack)
				select AstElem.NewWrap(e);

		private static readonly P PTimeV =
			from _1 in Parse.ScriptToken(ScriptToken.Drop)
			from _2 in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
			from num in PNumber
			select AstElem.NewTimeV(num);
		private static readonly Parser<ScriptToken, uint> PTimeHelper =
				from _0 in Parse.ScriptToken(ScriptToken.EndIf)
				from _1 in Parse.ScriptToken(ScriptToken.Drop)
				from _2 in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
				from n in PNumber
				from _3 in Parse.ScriptToken(ScriptToken.If)
				from _4 in Parse.ScriptToken(ScriptToken.Dup)
				select n;
		private static readonly P PTimeW =
				from n in PTimeHelper
				from _1 in Parse.ScriptToken(ScriptToken.Swap)
				select AstElem.NewTimeW(n);

		private static readonly P PTime =
			from n in PTimeHelper.Except(PTimeW)
			select AstElem.NewTime(n);

		private static readonly P PLikelyHelper =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from n in PNumber
				where n == 0
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from f in Parse.Ref(() => PF)
				select f;
		private static readonly P PUnlikely =
				from f in PLikelyHelper
				from _ in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewUnlikely(f);
		private static readonly P PLikely =
				from f in PLikelyHelper
				from _ in Parse.ScriptToken(ScriptToken.NotIf)
				select AstElem.NewLikely(f);

		private static readonly P POrIfOfQ =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from qr in Parse.Ref(() => PQ)
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from ql in Parse.Ref(() => PQ)
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(ql, qr);

		private static readonly P PHashW =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from f in Parse.Ref(() => PF)
				from _2 in Parse.ScriptToken(ScriptToken.If)
				from _3 in Parse.ScriptToken(ScriptToken.ZeroNotEqual)
				from _4 in Parse.ScriptToken(ScriptToken.Size)
				from _5 in Parse.ScriptToken(ScriptToken.Swap)
				where f.IsTrue()
				let inner = ((AstElem.True)f).Item1
				where inner.IsHashV()
				let hash = ((AstElem.HashV)inner).Item1
				select AstElem.NewHashW(hash);

		private static readonly P PAndCascSub =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from f in Parse.Ref(() => PF)
				from _3 in Parse.ScriptToken(ScriptToken.Else)
				select f;
		internal static readonly P PAndCasc =
				from f in PAndCascSub
				from n in PNumber
				where n == 0
				from _4 in Parse.ScriptToken(ScriptToken.NotIf)
				from e in ParseShortestE
				select AstElem.NewAndCasc(e, f);

		internal static readonly P POrIfOfEF =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from r in Parse.Ref(() => PE)
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from l in Parse.Ref(() => PF)
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(l, r);

		internal static readonly P POrIfOfF =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from r in Parse.Ref(() => PF)
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from l in Parse.Ref(() => PF)
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(l, r);
		internal static readonly P POrNotIf =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from er in Parse.Ref(() => PE)
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from fl in Parse.Ref(() => PF)
				from _3 in Parse.ScriptToken(ScriptToken.NotIf)
				select AstElem.NewOrNotIf(fl, er);

		private static readonly P POrIfOfV =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from vr in Parse.Ref(() => PV)
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from vl in Parse.Ref(() => PV)
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(vl, vr);

		internal static readonly P POrCont =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from vr in Parse.Ref(() => PV)
				from _2 in Parse.ScriptToken(ScriptToken.NotIf)
				from el in ParseShortestE
				select AstElem.NewOrCont(el, vr);

		internal static readonly P POrIfOfT =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from tr in Parse.Ref(() => PT)
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from tl in Parse.Ref(() => PT)
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(tl, tr);
		private static readonly P POrCasc =
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from tr in Parse.Ref(() => PT)
				from _2 in Parse.ScriptToken(ScriptToken.NotIf)
				from _3 in Parse.ScriptToken(ScriptToken.IfDup)
				from el in ParseShortestE
				select AstElem.NewOrCasc(el, tr);

		private static readonly P POrIfV =
				from _1 in Parse.ScriptToken(ScriptToken.Verify)
				from _2 in Parse.ScriptToken(ScriptToken.EndIf)
				from tr in Parse.Ref(() => PT)
				from _3 in Parse.ScriptToken(ScriptToken.Else)
				from tl in Parse.Ref(() => PT)
				from _4 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIfV(tl, tr);

		internal static readonly P PTrue =
				from n in PNumber
				where n == 1
				from v in Parse.Ref(() => PV)
				select AstElem.NewTrue(v);

		private static readonly P PPkQ =
				from pk in PPubKey
				select AstElem.NewPkQ(pk);

		private static readonly P PW =
				from expr in Parse.Ref(() => PAstElem)
				where expr.IsW()
				select expr;

		private static readonly P PF =
				from expr in Parse.Ref(() => PAstElem)
				where expr.IsF()
				select expr;

		private static readonly P PQ =
				from expr in Parse.Ref(() => PAstElem)
				where expr.IsQ()
				select expr;

		private static readonly P PE =
				from expr in Parse.Ref(() => PAstElem)
				where expr.IsE()
				select expr;

		private static readonly P PV =
				from expr in Parse.Ref(() => PAstElem)
				where expr.IsV()
				select expr;

		internal static readonly P PT =
				from expr in Parse.Ref(() => PAstElem)
				where expr.IsT()
				select expr;

		private static readonly P PENoPostProcess =
			from expr in Parse.Ref(() => PAstElemCore)
			where expr.IsE()
			select expr;
		private static readonly P ParseShortestE =
			from e in Parse.Ref(() => PENoPostProcess).Or(Parse.Ref(() => PE))
			select e;
		internal static readonly P PAstElemCore =
				PAndBool
					.Or(POrBool)
					.Or(PHashT)
					.Or(PThresh)
					.Or(PHashV)
					.Or(PThreshV)
					.Or(PPkW)
					.Or(PPk)
					.Or(POrKey)
					.Or(PPkV)
					.Or(POrKeyV)
					.Or(PMulti)
					.Or(PMultiV)
					.Or(PTimeF)
					.Or(PTimeT)
					.Or(PWrap)
					.Or(PTimeV)
					.Or(PTimeW)
					.Or(PTime)
					.Or(PUnlikely)
					.Or(PLikely)
					.Or(POrIfOfQ)
					.Or(PHashW)
					.Or(PAndCasc)
					.Or(POrIfOfF)
					.Or(POrIfOfEF)
					.Or(POrNotIf)
					.Or(POrIfOfV)
					.Or(POrCont)
					.Or(POrIfOfT)
					.Or(POrCasc)
					.Or(POrIfV)
					.Or(PTrue)
					.Or(PPkQ);

		private static P PostProcess(AstElem ast)
			=> (IInput<ScriptToken> i) =>
			{
				if (i.AtEnd)
					return ParserResult<ScriptToken, AstElem>.Success(i, ast);
				if (ast.IsT() || ast.IsF() || ast.IsV() || ast.IsQ())
				{
					var next = i.GetCurrent();
					if (next.Equals(ScriptToken.If) || next.Equals(ScriptToken.NotIf) || next.Equals(ScriptToken.Else) || next.Equals(ScriptToken.ToAltStack))
						return ParserResult<ScriptToken, AstElem>.Success(i, ast);
					else
					{
						var leftResult = PAstElem(i);
						if (leftResult.IsSuccess)
						{
							var left = leftResult.Value;
							if (!left.IsV())
							{
								return ParserResult<ScriptToken, AstElem>.Failure(i, "SubExpression was not V");
							}
							return ParserResult<ScriptToken, AstElem>.Success(leftResult.Rest, AstElem.NewAndCat(left, ast));
						}
						return leftResult;
					}
				}
				else
					return ParserResult<ScriptToken, AstElem>.Success(i, ast);
			};
		internal static readonly P PAstElem =
			PAstElemCore.Bind(ast => Parse.Ref(() => PostProcess(ast)));

		public static AstElem ParseScript(Script sc)
			=> PT.Parse(sc);
	}
}