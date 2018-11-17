using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;

namespace NBitcoin.Tests.Generators
{
	public class SegwitTransactionGenerators
	{
		public static Arbitrary<Transaction> TransactionArb() =>
			Arb.From(TX());

		public static Arbitrary<Tuple<Transaction, Network>> TransactionAndNetworkArb()
		{
			var result = from n in ChainParamsGenerator.NetworkGen()
									 from tx in TX(n)
									 select Tuple.Create(tx, n);
			return Arb.From(result);
		}

		public static Gen<TxIn> WitnessInput() =>
			from txin in LegacyTransactionGenerators.Input()
			from witscript in ScriptGenerator.RandomWitScript()
			select AttachWitScript(txin, witscript);

		private static TxIn AttachWitScript(TxIn txIn, WitScript wit)
		{
			txIn.WitScript = wit;
			return txIn;
		}

		public static Gen<TxOut> WitnessOutput() =>
			from m in MoneyGenerator.Money()
			from spk in ScriptGenerator.WitnessScriptPubKey()
			select new TxOut(m, spk);

		public static Gen<List<TxIn>> NonEmptyInputs() =>
			from txins in Gen.NonEmptyListOf(WitnessInput())
			select txins.ToList();

		public static Gen<List<TxOut>> NonEmptyOutputs() =>
			from txouts in Gen.NonEmptyListOf(WitnessOutput())
			select txouts.ToList();

		public static Gen<Transaction> TX() =>
			from network in ChainParamsGenerator.NetworkGen()
			from tx in TX(network)
			select tx;
		public static Gen<Transaction> TX(Network network) =>
			from version in Gen.Choose(0, Int32.MaxValue)
			from inputs in NonEmptyInputs()
			from outputs in NonEmptyOutputs()
			from locktime in PrimitiveGenerator.UInt32()
			let tx = LegacyTransactionGenerators.ComposeTx(Transaction.Create(network), inputs, outputs, locktime)
		  	where tx.HasWitness
		  	select tx;
	}
}