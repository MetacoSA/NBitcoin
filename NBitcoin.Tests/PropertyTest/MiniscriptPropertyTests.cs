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

		[Property(MaxTest = 12)]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldBidirectionallyConvertConcretePolicyToString(ConcretePolicy<PubKey, uint160> p)
		{
			var str = p.ToString();
			var c = ConcretePolicy<PubKey, uint160>.Parse(str);
			Assert.Equal(p, c);
			Assert.Equal(str, c.ToString());
			Assert.Equal(p.GetHashCode(), c.GetHashCode());
		}

		[Property(MaxTest = 100, Skip="For now")]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldCompileConcretePolicy(ConcretePolicy<PubKey, uint160> p)
		{
			var t = p.IsSafeNonMalleable();
			var isSafe = t.Item1;
			var IsNonMalleable = t.Item2;
			if (isSafe && IsNonMalleable)
			{
				Console.WriteLine($"Compiling {p}");
				var ms = p.Compile();
				Console.WriteLine($"Compiled {ms}");
			}
			else
			{
				Assert.Throws<CompilerException>(() => p.Compile());
			}
		}
	}
}
