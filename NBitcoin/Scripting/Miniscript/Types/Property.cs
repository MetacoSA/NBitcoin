using System;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript.Types
{
	public static class Property<T, TPk, TPKh>
	where T : IProperty<T>, new()
	where TPk : class, IMiniscriptKey<TPKh>, new()
	where TPKh : class, IMiniscriptKeyHash, new()
	{

		/// <summary>
		/// Compute the type of a fragment, given a function to look up
		/// the types of its children, if available and relevant for the
		/// given fragment.
		/// </summary>
		internal static T TypeCheck(Terminal<TPk, TPKh> fragment, Func<int, T> child)
		{
			var res = TypeCheckCore(fragment, child);
			res.SanityChecks();
			return res;
		}

		internal static T TypeCheck(Terminal<TPk, TPKh> fragment)
			=> TypeCheck(fragment, (_) => null);

		private static T TypeCheckCore(Terminal<TPk, TPKh> fragment,Func<int, T> child)
		{
			if (fragment is null)
				throw new ArgumentNullException(nameof(fragment));

			T GetChild(Terminal<TPk, TPKh> sub, int n)
				{
						var r = child(n);
						if (r is null)
							return TypeCheck(sub, _ => null);
						return r;
				}

			switch (fragment.Tag)
			{
				case Terminal<TPk, TPKh>.Tags.True:
					return new T().FromTrue();
				case Terminal<TPk, TPKh>.Tags.False:
					return new T().FromFalse();
			}

			switch (fragment)
			{
				case Terminal<TPk, TPKh>.Pk self:
					return new T().FromPk();
				case Terminal<TPk, TPKh>.PkH self:
					return new T().FromPkH();
				case Terminal<TPk, TPKh>.ThreshM self:
					if (self.Item1 == 0)
					{
						throw FragmentPropertyException.ZeroThreshold(fragment.ToString());
					}

					if (self.Item1 > self.Item2.Length)
					{
						throw FragmentPropertyException.OverThreshold(
							fragment.ToString()
							);
					}
					return new T().FromMulti((int)self.Item1, self.Item2.Length);
				case Terminal<TPk, TPKh>.After self:
					if (self.Item == 0)
					{
						throw FragmentPropertyException.ZeroTime(fragment.ToString());
					}

					return new T().FromAfter(self.Item);
				case Terminal<TPk, TPKh>.Older self:
					if (self.Item == 0)
					{
						throw FragmentPropertyException.ZeroTime(fragment.ToString());
					}

					return new T().FromOlder(self.Item);
				case Terminal<TPk, TPKh>.Sha256 self:
					return new T().FromSha256();
				case Terminal<TPk, TPKh>.Hash256 self:
					return new T().FromHash256();
				case Terminal<TPk, TPKh>.Ripemd160 self:
					return new T().FromRipemd160();
				case Terminal<TPk, TPKh>.Hash160 self:
					return new T().FromHash160();
				case Terminal<TPk, TPKh>.Alt self:
					return GetChild(self.Item.Node, 0).CastAlt();
				case Terminal<TPk, TPKh>.Swap self:
					return GetChild(self.Item.Node, 0).CastSwap();
				case Terminal<TPk, TPKh>.Check self:
					return GetChild(self.Item.Node, 0).CastCheck();
				case Terminal<TPk, TPKh>.DupIf self:
					return GetChild(self.Item.Node, 0).CastDupIf();
				case Terminal<TPk, TPKh>.Verify self:
					return GetChild(self.Item.Node, 0).CastVerify();
				case Terminal<TPk, TPKh>.NonZero self:
					return GetChild(self.Item.Node, 0).CastNonZero();
				case Terminal<TPk, TPKh>.ZeroNotEqual self:
					return GetChild(self.Item.Node, 0).CastZeroNotEqual();
				case Terminal<TPk, TPKh>.AndB self:
					var andBL = GetChild(self.Item1.Node, 0);
					var andBR = GetChild(self.Item2.Node, 1);
					return new T().AndB(andBL, andBR);
				case Terminal<TPk, TPKh>.AndV self:
					var andVL = GetChild(self.Item1.Node, 0);
					var andVR = GetChild(self.Item2.Node, 1);
					return new T().AndV(andVL, andVR);
				case Terminal<TPk, TPKh>.OrB self:
					var orBL = GetChild(self.Item1.Node, 0);
					var orBR = GetChild(self.Item2.Node, 1);
					return new T().OrB(orBL, orBR);
				case Terminal<TPk, TPKh>.OrD self:
					var orDL = GetChild(self.Item1.Node, 0);
					var orDR = GetChild(self.Item2.Node, 1);
					return new T().OrD(orDL, orDR);
				case Terminal<TPk, TPKh>.OrC self:
					var orCL = GetChild(self.Item1.Node, 0);
					var orCR = GetChild(self.Item2.Node, 1);
					return new T().OrC(orCL, orCR);
				case Terminal<TPk, TPKh>.OrI self:
					var orIL = GetChild(self.Item1.Node, 0);
					var orIR = GetChild(self.Item2.Node, 1);
					return new T().OrI(orIL, orIR);
				case Terminal<TPk, TPKh>.AndOr self:
					var a = GetChild(self.Item1.Node, 0);
					var b = GetChild(self.Item2.Node, 1);
					var c = GetChild(self.Item3.Node, 2);
					return new T().AndOr(a, b, c);
				case Terminal<TPk, TPKh>.Thresh self:
					if (self.Item1 == 0)
					{
						throw FragmentPropertyException.ZeroThreshold(fragment.ToString());
					}

					if (self.Item1 > self.Item2.Length)
					{
						throw FragmentPropertyException.OverThreshold(fragment.ToString());
					}
					return
						new T().Threshold((int)self.Item1, self.Item2.Length, (n) =>
							GetChild(self.Item2[n].Node, (int)n));
			}
			throw new Exception($"Unreachable! {fragment}");;
		}
	}
}
