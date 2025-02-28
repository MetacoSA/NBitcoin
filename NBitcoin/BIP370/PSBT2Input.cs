#nullable enable
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NBitcoin.BIP370;

public class PSBT2Input : PSBTInput
{
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

	internal PSBT2Input(SortedDictionary<byte[], byte[]> map, PSBT parent, uint index, TxIn input) : base(map, parent, index, input)
	{


	}

	protected override void Load(SortedDictionary<byte[], byte[]> map)
	{
		if (map.TryRemove([PSBT2Constants.PSBT_IN_REQUIRED_TIME_LOCKTIME], out var timeLockTimeBytes))
		{
			var locktime = new LockTime();
			new BitcoinStream(timeLockTimeBytes).ReadWrite(ref locktime);
			if (!locktime.IsTimeLock)
				throw new FormatException("PSBT v2 input locktime must be a time lock");
			LockTime = locktime.Date;
		}

		if (map.TryRemove([PSBT2Constants.PSBT_IN_REQUIRED_HEIGHT_LOCKTIME], out var locktimeBytes))
		{
			var locktime = new LockTime();
			new BitcoinStream(locktimeBytes).ReadWrite(ref locktime);
			if (!locktime.IsHeightLock)
				throw new FormatException("PSBT v2 input locktime must be a height lock");
			LockTimeHeight = locktime.Height;
		}
		base.Load(map);
	}

	protected override void WriteCore(JsonTextWriter jsonWriter)
	{
		jsonWriter.WritePropertyValue("outpoint", this.TxIn.PrevOut.ToString());
		jsonWriter.WritePropertyValue("sequence", this.TxIn.Sequence);
		if (LockTime is { } lockTime)
		{
			jsonWriter.WritePropertyValue("locktime", lockTime);
		}
		if (LockTimeHeight is { } lockTimeHeight)
		{
			jsonWriter.WritePropertyValue("locktime_height", lockTimeHeight);
		}
	}

	public override void Serialize(BitcoinStream stream)
	{
		// key
		stream.ReadWriteAsVarInt(ref defaultKeyLen);
		var key = PSBT2Constants.PSBT_IN_PREVIOUS_TXID;
		stream.ReadWrite(ref key);

		// value
		var data = TxIn.PrevOut.Hash.ToBytes();
		stream.ReadWriteAsVarString(ref data);

		// key
		stream.ReadWriteAsVarInt(ref defaultKeyLen);
		key = PSBT2Constants.PSBT_IN_OUTPUT_INDEX;
		stream.ReadWrite(ref key);

		// value
		data = BitConverter.GetBytes(TxIn.PrevOut.N);
		stream.ReadWriteAsVarString(ref data);

		// key
		stream.ReadWriteAsVarInt(ref defaultKeyLen);
		key = PSBT2Constants.PSBT_IN_SEQUENCE;
		stream.ReadWrite(ref key);

		// value
		data = BitConverter.GetBytes(TxIn.Sequence);
		stream.ReadWriteAsVarString(ref data);

		// key
		if (LockTime is not null)
		{
			stream.ReadWriteAsVarInt(ref defaultKeyLen);
			key = PSBT2Constants.PSBT_IN_REQUIRED_TIME_LOCKTIME;
			stream.ReadWrite(ref key);

			// value
			data = new LockTime(LockTime.Value).ToBytes();
			stream.ReadWriteAsVarString(ref data);
		}
		if (LockTimeHeight is not null)
		{
			var h = new LockTime(LockTimeHeight.Value);
			if (!h.IsHeightLock)
				throw new FormatException("LockTimeHeight is out of bounds");
			stream.ReadWriteAsVarInt(ref defaultKeyLen);
			key = PSBT2Constants.PSBT_IN_REQUIRED_HEIGHT_LOCKTIME;
			stream.ReadWrite(ref key);

			// value
			data = h.ToBytes();
			stream.ReadWriteAsVarString(ref data);
		}
		base.Serialize(stream);

	}

	protected override void SetSequenceCore(Sequence sequence)
	{
		TxIn.Sequence = sequence;
	}
}
