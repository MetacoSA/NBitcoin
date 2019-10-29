namespace NBitcoin.Scripting.Miniscript
{
	/// <summary>
	/// Basic type representing where the fragment can go
	/// </summary>
	public enum Base
	{
		/// <summary>
		/// Takes its inputs from the top of the stack.
		/// Pushes nonzero if the condition is statisfied. If not, if it
		/// does not abort, then 0 is pushed.
		/// </summary>
		B,
		/// <summary>
		/// Takes its inputs from the top of the stack. Pushes a
		/// public key, regardless of satisfaction, onto the stack.
		/// Must be wrapped in `c:` to turn into any other type.
		/// </summary>
		K,
		/// <summary>
		/// Takes its inputs from the top of the stack, which
		/// must satisfy the condition (will abort otherwise).
		/// Does not push anything onto the stack.
		/// </summary>
		V,
		/// <summary>
		/// Takes from the stack its inputs + element X at the top.
		/// If the inputs satisfy the condition, [nonzero X] or
		/// [X nonzero] is pushed. If not, if it does not abort,
		/// then [0 X] or [X 0] is pushed.
		/// </summary>
		W
	}
}