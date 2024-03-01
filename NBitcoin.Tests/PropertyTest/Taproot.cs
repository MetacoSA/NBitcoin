using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using NBitcoin.Crypto;
using NBitcoin.Tests.Generators;
using Xunit;

namespace NBitcoin.Tests.PropertyTest
{
#if HAS_SPAN
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
			if (((byte)ctrl.LeafVersion & 1) == 1)
				return;
			if ((byte)ctrl.LeafVersion == TaprootConstants.TAPROOT_LEAF_ANNEX)
				return;
			Assert.Equal(ctrl, ControlBlock.FromSlice(ctrl.ToBytes()));
		}
	}
#endif
}
