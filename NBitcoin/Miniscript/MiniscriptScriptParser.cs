using System;
using System.Linq;
using NBitcoin.Miniscript.Parser;
using P = NBitcoin.Miniscript.Parser.Parser<NBitcoin.Miniscript.ScriptToken, NBitcoin.Miniscript.AstElem>;
namespace NBitcoin.Miniscript
{
	internal static class MiniscriptScriptParser
	{

		private static readonly Parser<ScriptToken, uint> PNumber =
			from t in Parse.ScriptToken(ScriptToken.Tags.Number)
			select ((ScriptToken.Number)t).Item;

		private static readonly Parser<ScriptToken, PubKey> PPubKey =
			from t in Parse.ScriptToken(ScriptToken.Tags.Pk)
			select ((ScriptToken.Pk)t).Item;
		private static P PAndBool()
			=>
				from _ in Parse.ScriptToken(ScriptToken.BoolAnd)
				from w in Parse.Ref(() => PW())
				from e in Parse.Ref(() => PE())
				select AstElem.NewAndBool(e, w);

		private static P PBoolOr()
			=>
				from _ in Parse.ScriptToken(ScriptToken.BoolOr)
				from w in Parse.Ref(() => PW())
				from e in Parse.Ref(() => PE())
				select AstElem.NewOrBool(e, w);

		private static P PHashT()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.Equal)
				from hash in Parse.ScriptToken(ScriptToken.Tags.Sha256Hash)
				from _2 in Parse.ScriptToken(ScriptToken.Sha256)
				from _3 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from num in PNumber
				where num == 32
				from _4 in Parse.ScriptToken(ScriptToken.Size)
				select AstElem.NewHashT(((ScriptToken.Sha256Hash)hash).Item);

		private static Parser<ScriptToken, AstElem[]> ThreshSubExpr()
			=>
				from ws in
					(Parse.ScriptToken(ScriptToken.Add).Then(_ => PW()).Many())
				from e in Parse.Ref(() => PE()).Once()
				select e.Concat(ws).ToArray();
		private static P PThresh()
			=>
				from _ in Parse.ScriptToken(ScriptToken.Equal)
				from num in PNumber
				from subExprs in ThreshSubExpr()
				select (AstElem.NewThresh(num, subExprs));

		private static P PHashV()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from hash in Parse.ScriptToken(ScriptToken.Tags.Sha256Hash)
				from _2 in Parse.ScriptToken(ScriptToken.Sha256)
				from _3 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from num in PNumber
				where num == 32
				from _4 in Parse.ScriptToken(ScriptToken.Size)
				select AstElem.NewHashV(((ScriptToken.Sha256Hash)hash).Item);

		private static Parser<ScriptToken, AstElem[]> ThreshVSubExpr()
			=>
				from ws in Parse.Ref(() => PW()).Many()
				from e in Parse.Ref(() => PE()).Once()
				select e.Concat(ws.Reverse()).ToArray();

		private static P PThreshV()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EqualVerify)
				from num in PNumber
				from subExprs in ThreshVSubExpr()
				select AstElem.NewThreshV(num, subExprs);

		private static P PPkHelper()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckSig)
				from pk in Parse.ScriptToken(ScriptToken.Tags.Pk)
				select AstElem.NewPk(((ScriptToken.Pk)(pk)).Item);
		private static P PPkW()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckSig)
				from pk in Parse.ScriptToken(ScriptToken.Tags.Pk)
				from _2 in Parse.ScriptToken(ScriptToken.Swap)
				select AstElem.NewPkV(((ScriptToken.Pk)(pk)).Item);

		private static P PPk()
			=>
				from ppk in PPkHelper().Except(PPkV())
				select ppk;

		private static P POrKey()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckSig)
				from qR in Parse.Ref(() => PQ())
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from qL in Parse.Ref(() => PQ())
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrKey(qL, qR);
		private static P PPkV()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckSigVerify)
				from pk in Parse.ScriptToken(ScriptToken.Tags.Pk)
				select AstElem.NewPkV(((ScriptToken.Pk)pk).Item);

		private static P POrKeyV()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckSigVerify)
				from _2 in Parse.ScriptToken(ScriptToken.EndIf)
				from qR in Parse.Ref(() => PQ())
				from _3 in Parse.ScriptToken(ScriptToken.Else)
				from qL in Parse.Ref(() => PQ())
				from _4 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrKeyV(qL, qR);

		private static Parser<ScriptToken, Tuple<uint, PubKey[]>> PMultiHelper()
			=>
				from n in PNumber
				from pksStk in Parse.ScriptToken(ScriptToken.Tags.Pk).Repeat((int)n)
				from m in PNumber
				let pks = pksStk.Select(pkStk => ((ScriptToken.Pk)pkStk).Item).Reverse().ToArray()
				select Tuple.Create(m, pks);
		private static P PMulti()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckMultiSig)
				from t in PMultiHelper()
				select AstElem.NewMulti(t.Item1, t.Item2);

		private static P PMultiV()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckMultiSigVerify)
				from t in PMultiHelper()
				select AstElem.NewMultiV(t.Item1, t.Item2);

		private static P PTimeF()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.ZeroNotEqual)
				from _2 in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
				from n in PNumber
				select AstElem.NewTimeF(n);

		private static P PTimeT()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
				from n in PNumber
				select AstElem.NewTimeT(n);

		private static P PWrap()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.FromAltStack)
				from e in Parse.Ref(() => PE())
				from _2 in Parse.ScriptToken(ScriptToken.ToAltStack)
				select AstElem.NewWrap(e);

		private static Parser<ScriptToken, uint> PTimeWHelper()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.Drop)
				from _2 in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
				from n in PNumber
				from _3 in Parse.ScriptToken(ScriptToken.If)
				from _4 in Parse.ScriptToken(ScriptToken.Dup)
				select n;
		private static P PTimeW()
			=>
				from n in PTimeWHelper()
				from _1 in Parse.ScriptToken(ScriptToken.Swap)
				select AstElem.NewTimeW(n);

		private static P PTimeTHelper()
			=>
				from n in PTimeWHelper()
				select AstElem.NewTimeT(n);

		private static P PTime()
			=> PTimeTHelper().Except(PTimeW());

		private static P PLikelyHelper()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from n in PNumber
				where n == 0
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from f in Parse.Ref(() => PF())
				select f;
		private static P PUnlikely()
			=>
				from f in PLikelyHelper()
				from _ in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewUnlikely(f);
		private static P PLikely()
			=>
				from f in PLikelyHelper()
				from _ in Parse.ScriptToken(ScriptToken.NotIf)
				select AstElem.NewLikely(f);

		private static P POrIf1()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from qr in Parse.Ref(() => PQ())
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from ql in Parse.Ref(() => PQ())
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(ql, qr);

		private static P PHashW()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from f in Parse.Ref(() => PF())
				from _2 in Parse.ScriptToken(ScriptToken.If)
				from _3 in Parse.ScriptToken(ScriptToken.ZeroNotEqual)
				from _4 in Parse.ScriptToken(ScriptToken.Size)
				from _5 in Parse.ScriptToken(ScriptToken.Swap)
				where f.IsTrue()
				let inner = ((AstElem.True)f).Item1
				where inner.IsHashV()
				let hash = ((AstElem.HashV)inner).Item1
				select AstElem.NewHashW(hash);

		private static P PAndCascSub()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from f in Parse.Ref(() => PF())
				from _3 in Parse.ScriptToken(ScriptToken.Else)
				select f;
		private static P PAndCasc()
			=>
				from f in PAndCascSub()
				from n in PNumber
				where n == 0
				from _4 in Parse.ScriptToken(ScriptToken.NotIf)
				from e in Parse.Ref(() => PE())
				select AstElem.NewAndCasc(f, e);

		private static P POrIf2()
			=>
				from fr in PAndCascSub()
				from fl in Parse.Ref(() => PF())
				from _1 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(fl, fr);
		private static P POrNotIf()
			=>
				from fr in PAndCascSub()
				from el in Parse.Ref(() => PE())
				from _1 in Parse.ScriptToken(ScriptToken.NotIf)
				select AstElem.NewOrIf(el, fr);

		private static P POrIf3()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from vr in Parse.Ref(() => PV())
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from vl in Parse.Ref(() => PV())
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(vl, vr);

		private static P POrCont()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from vr in Parse.Ref(() => PV())
				from _2 in Parse.ScriptToken(ScriptToken.NotIf)
				from el in Parse.Ref(() => PE())
				select AstElem.NewOrCont(el, vr);

		private static P POrIf4()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from tr in Parse.Ref(() => PT())
				from _2 in Parse.ScriptToken(ScriptToken.Else)
				from tl in Parse.Ref(() => PT())
				from _3 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIf(tl, tr);

		private static P POrIf()
			=> POrIf1().Or(POrIf2()).Or(POrIf3()).Or(POrIf4());
		private static P POrCasc()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.EndIf)
				from tr in Parse.Ref(() => PT())
				from _2 in Parse.ScriptToken(ScriptToken.NotIf)
				from _3 in Parse.ScriptToken(ScriptToken.IfDup)
				from el in Parse.Ref(() => PE())
				select AstElem.NewOrCasc(el, tr);

		private static P POrIfV()
			=>
				from _1 in Parse.ScriptToken(ScriptToken.Verify)
				from _2 in Parse.ScriptToken(ScriptToken.EndIf)
				from tr in Parse.Ref(() => PT())
				from _3 in Parse.ScriptToken(ScriptToken.Else)
				from tl in Parse.Ref(() => PT())
				from _4 in Parse.ScriptToken(ScriptToken.If)
				select AstElem.NewOrIfV(tl, tr);

		private static P PTrue()
			=>
				from n in PNumber
				where n == 1
				from v in Parse.Ref(() => PV())
				select AstElem.NewTrue(v);

		private static P PPkQ()
			=>
				from pk in PPubKey
				select AstElem.NewTruee);

		private static P PW()
			=>
				from expr in PAstElem()
				where expr.IsW()
				select expr;

		private static P PF()
			=>
				from expr in PAstElem()
				where expr.IsF()
				select expr;

		private static P PQ()
			=>
				from expr in PAstElem()
				where expr.IsQ()
				select expr;

		private static P PE()
			=>
				from expr in PAstElem()
				where expr.IsE()
				select expr;

		private static P PV()
			=>
				from expr in PAstElem()
				where expr.IsV()
				select expr;

		private static P PT()
			=>
				from expr in PAstElem()
				where expr.IsT()
				select expr;

		private static P PAstElem()
			=> PW();

	}
}