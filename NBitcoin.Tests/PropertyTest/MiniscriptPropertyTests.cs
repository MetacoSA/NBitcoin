using System;
using System.Threading;
using FsCheck;
using Xunit;
using FsCheck.Xunit;
using NBitcoin.Scripting.Miniscript.Policy;
using NBitcoin.Tests.Generators;

namespace NBitcoin.Tests.PropertyTest
{
	public class MiniscriptPropertyTests
	{
		public MiniscriptPropertyTests()
		{
			Arb.Register<CryptoGenerator>();
			Arb.Register<ConcretePolicyGenerator>();
		}

		[Property(MaxTest = 100)]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldBidirectionallyConvertConcretePolicyToString(ConcretePolicy<PubKey, uint160> p)
		{
			var str = p.ToString();
			var c = ConcretePolicy<PubKey, uint160>.Parse(str);
			Assert.Equal(p, c);
			Assert.Equal(p.GetHashCode(), c.GetHashCode());
		}

		[Property(MaxTest = 100)]
	}
}
