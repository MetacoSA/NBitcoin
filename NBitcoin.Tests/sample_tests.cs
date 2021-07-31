using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class sample_tests
	{
#if HAS_SPAN
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public async Task CanBuildTaprootSingleSigTransactions()
		{
			using (var nodeBuilder = NodeBuilderEx.Create())
			{
				var rpc = nodeBuilder.CreateNode().CreateRPCClient();
				nodeBuilder.StartAll();
				rpc.Generate(102);
				var change = new Key();
				var rootKey = new ExtKey();
				var accountKeyPath = new KeyPath("86'/0'/0'");
				var accountRootKeyPath = new RootedKeyPath(rootKey.GetPublicKey().GetHDFingerPrint(), accountKeyPath);
				var accountKey = rootKey.Derive(accountKeyPath);
				var key = accountKey.Derive(new KeyPath("0/0")).PrivateKey;
				var address = key.PubKey.GetAddress(ScriptPubKeyType.TaprootBIP86, nodeBuilder.Network);
				var destination = new Key();
				var amount = new Money(1, MoneyUnit.BTC);
				uint256 id = null;
				Transaction tx = null;
				ICoin coin = null;
				TransactionBuilder builder = null;
				var rate = new FeeRate(Money.Satoshis(1), 1);

				async Task RefreshCoin()
				{
					id = await rpc.SendToAddressAsync(address, Money.Coins(1));
					tx = await rpc.GetRawTransactionAsync(id);
					coin = tx.Outputs.AsCoins().Where(o => o.ScriptPubKey == address.ScriptPubKey).Single();
					builder = Network.Main.CreateTransactionBuilder(0);
				}
				await RefreshCoin();
				var signedTx = builder
					.AddCoins(coin)
					.AddKeys(key)
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(change)
					.SendEstimatedFees(rate)
					.BuildTransaction(true);
				rpc.SendRawTransaction(signedTx);

				await RefreshCoin();
				// Let's try again, but this time with PSBT
				var psbt = builder
					.AddCoins(coin)
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(change)
					.SendEstimatedFees(rate)
					.BuildPSBT(false);

				var tk = key.PubKey.GetTaprootFullPubKey();
				psbt.Inputs[0].HDTaprootKeyPaths.Add(tk.OutputKey, new TaprootKeyPath(accountRootKeyPath.Derive(KeyPath.Parse("0/0"))));
				psbt.SignAll(ScriptPubKeyType.TaprootBIP86, accountKey, accountRootKeyPath);

				// Check if we can roundtrip
				psbt = CanRoundtripPSBT(psbt);

				psbt.Finalize();
				rpc.SendRawTransaction(psbt.ExtractTransaction());

				// Let's try again, but this time with BuildPSBT(true)
				await RefreshCoin();
				psbt = builder
					.AddCoins(coin)
					.AddKeys(key)
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(change)
					.SendEstimatedFees(rate)
					.BuildPSBT(true);
				psbt.Finalize();
				rpc.SendRawTransaction(psbt.ExtractTransaction());

				// Let's try again, this time with a merkle root
				var merkleRoot = RandomUtils.GetUInt256();
				address = key.PubKey.GetTaprootFullPubKey(merkleRoot).GetAddress(nodeBuilder.Network);

				await RefreshCoin();
				psbt = builder
					.AddCoins(coin)
					.AddKeys(key.CreateTaprootKeyPair(merkleRoot))
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(change)
					.SendEstimatedFees(rate)
					.BuildPSBT(true);
				Assert.NotNull(psbt.Inputs[0].TaprootMerkleRoot);
				Assert.NotNull(psbt.Inputs[0].TaprootInternalKey);
				Assert.NotNull(psbt.Inputs[0].TaprootKeySignature);
				psbt = CanRoundtripPSBT(psbt);
				psbt.Finalize();
				rpc.SendRawTransaction(psbt.ExtractTransaction());

				// Can we sign the PSBT separately?
				await RefreshCoin();
				psbt = builder
					.AddCoins(coin)
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(change)
					.SendEstimatedFees(rate)
					.BuildPSBT(false);

				var taprootKeyPair = key.CreateTaprootKeyPair(merkleRoot);
				psbt.Inputs[0].Sign(taprootKeyPair);
				Assert.NotNull(psbt.Inputs[0].TaprootMerkleRoot);
				Assert.NotNull(psbt.Inputs[0].TaprootInternalKey);
				Assert.NotNull(psbt.Inputs[0].TaprootKeySignature);

				// This line is useless, we just use it to test the PSBT roundtrip
				psbt.Inputs[0].HDTaprootKeyPaths.Add(taprootKeyPair.PubKey,
												     new TaprootKeyPath(RootedKeyPath.Parse("12345678/86'/0'/0'/0/0"),
													 new uint256[] { RandomUtils.GetUInt256() }));
				psbt = CanRoundtripPSBT(psbt);
				psbt.Finalize();
				rpc.SendRawTransaction(psbt.ExtractTransaction());

				// Can we sign the transaction separately?
				await RefreshCoin();
				var coin1 = coin;
				await RefreshCoin();
				var coin2 = coin;
				builder = Network.Main.CreateTransactionBuilder(0);
				signedTx = builder
					.AddCoins(coin1, coin2)
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(change)
					.SendEstimatedFees(rate)
					.BuildTransaction(false);
				var unsignedTx = signedTx.Clone();
				builder = Network.Main.CreateTransactionBuilder(0);
				builder.AddKeys(key.CreateTaprootKeyPair(merkleRoot));
				builder.AddCoins(coin1);
				var ex = Assert.Throws<InvalidOperationException>(() => builder.SignTransactionInPlace(signedTx));
				Assert.Contains("taproot", ex.Message);
				builder.AddCoin(coin2);
				builder.SignTransactionInPlace(signedTx);
				Assert.True(!WitScript.IsNullOrEmpty(signedTx.Inputs.FindIndexedInput(coin2.Outpoint).WitScript));
				// Another solution is to set the precomputed transaction data.
				signedTx = unsignedTx;
				builder = Network.Main.CreateTransactionBuilder(0);
				builder.AddKeys(key.CreateTaprootKeyPair(merkleRoot));
				builder.AddCoins(coin2);
				builder.SetSigningOptions(new SigningOptions() { PrecomputedTransactionData = signedTx.PrecomputeTransactionData(new ICoin[] { coin1, coin2 }) });
				builder.SignTransactionInPlace(signedTx);
				Assert.True(!WitScript.IsNullOrEmpty(signedTx.Inputs.FindIndexedInput(coin2.Outpoint).WitScript));


				// Let's check if we estimate precisely the size of a taproot transaction.
				await RefreshCoin();
				signedTx = builder
					.AddCoins(coin)
					.AddKeys(key.CreateTaprootKeyPair(merkleRoot))
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(change)
					.SendEstimatedFees(rate)
					.BuildTransaction(false);
				var actualvsize = builder.EstimateSize(signedTx, true);
				builder.SignTransactionInPlace(signedTx);
				var expectedvsize = signedTx.GetVirtualSize();
				// The estimator can't assume the sighash to be default
				// for all inputs, so we likely overestimate 1 bytes per input
				Assert.Equal(expectedvsize, actualvsize - 1);
			}
		}

		private static PSBT CanRoundtripPSBT(PSBT psbt)
		{
			var psbtBefore = psbt.ToString();
			psbt = psbt.Clone();
			var psbtAfter = psbt.ToString();
			Assert.Equal(psbtBefore, psbtAfter);
			return psbt;
		}
#endif
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


				var destination = new Key();
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
					.SetChange(new Key())
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


				var destination = new Key();
				var amount = new Money(1, MoneyUnit.BTC);


				var builder = Network.Main.CreateTransactionBuilder();
				var rate = new FeeRate(Money.Satoshis(1), 1);
				var partiallySignedTx = builder
					.AddCoins(scriptCoin)
					.AddKeys(keys[0])
					.Send(destination, amount)
					.SubtractFees()
					.SetChange(new Key())
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


				var destination = new Key();
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
