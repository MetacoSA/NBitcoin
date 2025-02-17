using System;
using System.Collections.Generic;

namespace NBitcoin.BIP370;

public class PSBT2Input : PSBTInput
{
	public LockTime LockTime { get; set; } = LockTime.Zero;
	public LockTime LockTimeHeight { get; set; } = LockTime.Zero;

	public bool IsSatisfiedWithHeightBasedLockTime()
	{
		return LockTimeHeight != LockTime.Zero ||
		       (LockTime != 0 && LockTimeHeight != 0) ||
		       (LockTime == 0 && LockTimeHeight == 0);
	}

	public bool RequiresTimeBasedLockTime()
	{
	    return LockTime != LockTime.Zero && LockTimeHeight == LockTime.Zero;
	}

	public bool RequiresHeightBasedLockTime()
	{
	    return LockTime == LockTime.Zero && LockTimeHeight != LockTime.Zero;
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
			if(locktime != 0 && !locktime.IsTimeLock)
			{
				throw new FormatException("PSBT v2 input locktime must be a time lock");
			}
			LockTime = locktime;
		}

		if (map.TryRemove([PSBT2Constants.PSBT_IN_REQUIRED_HEIGHT_LOCKTIME], out var locktimeBytes))
		{
			var locktime = new LockTime();
			new BitcoinStream(locktimeBytes).ReadWrite(ref locktime);
			if(locktime != 0 && !locktime.IsHeightLock)
			{
				throw new FormatException("PSBT v2 input locktime must be a height lock");
			}
			LockTimeHeight = locktime;
		}
		base.Load(map);
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
		if(LockTime != LockTime.Zero)
		{
			stream.ReadWriteAsVarInt(ref defaultKeyLen);
			key = PSBT2Constants.PSBT_IN_REQUIRED_TIME_LOCKTIME;
			stream.ReadWrite(ref key);

			// value
			data = LockTime.ToBytes();
			stream.ReadWriteAsVarString(ref data);
		}
		if(LockTimeHeight != LockTime.Zero)
		{
			stream.ReadWriteAsVarInt(ref defaultKeyLen);
			key = PSBT2Constants.PSBT_IN_REQUIRED_HEIGHT_LOCKTIME;
			stream.ReadWrite(ref key);

			// value
			data = LockTimeHeight.ToBytes();
			stream.ReadWriteAsVarString(ref data);
		}
		base.Serialize(stream);

	}
}
