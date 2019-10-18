namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// A token of the from `x()` or `x`
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Tree<T>
	{
		public readonly string Name;
		public readonly Tree<T>[] Args;

		public static Tree<T> Parse(string str)
		{
			throw new System.Exception("");
		}
	}
}
