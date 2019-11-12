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
			Assert.Equal(str, c.ToString());
			Assert.Equal(p.GetHashCode(), c.GetHashCode());
		}

		[Property(MaxTest = 100)]
		public void ShouldCompileConcretePolicy(ConcretePolicy<PubKey, uint160> p)
		{
			Console.WriteLine(p);
			var t = p.IsSafeNonMalleable();
			var isSafe = t.Item1;
			var IsNonMalleable = t.Item2;
			if (isSafe && IsNonMalleable)
			{
				var ms = p.Compile();
			}
			else
			{
				Assert.Throws<CompilerException>(() => p.Compile());
			}
		}
	}
}
