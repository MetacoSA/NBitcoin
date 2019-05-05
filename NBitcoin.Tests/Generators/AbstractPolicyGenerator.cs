using System;
using System.Collections.Generic;
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
			=> new ArbitraryAbstractPolicy();

		public class ArbitraryAbstractPolicy : Arbitrary<AbstractPolicy>
		{
			public override Gen<AbstractPolicy> Generator { get { return Gen.Sized(s => AbstractPolicyGen(s)); } }
			public override IEnumerable<AbstractPolicy> Shrinker(AbstractPolicy parent)
			{
				switch (parent)
				{
					case AbstractPolicy.And p:
						{
							yield return p.Item1;
							yield return p.Item2;
							foreach (var t in Shrinker(p.Item1).SelectMany(shrinkedItem1 => Shrinker(p.Item2), (i1, i2) => Tuple.Create(i1, i2)))
								yield return AbstractPolicy.NewAnd(t.Item1, t.Item2);
							foreach (var subShrinked in Shrinker(p.Item1).Select(shrinkedItem1 => AbstractPolicy.NewAnd(shrinkedItem1, p.Item2)))
								yield return subShrinked;
							foreach (var subShrinked in Shrinker(p.Item2).Select(shrinkedItem2 => AbstractPolicy.NewAnd(p.Item1, shrinkedItem2)))
								yield return subShrinked;
							break;
						}
					case AbstractPolicy.Or p:
						{
							yield return p.Item1;
							yield return p.Item2;
							foreach (var t in Shrinker(p.Item1).SelectMany(shrinkedItem1 => Shrinker(p.Item2), (i1, i2) => Tuple.Create(i1, i2)))
								yield return AbstractPolicy.NewOr(t.Item1, t.Item2);
							foreach (var subShrinked in Shrinker(p.Item1).Select(shrinkedItem1 => AbstractPolicy.NewOr(shrinkedItem1, p.Item2)))
								yield return subShrinked;
							foreach (var subShrinked in Shrinker(p.Item2).Select(shrinkedItem2 => AbstractPolicy.NewOr(p.Item1, shrinkedItem2)))
								yield return subShrinked;
							break;
						}
					case AbstractPolicy.AsymmetricOr p:
						{
							yield return p.Item1;
							yield return p.Item2;
							foreach (var t in Shrinker(p.Item1).SelectMany(shrinkedItem1 => Shrinker(p.Item2), (i1, i2) => Tuple.Create(i1, i2)))
								yield return AbstractPolicy.NewAsymmetricOr(t.Item1, t.Item2);
							foreach (var subShrinked in Shrinker(p.Item1).Select(shrinkedItem1 => AbstractPolicy.NewAsymmetricOr(shrinkedItem1, p.Item2)))
								yield return subShrinked;
							foreach (var subShrinked in Shrinker(p.Item2).Select(shrinkedItem2 => AbstractPolicy.NewAsymmetricOr(p.Item1, shrinkedItem2)))
								yield return subShrinked;
							break;
						}
					case AbstractPolicy.Threshold p:
						{
							foreach (var subP in p.Item2)
							{
								yield return subP;
							}
							foreach (var i in Arb.Shrink(p.Item2).Select(subs => subs.Select(sub => Shrinker(sub))))
							{
								foreach (var i2 in i)
								{
									if (1 < i2.Count())
										yield return AbstractPolicy.NewThreshold(1, i2.ToArray());
								}
							}

							if (p.Item2.Length == 2)
								yield break;

							foreach (var i in Arb.Shrink(p.Item2))
							{
								if (1 < i.Length)
									yield return AbstractPolicy.NewThreshold(1, i);
							}
							yield break;
						}
					case AbstractPolicy.Multi p:
						{
							yield return AbstractPolicy.NewCheckSig(p.Item2[0]);
							if (p.Item2.Length > 2)
								yield return AbstractPolicy.NewMulti(2, p.Item2.Take(2).ToArray());
							foreach (var i in Arb.Shrink(p.Item2))
							{
								if (i.Length > 2)
									yield return AbstractPolicy.NewMulti(2, i.ToArray());
							}
							break;
						}
					default:
						{
							yield break;
						}
				}
			}

		}

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
				from m in Gen.Choose(2, 16)
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
				from actualNum in num == 1 ? Gen.Choose(2, 6) : Gen.Choose(num ,6)
				from subPolicies in Gen.ArrayOf(actualNum, subGen)
				select Tuple.Create((uint)num, subPolicies);
	}
}