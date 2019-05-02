using System;
using NBitcoin.Miniscript.Parser;

namespace NBitcoin.Miniscript
{
	/// <summary>
	/// Facade for handling Miniscript related things.
	/// </summary>
	public class Miniscript : IEquatable<Miniscript>
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

		public uint MaxSatisfactionSize(uint costForOne)
		{
			return this.Ast.MaxSatisfactionSize(costForOne);
		}

		public int ScriptSize()
			=> this.Script.Length;

		public Miniscript(AstElem astElem)
		{
			if (astElem == null)
				throw new System.ArgumentNullException(nameof(astElem));
			Policy = astElem.ToPolicy();
			ast = astElem;
		}

		public override string ToString()
		{
			return this.Policy.ToString();
		}
		public static Miniscript FromPolicy(AbstractPolicy policy)
			=> new Miniscript(policy);

		public static Miniscript FromAstElem(AstElem ast)
			=> new Miniscript(ast);

		public static Miniscript Parse(string dsl)
			=> Miniscript.FromPolicy(MiniscriptDSLParser.DSLParser.Parse(dsl));

		public static bool TryParse(string dsl, out Miniscript result)
		{
			result = null;
			var res = MiniscriptDSLParser.DSLParser.TryParse(dsl);
			if (!res.IsSuccess)
				return false;
			result = Miniscript.FromPolicy(res.Value);
			return true;
		}
		public static Miniscript ParseScript(Script sc)
			=> Miniscript.FromAstElem(MiniscriptScriptParser.ParseScript(sc));

		public static bool TryParseScript(Script sc, out Miniscript result)
		{
			result = null;
			var res = MiniscriptScriptParser.PAstElem.TryParse(sc);
			if (!res.IsSuccess)
				return false;
			result = Miniscript.FromAstElem(res.Value);
			return true;
		}

		public sealed override bool Equals(object obj)
		{
			Miniscript other = obj as Miniscript;
			if (other != null)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(Miniscript other)
			=> this.Policy.Equals(other.Policy);

		public override int GetHashCode()
			=> this.Policy.GetHashCode();
	}
}