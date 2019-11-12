using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using FsCheck;
using NBitcoin.Scripting.Miniscript;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Tests.Generators
{
	public class ConcretePolicyGenerator
	{
		public static Arbitrary<ConcretePolicy<PubKey, uint160>> PubKeyConcretePolicyArb()
			=> new ArbitraryPubKeyConcretePolicy();

		public class ArbitraryPubKeyConcretePolicy : Arbitrary<ConcretePolicy<PubKey, uint160>>
		{
			public override Gen<ConcretePolicy<PubKey, uint160>> Generator => Gen.Sized(ConcretePolicyGen);

			public override IEnumerable<ConcretePolicy<PubKey, uint160>> Shrinker(ConcretePolicy<PubKey, uint160> parent)
			{
				switch (parent)
				{
					case ConcretePolicy<PubKey, uint160>.And p:
						foreach (var i in p.Item)
							yield return i;
						foreach (var i in Arb.Shrink(p.Item))
							if (i.Count == 2)
								yield return ConcretePolicy<PubKey, uint160>.NewAnd(i);
						break;
					case ConcretePolicy<PubKey, uint160>.Or p:
						foreach (var i in p.Item)
							yield return i.Item2;
						foreach (var i in Arb.Shrink(p.Item))
							if (i.Count == 2)
								yield return ConcretePolicy<PubKey, uint160>.NewOr(i);
						break;
					case ConcretePolicy<PubKey, uint160>.Threshold p:
						foreach (var subP in p.Item2)
						{
							yield return subP;
						}
						foreach (var i in Arb.Shrink(p.Item2).Select(s => s.Select(sub => Shrinker(sub))))
						{
							foreach (var i2 in i)
								if (1 < i2.Count())
									yield return ConcretePolicy<PubKey, uint160>.NewThreshold(1, i2);
						}

						foreach (var subP in Arb.Shrink(p.Item2))
						{
							if (1 < subP.Count())
								yield return ConcretePolicy<PubKey, uint160>.NewThreshold(1, subP);
						}
						yield break;
				}
			}

			private static Gen<ConcretePolicy<PubKey, uint160>> ConcretePolicyGen(int size)
			{
				if (size == 0) return NonRecursivePolicyGen();
				return Gen.Frequency(
					Tuple.Create(3, NonRecursivePolicyGen()),
					Tuple.Create(2, RecursivePolicyGen(size / 2))
					);
			}

			private static Gen<ConcretePolicy<PubKey, uint160>> NonRecursivePolicyGen()
				=> Gen.OneOf(new[]
				{
					KeyGen(),
					AfterGen(),
					OlderGen(),
					Sha256Gen(),
					Hash256Gen(),
					Ripemd160Gen(),
					Hash160Gen()
				});

			private static Gen<ConcretePolicy<PubKey, uint160>> KeyGen()
				=>
					from inner in CryptoGenerator.PublicKey()
					select ConcretePolicy<PubKey, uint160>.NewKey(inner);

			private static Gen<ConcretePolicy<PubKey, uint160>> AfterGen()
				=>
					from t in Arb.Generate<uint>()
					select ConcretePolicy<PubKey, uint160>.NewAfter(t);
			private static Gen<ConcretePolicy<PubKey, uint160>> OlderGen()
				=>
					from t in Arb.Generate<uint>()
					select ConcretePolicy<PubKey, uint160>.NewOlder(t);

			private static Gen<ConcretePolicy<PubKey, uint160>> Sha256Gen()
				=>
					from t in CryptoGenerator.Hash256()
					select ConcretePolicy<PubKey, uint160>.NewSha256(t);

			private static Gen<ConcretePolicy<PubKey, uint160>> Hash256Gen()
				=>
					from t in CryptoGenerator.Hash256()
					select ConcretePolicy<PubKey, uint160>.NewHash256(t);

			private static Gen<ConcretePolicy<PubKey, uint160>> Ripemd160Gen()
				=>
					from t in CryptoGenerator.Hash160()
					select ConcretePolicy<PubKey, uint160>.NewRipemd160(t);

			private static Gen<ConcretePolicy<PubKey, uint160>> Hash160Gen()
				=>
					from t in CryptoGenerator.Hash160()
					select ConcretePolicy<PubKey, uint160>.NewHash160(t);

			private static Gen<ConcretePolicy<PubKey, uint160>> RecursivePolicyGen(int size)
				=>
					Gen.OneOf(
					(from sub in ConcretePolicyGen(size).Two()
					select ConcretePolicy<PubKey, uint160>.NewAnd(new[] {sub.Item1, sub.Item2})),
					(from prob in Arb.Generate<PositiveInt>().Two()
					from sub in ConcretePolicyGen(size).Two()
					select ConcretePolicy<PubKey, uint160>.NewOr(
						new []{
						Tuple.Create((uint)prob.Item1.Get, sub.Item1),
						Tuple.Create((uint)prob.Item2.Get, sub.Item2)
						})),
					(from t in ThresholdContentsGen(size)
					select ConcretePolicy<PubKey, uint160>.NewThreshold(t.Item1, t.Item2))
					);

			private static Gen<Tuple<uint, ConcretePolicy<PubKey, uint160>[]>> ThresholdContentsGen(int size)
				=>
					from n in Gen.Choose(1, 6)
					from actualN in n == 1 ? Gen.Choose(2, 6) : Gen.Choose(n, 6)
					from a in Gen.ArrayOf(actualN, ConcretePolicyGen(size))
					select Tuple.Create((uint) n, a);
		}
	}

}
