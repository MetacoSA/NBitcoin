using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public static class MoneyExtensions
	{
		public static Money Sum(this IEnumerable<Money> moneys)
		{
			BigInteger result = BigInteger.Zero;
			foreach(var money in moneys)
			{
				result += money.Satoshi;
			}
			return new Money(result);
		}
	}

	public enum MoneyUnit : int
	{
		BTC = 100000000,
		MilliBTC = 100000,
		Bit = 100,
		Satoshi = 1
	}
	public class Money : IComparable, IComparable<Money>, IEquatable<Money>
	{
		public const long COIN = 100000000;
		public const long CENT = 1000000;
		public const long NANO = CENT / 100;

		public static bool TryParse(string bitcoin, out Money nRet)
		{
			nRet = new Money(0);
            if (string.IsNullOrEmpty(bitcoin))
				return false;

			string strWhole = "";
			long nUnits = 0;

			int i = 0;

			while(DataEncoder.IsSpace(bitcoin[i]))
			{
				if(i >= bitcoin.Length)
					return false;
				i++;
			}
			if(i >= bitcoin.Length)
				return false;
			bool minus = bitcoin[i] == '-';
			if(minus || bitcoin[i] == '+')
				i++;

			for( ; i < bitcoin.Length ; i++)
			{
				if(bitcoin[i] == '.')
				{
					i++;
					if(i >= bitcoin.Length)
						break;
					long nMult = CENT * 10;
					while(isdigit(bitcoin[i]) && (nMult > 0))
					{
						nUnits += nMult * (bitcoin[i] - '0');
						i++;
						if(i >= bitcoin.Length)
							break;
						nMult /= 10;
					}
					break;
				}
				if(DataEncoder.IsSpace(bitcoin[i]))
					break;
				if(!isdigit(bitcoin[i]))
					return false;
				strWhole += bitcoin[i];
			}
			for( ; i < bitcoin.Length ; i++)
				if(!DataEncoder.IsSpace(bitcoin[i]))
					return false;
			if(strWhole.Length > 10) // guard against 63 bit overflow
				return false;
			if(nUnits < 0 || nUnits > COIN)
				return false;

			var nWhole = BigInteger.Parse(strWhole);
			var nValue = nWhole * COIN + nUnits;

			nRet = new Money(minus ? -nValue : nValue);
			return true;
		}
		public static Money Parse(string bitcoin)
		{
			Money result;
			if(TryParse(bitcoin, out result))
			{
				return result;
			}
			throw new FormatException("Impossible to parse the string in a bitcoin amount");
		}
		long _Satoshis;
		public long Satoshi
		{
			get
			{
				return _Satoshis;
			}
		}

		/// <summary>
		/// Get absolute value of the instance
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public Money Abs()
		{
			var a = this;
			if(a < Money.Zero)
				a = -a;
			return a;
		}

#if !NOBIGINT
		public Money(BigInteger satoshis)
#else
		internal Money(BigInteger satoshis)
#endif
		{
			_Satoshis = (long)satoshis;
		}

		public Money(int satoshis)
		{
			_Satoshis = satoshis;
		}

        public Money(uint satoshis)
        {
			_Satoshis = satoshis;
        }

        public Money(long satoshis)
		{
			_Satoshis = satoshis;
		}
		public Money(ulong satoshis)
		{
			_Satoshis = (long)satoshis;
		}

		public Money(decimal amount, MoneyUnit unit)
		{
			_Satoshis = (long)(amount * (int)unit);
		}

		public static Money FromUnit(decimal amount, MoneyUnit unit)
		{
			return new Money(amount, unit);
		}

		public decimal ToUnit(MoneyUnit unit)
		{
			return (decimal)Satoshi / (int)unit;
		}

        public static Money Coins(decimal coins)
        {
            return new Money((long)(coins * COIN));
        }

        public static Money Bits(decimal bits)
        {
            return new Money((long)(bits * CENT));
        }

        public static Money Cents(decimal cents)
        {
            return new Money((long)(cents * CENT));
        }

        public static Money Satoshis(decimal sats)
        {
            return new Money((long)(sats));
        }

        public static Money Satoshis(ulong sats)
        {
            return new Money(sats);
        }

        public static Money Satoshis(long sats)
        {
            return new Money(sats);
        }

		#region IEquatable<Money> Members

		public bool Equals(Money other)
		{
			return _Satoshis.Equals(other._Satoshis);
		}

		public int CompareTo(Money other)
		{
			return _Satoshis.CompareTo(other._Satoshis);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			Money m = obj as Money;
			if(m != null)
				return _Satoshis.CompareTo(m._Satoshis);
#if !PORTABLE
			return _Satoshis.CompareTo(obj);
#else
			return _Satoshis.CompareTo((long)obj);
#endif
		}

		#endregion

		public static Money operator -(Money left, Money right)
		{
			return new Money(left._Satoshis - right._Satoshis);
		}
		public static Money operator -(Money left)
		{
			return new Money(-left._Satoshis);
		}
		public static Money operator +(Money left, Money right)
		{
			return new Money(left._Satoshis + right._Satoshis);
		}
		public static Money operator *(decimal left, Money right)
		{
			return Money.Satoshis(left * right._Satoshis);
		}
		public static Money operator /(decimal left, Money right)
		{
			return Money.Satoshis(left / right._Satoshis);
		}
		public static Money operator *(double left, Money right)
		{
			return Money.Satoshis((decimal)(left * right._Satoshis));
		}
		public static Money operator /(double left, Money right)
		{
			return Money.Satoshis((decimal)(left / right._Satoshis));
		}
		public static Money operator *(int left, Money right)
		{
			return Money.Satoshis(left * right._Satoshis);
		}
		public static Money operator /(int left, Money right)
		{
			return Money.Satoshis(left / right._Satoshis);
		}
		public static Money operator *(long left, Money right)
		{
			return Money.Satoshis(left * right._Satoshis);
		}
		public static Money operator /(long left, Money right)
		{
			return Money.Satoshis(left / right._Satoshis);
		}

		public static bool operator <(Money left, Money right)
		{
			return left._Satoshis < right._Satoshis;
		}
		public static bool operator >(Money left, Money right)
		{
			return left._Satoshis > right._Satoshis;
		}
		public static bool operator <=(Money left, Money right)
		{
			return left._Satoshis <= right._Satoshis;
		}
		public static bool operator >=(Money left, Money right)
		{
			return left._Satoshis >= right._Satoshis;
		}

		public static Money operator ++(Money left)
		{
			return new Money(left._Satoshis++);
		}
		public static Money operator --(Money left)
		{
			return new Money(left._Satoshis--);
		}
		public static implicit operator Money(long value)
		{
			return new Money(value);
		}
		public static implicit operator Money(int value)
		{
			return new Money(value);
		}

        public static implicit operator Money(uint value)
        {
            return new Money(value);
        }

        public static implicit operator Money(ulong value)
		{
			return new Money(value);
		}

		public static implicit operator long(Money value)
		{
			return (long)value.Satoshi;
		}

		public static implicit operator ulong(Money value)
		{
			return (ulong)value.Satoshi;
		}

		public static implicit operator Money(string value)
		{
			return Money.Parse(value);
        }

		public override bool Equals(object obj)
		{
			Money item = obj as Money;
			if(item == null)
				return false;
			return _Satoshis.Equals(item._Satoshis);
		}
		public static bool operator ==(Money a, Money b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._Satoshis == b._Satoshis;
		}

		public static bool operator !=(Money a, Money b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _Satoshis.GetHashCode();
		}

		

		public override string ToString()
		{
			return ToString(false, true);
		}
		public string ToString(bool fplus, bool trimExcessZero = true)
		{
			// Note: not using straight sprintf here because we do NOT want
			// localized number formatting.
			var n_abs = (_Satoshis > 0 ? _Satoshis : -_Satoshis);
			var quotient = n_abs / COIN;
			var remainder = n_abs % COIN;

			string str = String.Format("{0}.{1:D8}", quotient, remainder);

			if(trimExcessZero)
			{
				// Right-trim excess zeros before the decimal point:
				int nTrim = 0;
				for(int i = str.Length - 1 ; (str[i] == '0' && isdigit(str[i - 2])) ; --i)
					++nTrim;
				if(nTrim != 0)
					str = str.Remove(str.Length - nTrim, nTrim);
			}
			if(_Satoshis < 0)
				str = "-" + str;
			else if(fplus && _Satoshis > 0)
				str = "+" + str;
			return str;
		}
		public static bool isdigit(char c)
		{
			return c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9';
		}



		static Money _Zero = new Money(0);
		public static Money Zero
		{
			get
			{
				return _Zero;
			}
		}

		static Money _Dust = new Money(600);
		public static Money Dust
		{
			get
			{
				return _Dust;
			}
		}

		/// <summary>
		/// Tell if amount is almost equal to this instance
		/// </summary>
		/// <param name="amount"></param>
		/// <param name="dust">more or less dust (default : 600 satoshi)</param>
		/// <returns>true if equals, else false</returns>
		public bool Almost(Money amount, Money dust = null)
		{
			if(dust == null)
				dust = Dust;
			return (amount - this).Abs() <= dust;
		}
		
		/// <summary>
		/// Tell if amount is almost equal to this instance
		/// </summary>
		/// <param name="amount"></param>
		/// <param name="margin">error margin (between 0 and 1)</param>
		/// <returns>true if equals, else false</returns>
		public bool Almost(Money amount, decimal margin)
		{
			if(margin < 0.0m || margin > 1.0m)
				throw new ArgumentOutOfRangeException("margin", "margin should be between 0 and 1");
			 var dust = Money.Satoshis((decimal)amount.Satoshi * margin);
			 return Almost(amount, dust);
		}

		public static Money Min(Money a, Money b)
		{
			if(a == null)
				throw new ArgumentNullException("a");
			if(b == null)
				throw new ArgumentNullException("b");
			if(a <= b)
				return a;
			return b;
		}
	}
}
