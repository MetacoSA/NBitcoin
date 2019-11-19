using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NBitcoin.Scripting.Miniscript.Types;
using NBitcoin.Scripting.Parser;

// When parsing from a script, PubKey is always represented as a "real" PubKey, not by other
// `IMiniscriptKey` such as HDKey or dummy key.
// So we constrain generic parameters like this.
using MS = NBitcoin.Scripting.Miniscript.Miniscript<NBitcoin.PubKey, NBitcoin.uint160>;
using PM =
	NBitcoin.Scripting.Parser.Parser<
		NBitcoin.Scripting.Miniscript.ScriptToken,
		NBitcoin.Scripting.Miniscript.Miniscript<NBitcoin.PubKey, NBitcoin.uint160>
	>;
using Terminal = NBitcoin.Scripting.Miniscript.Terminal<NBitcoin.PubKey, NBitcoin.uint160>;

namespace NBitcoin.Scripting.Miniscript
{
	internal static class ScriptParser
	{
		# region helpers
		private static PM CheckTerminal(Terminal term)
			=> i => {
				var errors = new List<FragmentPropertyException>();
				if (Property<MiniscriptFragmentType, PubKey, uint160>.TypeCheck(term, out var fragmentType, errors)
						&& Property<ExtData, PubKey, uint160>.TypeCheck(term, out var extData, errors))
					return ParserResult<ScriptToken, MS>.Success(i, new MS(fragmentType, term, extData));
				return ParserResult<ScriptToken, MS>.Failure(i, $"inner type of the miniscript was not valid. ErrorMsg: {errors.Flatten()}");

			};

		# endregion
		# region leaf
		private static readonly Parser<ScriptToken, uint> PNumber
			=
			from z in Parse.ScriptToken<ScriptToken.Number>()
			select z.Item;

		private static readonly PM PPubkey
			=
			from pk in Parse.ScriptToken<ScriptToken.Pk>()
			from ms in CheckTerminal(Terminal.NewPk(pk.Item))
			select ms;

		private static readonly PM PPubkeyHash
			=
			from _v in Parse.ScriptToken(ScriptToken.Verify)
			from e in Parse.ScriptToken(ScriptToken.Equal)
			from hash20 in Parse.ScriptToken<ScriptToken.Hash20>()
			from _hash160 in Parse.ScriptToken(ScriptToken.Hash160)
			from _dup in Parse.ScriptToken(ScriptToken.Dup)
			from ms in CheckTerminal(Terminal.NewPkH(hash20.Item))
			select ms;

		private static readonly PM PCheckSig
			=
			from _c in Parse.ScriptToken(ScriptToken.CheckSig)
			from sub in Parse.Ref(() => PExpr)
			from ms in CheckTerminal(Terminal.NewCheck(sub))
			select ms;

		private static PM POlder
			=
			from _t in Parse.ScriptToken(ScriptToken.CheckSequenceVerify)
			from x in PNumber
			from ms in CheckTerminal(Terminal.NewOlder(x))
			select ms;

		private static PM PAfter
			=
				from _t in Parse.ScriptToken(ScriptToken.CheckLocktimeVerify)
				from x in PNumber
				from ms in CheckTerminal(Terminal.NewAfter(x))
				select ms;

		private static Parser<ScriptToken, int> PHashInner
			=
			from _sha256 in Parse.ScriptToken(ScriptToken.Verify)
			from _equal in Parse.ScriptToken(ScriptToken.Equal)
			from num in PNumber
			where num == 32u
			from _s in Parse.ScriptToken(ScriptToken.Size)
			select 1;
		private static PM PSha256
			=
			from hash in Parse.ScriptToken<ScriptToken.Hash32>()
			from _hashop in Parse.ScriptToken(ScriptToken.Sha256)
			from _dummy in PHashInner
			from ms in CheckTerminal(Terminal.NewSha256(hash.Item))
			select ms;
		private static PM PHash256
			=
			from hash in Parse.ScriptToken<ScriptToken.Hash32>()
			from _hashop in Parse.ScriptToken(ScriptToken.Hash256)
			from _dummy in PHashInner
			from ms in CheckTerminal(Terminal.NewHash256(hash.Item))
			select ms;
		private static PM PRipemd160
			=
			from hash in Parse.ScriptToken<ScriptToken.Hash20>()
			from _hashOp in Parse.ScriptToken(ScriptToken.Ripemd160)
			from _dummy in PHashInner
			from ms in CheckTerminal(Terminal.NewRipemd160(hash.Item))
			select ms;
		private static PM PHash160
			=
			from hash in Parse.ScriptToken<ScriptToken.Hash20>()
			from _hashOp in Parse.ScriptToken(ScriptToken.Hash160)
			from _dummy in PHashInner
			from ms in CheckTerminal(Terminal.NewHash160(hash.Item))
			select ms;
		private static PM PHashLocks =
			PSha256.Or(PHash256).Or(PRipemd160).Or(PHash160);

		private static PM PFalse
			=
			from num in PNumber
			where num == 0u
			select MS.FromAst(Terminal.NewFalse());

		private static PM PTrue
			=
			from num in PNumber
			where num == 1u
			select MS.FromAst(Terminal.NewTrue());

		# endregion

		# region unary wrappers
		private static readonly PM PAlt
			=
				from _alt1 in Parse.ScriptToken(ScriptToken.FromAltStack)
				from expr in Parse.Ref(() => PExpr)
				from alt in Parse.ScriptToken(ScriptToken.ToAltStack)
				from ms in  CheckTerminal(Terminal.NewAlt(expr))
				select ms;

		private static readonly PM PSwap
			=
				from expr in Parse.Ref(() => PExpr)
				from _swap in Parse.ScriptToken(ScriptToken.Swap)
				from ms in CheckTerminal(Terminal.NewSwap(expr))
				select ms;

		private static readonly PM PDupIf
			=
				from _dupif in Parse.ScriptToken(ScriptToken.EndIf)
				from v in Parse.Ref(() => PExpr)
				from _if in Parse.ScriptToken(ScriptToken.If)
				from _dup in Parse.ScriptToken(ScriptToken.Dup)
				from ms in CheckTerminal(Terminal.NewDupIf(v))
				select ms;
		private static readonly PM PVerify
			=
			(
				from v in Parse.ScriptToken(ScriptToken.Verify)
				from inner in Parse.Ref(() => PExpr)
				from ms in CheckTerminal(Terminal.NewVerify(inner))
				select ms
			);

		private static readonly PM PNonZero
			=
				from _x in Parse.ScriptToken(ScriptToken.EndIf)
				from inner in Parse.Ref(() => PExpr)
				from _if in Parse.ScriptToken(ScriptToken.If)
				from _zeronotequal in Parse.ScriptToken(ScriptToken.ZeroNotEqual)
				from _size in Parse.ScriptToken(ScriptToken.Size)
				from ms in CheckTerminal(Terminal.NewNonZero(inner))
				select ms;
		private static readonly PM PZeroNotEqual
			=
				from _x in Parse.ScriptToken(ScriptToken.ZeroNotEqual)
				from inner in Parse.Ref(() => PExpr)
				from ms in CheckTerminal(Terminal.NewZeroNotEqual(inner))
				select ms;

		# endregion

		# region binary expressions
		// ------ conjunctions ------

		private static readonly PM PMaybeAndV
			=
				from expr1 in
					Parse.Ref(() =>
						PExpr
							.Except(
								Parse.ScriptToken(ScriptToken.If)
								.Or(Parse.ScriptToken(ScriptToken.NotIf))
								.Or(Parse.ScriptToken(ScriptToken.Else))
								.Or(Parse.ScriptToken(ScriptToken.ToAltStack)))
					)
				from expr2 in Parse.Ref(() => PExpr)
				from ms in CheckTerminal(Terminal.NewAndV(expr2, expr1))
				select ms;

		private static PM PAndB
			=
			from _booland in Parse.ScriptToken(ScriptToken.BoolAnd)
			from w in Parse.Ref(() => PExpr)
			from e in Parse.Ref(() => PExpr)
			from ms in CheckTerminal(Terminal.NewAndB(e, w))
			select ms;
		// -----------------------

		// ------ disjunctions ------
		private static PM POrB
			=
			from _e in Parse.ScriptToken(ScriptToken.BoolOr)
			from inner1 in Parse.Ref(() => PExpr)
			from _ in Parse.Ref(() => PSwap)
			from inner2 in Parse.Ref(() => PExpr)
			from ms in CheckTerminal(Terminal.NewOrB(inner2, inner1))
			select ms;

		private static PM POrD
			=
			from _endif in Parse.ScriptToken(ScriptToken.EndIf)
			from inner1TOrE in Parse.Ref(() => PExpr)
			from _notif in Parse.ScriptToken(ScriptToken.NotIf)
			from _ifdup in Parse.ScriptToken(ScriptToken.IfDup)
			from inner2E in Parse.Ref(() => PExpr)
			from ms in CheckTerminal(Terminal.NewOrD(inner2E, inner1TOrE))
			select ms;

		private static PM POrC
			=
			from _endif in Parse.ScriptToken(ScriptToken.EndIf)
			from inner1v in Parse.Ref(() => PExpr)
			from _notif in Parse.ScriptToken(ScriptToken.NotIf)
			from inner2e in Parse.Ref(() => PExpr)
			from ms in CheckTerminal(Terminal.NewOrC(inner2e, inner1v))
			select ms;

		private static PM POrI
			=
			from _endif in Parse.ScriptToken(ScriptToken.EndIf)
			from inner1 in Parse.Ref(() => PExpr)
			from _else in Parse.ScriptToken(ScriptToken.Else)
			from inner2 in Parse.Ref(() => PExpr)
			from _if in Parse.ScriptToken(ScriptToken.If)
			from ms in CheckTerminal(Terminal.NewOrI(inner2, inner1))
			select ms;

		# endregion

		# region ternary and threshold
		private static PM PThresh
			=
			from _equal in Parse.ScriptToken(ScriptToken.Equal)
			from k in PNumber
			from ws in (Parse.ScriptToken(ScriptToken.Add).Then(_ => Parse.Ref(() => PExpr))).AtLeastOnce()
			from e in Parse.Ref(() => PExpr)
	 		from ms in CheckTerminal(Terminal.NewThresh(k, ws.Reverse().Concat(new [] { e })))
			select ms;
		private static PM PAndOr
			=
				from _endif in Parse.ScriptToken(ScriptToken.EndIf)
				from inner1TOrE in Parse.Ref(() => PExpr)
				from _else in Parse.ScriptToken(ScriptToken.Else)
				from inner2 in Parse.Ref(() => PExpr)
				from _notif in Parse.ScriptToken(ScriptToken.NotIf)
				from inner3 in Parse.Ref(() => PExpr)
				select MS.FromAst(Terminal.NewAndOr(inner3, inner2, inner1TOrE));

		private static PM PThreshM
			=
			from _ck in Parse.ScriptToken(ScriptToken.CheckMultiSig)
			from nPubKey in PNumber
			where nPubKey <= 20u
			from pks in Parse.ScriptToken<ScriptToken.Pk>().AtLeastOnce()
			let pksInner = pks.Select(pk => pk.Item).ToList()
			where pksInner.Count == nPubKey
			from k in PNumber
			where k <= pksInner.Count
			from ms in CheckTerminal(Terminal.NewThreshM(k, pksInner))
			select ms;

		# endregion

		///<summary>
		/// Optimize speed by not checking failure case?
		///</summary>
		private static readonly PM PExpr =
			// -------- tree --------
			PTrue
				.Or(PFalse)
				.Or(PPubkey)
				.Or(PPubkeyHash)
				.Or(PAfter)
				.Or(POlder)
				.Or(PHashLocks)
				// -------- wrapper -------
				.Or(PAlt)
				.Or(PSwap)
				.Or(PCheckSig)
				.Or(PDupIf)
				.Or(PVerify)
				.Or(PNonZero)
				.Or(PZeroNotEqual)
				// --------- conjunctions --------
				.Or(PMaybeAndV)
				.Or(PAndB)
				.Or(PAndOr)
				// --------- disjunctions --------
				.Or(POrB)
				.Or(POrD)
				.Or(POrC)
				.Or(POrI)
				.Or(PThresh)
				.Or(PThreshM);

	}
}
