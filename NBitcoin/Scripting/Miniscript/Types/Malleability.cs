using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript.Types
{
	/// <summary>
	/// Whether the fragment has a dissatisfaction, and if so, whether it is unique.
	/// Affects both correctness and malleability-freeness, since we assume 3rd parties
	/// are able to produce dissatisfactions for all fragments.
	/// </summary>
	internal enum Dissat
	{
		/// <summary>
		/// Fragment has no dissatisfaction and will abort given non-satisfying input
		/// </summary>
		None,
		/// <summary>
		/// Fragment has a unique dissatisfaction, which is always available.
		/// </summary>
		Unique,

		/// <summary>
		/// No assumptions may be made about dissatisfying this fragment. this
		/// does not necessarily mean that there are multiple dissatisfactions;
		/// There may be none, or none that are always available (e.g. for a `pk_h`
		/// the key preimage may not be available.)
		/// </summary>
		Unknown
	}

	internal static class DissatExtension
	{
		internal static bool IsSubType(this Dissat self, Dissat other)
		{
			if (self == other) return true;
			if (other == Dissat.Unknown) return true;
			return false;
		}

		public static string DebugPrint(this Dissat self)
			=> self == Dissat.None ? "f" :
				self == Dissat.Unique ? "e" :
				"";
	}

	internal class Malleability : IProperty<Malleability>
	{
		internal readonly Dissat Dissat;
		internal readonly bool Safe;
		internal readonly bool NonMalleable;

		public Malleability(Dissat dissat, bool safe, bool nonMalleable)
		{
			Dissat = dissat;
			Safe = safe;
			NonMalleable = nonMalleable;
		}

		public Malleability() {}

		internal bool IsSubtype(Malleability other) =>
			Dissat.IsSubType(other.Dissat) &&
				!(!this.Safe && other.Safe) &&
				!(!NonMalleable && other.NonMalleable);

		public override Malleability FromTrue() =>
			new Malleability(Dissat.None, false, true);

		public override Malleability FromFalse() =>
			new Malleability(Dissat.Unique, true, true);

		public override Malleability FromPk() =>
			new Malleability(Dissat.Unique, true, true);

		public override Malleability FromPkH() =>
			new Malleability(Dissat.Unique, true, true);

		public override Malleability FromMulti(int k, int pkLength) =>
			new Malleability(Dissat.Unique, true,true);

		public override Malleability FromTime(uint time) =>
			new Malleability(Dissat.None, false, true);

		public override Malleability FromHash() =>
			new Malleability(Dissat.Unknown, false, true);

		public override bool TryCastAlt(out Malleability result, List<FragmentPropertyException> error)
		{
			result = this;
			return true;
		}

		public override bool TryCastSwap(out Malleability result, List<FragmentPropertyException> error)
		{
			result = this;
			return true;
		}

		public override bool TryCastCheck(out Malleability result, List<FragmentPropertyException> error)
		{
			result = this;
			return true;
		}

		public override bool TryCastDupIf(out Malleability result, List<FragmentPropertyException> error)
		{
			result =
			new Malleability(
				(Dissat == Dissat.None ? Dissat.Unique : Dissat.Unknown),
				Safe,
				NonMalleable
				);
			return true;
		}

		public override bool TryCastVerify(out Malleability result, List<FragmentPropertyException> error)
		{
			result =
			new Malleability(
				Dissat.None,
				Safe,
				NonMalleable
			);
			return true;
		}

		public override bool TryCastNonZero(out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				Dissat == Dissat.None ? Dissat.Unique : Dissat.Unknown,
				Safe,
				NonMalleable
			);
			return true;
		}

		public override bool TryCastZeroNotEqual(out Malleability result, List<FragmentPropertyException> error)
		{
			result = this;
			return true;
		}

		public override bool TryCastTrue(out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				Dissat.None,
				Safe,
				NonMalleable
			);
			return true;
		}

		public override bool TryCastOrIFalse(out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				Dissat == Dissat.None ? Dissat.Unique : Dissat.Unknown,
				Safe,
				NonMalleable
			);
			return true;
		}

		public override bool TryAndB(Malleability left, Malleability right, out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				(left.Dissat == Dissat.None && right.Dissat == Dissat.None) ||
				(left.Dissat == Dissat.None && left.Safe) ||
				(right.Dissat == Dissat.None && right.Safe)
					? Dissat.None
					:
					(left.Dissat == Dissat.Unique && right.Dissat == Dissat.Unique && left.Safe && right.Safe)
						?
						Dissat.Unique
						:
						Dissat.Unknown,
				left.Safe || right.Safe,
				left.NonMalleable && right.NonMalleable
			);
			return true;
		}


		public override bool TryAndV(Malleability left, Malleability right, out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				(left.Safe) || (right.Dissat == Dissat.None) ? Dissat.None : Dissat.Unknown,
				left.Safe || right.Safe,
				left.NonMalleable && right.NonMalleable
			);
			return true;
		}

		public override bool TryOrB(Malleability left, Malleability right, out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				Dissat.Unique,
				left.Safe && right.Safe,
				left.NonMalleable
				&& left.Dissat == Dissat.Unique
				&& right.NonMalleable
				&& right.Dissat == Dissat.Unique
				&& (left.Safe || right.Safe)
			);
			return true;
		}

		public override bool TryOrD(Malleability left, Malleability right, out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				right.Dissat,
				left.Safe && right.Safe,
				left.NonMalleable
				&& left.Dissat == Dissat.Unique
				&& right.NonMalleable
				&& (left.Safe || right.Safe)
			);
			return true;
		}


		public override bool TryOrC(Malleability left, Malleability right, out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				Dissat.None,
				left.Safe && right.Safe,
				left.NonMalleable
				&& left.Dissat == Dissat.Unique
				&& right.NonMalleable
				&& (left.Safe || right.Safe)
			);
			return true;
		}

		public override bool TryOrI(Malleability left, Malleability right, out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				(left.Dissat == Dissat.None && right.Dissat == Dissat.None) ? Dissat.None :
				(left.Dissat == Dissat.Unique && right.Dissat == Dissat.None) ? Dissat.Unique :
				(left.Dissat == Dissat.None && right.Dissat == Dissat.Unique) ? Dissat.Unique :
				Dissat.Unknown,
				left.Safe && right.Safe,
				left.NonMalleable && right.NonMalleable && (left.Safe || right.Safe)
			);
			return true;
		}

		public override bool TryAndOr(Malleability a, Malleability b, Malleability c, out Malleability result, List<FragmentPropertyException> error)
		{
			result = new Malleability(
				(b.Dissat == Dissat.None && c.Dissat == Dissat.Unique) ? Dissat.Unique :
				(a.Safe && c.Dissat == Dissat.Unique) ? Dissat.Unique :
				(b.Dissat == Dissat.None && c.Dissat == Dissat.None) ? Dissat.None :
				(a.Safe && c.Dissat == Dissat.None) ? Dissat.None :
				Dissat.Unknown,
				(a.Safe || b.Safe) && c.Safe,
				a.NonMalleable
				&& b.NonMalleable
				&& c.NonMalleable
				&& a.Dissat == Dissat.Unique
				&& (a.Safe || b.Safe || c.Safe)
			);
			return true;
		}

		public override bool TryThreshold(int k, int n, SubCk ck, out Malleability result, List<FragmentPropertyException> error)
		{
			result = null;
			var safeCount = 0;
			var allAreDissatUnique = true;
			var allAreNonMalleable = true;
			for (int i = 0; i < n; i++)
			{
				if (!ck(i, out var subType, error))
					return false;
				safeCount += (subType.Safe ? 1 : 0);
				allAreDissatUnique &= subType.Dissat == Dissat.Unique;
				allAreNonMalleable &= subType.NonMalleable;
			}
			result = new Malleability(
				allAreDissatUnique && (k == 1 || safeCount == n) ? Dissat.Unique : Dissat.Unknown,
					safeCount > n - k,
					allAreNonMalleable && safeCount >= n -k && (k == n || allAreDissatUnique)
				);
			return true;
		}

	}
}
