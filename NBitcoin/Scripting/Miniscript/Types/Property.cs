using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript.Types
{
	internal static class Property<T, TPk, TPKh>
	where T : IProperty<T>, new()
	where TPk : class, IMiniscriptKey<TPKh>, new()
	where TPKh : class, IMiniscriptKeyHash, new()
	{

		/// <summary>
		/// Compute the type of a fragment, given a function to look up
		/// the types of its children, if available and relevant for the
		/// given fragment.
		/// </summary>
		internal static bool TryTypeCheck(Terminal<TPk, TPKh> fragment, Func<int, T> child, out T res, List<FragmentPropertyException> errors)
		{
			if (TryTypeCheckCore(fragment, child, out res, errors))
			{
				res.SanityChecks();
				return true;
			}
			return false;
		}

		internal static bool TypeCheck(Terminal<TPk, TPKh> fragment, out T res, List<FragmentPropertyException> errors)
			=> TryTypeCheck(fragment, (_) => null, out res, errors);

		private static bool TryTypeCheckCore(Terminal<TPk, TPKh> fragment,Func<int, T> child, out T result, List<FragmentPropertyException> errors)
		{
			result = null;
			if (fragment is null)
				throw new ArgumentNullException(nameof(fragment));

			bool TryGetChild(Terminal<TPk, TPKh> sub, int n, out T subResult)
				{
						var r = child(n);
						if (r is null)
							return TryTypeCheck(sub, _ => null, out subResult, errors);
						subResult = r;
						return true;
				}

			switch (fragment.Tag)
			{
				case Terminal<TPk, TPKh>.Tags.True:
					result = new T().FromTrue();
					return true;
				case Terminal<TPk, TPKh>.Tags.False:
					result = new T().FromFalse();
					return true;
			}

			switch (fragment)
			{
				case Terminal<TPk, TPKh>.Pk self:
					result = new T().FromPk();
					return true;
				case Terminal<TPk, TPKh>.PkH self:
					result = new T().FromPkH();
					return true;
				case Terminal<TPk, TPKh>.ThreshM self:
					if (self.Item1 == 0)
					{
						errors.Add(FragmentPropertyException.ZeroThreshold(fragment.ToString()));
						return false;
					}

					if (self.Item1 > self.Item2.Length)
					{
						errors.Add(FragmentPropertyException.OverThreshold(
							fragment.ToString()
						));
						return false;
					}
					result = new T().FromMulti((int)self.Item1, self.Item2.Length);
					return true;
				case Terminal<TPk, TPKh>.After self:
					if (self.Item == 0)
					{
						errors.Add(FragmentPropertyException.ZeroTime(fragment.ToString()));
						return false;
					}

					result = new T().FromAfter(self.Item);
					return true;
				case Terminal<TPk, TPKh>.Older self:
					if (self.Item == 0)
					{
						errors.Add(FragmentPropertyException.ZeroTime(fragment.ToString()));
						return false;
					}

					result = new T().FromOlder(self.Item);
					return true;
				case Terminal<TPk, TPKh>.Sha256 self:
					result = new T().FromSha256();
					return true;
				case Terminal<TPk, TPKh>.Hash256 self:
					result = new T().FromHash256();
					return true;
				case Terminal<TPk, TPKh>.Ripemd160 self:
					result = new T().FromRipemd160();
					return true;
				case Terminal<TPk, TPKh>.Hash160 self:
					result = new T().FromHash160();
					return true;
				case Terminal<TPk, TPKh>.Alt self:
					return (TryGetChild(self.Item.Node, 0, out var subAlt) && subAlt.TryCastAlt(out result, errors));
				case Terminal<TPk, TPKh>.Swap self:
					return (TryGetChild(self.Item.Node, 0, out var subSwap) && subSwap.TryCastSwap(out result, errors));
				case Terminal<TPk, TPKh>.Check self:
					return (TryGetChild(self.Item.Node, 0, out var subCheck) && subCheck.TryCastCheck(out result, errors));
				case Terminal<TPk, TPKh>.DupIf self:
					return (TryGetChild(self.Item.Node, 0, out var subDupIf) && subDupIf.TryCastDupIf(out result, errors));
				case Terminal<TPk, TPKh>.Verify self:
					return (TryGetChild(self.Item.Node, 0, out var subVerify) &&
					        subVerify.TryCastVerify(out result, errors));
				case Terminal<TPk, TPKh>.NonZero self:
					return (TryGetChild(self.Item.Node, 0, out var subNonZero) &&
					        subNonZero.TryCastNonZero(out result, errors));
				case Terminal<TPk, TPKh>.ZeroNotEqual self:
					return (TryGetChild(self.Item.Node, 0, out var sub0NotEqual) &&
					        sub0NotEqual.TryCastZeroNotEqual(out result, errors));
				case Terminal<TPk, TPKh>.AndB self:
					return (TryGetChild(self.Item1.Node, 0, out var andBL)
						&& TryGetChild(self.Item2.Node, 1, out var andBR)
						&& new T().TryAndB(andBL, andBR, out result, errors)) ;
				case Terminal<TPk, TPKh>.AndV self:
					return
						TryGetChild(self.Item1.Node, 0, out var andVL)
						&& TryGetChild(self.Item2.Node, 1, out var andVR)
						&& new T().TryAndV(andVL, andVR, out result, errors);
				case Terminal<TPk, TPKh>.OrB self:
					return
						TryGetChild(self.Item1.Node, 0, out var orBL)
						&& TryGetChild(self.Item2.Node, 1, out var orBR)
						&& new T().TryOrB(orBL, orBR, out result, errors);
				case Terminal<TPk, TPKh>.OrD self:
					return
						TryGetChild(self.Item1.Node, 0, out var orDL)
						&& TryGetChild(self.Item2.Node, 1, out var orDR)
						&& new T().TryOrD(orDL, orDR, out result, errors);
				case Terminal<TPk, TPKh>.OrC self:
					return
						TryGetChild(self.Item1.Node, 0, out var orCL)
						&& TryGetChild(self.Item2.Node, 1, out var orCR)
						&& new T().TryOrC(orCL, orCR, out result, errors);
				case Terminal<TPk, TPKh>.OrI self:
					return
						TryGetChild(self.Item1.Node, 0, out var orIL)
						&& TryGetChild(self.Item2.Node, 1, out var orIR)
						&& new T().TryOrI(orIL, orIR, out result, errors);
				case Terminal<TPk, TPKh>.AndOr self:
					return
						TryGetChild(self.Item1.Node, 0, out var a)
						&& TryGetChild(self.Item2.Node, 1, out var b)
						&& TryGetChild(self.Item3.Node, 2, out var c)
						&& new T().TryAndOr(a, b, c, out result, errors);
				case Terminal<TPk, TPKh>.Thresh self:
					if (self.Item1 == 0)
					{
						errors.Add(FragmentPropertyException.ZeroThreshold(fragment.ToString()));
						return false;
					}

					if (self.Item1 > self.Item2.Length)
					{
						errors.Add(FragmentPropertyException.OverThreshold(fragment.ToString()));
						return false;
					}

					IProperty<T>.SubCk subCk =
						(int i, out T property, List<FragmentPropertyException> list) => (TryGetChild(self.Item2[i].Node, i, out property));

					return (new T().TryThreshold(
						(int) self.Item1,
						self.Item2.Length, subCk, out result, errors));
			}
			throw new Exception($"Unreachable! {fragment}");;
		}
	}
}
