using System;
using NBitcoin.Miniscript;
using NBitcoin;
using Xunit;
namespace NBitcoin.Tests
{
	public class MiniscriptPSBTTests
	{
		public MiniscriptPSBTTests()
		{
		}

        private TransactionSignature DummyKeyFn(PubKey key) => null;
        public void ShouldSatisfyMiniscript()
		{
            var key = new NBitcoin.Key();
            var scriptStr = $"and(pk({key.PubKey}), time({new LockTime(10000)}))";
            var ms = NBitcoin.Miniscript.Miniscript.parseUnsafe(scriptStr);
            Assert.NotNull(ms);

            var r1 = ms.Satisfy(DummyKeyFn, null, null);
        }
	}
}
