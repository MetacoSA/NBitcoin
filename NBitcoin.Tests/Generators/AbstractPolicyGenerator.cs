using System;
using System.Linq;
using FsCheck;
using NBitcoin.Miniscript;

namespace NBitcoin.Tests.Generators
{
	public class AbstractPolicyGenerator
	{

		/// <summary>
		///  Ideally This should be able to specify the size from the callers side. But who cares.
		/// </summary>
		/// <returns></returns>
		public static Arbitrary<AbstractPolicy> AbstractPolicyArb()
			=> Arb.From(AbstractPolicyGen(32));

		private static Gen<AbstractPolicy> AbstractPolicyGen(int size)
		{
				if (size == 0)
			{
				return NonRecursivePolicyGen();
			}
			else
			{
				return Gen.Frequency
					(
						Tuple.Create(3, NonRecursivePolicyGen()),
						Tuple.Create(2, RecursivePolicyGen(AbstractPolicyGen(size / 2)))
					);
			}
		}

		private static Gen<AbstractPolicy> NonRecursivePolicyGen()
			=>
				Gen.OneOf(
					new[]{
						CheckSigGen(),
						MultiSigGen(),
						TimeGen(),
						HashGen()
					}
				);
		private static Gen<AbstractPolicy> CheckSigGen()
			=> CryptoGenerator.PublicKey().Select(pk => AbstractPolicy.NewCheckSig(pk));

		private static Gen<AbstractPolicy> MultiSigGen()
			=>
				from m in Gen.Choose(1, 16)
				from numKeys in Gen.Choose(m, 16)
				from pks in Gen.ArrayOf(numKeys, CryptoGenerator.PublicKey())
				select AbstractPolicy.NewMulti((uint)m, pks);

		private static Gen<AbstractPolicy> TimeGen()
			=>
				from t in Gen.Choose(0, 65535)
				select AbstractPolicy.NewTime((uint)t);

		private static Gen<AbstractPolicy> HashGen()
			=>
				from t in CryptoGenerator.Hash256()
				select AbstractPolicy.NewHash(t);

		private static Gen<AbstractPolicy> RecursivePolicyGen(Gen<AbstractPolicy> subGen)
			=> Gen.OneOf
			(
				subGen.Two().Select(t => AbstractPolicy.NewAnd(t.Item1, t.Item2)),
				subGen.Two().Select(t => AbstractPolicy.NewOr(t.Item1, t.Item2)),
				subGen.Two().Select(t => AbstractPolicy.NewAsymmetricOr(t.Item1, t.Item2)),
				ThresholdContentsGen(subGen).Select(t => AbstractPolicy.NewThreshold(t.Item1, t.Item2))
			);

		private static Gen<Tuple<UInt32, AbstractPolicy[]>> ThresholdContentsGen(Gen<AbstractPolicy> subGen)
			=>
				from num in Gen.Choose(1, 6)
				from actualNum in Gen.Choose(num, 6)
				from subPolicies in Gen.ArrayOf(actualNum, subGen)
				select Tuple.Create((uint)num, subPolicies);
	}
}