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

    public static Gen<OutPoint> outPoint() => from txid in CryptoGenerator.hash256()
                                              from vout in Gen.Choose(0, Int32.MaxValue)
                                              select new OutPoint(txid, vout);

    public static Gen<TxIn> input() => from prevout in outPoint()
                                       from ss in ScriptGenerator.scriptSig()
                                       from nSequence in PrimitiveGenerator.uint32()
                                       select new TxIn(prevout, ss) { Sequence = nSequence };

    public static Gen<List<TxIn>> nonEmptyInputs() => from txins in Gen.NonEmptyListOf(input())
                                                     select txins.ToList();

    public static Gen<TxOut> output() =>
      from m in MoneyGenerator.Money()
      from spk in ScriptGenerator.legacyScriptPubKey()
      select new TxOut(m, spk);

    public static Gen<List<TxOut>> nonEmptyOutputs() =>
      from txouts in Gen.NonEmptyListOf(output())
      select txouts.ToList();

    public static Gen<Transaction> TX() =>
      from network in ChainParamsGenerator.NetworkGen()
      from tx in TX(network)
      select tx;

    public static Gen<Transaction> TX(Network network) =>
      from version in Gen.Choose(0, Int32.MaxValue)
      from txin in nonEmptyInputs()
      from txout in nonEmptyOutputs()
      from locktime in PrimitiveGenerator.uint32()
      select CompoundTx(Transaction.Create(network), txin, txout, locktime);

    // We need this since Tranaction is mutable.
    public static Transaction CompoundTx(Transaction tx, List<TxIn> inputs, List<TxOut> outputs, uint locktime)
    {
      tx.Inputs.AddRange(inputs);
      tx.Outputs.AddRange(outputs);
      tx.LockTime = locktime;
      return tx;
    }
  }
};