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
		static Target _Difficulty1 = new Target(new byte[] { 0x1d, 0x00, 0xff, 0xff });
		public static Target Difficulty1
		{
			get
			{
				return _Difficulty1;
			}
		}

		public Target(uint compact)
			: this(ToBytes(compact))
		{

		}

		private static byte[] ToBytes(uint bits)
		{
			return new byte[]
			{
				(byte)(bits >> 24),
				(byte)(bits >> 16),
				(byte)(bits >> 8),
				(byte)(bits)
			};
		}



		BigInteger _Target;

		public Target(byte[] compact)
		{
			if(compact.Length == 4)
			{
				var exp = compact[0];
				var val = new BigInteger(compact.SafeSubarray(1, 3));
				_Target = val.ShiftLeft(8  * (exp - 3));
			}
			else
				throw new FormatException("Invalid number of bytes");
		}		
		
		public Target(BigInteger target)
		{
			_Target = target;
			_Target = new Target(this.ToCompact())._Target;
		}
		public Target(uint256 target)
		{
			_Target = new BigInteger(target.ToBytes(false));
			_Target = new Target(this.ToCompact())._Target;
		}

		public static implicit operator Target(uint a)
		{
			return new Target(a);
		}
		public static implicit operator uint(Target a)
		{
			var bytes = a._Target.ToByteArray();
			var val = bytes.SafeSubarray(0, Math.Min(bytes.Length, 3));
			Array.Reverse(val);
			var exp = (byte)(bytes.Length);
			var missing = 4 - val.Length;
			if(missing > 0)
				val = val.Concat(new byte[missing]).ToArray();
			if(missing < 0)
				val = val.Take(-missing).ToArray();
			return (uint)val[0] + (uint)(val[1] << 8) + (uint)(val[2] << 16) + (uint)(exp << 24);
		}

		double? _Difficulty;
		
		public double Difficulty
		{
			get
			{
				if(_Difficulty == null)
				{
					var qr = Difficulty1._Target.DivideAndRemainder(_Target);
					var quotient = qr[0];
					var remainder = qr[1];
					var decimalPart = BigInteger.Zero;

					var quotientStr = quotient.ToString();
					int precision = 12;
					StringBuilder builder = new StringBuilder(quotientStr.Length + 1 + precision);
					builder.Append(quotientStr);
					builder.Append('.');
					for(int i = 0; i < precision; i++)
					{
						var div = (remainder.Multiply(BigInteger.Ten)).Divide(_Target);
						decimalPart = decimalPart.Multiply(BigInteger.Ten);
						decimalPart = decimalPart.Add(div);

						remainder = remainder.Multiply(BigInteger.Ten).Subtract(div.Multiply(_Target));
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
			if(item == null)
				return false;
			return _Target.Equals(item._Target);
		}
		public static bool operator ==(Target a, Target b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a._Target.Equals(b._Target);
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
			return _Target;
		}

		public uint ToCompact()
		{
			return (uint)this;
		}

		public uint256 ToUInt256()
		{
			return ToUInt256(_Target);
		}

		internal static uint256 ToUInt256(BigInteger input)
		{
			var array = input.ToByteArray();

			var missingZero = 32 - array.Length;
			if(missingZero < 0)
				throw new InvalidOperationException("Awful bug, this should never happen");
			if(missingZero != 0)
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
