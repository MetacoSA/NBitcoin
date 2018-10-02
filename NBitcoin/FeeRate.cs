﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class FeeRate : IEquatable<FeeRate>, IComparable<FeeRate>
	{
		private readonly Money _FeePerK;
		/// <summary>
		/// Fee per KB
		/// </summary>
		public Money FeePerK
		{
			get
			{
				return _FeePerK;
			}
		}

		/// <summary>
		/// Satoshi per Byte
		/// </summary>
		public decimal SatoshiPerByte
		{
			get
			{
				return (decimal)_FeePerK.Satoshi/1000;
			}
		}

		readonly static FeeRate _Zero = new FeeRate(Money.Zero);
		public static FeeRate Zero
		{
			get
			{
				return _Zero;
			}
		}

		public FeeRate(Money feePerK)
		{
			if(feePerK == null)
				throw new ArgumentNullException(nameof(feePerK));
			if(feePerK.Satoshi < 0)
				throw new ArgumentOutOfRangeException("feePerK");
			_FeePerK = feePerK;
		}

		public FeeRate(Money feePaid, int size)
		{
			if(feePaid == null)
				throw new ArgumentNullException(nameof(feePaid));
			if(feePaid.Satoshi < 0)
				throw new ArgumentOutOfRangeException("feePaid");
			if(size > 0)
				_FeePerK = feePaid * 1000 / size;
			else
				_FeePerK = 0;
		}

		public FeeRate(decimal satoshiPerByte)
		{
			if(satoshiPerByte < 0)
				throw new ArgumentOutOfRangeException("satoshiPerByte");
			_FeePerK = Money.Satoshis(satoshiPerByte * 1000);
		}

		/// <summary>
		/// Get fee for the size
		/// </summary>
		/// <param name="virtualSize">Size in bytes</param>
		/// <returns></returns>
		public Money GetFee(int virtualSize)
		{
			Money nFee = _FeePerK.Satoshi * virtualSize / 1000;
			if(nFee == 0 && _FeePerK.Satoshi > 0)
				nFee = _FeePerK.Satoshi;
			return nFee;
		}
		public Money GetFee(Transaction tx)
		{
			return GetFee(tx.GetVirtualSize());
		}

		public override bool Equals(object obj)
		{
			if(Object.ReferenceEquals(this, obj))
				return true;
			if(((object)this == null) || (obj == null))
				return false;
			var left = this;
			var right = obj as FeeRate;
			if(right == null)
				return false;
			return left._FeePerK == right._FeePerK;
		}

		public override string ToString()
		{
			int divisibility = 0;
			var value = SatoshiPerByte;
			while(true)
			{
				var rounded = Math.Round(value, divisibility, MidpointRounding.AwayFromZero);
				if((Math.Abs(rounded - value) / value) < 0.001m)
				{
					value = rounded;
					break;
				}
				divisibility++;
			}
			return String.Format("{0} Sat/B", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}

		#region IEquatable<FeeRate> Members

		public bool Equals(FeeRate other)
		{
			return other != null && _FeePerK.Equals(other._FeePerK);
		}

		public int CompareTo(FeeRate other)
		{
			return other == null 
				? 1 
				: _FeePerK.CompareTo(other._FeePerK);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (obj == null)
				return 1;
			var m = obj as FeeRate;
			if (m != null)
				return _FeePerK.CompareTo(m._FeePerK);
#if !NETCORE
			return _FeePerK.CompareTo(obj);
#else
			return _FeePerK.CompareTo((long)obj);
#endif
		}

		#endregion

		public static bool operator <(FeeRate left, FeeRate right)
		{
			if (left == null)
				throw new ArgumentNullException(nameof(left));
			if (right == null)
				throw new ArgumentNullException(nameof(right));
			return left._FeePerK < right._FeePerK;
		}
		public static bool operator >(FeeRate left, FeeRate right)
		{
			if (left == null)
				throw new ArgumentNullException(nameof(left));
			if (right == null)
				throw new ArgumentNullException(nameof(right));
			return left._FeePerK > right._FeePerK;
		}
		public static bool operator <=(FeeRate left, FeeRate right)
		{
			if (left == null)
				throw new ArgumentNullException(nameof(left));
			if (right == null)
				throw new ArgumentNullException(nameof(right));
			return left._FeePerK <= right._FeePerK;
		}
		public static bool operator >=(FeeRate left, FeeRate right)
		{
			if (left == null)
				throw new ArgumentNullException(nameof(left));
			if (right == null)
				throw new ArgumentNullException(nameof(right));
			return left._FeePerK >= right._FeePerK;
		}

		public static bool operator ==(FeeRate left, FeeRate right)
		{
			if (Object.ReferenceEquals(left, right))
				return true;
			if (((object)left == null) || ((object)right == null))
				return false;
			return left._FeePerK == right._FeePerK;
		}

		public static bool operator !=(FeeRate left, FeeRate right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return _FeePerK.GetHashCode();
		}

		public static FeeRate Min(FeeRate left, FeeRate right)
		{
			if (left == null)
				throw new ArgumentNullException(nameof(left));
			if (right == null)
				throw new ArgumentNullException(nameof(right));
			return left <= right 
				? left 
				: right;
		}

		public static FeeRate Max(FeeRate left, FeeRate right)
		{
			if (left == null)
				throw new ArgumentNullException(nameof(left));
			if (right == null)
				throw new ArgumentNullException(nameof(right));
			return left >= right
				? left
				: right;
		}
	}
}
