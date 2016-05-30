using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
				var val = compact.SafeSubarray(1, 3).Reverse().ToArray();
				_Target = new BigInteger(val) << 8 * (exp - 3);
			}
			else
				throw new FormatException("Invalid number of bytes");
		}

#if !NOBIGINT
		public Target(BigInteger target)
#else
		internal Target(BigInteger target)
#endif
		{
			_Target = target;
			_Target = new Target(this.ToCompact())._Target;
		}
		public Target(uint256 target)
		{
			_Target = new BigInteger(target.ToBytes());
			_Target = new Target(this.ToCompact())._Target;
		}

		public static implicit operator Target(uint a)
		{
			return new Target(a);
		}
		public static implicit operator uint(Target a)
		{
			var bytes = a._Target.ToByteArray().Reverse().ToArray();
			var val = bytes.SafeSubarray(0, Math.Min(bytes.Length, 3)).Reverse().ToArray();
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
					BigInteger remainder;
					var quotient = BigInteger.DivRem(Difficulty1._Target, _Target, out remainder);
					var decimalPart = BigInteger.Zero;
					for(int i = 0; i < 12; i++)
					{
						var div = (remainder * 10) / _Target;

						decimalPart *= 10;
						decimalPart += div;

						remainder = remainder * 10 - div * _Target;
					}
					_Difficulty = double.Parse(quotient.ToString() + "." + decimalPart.ToString(), new NumberFormatInfo()
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
			return a._Target == b._Target;
		}

		public static bool operator !=(Target a, Target b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return _Target.GetHashCode();
		}

#if !NOBIGINT
		public BigInteger ToBigInteger()
#else
		internal BigInteger ToBigInteger()
#endif
		{
			return _Target;
		}

		public uint ToCompact()
		{
			return (uint)this;
		}

		public uint256 ToUInt256()
		{
			var array = _Target.ToByteArray();
			var missingZero = 32 - array.Length;
			if(missingZero < 0)
				throw new InvalidOperationException("Awful bug, this should never happen");
			if(missingZero != 0)
			{
				array = array.Concat(new byte[missingZero]).ToArray();
			}
			return new uint256(array);
		}

		public override string ToString()
		{
			return ToUInt256().ToString();
		}
	}
}
