using System.IO;
using FsCheck;
using FsCheck.Xunit;
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

		[Property(MaxTest = 5)]
		[Trait("UnitTest", "UnitTest")]
		public void CanCloneAndCombine(PSBT psbt)
		{
			var tmp = psbt.Clone();
			Assert.Equal(psbt, tmp, ComparerInstance);
			var combined = psbt.Combine(tmp);
			Assert.Equal(psbt, combined, ComparerInstance);
		}

		[Property(MaxTest = 5)]
		[Trait("UnitTest", "UnitTest")]
		public void CanCoinJoin(PSBT a, PSBT b)
		{
			var result = a.CoinJoin(b);
			Assert.Equal(result.Inputs.Count, a.Inputs.Count + b.Inputs.Count);
			Assert.Equal(result.Outputs.Count, a.Outputs.Count + b.Outputs.Count);
			var tx = result.GetGlobalTransaction();
			var atx = a.GetGlobalTransaction();
			var btx = b.GetGlobalTransaction();
			Assert.Equal(tx.Inputs.Count, atx.Inputs.Count + btx.Inputs.Count);
			Assert.Equal(tx.Outputs.Count, atx.Outputs.Count + btx.Outputs.Count);
			// These will work in netcoreapp2.1, but not in net472 ... :(
			// Assert.Subset<PSBTInput>(result.inputs.ToHashSet(), a.inputs.ToHashSet());
			// Assert.Subset<PSBTInput>(result.inputs.ToHashSet(), b.inputs.ToHashSet());
			// Assert.Subset<PSBTOutput>(result.outputs.ToHashSet(), a.outputs.ToHashSet());
			// Assert.Subset<PSBTOutput>(result.outputs.ToHashSet(), b.outputs.ToHashSet());
			// Assert.Subset<TxIn>(result.tx.Inputs.ToHashSet(), b.tx.Inputs.ToHashSet());
			// Assert.Subset<TxOut>(result.tx.Outputs.ToHashSet(), b.tx.Outputs.ToHashSet());
		}
	}
}
