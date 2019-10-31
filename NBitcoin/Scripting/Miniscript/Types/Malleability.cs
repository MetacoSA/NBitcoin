using System;
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
				other.Safe &&
				!(!NonMalleable && other.NonMalleable);

		public Malleability FromTrue() =>
			new Malleability(Dissat.None, false, true);

		public Malleability FromFalse() =>
			new Malleability(Dissat.None, true, true);

		public Malleability FromPk() =>
			new Malleability(Dissat.Unique, true, true);

		public Malleability FromPkH() =>
			new Malleability(Dissat.Unique, true, true);

		public Malleability FromMulti(int k, int pkLength) =>
			new Malleability(Dissat.Unique, true,true);

		public Malleability FromAfter(uint time)
		{
			throw new System.NotImplementedException();
		}

		public Malleability FromOlder(uint time)
		{
			throw new System.NotImplementedException();
		}

		public Malleability FromHash() =>
			new Malleability(Dissat.None, false, true);

		public Malleability FromSha256()
		{
			throw new System.NotImplementedException();
		}

		public Malleability FromHash256()
		{
			throw new System.NotImplementedException();
		}

		public Malleability FromRipemd160()
		{
			throw new System.NotImplementedException();
		}

		public Malleability FromHash160()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastAlt() =>
			this;

		public Malleability CastSwap()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastCheck()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastDupIf()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastVerify()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastNonZero()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastZeroNotEqual()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastTrue()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastOrIFalse()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastUnLikely()
		{
			throw new System.NotImplementedException();
		}

		public Malleability CastLikely()
		{
			throw new System.NotImplementedException();
		}

		public Malleability AndB(Malleability left, Malleability right)
		{
			throw new NotImplementedException();
		}

		public Malleability AndV(Malleability left, Malleability right)
		{
			throw new NotImplementedException();
		}

		public Malleability AndN(Malleability left, Malleability right)
		{
			throw new NotImplementedException();
		}

		public Malleability OrB(Malleability left, Malleability right)
		{
			throw new NotImplementedException();
		}

		public Malleability OrD(Malleability left, Malleability right)
		{
			throw new NotImplementedException();
		}

		public Malleability OrC(Malleability left, Malleability right)
		{
			throw new NotImplementedException();
		}

		public Malleability OrI(Malleability left, Malleability right)
		{
			throw new NotImplementedException();
		}

		public Malleability AndOr(Malleability a, Malleability b, Malleability c)
		{
			throw new NotImplementedException();
		}

		public Malleability Threshold(int k, int n, Func<uint, Malleability> subCk)
		{
			throw new NotImplementedException();
		}

		public Malleability AndB()
		{
			throw new System.NotImplementedException();
		}

		public void SanityChecks()
		{
			throw new System.NotImplementedException();
		}

	}
}
