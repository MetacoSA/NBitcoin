using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	//https://github.com/OpenAssets/open-assets-protocol/blob/master/specification.mediawiki
	public class ColoredCoinsTests
	{
		class ColoredCoinTester
		{
			public ColoredCoinTester([CallerMemberName]string test = null)
			{
				var testcase = JsonConvert.DeserializeObject<TestCase[]>(File.ReadAllText("Data/openasset-known-tx.json"))
					.First(t => t.test == test);
				NoSqlTransactionRepository repository = new NoSqlTransactionRepository();
				foreach(var tx in testcase.txs)
				{
					var txObj = new Transaction(tx);
					repository.Put(txObj.GetHash(), txObj);
				}
				TestedTxId = new uint256(testcase.testedtx);
				Repository = new NoSqlColoredTransactionRepository(repository, new InMemoryNoSqlRepository());
			}


			public IColoredTransactionRepository Repository
			{
				get;
				set;
			}

			public uint256 TestedTxId
			{
				get;
				set;
			}

			public string AutoDownloadMissingTransaction(Action act)
			{
				StringBuilder builder = new StringBuilder();
				while(true)
				{
					try
					{
						act();
						break;
					}
					catch(TransactionNotFoundException ex)
					{
						WebClient client = new WebClient();
						var result = client.DownloadString("http://btc.blockr.io/api/v1/tx/raw/" + ex.TxId);
						var json = JObject.Parse(result);
						var tx = new Transaction(json["data"]["tx"]["hex"].ToString());

						builder.AppendLine("\"" + json["data"]["tx"]["hex"].ToString() + "\",\r\n");
						Repository.Transactions.Put(tx.GetHash(), tx);
					}
				}
				return builder.ToString();
			}
		}
		class TestCase
		{
			public string test
			{
				get;
				set;
			}
			public string testedtx
			{
				get;
				set;
			}
			public string[] txs
			{
				get;
				set;
			}
		}

		class AssetKey
		{
			public AssetKey()
			{
				Key = new Key();
				ScriptPubKey = Key.PubKey.GetAddress(Network.Main).PaymentScript;
				Id = ScriptPubKey.ID.ToAssetId();
			}
			public Key Key
			{
				get;
				set;
			}
			public Script ScriptPubKey
			{
				get;
				set;
			}
			public AssetId Id
			{
				get;
				set;
			}
		}

		//[Fact]
		public void TestFun()
		{
			var repo = new NoSqlColoredTransactionRepository(new BlockrTransactionRepository());
			var colored = ColoredTransaction.FetchColors(new uint256("b4399a545c4ddd640920d63af75e7367fe4d94b2d7f7a3423105e25ac5f165a6"), repo);

			var prismColored = new CoinprismColoredTransactionRepository().Get(new uint256("b4399a545c4ddd640920d63af75e7367fe4d94b2d7f7a3423105e25ac5f165a6"));

			Assert.True(colored.ToBytes().SequenceEqual(prismColored.ToBytes()));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		//https://github.com/OpenAssets/open-assets-protocol/blob/master/specification.mediawiki
		public void CanColorizeSpecScenario()
		{
			var repo = new NoSqlColoredTransactionRepository();
			var dust = Money.Parse("0.00005");
			var colored = new ColoredTransaction();
			var a1 = new AssetKey();
			var a2 = new AssetKey();
			var h = new AssetKey();
			var sender = new Key().PubKey.GetAddress(Network.Main);
			var receiver = new Key().PubKey.GetAddress(Network.Main);

			colored.Marker = new ColorMarker(new ulong[] { 0, 10, 6, 0, 7, 3 });
			colored.Inputs.Add(new ColoredEntry(0, new Asset(a1.Id, 3UL)));
			colored.Inputs.Add(new ColoredEntry(1, new Asset(a1.Id, 2UL)));
			colored.Inputs.Add(new ColoredEntry(3, new Asset(a1.Id, 5UL)));
			colored.Inputs.Add(new ColoredEntry(4, new Asset(a1.Id, 3UL)));
			colored.Inputs.Add(new ColoredEntry(5, new Asset(a2.Id, 9UL)));

			colored.Issuances.Add(new ColoredEntry(1, new Asset(h.Id, 10UL)));
			colored.Transfers.Add(new ColoredEntry(3, new Asset(a1.Id, 6UL)));
			colored.Transfers.Add(new ColoredEntry(5, new Asset(a1.Id, 7UL)));
			colored.Transfers.Add(new ColoredEntry(6, new Asset(a2.Id, 3UL)));
			var destroyed = colored.GetDestroyedAssets();
			Assert.True(destroyed.Length == 1);
			Assert.True(destroyed[0].Quantity == 6);
			Assert.True(destroyed[0].Id == a2.Id);
			colored = colored.Clone();
			destroyed = colored.GetDestroyedAssets();
			Assert.True(destroyed.Length == 1);
			Assert.True(destroyed[0].Quantity == 6);
			Assert.True(destroyed[0].Id == a2.Id);

			var prior = new Transaction();
			prior.Outputs.Add(new TxOut(dust, a1.ScriptPubKey));
			prior.Outputs.Add(new TxOut(dust, a2.ScriptPubKey));
			prior.Outputs.Add(new TxOut(dust, h.ScriptPubKey));
			repo.Transactions.Put(prior.GetHash(), prior);

			var issuanceA1 = new Transaction();
			issuanceA1.Inputs.Add(new TxIn(new OutPoint(prior.GetHash(), 0)));
			issuanceA1.Outputs.Add(new TxOut(dust, h.ScriptPubKey));
			issuanceA1.Outputs.Add(new TxOut(dust, sender));
			issuanceA1.Outputs.Add(new TxOut(dust, sender));
			issuanceA1.Outputs.Add(new TxOut(dust, sender));
			issuanceA1.Outputs.Add(new TxOut(dust, new ColorMarker(new ulong[] { 3, 2, 5, 3 }).GetScript()));
			repo.Transactions.Put(issuanceA1.GetHash(), issuanceA1);

			var issuanceA2 = new Transaction();
			issuanceA2.Inputs.Add(new TxIn(new OutPoint(prior.GetHash(), 1)));
			issuanceA2.Outputs.Add(new TxOut(dust, sender));
			issuanceA2.Outputs.Add(new TxOut(dust, new ColorMarker(new ulong[] { 9 }).GetScript()));
			repo.Transactions.Put(issuanceA2.GetHash(), issuanceA2);

			var testedTx = CreateSpecTransaction(repo, dust, receiver, prior, issuanceA1, issuanceA2);
			var actualColored = testedTx.GetColoredTransaction(repo);

			Assert.True(colored.ToBytes().SequenceEqual(actualColored.ToBytes()));


			//Finally, for each transfer output, if the asset units forming that output all have the same asset address, the output gets assigned that asset address. If any output contains units from more than one distinct asset address, the whole transaction is considered invalid, and all outputs are uncolored.

			var testedBadTx = CreateSpecTransaction(repo, dust, receiver, prior, issuanceA1, issuanceA2);
			testedBadTx.Outputs[2] = new TxOut(dust, new ColorMarker(new ulong[] { 0, 10, 6, 0, 6, 4 }).GetScript());
			repo.Transactions.Put(testedBadTx.GetHash(), testedBadTx);
			colored = testedBadTx.GetColoredTransaction(repo);

			destroyed = colored.GetDestroyedAssets();
			Assert.True(destroyed.Length == 2);
			Assert.True(destroyed[0].Id == a1.Id);
			Assert.True(destroyed[0].Quantity == 13);
			Assert.True(destroyed[1].Id == a2.Id);
			Assert.True(destroyed[1].Quantity == 9);


			//If there are more items in the  asset quantity list  than the number of colorable outputs, the transaction is deemed invalid, and all outputs are uncolored.
			testedBadTx = CreateSpecTransaction(repo, dust, receiver, prior, issuanceA1, issuanceA2);
			testedBadTx.Outputs[2] = new TxOut(dust, new ColorMarker(new ulong[] { 0, 10, 6, 0, 7, 4, 10, 10 }).GetScript());
			repo.Transactions.Put(testedBadTx.GetHash(), testedBadTx);

			colored = testedBadTx.GetColoredTransaction(repo);

			destroyed = colored.GetDestroyedAssets();
			Assert.True(destroyed.Length == 2);
			Assert.True(destroyed[0].Id == a1.Id);
			Assert.True(destroyed[0].Quantity == 13);
			Assert.True(destroyed[1].Id == a2.Id);
			Assert.True(destroyed[1].Quantity == 9);
		}

		private static Transaction CreateSpecTransaction(NoSqlColoredTransactionRepository repo, Money dust, BitcoinAddress receiver, Transaction prior, Transaction issuanceA1, Transaction issuanceA2)
		{
			var testedTx = new Transaction();
			testedTx.Inputs.Add(new TxIn(new OutPoint(issuanceA1.GetHash(), 0)));
			testedTx.Inputs.Add(new TxIn(new OutPoint(issuanceA1.GetHash(), 1)));
			testedTx.Inputs.Add(new TxIn(new OutPoint(prior.GetHash(), 0)));
			testedTx.Inputs.Add(new TxIn(new OutPoint(issuanceA1.GetHash(), 2)));
			testedTx.Inputs.Add(new TxIn(new OutPoint(issuanceA1.GetHash(), 3)));
			testedTx.Inputs.Add(new TxIn(new OutPoint(issuanceA2.GetHash(), 0)));

			testedTx.Outputs.Add(new TxOut(Money.Parse("0.6"), receiver));
			testedTx.Outputs.Add(new TxOut(dust, receiver));
			testedTx.Outputs.Add(new TxOut(dust, new ColorMarker(new ulong[] { 0, 10, 6, 0, 7, 3 }).GetScript()));
			testedTx.Outputs.Add(new TxOut(dust, receiver));
			testedTx.Outputs.Add(new TxOut(dust, receiver));
			testedTx.Outputs.Add(new TxOut(dust, receiver));
			testedTx.Outputs.Add(new TxOut(dust, receiver));
			repo.Transactions.Put(testedTx.GetHash(), testedTx);
			return testedTx;
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanFetchTransactionFromCoinprism()
		{
			CanFetchTransactionFromCoinprismCore("CanColorizeIssuanceTransaction");
			CanFetchTransactionFromCoinprismCore("CanColorizeTransferTransaction");
			Assert.Null(new CoinprismColoredTransactionRepository().Get(new uint256("b4399a545c4ddd640920d63af75e7367fe4d94b2d7f7a3423105e25ac5f165a5")));
		}

		private void CanFetchTransactionFromCoinprismCore(string test)
		{
			var tester = CreateTester(test);
			var expected = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			var actual = new CoinprismColoredTransactionRepository().Get(tester.TestedTxId);
			Assert.True(actual.ToBytes().SequenceEqual(expected.ToBytes()));
		}

		//https://www.coinprism.info/tx/b4399a545c4ddd640920d63af75e7367fe4d94b2d7f7a3423105e25ac5f165a6
		//Asset Id : 3QzJDrSsi4Pm2DhcZFXR9MGJsXXtsYhUsq
		//1BvvRfz4XnxSWJ524TusetYKrtZnAbgV3r to 18Jcv42cRknPmxrQPb2zSBuEVWq3egjCKq
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanColorizeOutputs()
		{
			var tester = CreateTester("CanColorizeIssuanceTransaction");

			var colored1 = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			Assert.True(colored1.Inputs.Count == 0);
			Assert.True(colored1.Issuances.Count == 1);
			Assert.True(colored1.Transfers.Count == 0);
			Assert.Equal("Af59wop4VJjXk2DAzoX9scAUCcAsghPHFX", colored1.Issuances[0].Asset.Id.GetWif(Network.Main).ToString());

			tester = CreateTester("CanColorizeTransferTransaction");
			var colored2 = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			Assert.True(colored2.Inputs.Count == 1);
			Assert.True(colored2.Inputs[0].Asset.ToBytes().SequenceEqual(colored1.Issuances[0].Asset.ToBytes()));
			Assert.True(colored2.Issuances.Count == 0);
			Assert.True(colored2.Transfers.Count == 2);
			Assert.Equal("Af59wop4VJjXk2DAzoX9scAUCcAsghPHFX", colored2.Transfers[0].Asset.Id.GetWif(Network.Main).ToString());

			tester = CreateTester("CanColorizeTransferTransaction");
			var tx = tester.Repository.Transactions.Get(tester.TestedTxId);
			//If there are less items in the  asset quantity list  than the number of colorable outputs (all the outputs except the marker output), the outputs in excess receive an asset quantity of zero.
			tx.Outputs.Add(new TxOut());
			tx.Outputs.Add(new TxOut());
			tx.Outputs.Add(new TxOut());
			tester.TestedTxId = tx.GetHash();
			tester.Repository.Transactions.Put(tester.TestedTxId, tx);
			colored2 = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			Assert.True(colored2.Inputs.Count == 1);
			Assert.True(colored2.Inputs[0].Asset.ToBytes().SequenceEqual(colored1.Issuances[0].Asset.ToBytes()));
			Assert.True(colored2.Issuances.Count == 0);
			Assert.True(colored2.Transfers.Count == 2);
			Assert.Equal("Af59wop4VJjXk2DAzoX9scAUCcAsghPHFX", colored2.Transfers[0].Asset.Id.GetWif(Network.Main).ToString());
			var destroyed = colored2.GetDestroyedAssets();
			Assert.True(destroyed.Length == 0);

			tester = CreateTester("CanColorizeTransferTransaction");
			tx = tester.Repository.Transactions.Get(tester.TestedTxId);
			//If there are more items in the  asset quantity list  than the number of colorable outputs, the transaction is deemed invalid, and all outputs are uncolored.
			var payload = tx.GetColoredMarker();
			payload.Quantities = payload.Quantities.Concat(new ulong[] { 1, 2 }).ToArray();
			tx.Outputs[0].ScriptPubKey = payload.GetScript();
			Assert.False(tx.HasValidColoredMarker());
			tester.TestedTxId = tx.GetHash();
			tester.Repository.Transactions.Put(tester.TestedTxId, tx);
			colored2 = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			Assert.True(colored2.Inputs.Count == 1);
			Assert.True(colored2.Issuances.Count == 0);
			Assert.True(colored2.Transfers.Count == 0);

			tester = CreateTester("CanColorizeTransferTransaction");
			tx = tester.Repository.Transactions.Get(tester.TestedTxId);
			//If the marker output is malformed, the transaction is invalid, and all outputs are uncolored.
			tx.Outputs[0].ScriptPubKey = new Script();
			tester.TestedTxId = tx.GetHash();
			tester.Repository.Transactions.Put(tester.TestedTxId, tx);
			colored2 = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			Assert.True(colored2.Inputs.Count == 1);
			Assert.True(colored2.Issuances.Count == 0);
			Assert.True(colored2.Transfers.Count == 0);


			tester = CreateTester("CanColorizeTransferTransaction");
			tx = tester.Repository.Transactions.Get(tester.TestedTxId);
			//If there are less asset units in the input sequence than in the output sequence, the transaction is considered invalid and all outputs are uncolored.
			payload = tx.GetColoredMarker();
			payload.Quantities[0] = 1001;
			tx.Outputs[0].ScriptPubKey = payload.GetScript();
			tester.TestedTxId = tx.GetHash();
			tester.Repository.Transactions.Put(tester.TestedTxId, tx);
			colored2 = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			Assert.True(colored2.Inputs.Count == 1);
			Assert.True(colored2.Issuances.Count == 0);
			Assert.True(colored2.Transfers.Count == 0);


			tester = CreateTester("CanColorizeTransferTransaction");
			tx = tester.Repository.Transactions.Get(tester.TestedTxId);
			//If there are more asset units in the input sequence than in the output sequence, the transaction is considered valid
			payload = tx.GetColoredMarker();
			payload.Quantities[0] = 999;
			tx.Outputs[0].ScriptPubKey = payload.GetScript();
			tester.TestedTxId = tx.GetHash();
			tester.Repository.Transactions.Put(tester.TestedTxId, tx);
			colored2 = ColoredTransaction.FetchColors(tester.TestedTxId, tester.Repository);
			Assert.True(colored2.Inputs.Count == 1);
			Assert.True(colored2.Issuances.Count == 0);
			Assert.True(colored2.Transfers.Count == 2);
			destroyed = colored2.GetDestroyedAssets();
			Assert.True(destroyed.Length == 1);
			Assert.True(destroyed[0].Quantity == 1);
			Assert.True(destroyed[0].Id == colored2.Inputs[0].Asset.Id);
		}



		private ColoredCoinTester CreateTester([CallerMemberName]string test = null)
		{
			return new ColoredCoinTester(test);
		}


		//Data in the marker output      Description
		//-----------------------------  -------------------------------------------------------------------
		//0x6a                           The OP_RETURN opcode.
		//0x10                           The PUSHDATA opcode for a 16 bytes payload.
		//0x4f 0x41                      The Open Assets Protocol tag.
		//0x01 0x00                      Version 1 of the protocol.
		//0x03                           There are 3 items in the asset quantity list.
		//0xac 0x02 0x00 0xe5 0x8e 0x26  The asset quantity list:
		//							   - '0xac 0x02' means output 0 has an asset quantity of 300.
		//							   - Output 1 is skipped and has an asset quantity of 0
		//								 because it is the marker output.
		//							   - '0x00' means output 2 has an asset quantity of 0.
		//							   - '0xe5 0x8e 0x26' means output 3 has an asset quantity of 624,485.
		//							   - Outputs after output 3 (if any) have an asset quantity of 0.
		//0x04                           The metadata is 4 bytes long.
		//0x12 0x34 0x56 0x78            Some arbitrary metadata.
		//00000000000000001c7a19e8ef62d815d84a473f543de77f23b8342fc26812a9 at 299220 Monday, May 5, 2014 3:47:37 PM first block
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanParseColorMarker()
		{
			var script = new Script(Encoders.Hex.DecodeData("6a104f41010003ac0200e58e260412345678"));
			var marker = ColorMarker.TryParse(script);
			Assert.NotNull(marker);
			Assert.Equal(1, marker.Version);
			Assert.Equal(3, marker.Quantities.Length);
			Assert.True(marker.Quantities.SequenceEqual(new ulong[] { 300, 0, 624485 }));
			Assert.True(marker.Metadata.SequenceEqual(new byte[] { 0x12, 0x34, 0x56, 0x78 }));
			Assert.Equal(script.ToString(), marker.GetScript().ToString());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreateAssetAddress()
		{
			//The issuer first generates a private key: 18E14A7B6A307F426A94F8114701E7C8E774E7F9A47E2C2035DB29A206321725.
			var key = new Key(TestUtils.ParseHex("18E14A7B6A307F426A94F8114701E7C8E774E7F9A47E2C2035DB29A206321725"));
			//He calculates the corresponding address: 16UwLL9Risc3QfPqBUvKofHmBQ7wMtjvM.
			var address = key.PubKey.Decompress().GetAddress(Network.Main);
			Assert.Equal("16UwLL9Risc3QfPqBUvKofHmBQ7wMtjvM", address.ToString());

			//Next, he builds the Pay-to-PubKey-Hash script associated to that address: OP_DUP OP_HASH160 010966776006953D5567439E5E39F86A0D273BEE OP_EQUALVERIFY OP_CHECKSIG
			Script script = Script.CreateFromDestinationAddress(address);
			Assert.Equal("OP_DUP OP_HASH160 010966776006953D5567439E5E39F86A0D273BEE OP_EQUALVERIFY OP_CHECKSIG", script.ToString().ToUpper());

			var oo = script.GetScriptAddress(Network.Main);
			//The script is hashed: 36e0ea8e93eaa0285d641305f4c81e563aa570a2.
			Assert.Equal("36e0ea8e93eaa0285d641305f4c81e563aa570a2", script.ID.ToString());

			Assert.Equal("36e0ea8e93eaa0285d641305f4c81e563aa570a2", key.PubKey.Decompress().HashPaymentScript.ID.ToString());
			//Finally, the hash is converted to a base 58 string with checksum using version byte 23: ALn3aK1fSuG27N96UGYB1kUYUpGKRhBuBC. 
			Assert.Equal("ALn3aK1fSuG27N96UGYB1kUYUpGKRhBuBC", script.ID.ToAssetId().GetWif(Network.Main).ToString());
		}

	}
}
