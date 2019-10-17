using System.Runtime.InteropServices;

namespace NBitcoin.Scripting.Miniscript.Types
{
	internal class Malleability : Property
	{
		internal Dissat Dissat;
		internal bool Safe;
		internal bool NonMalleable;

		private Malleability(Dissat dissat, bool safe, bool nonMalleable)
		{
			Dissat = dissat;
			Safe = safe;
			NonMalleable = nonMalleable;
		}

		internal bool IsSubtype(Malleability other) =>
			this.Dissat.IsSubtype((other.Dissat)) &&
			(!(!(this.Safe) && other.Safe)) &&
			(!(!(this.NonMalleable) && other.NonMalleable));

		internal static Malleability FromTrue() =>
			new Malleability(Dissat.Unique, false, true);

		internal static Malleability FromFalse() =>
			new Malleability(Dissat.Unique,true, true);
		internal static Malleability FromPk() =>
			new Malleability(Dissat.Unique,true, true);
	}

	/// <summary>
	/// Whether the fragment has a dissatisfaction, and if so, whether
	/// it is unique. Affects both correctness and malleability-freeness,
	/// since we assume 3rd parties are able to produce dissatisfactions
	/// for all fragments.
	/// </summary>
	internal enum Dissat
	{
		None,
		Unique,
		Unknown
	}

	internal static class DissatExtension
	{
		internal static bool IsSubtype(this Dissat self, Dissat other) =>
			(self == other) || other == Dissat.Unknown;

	}
}
