using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NBitcoin.Scripting.Miniscript.Types;
using NBitcoin.Scripting.Parser;

/// When parsing from script, PubKey is always represented as a "real" PubKey, not by other
/// `IMiniscriptKey` such as HDKey or dummy key.
/// So we constrain generic parameters like this.
using MS = NBitcoin.Scripting.Miniscript.Miniscript<NBitcoin.PubKey, NBitcoin.uint160>;
using PM =
	NBitcoin.Scripting.Parser.Parser<
		NBitcoin.Scripting.Miniscript.ScriptToken,
		NBitcoin.Scripting.Miniscript.Miniscript<NBitcoin.PubKey, NBitcoin.uint160>
	>;
using Terminal = NBitcoin.Scripting.Miniscript.Terminal<NBitcoin.PubKey, NBitcoin.uint160>;

namespace NBitcoin.Scripting.Miniscript
{
	internal static class TerminalStackExtension
	{
		internal static void Reduce0<TPk, TPKh>(this Stack<Miniscript<TPk, TPKh>> self, Terminal<TPk, TPKh> term)
			where TPk : class, IMiniscriptKey<TPKh>, new()
			where TPKh : class, IMiniscriptKeyHash, new()
		{
			var e = new List<FragmentPropertyException>();
			if (Property<MiniscriptFragmentType, TPk, TPKh>.TypeCheck(term, out var ty, e)
				&& Property<ExtData, TPk, TPKh>.TypeCheck(term, out var ext, e))
				self.Push(new Miniscript<TPk,TPKh>(ty, term, ext));
			throw new ParsingException("Failed to reduce");
		}

		internal static void Reduce1<TPk, TPKh>(
			this Stack<Miniscript<TPk, TPKh>> self,
			Func<Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> wrap)
			where TPk : class, IMiniscriptKey<TPKh>, new()
			where TPKh : class, IMiniscriptKeyHash, new()
			=> self.Reduce0(wrap(self.Pop()));

		internal static void Reduce2<TPk, TPKh>(
			this Stack<Miniscript<TPk, TPKh>> self,
			Func<Miniscript<TPk, TPKh>, Miniscript<TPk, TPKh>, Terminal<TPk, TPKh>> wrap)
			where TPk : class, IMiniscriptKey<TPKh>, new()
			where TPKh : class, IMiniscriptKeyHash, new()
			=> self.Reduce0(wrap(self.Pop(), self.Pop()));
	}
	internal static class ScriptParser
	{
		private static readonly Parser<ScriptToken, uint> PNumber
			=
			from z in Parse.ScriptToken<ScriptToken.Number>()
			select z.Item;

		private static readonly PM PPubkey
			=
			from pk in Parse.ScriptToken<ScriptToken.Pk>()
			select MS.FromAst(Terminal.NewPk(pk.Item));

		private static readonly PM PCheckSig
			=
			from _c in Parse.ScriptToken(ScriptToken.CheckSig)
			from sub in Parse.Ref(() => PExpr)
			select MS.FromAst(Terminal.NewCheck(sub));

		private static readonly PM PVerify
			=
			from _v in Parse.ScriptToken(ScriptToken.Verify)
			from e in Parse.ScriptToken(ScriptToken.Equal)
			from hash20 in Parse.ScriptToken<ScriptToken.Hash20>()
			from _hash160 in Parse.ScriptToken(ScriptToken.Hash160)
			from _dup in Parse.ScriptToken(ScriptToken.Dup)
			select MS.FromAst(Terminal.NewPkH(hash20.Item));

		private static readonly Parser<ScriptToken, MS> PExpr =
			PPubkey
				.Or(PCheckSig);
		# region ported rust-miniscript

		private class TermOrRecurse
		{

		}

		/*
		private static void MatchToken(Stack<ScriptToken> tokens, List<Tuple<int, Action<ScriptToken>>> action)
		{
			if (tokens.Count() == 1)
				action(tokens.First());
			else if (tokens.TryPeek(out var token))
			{
				if (token.Equals(pattern.First()))
				{
					MatchToken(tokens, pattern.Skip(1).ToList(), action);
				}
				else
				{
					throw new ParsingException($"Unexpected {token}");
				}
			}
			else
				throw new ParsingException("Unexpected start!");
		}
		*/
		public static Miniscript<TPk, TPKh> ParseMiniscript(Stack<ScriptToken> tokens)
		{
			var nonTerm = new Stack<NonTerm>();
			var term = new Stack<Miniscript<TPk, TPKh>>();
			nonTerm.Push(NonTerm.MaybeAndV);
			nonTerm.Push(NonTerm.MaybeSwap);
			nonTerm.Push(NonTerm.Expression);
			while (true)
			{
				switch (nonTerm.Pop())
				{
					case NonTerm x when (x == NonTerm.Expression):
						MatchToken(tokens, );
						break;
					case NonTerm x when (x == NonTerm.MaybeAndV):
						if (tokens.TryPeek(out var maybeAndV))
						{
							if (!(maybeAndV.Equals(ScriptToken.If)
							      || maybeAndV.Equals(ScriptToken.NotIf)
							      || maybeAndV.Equals(ScriptToken.Else)
							      || maybeAndV.Equals(ScriptToken.ToAltStack)))
							{
								nonTerm.Push(NonTerm.AndV);
								nonTerm.Push(NonTerm.Expression);
							}
						}
						break;
					case NonTerm x when (x == NonTerm.MaybeSwap):
						if (tokens.TryPeek(out var maybeSwap) && maybeSwap.Equals(ScriptToken.Swap))
						{
							tokens.Pop();
							term.Reduce1(Terminal<TPk, TPKh>.NewSwap);
							nonTerm.Push(NonTerm.MaybeSwap);
						}
						break;
					case NonTerm x when (x == NonTerm.Alt):
						break;
					case NonTerm x when (x == NonTerm.Check):
						break;
				};
			}
		}
		# endregion
	}
}
