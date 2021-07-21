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
		[Trait("PropertyTest", "PropertyTest")]
		public void ShouldNotChangeByCloneOrConstructor(ExtKey key)
		{
			Assert.Equal(ExtKey.CreateFromBytes(key.ToBytes()), new ExtKey(key.Neuter(), key.PrivateKey));
		}

		[Property]
		[Trait("PropertyTest", "PropertyTest")]
		public void ShouldRecoverNeuteredParentKeyIfNotHardened(ExtKey key, uint index)
		{
			var child = key.Derive(index);
			Assert.Equal(child.Child, index);
			if (!child.IsHardened)
				Assert.Equal(key, child.GetParentExtKey(key.Neuter()));
		}
	}
}
