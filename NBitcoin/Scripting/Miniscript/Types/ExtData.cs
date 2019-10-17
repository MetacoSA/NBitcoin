using System;

namespace NBitcoin.Scripting.Miniscript.Types
{
	/// <summary>
	///  Whether a fragment is OK to be used in non-segwit scripts
	/// </summary>
	internal enum LegacySafe
	{
		LegacySafe,
		SegwitOnly
	}
	internal class ExtData
	{
		internal LegacySafe LegacySafe;
		internal UInt64 PkCost;
		internal bool HasVerifyForm;
		internal UInt64 OpsCountStatic;
		internal UInt64?  OpsCountSat;
		internal UInt64? OpsCountNSat;

		/// <summary>
		///  Compute the type of a fragment, given a function to lookup
		/// the types of its children, if available and relevant for the given fragment.
		/// </summary>
		/// <param name="fragment"></param>
		/// <param name="child"></param>
		/// <returns></returns>
		internal static ExtData TypeCheck(Terminal fragment, Func<UInt64, ExtData> child)
		{
			throw new System.Exception("TODO");
		}
	}
}
