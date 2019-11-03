namespace NBitcoin.Scripting.Miniscript
{
	public static partial class InputExtensions
	{
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