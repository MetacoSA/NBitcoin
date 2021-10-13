using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Bench
{
	public class PSBTSigningBench
	{
		string psbtStr;
		string psbtSignedStr;
		private RootedKeyPath accPath;
		private IHDKey acc;

		[GlobalSetup]
		public void Setup()
		{
			var seed = new ExtKey();
			accPath = new KeyPath("87'/0'/0'").ToRootedKeyPath(seed.GetPublicKey().GetHDFingerPrint());
			acc = seed.Derive(accPath.KeyPath).AsHDKeyCache();
			var coins = Enumerable
				.Range(0, 1300)
				.Select(i => new Coin(RandomOutpoint(), new TxOut(Money.Coins(1.0m), acc.Derive(0).Derive((uint)i).GetPublicKey().GetScriptPubKey(ScriptPubKeyType.Segwit))))
				.ToArray();
			var tx = Transaction.Create(Network.Main);
			foreach (var c in coins)
			{
				tx.Inputs.Add(c.Outpoint);
			}
			tx.Outputs.Add(Money.Coins(1299.0m), new Key());
			var psbt = PSBT.FromTransaction(tx, Network.Main);
			psbt.AddCoins(coins);
			for (int i = 0; i < coins.Length; i++)
			{
				psbt.Inputs[i].AddKeyPath(acc.Derive(0).Derive((uint)i).GetPublicKey(), accPath.Derive(0).Derive((uint)i));
			}
			psbtStr = psbt.ToBase64();
			psbt.SignAll(acc.AsHDScriptPubKey(ScriptPubKeyType.Segwit), acc, accPath);
			psbtSignedStr = psbt.ToBase64();
		}

		private OutPoint RandomOutpoint()
		{
			return new OutPoint(RandomUtils.GetUInt256(), 0);
		}

		[Benchmark]
		public void BenchSignPSBT()
		{
			var psbt = PSBT.Parse(psbtStr, Network.Main);
			psbt.SignAll(acc.AsHDScriptPubKey(ScriptPubKeyType.Segwit), acc, accPath);
		}
		[Benchmark]
		public void BenchFinalizePSBT()
		{
			var psbt = PSBT.Parse(psbtSignedStr, Network.Main);
			psbt.Finalize();
		}
	}
}
