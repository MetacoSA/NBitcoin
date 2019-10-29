using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript
{
	public interface ILiftable<TPk> where TPk : IMiniscriptKey
	{
		AbstractPolicy<TPk> Lift();
	}

	public partial class Miniscript<TPk> : ILiftable<TPk> where TPk : IMiniscriptKey
	{
		public AbstractPolicy<TPk> Lift() =>
			this.Node.Lift();
	}

	public partial class Terminal<TPk> : ILiftable<TPk> where TPk : IMiniscriptKey
	{
		public AbstractPolicy<TPk> Lift()
		{
			switch (this.Tag)
			{
				case Tags.True: return AbstractPolicy<TPk>.Trivial;
				case Tags.False: return AbstractPolicy<TPk>.UnSatisfiable;
			}

			switch (this)
			{
				case Pk self: return AbstractPolicy<TPk>.NewKeyHash(self.Item.MiniscriptKeyHash);
				case PkH self: return AbstractPolicy<TPk>.NewKeyHash(self.Item);
				case After self: return AbstractPolicy<TPk>.NewAfter(self.Item);
				case Older self: return AbstractPolicy<TPk>.NewOlder(self.Item);
				case Sha256 self: return AbstractPolicy<TPk>.NewSha256(self.Item);
				case Hash256 self: return AbstractPolicy<TPk>.NewHash256(self.Item);
				case Ripemd160 self: return AbstractPolicy<TPk>.NewRipemd160(self.Item);
				case Hash160 self: return AbstractPolicy<TPk>.NewHash160(self.Item);
				case Alt self: return self.Item.Node.Lift();
				case Check self: return self.Item.Node.Lift();
				case DupIf self: return self.Item.Node.Lift();
				case Verify self: return self.Item.Node.Lift();
				case NonZero self: return self.Item.Node.Lift();
				case ZeroNotEqual self: return self.Item.Node.Lift();
				case AndV self:
					return AbstractPolicy<TPk>.NewAnd(new List<AbstractPolicy<TPk>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift()});
				case AndB self:
					return AbstractPolicy<TPk>.NewAnd(new List<AbstractPolicy<TPk>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift()});
				case AndOr self:
					var inner = AbstractPolicy<TPk>.NewAnd(new List<AbstractPolicy<TPk>>() {self.Item1.Node.Lift(), self.Item3.Node.Lift()});
					return AbstractPolicy<TPk>.NewOr(new List<AbstractPolicy<TPk>>() {inner, self.Item2.Node.Lift()});
				case OrB self:
					return AbstractPolicy<TPk>.NewOr(new List<AbstractPolicy<TPk>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() });
				case OrD self:
					return AbstractPolicy<TPk>.NewOr(new List<AbstractPolicy<TPk>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() });
				case OrC self:
					return AbstractPolicy<TPk>.NewOr(new List<AbstractPolicy<TPk>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() });
				case OrI self:
					return AbstractPolicy<TPk>.NewOr(new List<AbstractPolicy<TPk>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() });
				case Thresh self:
					return AbstractPolicy<TPk>.NewThreshold(self.Item1, self.Item2.Select(sub => sub.Node.Lift()).ToList());
				case ThreshM self:
					return
						AbstractPolicy<TPk>.NewThreshold(
							self.Item1,
							self.Item2.Select(sub => AbstractPolicy<TPk>.NewKeyHash(sub.Item.MiniscriptKeyHash)).ToList()
						);
			}

			throw new Exception("Unreachable!");
		}
	}
}
