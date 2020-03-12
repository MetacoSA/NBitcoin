#if NO_NATIVE_BIGNUM
using NBitcoin.BouncyCastle.Math;
#else
using System.Numerics;
#endif
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
				InitDifficulty1();
				return _Difficulty1;
			}
		}

		private static void InitDifficulty1()
		{
			if (_Difficulty1 == null)
			{
				_Difficulty1 = new Target(0x1d00ffff);
				_Difficulty1BigInteger = Difficulty1.ToBigInteger();
			}
		}

		private static BigInteger Difficulty1BigInteger
		{
			get
			{
				InitDifficulty1();
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

		public Target(BigInteger target) : this(ToUInt256(target))
		{
		}
		public Target(uint256 target)
		{
			int nSize = (target.GetBisCount() + 7) / 8;
			uint nCompact = 0;
			if (nSize <= 3)
			{
				nCompact = (uint)(target.GetLow64() << 8 * (3 - nSize));
			}
			else
			{
				BigInteger bn = ToBigInteger(target) >> 8 * (nSize - 3);
				nCompact = (uint)(ulong)bn;
			}
			// The 0x00800000 bit denotes the sign.
			// Thus, if it is already set, divide the mantissa by 256 and increase the exponent.
			if ((nCompact & 0x00800000) != 0)
			{
				nCompact >>= 8;
				nSize++;
			}
			System.Diagnostics.Debug.Assert((nCompact & ~0x007fffff) == 0);
			System.Diagnostics.Debug.Assert(nSize < 256);
			nCompact |= (uint)(nSize << 24);
			//nCompact |= (fNegative && (nCompact & 0x007fffff) ? 0x00800000 : 0);
			_Target = nCompact;
		}

		internal static BigInteger ToBigInteger(uint256 target)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));
#if NO_NATIVE_BIGNUM
			return new BigInteger(1, target.ToBytes(false));
#else
#if HAS_SPAN
			Span<byte> array = stackalloc byte[33];
			array[32] = 0;
			target.ToBytes(array, true);
#else
			var bytes = target.ToBytes(true);
			var array = bytes;
			if ((bytes[31] & 0x80) != 0)
			{
				array = new byte[33];
				array[32] = 0;
				Array.Copy(bytes, 0, array, 0, 32);
			}
#endif
			return new BigInteger(array);
#endif
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
#if NO_NATIVE_BIGNUM
		static BigInteger Ten = BigInteger.Ten;
#else
		static BigInteger Ten = new BigInteger(10);
#endif

		double? _Difficulty;
		public double Difficulty
		{
			get
			{
				if (_Difficulty == null)
				{
					var target = this.ToBigInteger();
#if NO_NATIVE_BIGNUM
					var qr = Difficulty1BigInteger.DivideAndRemainder(target);
					var quotient = qr[0];
					var remainder = qr[1];
#else
					var quotient = BigInteger.DivRem(Difficulty1BigInteger, target, out var remainder);
#endif
					var decimalPart = BigInteger.Zero;

					var quotientStr = quotient.ToString();
					int precision = 12;
					StringBuilder builder = new StringBuilder(quotientStr.Length + 1 + precision);
					builder.Append(quotientStr);
					builder.Append('.');
					for (int i = 0; i < precision; i++)
					{
#if NO_NATIVE_BIGNUM
						var div = (remainder.Multiply(Ten)).Divide(target);
						decimalPart = decimalPart.Multiply(Ten);
						decimalPart = decimalPart.Add(div);
						remainder = remainder.Multiply(Ten).Subtract(div.Multiply(target));
#else
						var div = (remainder * Ten) / target;
						decimalPart = decimalPart * Ten;
						decimalPart = decimalPart + div;
						remainder = (remainder * Ten) - (div * target);
#endif
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
#if NO_NATIVE_BIGNUM
			return BigInteger.ValueOf(value).ShiftLeft(8 * ((int)exp - 3));
#else
			return new BigInteger(value) << (8 * ((int)exp - 3));
#endif
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
			if (input.Sign < 1)
				throw new ArgumentException(paramName: nameof(input), message: "input should not be negative");
			// Big endian
#if NO_NATIVE_BIGNUM
			var array = input.ToByteArrayUnsigned();
			int len = array.Length;
			Array.Reverse(array);
#else
			// We are little endian + 1 bit sign
			var array = input.ToByteArray();
			int len = array.Length;
			// Strip sign
			if (array[array.Length - 1] == 0)
				len--;
#endif
			if (len > 32)
				throw new ArgumentException(paramName: nameof(input), message: "input is too big");
			if (len == 32)
				return new uint256(array, 0, len);

#if HAS_SPAN
			Span<byte> tmp = stackalloc byte[32];
			tmp.Clear();
			array.AsSpan().Slice(0, len).CopyTo(tmp);
#else
			byte[] tmp = new byte[32];
			Array.Copy(array, 0, tmp, 0, len);
#endif
			return new uint256(tmp);

		}

		public override string ToString()
		{
			return ToUInt256().ToString();
		}
	}
}
