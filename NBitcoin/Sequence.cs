using System;
using System.Text;

namespace NBitcoin
{
	public enum SequenceLockType
	{
		Time,
		Height
	}
	public struct Sequence
	{
		public static Sequence Final
		{
			get
			{
				return new Sequence(SEQUENCE_FINAL);
			}
		}

		public static Sequence OptInRBF
		{
			get
			{
				return new Sequence(MAX_BIP125_RBF_SEQUENCE);
			}
		}

		public static Sequence FeeSnipping
		{
			get
			{
				return new Sequence(SEQUENCE_FINAL - 1);
			}
		}

		/// <summary>
		/// If this flag set, CTxIn::nSequence is NOT interpreted as a
		/// relative lock-time. 
		/// </summary>
		public const uint SEQUENCE_LOCKTIME_DISABLE_FLAG = (1U << 31);

		/// <summary>
		/// If CTxIn::nSequence encodes a relative lock-time and this flag
		/// is set, the relative lock-time has units of 512 seconds,
		/// otherwise it specifies blocks with a granularity of 1. 
		/// </summary>
		public const uint SEQUENCE_LOCKTIME_TYPE_FLAG = (1U << 22);

		/// <summary>
		/// If CTxIn::nSequence encodes a relative lock-time, this mask is
		/// applied to extract that lock-time from the sequence field.
		/// </summary>
		public const uint SEQUENCE_LOCKTIME_MASK = 0x0000ffff;

		/// <summary>
		/// Setting nSequence to this value for every input in a transaction
		/// disables nLockTime. */
		/// </summary>
		/// <remarks>
		/// If this flag set, CTxIn::nSequence is NOT interpreted as a
		/// relative lock-time. Setting the most significant bit of a
		/// sequence number disabled relative lock-time.
		/// </remarks>
		public const uint SEQUENCE_FINAL = 0xffffffff;

		/// <summary>
		/// Setting nSequence to this value on any input in a transaction
		/// to signal the transaction is replaceable. */
		/// </summary>
		public const uint MAX_BIP125_RBF_SEQUENCE = SEQUENCE_FINAL - 2;

		/// <summary>
		/// In order to use the same number of bits to encode roughly the
		/// same wall-clock duration, and because blocks are naturally
		/// limited to occur every 600s on average, the minimum granularity
		/// for time-based relative lock-time is fixed at 512 seconds.
		/// Converting from CTxIn::nSequence to seconds is performed by
		/// multiplying by 512 = 2^9, or equivalently shifting up by
		/// 9 bits. 
		/// </summary>
		internal const int SEQUENCE_LOCKTIME_GRANULARITY = 9;


		uint _ValueInv;
		public uint Value
		{
			get
			{
				return 0xFFFFFFFF - _ValueInv;
			}
		}
		public Sequence(uint value)
		{
			_ValueInv = 0xFFFFFFFF - value;
		}

		public Sequence(int lockHeight)
		{
			if (lockHeight > 0xFFFF || lockHeight < 0)
				throw new ArgumentOutOfRangeException("Relative lock height must be positive and lower or equals to 0xFFFF (65535 blocks)");
			_ValueInv = 0xFFFFFFFF - (uint)lockHeight;
		}
		public Sequence(TimeSpan period)
		{
			if (period.TotalSeconds > (0xFFFF * 512) || period.TotalSeconds < 0)
				throw new ArgumentOutOfRangeException("Relative lock time must be positive and lower or equals to " + (0xFFFF * 512) + " seconds (approx 388 days)");
			var value = (uint)(period.TotalSeconds / (1 << Sequence.SEQUENCE_LOCKTIME_GRANULARITY));
			value |= SEQUENCE_LOCKTIME_TYPE_FLAG;
			_ValueInv = 0xFFFFFFFF - (uint)value;
		}

		public bool IsRelativeLock
		{
			get
			{
				return (Value & SEQUENCE_LOCKTIME_DISABLE_FLAG) == 0;
			}
		}

		public bool IsRBF
		{
			get
			{
				return Value <= MAX_BIP125_RBF_SEQUENCE;
			}
		}

		public SequenceLockType LockType
		{
			get
			{
				AssertRelativeLock();
				return (Value & SEQUENCE_LOCKTIME_TYPE_FLAG) != 0 ? SequenceLockType.Time : SequenceLockType.Height;
			}
		}

		public static implicit operator uint(Sequence a)
		{
			return a.Value;
		}
		public static implicit operator Sequence(uint a)
		{
			return new Sequence(a);
		}

		private void AssertRelativeLock()
		{
			if (!IsRelativeLock)
				throw new InvalidOperationException("This sequence is not a relative lock");
		}

		public override string ToString()
		{
			if (IsRelativeLock)
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("Relative lock (" + LockType + "): ");
				if (LockType == SequenceLockType.Height)
					builder.Append(LockHeight + " blocks");
				else
					builder.Append(LockPeriod);
				return builder.ToString();
			}
			return Value.ToString();
		}

		public int LockHeight
		{
			get
			{
				AssertRelativeLock();
				if (LockType != SequenceLockType.Height)
					throw new InvalidOperationException("This sequence is a time based relative lock");
				return (int)(Value & SEQUENCE_LOCKTIME_MASK);
			}
		}
		public TimeSpan LockPeriod
		{
			get
			{
				AssertRelativeLock();
				if (LockType != SequenceLockType.Time)
					throw new InvalidOperationException("This sequence is a height based relative lock");
				return TimeSpan.FromSeconds((int)(Value & SEQUENCE_LOCKTIME_MASK) * (1 << Sequence.SEQUENCE_LOCKTIME_GRANULARITY));
			}
		}
	}
}
