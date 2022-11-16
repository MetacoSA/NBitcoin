using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using NBitcoin.Crypto;
using NBitcoin.Tests.Generators;
using Xunit;

namespace NBitcoin.Tests.PropertyTest
{
#if NET6_0_OR_GREATER
	public class TaprootTests
	{
	public TaprootTests()
		{
			Arb.Register<CryptoGenerator>();
		}

		[Property(MaxTest = 100)]
		[Trait("UnitTest", "UnitTest")]
		public void SerializeDeserializeControlBlock(ControlBlock ctrl)
		{
			if ((ctrl.LeafVersion & 1) == 1)
				return;
			if (ctrl.LeafVersion == 0x50) // annex
				return;
			Assert.Equal(ctrl, ControlBlock.FromSlice(ctrl.ToBytes()));
		}
	}
#endif
}
