using System;
using System.Threading;
using FsCheck;
using NBitcoin.Scripting.Miniscript;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Tests.Generators
{
	public class ConcretePolicyGenerator<TPk, TPKh>
	where TPk : class, IMiniscriptKey<TPKh>, new()
	where TPKh : class, IMiniscriptKeyHash, new()
	{
		public static readonly Arbitrary<ConcretePolicy<TPk, TPKh>> ConcretePolicyArb
			= new ArbitraryConcretePolicy();

		public class ArbitraryConcretePolicy : Arbitrary<ConcretePolicy<TPk, TPKh>>
		{
			public override Gen<ConcretePolicy<TPk, TPKh>> Generator => Gen.Sized(ConcretePolicyGen);

			private static Gen<ConcretePolicy<TPk, TPKh>> ConcretePolicyGen(int size)
			{
				if (size == 0) return NonRecursivePolicyGen();
				return Gen.Frequency(
					Tuple.Create(3, NonRecursivePolicyGen()),
					Tuple.Create(2, RecursivePolicyGen(size))
					);
			}

			private static Gen<ConcretePolicy<TPk, TPKh>> NonRecursivePolicyGen()
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

			private static Gen<ConcretePolicy<TPk, TPKh>> KeyGen()
				=>
					from inner in Arb.Generate<TPk>()
					select ConcretePolicy<TPk, TPKh>.NewKey(inner);

			private static Gen<ConcretePolicy<TPk, TPKh>> AfterGen()
				=>
					from t in Arb.Generate<uint>()
					select ConcretePolicy<TPk, TPKh>.NewAfter(t);
			private static Gen<ConcretePolicy<TPk, TPKh>> OlderGen()
				=>
					from t in Arb.Generate<uint>()
					select ConcretePolicy<TPk, TPKh>.NewOlder(t);

			private static Gen<ConcretePolicy<TPk, TPKh>> Sha256Gen()
				=>
					from t in CryptoGenerator.Hash256()
					select ConcretePolicy<TPk, TPKh>.NewSha256(t);

			private static Gen<ConcretePolicy<TPk, TPKh>> Hash256Gen()
				=>
					from t in CryptoGenerator.Hash256()
					select ConcretePolicy<TPk, TPKh>.NewHash256(t);

			private static Gen<ConcretePolicy<TPk, TPKh>> Ripemd160Gen()
				=>
					from t in CryptoGenerator.Hash160()
					select ConcretePolicy<TPk, TPKh>.NewRipemd160(t);

			private static Gen<ConcretePolicy<TPk, TPKh>> Hash160Gen()
				=>
					from t in CryptoGenerator.Hash160()
					select ConcretePolicy<TPk, TPKh>.NewHash160(t);

			private static Gen<ConcretePolicy<TPk, TPKh>> RecursivePolicyGen(int size)
				=>
					Gen.OneOf(
					(from sub in ConcretePolicyGen(size).Two()
					select ConcretePolicy<TPk, TPKh>.NewAnd(new[] {sub.Item1, sub.Item2})),
					(from prob in Arb.Generate<PositiveInt>().Two()
					from sub in ConcretePolicyGen(size).Two()
					select ConcretePolicy<TPk, TPKh>.NewOr(
						new []{
						Tuple.Create((uint)prob.Item1.Get, sub.Item1),
						Tuple.Create((uint)prob.Item2.Get, sub.Item2)
						})),
					(from t in ThresholdContentsGen(size)
					select ConcretePolicy<TPk, TPKh>.NewThreshold(t.Item1, t.Item2))
					);

			private static Gen<Tuple<uint, ConcretePolicy<TPk, TPKh>[]>> ThresholdContentsGen(int size)
				=>
					from n in Gen.Choose(1, 6)
					from actualN in n == 1 ? Gen.Choose(2, 6) : Gen.Choose(n, 6)
					from a in Gen.ArrayOf(actualN, ConcretePolicyGen(size))
					select Tuple.Create((uint) n, a);
		}
	}

}
