#if HAS_SPAN
using NBitcoin.Secp256k1;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace NBitcoin.Tests
{
	public partial class Secp256k1Tests
	{
		static int secp256k1_test_rng_integer_bits_left = 0;
		static ulong secp256k1_test_rng_integer;
		static int[] addbits = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 1, 0 };
		private readonly ITestOutputHelper Logs;
		private void secp256k1_rand256(Span<byte> output)
		{
			// Should reproduce the secp256k1_test_rng
			RandomUtils.GetBytes(output);
		}

		private byte[] secp256k1_rand256()
		{
			var output = new byte[32];
			RandomUtils.GetBytes(output);
			return output;
		}

		private void secp256k1_rand256_test(Span<byte> output)
		{
			secp256k1_rand_bytes_test(output, 32);
		}
		Scalar random_scalar_order_test()
		{
			Scalar scalar = Scalar.Zero;
			Span<byte> output = stackalloc byte[32];
			do
			{
				RandomUtils.GetBytes(output);
				scalar = new Scalar(output, out int overflow);
				if (overflow != 0 || scalar.IsZero)
				{
					continue;
				}
				break;
			} while (true);
			return scalar;
		}
		private void secp256k1_rand_bytes_test(Span<byte> bytes, int len)
		{
			int bits = 0;
			bytes = bytes.Slice(0, len);
			bytes.Fill(0);
			while (bits < len * 8)
			{
				uint now;
				uint val;
				now = 1 + (secp256k1_rand_bits(6) * secp256k1_rand_bits(5) + 16) / 31;
				val = secp256k1_rand_bits(1);
				while (now > 0 && bits < len * 8)
				{
					bytes[bits / 8] |= (byte)(val << (bits % 8));
					now--;
					bits++;
				}
			}
		}

		private Scalar random_scalar_order()
		{
			Scalar num;
			Span<byte> b32 = stackalloc byte[32];
			do
			{
				b32.Clear();
				secp256k1_rand256(b32);
				num = new Scalar(b32, out var overflow);
				if (overflow != 0 || num.IsZero)
				{
					continue;
				}
				return num;
			} while (true);
		}
		static void rand_flip_bit(Span<byte> array)
		{
			array[(int)secp256k1_rand_int((uint)array.Length)] ^= (byte)(1 << (int)secp256k1_rand_int(8));
		}
		static uint secp256k1_rand_int(uint range)
		{
			/* We want a uniform integer between 0 and range-1, inclusive.
			 * B is the smallest number such that range <= 2**B.
			 * two mechanisms implemented here:
			 * - generate B bits numbers until one below range is found, and return it
			 * - find the largest multiple M of range that is <= 2**(B+A), generate B+A
			 *   bits numbers until one below M is found, and return it modulo range
			 * The second mechanism consumes A more bits of entropy in every iteration,
			 * but may need fewer iterations due to M being closer to 2**(B+A) then
			 * range is to 2**B. The array below (indexed by B) contains a 0 when the
			 * first mechanism is to be used, and the number A otherwise.
			 */
			uint trange, mult;
			int bits = 0;
			if (range <= 1)
			{
				return 0;
			}
			trange = range - 1;
			while (trange > 0)
			{
				trange >>= 1;
				bits++;
			}
			if (addbits[bits] != 0)
			{
				bits = bits + addbits[bits];
				mult = ((~((uint)0)) >> (32 - bits)) / range;
				trange = range * mult;
			}
			else
			{
				trange = range;
				mult = 1;
			}
			while (true)
			{
				uint x = secp256k1_rand_bits(bits);
				if (x < trange)
				{
					return (mult == 1) ? x : (x % range);
				}
			}
		}
		static uint secp256k1_rand_bits(int bits)
		{
			uint ret;
			if (secp256k1_test_rng_integer_bits_left < bits)
			{
				secp256k1_test_rng_integer |= (((ulong)RandomUtils.GetUInt32()) << secp256k1_test_rng_integer_bits_left);
				secp256k1_test_rng_integer_bits_left += 32;
			}
			ret = (uint)secp256k1_test_rng_integer;
			secp256k1_test_rng_integer >>= bits;
			secp256k1_test_rng_integer_bits_left -= bits;
			ret &= ((~((uint)0)) >> (32 - bits));
			return ret;
		}

		FE random_field_element_test()
		{
			FE field;
			Span<byte> output = stackalloc byte[32];
			do
			{
				RandomUtils.GetBytes(output);
				if (FE.TryCreate(output, out field))
				{
					break;
				}
			} while (true);
			return field;
		}

		private FE random_fe()
		{
			FE field;
			Span<byte> output = stackalloc byte[32];
			do
			{
				secp256k1_rand256(output);
				if (FE.TryCreate(output, out field))
				{
					return field;
				}
			} while (true);
		}

		void ge_equals_gej(in GE a, in GEJ b)
		{
			FE z2s;
			FE u1, u2, s1, s2;
			Assert.True(a.infinity == b.infinity);
			if (a.infinity)
			{
				return;
			}
			/* Check a.x * b.z^2 == b.x && a.y * b.z^3 == b.y, to avoid inverses. */
			z2s = b.z.Sqr();
			u1 = a.x * z2s;
			u2 = b.x;
			u2 = u2.NormalizeWeak();
			s1 = a.y * z2s;
			s1 = s1 * b.z;
			s2 = b.y;
			s2 = s2.NormalizeWeak();
			Assert.True(u1.EqualsVariable(u2));
			Assert.True(s1.EqualsVariable(s2));
		}
		void random_field_element_magnitude(ref GE ge, char coordinate)
		{
			switch (coordinate)
			{
				case 'x':
					{
						var x = ge.x;
						random_field_element_magnitude(ref x);
						ge = new GE(x, ge.y, ge.infinity);
					}
					break;
				case 'y':
					{
						var y = ge.y;
						random_field_element_magnitude(ref y);
						ge = new GE(ge.x, y, ge.infinity);
					}
					break;
			}
		}
		void random_field_element_magnitude(ref GEJ ge, char coordinate)
		{
			switch (coordinate)
			{
				case 'x':
					{
						var x = ge.x;
						random_field_element_magnitude(ref x);
						ge = new GEJ(x, ge.y, ge.z, ge.infinity);
					}
					break;
				case 'y':
					{
						var y = ge.y;
						random_field_element_magnitude(ref y);
						ge = new GEJ(ge.x, y, ge.z, ge.infinity);
					}
					break;
				case 'z':
					{
						var z = ge.z;
						random_field_element_magnitude(ref z);
						ge = new GEJ(ge.x, ge.y, z, ge.infinity);
					}
					break;
			}
		}
		void random_field_element_magnitude(ref FE fe)
		{
			FE zero;
			var n = secp256k1_rand_int(9U);
			fe = fe.Normalize();
			if (n == 0)
			{
				return;
			}
			zero = default;
			zero = zero.Negate(0);
			zero = zero * (n - 1);
			fe += zero;
			Assert.True(fe.magnitude == n);
		}

		private void random_group_element_jacobian_test(ref GEJ gej, ref GE ge)
		{
			FE z2, z3;
			var (gex, gey, geinfinity) = ge;
			var (gejx, gejy, gejz, gejinfinity) = gej;
			do
			{
				gejz = random_field_element_test();
				if (!gejz.IsZero)
				{
					break;
				}
			} while (true);
			z2 = gejz.Sqr();
			z3 = z2 * gejz;
			gejx = gex * z2;
			gejy = gey * z3;
			gejinfinity = geinfinity;
			gej = new GEJ(gejx, gejy, gejz, gejinfinity);
			ge = new GE(gex, gey, geinfinity);
		}

		private GE random_group_element_test()
		{
			FE fe;
			GE ge;
			do
			{
				fe = random_field_element_test();
				if (GE.TryCreateXOVariable(fe, secp256k1_rand_bits(1) == 1, out ge))
				{
					ge = ge.NormalizeY();
					break;
				}
			} while (true);
			return ge;
		}

		private int fe_memcmp(FE a, FE b)
		{
			for (int i = 0; i < 9; i++)
			{
				if (a.At(i) != b.At(i))
					return 1;
			}
			return 0;
		}

		private FE random_fe_non_square()
		{
			var ns = random_fe_non_zero();
			if (ns.Sqrt(out var r))
			{
				ns = ns.Negate(1);
			}
			return ns;
		}

		private void check_fe_equal(FE a, FE b)
		{
			FE an = a.NormalizeWeak();
			FE bn = b.NormalizeVariable();
			Assert.Equal(an, bn);

			FE cn = b.Normalize();
			Assert.Equal(an, cn);
		}
		private void check_fe_inverse(FE a, FE b)
		{
			FE one = FE.CONST(0, 0, 0, 0, 0, 0, 0, 1);
			FE x = a * b;
			check_fe_equal(x, one);
		}

		private FE random_fe_non_zero()
		{
			FE nz = default;
			int tries = 10;
			while (--tries >= 0)
			{
				nz = random_fe();
				nz = nz.Normalize();
				if (!nz.IsZero)
				{
					break;
				}
			}
			/* Infinitesimal probability of spurious failure here */
			Assert.True(tries >= 0);
			return nz;
		}

	}
}
#endif
