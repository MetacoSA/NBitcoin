using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript
{
	public interface ILiftable<TPk, TPKh>
		where TPk : IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
	{
		AbstractPolicy<TPk, TPKh> Lift();
	}

	public partial class Miniscript<TPk, TPKh> : ILiftable<TPk, TPKh>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		public AbstractPolicy<TPk, TPKh> Lift() =>
			this.Node.Lift();
	}

	public partial class Terminal<TPk, TPKh>
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		public AbstractPolicy<TPk, TPKh> Lift()
		{
			switch (this.Tag)
			{
				case Tags.True: return AbstractPolicy<TPk, TPKh>.Trivial;
				case Tags.False: return AbstractPolicy<TPk, TPKh>.UnSatisfiable;
			}

			switch (this)
			{
				case Pk self: return AbstractPolicy<TPk, TPKh>.NewKeyHash(self.Item.ToPubKeyHash()).Normalize();
				case PkH self: return AbstractPolicy<TPk, TPKh>.NewKeyHash(self.Item).Normalize();
				case After self: return AbstractPolicy<TPk, TPKh>.NewAfter(self.Item).Normalize();
				case Older self: return AbstractPolicy<TPk, TPKh>.NewOlder(self.Item).Normalize();
				case Sha256 self: return AbstractPolicy<TPk, TPKh>.NewSha256(self.Item).Normalize();
				case Hash256 self: return AbstractPolicy<TPk, TPKh>.NewHash256(self.Item).Normalize();
				case Ripemd160 self: return AbstractPolicy<TPk, TPKh>.NewRipemd160(self.Item).Normalize();
				case Hash160 self: return AbstractPolicy<TPk, TPKh>.NewHash160(self.Item).Normalize();
				case Alt self: return self.Item.Node.Lift().Normalize();
				case Check self: return self.Item.Node.Lift().Normalize();
				case DupIf self: return self.Item.Node.Lift().Normalize();
				case Verify self: return self.Item.Node.Lift().Normalize();
				case NonZero self: return self.Item.Node.Lift().Normalize();
				case ZeroNotEqual self: return self.Item.Node.Lift().Normalize();
				case AndV self:
					return AbstractPolicy<TPk, TPKh>.NewAnd(new List<AbstractPolicy<TPk, TPKh>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift()}).Normalize();
				case AndB self:
					return AbstractPolicy<TPk, TPKh>.NewAnd(new List<AbstractPolicy<TPk, TPKh>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift()}).Normalize();
				case AndOr self:
					var inner = AbstractPolicy<TPk, TPKh>.NewAnd(new List<AbstractPolicy<TPk, TPKh>>() {self.Item1.Node.Lift(), self.Item3.Node.Lift()}).Normalize();
					return AbstractPolicy<TPk, TPKh>.NewOr(new List<AbstractPolicy<TPk, TPKh>>() {inner, self.Item2.Node.Lift()}).Normalize();
				case OrB self:
					return AbstractPolicy<TPk, TPKh>.NewOr(new List<AbstractPolicy<TPk, TPKh>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() }).Normalize();
				case OrD self:
					return AbstractPolicy<TPk, TPKh>.NewOr(new List<AbstractPolicy<TPk, TPKh>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() }).Normalize();
				case OrC self:
					return AbstractPolicy<TPk, TPKh>.NewOr(new List<AbstractPolicy<TPk, TPKh>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() }).Normalize();
				case OrI self:
					return AbstractPolicy<TPk, TPKh>.NewOr(new List<AbstractPolicy<TPk, TPKh>>() { self.Item1.Node.Lift(), self.Item2.Node.Lift() }).Normalize();
				case Thresh self:
					return AbstractPolicy<TPk, TPKh>.NewThreshold(self.Item1, self.Item2.Select(sub => sub.Node.Lift()).ToList()).Normalize();
				case ThreshM self:
					return
						AbstractPolicy<TPk, TPKh>.NewThreshold(
							self.Item1,
							self.Item2.Select(sub => AbstractPolicy<TPk, TPKh>.NewKeyHash(sub.ToPubKeyHash())).ToList()
						).Normalize();
			}

			throw new Exception("Unreachable!");
		}
	}
}
