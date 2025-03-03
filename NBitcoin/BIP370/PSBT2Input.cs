#nullable enable
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NBitcoin.BIP370;

public class PSBT2Input : PSBTInput
{
	internal static void FillMap(Map m, TxIn input)
	{
		if (input.Sequence != NBitcoin.Sequence.Final)
			m.Add([PSBT2Constants.PSBT_IN_SEQUENCE], input.Sequence.Value);
		m.Add([PSBT2Constants.PSBT_IN_OUTPUT_INDEX], input.PrevOut.N);
		m.Add([PSBT2Constants.PSBT_IN_PREVIOUS_TXID], input.PrevOut.Hash.ToBytes());
	}

	public override void UpdateFrom(PSBTInput other)
	{
		if (other is PSBT2Input o)
		{
			if (o.Sequence != Sequence.Final)
				Sequence = o.Sequence;
			if (o.LockTime is { } lockTime)
				LockTime = lockTime;
			if (o.LockTimeHeight is { } lockTimeHeight)
				LockTimeHeight = lockTimeHeight;
		}
		base.UpdateFrom(other);
	}

	DateTimeOffset? _LockTime;
	public DateTimeOffset? LockTime
	{
		get
		{
			return _LockTime;
		}
		set
		{
			if (value is { } d && !new LockTime(d).IsTimeLock)
				throw new ArgumentOutOfRangeException("PSBT v2 input locktime must be a time lock", nameof(value));
			_LockTime = value;
		}
	}
	int? _LockTimeHeight;
	public int? LockTimeHeight
	{
		get
		{
			return _LockTimeHeight;
		}
		set
		{
			if (value is { } d && !new LockTime(d).IsHeightLock)
				throw new ArgumentOutOfRangeException("PSBT v2 input locktime must be a height lock", nameof(value));
			_LockTimeHeight = value;
		}
	}

	/// <summary>
	/// Convert <see cref="LockTime"/> or <see cref="LockTimeHeight"/> to a locktime.
	/// </summary>
	public LockTime? UnifiedTimeLock
	{
		get =>
			this switch
			{
				{ LockTimeHeight: { } v } => new NBitcoin.LockTime(v),
				{ LockTimeHeight: null, LockTime: { } v } => new NBitcoin.LockTime(v),
				_ => null
			};
		set
		{
			if (value is null)
			{
				LockTime = null;
				LockTimeHeight = null;
			}
			else if (value.Value.IsTimeLock)
			{
				LockTime = value.Value.Date;
				LockTimeHeight = null;
			}
			else if (value.Value.IsHeightLock)
			{
				LockTime = null;
				LockTimeHeight = value.Value.Height;
			}
		}
	}

	internal PSBT2Input(OutPoint prevOut, PSBT parent, uint inputIndex) : base(new Map(), parent, inputIndex)
	{
		PrevOut = prevOut;
	}
	internal PSBT2Input(Map map, PSBT parent, uint inputIndex) : base(map, parent, inputIndex)
	{
		if (!map.TryRemove<byte[]>(PSBT2Constants.PSBT_IN_PREVIOUS_TXID, out var txidBytes) || txidBytes.Length != 32)
			throw new FormatException("PSBT v2 must contain PSBT_IN_PREVIOUS_TXID");

		if (!map.TryRemove<uint>(PSBT2Constants.PSBT_IN_OUTPUT_INDEX, out var index))
			throw new FormatException("PSBT v2 must contain PSBT_IN_OUTPUT_INDEX");

		this.PrevOut = new OutPoint(new uint256(txidBytes), index);

		if (map.TryRemove<uint>(PSBT2Constants.PSBT_IN_SEQUENCE, out var s))
			Sequence = new Sequence(s);

		if (map.TryRemove<uint>(PSBT2Constants.PSBT_IN_REQUIRED_TIME_LOCKTIME, out var timeLockTimeV))
		{
			var locktime = new LockTime(timeLockTimeV);
			if (!locktime.IsTimeLock)
				throw new FormatException("PSBT v2 input locktime must be a time lock");
			LockTime = locktime.Date;
		}

		if (map.TryRemove<uint>(PSBT2Constants.PSBT_IN_REQUIRED_HEIGHT_LOCKTIME, out var locktimeV))
		{
			var locktime = new LockTime(locktimeV);
			if (!locktime.IsHeightLock)
				throw new FormatException("PSBT v2 input locktime must be a height lock");
			LockTimeHeight = locktime.Height;
		}
	}

	public override OutPoint PrevOut { get; }

	protected override void WriteCore(JsonTextWriter jsonWriter)
	{
		jsonWriter.WritePropertyValue("outpoint", PrevOut.ToString());
		jsonWriter.WritePropertyValue("sequence", Sequence.ToString());
		if (LockTime is { } lockTime)
		{
			jsonWriter.WritePropertyValue("locktime", lockTime);
		}
		if (LockTimeHeight is { } lockTimeHeight)
		{
			jsonWriter.WritePropertyValue("locktime_height", lockTimeHeight);
		}
	}

	internal override void FillMap(Map map)
	{
		base.FillMap(map);
		map.Add([PSBT2Constants.PSBT_IN_PREVIOUS_TXID], PrevOut.Hash.ToBytes());
		map.Add([PSBT2Constants.PSBT_IN_OUTPUT_INDEX], PrevOut.N);

		if (Sequence != Sequence.Final)
			map.Add([PSBT2Constants.PSBT_IN_SEQUENCE], Sequence.Value);

		// key
		if (LockTime is not null)
			map.Add([PSBT2Constants.PSBT_IN_REQUIRED_TIME_LOCKTIME], new LockTime(LockTime.Value).ToBytes());
		
		if (LockTimeHeight is not null)
		{
			var h = new LockTime(LockTimeHeight.Value);
			if (!h.IsHeightLock)
				throw new FormatException("LockTimeHeight is out of bounds");
			map.Add([PSBT2Constants.PSBT_IN_REQUIRED_HEIGHT_LOCKTIME], h.ToBytes());
		}
	}

	public override Sequence Sequence { get; set; } = Sequence.Final;

	internal TxIn CreateTxIn()
	{
		var txin = Parent.Network.Consensus.ConsensusFactory.CreateTxIn();
		txin.Sequence = Sequence;
		txin.PrevOut = PrevOut;
		return txin;
	}
}
