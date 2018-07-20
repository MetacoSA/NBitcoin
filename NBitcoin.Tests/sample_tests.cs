using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace NBitcoin.Tests
{
    public class sample_tests
    {
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildSegwitP2SHMultisigTransactions()
		{
			using(var nodeBuilder = NodeBuilderEx.Create())
			{
				var rpc = nodeBuilder.CreateNode().CreateRPCClient();
				nodeBuilder.StartAll();
				rpc.Generate(102);

				// Build the keys and addresses
				var masterKeys = Enumerable.Range(0, 3).Select(_ => new ExtKey()).ToArray();
				var keyRedeemAddresses = Enumerable.Range(0, 4)
									  .Select(i => masterKeys.Select(m => m.Derive(i, false)).ToArray())
									  .Select(keys =>
									  (
										Keys: keys.Select(k => k.PrivateKey).ToArray(),
										Redeem: PayToMultiSigTemplate.Instance.GenerateScriptPubKey(keys.Length, keys.Select(k => k.PrivateKey.PubKey).ToArray()))
									  ).Select(_ =>
									  (
										Keys: _.Keys,
										Redeem: _.Redeem,
										Address: _.Redeem.WitHash.ScriptPubKey.Hash.ScriptPubKey.GetDestinationAddress(nodeBuilder.Network)
									  ));


				// Fund the addresses
				var scriptCoins = keyRedeemAddresses.Select(async kra =>
				{
					var id = await rpc.SendToAddressAsync(kra.Address, Money.Coins(1));
					var tx = await rpc.GetRawTransactionAsync(id);
					return tx.Outputs.AsCoins().Where(o => o.ScriptPubKey == kra.Address.ScriptPubKey)
									.Select(c => c.ToScriptCoin(kra.Redeem)).Single();
				}).Select(t => t.Result).ToArray();


				var destination = new Key().ScriptPubKey;
				var amount = new Money(1, MoneyUnit.BTC);
				var redeemScripts = keyRedeemAddresses.Select(kra => kra.Redeem).ToArray();
				var privateKeys = keyRedeemAddresses.SelectMany(kra => kra.Keys).ToArray();


				var builder = new TransactionBuilder();
				var rate = new FeeRate(Money.Satoshis(1), 1);
				var signedTx = builder
					.AddCoins(scriptCoins)
					.AddKeys(privateKeys)
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(new Key().ScriptPubKey)
					.SendEstimatedFees(rate)
					.BuildTransaction(true);

				rpc.SendRawTransaction(signedTx);
				var errors = builder.Check(signedTx);
				Assert.Empty(errors);
			}
		}
	}
}
