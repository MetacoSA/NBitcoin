﻿using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBitcoin.OpenAsset;

namespace NBitcoin
{
	public static class MoneyExtensions
	{
		public static Money Sum(this IEnumerable<Money> moneys)
		{
			if(moneys == null)
				throw new ArgumentNullException(nameof(moneys));
			long result = 0;
			foreach(var money in moneys)
			{
				result = checked(result + money.Satoshi);
			}
			return new Money(result);
		}

		public static IMoney Sum(this IEnumerable<IMoney> moneys, IMoney zero)
		{
			if(moneys == null)
				throw new ArgumentNullException(nameof(moneys));
			if(zero == null)
				throw new ArgumentNullException(nameof(zero));
			IMoney result = zero;
			foreach(var money in moneys)
			{
				result = result.Add(money);
			}
			return result;
		}


		public static AssetMoney Sum(this IEnumerable<AssetMoney> moneys, AssetId assetId)
		{
			if(moneys == null)
				throw new ArgumentNullException(nameof(moneys));
			if(assetId == null)
				throw new ArgumentNullException(nameof(assetId));
			long result = 0;
			AssetId id = null;
			foreach(var money in moneys)
			{
				result = checked(result + money.Quantity);
				if(id == null)
					id = money.Id;
				else if(id != money.Id)
					throw new ArgumentException("Impossible to add AssetMoney with different asset ids", "moneys");
			}
			if(id == null)
				return new AssetMoney(assetId);
			return new AssetMoney(id, result);
		}
	}

	public enum MoneyUnit : int
	{
		BTC = 100000000,
		MilliBTC = 100000,
		Bit = 100,
		Satoshi = 1
	}

	public interface IMoney : IComparable, IComparable<IMoney>, IEquatable<IMoney>
	{
		IMoney Add(IMoney money);
		IMoney Sub(IMoney money);
		IMoney Negate();
		bool IsCompatible(IMoney money);
		IEnumerable<IMoney> Split(int parts);
	}

	public class MoneyBag : IMoney, IEnumerable<IMoney>, IEquatable<MoneyBag>
	{
		private readonly List<IMoney> _bag = new List<IMoney>();

		public MoneyBag()
			: this(new List<MoneyBag>())
		{

		}
		public MoneyBag(MoneyBag money)
			: this(money._bag)
		{
		}

		public MoneyBag(params IMoney[] bag)
			: this((IEnumerable<IMoney>)bag)
		{
		}

		private MoneyBag(IEnumerable<IMoney> bag)
		{
			foreach(var money in bag)
			{
				AppendMoney(money);
			}
		}

		private void AppendMoney(MoneyBag money)
		{
			foreach(var m in money._bag)
			{
				AppendMoney(m);
			}
		}

		private void AppendMoney(IMoney money)
		{
			var moneyBag = money as MoneyBag;
			if(moneyBag != null)
			{
				AppendMoney(moneyBag);
				return;
			}

			var firstCompatible = _bag.FirstOrDefault(x => x.IsCompatible(money));
			if(firstCompatible == null)
			{
				_bag.Add(money);
			}
			else
			{
				_bag.Remove(firstCompatible);
				var zero = firstCompatible.Sub(firstCompatible);
				var total = firstCompatible.Add(money);
				if(!zero.Equals(total))
					_bag.Add(total);
			}
		}

		public int CompareTo(object obj)
		{
			throw new NotSupportedException("Comparisons are not possible for MoneyBag");
		}

		public int CompareTo(IMoney other)
		{
			throw new NotSupportedException("Comparisons are not possible for MoneyBag");
		}
		public bool Equals(MoneyBag other)
		{
			return Equals(other as IMoney);
		}
		public bool Equals(IMoney other)
		{
			if(other == null)
				return false;
			var m = new MoneyBag(other);
			return m._bag.SequenceEqual(_bag);
		}

		public static MoneyBag operator -(MoneyBag left, IMoney right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return (MoneyBag)((IMoney)left).Sub(right);
		}

		public static MoneyBag operator +(MoneyBag left, IMoney right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return (MoneyBag)((IMoney)left).Add(right);
		}

		IMoney IMoney.Add(IMoney money)
		{
			var m = new MoneyBag(_bag);
			m.AppendMoney(money);
			return m;
		}

		IMoney IMoney.Sub(IMoney money)
		{
			return ((IMoney)this).Add(money.Negate());
		}

		IMoney IMoney.Negate()
		{
			return new MoneyBag(_bag.Select(x => x.Negate()));
		}

		bool IMoney.IsCompatible(IMoney money)
		{
			return true;
		}

		/// <summary>
		/// Get the Money corresponding to the input assetId
		/// </summary>
		/// <param name="assetId">The asset id, if null, will assume bitcoin amount</param>
		/// <returns>Never returns null, eithers the AssetMoney or Money if assetId is null</returns>
		public IMoney GetAmount(AssetId assetId = null)
		{
			if(assetId == null)
				return this.OfType<Money>().FirstOrDefault() ?? Money.Zero;
			else
				return this.OfType<AssetMoney>().Where(a => a.Id == assetId).FirstOrDefault() ?? new AssetMoney(assetId, 0);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach(var money in _bag)
			{
				sb.AppendFormat("{0} ", money);
			}
			return sb.ToString();
		}

		public IEnumerator<IMoney> GetEnumerator()
		{
			return _bag.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _bag.GetEnumerator();
		}

		/// <summary>
		/// Split the MoneyBag in several one, without loss
		/// </summary>
		/// <param name="parts">The number of parts (must be more than 0)</param>
		/// <returns>The splitted money</returns>
		public IEnumerable<MoneyBag> Split(int parts)
		{
			if(parts <= 0)
				throw new ArgumentOutOfRangeException("Parts should be more than 0", "parts");
			List<List<IMoney>> splits = new List<List<IMoney>>();
			foreach(var money in this)
			{
				splits.Add(money.Split(parts).ToList());
			}

			for(int i = 0; i < parts; i++)
			{
				MoneyBag bag = new MoneyBag();
				foreach(var split in splits)
				{
					bag += split[i];
				}
				yield return bag;
			}
		}

		#region IMoney Members


		IEnumerable<IMoney> IMoney.Split(int parts)
		{
			return Split(parts);
		}

		#endregion
	}

	public class Money : IComparable, IComparable<Money>, IEquatable<Money>, IMoney
	{

		// for decimal.TryParse. None of the NumberStyles' composed values is useful for bitcoin style
		private const NumberStyles BitcoinStyle =
						  NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
						| NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;


		/// <summary>
		/// Parse a bitcoin amount (Culture Invariant)
		/// </summary>
		/// <param name="bitcoin"></param>
		/// <param name="nRet"></param>
		/// <returns></returns>
		public static bool TryParse(string bitcoin, out Money nRet)
		{
			nRet = null;

			decimal value;
			if(!decimal.TryParse(bitcoin, BitcoinStyle, CultureInfo.InvariantCulture, out value))
			{
				return false;
			}

			try
			{
				nRet = new Money(value, MoneyUnit.BTC);
				return true;
			}
			catch(OverflowException)
			{
				return false;
			}
		}

		/// <summary>
		/// Parse a bitcoin amount (Culture Invariant)
		/// </summary>
		/// <param name="bitcoin"></param>
		/// <returns></returns>
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
			// used as a central point where long.MinValue checking can be enforced 
			private set
			{
				CheckLongMinValue(value);
				_Satoshis = value;
			}
		}

		/// <summary>
		/// Get absolute value of the instance
		/// </summary>
		/// <returns></returns>
		public Money Abs()
		{
			var a = this;
			if(a < Money.Zero)
				a = -a;
			return a;
		}		

		public Money(int satoshis)
		{
			Satoshi = satoshis;
		}

		public Money(uint satoshis)
		{
			Satoshi = satoshis;
		}

		public Money(long satoshis)
		{
			Satoshi = satoshis;
		}

		public Money(ulong satoshis)
		{
			// overflow check. 
			// ulong.MaxValue is greater than long.MaxValue
			checked
			{
				Satoshi = (long)satoshis;
			}
		}

		public Money(decimal amount, MoneyUnit unit)
		{
			// sanity check. Only valid units are allowed
			CheckMoneyUnit(unit, "unit");
			checked
			{
				var satoshi = amount * (int)unit;
				Satoshi = (long)satoshi;
			}
		}


		/// <summary>
		/// Split the Money in parts without loss
		/// </summary>
		/// <param name="parts">The number of parts (must be more than 0)</param>
		/// <returns>The splitted money</returns>
		public IEnumerable<Money> Split(int parts)
		{
			if(parts <= 0)
				throw new ArgumentOutOfRangeException("Parts should be more than 0", "parts");
			long remain;
			long result = DivRem(_Satoshis, parts, out remain);

			for(int i = 0; i < parts; i++)
			{
				yield return Money.Satoshis(result + (remain > 0 ? 1 : 0));
				remain--;
			}
		}

		private static long DivRem(long a, long b, out long result)
		{
			result = a % b;
			return a / b;
		}

		public static Money FromUnit(decimal amount, MoneyUnit unit)
		{
			return new Money(amount, unit);
		}

		/// <summary>
		/// Convert Money to decimal (same as ToDecimal)
		/// </summary>
		/// <param name="unit"></param>
		/// <returns></returns>
		public decimal ToUnit(MoneyUnit unit)
		{
			CheckMoneyUnit(unit, "unit");
			// overflow safe because (long / int) always fit in decimal 
			// decimal operations are checked by default
			return (decimal)Satoshi / (int)unit;
		}
		/// <summary>
		/// Convert Money to decimal (same as ToUnit)
		/// </summary>
		/// <param name="unit"></param>
		/// <returns></returns>
		public decimal ToDecimal(MoneyUnit unit)
		{
			return ToUnit(unit);
		}

		public static Money Coins(decimal coins)
		{
			// overflow safe.
			// decimal operations are checked by default
			return new Money(coins * COIN, MoneyUnit.Satoshi);
		}

		public static Money Bits(decimal bits)
		{
			// overflow safe.
			// decimal operations are checked by default
			return new Money(bits * CENT, MoneyUnit.Satoshi);
		}

		public static Money Cents(decimal cents)
		{
			// overflow safe.
			// decimal operations are checked by default
			return new Money(cents * CENT, MoneyUnit.Satoshi);
		}

		public static Money Satoshis(decimal sats)
		{
			return new Money(sats, MoneyUnit.Satoshi);
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
			if(other == null)
				return false;
			return _Satoshis.Equals(other._Satoshis);
		}

		public int CompareTo(Money other)
		{
			if(other == null)
				return 1;
			return _Satoshis.CompareTo(other._Satoshis);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if(obj == null)
				return 1;
			Money m = obj as Money;
			if(m != null)
				return _Satoshis.CompareTo(m._Satoshis);
#if !(PORTABLE || NETCORE)
			return _Satoshis.CompareTo(obj);
#else
			return _Satoshis.CompareTo((long)obj);
#endif
		}

		#endregion

		public static Money operator -(Money left, Money right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return new Money(checked(left._Satoshis - right._Satoshis));
		}
		public static Money operator -(Money left)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			return new Money(checked(-left._Satoshis));
		}
		public static Money operator +(Money left, Money right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return new Money(checked(left._Satoshis + right._Satoshis));
		}
		public static Money operator *(int left, Money right)
		{
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return Money.Satoshis(checked(left * right._Satoshis));
		}

		public static Money operator *(Money right, int left)
		{
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return Money.Satoshis(checked(right._Satoshis * left));
		}
		public static Money operator *(long left, Money right)
		{
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return Money.Satoshis(checked(left * right._Satoshis));
		}
		public static Money operator *(Money right, long left)
		{
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return Money.Satoshis(checked(left * right._Satoshis));
		}

		public static Money operator /(Money left, long right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			return new Money(checked(left._Satoshis / right));
		}

		public static bool operator <(Money left, Money right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return left._Satoshis < right._Satoshis;
		}
		public static bool operator >(Money left, Money right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return left._Satoshis > right._Satoshis;
		}
		public static bool operator <=(Money left, Money right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return left._Satoshis <= right._Satoshis;
		}
		public static bool operator >=(Money left, Money right)
		{
			if(left == null)
				throw new ArgumentNullException(nameof(left));
			if(right == null)
				throw new ArgumentNullException(nameof(right));
			return left._Satoshis >= right._Satoshis;
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
			return new Money(checked((long)value));
		}

		public static implicit operator long(Money value)
		{
			return value.Satoshi;
		}

		public static implicit operator ulong(Money value)
		{
			return checked((ulong)value.Satoshi);
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
			if(Object.ReferenceEquals(a, b))
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


		/// <summary>
		/// Returns a culture invariant string representation of Bitcoin amount
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ToString(false, false);
		}

		/// <summary>
		/// Returns a culture invariant string representation of Bitcoin amount
		/// </summary>
		/// <param name="fplus">True if show + for a positive amount</param>
		/// <param name="trimExcessZero">True if trim excess zeroes</param>
		/// <returns></returns>
		public string ToString(bool fplus, bool trimExcessZero = true)
		{
			var fmt = string.Format(CultureInfo.InvariantCulture, "{{0:{0}{1}B}}",
									(fplus ? "+" : null),
									(trimExcessZero ? "2" : "8"));
			return string.Format(BitcoinFormatter.Formatter, fmt, _Satoshis);
		}


		static Money _Zero = new Money(0);
		public static Money Zero
		{
			get
			{
				return _Zero;
			}
		}

		/// <summary>
		/// Tell if amount is almost equal to this instance
		/// </summary>
		/// <param name="amount"></param>
		/// <param name="dust">more or less amount</param>
		/// <returns>true if equals, else false</returns>
		public bool Almost(Money amount, Money dust)
		{
			if(amount == null)
				throw new ArgumentNullException(nameof(amount));
			if(dust == null)
				throw new ArgumentNullException(nameof(dust));
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
			if(amount == null)
				throw new ArgumentNullException(nameof(amount));
			if(margin < 0.0m || margin > 1.0m)
				throw new ArgumentOutOfRangeException("margin", "margin should be between 0 and 1");
			var dust = Money.Satoshis((decimal)this.Satoshi * margin);
			return Almost(amount, dust);
		}

		public static Money Min(Money a, Money b)
		{
			if(a == null)
				throw new ArgumentNullException(nameof(a));
			if(b == null)
				throw new ArgumentNullException(nameof(b));
			if(a <= b)
				return a;
			return b;
		}

		public static Money Max(Money a, Money b)
		{
			if(a == null)
				throw new ArgumentNullException(nameof(a));
			if(b == null)
				throw new ArgumentNullException(nameof(b));
			if(a >= b)
				return a;
			return b;
		}

		private static void CheckLongMinValue(long value)
		{
			if(value == long.MinValue)
				throw new OverflowException("satoshis amount should be greater than long.MinValue");
		}

		private static void CheckMoneyUnit(MoneyUnit value, string paramName)
		{
			var typeOfMoneyUnit = typeof(MoneyUnit);
			if(!Enum.IsDefined(typeOfMoneyUnit, value))
			{
				throw new ArgumentException("Invalid value for MoneyUnit", paramName);
			}
		}

		#region IMoney Members

		IMoney IMoney.Add(IMoney money)
		{
			return this + (Money)money;
		}

		IMoney IMoney.Sub(IMoney money)
		{
			return this - (Money)money;
		}

		IMoney IMoney.Negate()
		{
			return -this;
		}

		#endregion

		#region IComparable Members

		int IComparable.CompareTo(object obj)
		{
			return this.CompareTo(obj);
		}

		#endregion

		#region IComparable<IMoney> Members

		int IComparable<IMoney>.CompareTo(IMoney other)
		{
			return this.CompareTo(other);
		}

		#endregion

		#region IEquatable<IMoney> Members

		bool IEquatable<IMoney>.Equals(IMoney other)
		{
			return this.Equals(other);
		}

		bool IMoney.IsCompatible(IMoney money)
		{
			if(money == null)
				throw new ArgumentNullException(nameof(money));
			return money is Money;
		}

		#endregion

		public const long COIN = 100 * 1000 * 1000;
		public const long CENT = COIN / 100;
		public const long NANO = CENT / 100;


		#region IMoney Members


		IEnumerable<IMoney> IMoney.Split(int parts)
		{
			return Split(parts);
		}

		#endregion
	}

	static class CharExtensions
	{
		// .NET Char class already provides an static IsDigit method however
		// it behaves differently depending on if char is a Latin or not.
		public static bool IsDigit(this char c)
		{
			return c == '0' || c == '1' || c == '2' || c == '3' || c == '4' || c == '5' || c == '6' || c == '7' || c == '8' || c == '9';
		}
	}

	internal class BitcoinFormatter : IFormatProvider, ICustomFormatter
	{
		public static readonly BitcoinFormatter Formatter = new BitcoinFormatter();

		public object GetFormat(Type formatType)
		{
			return formatType == typeof(ICustomFormatter) ? this : null;
		}

		public string Format(string format, object arg, IFormatProvider formatProvider)
		{
			if(!this.Equals(formatProvider))
			{
				return null;
			}
			var i = 0;
			var plus = format[i] == '+';
			if(plus)
				i++;
			int decPos = 0;
			if(int.TryParse(format.Substring(i, 1), out decPos))
			{
				i++;
			}
			var unit = format[i];
			var unitToUseInCalc = MoneyUnit.BTC;
			switch(unit)
			{
				case 'B':
					unitToUseInCalc = MoneyUnit.BTC;
					break;
			}
			var val = Convert.ToDecimal(arg, CultureInfo.InvariantCulture) / (int)unitToUseInCalc;
			var zeros = new string('0', decPos);
			var rest = new string('#', 10 - decPos);
			var fmt = plus && val > 0 ? "+" : string.Empty;

			fmt += "{0:0" + (decPos > 0 ? "." + zeros + rest : string.Empty) + "}";
			return string.Format(CultureInfo.InvariantCulture, fmt, val);
		}
	}


}
