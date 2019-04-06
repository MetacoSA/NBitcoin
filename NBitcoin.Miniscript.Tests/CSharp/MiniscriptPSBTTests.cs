using System;
using System.Collections.Generic;
using Xunit;
using NBitcoin.BIP174;
using Microsoft.FSharp.Core;

namespace NBitcoin.Miniscript.Tests.CSharp
{
	public class MiniscriptPSBTTests
	{
		private Key[] privKeys { get; }
		public Network Network { get; }

		public MiniscriptPSBTTests()
		{
			privKeys = new[] { new Key(), new Key(), new Key(), new Key() };
			Network = Network.Main;
		}

		private TransactionSignature GetDummySig()
		{
			var hash = new uint256();
			var ecdsa = privKeys[0].Sign(hash);
			return new TransactionSignature(ecdsa, SigHash.All);
		}

		[Fact]
		public void ShouldSatisfyMiniscript()
		{
			var policyStr = $"aor(and(pk({privKeys[0].PubKey}), time({10000})), multi(2, {privKeys[0].PubKey}, {privKeys[1].PubKey})";
			var ms = Miniscript.ParseUnsafe(policyStr);
			Assert.NotNull(ms);

			// Giving empty dict.
			var sigDict = new Dictionary<PubKey, TransactionSignature>();
			var r1 = ms.Satisfy(sigDict);
			// Assert.NotNull(r1);
			Console.WriteLine(r1);

			// Giving lambda
			var someSig = FSharpOption<TransactionSignature>.Some(GetDummySig());
			var r2 = ms.Satisfy(pk => pk == privKeys[0].PubKey ? someSig : null);
			// Assert.NotNull(r2);
			Console.WriteLine(r2);
		}

		[Fact]
		public void ShouldSatisfyPSBT()
		{
			var policyStr = $"aor(and(pk({privKeys[0].PubKey}), time({10000})), multi(2, {privKeys[0].PubKey}, {privKeys[1].PubKey})";
			var ms = Miniscript.ParseUnsafe(policyStr);
			var script = ms.ToScript();
			var funds = Utils.CreateDummyFunds(Network, privKeys, script);
			var tx = Utils.CreateTxToSpendFunds(funds, privKeys, script, false, false);
			var psbt = PSBT.FromTransaction(tx)
				.AddTransactions(funds)
				.AddScript(script);
			// psbt.Satisfy();
		}
	}
}
