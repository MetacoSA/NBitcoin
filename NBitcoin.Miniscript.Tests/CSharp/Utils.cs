using System;
using NBitcoin;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Miniscript.Tests.CSharp
{
	/// <summary>
	///  Copied and tweaked from `NBitcoin.Tests.PSBTTests` .
	/// It could possibly reference the method directly, but we prefered to keep the libraries separated.
	/// </summary>
	public class Utils
	{
		public Utils()
		{
		}
		static internal ICoin[] DummyFundsToCoins(IEnumerable<Transaction> txs, Script redeem, Key key)
			{
				var barecoins = txs.SelectMany(tx => tx.Outputs.AsCoins()).ToArray();
				var coins = new ICoin[barecoins.Length];
				coins[0] = barecoins[0];
				coins[1] = barecoins[1];
				coins[2] = redeem != null ? new ScriptCoin(barecoins[2], redeem) : barecoins[2]; // p2sh
				coins[3] = redeem != null ? new ScriptCoin(barecoins[3], redeem) : barecoins[3]; // p2wsh
				coins[4] = key != null ? new ScriptCoin(barecoins[4], key.PubKey.WitHash.ScriptPubKey) : barecoins[4]; // p2sh-p2wpkh
				coins[5] = redeem != null ? new ScriptCoin(barecoins[5], redeem) : barecoins[5]; // p2sh-p2wsh
				return coins;
			}

			static internal Transaction CreateTxToSpendFunds(
					Transaction[] funds,
					Key[] keys,
					Script redeem,
					bool withScript,
					bool sign
				)
			{
				var tx = Network.Main.CreateTransaction();
				tx.Inputs.Add(new OutPoint(funds[0].GetHash(), 0)); // p2pkh
				tx.Inputs.Add(new OutPoint(funds[0].GetHash(), 1)); // p2wpkh
				tx.Inputs.Add(new OutPoint(funds[1].GetHash(), 0)); // p2sh
				tx.Inputs.Add(new OutPoint(funds[2].GetHash(), 0)); // p2wsh
				tx.Inputs.Add(new OutPoint(funds[3].GetHash(), 0)); // p2sh-p2wpkh
				tx.Inputs.Add(new OutPoint(funds[4].GetHash(), 0)); // p2sh-p2wsh

				var dummyOut = new TxOut(Money.Coins(0.599m), keys[0]);
				tx.Outputs.Add(dummyOut);

				if (withScript)
				{
					// OP_0 + three empty signatures
					var emptySigPush = new Script(OpcodeType.OP_0, OpcodeType.OP_0, OpcodeType.OP_0, OpcodeType.OP_0);
					tx.Inputs[0].ScriptSig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(null, keys[0].PubKey);
					tx.Inputs[1].WitScript = PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(null, keys[0].PubKey);
					tx.Inputs[2].ScriptSig = emptySigPush + Op.GetPushOp(redeem.ToBytes());
					tx.Inputs[3].WitScript = PayToWitScriptHashTemplate.Instance.GenerateWitScript(emptySigPush, redeem);
					tx.Inputs[4].ScriptSig = new Script(Op.GetPushOp(keys[0].PubKey.WitHash.ScriptPubKey.ToBytes()));
					tx.Inputs[4].WitScript = PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(null, keys[0].PubKey);
					tx.Inputs[5].ScriptSig = new Script(Op.GetPushOp(redeem.WitHash.ScriptPubKey.ToBytes()));
					tx.Inputs[5].WitScript = PayToWitScriptHashTemplate.Instance.GenerateWitScript(emptySigPush, redeem);
				}

				if (sign)
				{
					tx.Sign(keys, DummyFundsToCoins(funds, redeem, keys[0]));
				}
				return tx;
			}

			static public Transaction[] CreateDummyFunds(Network network, Key[] keyForOutput, Script redeem)
			{
				// 1. p2pkh and p2wpkh
				var tx1 = network.CreateTransaction();
				tx1.Inputs.Add(TxIn.CreateCoinbase(200));
				tx1.Outputs.Add(new TxOut(Money.Coins(0.1m), keyForOutput[0].PubKey.Hash));
				tx1.Outputs.Add(new TxOut(Money.Coins(0.1m), keyForOutput[0].PubKey.WitHash));

				// 2. p2sh-multisig
				var tx2 = network.CreateTransaction();
				tx2.Inputs.Add(TxIn.CreateCoinbase(200));
				tx2.Outputs.Add(new TxOut(Money.Coins(0.1m), redeem.Hash));

				// 3. p2wsh
				var tx3 = network.CreateTransaction();
				tx3.Inputs.Add(TxIn.CreateCoinbase(200));
				tx3.Outputs.Add(new TxOut(Money.Coins(0.1m), redeem.WitHash));

				// 4. p2sh-p2wpkh
				var tx4 = network.CreateTransaction();
				tx4.Inputs.Add(TxIn.CreateCoinbase(200));
				tx4.Outputs.Add(new TxOut(Money.Coins(0.1m), keyForOutput[0].PubKey.WitHash.ScriptPubKey.Hash));

				// 5. p2sh-p2wsh
				var tx5 = network.CreateTransaction();
				tx5.Inputs.Add(TxIn.CreateCoinbase(200));
				tx5.Outputs.Add(new TxOut(Money.Coins(0.1m), redeem.WitHash.ScriptPubKey.Hash.ScriptPubKey));
				return new Transaction[] { tx1, tx2, tx3, tx4, tx5 };

		}
	}
}
