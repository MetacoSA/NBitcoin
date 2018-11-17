using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NBitcoin;
using NBitcoin.Tests.Generators;

namespace NBitcoin.Tests.Generators
{
	public class LegacyTransactionGenerators
	{
		public static Arbitrary<Transaction> TransactionArb() => Arb.From(TX());
		public static Arbitrary<Tuple<Transaction, Network>> TransactionAndNetworkArb()
		{
			var result = from n in ChainParamsGenerator.NetworkGen()
									 from tx in TX(n)
									 select Tuple.Create(tx, n);
			return Arb.From(result);
		}

		public static Gen<OutPoint> OutPoint() => from txid in CryptoGenerator.Hash256()
																							from vout in Gen.Choose(0, Int32.MaxValue)
																							select new OutPoint(txid, vout);

		public static Gen<TxIn> Input() => from prevout in OutPoint()
																			 from ss in ScriptGenerator.RandomScriptSig()
																			 from nSequence in PrimitiveGenerator.UInt32()
																			 select new TxIn(prevout, ss) { Sequence = nSequence };

		public static Gen<List<TxIn>> NonEmptyInputs() => from txins in Gen.NonEmptyListOf(Input())
																											select txins.ToList();

		public static Gen<TxOut> Output() =>
			from m in MoneyGenerator.Money()
			from spk in ScriptGenerator.LegacyScriptPubKey()
			select new TxOut(m, spk);

		public static Gen<List<TxOut>> NonEmptyOutputs() =>
			from txouts in Gen.NonEmptyListOf(Output())
			select txouts.ToList();

		public static Gen<Transaction> TX() =>
			from network in ChainParamsGenerator.NetworkGen()
			from tx in TX(network)
			select tx;

		public static Gen<Transaction> TX(Network network) =>
			from version in Gen.Choose(0, Int32.MaxValue)
			from txin in NonEmptyInputs()
			from txout in NonEmptyOutputs()
			from locktime in PrimitiveGenerator.UInt32()
			select ComposeTx(Transaction.Create(network), txin, txout, locktime);

		// We need this since Tranaction is mutable.
		public static Transaction ComposeTx(Transaction tx, List<TxIn> inputs, List<TxOut> outputs, uint locktime)
		{
			tx.Inputs.AddRange(inputs);
			tx.Outputs.AddRange(outputs);
			tx.LockTime = locktime;
			return tx;
		}
	}
};