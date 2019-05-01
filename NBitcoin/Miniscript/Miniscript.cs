namespace NBitcoin.Miniscript
{
	/// <summary>
	/// Facade for handling Miniscript related things.
	/// </summary>
	public class Miniscript
	{

		public AbstractPolicy Policy { get; }

		private AstElem ast;
		public AstElem Ast { get {
				if (ast == null)
				{
					this.ast = CompiledNode.FromPolicy(Policy).BestT(0.0, 0.0).Ast;
				}
				return ast;
			} }

		private Script script;
		public Script Script { get {
				if (script == null)
				{
					this.script = Ast.ToScript();
				}
				return this.script;
			} }

		public Miniscript(AbstractPolicy policy)
		{
			if (policy == null)
				throw new System.ArgumentNullException(nameof(policy));
			Policy = policy;
		}

		public override string ToString()
		{
			return this.Policy.ToString();
		}
		public static Miniscript FromPolicy(AbstractPolicy policy)
			=> new Miniscript(policy);
	}
}