using System;
using System.Collections.Generic;
using Xunit;
using NBitcoin.Crypto;
using NBitcoin.BIP174;
using NBitcoin.Miniscript;
using static NBitcoin.Miniscript.AbstractPolicy;
using System.Linq;

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
			var ms = Miniscript.FromStringUnsafe(policyStr);
			Assert.NotNull(ms);

			// We can write AbstractPolicy directly instead of using string representation.
			var pubKeys = privKeys.Select(p => p.PubKey).Take(2).ToArray();
			var policy = new AsymmetricOr(
				new And(
					new AbstractPolicy.Key(privKeys[0].PubKey),
					new Time(new LockTime(10000))
					),
				new Multi(2, pubKeys)
			);
			// And it is EqualityComparable by default. 🎉
			var msFromPolicy = Miniscript.FromPolicyUnsafe(policy);
			Assert.Equal(ms, msFromPolicy);

			Func<PubKey, TransactionSignature> dummySignatureProvider =
				pk => pk == privKeys[0].PubKey ? GetDummySig() : null;
			Assert.Throws<MiniscriptSatisfyException>(() => ms.SatisfyUnsafe(dummySignatureProvider));

			Assert.Throws<MiniscriptSatisfyException>(() => ms.SatisfyUnsafe(dummySignatureProvider, null, 9999u));
			var r3 = ms.Satisfy(dummySignatureProvider, null, 10000u);
			Assert.True(r3.IsOk);

			Func<PubKey, TransactionSignature> dummySignatureProvider2 =
				pk => (pk == privKeys[0].PubKey || pk == privKeys[1].PubKey) ? GetDummySig() : null;
			var r5 = ms.Satisfy(dummySignatureProvider2);
			Assert.True(r5.IsOk);
		}

		[Fact]
		public void ShouldSatisfyPSBTWithComplexScript()
		{
			// case 1: bip199 HTLC
			var alice = privKeys[0];
			var bob = privKeys[1];
			var bobSecret = new uint256(0xdeadbeef);
			var bobHash = new uint256(Hashes.SHA256(bobSecret.ToBytes()), false);
			Console.WriteLine($"bobSecret and bobHash is {bobSecret} {bobHash}");
			var policyStr = $"aor(and(hash({bobHash}), pk({bob.PubKey})), and(pk({alice.PubKey}), time({10000})))";
			var ms = Miniscript.FromStringUnsafe(policyStr);
			var script = ms.ToScript();
			var funds = Utils.CreateDummyFunds(Network, privKeys, script);
			var tx = Utils.CreateTxToSpendFunds(funds, privKeys, script, false, false);
			var psbt = PSBT.FromTransaction(tx)
				.AddTransactions(funds)
				.AddScript(script);

			// Can not finalize without signatures.
			Assert.Throws<AggregateException>(() => psbt.FinalizeUnsafe(h => h == bobHash ? bobSecret : null, age: 10001u));
			// It has signature but it is not matured.
			psbt.SignAll(alice);
			Assert.Throws<AggregateException>(() => psbt.FinalizeUnsafe(h => h == bobHash ? bobSecret : null, age: 9999u));

			// it has both signature and a secret.
			psbt.SignAll(bob);
			psbt.FinalizeUnsafe(h => h == bobHash ? bobSecret : null);
			Assert.True(psbt.CanExtractTX());

			var txExtracted = psbt.ExtractTX();
			var builder = Network.CreateTransactionBuilder();
			builder.AddCoins(Utils.DummyFundsToCoins(funds, script, privKeys[0])).AddKeys(privKeys);
			if (!builder.Verify(txExtracted, (Money)null, out var errors))
				throw new InvalidOperationException(errors.Aggregate(string.Empty, (a, b) => a + ";\n" + b));
		}
	}
}
