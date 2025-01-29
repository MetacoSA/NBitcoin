using System;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;

namespace NBitcoin.BIP370;

public class PSBT2Output : PSBTOutput
{

	internal PSBT2Output(Money value, Script scriptPubKey, PSBT parent, uint index) : base(new Map(), parent, index)
	{
		this.Value = value;
		this.ScriptPubKey = scriptPubKey;
	}
	internal PSBT2Output(Map map, PSBT parent, uint index) : base(map, parent, index)
	{
		if (map.TryRemove<long>(PSBT2Constants.PSBT_OUT_AMOUNT, out var vl))
			Value = new Money(vl);
		else
			throw new FormatException("PSBT v2 must contain PSBT_OUT_AMOUNT");
		if (map.TryRemove<byte[]>(PSBT2Constants.PSBT_OUT_SCRIPT, out var script))
			ScriptPubKey = Script.FromBytesUnsafe(script);
		else
			throw new FormatException("PSBT v2 must contain PSBT_OUT_SCRIPT");
	}
	public override Script ScriptPubKey { get; set; }
	public override Money Value { get; set; }

	internal static void FillMap(Map map, TxOut txout)
	{
		map.Add([PSBT2Constants.PSBT_OUT_AMOUNT], txout.Value.Satoshi);
		map.Add([PSBT2Constants.PSBT_OUT_SCRIPT], txout.ScriptPubKey.ToBytes());
	}

	internal override void FillMap(Map map)
	{
		base.FillMap(map);
		map.Add([PSBT2Constants.PSBT_OUT_AMOUNT], Value.Satoshi);
		map.Add([PSBT2Constants.PSBT_OUT_SCRIPT], ScriptPubKey.ToBytes());
	}
	public override Coin GetCoin()
	{
		var outpoint = new OutPoint(Parent.GetGlobalTransaction(true).GetHash(), this.Index);
		return new Coin(outpoint, GetTxOut());
	}

	internal TxOut GetTxOut()
	{
		var txOut = Parent.Network.Consensus.ConsensusFactory.CreateTxOut();
		txOut.Value = Value;
		txOut.ScriptPubKey = ScriptPubKey;
		return txOut;
	}
}
