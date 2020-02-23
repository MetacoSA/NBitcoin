namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// Type property representing exceptions about how many inputs
	/// the fragment accepts, and assumptions about that.
	/// </summary>
	internal enum Input
	{
		Zero,
		One,
		Any,
		OneNonZero,
		AnyNonZero
	}

	internal static class InputExtension
	{
		public static string DebugPrint(this Input i)
			=> (i == Input.Zero) ? "z" :
				(i == Input.One) ? "o" :
				(i == Input.OneNonZero) ? "on" :
				(i == Input.Any) ? "" :
				(i == Input.AnyNonZero) ? "n" : "";
		public static bool IsSubtype(this Input self, Input other)
		{
			if (self == other)
				return true;

			if (self == Input.OneNonZero && other == Input.One)
				return true;

			if (self == Input.OneNonZero && other == Input.AnyNonZero)
				return true;

			if (other == Input.Any)
				return true;

			return false;
		}
	}
}
