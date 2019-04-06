using System;
using System.Collections.Generic;
using Xunit;

namespace NBitcoin.Miniscript.Tests.CSharp
{
	public class MiniscriptPSBTTests
	{
		private Key privKey { get; set; }
		public MiniscriptPSBTTests()
		{
			privkey = new NBitcoin.Key();
		}

		[Fact]
        public void ShouldSatisfyMiniscript()
		{
            var scriptStr = $"and(pk({privKey.PubKey}), time({10000}))";
			var ms = Miniscript.parseUnsafe(scriptStr);
			Assert.NotNull(ms);

			var sigDict = new Dictionary<PubKey, TransactionSignature>();
			var r1 = ms.Satisfy(sigDict);


			Assert.
		}
	}
}
