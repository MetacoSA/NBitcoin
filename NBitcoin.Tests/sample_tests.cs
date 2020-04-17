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
			using (var nodeBuilder = NodeBuilderEx.Create())
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


				var builder = Network.Main.CreateTransactionBuilder();
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

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildSegwitP2SHMultisigTransactionsWithPSBT()
		{
			using (var nodeBuilder = NodeBuilderEx.Create())
			{
				var rpc = nodeBuilder.CreateNode().CreateRPCClient();
				nodeBuilder.StartAll();
				rpc.Generate(102);

				// Build the keys and addresses
				var masterKeys = Enumerable.Range(0, 3).Select(_ => new ExtKey()).ToArray();
				var keys = masterKeys.Select(mk => mk.Derive(0, false).PrivateKey).ToArray();
				var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(keys.Length, keys.Select(k => k.PubKey).ToArray());
				var address = redeem.WitHash.ScriptPubKey.Hash.ScriptPubKey.GetDestinationAddress(nodeBuilder.Network);

				var id = rpc.SendToAddress(address, Money.Coins(1));
				var tx = rpc.GetRawTransaction(id);
				var scriptCoin = tx.Outputs.AsCoins()
									.Where(o => o.ScriptPubKey == address.ScriptPubKey)
									.Select(o => o.ToScriptCoin(redeem))
									.Single();


				var destination = new Key().ScriptPubKey;
				var amount = new Money(1, MoneyUnit.BTC);


				var builder = Network.Main.CreateTransactionBuilder();
				var rate = new FeeRate(Money.Satoshis(1), 1);
				var partiallySignedTx = builder
					.AddCoins(scriptCoin)
					.AddKeys(keys[0])
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(new Key().ScriptPubKey)
					.SendEstimatedFees(rate)
					.BuildPSBT(true);
				Assert.True(partiallySignedTx.Inputs.All(i => i.PartialSigs.Count == 1));

				partiallySignedTx = PSBT.Load(partiallySignedTx.ToBytes(), Network.Main);

				Network.Main.CreateTransactionBuilder()
						  .AddKeys(keys[1], keys[2])
						  .SignPSBT(partiallySignedTx);

				Assert.True(partiallySignedTx.Inputs.All(i => i.PartialSigs.Count == 3));
				partiallySignedTx.Finalize();
				Assert.DoesNotContain(partiallySignedTx.Inputs.Select(i => i.GetSignableCoin()), o => o is null);

				var signedTx = partiallySignedTx.ExtractTransaction();
				rpc.SendRawTransaction(signedTx);
				var errors = builder.Check(signedTx);
				Assert.Empty(errors);
			}
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSignPSBTWithRootAndAccountKey()
		{
			using (var nodeBuilder = NodeBuilderEx.Create())
			{
				var rpc = nodeBuilder.CreateNode().CreateRPCClient();
				nodeBuilder.StartAll();
				rpc.Generate(102);
				uint hardenedFlag = 0U;
			retry:
				var masterKey = new ExtKey();
				var accountKeyPath = new RootedKeyPath(masterKey, new KeyPath("49'/0'/0'"));
				var accountKey = masterKey.Derive(accountKeyPath);
				var addresses = Enumerable.Range(0, 5)
									  .Select(i =>
									  {
										  var addressPath = new KeyPath(new uint[] { 0U | hardenedFlag, (uint)i | hardenedFlag });
										  var fullAddressPath = accountKeyPath.Derive(addressPath);
										  var address = accountKey.Derive(addressPath).GetPublicKey().WitHash.GetAddress(nodeBuilder.Network);
										  return new
										  {
											  FullAddressPath = fullAddressPath,
											  AddressPath = addressPath,
											  Address = address
										  };
									  }).ToList();

				var changeAddress = addresses.Last();
				addresses = addresses.Take(addresses.Count - 1).ToList();


				// Fund the addresses
				var coins = addresses.Select(async kra =>
				{
					var id = await rpc.SendToAddressAsync(kra.Address, Money.Coins(1));
					var tx = await rpc.GetRawTransactionAsync(id);
					return tx.Outputs.AsCoins().Where(o => o.ScriptPubKey == kra.Address.ScriptPubKey).Single();
				}).Select(t => t.Result).ToArray();


				var destination = new Key().ScriptPubKey;
				var amount = new Money(1, MoneyUnit.BTC);

				var builder = Network.Main.CreateTransactionBuilder();

				var fee = new Money(100_000L);
				var partiallySignedTx = builder
					.AddCoins(coins)
					.Send(destination, amount)
					.SetChange(changeAddress.Address)
					.SendFees(fee)
					.BuildPSBT(false);
				partiallySignedTx.AddKeyPath(masterKey, addresses.Concat(new[] { changeAddress }).Select(a => a.FullAddressPath.KeyPath).ToArray());
				var expectedBalance = -amount - fee;
				var actualBalance = partiallySignedTx.GetBalance(ScriptPubKeyType.Segwit, accountKey, accountKeyPath);
				Assert.Equal(expectedBalance, actualBalance);

				actualBalance = partiallySignedTx.GetBalance(ScriptPubKeyType.Segwit, masterKey);
				Assert.Equal(expectedBalance, actualBalance);
				Assert.Equal(Money.Zero, partiallySignedTx.GetBalance(ScriptPubKeyType.Legacy, masterKey));
				// You can sign with accountKey and keypath
				var memento = partiallySignedTx.Clone();

				partiallySignedTx.SignAll(ScriptPubKeyType.Segwit, accountKey, accountKeyPath);
				Assert.True(partiallySignedTx.Inputs.All(i => i.PartialSigs.Count == 1));
				partiallySignedTx.Finalize();

				var partiallySignedTx2 = memento;

				// Or you can sign with the masterKey
				partiallySignedTx2.SignAll(ScriptPubKeyType.Segwit, masterKey);
				Assert.True(partiallySignedTx2.Inputs.All(i => i.PartialSigs.Count == 1));
				partiallySignedTx2.Finalize();

				Assert.Equal(partiallySignedTx, partiallySignedTx2);

				var signedTx = partiallySignedTx.ExtractTransaction();
				rpc.SendRawTransaction(signedTx);
				var errors = builder.Check(signedTx);
				Assert.Empty(errors);

				if (hardenedFlag == 0)
				{
					hardenedFlag = 0x80000000;
					goto retry;
				}
			}
		}
	}
}
