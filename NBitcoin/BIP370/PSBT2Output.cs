using System;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;

namespace NBitcoin.BIP370;

public class PSBT2Output : PSBTOutput
{
	private const long MaxMoney = 21_000_000L * Money.COIN;

	internal PSBT2Output(Money value, Script scriptPubKey, PSBT parent, uint index) : base(new Map(), parent, index)
	{
		this.Value = value;
		this.ScriptPubKey = scriptPubKey;
	}
	internal PSBT2Output(Map map, PSBT parent, uint index) : base(map, parent, index)
	{
		if (map.TryRemove<long>(PSBT2Constants.PSBT_OUT_AMOUNT, out var vl))
		{
			if (vl < 0 || vl > MaxMoney)
				throw new FormatException("PSBT v2 output amount is out of range");
			Value = new Money(vl);
		}
		else
			throw new FormatException("PSBT v2 must contain PSBT_OUT_AMOUNT");
		if (map.TryRemove<byte[]>(PSBT2Constants.PSBT_OUT_SCRIPT, out var script))
			ScriptPubKey = Script.FromBytesUnsafe(script);
		else
			throw new FormatException("PSBT v2 must contain PSBT_OUT_SCRIPT");
	}
	public override Script ScriptPubKey { get; set; }
	private Money _Value;
	public override Money Value
	{
		get => _Value;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			if (value.Satoshi < 0 || value.Satoshi > MaxMoney)
				throw new ArgumentOutOfRangeException(nameof(value), "PSBT v2 output amount is out of range");
			_Value = value;
		}
	}

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

	public override TxOut GetTxOut()
	{
		var txOut = Parent.Network.Consensus.ConsensusFactory.CreateTxOut();
		txOut.Value = Value;
		txOut.ScriptPubKey = ScriptPubKey;
		return txOut;
	}
}
