using FsCheck;
using FsCheck.Xunit;
using NBitcoin.Tests.Generators;
using Xunit;

namespace NBitcoin.Tests.PropertyTest
{
	public class ExtKeyTest
	{
		public ExtKeyTest()
		{
			Arb.Register<CryptoGenerator>();
			Arb.Register<ChainParamsGenerator>();
		}

		[Property]
		[Trait("PropertyTest", "Immutability")]
		public void ShouldNotChangeByCloneOrConstructor(ExtKey key)
		{
			Assert.Equal(key.Clone(), new ExtKey(key.Neuter(), key.PrivateKey));
		}

		[Property]
		[Trait("PropertyTest", "BidirectionalConversion")]
		public void ShouldRecoverNeuteredParentKeyIfNotHardened(ExtKey key, uint index)
		{
			var child = key.Derive((int) index, false);
			Assert.Equal(child.Child, index);

			Assert.Equal(key, child.GetParentExtKey(key.Neuter()));
		}
	}
}