using NBitcoin.BouncyCastle.Math;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NBitcoin
{
	/// <summary>
	/// Represent the challenge that miners must solve for finding a new block
	/// </summary>
	public class Target
	{
		static Target _Difficulty1;
		static BigInteger _Difficulty1BigInteger;
		public static Target Difficulty1
		{
			get
			{
				if (_Difficulty1 == null)
				{
					_Difficulty1 = new Target(0x1d00ffff);
				}
				return _Difficulty1;
			}
		}

		private static BigInteger Difficulty1BigInteger
		{
			get
			{
				if (_Difficulty1BigInteger == null)
				{
					_Difficulty1BigInteger = Difficulty1.ToBigInteger();
				}
				return _Difficulty1BigInteger;
			}
		}

		public Target(uint compact)
		{
			_Target = compact;
		}



		uint _Target;

		public Target(byte[] compact)
		{
			if (compact.Length == 4)
			{
				_Target = Utils.ToUInt32(compact, false);
			}
			else
				throw new FormatException("Invalid number of bytes");
		}

		public Target(BigInteger target)
		{
			var bytes = target.ToByteArray();
			var exp = bytes.Length;

			var value = (uint)target.ShiftRight(8 * (exp - 3)).LongValue;
			_Target = (uint)exp << 24 | value & 0x00ffffff;
			if ((value & 0x00ffffff) == 0)
			{
				_Target = 0x03000000;
			}
			else
			{
				while ((_Target & 0x000000ff) == 0)
				{
					exp++;
					value = value >> 8;
					_Target = (uint)exp << 24 | value & 0x00ffffff;
				}
			}
		}
		public Target(uint256 target) : this(ToBigInteger(target))
		{
		}

		private static BigInteger ToBigInteger(uint256 target)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			return new BigInteger(1, target.ToBytes(false));
		}

		public static implicit operator Target(uint a)
		{
			return new Target(a);
		}
		public static implicit operator uint(Target a)
		{
			if (a == null)
				throw new ArgumentNullException(nameof(a));
			return a._Target;
		}

		double? _Difficulty;

		public double Difficulty
		{
			get
			{
				if (_Difficulty == null)
				{
					var target = this.ToBigInteger();
					var qr = Difficulty1BigInteger.DivideAndRemainder(target);
					var quotient = qr[0];
					var remainder = qr[1];
					var decimalPart = BigInteger.Zero;

					var quotientStr = quotient.ToString();
					int precision = 12;
					StringBuilder builder = new StringBuilder(quotientStr.Length + 1 + precision);
					builder.Append(quotientStr);
					builder.Append('.');
					for (int i = 0; i < precision; i++)
					{
						var div = (remainder.Multiply(BigInteger.Ten)).Divide(target);
						decimalPart = decimalPart.Multiply(BigInteger.Ten);
						decimalPart = decimalPart.Add(div);

						remainder = remainder.Multiply(BigInteger.Ten).Subtract(div.Multiply(target));
					}
					builder.Append(decimalPart.ToString().PadLeft(precision, '0'));
					_Difficulty = double.Parse(builder.ToString(), new NumberFormatInfo()
					{
						NegativeSign = "-",
						NumberDecimalSeparator = "."
					});
				}
				return _Difficulty.Value;
			}
		}



		public override bool Equals(object obj)
		{
			Target item = obj as Target;
			if (item == null)
				return false;
			return _Target.Equals(item._Target);
		}
		public static bool operator ==(Target a, Target b)
		{
			if (a is Target aa && b is Target bb)
			{
				return a._Target == bb._Target;
			}
			return a is null && b is null;
		}

		public static bool operator !=(Target a, Target b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _Target.GetHashCode();
		}

		public BigInteger ToBigInteger()
		{
			var exp = _Target >> 24;
			var value = _Target & 0x00FFFFFF;
			return BigInteger.ValueOf(value).ShiftLeft(8 * ((int)exp - 3));
		}

		public uint ToCompact()
		{
			return _Target;
		}

		public uint256 ToUInt256()
		{
			return ToUInt256(ToBigInteger());
		}

		internal static uint256 ToUInt256(BigInteger input)
		{
			var array = input.ToByteArray();

			var missingZero = 32 - array.Length;
			if (missingZero < 0)
				throw new InvalidOperationException("Awful bug, this should never happen");
			if (missingZero != 0)
			{
				array = new byte[missingZero].Concat(array).ToArray();
			}
			return new uint256(array, false);
		}

		public override string ToString()
		{
			return ToUInt256().ToString();
		}
	}
}
