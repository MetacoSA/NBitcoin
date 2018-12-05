using System.IO;
using FsCheck;
using FsCheck.Xunit;
using NBitcoin.BIP174;
using NBitcoin.Tests.Generators;
using NBitcoin;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Tests.PropertyTest
{
	public class PSBTTest
	{

		public PSBTTest()
		{
			Arb.Register<PSBTGenerator>();
			Arb.Register<ChainParamsGenerator>();
			Arb.Register<CryptoGenerator>();
			Arb.Register<PrimitiveGenerator>();
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

			var copy = item.Clone();
			Assert.Equal(item, copy);
		}

		[Property(MaxTest = 15)]
		[Trait("UnitTest", "UnitTest")]
		public void ShouldAddKeyInfo(PSBT psbt, Key key, uint MasterKeyFingerPrint, KeyPath path)
		{
			for (var i = 0; i < psbt.inputs.Count; i++)
			{
				psbt.AddPathTo(i, key.PubKey, MasterKeyFingerPrint, path);
				Assert.Single(psbt.inputs[i].HDKeyPaths);
			}
			for (var i = 0; i < psbt.outputs.Count; i++)
			{
				psbt.AddPathTo(i, key.PubKey, MasterKeyFingerPrint, path, false);
				Assert.Single(psbt.outputs[i].HDKeyPaths);
			}
		}

		[Property(MaxTest = 10)]
		[Trait("UnitTest", "UnitTest")]
		public void CanCloneAndCombine(PSBT psbt)
		{
			var tmp = psbt.Clone();
			Assert.Equal(psbt, tmp, new PSBTComparer());
			// var combined = psbt.Combine(tmp);
			// Assert.Equal(psbt, combined);
		}

		private class PSBTComparer : EqualityComparer<PSBT>
		{
			public override bool Equals(PSBT a, PSBT b) => a.Equals(b);
			public override int GetHashCode(PSBT psbt) => psbt.GetHashCode();
		}

	}
}