using System.IO;
using FsCheck;
using FsCheck.Xunit;
using NBitcoin.BIP174;
using NBitcoin.Tests.Generators;
using NBitcoin;
using Xunit;

namespace NBitcoin.Tests.PropertyTest
{
	public class PSBTTest
	{

		public PSBTTest()
		{
			Arb.Register<PSBTGenerator>();
			Arb.Register<StandardTransactionGenerator>();
		}

		[Property]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeInputAsymmetric(PSBTInput psbtin) => SerializeAsymmetric(psbtin);

		[Property]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeOutputAsymmetric(PSBTOutput psbtout) => SerializeAsymmetric(psbtout);

		private void SerializeAsymmetric<T>(T item) where T : IBitcoinSerializable, new()
		{
			var ms = new MemoryStream();
			var stream = new BitcoinStream(ms, true);
			item.ReadWrite(stream);
			var stream2 = new BitcoinStream(ms, false);
			ms.Position = 0;
			var item2 = new T();
			item2.ReadWrite(stream2);
			Assert.Equal(item, item2);
		}

		[Property]
		[Trait("UnitTest", "UnitTest")]
		public void PSBTInputShouldInstantiateFromTxIn(TxIn txin)
		{
			var psbtin = PSBTInput.FromTxIn(txin);
		}
	}
}