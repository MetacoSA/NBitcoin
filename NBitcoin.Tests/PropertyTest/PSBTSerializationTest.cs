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
using static NBitcoin.Tests.Comparer;

namespace NBitcoin.Tests.PropertyTest
{
	public class PSBTTest
	{
		private PSBTComparer ComparerInstance { get; }

		public PSBTTest()
		{
			Arb.Register<PSBTGenerator>();
			Arb.Register<ChainParamsGenerator>();
			Arb.Register<CryptoGenerator>();
			Arb.Register<PrimitiveGenerator>();

			ComparerInstance = new PSBTComparer();
		}

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
			for (var i = 0; i < psbt.Inputs.Count; i++)
			{
				psbt.AddPathTo(i, key.PubKey, MasterKeyFingerPrint, path);
				Assert.Single(psbt.Inputs[i].HDKeyPaths);
			}
			for (var i = 0; i < psbt.Outputs.Count; i++)
			{
				psbt.AddPathTo(i, key.PubKey, MasterKeyFingerPrint, path, false);
				Assert.Single(psbt.Outputs[i].HDKeyPaths);
			}
		}

		[Property(MaxTest = 10)]
		[Trait("UnitTest", "UnitTest")]
		public void CanCloneAndCombine(PSBT psbt)
		{
			var tmp = psbt.Clone();
			Assert.Equal(psbt, tmp, ComparerInstance);
			var combined = psbt.Combine(tmp);
			Assert.Equal(psbt, combined, ComparerInstance);
		}

		[Property(MaxTest = 10)]
		public void CanCoinJoin(PSBT a, PSBT b)
		{
			var result = a.CoinJoin(b);
			Assert.Equal(result.Inputs.Count, a.Inputs.Count + b.Inputs.Count);
			Assert.Equal(result.Outputs.Count, a.Outputs.Count + b.Outputs.Count);
			Assert.Equal(result.tx.Inputs.Count, a.tx.Inputs.Count + b.tx.Inputs.Count);
			Assert.Equal(result.tx.Outputs.Count, a.tx.Outputs.Count + b.tx.Outputs.Count);
			// These will work in netcoreapp2.1, but not in net461 ... :(
			// Assert.Subset<PSBTInput>(result.inputs.ToHashSet(), a.inputs.ToHashSet());
			// Assert.Subset<PSBTInput>(result.inputs.ToHashSet(), b.inputs.ToHashSet());
			// Assert.Subset<PSBTOutput>(result.outputs.ToHashSet(), a.outputs.ToHashSet());
			// Assert.Subset<PSBTOutput>(result.outputs.ToHashSet(), b.outputs.ToHashSet());
			// Assert.Subset<TxIn>(result.tx.Inputs.ToHashSet(), b.tx.Inputs.ToHashSet());
			// Assert.Subset<TxOut>(result.tx.Outputs.ToHashSet(), b.tx.Outputs.ToHashSet());
		}
	}
}