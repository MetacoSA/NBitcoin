using NBitcoin.BIP174;
using Xunit;

namespace NBitcoin.Tests
{
	public class psbt_wallet_tests
	{

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public static void psbt_updater_test()
		{
			var t = PartiallySignedTransaction.Parse("70736274ff01009a020000000258e87a21b56daf0c23be8e7070456c336f7cbaa5c8757924f545887bb2abdd750000000000ffffffff838d0427d0ec650a68aa46bb0b098aea4422c071b2ca78352a077959d07cea1d0100000000ffffffff0270aaf00800000000160014d85c2b71d0060b09c9886aeb815e50991dda124d00e1f5050000000016001400aea9a2e5f0f876a588df5546e8742d1d87008f000000000000000000");
			
		}
	}
}
