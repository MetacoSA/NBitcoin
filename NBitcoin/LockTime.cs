﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public struct LockTime : IBitcoinSerializable
	{
		const uint LOCKTIME_THRESHOLD = 500000000;
		uint _value;


		public LockTime(DateTimeOffset dateTime)
		{
			_value = Utils.DateTimeToUnixTime(dateTime);
		}
		public LockTime(int valueOrHeight)
		{
			_value = (uint)valueOrHeight;
		}
		public LockTime(uint valueOrHeight)
		{
			_value = valueOrHeight;
		}


		public DateTimeOffset Date
		{
			get
			{
				if(!IsTimeLock)
					throw new InvalidOperationException("This is not a time based lock");
				return Utils.UnixTimeToDateTime(_value);
			}
		}

		public int Height
		{
			get
			{
				if(!IsHeightLock)
					throw new InvalidOperationException("This is not a height based lock");
				return (int)_value;
			}
		}

		public uint Value
		{
			get
			{
				return _value;
			}
		}


		public bool IsHeightLock
		{
			get
			{
				return _value < LOCKTIME_THRESHOLD; // Tue Nov  5 00:53:20 1985 UTC
			}
		}

		public bool IsTimeLock
		{
			get
			{
				return !IsHeightLock;
			}
		}


		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _value);
		}

		#endregion

		public override string ToString()
		{
			return IsHeightLock ? "Height : " + Height : "Date : " + Date;
		}

		public static implicit operator LockTime(int valueOrHeight)
		{
			return new LockTime(valueOrHeight);
		}

		public static implicit operator LockTime(DateTimeOffset date)
		{
			return new LockTime(date);
		}

		public static implicit operator LockTime(uint valueOrHeight)
		{
			return new LockTime(valueOrHeight);
		}

		public static implicit operator DateTimeOffset(LockTime lockTime)
		{
			return lockTime.Date;
		}
		public static implicit operator int(LockTime lockTime)
		{
			return (int)lockTime._value;
		}

		public static implicit operator uint(LockTime lockTime)
		{
			return lockTime._value;
		}

		public override bool Equals(object obj)
		{
			LockTime item = obj is LockTime ? (LockTime)obj : default(LockTime);
			if(item == null)
				return false;
			return _value.Equals(item._value);
		}
		public static bool operator ==(LockTime a, LockTime b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._value == b._value;
		}

		public static bool operator !=(LockTime a, LockTime b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}
	}
}
