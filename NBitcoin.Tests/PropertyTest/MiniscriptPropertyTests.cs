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

		[Property(MaxTest = 10)]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldBidirectionallyConvertConcretePolicy(ConcretePolicy<PubKey, uint160> p)
		{
			Console.WriteLine("start test");
			var str = p.ToString();
			Console.WriteLine($"Going to parse {str}");
			var c = ConcretePolicy<PubKey, uint160>.Parse(str);
			Assert.Equal(p, c);
		}
	}
}
