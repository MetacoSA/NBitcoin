namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// Type property representing exceptions about how many inputs
	/// the fragment accepts, and assumptions about that.
	/// </summary>
	public enum Input
	{
		Zero,
		One,
		Any,
		OneNonZero,
		AnyNonZero
	}
}
