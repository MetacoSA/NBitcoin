#if HAS_SPAN
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NBitcoin.Secp256k1;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using System.Security.Cryptography;
using System.IO;
using NBitcoin.Crypto;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using NBitcoin.Secp256k1.Musig;
using NBitcoin.DataEncoders;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace NBitcoin.Tests
{
	public partial class Secp256k1Tests
	{

		public Secp256k1Tests(ITestOutputHelper helper)
		{
			Logs = helper;
			Random = new RFC6979HMACSHA256();
			Span<byte> seed = stackalloc byte[32];
			RandomUtils.GetBytes(seed);
			Logs.WriteLine("Random seed: " + Encoders.Hex.EncodeData(seed));
			Random.Initialize(seed);
		}
		RFC6979HMACSHA256 Random;

		Scalar One = new Scalar(1, 0, 0, 0, 0, 0, 0, 0);
		Scalar Two = new Scalar(2, 0, 0, 0, 0, 0, 0, 0);
		Scalar Three = new Scalar(3, 0, 0, 0, 0, 0, 0, 0);
		Scalar Six = new Scalar(6, 0, 0, 0, 0, 0, 0, 0);
		Scalar Nine = new Scalar(9, 0, 0, 0, 0, 0, 0, 0);
		Scalar OneToEight = new Scalar(1, 2, 3, 4, 5, 6, 7, 8);
		static int count = 10;

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void can_sign_deterministically()
		{
			var key = DataEncoders.Encoders.Hex.DecodeData("52561d97bd6bd2d1aa07e55bf8803e65b63ad76fce819badbf39e00ba14abbb5");
			var data = DataEncoders.Encoders.Hex.DecodeData("1ad99601407cd7859f933a5040f797ac3a9516338b3a0312838f06a07d043147");
			var eckey = Context.Instance.CreateECPrivKey(key);
			var ecpubkey = eckey.CreatePubKey();
			var expectedpubkey = DataEncoders.Encoders.Hex.DecodeData("038debad560e99727fa9d279f4799f9d576acc69b962f6b5fc4755ccef02a0d0dd");
			Assert.True(Utils.ArrayEqual(expectedpubkey, ecpubkey.ToBytes()));
			var sig = eckey.SignECDSARFC6979(data);
			var expectedSig = DataEncoders.Encoders.Hex.DecodeData("3044022023632637ba55497e309b858fe23b4b6354c1842a99f1deeee805a649082c370702202cf2dd37a8c02e38d06fe560dd33f5f006044d43b9ab4f5185c4405924a17896");
			Assert.True(ecpubkey.SigVerify(sig, data));
			Assert.True(Utils.ArrayEqual(expectedSig, sig.ToDER()));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_field_inv()
		{
			FE x, xi, xii;
			int i;
			for (i = 0; i < 10 * count; i++)
			{
				x = random_fe_non_zero();
				xi = x.Inverse();

				check_fe_inverse(x, xi);
				xii = xi.Inverse();
				Assert.Equal(x, xii);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_group_decompress()
		{
			int i;
			for (i = 0; i < count * 4; i++)
			{
				FE fe = random_field_element_test();
				test_group_decompress(fe);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanBuildECMultiplicationContext()
		{
			var ctx = ECMultContext.Instance;
			FE gx = new FE(0x004AA6EBU, 0x011FF73CU, 0x01F4B4CCU, 0x03CCAE8DU, 0x01C4CCB1U, 0x03DCCA61U, 0x02E73D88U, 0x000CE09AU, 0x003D056AU, 0x00061AD2U, 1, true);
			FE gy = new FE(0x0280888BU, 0x01E5FE1BU, 0x038B4A4AU, 0x02422544U, 0x0321FB80U, 0x0010602AU, 0x017446E2U, 0x03DDF8B8U, 0x0132C67CU, 0x000EE54BU, 1, true);
			GE expected = new GE(gx, gy, false);

			ge_equals_ge(expected, ctx.pre_g[32].ToGroupElement());

			FE g3x = new FE(0x00890DB9U, 0x01B09D48U, 0x01E76193U, 0x00177D9AU, 0x03E840E0U, 0x01C43464U, 0x019B01A1U, 0x03442D1BU, 0x01617A56U, 0x00274F09U, 1, true);
			FE g3y = new FE(0x00521EA3U, 0x019F38DEU, 0x02ACA044U, 0x0337A2BCU, 0x001F89FFU, 0x00B6A6E5U, 0x01FEE9B8U, 0x03A6F30BU, 0x00BC4C4AU, 0x001AEF70U, 1, true);
			expected = new GE(g3x, g3y, false);

			ge_equals_ge(expected, ctx.pre_g_128[32].ToGroupElement());

			// Cross check with the same code running on the C version
			uint v = 0;
			for (int i = 0; i < ctx.pre_g.Length; i++)
			{
				var a = ctx.pre_g[i];
				var b = ctx.pre_g_128[i];
				v += a.x.n0 + a.x.n1 + a.x.n2 + a.x.n3 + a.x.n4 + a.x.n5 + a.x.n6 + a.x.n7;
				v += b.x.n0 + b.x.n1 + b.x.n2 + b.x.n3 + b.x.n4 + b.x.n5 + b.x.n6 + b.x.n7;
				v += a.y.n0 + a.y.n1 + a.y.n2 + a.y.n3 + a.y.n4 + a.y.n5 + a.y.n6 + a.y.n7;
				v += b.y.n0 + b.y.n1 + b.y.n2 + b.y.n3 + b.y.n4 + b.y.n5 + b.y.n6 + b.y.n7;
			}
			Assert.Equal(0xc68acec4, v);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_ecmult_chain()
		{
			/* random starting point A (on the curve) */
			GEJ a = GEJ.CONST(
				0x8b30bbe9, 0xae2a9906, 0x96b22f67, 0x0709dff3,
				0x727fd8bc, 0x04d3362c, 0x6c7bf458, 0xe2846004,
				0xa357ae91, 0x5c4a6528, 0x1309edf2, 0x0504740f,
				0x0eb33439, 0x90216b4f, 0x81063cb6, 0x5f2f7e0f
			);
			/* two random initial factors xn and gn */
			Scalar xn = Scalar.CONST(
				0x84cc5452, 0xf7fde1ed, 0xb4d38a8c, 0xe9b1b84c,
				0xcef31f14, 0x6e569be9, 0x705d357a, 0x42985407
			);
			Scalar gn = Scalar.CONST(
				0xa1e58d22, 0x553dcd42, 0xb2398062, 0x5d4c57a9,
				0x6e9323d4, 0x2b3152e5, 0xca2c3990, 0xedc7c9de
			);
			/* two small multipliers to be applied to xn and gn in every iteration: */
			Scalar xf = Scalar.CONST(0, 0, 0, 0, 0, 0, 0, 0x1337);
			Scalar gf = Scalar.CONST(0, 0, 0, 0, 0, 0, 0, 0x7113);
			/* accumulators with the resulting coefficients to A and G */
			Scalar ae = Scalar.CONST(0, 0, 0, 0, 0, 0, 0, 1);
			Scalar ge = Scalar.CONST(0, 0, 0, 0, 0, 0, 0, 0);
			/* actual points */
			GEJ x;
			GEJ x2;
			int i;

			/* the point being computed */
			x = a;
			for (i = 0; i < 200 * count; i++)
			{
				/* in each iteration, compute X = xn*X + gn*G; */
				x = ecmult_ctx.Mult(x, xn, gn);
				/* also compute ae and ge: the actual accumulated factors for A and G */
				/* if X was (ae*A+ge*G), xn*X + gn*G results in (xn*ae*A + (xn*ge+gn)*G) */
				ae = ae * xn;
				ge = ge * xn;
				ge += gn;
				/* modify xn and gn */
				xn = xn * xf;
				gn = gn * gf;

				/* verify */
				if (i == 19999)
				{
					/* expected result after 19999 iterations */
					GEJ rp = GEJ.CONST(
						0xD6E96687, 0xF9B10D09, 0x2A6F3543, 0x9D86CEBE,
						0xA4535D0D, 0x409F5358, 0x6440BD74, 0xB933E830,
						0xB95CBCA2, 0xC77DA786, 0x539BE8FD, 0x53354D2D,
						0x3B4F566A, 0xE6580454, 0x07ED6015, 0xEE1B2A88
					);
					rp = rp.Negate();
					rp = rp.AddVariable(x, out _);
					Assert.True(rp.IsInfinity);
				}
			}
			/* redo the computation, but directly with the resulting ae and ge coefficients: */
			x2 = ecmult_ctx.Mult(a, ae, ge);
			x2 = x2.Negate();
			x2 = x2.AddVariable(x, out _);
			Assert.True(x2.IsInfinity);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_point_times_order()
		{
			int i;
			FE x = FE.CONST(0, 0, 0, 0, 0, 0, 0, 2);
			FE xr = FE.CONST(
				0x7603CB59, 0xB0EF6C63, 0xFE608479, 0x2A0C378C,
				0xDB3233A8, 0x0F8A9A09, 0xA877DEAD, 0x31B38C45
			);
			for (i = 0; i < 500; i++)
			{
				if (GE.TryCreateXOVariable(x, true, out GE p))
				{
					GEJ j;
					Assert.True(p.IsValidVariable);
					j = p.ToGroupElementJacobian();
					Assert.True(j.IsValidVariable);
					test_point_times_order(j);
				}
				x = x.Sqr();
			}
			x = x.NormalizeVariable();
			Assert.True(x.EqualsVariable(xr));
		}
		ECMultContext ecmult_ctx = ECMultContext.Instance;
		Context ctx = Context.Instance;
		private void test_point_times_order(GEJ point)
		{
			/* X * (point + G) + (order-X) * (pointer + G) = 0 */
			Scalar x;
			Scalar nx;
			Scalar zero = Scalar.Zero;
			Scalar one = Scalar.One;
			GEJ res1, res2;
			GE res3;
			byte[] pub = new byte[65];
			int psize = 65;
			x = random_scalar_order_test();
			nx = x.Negate();
			res1 = ecmult_ctx.Mult(point, x, x); /* calc res1 = x * point + x * G; */
			res2 = ecmult_ctx.Mult(point, nx, nx); /* calc res2 = (order - x) * point + (order - x) * G; */
			res1 = res1.AddVariable(res2, out _);
			Assert.True(res1.IsInfinity);
			Assert.True(!res1.IsValidVariable);
			res3 = res1.ToGroupElement();
			Assert.True(res3.IsInfinity);
			Assert.True(!res3.IsValidVariable);
			var pubs = pub.AsSpan();
			Assert.Throws<InvalidOperationException>(() => new ECPubKey(res3, ctx));
			psize = 65;
			pubs = pub.AsSpan();
			Assert.Throws<InvalidOperationException>(() => new ECPubKey(res3, ctx));
			psize = pubs.Length;
			/* check zero/one edge cases */
			res1 = ecmult_ctx.Mult(point, zero, zero);
			res3 = res1.ToGroupElement();
			Assert.True(res3.IsInfinity);
			res1 = ecmult_ctx.Mult(point, one, zero);
			res3 = res1.ToGroupElement();
			ge_equals_gej(res3, point);
			res1 = ecmult_ctx.Mult(point, zero, one);
			res3 = res1.ToGroupElement();
			ge_equals_ge(res3, EC.G);
		}

		private void ge_equals_ge(GE a, GE b)
		{
			Assert.True(a.infinity == b.infinity);
			if (a.infinity)
			{
				return;
			}
			Assert.True(a.x == b.x);
			Assert.True(a.y == b.y);
		}

		private void test_group_decompress(FE x)
		{
			/* The input itself, normalized. */
			FE fex = x;
			FE fez;
			/* Results of set_xquad_var, set_xo_var(..., 0), set_xo_var(..., 1). */
			GE ge_quad, ge_even, ge_odd;
			GEJ gej_quad;
			/* Return values of the above calls. */
			bool res_quad, res_even, res_odd;

			fex = fex.NormalizeVariable();

			res_quad = GE.TryCreateXQuad(fex, out ge_quad);
			res_even = GE.TryCreateXOVariable(fex, false, out ge_even);
			res_odd = GE.TryCreateXOVariable(fex, true, out ge_odd);

			Assert.True(res_quad == res_even);
			Assert.True(res_quad == res_odd);

			if (res_quad)
			{
				ge_quad = new GE(ge_quad.x.NormalizeVariable(), ge_quad.y.NormalizeVariable(), ge_quad.infinity);
				ge_odd = new GE(ge_odd.x.NormalizeVariable(), ge_odd.y.NormalizeVariable(), ge_odd.infinity);
				ge_even = new GE(ge_even.x.NormalizeVariable(), ge_even.y.NormalizeVariable(), ge_even.infinity);

				/* No infinity allowed. */
				Assert.True(!ge_quad.infinity);
				Assert.True(!ge_even.infinity);
				Assert.True(!ge_odd.infinity);

				/* Check that the x coordinates check out. */
				Assert.True(ge_quad.x.EqualsVariable(x));
				Assert.True(ge_even.x.EqualsVariable(x));
				Assert.True(ge_odd.x.EqualsVariable(x));

				/* Check that the Y coordinate result in ge_quad is a square. */
				Assert.True(ge_quad.y.IsQuadVariable);

				/* Check odd/even Y in ge_odd, ge_even. */
				Assert.True(ge_odd.y.IsOdd);
				Assert.True(!ge_even.y.IsOdd);

				/* Check secp256k1_gej_has_quad_y_var. */
				gej_quad = ge_quad.ToGroupElementJacobian();
				Assert.True(gej_quad.HasQuadYVariable);
				do
				{
					fez = random_field_element_test();
				} while (fez.IsZero);
				gej_quad = gej_quad.Rescale(fez);
				Assert.True(gej_quad.HasQuadYVariable);
				gej_quad = gej_quad.Negate();
				Assert.True(!gej_quad.HasQuadYVariable);
				do
				{
					fez = random_field_element_test();
				} while (fez.IsZero);
				gej_quad = gej_quad.Rescale(fez);
				Assert.True(!gej_quad.HasQuadYVariable);
				gej_quad = gej_quad.Negate();
				Assert.True(gej_quad.HasQuadYVariable);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_sqr()
		{
			FE x, s;
			{
				int i;
				x = new FE(1U);
				x = x.Negate(1);

				for (i = 1; i <= 512; ++i)
				{
					x = x * 2;
					x = x.Normalize();
					s = x.Sqr();
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_hmac_sha256_tests()
		{
			var keys = new string[]{
		"\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b\x0b",
		"\x4a\x65\x66\x65",
		"\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa",
		"\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0a\x0b\x0c\x0d\x0e\x0f\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19",
		"\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa",
		"\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa\xaa"
	};
			var inputs = new string[]{
		"\x48\x69\x20\x54\x68\x65\x72\x65",
		"\x77\x68\x61\x74\x20\x64\x6f\x20\x79\x61\x20\x77\x61\x6e\x74\x20\x66\x6f\x72\x20\x6e\x6f\x74\x68\x69\x6e\x67\x3f",
		"\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd\xdd",
		"\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd\xcd",
		"\x54\x65\x73\x74\x20\x55\x73\x69\x6e\x67\x20\x4c\x61\x72\x67\x65\x72\x20\x54\x68\x61\x6e\x20\x42\x6c\x6f\x63\x6b\x2d\x53\x69\x7a\x65\x20\x4b\x65\x79\x20\x2d\x20\x48\x61\x73\x68\x20\x4b\x65\x79\x20\x46\x69\x72\x73\x74",
		"\x54\x68\x69\x73\x20\x69\x73\x20\x61\x20\x74\x65\x73\x74\x20\x75\x73\x69\x6e\x67\x20\x61\x20\x6c\x61\x72\x67\x65\x72\x20\x74\x68\x61\x6e\x20\x62\x6c\x6f\x63\x6b\x2d\x73\x69\x7a\x65\x20\x6b\x65\x79\x20\x61\x6e\x64\x20\x61\x20\x6c\x61\x72\x67\x65\x72\x20\x74\x68\x61\x6e\x20\x62\x6c\x6f\x63\x6b\x2d\x73\x69\x7a\x65\x20\x64\x61\x74\x61\x2e\x20\x54\x68\x65\x20\x6b\x65\x79\x20\x6e\x65\x65\x64\x73\x20\x74\x6f\x20\x62\x65\x20\x68\x61\x73\x68\x65\x64\x20\x62\x65\x66\x6f\x72\x65\x20\x62\x65\x69\x6e\x67\x20\x75\x73\x65\x64\x20\x62\x79\x20\x74\x68\x65\x20\x48\x4d\x41\x43\x20\x61\x6c\x67\x6f\x72\x69\x74\x68\x6d\x2e"
	};
			byte[][] outputs = new[]
			{
				new byte[] { 0xb0, 0x34, 0x4c, 0x61, 0xd8, 0xdb, 0x38, 0x53, 0x5c, 0xa8, 0xaf, 0xce, 0xaf, 0x0b, 0xf1, 0x2b, 0x88, 0x1d, 0xc2, 0x00, 0xc9, 0x83, 0x3d, 0xa7, 0x26, 0xe9, 0x37, 0x6c, 0x2e, 0x32, 0xcf, 0xf7 },
				new byte[] { 0x5b, 0xdc, 0xc1, 0x46, 0xbf, 0x60, 0x75, 0x4e, 0x6a, 0x04, 0x24, 0x26, 0x08, 0x95, 0x75, 0xc7, 0x5a, 0x00, 0x3f, 0x08, 0x9d, 0x27, 0x39, 0x83, 0x9d, 0xec, 0x58, 0xb9, 0x64, 0xec, 0x38, 0x43 },
				new byte[] { 0x77, 0x3e, 0xa9, 0x1e, 0x36, 0x80, 0x0e, 0x46, 0x85, 0x4d, 0xb8, 0xeb, 0xd0, 0x91, 0x81, 0xa7, 0x29, 0x59, 0x09, 0x8b, 0x3e, 0xf8, 0xc1, 0x22, 0xd9, 0x63, 0x55, 0x14, 0xce, 0xd5, 0x65, 0xfe },
				new byte[] { 0x82, 0x55, 0x8a, 0x38, 0x9a, 0x44, 0x3c, 0x0e, 0xa4, 0xcc, 0x81, 0x98, 0x99, 0xf2, 0x08, 0x3a, 0x85, 0xf0, 0xfa, 0xa3, 0xe5, 0x78, 0xf8, 0x07, 0x7a, 0x2e, 0x3f, 0xf4, 0x67, 0x29, 0x66, 0x5b },
				new byte[] { 0x60, 0xe4, 0x31, 0x59, 0x1e, 0xe0, 0xb6, 0x7f, 0x0d, 0x8a, 0x26, 0xaa, 0xcb, 0xf5, 0xb7, 0x7f, 0x8e, 0x0b, 0xc6, 0x21, 0x37, 0x28, 0xc5, 0x14, 0x05, 0x46, 0x04, 0x0f, 0x0e, 0xe3, 0x7f, 0x54 },
				new byte[] { 0x9b, 0x09, 0xff, 0xa7, 0x1b, 0x94, 0x2f, 0xcb, 0x27, 0x63, 0x5f, 0xbc, 0xd5, 0xb0, 0xe9, 0x44, 0xbf, 0xdc, 0x63, 0x64, 0x4f, 0x07, 0x13, 0x93, 0x8a, 0x7f, 0x51, 0x53, 0x5c, 0x3a, 0x35, 0xe2 }
			};
			int i;
			Span<byte> output = stackalloc byte[32];
			for (i = 0; i < 6; i++)
			{
				Secp256k1.HMACSHA256 hasher = new Secp256k1.HMACSHA256();
				hasher.Initialize(ToBytes(keys[i]));
				hasher.Write32(ToBytes(inputs[i]));
				hasher.Finalize(output);
				Assert.True(Utils.ArrayEqual(output.ToArray(), outputs[i]));

				if (inputs[i].Length > 0)
				{
					var split = secp256k1_rand_int((uint)inputs[i].Length);
					hasher.Initialize(ToBytes(keys[i]));
					hasher.Write32(ToBytes(inputs[i]).AsSpan().Slice(0, (int)split));
					hasher.Write32(ToBytes(inputs[i]).AsSpan().Slice((int)split));
					hasher.Finalize(output);
					Assert.True(Utils.ArrayEqual(output.ToArray(), outputs[i]));
				}
			}
		}

		private byte[] ToBytes(string v)
		{
			return v.ToCharArray().Select(c => (byte)c).ToArray();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_rfc6979_hmac_sha256_tests()
		{
			byte[] key1 = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x00, 0x4b, 0xf5, 0x12, 0x2f, 0x34, 0x45, 0x54, 0xc5, 0x3b, 0xde, 0x2e, 0xbb, 0x8c, 0xd2, 0xb7, 0xe3, 0xd1, 0x60, 0x0a, 0xd6, 0x31, 0xc3, 0x85, 0xa5, 0xd7, 0xcc, 0xe2, 0x3c, 0x77, 0x85, 0x45, 0x9a, 0 };
			byte[][] out1 = new[] {
					new byte[] {0x4f, 0xe2, 0x95, 0x25, 0xb2, 0x08, 0x68, 0x09, 0x15, 0x9a, 0xcd, 0xf0, 0x50, 0x6e, 0xfb, 0x86, 0xb0, 0xec, 0x93, 0x2c, 0x7b, 0xa4, 0x42, 0x56, 0xab, 0x32, 0x1e, 0x42, 0x1e, 0x67, 0xe9, 0xfb},
					new byte[] {0x2b, 0xf0, 0xff, 0xf1, 0xd3, 0xc3, 0x78, 0xa2, 0x2d, 0xc5, 0xde, 0x1d, 0x85, 0x65, 0x22, 0x32, 0x5c, 0x65, 0xb5, 0x04, 0x49, 0x1a, 0x0c, 0xbd, 0x01, 0xcb, 0x8f, 0x3a, 0xa6, 0x7f, 0xfd, 0x4a},
					new byte[] {0xf5, 0x28, 0xb4, 0x10, 0xcb, 0x54, 0x1f, 0x77, 0x00, 0x0d, 0x7a, 0xfb, 0x6c, 0x5b, 0x53, 0xc5, 0xc4, 0x71, 0xea, 0xb4, 0x3e, 0x46, 0x6d, 0x9a, 0xc5, 0x19, 0x0c, 0x39, 0xc8, 0x2f, 0xd8, 0x2e}
			};

			byte[] key2 = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xe3, 0xb0, 0xc4, 0x42, 0x98, 0xfc, 0x1c, 0x14, 0x9a, 0xfb, 0xf4, 0xc8, 0x99, 0x6f, 0xb9, 0x24, 0x27, 0xae, 0x41, 0xe4, 0x64, 0x9b, 0x93, 0x4c, 0xa4, 0x95, 0x99, 0x1b, 0x78, 0x52, 0xb8, 0x55 };
			byte[][] out2 = new[]{
					new byte[] { 0x9c, 0x23, 0x6c, 0x16, 0x5b, 0x82, 0xae, 0x0c, 0xd5, 0x90, 0x65, 0x9e, 0x10, 0x0b, 0x6b, 0xab, 0x30, 0x36, 0xe7, 0xba, 0x8b, 0x06, 0x74, 0x9b, 0xaf, 0x69, 0x81, 0xe1, 0x6f, 0x1a, 0x2b, 0x95},
					new byte[] { 0xdf, 0x47, 0x10, 0x61, 0x62, 0x5b, 0xc0, 0xea, 0x14, 0xb6, 0x82, 0xfe, 0xee, 0x2c, 0x9c, 0x02, 0xf2, 0x35, 0xda, 0x04, 0x20, 0x4c, 0x1d, 0x62, 0xa1, 0x53, 0x6c, 0x6e, 0x17, 0xae, 0xd7, 0xa9},
					new byte[] { 0x75, 0x97, 0x88, 0x7c, 0xbd, 0x76, 0x32, 0x1f, 0x32, 0xe3, 0x04, 0x40, 0x67, 0x9a, 0x22, 0xcf, 0x7f, 0x8d, 0x9d, 0x2e, 0xac, 0x39, 0x0e, 0x58, 0x1f, 0xea, 0x09, 0x1c, 0xe2, 0x02, 0xba, 0x94}
			};

			using RFC6979HMACSHA256 rng = new RFC6979HMACSHA256();
			Span<byte> output = stackalloc byte[32];
			output.Fill(0);
			int i;
			rng.Initialize(key1.AsSpan().Slice(0, 64));
			for (i = 0; i < 3; i++)
			{
				rng.Generate(output);
				Assert.True(Utils.ArrayEqual(output.ToArray(), out1[i]));
			}
			rng.Dispose();

			rng.Initialize(key1);
			for (i = 0; i < 3; i++)
			{
				rng.Generate(output);
				Assert.False(Utils.ArrayEqual(output.ToArray(), out1[i]));
			}
			rng.Dispose();

			rng.Initialize(key2.AsSpan().Slice(0, 64));
			for (i = 0; i < 3; i++)
			{
				rng.Generate(output);
				Assert.True(Utils.ArrayEqual(output.ToArray(), out2[i]));
			}
			rng.Dispose();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_ecdsa_end_to_end()
		{
			int i;
			for (i = 0; i < 64 * count; i++)
			{
				test_ecdsa_end_to_end();
			}
		}

		private void test_ecdsa_end_to_end()
		{
			var ctx = Context.Instance;
			byte[] extra = new byte[32];
			int pubkeyclen;
			ECPrivKey privkey = null;
			byte[] message = new byte[32];
			ECPrivKey privkey2 = null;

			SecpECDSASignature[] signature = new SecpECDSASignature[6];
			Scalar r, s;
			byte[] sig = new byte[74];
			int siglen = 74;
			Span<byte> pubkeyc = stackalloc byte[65];
			ECPubKey pubkey;
			ECPubKey pubkey_tmp;
			Span<byte> seckey = stackalloc byte[300];
			int seckeylen = 300;

			/* Generate a random key and message. */
			{
				var msg = random_scalar_order_test();
				var key = random_scalar_order_test();
				privkey = ctx.CreateECPrivKey(key);
				msg.WriteToSpan(message);
			}

			/* Construct and verify corresponding public key. */
			pubkey = privkey.CreatePubKey();
			/* Verify exporting and importing public key. */


			pubkey.WriteToSpan(secp256k1_rand_bits(1) == 1, pubkeyc, out pubkeyclen);
			pubkeyc = pubkeyc.Slice(0, pubkeyclen);
			Assert.True(ctx.TryCreatePubKey(pubkeyc, out pubkey));

			///* Verify negation changes the key and changes it back */
			pubkey_tmp = pubkey;
			Assert.NotNull(pubkey_tmp = pubkey_tmp.Negate());
			Assert.NotEqual(pubkey_tmp, pubkey);
			Assert.NotNull(pubkey_tmp = pubkey_tmp.Negate());
			Assert.Equal(pubkey_tmp, pubkey);

			///* Verify private key import and export. */
			privkey.WriteDerToSpan(secp256k1_rand_bits(1) == 1, seckey, out seckeylen);
			Assert.True(ctx.TryCreatePrivKeyFromDer(seckey, out privkey2));
			Assert.Equal(privkey, privkey2);

			/* Optionally tweak the keys using addition. */
			if (secp256k1_rand_int(3) == 0)
			{
				bool ret1;
				bool ret2;
				Span<byte> rnd = stackalloc byte[32];
				ECPubKey pubkey2;
				secp256k1_rand256_test(rnd);
				ret1 = privkey.TryTweakAdd(rnd, out privkey);
				ret2 = pubkey.TryAddTweak(rnd, out pubkey);
				Assert.Equal(ret1, ret2);
				if (!ret1)
					return;
				pubkey2 = privkey.CreatePubKey();
				Assert.Equal(pubkey, pubkey2);
			}

			///* Optionally tweak the keys using multiplication. */
			if (secp256k1_rand_int(3) == 0)
			{
				bool ret1;
				bool ret2;
				Span<byte> rnd = stackalloc byte[32];
				ECPubKey pubkey2;
				secp256k1_rand256_test(rnd);
				ret1 = privkey.TryTweakMul(rnd, out privkey);
				ret2 = pubkey.TryTweakMul(rnd, out pubkey);
				Assert.Equal(ret1, ret2);
				if (!ret1)
					return;
				pubkey2 = privkey.CreatePubKey();
				Assert.Equal(pubkey, pubkey2);
			}

			/* Sign. */
			Assert.True(privkey.TrySignECDSA(message, out signature[0]));
			Assert.True(privkey.TrySignECDSA(message, out signature[4]));
			Assert.True(privkey.TrySignECDSA(message, new RFC6979NonceFunction(extra), out signature[1]));
			extra[31] = 1;
			Assert.True(privkey.TrySignECDSA(message, new RFC6979NonceFunction(extra), out signature[2]));
			extra[31] = 0;
			extra[0] = 1;
			Assert.True(privkey.TrySignECDSA(message, new RFC6979NonceFunction(extra), out signature[3]));
			Assert.Equal(signature[0], signature[4]);
			Assert.NotEqual(signature[0], signature[1]);
			Assert.NotEqual(signature[0], signature[2]);
			Assert.NotEqual(signature[0], signature[3]);
			Assert.NotEqual(signature[1], signature[2]);
			Assert.NotEqual(signature[1], signature[3]);
			Assert.NotEqual(signature[2], signature[3]);
			/* Verify. */
			Assert.True(pubkey.SigVerify(signature[0], message));
			Assert.True(pubkey.SigVerify(signature[1], message));
			Assert.True(pubkey.SigVerify(signature[2], message));
			Assert.True(pubkey.SigVerify(signature[3], message));
			/* Test lower-S form, malleate, verify and fail, test again, malleate again */
			Assert.True(!signature[0].TryNormalize(out _));
			(r, s) = signature[0];
			s = s.Negate();
			signature[5] = new SecpECDSASignature(r, s, true);
			Assert.False(pubkey.SigVerify(signature[5], message));
			Assert.True(signature[5].TryNormalize(out _));
			Assert.True(signature[5].TryNormalize(out signature[5]));
			Assert.False(signature[5].TryNormalize(out _));
			Assert.False(signature[5].TryNormalize(out signature[5]));
			Assert.True(pubkey.SigVerify(signature[5], message));
			s = s.Negate();
			signature[5] = new SecpECDSASignature(r, s, true);
			Assert.False(signature[5].TryNormalize(out _));
			Assert.True(pubkey.SigVerify(signature[5], message));
			Assert.Equal(signature[5], signature[0]);
			/* Serialize/parse DER and verify again */
			Assert.True(signature[0].WriteDerToSpan(sig, out siglen));
			signature[0] = null;
			var sigspan = sig.AsSpan().Slice(0, siglen);
			Assert.True(SecpECDSASignature.TryCreateFromDer(sigspan, out signature[0]));
			Assert.True(pubkey.SigVerify(signature[0], message));
			/* Serialize/destroy/parse DER and verify again. */
			siglen = 74;
			sigspan = sig.AsSpan().Slice(0, siglen);
			Assert.True(signature[0].WriteDerToSpan(sigspan, out siglen));
			sigspan = sig.AsSpan().Slice(0, siglen);
			sig[secp256k1_rand_int((uint)siglen)] += (byte)(1 + secp256k1_rand_int(255));
			Assert.True(!SecpECDSASignature.TryCreateFromDer(sigspan, out signature[0]) ||
				  !pubkey.SigVerify(signature[0], message));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void EnsureNoOptimization()
		{
			// This is not really a test, it show which methods do not have optimization off
			HashSet<string> whitelist = new HashSet<string>();
			whitelist.Add("get_Zero");
			whitelist.Add("ToStorage");
			whitelist.Add("TryCreate");
			whitelist.Add("Deconstruct");
			whitelist.Add("MemberwiseClone");
			whitelist.Add("Finalize");
			whitelist.Add("At");
			whitelist.Add("ValueType");
			whitelist.Add("Object");
			whitelist.Add("ToC");
			foreach (var method in typeof(Scalar).Assembly.GetTypes()
				.Where(t => t.Namespace?.StartsWith(typeof(Scalar).Namespace) is true)
				.SelectMany(m => m.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)))
			{
				if (whitelist.Contains(method.DeclaringType.Name))
					continue;
				if (whitelist.Contains(method.Name))
					continue;
				if (method.Name.EndsWith("Variable") ||
					method.Name.StartsWith("op_") ||
					method.Name.StartsWith("VERIFY") ||
					method.Name.EndsWith("_CONST"))
					continue;
				var optimized = method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoOptimization);
				if (method.Name.StartsWith("get_") && method.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveInlining))
					continue;
				var methodName = $"{method.DeclaringType.Name}.{method.Name}";
				if (!optimized)
					Logs.WriteLine($"Type {method.DeclaringType.Name}.{method.Name} does not have NoOptimization set");
				//Assert.True(optimized, $"Type {method.DeclaringType.Name}.{method.Name} does not have NoOptimization set");
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_field_convert()
		{
			byte[] b32 = new byte[]{
		0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
		0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
		0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
		0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40
	};
			FEStorage fes = FEStorage.CONST(
				0x00010203U, 0x04050607U, 0x11121314U, 0x15161718U,
				0x22232425U, 0x26272829U, 0x33343536U, 0x37383940U
			);
			FE fe = FE.CONST(
				0x00010203U, 0x04050607U, 0x11121314U, 0x15161718U,
				0x22232425U, 0x26272829U, 0x33343536U, 0x37383940U
			);
			FE fe2;
			Span<byte> b322 = stackalloc byte[32];
			FEStorage fes2;
			/* Check conversions to fe. */
			Assert.True(FE.TryCreate(b32, out fe2));
			Assert.Equal(fe, fe2);
			fe2 = fes.ToFieldElement();
			Assert.Equal(fe, fe2);
			/* Check conversion from fe. */
			fe.WriteToSpan(b322);
			AssertEx.CollectionEquals(b322.ToArray(), b32);
			fes2 = fe.ToStorage();
			Assert.Equal(fes, fes2);
		}

		void test_sqrt(in FE a, FE? k)
		{
			FE r2;
			bool v = a.Sqrt(out FE r1);
			Assert.True(!v == (k is null));

			if (!(k is null))
			{
				/* Check that the returned root is +/- the given known answer */
				r2 = r1.Negate(1);
				r1 += k.Value;
				r2 += k.Value;
				r1 = r1.Normalize();
				r2 = r2.Normalize();
				Assert.True(r1.IsZero || r2.IsZero);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_sqrt()
		{
			FE ns, x, s, t;
			int i;

			/* Check sqrt(0) is 0 */
			x = new FE(0);
			s = x.Sqr();
			test_sqrt(s, x);

			/* Check sqrt of small squares (and their negatives) */
			for (i = 1; i <= 100; i++)
			{
				x = new FE((uint)i);
				s = x.Sqr();
				test_sqrt(s, x);
				t = s.Negate(1);
				test_sqrt(t, null);
			}

			/* Consistency checks for large random values */
			for (i = 0; i < 10; i++)
			{
				int j;
				ns = random_fe_non_square();
				for (j = 0; j < count; j++)
				{
					x = random_fe();
					s = x.Sqr();
					test_sqrt(s, x);
					t = s.Negate(1);
					test_sqrt(t, null);
					t = s * ns;
					test_sqrt(t, null);
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_ecmult_const_tests()
		{
			ecmult_const_mult_zero_one();
			ecmult_const_random_mult();
			ecmult_const_commutativity();
			ecmult_const_chain_multiply();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ecmult_const_chain_multiply()
		{
			/* Check known result (randomly generated test problem from sage) */
			Scalar scalar = Scalar.CONST(
				0x4968d524, 0x2abf9b7a, 0x466abbcf, 0x34b11b6d,
				0xcd83d307, 0x827bed62, 0x05fad0ce, 0x18fae63b
			);
			GEJ expected_point = GEJ.CONST(
				0x5494c15d, 0x32099706, 0xc2395f94, 0x348745fd,
				0x757ce30e, 0x4e8c90fb, 0xa2bad184, 0xf883c69f,
				0x5d195d20, 0xe191bf7f, 0x1be3e55f, 0x56a80196,
				0x6071ad01, 0xf1462f66, 0xc997fa94, 0xdb858435
			);
			GEJ point;
			GE res;
			int i;
			point = EC.G.ToGroupElementJacobian();

			for (i = 0; i < 100; ++i)
			{
				GE tmp;
				tmp = point.ToGroupElement();
				point = tmp.MultConst(scalar, 256);
			}
			res = point.ToGroupElement();
			ge_equals_gej(res, expected_point);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ecmult_const_random_mult()
		{
			/* random starting point A (on the curve) */
			GE a = GE.CONST(
				0x6d986544, 0x57ff52b8, 0xcf1b8126, 0x5b802a5b,
				0xa97f9263, 0xb1e88044, 0x93351325, 0x91bc450a,
				0x535c59f7, 0x325e5d2b, 0xc391fbe8, 0x3c12787c,
				0x337e4a98, 0xe82a9011, 0x0123ba37, 0xdd769c7d
			);
			/* random initial factor xn */
			Scalar xn = Scalar.CONST(
				0x649d4f77, 0xc4242df7, 0x7f2079c9, 0x14530327,
				0xa31b876a, 0xd2d8ce2a, 0x2236d5c6, 0xd7b2029b
			);
			/* expected xn * A (from sage) */
			GE expected_b = GE.CONST(
				0x23773684, 0x4d209dc7, 0x098a786f, 0x20d06fcd,
				0x070a38bf, 0xc11ac651, 0x03004319, 0x1e2a8786,
				0xed8c3b8e, 0xc06dd57b, 0xd06ea66e, 0x45492b0f,
				0xb84e4e1b, 0xfb77e21f, 0x96baae2a, 0x63dec956
			);
			GEJ b = a.MultConst(xn, 256);

			Assert.True(a.IsValidVariable);
			ge_equals_gej(expected_b, b);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ecmult_const_commutativity()
		{
			Scalar a;
			Scalar b;
			GEJ res1;
			GEJ res2;
			GE mid1;
			GE mid2;
			a = random_scalar_order_test();
			b = random_scalar_order_test();

			res1 = EC.G.MultConst(a, 256);
			res2 = EC.G.MultConst(b, 256);
			mid1 = res1.ToGroupElement();
			mid2 = res2.ToGroupElement();
			res1 = mid1.MultConst(b, 256);
			res2 = mid2.MultConst(a, 256);
			mid1 = res1.ToGroupElement();
			mid2 = res2.ToGroupElement();
			ge_equals_ge(mid1, mid2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ecmult_const_mult_zero_one()
		{
			Scalar zero = Scalar.CONST(0, 0, 0, 0, 0, 0, 0, 0);
			Scalar one = Scalar.CONST(0, 0, 0, 0, 0, 0, 0, 1);
			Scalar negone;
			GEJ res1;
			GE res2;
			GE point;
			negone = one.Negate();

			//point = random_group_element_test();
			FE pointx = new FE(0x0317018C, 0x0288AE65, 0x036675A5, 0x02F953F1, 0x032BBF89, 0x018634AF, 0x025D89A8, 0x01A5D73D, 0x03EB429E, 0x000F1C67, 1, true);
			FE pointy = new FE(0x003956B3, 0x01E3AB45, 0x03E26A99, 0x0122353E, 0x01D34895, 0x03C3943F, 0x00BEE690, 0x01E2CFF8, 0x01F7E771, 0x00293A0B, 1, true);
			point = new GE(pointx, pointy, false);


			res1 = point.MultConst(zero, 3);
			res2 = res1.ToGroupElement();
			Assert.True(res2.IsInfinity);
			res1 = point.MultConst(one, 2);
			res2 = res1.ToGroupElement();
			ge_equals_ge(res2, point);
			res1 = point.MultConst(negone, 256);
			res1 = res1.Negate();
			res2 = res1.ToGroupElement();
			ge_equals_ge(res2, point);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_field_misc()
		{
			FE x;
			FE y;
			FE z;
			FE q;
			FE fe5 = FE.CONST(0, 0, 0, 0, 0, 0, 0, 5);
			int i, j;
			for (i = 0; i < 5 * count; i++)
			{
				FEStorage xs, ys, zs;
				x = random_fe();
				y = random_fe_non_zero();
				/* Test the fe equality and comparison operations. */
				Assert.True(x.CompareToVariable(x) == 0);
				Assert.True(x.EqualsVariable(x));
				z = x;
				z += y;
				/* Test fe conditional move; z is not normalized here. */
				q = x;
				FE.CMov(ref x, z, 0);
				Assert.True(!x.normalized && x.magnitude == z.magnitude);
				FE.CMov(ref x, x, 1);
				Assert.True(fe_memcmp(x, z) != 0);
				Assert.True(fe_memcmp(x, q) == 0);
				FE.CMov(ref q, z, 1);
				Assert.True(!q.normalized && q.magnitude == z.magnitude);
				Assert.True(fe_memcmp(q, z) == 0);
				x = x.NormalizeVariable();
				z = z.NormalizeVariable();
				Assert.False(x.EqualsVariable(y));
				q = q.NormalizeVariable();
				FE.CMov(ref q, z, (i & 1));
				Assert.True(q.normalized && q.magnitude == 1);
				for (j = 0; j < 6; j++)
				{
					z = z.Negate(j + 1);
					q = q.NormalizeVariable();
					FE.CMov(ref q, z, (j & 1));
					Assert.True(!q.normalized && q.magnitude == (j + 2));
				}
				z = z.NormalizeVariable();
				/* Test storage conversion and conditional moves. */
				xs = x.ToStorage();
				ys = y.ToStorage();
				zs = z.ToStorage();
				FEStorage.CMov(ref zs, xs, 0);
				FEStorage.CMov(ref zs, zs, 1);
				Assert.NotEqual(xs, zs);
				FEStorage.CMov(ref ys, xs, 1);
				Assert.Equal(xs, ys);
				x = xs.ToFieldElement();
				y = ys.ToFieldElement();
				z = zs.ToFieldElement();
				/* Test that mul_int, mul, and add agree. */
				y += x;
				y += x;
				z = x;
				z *= 3;
				check_fe_equal(y, z);
				y += x;
				z += x;
				check_fe_equal(z, y);
				z = x;
				z *= 5;
				q = x * fe5;
				check_fe_equal(z, q);
				x = x.Negate(1);
				z += x;
				q += x;
				check_fe_equal(y, z);
				check_fe_equal(q, y);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_ge()
		{
			int i;
			for (i = 0; i < count * 32; i++)
			{
				test_ge();
			}
			test_add_neg_y_diff_x();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_normalizes_to_zero()
		{
			FE fe = new FE(0x1298baf3, 0x138381c5, 0x13162263, 0x0d1b8377, 0x109bb537, 0x0e33045f, 0x0e808c49, 0x0c8d29c5, 0x1291a325, 0x0116a7eb, 3, false);
			Assert.False(fe.NormalizesToZeroVariable());
			Assert.False(fe.NormalizesToZero());
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_scalar_split()
		{
			Scalar full;
			Scalar slam = default;
			Scalar s1 = default;
			Span<byte> zero = stackalloc byte[32];
			Span<byte> tmp = stackalloc byte[32];

			full = random_scalar_order_test();

			full.SplitLambda(out s1, out slam);

			/* check that both are <= 128 bits in size */
			if (s1.IsHigh)
			{
				s1 = s1.Negate();
			}
			if (slam.IsHigh)
			{
				slam = slam.Negate();
			}

			s1.WriteToSpan(tmp);
			AssertEx.CollectionEquals(zero.Slice(0, 16).ToArray(), tmp.Slice(0, 16).ToArray());
			slam.WriteToSpan(tmp);
			AssertEx.CollectionEquals(zero.Slice(0, 16).ToArray(), tmp.Slice(0, 16).ToArray());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_ge_neg()
		{
			GE ge = new GE(
				new FE(0x03ff0100, 0x00003c3f, 0x00000000, 0x00000000, 0x02c0ff01, 0x03ffc1ff, 0x000fefff, 0x00000001, 0x03ff0300, 0x003fffff, 1, true),
				new FE(0x03fba4e9, 0x0239dfde, 0x03918486, 0x02c5c78a, 0x0260ac5c, 0x02ff047d, 0x01105975, 0x020e698e, 0x02a3e2e6, 0x0003e8e8, 1, true),
				false
				);

			GE expectedNeg = new GE(
				new FE(0x03ff0100, 0x00003c3f, 0x00000000, 0x00000000, 0x02c0ff01, 0x03ffc1ff, 0x000fefff, 0x00000001, 0x03ff0300, 0x003fffff, 1, true),
				new FE(0x0c044bd3, 0x0dc61f1e, 0x0c6e7b76, 0x0d3a3872, 0x0d9f53a0, 0x0d00fb7f, 0x0eefa687, 0x0df1966e, 0x0d5c1d16, 0x00fc1714, 2, false),
				false
				);

			var actualNeg = ge.Negate();
			Assert.Equal(expectedNeg.x.GetHashCode(), actualNeg.x.GetHashCode());
			Assert.Equal(expectedNeg.y.GetHashCode(), actualNeg.y.GetHashCode());
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_add_neg_y_diff_x()
		{
			/* The point of this test is to check that we can add two points
		* whose y-coordinates are negatives of each other but whose x
		* coordinates differ. If the x-coordinates were the same, these
		* points would be negatives of each other and their sum is
		* infinity. This is cool because it "covers up" any degeneracy
		* in the addition algorithm that would cause the xy coordinates
		* of the sum to be wrong (since infinity has no xy coordinates).
		* HOWEVER, if the x-coordinates are different, infinity is the
		* wrong answer, and such degeneracies are exposed. This is the
		* root of https://github.com/bitcoin-core/secp256k1/issues/257
		* which this test is a regression test for.
		*
		* These points were generated in sage as
		* # secp256k1 params
		* F = FiniteField (0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F)
		* C = EllipticCurve ([F (0), F (7)])
		* G = C.lift_x(0x79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798)
		* N = FiniteField(G.order())
		*
		* # endomorphism values (lambda is 1^{1/3} in N, beta is 1^{1/3} in F)
		* x = polygen(N)
		* lam  = (1 - x^3).roots()[1][0]
		*
		* # random "bad pair"
		* P = C.random_element()
		* Q = -int(lam) * P
		* print "    P: %x %x" % P.xy()
		* print "    Q: %x %x" % Q.xy()
		* print "P + Q: %x %x" % (P + Q).xy()
		*/
			GEJ aj = GEJ.CONST(
				0x8d24cd95, 0x0a355af1, 0x3c543505, 0x44238d30,
				0x0643d79f, 0x05a59614, 0x2f8ec030, 0xd58977cb,
				0x001e337a, 0x38093dcd, 0x6c0f386d, 0x0b1293a8,
				0x4d72c879, 0xd7681924, 0x44e6d2f3, 0x9190117d
			);
			GEJ bj = GEJ.CONST(
				0xc7b74206, 0x1f788cd9, 0xabd0937d, 0x164a0d86,
				0x95f6ff75, 0xf19a4ce9, 0xd013bd7b, 0xbf92d2a7,
				0xffe1cc85, 0xc7f6c232, 0x93f0c792, 0xf4ed6c57,
				0xb28d3786, 0x2897e6db, 0xbb192d0b, 0x6e6feab2
			);
			GEJ sumj = GEJ.CONST(
				0x671a63c0, 0x3efdad4c, 0x389a7798, 0x24356027,
				0xb3d69010, 0x278625c3, 0x5c86d390, 0x184a8f7a,
				0x5f6409c2, 0x2ce01f2b, 0x511fd375, 0x25071d08,
				0xda651801, 0x70e95caf, 0x8f0d893c, 0xbed8fbbe
			);
			GE b;
			GEJ resj;
			GE res;
			b = bj.ToGroupElement();

			resj = aj.AddVariable(bj, out _);
			res = resj.ToGroupElement();

			ge_equals_gej(res, sumj);

			resj = aj + b;

			res = resj.ToGroupElement();

			ge_equals_gej(res, sumj);

			resj = aj.AddVariable(b, out _);
			res = resj.ToGroupElement();
			ge_equals_gej(res, sumj);
		}

		private void test_ge()
		{
			int i, i1;
			int runs = 6;
			/* Points: (infinity, p1, p1, -p1, -p1, p2, p2, -p2, -p2, p3, p3, -p3, -p3, p4, p4, -p4, -p4).
		* The second in each pair of identical points uses a random Z coordinate in the Jacobian form.
		* All magnitudes are randomized.
		* All 17*17 combinations of points are added to each other, using all applicable methods.
		*
		* When the endomorphism code is compiled in, p5 = lambda*p1 and p6 = lambda^2*p1 are added as well.
		*/
			GE[] ge = new GE[1 + 4 * runs];
			GEJ[] gej = new GEJ[1 + 4 * runs];
			FE[] zinv = new FE[1 + 4 * runs];
			FE zf;
			FE zfi2, zfi3;
			gej[0] = GEJ.Infinity;
			ge[0] = default;
			ge[0] = gej[0].ToGroupElementVariable();
			for (i = 0; i < runs; i++)
			{
				int j;
				GE g = random_group_element_test();

				if (i >= runs - 2)
				{
					g = ge[1].MultiplyLambda();
				}
				if (i >= runs - 1)
				{
					g = g.MultiplyLambda();
				}

				ge[1 + 4 * i] = g;
				ge[2 + 4 * i] = g;
				ge[3 + 4 * i] = g.Negate();
				ge[4 + 4 * i] = g.Negate();
				gej[1 + 4 * i] = ge[1 + 4 * i].ToGroupElementJacobian();

				random_group_element_jacobian_test(ref gej[2 + 4 * i], ref ge[2 + 4 * i]);
				gej[3 + 4 * i] = ge[3 + 4 * i].ToGroupElementJacobian();
				random_group_element_jacobian_test(ref gej[4 + 4 * i], ref ge[4 + 4 * i]);
				for (j = 0; j < 4; j++)
				{
					random_field_element_magnitude(ref ge[1 + j + 4 * i], 'x');
					random_field_element_magnitude(ref ge[1 + j + 4 * i], 'y');
					random_field_element_magnitude(ref gej[1 + j + 4 * i], 'x');
					random_field_element_magnitude(ref gej[1 + j + 4 * i], 'y');
					random_field_element_magnitude(ref gej[1 + j + 4 * i], 'z');
				}
			}

			/* Compute z inverses. */
			{
				FE[] zs = new FE[1 + 4 * runs];
				for (i = 0; i < 4 * runs + 1; i++)
				{
					if (i == 0)
					{
						/* The point at infinity does not have a meaningful z inverse. Any should do. */
						do
						{
							zs[i] = random_field_element_test();
						} while (zs[i].IsZero);
					}
					else
					{
						zs[i] = gej[i].z;
					}
				}
				FE.InverseAllVariable(zinv, zs, 4 * runs + 1);
			}

			/* Generate random zf, and zfi2 = 1/zf^2, zfi3 = 1/zf^3 */
			do
			{
				zf = random_field_element_test();
			} while (zf.IsZero);


			random_field_element_magnitude(ref zf);
			zfi3 = zf.InverseVariable();
			zfi2 = zfi3.Sqr();
			zfi3 = zfi3 * zfi2;

			for (i1 = 0; i1 < 1 + 4 * runs; i1++)
			{
				int i2;
				for (i2 = 0; i2 < 1 + 4 * runs; i2++)
				{
					/* Compute reference result using gej + gej (var). */
					GEJ refj, resj;
					GE @ref;
					FE zr = default;

					if (gej[i1].IsInfinity)
						refj = gej[i1].AddVariable(gej[i2], out _);
					else
						refj = gej[i1].AddVariable(gej[i2], out zr);

					/* Check Z ratio. */
					if (!gej[i1].IsInfinity && !refj.IsInfinity)
					{
						FE zrz;
						zrz = zr * gej[i1].z;
						Assert.True(zrz.EqualsVariable(refj.z));
					}
					@ref = refj.ToGroupElementVariable();

					/* Test gej + ge with Z ratio result (var). */
					if (gej[i1].IsInfinity)
						resj = gej[i1].AddVariable(ge[i2], out _);
					else
						resj = gej[i1].AddVariable(ge[i2], out zr);
					ge_equals_gej(@ref, resj);
					if (!gej[i1].IsInfinity && !resj.IsInfinity)
					{
						FE zrz = zr * gej[i1].z;
						Assert.True(zrz.EqualsVariable(resj.z));
					}

					/* Test gej + ge (var, with additional Z factor). */
					{
						GE ge2_zfi = ge[i2]; /* the second term with x and y rescaled for z = 1/zf */
						var ge2zfix = ge2_zfi.x * zfi2;
						var ge2zfiy = ge2_zfi.y * zfi3;
						random_field_element_magnitude(ref ge2zfix);
						random_field_element_magnitude(ref ge2zfiy);
						ge2_zfi = new GE(ge2zfix, ge2zfiy, ge2_zfi.infinity);
						resj = gej[i1].AddZInvVariable(ge2_zfi, zf);
						ge_equals_gej(@ref, resj);
					}

					/* Test gej + ge (const). */
					if (i2 != 0)
					{
						/* secp256k1_gej_add_ge does not support its second argument being infinity. */
						resj = gej[i1] + ge[i2];
						ge_equals_gej(@ref, resj);
					}

					/* Test doubling (var). */
					if ((i1 == 0 && i2 == 0) || ((i1 + 3) / 4 == (i2 + 3) / 4 && ((i1 + 3) % 4) / 2 == ((i2 + 3) % 4) / 2))
					{
						FE zr2;
						/* Normal doubling with Z ratio result. */
						resj = gej[i1].DoubleVariable(out zr2);
						ge_equals_gej(@ref, resj);
						/* Check Z ratio. */
						zr2 = zr2 * gej[i1].z;
						Assert.True(zr2.EqualsVariable(resj.z));
						/* Normal doubling. */
						resj = gej[i2].DoubleVariable();
						ge_equals_gej(@ref, resj);
					}

					/* Test adding opposites. */
					if ((i1 == 0 && i2 == 0) || ((i1 + 3) / 4 == (i2 + 3) / 4 && ((i1 + 3) % 4) / 2 != ((i2 + 3) % 4) / 2))
					{
						Assert.True(@ref.IsInfinity);
					}

					/* Test adding infinity. */
					if (i1 == 0)
					{
						Assert.True(ge[i1].IsInfinity);
						Assert.True(gej[i1].IsInfinity);
						ge_equals_gej(@ref, gej[i2]);
					}
					if (i2 == 0)
					{
						Assert.True(ge[i2].IsInfinity);
						Assert.True(gej[i2].IsInfinity);
						ge_equals_gej(@ref, gej[i1]);
					}
				}
			}

			/* Test adding all points together in random order equals infinity. */
			{
				GEJ sum = GEJ.Infinity;
				GEJ[] gej_shuffled = new GEJ[4 * runs + 1];
				for (i = 0; i < 4 * runs + 1; i++)
				{
					gej_shuffled[i] = gej[i];
				}
				for (i = 0; i < 4 * runs + 1; i++)
				{
					int swap = (int)(i + secp256k1_rand_int((uint)(4U * runs + 1U - i)));
					if (swap != i)
					{
						GEJ t = gej_shuffled[i];
						gej_shuffled[i] = gej_shuffled[swap];
						gej_shuffled[swap] = t;
					}
				}
				for (i = 0; i < 4 * runs + 1; i++)
				{
					sum = sum.AddVariable(gej_shuffled[i], out _);
				}
				Assert.True(sum.IsInfinity);
			}

			/* Test batch gej -> ge conversion with and without known z ratios. */
			{
				FE[] zr = new FE[4 * runs + 1];
				GE[] ge_set_all = new GE[4 * runs + 1];
				for (i = 0; i < 4 * runs + 1; i++)
				{
					/* Compute gej[i + 1].z / gez[i].z (with gej[n].z taken to be 1). */
					if (i < 4 * runs)
					{
						zr[i + 1] = zinv[i] * gej[i + 1].z;
					}
				}

				GE.SetAllGroupElementJacobianVariable(ge_set_all, gej, 4 * runs + 1);
				for (i = 0; i < 4 * runs + 1; i++)
				{
					FE s = random_fe_non_zero();
					gej[i] = gej[i].Rescale(s);
					ge_equals_gej(ge_set_all[i], gej[i]);
				}
			}

			/* Test batch gej -> ge conversion with many infinities. */
			for (i = 0; i < 4 * runs + 1; i++)
			{
				ge[i] = random_group_element_test();
				/* randomly set half the points to infinity */
				if (ge[i].x.IsOdd)
				{
					ge[i] = GE.Infinity;
				}
				gej[i] = ge[i].ToGroupElementJacobian();
			}
			/* batch invert */
			GE.SetAllGroupElementJacobianVariable(ge, gej, 4 * runs + 1);
			/* check result */
			for (i = 0; i < 4 * runs + 1; i++)
			{
				ge_equals_gej(ge[i], gej[i]);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanDoBasicScalarOperations()
		{
			var actual = One + Two;
			Assert.Equal(Three, actual);

			var expected = new Scalar(2, 4, 6, 8, 10, 12, 14, 16);
			Assert.Equal(expected, OneToEight + OneToEight);

			actual = Three.Sqr();
			Assert.Equal(Nine, actual);

			actual = Two * Three;
			Assert.Equal(Six, actual);
			var inv = Six.Inverse();
			actual = inv * Six;
			Assert.Equal(One, actual);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanCreateScalarFromULong()
		{
			var fromUInt = new Scalar(uint.MaxValue);
			fromUInt = fromUInt.Multiply(new Scalar(17));

			var fromULong = new Scalar(17UL * uint.MaxValue);
			Assert.Equal(fromULong, fromUInt);

			var two = Scalar.One + Scalar.One;
			var max = two.Sqr(6) + two.Negate();
			var maxULong = new Scalar(ulong.MaxValue - 1);
			Assert.Equal(maxULong, max);

			Assert.Equal(new Scalar(17) + new Scalar(10), new Scalar(27));
			Assert.Equal(new Scalar(0xabcdef00_abcdef00UL) + new Scalar(0x00000001_00000000UL), new Scalar(0xabcdef01_abcdef00UL));
			Assert.Equal(new Scalar(0x00000001_00000000UL), new Scalar(0xffffffff) + new Scalar(1));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanSerializeScalar()
		{
			Span<byte> output = stackalloc byte[32];
			OneToEight.WriteToSpan(output);
			var actual = new Scalar(output);
			Assert.Equal(OneToEight, actual);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_scalar_tests()
		{
			int i;
			for (i = 0; i < 128 * count; i++)
			{
				scalar_test();
			}
			{
				/* (-1)+1 should be zero. */
				Scalar s = new Scalar(1);
				Assert.True(s.IsOne);
				Scalar o = s.Negate();
				o = o + s;
				Assert.True(o.IsZero);
				o = o.Negate();
				Assert.True(o.IsZero);
			}
			{
				/* Does check_overflow check catch all ones? */
				Scalar overflowed = new Scalar(
					0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU,
					0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU, 0xFFFFFFFFU
				);
				Assert.True(overflowed.IsOverflow);
			}

			{
				byte[,,] chal = {
			{{0xff, 0xff, 0x03, 0x07, 0x00, 0x00, 0x00, 0x00,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x03,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0xff, 0xff,
			  0xff, 0xff, 0x03, 0x00, 0xc0, 0xff, 0xff, 0xff},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0x0f, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf8,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0x03, 0x00, 0x00, 0x00, 0x00, 0xe0, 0xff}},
			{{0xef, 0xff, 0x1f, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0x3f, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
			 {0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xe0,
			  0xff, 0xff, 0xff, 0xff, 0xfc, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0x7f, 0x00, 0x80, 0xff}},
			{{0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00,
			  0x80, 0x00, 0x00, 0x80, 0xff, 0x3f, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0xf8, 0xff, 0xff, 0xff, 0x00},
			 {0x00, 0x00, 0xfc, 0xff, 0xff, 0xff, 0xff, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x0f, 0x00, 0xe0,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x7f, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x7f, 0xff, 0xff, 0xff}},
			{{0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x00, 0x1e, 0xf8, 0xff, 0xff, 0xff, 0xfd, 0xff},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x1f,
			  0x00, 0x00, 0x00, 0xf8, 0xff, 0x03, 0x00, 0xe0,
			  0xff, 0x0f, 0x00, 0x00, 0x00, 0x00, 0xf0, 0xff,
			  0xf3, 0xff, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00}},
			{{0x80, 0x00, 0x00, 0x80, 0xff, 0xff, 0xff, 0x00,
			  0x00, 0x1c, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xe0, 0xff, 0xff, 0xff, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0xe0, 0xff, 0xff, 0xff},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x03, 0x00,
			  0xf8, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0x1f, 0x00, 0x00, 0x80, 0xff, 0xff, 0x3f,
			  0x00, 0xfe, 0xff, 0xff, 0xff, 0xdf, 0xff, 0xff}},
			{{0xff, 0xff, 0xff, 0xff, 0x00, 0x0f, 0xfc, 0x9f,
			  0xff, 0xff, 0xff, 0x00, 0x80, 0x00, 0x00, 0x80,
			  0xff, 0x0f, 0xfc, 0xff, 0x7f, 0x00, 0x00, 0x00,
			  0x00, 0xf8, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00},
			 {0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
			  0x00, 0x00, 0xf8, 0xff, 0x0f, 0xc0, 0xff, 0xff,
			  0xff, 0x1f, 0x00, 0x00, 0x00, 0xc0, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0x07, 0x80, 0xff, 0xff, 0xff}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0x3f, 0x00, 0x00,
			  0x80, 0x00, 0x00, 0x80, 0xff, 0xff, 0xff, 0xff,
			  0xf7, 0xff, 0xff, 0xef, 0xff, 0xff, 0xff, 0x00,
			  0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0xf0},
			 {0x00, 0x00, 0x00, 0x00, 0xf8, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0x01, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x80, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}},
			{{0x00, 0xf8, 0xff, 0x03, 0xff, 0xff, 0xff, 0x00,
			  0x00, 0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x80, 0x00, 0x00, 0x80, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0x03, 0xc0, 0xff, 0x0f, 0xfc, 0xff},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xe0, 0xff, 0xff,
			  0xff, 0x01, 0x00, 0x00, 0x00, 0x3f, 0x00, 0xc0,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}},
			{{0x8f, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0xf8, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0x7f, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}},
			{{0x00, 0x00, 0x00, 0xc0, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0x03, 0x00, 0x80, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0x00, 0x00, 0x80, 0xff, 0x7f},
			 {0xff, 0xcf, 0xff, 0xff, 0x01, 0x00, 0x00, 0x00,
			  0x00, 0xc0, 0xff, 0xcf, 0xff, 0xff, 0xff, 0xff,
			  0xbf, 0xff, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x80, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00}},
			{{0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0xff, 0xff,
			  0xff, 0xff, 0x00, 0xfc, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0x00, 0x80, 0x00, 0x00, 0x80,
			  0xff, 0x01, 0xfc, 0xff, 0x01, 0x00, 0xfe, 0xff},
			 {0xff, 0xff, 0xff, 0x03, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x03, 0x00}},
			{{0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0xe0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0x00, 0xf8, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0x7f, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0xf8, 0xff, 0x01, 0x00, 0xf0, 0xff, 0xff,
			  0xe0, 0xff, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0xff, 0x00},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00,
			  0xfc, 0xff, 0xff, 0x3f, 0xf0, 0xff, 0xff, 0x3f,
			  0x00, 0x00, 0xf8, 0x07, 0x00, 0x00, 0x00, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0x0f, 0x7e, 0x00, 0x00}},
			{{0x00, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0x1f, 0x00, 0x00, 0xfe, 0x07, 0x00},
			 {0x00, 0x00, 0x00, 0xf0, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xfb, 0xff, 0x07, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x60}},
			{{0xff, 0x01, 0x00, 0xff, 0xff, 0xff, 0x0f, 0x00,
			  0x80, 0x7f, 0xfe, 0xff, 0xff, 0xff, 0xff, 0x03,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x80, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff},
			 {0xff, 0xff, 0x1f, 0x00, 0xf0, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0x3f, 0x00, 0x00, 0x00, 0x00}},
			{{0x80, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xf1, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x03,
			  0x00, 0x00, 0x00, 0xe0, 0xff, 0xff, 0xff, 0xff}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x7e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0xc0, 0xff, 0xff, 0xcf, 0xff, 0x1f, 0x00, 0x00,
			  0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0xe0, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x3f, 0x00, 0x7e,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}},
			{{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0xfc, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7c, 0x00},
			 {0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0x7f, 0x00, 0x80, 0x00, 0x00, 0x00,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x00, 0x00, 0xe0, 0xff, 0xff, 0xff, 0xff, 0xff}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0x1f, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00},
			 {0xf0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0x3f, 0x00, 0x00, 0x80,
			  0xff, 0x01, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff,
			  0xff, 0x7f, 0xf8, 0xff, 0xff, 0x1f, 0x00, 0xfe}},
			{{0xff, 0xff, 0xff, 0x3f, 0xf8, 0xff, 0xff, 0xff,
			  0xff, 0x03, 0xfe, 0x01, 0x00, 0x00, 0x00, 0x00,
			  0xf0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x07},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0x01, 0x80, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00}},
			{{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe,
			  0xba, 0xae, 0xdc, 0xe6, 0xaf, 0x48, 0xa0, 0x3b,
			  0xbf, 0xd2, 0x5e, 0x8c, 0xd0, 0x36, 0x41, 0x40}},
			{{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}},
			{{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff},
			 {0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0xc0,
			  0xff, 0x0f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0xf0, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01, 0x00,
			  0xf0, 0xff, 0xff, 0xff, 0xff, 0x07, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0xfe, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0x01, 0xff, 0xff, 0xff}},
			{{0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe,
			  0xba, 0xae, 0xdc, 0xe6, 0xaf, 0x48, 0xa0, 0x3b,
			  0xbf, 0xd2, 0x5e, 0x8c, 0xd0, 0x36, 0x41, 0x40},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0x7e, 0x00, 0x00, 0xc0, 0xff, 0xff, 0x07, 0x00,
			  0x80, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
			  0xfc, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff},
			 {0xff, 0x01, 0x00, 0x00, 0x00, 0xe0, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x1f, 0x00, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x03, 0x00, 0x00,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff}},
			{{0xff, 0xff, 0xf0, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0xf0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x00, 0xe0, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01,
			  0x80, 0x00, 0x00, 0x80, 0xff, 0xff, 0xff, 0xff},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0xe0, 0xff, 0xff,
			  0xff, 0xff, 0x3f, 0x00, 0xf8, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0x3f, 0x00, 0x00, 0xc0, 0xf1, 0x7f, 0x00}},
			{{0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0xc0, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x80, 0x00, 0x00, 0x80, 0xff, 0xff, 0xff, 0x00},
			 {0x00, 0xf8, 0xff, 0xff, 0xff, 0xff, 0xff, 0x01,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xf8, 0xff,
			  0xff, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x80, 0x1f,
			  0x00, 0x00, 0xfc, 0xff, 0xff, 0x01, 0xff, 0xff}},
			{{0x00, 0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x80, 0x00, 0x00, 0x80, 0xff, 0x03, 0xe0, 0x01,
			  0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0xfc, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00},
			 {0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00,
			  0xfe, 0xff, 0xff, 0xf0, 0x07, 0x00, 0x3c, 0x80,
			  0xff, 0xff, 0xff, 0xff, 0xfc, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0x07, 0xe0, 0xff, 0x00, 0x00, 0x00}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0xfc, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x07, 0xf8,
			  0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80},
			 {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x0c, 0x80, 0x00,
			  0x00, 0x00, 0x00, 0xc0, 0x7f, 0xfe, 0xff, 0x1f,
			  0x00, 0xfe, 0xff, 0x03, 0x00, 0x00, 0xfe, 0xff}},
			{{0xff, 0xff, 0x81, 0xff, 0xff, 0xff, 0xff, 0x00,
			  0x80, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x83,
			  0xff, 0xff, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80,
			  0xff, 0xff, 0x7f, 0x00, 0x00, 0x00, 0x00, 0xf0},
			 {0xff, 0x01, 0x00, 0x00, 0x00, 0x00, 0xf8, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x1f, 0x00, 0x00,
			  0xf8, 0x07, 0x00, 0x80, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xc7, 0xff, 0xff, 0xe0, 0xff, 0xff, 0xff}},
			{{0x82, 0xc9, 0xfa, 0xb0, 0x68, 0x04, 0xa0, 0x00,
			  0x82, 0xc9, 0xfa, 0xb0, 0x68, 0x04, 0xa0, 0x00,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x6f, 0x03, 0xfb,
			  0xfa, 0x8a, 0x7d, 0xdf, 0x13, 0x86, 0xe2, 0x03},
			 {0x82, 0xc9, 0xfa, 0xb0, 0x68, 0x04, 0xa0, 0x00,
			  0x82, 0xc9, 0xfa, 0xb0, 0x68, 0x04, 0xa0, 0x00,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0x6f, 0x03, 0xfb,
			  0xfa, 0x8a, 0x7d, 0xdf, 0x13, 0x86, 0xe2, 0x03}}
		};

				byte[,,] res = {
			{{0x0c, 0x3b, 0x0a, 0xca, 0x8d, 0x1a, 0x2f, 0xb9,
			  0x8a, 0x7b, 0x53, 0x5a, 0x1f, 0xc5, 0x22, 0xa1,
			  0x07, 0x2a, 0x48, 0xea, 0x02, 0xeb, 0xb3, 0xd6,
			  0x20, 0x1e, 0x86, 0xd0, 0x95, 0xf6, 0x92, 0x35},
			 {0xdc, 0x90, 0x7a, 0x07, 0x2e, 0x1e, 0x44, 0x6d,
			  0xf8, 0x15, 0x24, 0x5b, 0x5a, 0x96, 0x37, 0x9c,
			  0x37, 0x7b, 0x0d, 0xac, 0x1b, 0x65, 0x58, 0x49,
			  0x43, 0xb7, 0x31, 0xbb, 0xa7, 0xf4, 0x97, 0x15}},
			{{0xf1, 0xf7, 0x3a, 0x50, 0xe6, 0x10, 0xba, 0x22,
			  0x43, 0x4d, 0x1f, 0x1f, 0x7c, 0x27, 0xca, 0x9c,
			  0xb8, 0xb6, 0xa0, 0xfc, 0xd8, 0xc0, 0x05, 0x2f,
			  0xf7, 0x08, 0xe1, 0x76, 0xdd, 0xd0, 0x80, 0xc8},
			 {0xe3, 0x80, 0x80, 0xb8, 0xdb, 0xe3, 0xa9, 0x77,
			  0x00, 0xb0, 0xf5, 0x2e, 0x27, 0xe2, 0x68, 0xc4,
			  0x88, 0xe8, 0x04, 0xc1, 0x12, 0xbf, 0x78, 0x59,
			  0xe6, 0xa9, 0x7c, 0xe1, 0x81, 0xdd, 0xb9, 0xd5}},
			{{0x96, 0xe2, 0xee, 0x01, 0xa6, 0x80, 0x31, 0xef,
			  0x5c, 0xd0, 0x19, 0xb4, 0x7d, 0x5f, 0x79, 0xab,
			  0xa1, 0x97, 0xd3, 0x7e, 0x33, 0xbb, 0x86, 0x55,
			  0x60, 0x20, 0x10, 0x0d, 0x94, 0x2d, 0x11, 0x7c},
			 {0xcc, 0xab, 0xe0, 0xe8, 0x98, 0x65, 0x12, 0x96,
			  0x38, 0x5a, 0x1a, 0xf2, 0x85, 0x23, 0x59, 0x5f,
			  0xf9, 0xf3, 0xc2, 0x81, 0x70, 0x92, 0x65, 0x12,
			  0x9c, 0x65, 0x1e, 0x96, 0x00, 0xef, 0xe7, 0x63}},
			{{0xac, 0x1e, 0x62, 0xc2, 0x59, 0xfc, 0x4e, 0x5c,
			  0x83, 0xb0, 0xd0, 0x6f, 0xce, 0x19, 0xf6, 0xbf,
			  0xa4, 0xb0, 0xe0, 0x53, 0x66, 0x1f, 0xbf, 0xc9,
			  0x33, 0x47, 0x37, 0xa9, 0x3d, 0x5d, 0xb0, 0x48},
			 {0x86, 0xb9, 0x2a, 0x7f, 0x8e, 0xa8, 0x60, 0x42,
			  0x26, 0x6d, 0x6e, 0x1c, 0xa2, 0xec, 0xe0, 0xe5,
			  0x3e, 0x0a, 0x33, 0xbb, 0x61, 0x4c, 0x9f, 0x3c,
			  0xd1, 0xdf, 0x49, 0x33, 0xcd, 0x72, 0x78, 0x18}},
			{{0xf7, 0xd3, 0xcd, 0x49, 0x5c, 0x13, 0x22, 0xfb,
			  0x2e, 0xb2, 0x2f, 0x27, 0xf5, 0x8a, 0x5d, 0x74,
			  0xc1, 0x58, 0xc5, 0xc2, 0x2d, 0x9f, 0x52, 0xc6,
			  0x63, 0x9f, 0xba, 0x05, 0x76, 0x45, 0x7a, 0x63},
			 {0x8a, 0xfa, 0x55, 0x4d, 0xdd, 0xa3, 0xb2, 0xc3,
			  0x44, 0xfd, 0xec, 0x72, 0xde, 0xef, 0xc0, 0x99,
			  0xf5, 0x9f, 0xe2, 0x52, 0xb4, 0x05, 0x32, 0x58,
			  0x57, 0xc1, 0x8f, 0xea, 0xc3, 0x24, 0x5b, 0x94}},
			{{0x05, 0x83, 0xee, 0xdd, 0x64, 0xf0, 0x14, 0x3b,
			  0xa0, 0x14, 0x4a, 0x3a, 0x41, 0x82, 0x7c, 0xa7,
			  0x2c, 0xaa, 0xb1, 0x76, 0xbb, 0x59, 0x64, 0x5f,
			  0x52, 0xad, 0x25, 0x29, 0x9d, 0x8f, 0x0b, 0xb0},
			 {0x7e, 0xe3, 0x7c, 0xca, 0xcd, 0x4f, 0xb0, 0x6d,
			  0x7a, 0xb2, 0x3e, 0xa0, 0x08, 0xb9, 0xa8, 0x2d,
			  0xc2, 0xf4, 0x99, 0x66, 0xcc, 0xac, 0xd8, 0xb9,
			  0x72, 0x2a, 0x4a, 0x3e, 0x0f, 0x7b, 0xbf, 0xf4}},
			{{0x8c, 0x9c, 0x78, 0x2b, 0x39, 0x61, 0x7e, 0xf7,
			  0x65, 0x37, 0x66, 0x09, 0x38, 0xb9, 0x6f, 0x70,
			  0x78, 0x87, 0xff, 0xcf, 0x93, 0xca, 0x85, 0x06,
			  0x44, 0x84, 0xa7, 0xfe, 0xd3, 0xa4, 0xe3, 0x7e},
			 {0xa2, 0x56, 0x49, 0x23, 0x54, 0xa5, 0x50, 0xe9,
			  0x5f, 0xf0, 0x4d, 0xe7, 0xdc, 0x38, 0x32, 0x79,
			  0x4f, 0x1c, 0xb7, 0xe4, 0xbb, 0xf8, 0xbb, 0x2e,
			  0x40, 0x41, 0x4b, 0xcc, 0xe3, 0x1e, 0x16, 0x36}},
			{{0x0c, 0x1e, 0xd7, 0x09, 0x25, 0x40, 0x97, 0xcb,
			  0x5c, 0x46, 0xa8, 0xda, 0xef, 0x25, 0xd5, 0xe5,
			  0x92, 0x4d, 0xcf, 0xa3, 0xc4, 0x5d, 0x35, 0x4a,
			  0xe4, 0x61, 0x92, 0xf3, 0xbf, 0x0e, 0xcd, 0xbe},
			 {0xe4, 0xaf, 0x0a, 0xb3, 0x30, 0x8b, 0x9b, 0x48,
			  0x49, 0x43, 0xc7, 0x64, 0x60, 0x4a, 0x2b, 0x9e,
			  0x95, 0x5f, 0x56, 0xe8, 0x35, 0xdc, 0xeb, 0xdc,
			  0xc7, 0xc4, 0xfe, 0x30, 0x40, 0xc7, 0xbf, 0xa4}},
			{{0xd4, 0xa0, 0xf5, 0x81, 0x49, 0x6b, 0xb6, 0x8b,
			  0x0a, 0x69, 0xf9, 0xfe, 0xa8, 0x32, 0xe5, 0xe0,
			  0xa5, 0xcd, 0x02, 0x53, 0xf9, 0x2c, 0xe3, 0x53,
			  0x83, 0x36, 0xc6, 0x02, 0xb5, 0xeb, 0x64, 0xb8},
			 {0x1d, 0x42, 0xb9, 0xf9, 0xe9, 0xe3, 0x93, 0x2c,
			  0x4c, 0xee, 0x6c, 0x5a, 0x47, 0x9e, 0x62, 0x01,
			  0x6b, 0x04, 0xfe, 0xa4, 0x30, 0x2b, 0x0d, 0x4f,
			  0x71, 0x10, 0xd3, 0x55, 0xca, 0xf3, 0x5e, 0x80}},
			{{0x77, 0x05, 0xf6, 0x0c, 0x15, 0x9b, 0x45, 0xe7,
			  0xb9, 0x11, 0xb8, 0xf5, 0xd6, 0xda, 0x73, 0x0c,
			  0xda, 0x92, 0xea, 0xd0, 0x9d, 0xd0, 0x18, 0x92,
			  0xce, 0x9a, 0xaa, 0xee, 0x0f, 0xef, 0xde, 0x30},
			 {0xf1, 0xf1, 0xd6, 0x9b, 0x51, 0xd7, 0x77, 0x62,
			  0x52, 0x10, 0xb8, 0x7a, 0x84, 0x9d, 0x15, 0x4e,
			  0x07, 0xdc, 0x1e, 0x75, 0x0d, 0x0c, 0x3b, 0xdb,
			  0x74, 0x58, 0x62, 0x02, 0x90, 0x54, 0x8b, 0x43}},
			{{0xa6, 0xfe, 0x0b, 0x87, 0x80, 0x43, 0x67, 0x25,
			  0x57, 0x5d, 0xec, 0x40, 0x50, 0x08, 0xd5, 0x5d,
			  0x43, 0xd7, 0xe0, 0xaa, 0xe0, 0x13, 0xb6, 0xb0,
			  0xc0, 0xd4, 0xe5, 0x0d, 0x45, 0x83, 0xd6, 0x13},
			 {0x40, 0x45, 0x0a, 0x92, 0x31, 0xea, 0x8c, 0x60,
			  0x8c, 0x1f, 0xd8, 0x76, 0x45, 0xb9, 0x29, 0x00,
			  0x26, 0x32, 0xd8, 0xa6, 0x96, 0x88, 0xe2, 0xc4,
			  0x8b, 0xdb, 0x7f, 0x17, 0x87, 0xcc, 0xc8, 0xf2}},
			{{0xc2, 0x56, 0xe2, 0xb6, 0x1a, 0x81, 0xe7, 0x31,
			  0x63, 0x2e, 0xbb, 0x0d, 0x2f, 0x81, 0x67, 0xd4,
			  0x22, 0xe2, 0x38, 0x02, 0x25, 0x97, 0xc7, 0x88,
			  0x6e, 0xdf, 0xbe, 0x2a, 0xa5, 0x73, 0x63, 0xaa},
			 {0x50, 0x45, 0xe2, 0xc3, 0xbd, 0x89, 0xfc, 0x57,
			  0xbd, 0x3c, 0xa3, 0x98, 0x7e, 0x7f, 0x36, 0x38,
			  0x92, 0x39, 0x1f, 0x0f, 0x81, 0x1a, 0x06, 0x51,
			  0x1f, 0x8d, 0x6a, 0xff, 0x47, 0x16, 0x06, 0x9c}},
			{{0x33, 0x95, 0xa2, 0x6f, 0x27, 0x5f, 0x9c, 0x9c,
			  0x64, 0x45, 0xcb, 0xd1, 0x3c, 0xee, 0x5e, 0x5f,
			  0x48, 0xa6, 0xaf, 0xe3, 0x79, 0xcf, 0xb1, 0xe2,
			  0xbf, 0x55, 0x0e, 0xa2, 0x3b, 0x62, 0xf0, 0xe4},
			 {0x14, 0xe8, 0x06, 0xe3, 0xbe, 0x7e, 0x67, 0x01,
			  0xc5, 0x21, 0x67, 0xd8, 0x54, 0xb5, 0x7f, 0xa4,
			  0xf9, 0x75, 0x70, 0x1c, 0xfd, 0x79, 0xdb, 0x86,
			  0xad, 0x37, 0x85, 0x83, 0x56, 0x4e, 0xf0, 0xbf}},
			{{0xbc, 0xa6, 0xe0, 0x56, 0x4e, 0xef, 0xfa, 0xf5,
			  0x1d, 0x5d, 0x3f, 0x2a, 0x5b, 0x19, 0xab, 0x51,
			  0xc5, 0x8b, 0xdd, 0x98, 0x28, 0x35, 0x2f, 0xc3,
			  0x81, 0x4f, 0x5c, 0xe5, 0x70, 0xb9, 0xeb, 0x62},
			 {0xc4, 0x6d, 0x26, 0xb0, 0x17, 0x6b, 0xfe, 0x6c,
			  0x12, 0xf8, 0xe7, 0xc1, 0xf5, 0x2f, 0xfa, 0x91,
			  0x13, 0x27, 0xbd, 0x73, 0xcc, 0x33, 0x31, 0x1c,
			  0x39, 0xe3, 0x27, 0x6a, 0x95, 0xcf, 0xc5, 0xfb}},
			{{0x30, 0xb2, 0x99, 0x84, 0xf0, 0x18, 0x2a, 0x6e,
			  0x1e, 0x27, 0xed, 0xa2, 0x29, 0x99, 0x41, 0x56,
			  0xe8, 0xd4, 0x0d, 0xef, 0x99, 0x9c, 0xf3, 0x58,
			  0x29, 0x55, 0x1a, 0xc0, 0x68, 0xd6, 0x74, 0xa4},
			 {0x07, 0x9c, 0xe7, 0xec, 0xf5, 0x36, 0x73, 0x41,
			  0xa3, 0x1c, 0xe5, 0x93, 0x97, 0x6a, 0xfd, 0xf7,
			  0x53, 0x18, 0xab, 0xaf, 0xeb, 0x85, 0xbd, 0x92,
			  0x90, 0xab, 0x3c, 0xbf, 0x30, 0x82, 0xad, 0xf6}},
			{{0xc6, 0x87, 0x8a, 0x2a, 0xea, 0xc0, 0xa9, 0xec,
			  0x6d, 0xd3, 0xdc, 0x32, 0x23, 0xce, 0x62, 0x19,
			  0xa4, 0x7e, 0xa8, 0xdd, 0x1c, 0x33, 0xae, 0xd3,
			  0x4f, 0x62, 0x9f, 0x52, 0xe7, 0x65, 0x46, 0xf4},
			 {0x97, 0x51, 0x27, 0x67, 0x2d, 0xa2, 0x82, 0x87,
			  0x98, 0xd3, 0xb6, 0x14, 0x7f, 0x51, 0xd3, 0x9a,
			  0x0b, 0xd0, 0x76, 0x81, 0xb2, 0x4f, 0x58, 0x92,
			  0xa4, 0x86, 0xa1, 0xa7, 0x09, 0x1d, 0xef, 0x9b}},
			{{0xb3, 0x0f, 0x2b, 0x69, 0x0d, 0x06, 0x90, 0x64,
			  0xbd, 0x43, 0x4c, 0x10, 0xe8, 0x98, 0x1c, 0xa3,
			  0xe1, 0x68, 0xe9, 0x79, 0x6c, 0x29, 0x51, 0x3f,
			  0x41, 0xdc, 0xdf, 0x1f, 0xf3, 0x60, 0xbe, 0x33},
			 {0xa1, 0x5f, 0xf7, 0x1d, 0xb4, 0x3e, 0x9b, 0x3c,
			  0xe7, 0xbd, 0xb6, 0x06, 0xd5, 0x60, 0x06, 0x6d,
			  0x50, 0xd2, 0xf4, 0x1a, 0x31, 0x08, 0xf2, 0xea,
			  0x8e, 0xef, 0x5f, 0x7d, 0xb6, 0xd0, 0xc0, 0x27}},
			{{0x62, 0x9a, 0xd9, 0xbb, 0x38, 0x36, 0xce, 0xf7,
			  0x5d, 0x2f, 0x13, 0xec, 0xc8, 0x2d, 0x02, 0x8a,
			  0x2e, 0x72, 0xf0, 0xe5, 0x15, 0x9d, 0x72, 0xae,
			  0xfc, 0xb3, 0x4f, 0x02, 0xea, 0xe1, 0x09, 0xfe},
			 {0x00, 0x00, 0x00, 0x00, 0xfa, 0x0a, 0x3d, 0xbc,
			  0xad, 0x16, 0x0c, 0xb6, 0xe7, 0x7c, 0x8b, 0x39,
			  0x9a, 0x43, 0xbb, 0xe3, 0xc2, 0x55, 0x15, 0x14,
			  0x75, 0xac, 0x90, 0x9b, 0x7f, 0x9a, 0x92, 0x00}},
			{{0x8b, 0xac, 0x70, 0x86, 0x29, 0x8f, 0x00, 0x23,
			  0x7b, 0x45, 0x30, 0xaa, 0xb8, 0x4c, 0xc7, 0x8d,
			  0x4e, 0x47, 0x85, 0xc6, 0x19, 0xe3, 0x96, 0xc2,
			  0x9a, 0xa0, 0x12, 0xed, 0x6f, 0xd7, 0x76, 0x16},
			 {0x45, 0xaf, 0x7e, 0x33, 0xc7, 0x7f, 0x10, 0x6c,
			  0x7c, 0x9f, 0x29, 0xc1, 0xa8, 0x7e, 0x15, 0x84,
			  0xe7, 0x7d, 0xc0, 0x6d, 0xab, 0x71, 0x5d, 0xd0,
			  0x6b, 0x9f, 0x97, 0xab, 0xcb, 0x51, 0x0c, 0x9f}},
			{{0x9e, 0xc3, 0x92, 0xb4, 0x04, 0x9f, 0xc8, 0xbb,
			  0xdd, 0x9e, 0xc6, 0x05, 0xfd, 0x65, 0xec, 0x94,
			  0x7f, 0x2c, 0x16, 0xc4, 0x40, 0xac, 0x63, 0x7b,
			  0x7d, 0xb8, 0x0c, 0xe4, 0x5b, 0xe3, 0xa7, 0x0e},
			 {0x43, 0xf4, 0x44, 0xe8, 0xcc, 0xc8, 0xd4, 0x54,
			  0x33, 0x37, 0x50, 0xf2, 0x87, 0x42, 0x2e, 0x00,
			  0x49, 0x60, 0x62, 0x02, 0xfd, 0x1a, 0x7c, 0xdb,
			  0x29, 0x6c, 0x6d, 0x54, 0x53, 0x08, 0xd1, 0xc8}},
			{{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}},
			{{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01}},
			{{0x27, 0x59, 0xc7, 0x35, 0x60, 0x71, 0xa6, 0xf1,
			  0x79, 0xa5, 0xfd, 0x79, 0x16, 0xf3, 0x41, 0xf0,
			  0x57, 0xb4, 0x02, 0x97, 0x32, 0xe7, 0xde, 0x59,
			  0xe2, 0x2d, 0x9b, 0x11, 0xea, 0x2c, 0x35, 0x92},
			 {0x27, 0x59, 0xc7, 0x35, 0x60, 0x71, 0xa6, 0xf1,
			  0x79, 0xa5, 0xfd, 0x79, 0x16, 0xf3, 0x41, 0xf0,
			  0x57, 0xb4, 0x02, 0x97, 0x32, 0xe7, 0xde, 0x59,
			  0xe2, 0x2d, 0x9b, 0x11, 0xea, 0x2c, 0x35, 0x92}},
			{{0x28, 0x56, 0xac, 0x0e, 0x4f, 0x98, 0x09, 0xf0,
			  0x49, 0xfa, 0x7f, 0x84, 0xac, 0x7e, 0x50, 0x5b,
			  0x17, 0x43, 0x14, 0x89, 0x9c, 0x53, 0xa8, 0x94,
			  0x30, 0xf2, 0x11, 0x4d, 0x92, 0x14, 0x27, 0xe8},
			 {0x39, 0x7a, 0x84, 0x56, 0x79, 0x9d, 0xec, 0x26,
			  0x2c, 0x53, 0xc1, 0x94, 0xc9, 0x8d, 0x9e, 0x9d,
			  0x32, 0x1f, 0xdd, 0x84, 0x04, 0xe8, 0xe2, 0x0a,
			  0x6b, 0xbe, 0xbb, 0x42, 0x40, 0x67, 0x30, 0x6c}},
			{{0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
			  0x45, 0x51, 0x23, 0x19, 0x50, 0xb7, 0x5f, 0xc4,
			  0x40, 0x2d, 0xa1, 0x73, 0x2f, 0xc9, 0xbe, 0xbd},
			 {0x27, 0x59, 0xc7, 0x35, 0x60, 0x71, 0xa6, 0xf1,
			  0x79, 0xa5, 0xfd, 0x79, 0x16, 0xf3, 0x41, 0xf0,
			  0x57, 0xb4, 0x02, 0x97, 0x32, 0xe7, 0xde, 0x59,
			  0xe2, 0x2d, 0x9b, 0x11, 0xea, 0x2c, 0x35, 0x92}},
			{{0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			  0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe,
			  0xba, 0xae, 0xdc, 0xe6, 0xaf, 0x48, 0xa0, 0x3b,
			  0xbf, 0xd2, 0x5e, 0x8c, 0xd0, 0x36, 0x41, 0x40},
			 {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01}},
			{{0x1c, 0xc4, 0xf7, 0xda, 0x0f, 0x65, 0xca, 0x39,
			  0x70, 0x52, 0x92, 0x8e, 0xc3, 0xc8, 0x15, 0xea,
			  0x7f, 0x10, 0x9e, 0x77, 0x4b, 0x6e, 0x2d, 0xdf,
			  0xe8, 0x30, 0x9d, 0xda, 0xe8, 0x9a, 0x65, 0xae},
			 {0x02, 0xb0, 0x16, 0xb1, 0x1d, 0xc8, 0x57, 0x7b,
			  0xa2, 0x3a, 0xa2, 0xa3, 0x38, 0x5c, 0x8f, 0xeb,
			  0x66, 0x37, 0x91, 0xa8, 0x5f, 0xef, 0x04, 0xf6,
			  0x59, 0x75, 0xe1, 0xee, 0x92, 0xf6, 0x0e, 0x30}},
			{{0x8d, 0x76, 0x14, 0xa4, 0x14, 0x06, 0x9f, 0x9a,
			  0xdf, 0x4a, 0x85, 0xa7, 0x6b, 0xbf, 0x29, 0x6f,
			  0xbc, 0x34, 0x87, 0x5d, 0xeb, 0xbb, 0x2e, 0xa9,
			  0xc9, 0x1f, 0x58, 0xd6, 0x9a, 0x82, 0xa0, 0x56},
			 {0xd4, 0xb9, 0xdb, 0x88, 0x1d, 0x04, 0xe9, 0x93,
			  0x8d, 0x3f, 0x20, 0xd5, 0x86, 0xa8, 0x83, 0x07,
			  0xdb, 0x09, 0xd8, 0x22, 0x1f, 0x7f, 0xf1, 0x71,
			  0xc8, 0xe7, 0x5d, 0x47, 0xaf, 0x8b, 0x72, 0xe9}},
			{{0x83, 0xb9, 0x39, 0xb2, 0xa4, 0xdf, 0x46, 0x87,
			  0xc2, 0xb8, 0xf1, 0xe6, 0x4c, 0xd1, 0xe2, 0xa9,
			  0xe4, 0x70, 0x30, 0x34, 0xbc, 0x52, 0x7c, 0x55,
			  0xa6, 0xec, 0x80, 0xa4, 0xe5, 0xd2, 0xdc, 0x73},
			 {0x08, 0xf1, 0x03, 0xcf, 0x16, 0x73, 0xe8, 0x7d,
			  0xb6, 0x7e, 0x9b, 0xc0, 0xb4, 0xc2, 0xa5, 0x86,
			  0x02, 0x77, 0xd5, 0x27, 0x86, 0xa5, 0x15, 0xfb,
			  0xae, 0x9b, 0x8c, 0xa9, 0xf9, 0xf8, 0xa8, 0x4a}},
			{{0x8b, 0x00, 0x49, 0xdb, 0xfa, 0xf0, 0x1b, 0xa2,
			  0xed, 0x8a, 0x9a, 0x7a, 0x36, 0x78, 0x4a, 0xc7,
			  0xf7, 0xad, 0x39, 0xd0, 0x6c, 0x65, 0x7a, 0x41,
			  0xce, 0xd6, 0xd6, 0x4c, 0x20, 0x21, 0x6b, 0xc7},
			 {0xc6, 0xca, 0x78, 0x1d, 0x32, 0x6c, 0x6c, 0x06,
			  0x91, 0xf2, 0x1a, 0xe8, 0x43, 0x16, 0xea, 0x04,
			  0x3c, 0x1f, 0x07, 0x85, 0xf7, 0x09, 0x22, 0x08,
			  0xba, 0x13, 0xfd, 0x78, 0x1e, 0x3f, 0x6f, 0x62}},
			{{0x25, 0x9b, 0x7c, 0xb0, 0xac, 0x72, 0x6f, 0xb2,
			  0xe3, 0x53, 0x84, 0x7a, 0x1a, 0x9a, 0x98, 0x9b,
			  0x44, 0xd3, 0x59, 0xd0, 0x8e, 0x57, 0x41, 0x40,
			  0x78, 0xa7, 0x30, 0x2f, 0x4c, 0x9c, 0xb9, 0x68},
			 {0xb7, 0x75, 0x03, 0x63, 0x61, 0xc2, 0x48, 0x6e,
			  0x12, 0x3d, 0xbf, 0x4b, 0x27, 0xdf, 0xb1, 0x7a,
			  0xff, 0x4e, 0x31, 0x07, 0x83, 0xf4, 0x62, 0x5b,
			  0x19, 0xa5, 0xac, 0xa0, 0x32, 0x58, 0x0d, 0xa7}},
			{{0x43, 0x4f, 0x10, 0xa4, 0xca, 0xdb, 0x38, 0x67,
			  0xfa, 0xae, 0x96, 0xb5, 0x6d, 0x97, 0xff, 0x1f,
			  0xb6, 0x83, 0x43, 0xd3, 0xa0, 0x2d, 0x70, 0x7a,
			  0x64, 0x05, 0x4c, 0xa7, 0xc1, 0xa5, 0x21, 0x51},
			 {0xe4, 0xf1, 0x23, 0x84, 0xe1, 0xb5, 0x9d, 0xf2,
			  0xb8, 0x73, 0x8b, 0x45, 0x2b, 0x35, 0x46, 0x38,
			  0x10, 0x2b, 0x50, 0xf8, 0x8b, 0x35, 0xcd, 0x34,
			  0xc8, 0x0e, 0xf6, 0xdb, 0x09, 0x35, 0xf0, 0xda}},
			{{0xdb, 0x21, 0x5c, 0x8d, 0x83, 0x1d, 0xb3, 0x34,
			  0xc7, 0x0e, 0x43, 0xa1, 0x58, 0x79, 0x67, 0x13,
			  0x1e, 0x86, 0x5d, 0x89, 0x63, 0xe6, 0x0a, 0x46,
			  0x5c, 0x02, 0x97, 0x1b, 0x62, 0x43, 0x86, 0xf5},
			 {0xdb, 0x21, 0x5c, 0x8d, 0x83, 0x1d, 0xb3, 0x34,
			  0xc7, 0x0e, 0x43, 0xa1, 0x58, 0x79, 0x67, 0x13,
			  0x1e, 0x86, 0x5d, 0x89, 0x63, 0xe6, 0x0a, 0x46,
			  0x5c, 0x02, 0x97, 0x1b, 0x62, 0x43, 0x86, 0xf5}}
		};
				Scalar one = new Scalar(1);
				int overflow;
				for (i = 0; i < 32; i++)
				{
					Scalar x = new Scalar(GetArray(chal, i, 0), out overflow);
					Assert.True(overflow == 0);
					Scalar y = new Scalar(GetArray(chal, i, 1), out overflow);
					Assert.True(overflow == 0);
					Scalar r1 = new Scalar(GetArray(res, i, 0), out overflow);
					Assert.True(overflow == 0);
					Scalar r2 = new Scalar(GetArray(res, i, 1), out overflow);
					Assert.True(overflow == 0);
					Scalar z = x * y;
					Assert.False(z.IsOverflow);
					Assert.Equal(r1, z);
					Scalar zz;
					if (!y.IsZero)
					{
						zz = y.Inverse();
						Assert.False(z.IsOverflow);
						z = z * zz;
						Assert.False(z.IsOverflow);
						Assert.Equal(x, z);
						zz = zz * y;
						Assert.False(z.IsOverflow);
						Assert.Equal(one, zz);
					}
					z = x * x;
					Assert.False(z.IsOverflow);
					zz = x.Sqr();
					Assert.False(z.IsOverflow);
					Assert.Equal(zz, z);
					Assert.Equal(r2, zz);
				}
			}
		}

		private byte[] GetArray(byte[,,] chal, int i0, int i1)
		{
			byte[] bytes = new byte[chal.GetLength(2)];
			for (int i = 0; i < bytes.Length; i++)
			{
				bytes[i] = (byte)chal.GetValue(i0, i1, i);
			}
			return bytes;
		}

		void scalar_test()
		{
			Span<byte> c = stackalloc byte[32];
			var s = random_scalar_order_test();
			var s1 = random_scalar_order_test();
			var s2 = random_scalar_order_test();

			s2.WriteToSpan(c);

			{
				int i;
				/* Test that fetching groups of 4 bits from a scalar and recursing n(i)=16*n(i-1)+p(i) reconstructs it. */
				Scalar n = Scalar.Zero;
				for (i = 0; i < 256; i += 4)
				{
					Scalar t = new Scalar(s.GetBits(256 - 4 - i, 4));
					int j;
					for (j = 0; j < 4; j++)
					{
						n = n + n;
					}
					n = n + t;
				}
				Assert.Equal(n, s);
			}

			{
				/* Test that fetching groups of randomly-sized bits from a scalar and recursing n(i)=b*n(i-1)+p(i) reconstructs it. */
				Scalar n = Scalar.Zero;
				int i = 0;
				while (i < 256)
				{
					int j;
					int now = (int)(secp256k1_rand_int(15) + 1);
					if (now + i > 256)
					{
						now = 256 - i;
					}
					Scalar t = new Scalar(s.GetBitsVariable(256 - now - i, now));
					for (j = 0; j < now; j++)
					{
						n = n + n;
					}
					n = n + t;
					i += now;
				}
				Assert.Equal(n, s);
			}


			{
				/* Test that scalar inverses are equal to the inverse of their number modulo the order. */
				if (!s.IsZero)
				{
					var inv = s.Inverse();
					inv = inv * s;
					/* Multiplying a scalar with its inverse must result in one. */
					Assert.True(inv.IsOne);
					inv = inv.Inverse();
					/* Inverting one must result in one. */
					Assert.True(inv.IsOne);
				}
			}

			{
				/* Test commutativity of add. */
				Scalar r1 = s1 + s2;
				Scalar r2 = s2 + s1;
				Assert.Equal(r1, r2);
			}

			{
				int i;
				/* Test add_bit. */
				uint bit = secp256k1_rand_bits(8);
				Scalar b = new Scalar(1);
				Assert.True(b.IsOne);
				for (i = 0; i < bit; i++)
				{
					b = b + b;
				}
				Scalar r1 = s1;
				Scalar r2 = s1;
				r1 = r1.Add(b, out var overflow);
				if (overflow == 0)
				{
					/* No overflow happened. */
					r2 = r2.CAddBit(bit, 1);
					Assert.Equal(r1, r2);
					/* cadd is a noop when flag is zero */
					r2 = r2.CAddBit(bit, 0);
					Assert.Equal(r1, r2);
				}
			}
			{
				/* Test commutativity of mul. */
				Scalar r1 = s1 * s2;
				Scalar r2 = s2 * s1;
				Assert.Equal(r1, r2);
			}

			{
				/* Test associativity of add. */
				Scalar r1 = s1 + s2;
				r1 = r1 + s;
				Scalar r2 = s2 + s;
				r2 = s1 + r2;
				Assert.Equal(r1, r2);
			}

			{
				/* Test associativity of mul. */
				Scalar r1 = s1 * s2;
				r1 = r1 * s;
				Scalar r2 = s2 * s;
				r2 = s1 * r2;
				Assert.Equal(r1, r2);
			}

			{
				/* Test distributitivity of mul over add. */
				Scalar r1 = s1 + s2;
				r1 = r1 * s;
				Scalar r2 = s1 * s;
				Scalar t = s2 * s;
				r2 = r2 + t;
				Assert.Equal(r1, r2);
			}

			{
				/* Test square. */
				Scalar r1 = s1.Sqr();
				Scalar r2 = s1 * s1;
				Assert.Equal(r1, r2);
			}

			{
				/* Test multiplicative identity. */
				Scalar v1 = new Scalar(1);
				Scalar r1 = s1 * v1;
				Assert.Equal(r1, s1);
			}

			{
				/* Test additive identity. */
				Scalar v0 = new Scalar(0);
				Scalar r1 = s1 + v0;
				Assert.Equal(r1, s1);
			}

			{
				/* Test zero product property. */
				Scalar v0 = new Scalar(0);
				Scalar r1 = s1 * v0;
				Assert.Equal(r1, v0);
			}
		}

		void test_wnaf(in Scalar number, int w)
		{

			Scalar x, two, t;
			Span<int> wnaf = stackalloc int[256];
			int zeroes = -1;
			int i;
			int bits;
			x = new Scalar(0);
			two = new Scalar(2);
			bits = ECMultContext.secp256k1_ecmult_wnaf(wnaf, 256, number, w);
			Assert.True(bits <= 256);
			for (i = bits - 1; i >= 0; i--)
			{
				int v = wnaf[i];
				x *= two;
				if (v != 0)
				{
					Assert.True(zeroes == -1 || zeroes >= w - 1); /* Assert.True that distance between non-zero elements is at least w-1 */
					zeroes = 0;
					Assert.True((v & 1) == 1); /* Assert.True non-zero elements are odd */
					Assert.True(v <= (1 << (w - 1)) - 1); /* Assert.True range below */
					Assert.True(v >= -(1 << (w - 1)) - 1); /* Assert.True range above */
				}
				else
				{
					Assert.True(zeroes != -1); /* Assert.True that no unnecessary zero padding exists */
					zeroes++;
				}
				if (v >= 0)
				{
					t = new Scalar((uint)v);
				}
				else
				{
					t = new Scalar((uint)-v);
					t = t.Negate();
				}
				x = x + t;
			}
			Assert.Equal(x, number); /* Assert.True that wnaf represents number */
		}

		void test_constant_wnaf_negate(in Scalar number)
		{
			Scalar neg1 = number;
			Scalar neg2 = number;
			int sign1 = 1;
			if (neg1.GetBits(0, 1) == 0)
			{
				neg1 = neg1.Negate();
				sign1 = -1;
			}
			int sign2 = neg2.CondNegate(neg2.IsEven ? 1 : 0, out neg2);
			Assert.True(sign1 == sign2);
			Assert.Equal(neg1, neg2);
		}

		void test_constant_wnaf(in Scalar number, int w)
		{
			Scalar x, shift;
			Span<int> wnaf = stackalloc int[256];
			int i;
			int skew;
			int bits = 256;
			Scalar num = number;

			x = new Scalar(0);
			shift = new Scalar(1U << w);
			/* With USE_ENDOMORPHISM on we only consider 128-bit numbers */
			for (i = 0; i < 16; ++i)
			{
				num.ShrInt(8, out num);
			}
			bits = 128;
			skew = Wnaf.Const(wnaf, num, w, bits);

			for (i = Wnaf.SIZE_BITS(bits, w); i >= 0; --i)
			{
				Scalar t;
				int v = wnaf[i];
				Assert.True(v != 0); /* Assert.True nonzero */
				Assert.True((v & 1) != 0);  /* Assert.True parity */
				Assert.True(v > -(1 << w)); /* Assert.True range above */
				Assert.True(v < (1 << w));  /* Assert.True range below */

				x *= shift;
				if (v >= 0)
				{
					t = new Scalar((uint)v);
				}
				else
				{
					t = new Scalar((uint)-v);
					t = t.Negate();
				}
				x = x + t;
			}
			/* Skew num because when encoding numbers as odd we use an offset */
			num = num.CAddBit(skew == 2 ? 1U : 0, 1);
			Assert.Equal(x, num);
		}

		void test_fixed_wnaf(in Scalar number, int w)
		{
			Scalar x, shift;
			Span<int> wnaf = stackalloc int[256];
			int i;
			int skew;
			Scalar num = number;

			x = new Scalar(0);
			shift = new Scalar(1U << w);
			/* With USE_ENDOMORPHISM on we only consider 128-bit numbers */
			for (i = 0; i < 16; ++i)
			{
				num.ShrInt(8, out num);
			}
			skew = Wnaf.Fixed(wnaf, num, w);

			for (i = Wnaf.SIZE(w) - 1; i >= 0; --i)
			{
				Scalar t;
				int v = wnaf[i];
				Assert.True(v == 0 || (v & 1) != 0);  /* Assert.True parity */
				Assert.True(v > -(1 << w)); /* Assert.True range above */
				Assert.True(v < (1 << w));  /* Assert.True range below */

				x = x * shift;
				if (v >= 0)
				{
					t = new Scalar((uint)v);
				}
				else
				{
					t = new Scalar((uint)-v);
					t = t.Negate();
				}
				x = x + t;
			}
			/* If skew is 1 then add 1 to num */
			num = num.CAddBit(0, skew == 1 ? 1 : 0);
			Assert.Equal(x, num);
		}

		/* Assert.Trues that the first 8 elements of wnaf are equal to wnaf_expected and the
		 * rest is 0.*/
		void test_fixed_wnaf_small_helper(Span<int> wnaf, Span<int> wnaf_expected, int w)
		{
			int i;
			for (i = Wnaf.SIZE(w) - 1; i >= 8; --i)
			{
				Assert.True(wnaf[i] == 0);
			}
			for (i = 7; i >= 0; --i)
			{
				Assert.True(wnaf[i] == wnaf_expected[i]);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_fixed_wnaf_small()
		{
			int w = 4;
			Span<int> wnaf = stackalloc int[256];
			int i;
			int skew;
			Scalar num;

			num = new Scalar(0);
			skew = Wnaf.Fixed(wnaf, num, w);
			for (i = Wnaf.SIZE(w) - 1; i >= 0; --i)
			{
				int v = wnaf[i];
				Assert.True(v == 0);
			}
			Assert.True(skew == 0);

			num = new Scalar(1);
			skew = Wnaf.Fixed(wnaf, num, w);
			for (i = Wnaf.SIZE(w) - 1; i >= 1; --i)
			{
				int v = wnaf[i];
				Assert.True(v == 0);
			}
			Assert.True(wnaf[0] == 1);
			Assert.True(skew == 0);

			{
				int[] wnaf_expected = new int[] { 0xf, 0xf, 0xf, 0xf, 0xf, 0xf, 0xf, 0xf };
				num = new Scalar(0xffffffff);
				skew = Wnaf.Fixed(wnaf, num, w);
				test_fixed_wnaf_small_helper(wnaf, wnaf_expected, w);
				Assert.True(skew == 0);
			}
			{
				int[] wnaf_expected = new int[] { -1, -1, -1, -1, -1, -1, -1, 0xf };
				num = new Scalar(0xeeeeeeee);
				skew = Wnaf.Fixed(wnaf, num, w);
				test_fixed_wnaf_small_helper(wnaf, wnaf_expected, w);
				Assert.True(skew == 1);
			}
			{
				int[] wnaf_expected = new int[] { 1, 0, 1, 0, 1, 0, 1, 0 };
				num = new Scalar(0x01010101);
				skew = Wnaf.Fixed(wnaf, num, w);
				test_fixed_wnaf_small_helper(wnaf, wnaf_expected, w);
				Assert.True(skew == 0);
			}
			{
				int[] wnaf_expected = new int[] { -0xf, 0, 0xf, -0xf, 0, 0xf, 1, 0 };
				num = new Scalar(0x01ef1ef1);
				skew = Wnaf.Fixed(wnaf, num, w);
				test_fixed_wnaf_small_helper(wnaf, wnaf_expected, w);
				Assert.True(skew == 0);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_wnaf()
		{
			int i;
			Scalar n;

			/* Sanity Assert.True: 1 and 2 are the smallest odd and even numbers and should
			 *               have easier-to-diagnose failure modes  */
			n = new Scalar(1);
			test_constant_wnaf(n, 4);
			n = new Scalar(2);
			test_constant_wnaf(n, 4);
			/* Test 0 */
			test_fixed_wnaf_small();
			/* Random tests */
			for (i = 0; i < count; i++)
			{
				n = random_scalar_order();
				test_wnaf(n, 4 + (i % 10));
				test_constant_wnaf_negate(n);
				test_constant_wnaf(n, 4 + (i % 10));
				test_fixed_wnaf(n, 4 + (i % 10));
			}
			n = new Scalar(0);
			Assert.True(n.CondNegate(1, out n) == -1);
			Assert.True(n.IsZero);
			Assert.True(n.CondNegate(0, out n) == 1);
			Assert.True(n.IsZero);
		}

		class RFC6979TestFailNonceFunction : INonceFunction
		{
			private readonly RFC6979NonceFunction rfc6979;

			public RFC6979TestFailNonceFunction(byte[] nonce)
			{
				rfc6979 = new RFC6979NonceFunction(nonce);
			}
			public bool TryGetNonce(Span<byte> nonce32, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> algo16, uint counter)
			{
				/* Dummy nonce generator that has a fatal error on the first counter value. */
				if (counter == 0)
				{
					return false;
				}
				return rfc6979.TryGetNonce(nonce32, msg32, key32, algo16, counter - 1);
			}
		}
		class RFC6979TestRetryNonceFunction : INonceFunction
		{
			private readonly RFC6979NonceFunction rfc6979;

			public RFC6979TestRetryNonceFunction(byte[] nonce)
			{
				rfc6979 = new RFC6979NonceFunction(nonce);
			}
			public bool TryGetNonce(Span<byte> nonce32, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> algo16, uint counter)
			{
				/* Dummy nonce generator that produces unacceptable nonces for the first several counter values. */
				if (counter < 3)
				{
					nonce32.Fill(counter == 0 ? (byte)0 : (byte)255);
					if (counter == 2)
					{
						nonce32[31]--;
					}
					return true;
				}
				if (counter < 5)
				{
					byte[] order = new byte[]{
		   0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
		   0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFE,
		   0xBA,0xAE,0xDC,0xE6,0xAF,0x48,0xA0,0x3B,
		   0xBF,0xD2,0x5E,0x8C,0xD0,0x36,0x41,0x41
	   };
					order.AsSpan().CopyTo(nonce32);
					if (counter == 4)
					{
						nonce32[31]++;
					}
					return true;
				}
				/* Retry rate of 6979 is negligible esp. as we only call this in deterministic tests. */
				/* If someone does fine a case where it retries for secp256k1, we'd like to know. */
				if (counter > 5)
				{
					return false;
				}
				return rfc6979.TryGetNonce(nonce32, msg32, key32, algo16, counter - 5);
			}
		}

		/* Tests several edge cases. */
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_ecdsa_edge_cases()
		{
			Context ctx = Context.Instance;
			int t;
			SecpECDSASignature sig;

			/* Test the case where ECDSA recomputes a point that is infinity. */
			{
				GEJ keyj;
				GE key;
				Scalar msg;
				Scalar sr, ss;
				ss = new Scalar(1);
				ss = ss.Negate();
				ss = ss.Inverse();
				sr = new Scalar(1);
				keyj = ctx.EcMultGenContext.MultGen(sr);
				key = keyj.ToGroupElement();
				msg = ss;
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
			}

			/* Verify signature with r of zero fails. */
			{
				byte[] pubkey_mods_zero = new byte[] {
			0x02, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xfe, 0xba, 0xae, 0xdc, 0xe6, 0xaf, 0x48, 0xa0,
			0x3b, 0xbf, 0xd2, 0x5e, 0x8c, 0xd0, 0x36, 0x41,
			0x41
		};
				GE key;
				Scalar msg;
				Scalar sr, ss;
				ss = new Scalar(1);
				msg = new Scalar(0);
				sr = new Scalar(0);
				Assert.True(GE.TryParse(pubkey_mods_zero, out key));
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
			}

			/* Verify signature with s of zero fails. */
			{
				byte[] pubkey = new byte[] {
			0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x01
		};
				GE key;
				Scalar msg;
				Scalar sr, ss;
				ss = new Scalar(0);
				msg = new Scalar(0);
				sr = new Scalar(1);
				Assert.True(GE.TryParse(pubkey, out key));
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
			}

			/* Verify signature with message 0 passes. */
			{
				byte[] pubkey = new byte[] {
			0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x02
		};
				byte[] pubkey2 = new byte[] {
			0x02, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xfe, 0xba, 0xae, 0xdc, 0xe6, 0xaf, 0x48, 0xa0,
			0x3b, 0xbf, 0xd2, 0x5e, 0x8c, 0xd0, 0x36, 0x41,
			0x43
		};
				GE key;
				GE key2;
				Scalar msg;
				Scalar sr, ss;
				ss = new Scalar(2);
				msg = new Scalar(0);
				sr = new Scalar(2);
				Assert.True(GE.TryParse(pubkey, out key));
				Assert.True(GE.TryParse(pubkey2, out key2));
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key2, msg));
				ss = ss.Negate();
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key2, msg));
				ss = new Scalar(1);
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key2, msg));
			}

			/* Verify signature with message 1 passes. */
			{
				byte[] pubkey = new byte[] {
			0x02, 0x14, 0x4e, 0x5a, 0x58, 0xef, 0x5b, 0x22,
			0x6f, 0xd2, 0xe2, 0x07, 0x6a, 0x77, 0xcf, 0x05,
			0xb4, 0x1d, 0xe7, 0x4a, 0x30, 0x98, 0x27, 0x8c,
			0x93, 0xe6, 0xe6, 0x3c, 0x0b, 0xc4, 0x73, 0x76,
			0x25
		};
				byte[] pubkey2 = new byte[] {
			0x02, 0x8a, 0xd5, 0x37, 0xed, 0x73, 0xd9, 0x40,
			0x1d, 0xa0, 0x33, 0xd2, 0xdc, 0xf0, 0xaf, 0xae,
			0x34, 0xcf, 0x5f, 0x96, 0x4c, 0x73, 0x28, 0x0f,
			0x92, 0xc0, 0xf6, 0x9d, 0xd9, 0xb2, 0x09, 0x10,
			0x62
		};
				byte[] csr = new byte[]{
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
			0x45, 0x51, 0x23, 0x19, 0x50, 0xb7, 0x5f, 0xc4,
			0x40, 0x2d, 0xa1, 0x72, 0x2f, 0xc9, 0xba, 0xeb
		};
				GE key;
				GE key2;
				Scalar msg;
				Scalar sr, ss;
				ss = new Scalar(1);
				msg = new Scalar(1);
				sr = new Scalar(csr, out _);
				Assert.True(GE.TryParse(pubkey, out key));
				Assert.True(GE.TryParse(pubkey2, out key2));
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key2, msg));
				ss = ss.Negate();
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key2, msg));
				ss = new Scalar(2);
				ss = ss.InverseVariable();
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key2, msg));
			}

			/* Verify signature with message -1 passes. */
			{
				byte[] pubkey = {
			0x03, 0xaf, 0x97, 0xff, 0x7d, 0x3a, 0xf6, 0xa0,
			0x02, 0x94, 0xbd, 0x9f, 0x4b, 0x2e, 0xd7, 0x52,
			0x28, 0xdb, 0x49, 0x2a, 0x65, 0xcb, 0x1e, 0x27,
			0x57, 0x9c, 0xba, 0x74, 0x20, 0xd5, 0x1d, 0x20,
			0xf1
		};
				byte[] csr = {
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
			0x45, 0x51, 0x23, 0x19, 0x50, 0xb7, 0x5f, 0xc4,
			0x40, 0x2d, 0xa1, 0x72, 0x2f, 0xc9, 0xba, 0xee
		};
				GE key;
				Scalar msg;
				Scalar sr, ss;
				ss = new Scalar(1);
				msg = new Scalar(1);
				msg = msg.Negate();
				sr = new Scalar(csr, out _);
				Assert.True(GE.TryParse(pubkey, out key));
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				ss = ss.Negate();
				Assert.True(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
				ss = new Scalar(3);
				ss = ss.InverseVariable();
				Assert.False(ECPubKey.secp256k1_ecdsa_sig_verify(ctx.EcMultContext, sr, ss, key, msg));
			}

			/* Signature where s would be zero. */
			{
				ECPubKey pubkey;
				int siglen;
				//int ecount;
				byte[] signature = new byte[72];
				byte[] nonce = new byte[]{
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
		};
				byte[] nonce2 = new byte[]{
			0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
			0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFE,
			0xBA,0xAE,0xDC,0xE6,0xAF,0x48,0xA0,0x3B,
			0xBF,0xD2,0x5E,0x8C,0xD0,0x36,0x41,0x40
		};
				var key = ctx.CreateECPrivKey(new byte[]{
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
		});
				byte[] msg = new byte[]{
			0x86, 0x41, 0x99, 0x81, 0x06, 0x23, 0x44, 0x53,
			0xaa, 0x5f, 0x9d, 0x6a, 0x31, 0x78, 0xf4, 0xf7,
			0xb8, 0x12, 0xe0, 0x0b, 0x81, 0x7a, 0x77, 0x62,
			0x65, 0xdf, 0xdd, 0x31, 0xb9, 0x3e, 0x29, 0xa9,
		};
				//ecount = 0;
				//secp256k1_context_set_illegal_callback(ctx, counting_illegal_callback_fn, &ecount);
				Assert.False(key.TrySignECDSA(msg, new PrecomputedNonceFunction(nonce), out sig));
				Assert.False(key.TrySignECDSA(msg, new PrecomputedNonceFunction(nonce2), out sig));
				msg[31] = 0xaa;
				Assert.True(key.TrySignECDSA(msg, new PrecomputedNonceFunction(nonce), out sig));
				//Assert.True(ecount == 0);
				//  CHECK(secp256k1_ecdsa_sign(ctx, NULL, msg, key, precomputed_nonce_function, nonce2) == 0);
				// Assert.False(key.TrySignECDSA(msg, new PrecomputedNonceFunction(nonce2), out _));
				//Assert.True(ecount == 1);
				Assert.False(key.TrySignECDSA(new ReadOnlySpan<byte>(), new PrecomputedNonceFunction(nonce2), out sig));
				//Assert.True(ecount == 2);
				var emptykey = key.Clone();
				emptykey.Clear();

				Assert.Throws<ObjectDisposedException>(() => emptykey.TrySignECDSA(msg, new PrecomputedNonceFunction(nonce2), out sig));
				//Assert.True(ecount == 3);
				Assert.True(key.TrySignECDSA(msg, new PrecomputedNonceFunction(nonce2), out sig));
				pubkey = key.CreatePubKey();
				Assert.False(pubkey.SigVerify(null as SecpECDSASignature, msg));
				// CHECK(ecount == 4);
				Assert.False(pubkey.SigVerify(sig, new ReadOnlySpan<byte>()));
				// CHECK(ecount == 5);
				//Assert.False(NULL.SigVerify(sig, msg));
				// CHECK(ecount == 6);
				Assert.True(pubkey.SigVerify(sig, msg));
				// CHECK(ecount == 6);
				//pubkey = NULL.CreatePubKey();
				// CHECK(ecount == 7);
				/* That pubkeyload fails via an ARGCHECK is a little odd but makes sense because pubkeys are an opaque data type. */
				//Assert.False(NULL.SigVerify(sig, msg));
				// CHECK(ecount == 8);
				siglen = 72;
				//CHECK(secp256k1_ecdsa_signature_serialize_der(ctx, NULL, &siglen, &sig) == 0);
				//// CHECK(ecount == 9);
				//CHECK(secp256k1_ecdsa_signature_serialize_der(ctx, signature, NULL, &sig) == 0);
				//// CHECK(ecount == 10);
				//CHECK(secp256k1_ecdsa_signature_serialize_der(ctx, signature, &siglen, NULL) == 0);
				//// CHECK(ecount == 11);
				sig.WriteDerToSpan(signature, out siglen);
				//// CHECK(ecount == 11);
				//CHECK(secp256k1_ecdsa_signature_parse_der(ctx, NULL, signature, siglen) == 0);
				//// CHECK(ecount == 12);
				//CHECK(secp256k1_ecdsa_signature_parse_der(ctx, &sig, NULL, siglen) == 0);
				//// CHECK(ecount == 13);
				Assert.True(Secp256k1.SecpECDSASignature.TryCreateFromDer(signature.AsSpan().Slice(0, siglen), out _));
				//// CHECK(ecount == 13);
				siglen = 10;
				/* Too little room for a signature does not fail via ARGCHECK. */
				Assert.False(Secp256k1.SecpECDSASignature.TryCreateFromDer(signature.AsSpan().Slice(0, siglen), out _));
				//// CHECK(ecount == 13);
				//ecount = 0;

				//Assert.False(NULL.TryNormalize(out NULL));
				//// CHECK(ecount == 1);
				//CHECK(secp256k1_ecdsa_signature_serialize_compact(ctx, NULL, &sig) == 0);
				//// CHECK(ecount == 2);
				//CHECK(secp256k1_ecdsa_signature_serialize_compact(ctx, signature, NULL) == 0);
				//// CHECK(ecount == 3);
				sig.WriteCompactToSpan(signature.AsSpan().Slice(0, 64));
				//// CHECK(ecount == 3);
				//Assert.False(SecpECDSASignature.TryCreateFromCompact(signature, out NULL));
				//// CHECK(ecount == 4);
				//CHECK(secp256k1_ecdsa_signature_parse_compact(ctx, &sig, NULL) == 0);
				//// CHECK(ecount == 5);
				Assert.True(SecpECDSASignature.TryCreateFromCompact(signature.AsSpan().Slice(0, 64), out sig));
				//// CHECK(ecount == 5);
				signature.AsSpan().Slice(0, 64).Fill(255);
				Assert.False(SecpECDSASignature.TryCreateFromCompact(signature.AsSpan().Slice(0, 64), out sig));
				//// CHECK(ecount == 5);
				////secp256k1_context_set_illegal_callback(ctx, NULL, NULL);
			}

			/* Nonce function corner cases. */
			for (t = 0; t < 2; t++)
			{
				byte[] zero = new byte[] { 0x00 };
				int i;
				byte[] key = new byte[32];
				byte[] msg = new byte[32];
				SecpECDSASignature sig2;
				Span<Scalar> sr = new Scalar[512];
				Scalar ss;
				byte[] extra;
				extra = t == 0 ? null : zero;
				msg.AsSpan().Fill(0);
				msg[31] = 1;
				/* High key results in signature failure. */
				key.AsSpan().Fill(0xff);
				// With NBitcoin, we can't even create an incorrect ECPrivKey
				Assert.False(ctx.TryCreateECPrivKey(key, out _));
				//CHECK(secp256k1_ecdsa_sign(ctx, &sig, msg, key, NULL, extra) == 0);
				//CHECK(is_empty_signature(&sig));
				/* Zero key results in signature failure. */
				key.AsSpan().Fill(0);
				// With NBitcoin, we can't even create an incorrect ECPrivKey
				Assert.False(ctx.TryCreateECPrivKey(key, out _));
				//CHECK(secp256k1_ecdsa_sign(ctx, &sig, msg, key, NULL, extra) == 0);
				//CHECK(is_empty_signature(&sig));
				/* Nonce function failure results in signature failure. */
				key[31] = 1;
				var keyo = ctx.CreateECPrivKey(key);
				Assert.False(keyo.TrySignECDSA(msg, new RFC6979TestFailNonceFunction(extra), out sig));
				Assert.Null(sig);
				/* The retry loop successfully makes its way to the first good value. */
				Assert.True(keyo.TrySignECDSA(msg, new RFC6979TestRetryNonceFunction(extra), out sig));
				Assert.NotNull(sig);
				Assert.True(keyo.TrySignECDSA(msg, new RFC6979NonceFunction(extra), out sig2));
				Assert.NotNull(sig2);
				Assert.Equal(sig, sig2);
				/* The default nonce function is deterministic. */
				Assert.True(keyo.TrySignECDSA(msg, new RFC6979NonceFunction(extra), out sig2));
				Assert.NotNull(sig2);
				Assert.Equal(sig, sig2);
				/* The default nonce function changes output with different messages. */
				for (i = 0; i < 256; i++)
				{
					int j;
					msg[0] = (byte)i;
					Assert.True(keyo.TrySignECDSA(msg, new RFC6979NonceFunction(extra), out sig2));
					Assert.NotNull(sig2);
					(sr[i], ss) = sig2;
					for (j = 0; j < i; j++)
					{
						Assert.NotEqual(sr[i], sr[j]);
					}
				}
				msg[0] = 0;
				msg[31] = 2;
				/* The default nonce function changes output with different keys. */
				for (i = 256; i < 512; i++)
				{
					int j;
					key[0] = (byte)(i - 256);
					keyo = ctx.CreateECPrivKey(key);
					Assert.True(keyo.TrySignECDSA(msg, new RFC6979NonceFunction(extra), out sig2));
					Assert.NotNull(sig2);
					(sr[i], ss) = sig2;
					for (j = 0; j < i; j++)
					{
						Assert.NotEqual(sr[i], sr[j]);
					}
				}
				key[0] = 0;
			}

			//{
			//	/* Check that optional nonce arguments do not have equivalent effect. */
			//	byte[] zeros = new byte[] { 0 };
			//	byte[] nonce =  new byte[32];
			//	byte[] nonce2 = new byte[32];
			//	byte[] nonce3 = new byte[32];
			//	byte[] nonce4 = new byte[32];

			//	Assert.True(new RFC6979NonceFunction().TrySign(nonce, zeros, zeros, new ReadOnlySpan<byte>(), 0));
			//	Assert.True(new RFC6979NonceFunction().TrySign(nonce2, zeros, zeros, zeros, 0));
			//	Assert.True(new RFC6979NonceFunction(zeros).TrySign(nonce3, zeros, zeros, new ReadOnlySpan<byte>(), 0));
			//	Assert.True(new RFC6979NonceFunction(zeros).TrySign(nonce4, zeros, zeros, zeros, 0));

			//	Assert.False(Utils.ArrayEqual(nonce, nonce2));
			//	Assert.False(Utils.ArrayEqual(nonce, nonce3));
			//	Assert.False(Utils.ArrayEqual(nonce, nonce4));
			//	Assert.False(Utils.ArrayEqual(nonce2, nonce3));
			//	Assert.False(Utils.ArrayEqual(nonce2, nonce4));
			//	Assert.False(Utils.ArrayEqual(nonce3, nonce4));
			//}


			/* Privkey export where pubkey is the point at infinity. */
			{
				byte[] privkey = new byte[300];
				//var seckey =
				Assert.False(ctx.TryCreateECPrivKey(new byte[]{
				0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
				0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xfe,
				0xba, 0xae, 0xdc, 0xe6, 0xaf, 0x48, 0xa0, 0x3b,
				0xbf, 0xd2, 0x5e, 0x8c, 0xd0, 0x36, 0x41, 0x41 }, out _));
				// Can't test, CreateECPrivKey enforce invariant
				//int outlen = 300;
				//CHECK(!ec_privkey_export_der(ctx, privkey, &outlen, seckey, 0));
				//outlen = 300;
				//CHECK(!ec_privkey_export_der(ctx, privkey, &outlen, seckey, 1));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_recovery_tests()
		{
			int i;
			for (i = 0; i < count; i++)
			{
				//test_ecdsa_recovery_api();
			}
			for (i = 0; i < 64 * count; i++)
			{
				test_ecdsa_recovery_end_to_end();
			}
			test_ecdsa_recovery_edge_cases();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_ecdsa_recovery_end_to_end()
		{
			var ctx = Context.Instance;
			byte[] extra = new byte[32];
			ECPrivKey privkey = null;
			Span<byte> message = stackalloc byte[32];
			SecpECDSASignature[] signature = new SecpECDSASignature[5];
			SecpRecoverableECDSASignature[] rsignature = new SecpRecoverableECDSASignature[5];
			byte[] sig = new byte[74];
			ECPubKey pubkey;
			ECPubKey recpubkey;
			int recid = 0;

			/* Generate a random key and message. */
			{
				Scalar msg, key;
				msg = random_scalar_order_test();
				key = random_scalar_order_test();
				privkey = ctx.CreateECPrivKey(key);
				key.WriteToSpan(message);
			}

			/* Construct and verify corresponding public key. */
			//CHECK(secp256k1_ec_seckey_verify(ctx, privkey) == 1);
			pubkey = privkey.CreatePubKey();

			/* Serialize/parse compact and verify/recover. */
			extra[0] = 0;
			Assert.True(privkey.TrySignRecoverable(message, out rsignature[0]));
			Assert.True(privkey.TrySignECDSA(message, out signature[0]));
			Assert.True(privkey.TrySignRecoverable(message, out rsignature[4]));
			Assert.True(privkey.TrySignRecoverable(message, new RFC6979NonceFunction(extra), out rsignature[1]));
			extra[31] = 1;
			Assert.True(privkey.TrySignRecoverable(message, new RFC6979NonceFunction(extra), out rsignature[2]));
			extra[31] = 0;
			extra[0] = 1;
			Assert.True(privkey.TrySignRecoverable(message, new RFC6979NonceFunction(extra), out rsignature[3]));
			rsignature[4].WriteToSpanCompact(sig.AsSpan().Slice(0, 64), out recid);
			signature[4] = rsignature[4].ToSignature();
			Assert.Equal(signature[4], signature[0]);

			Assert.True(pubkey.SigVerify(signature[4], message));
			rsignature[4] = null;
			Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sig.AsSpan().Slice(0, 64), recid, out rsignature[4]));
			signature[4] = rsignature[4].ToSignature();
			Assert.True(pubkey.SigVerify(signature[4], message));
			/* Parse compact (with recovery id) and recover. */
			Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sig.AsSpan().Slice(0, 64), recid, out rsignature[4]));
			Assert.True(ECPubKey.TryRecover(ctx, rsignature[4], message, out recpubkey));
			Assert.Equal(pubkey, recpubkey);
			/* Serialize/destroy/parse signature and verify again. */
			rsignature[4].WriteToSpanCompact(sig.AsSpan().Slice(0, 64), out recid);
			sig[secp256k1_rand_bits(6)] += (byte)(1 + secp256k1_rand_int(255));
			Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sig.AsSpan().Slice(0, 64), recid, out rsignature[4]));
			signature[4] = rsignature[4].ToSignature();
			Assert.False(pubkey.SigVerify(signature[4], message));
			/* Recover again */
			Assert.True(!ECPubKey.TryRecover(ctx, rsignature[4], message, out recpubkey) ||
				  pubkey != recpubkey);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_schnorrsig_bip_vectors()
		{
			{
				/* Test vector 1 */
				byte[] sk1 = {
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
		};
				byte[] pk1 = {
			0x02, 0x79, 0xBE, 0x66, 0x7E, 0xF9, 0xDC, 0xBB,
			0xAC, 0x55, 0xA0, 0x62, 0x95, 0xCE, 0x87, 0x0B,
			0x07, 0x02, 0x9B, 0xFC, 0xDB, 0x2D, 0xCE, 0x28,
			0xD9, 0x59, 0xF2, 0x81, 0x5B, 0x16, 0xF8, 0x17,
			0x98
		};
				byte[] msg1 = {
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
		};
				byte[] sig1 = {
			0x78, 0x7A, 0x84, 0x8E, 0x71, 0x04, 0x3D, 0x28,
			0x0C, 0x50, 0x47, 0x0E, 0x8E, 0x15, 0x32, 0xB2,
			0xDD, 0x5D, 0x20, 0xEE, 0x91, 0x2A, 0x45, 0xDB,
			0xDD, 0x2B, 0xD1, 0xDF, 0xBF, 0x18, 0x7E, 0xF6,
			0x70, 0x31, 0xA9, 0x88, 0x31, 0x85, 0x9D, 0xC3,
			0x4D, 0xFF, 0xEE, 0xDD, 0xA8, 0x68, 0x31, 0x84,
			0x2C, 0xCD, 0x00, 0x79, 0xE1, 0xF9, 0x2A, 0xF1,
			0x77, 0xF7, 0xF2, 0x2C, 0xC1, 0xDC, 0xED, 0x05
		};
				test_schnorrsig_bip_vectors_check_verify(pk1, msg1, sig1, true);
			}
		}

		void test_schnorrsig_bip_vectors_check_verify(byte[] pk_serialized, byte[] msg32, byte[] sig_serialized, bool expected)
		{
			var ctx = Context.Instance;
			//const unsigned char* msg_arr[1];
			//const secp256k1_schnorrsig* sig_arr[1];
			//const secp256k1_pubkey* pk_arr[1];
			ECPubKey pk;
			SecpSchnorrSignature sig;

			Assert.True(ctx.TryCreatePubKey(pk_serialized, out pk));
			Assert.True(SecpSchnorrSignature.TryCreate(sig_serialized, out sig));

			//sig_arr[0] = &sig;
			//msg_arr[0] = msg32;
			//pk_arr[0] = &pk;

			Assert.Equal(expected, pk.SigVerifySchnorr(sig, msg32));
			//CHECK(expected == secp256k1_schnorrsig_verify_batch(ctx, scratch, sig_arr, msg_arr, pk_arr, 1));
		}

		/* Tests several edge cases. */
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_ecdsa_recovery_edge_cases()
		{
			var msg32 = new char[]{
		'T', 'h', 'i', 's', ' ', 'i', 's', ' ',
		'a', ' ', 'v', 'e', 'r', 'y', ' ', 's',
		'e', 'c', 'r', 'e', 't', ' ', 'm', 'e',
		's', 's', 'a', 'g', 'e', '.', '.', '.'
	}.Select(c => (byte)c).ToArray().AsSpan();
			byte[] sig64 = new byte[]{
        /* Generated by signing the above message with nonce 'This is the nonce we will use...'
         * and secret key 0 (which is not valid), resulting in recid 1. */
        0x67, 0xCB, 0x28, 0x5F, 0x9C, 0xD1, 0x94, 0xE8,
		0x40, 0xD6, 0x29, 0x39, 0x7A, 0xF5, 0x56, 0x96,
		0x62, 0xFD, 0xE4, 0x46, 0x49, 0x99, 0x59, 0x63,
		0x17, 0x9A, 0x7D, 0xD1, 0x7B, 0xD2, 0x35, 0x32,
		0x4B, 0x1B, 0x7D, 0xF3, 0x4C, 0xE1, 0xF6, 0x8E,
		0x69, 0x4F, 0xF6, 0xF1, 0x1A, 0xC7, 0x51, 0xDD,
		0x7D, 0xD7, 0x3E, 0x38, 0x7E, 0xE4, 0xFC, 0x86,
		0x6E, 0x1B, 0xE8, 0xEC, 0xC7, 0xDD, 0x95, 0x57
	};
			ECPubKey pubkey;
			/* signature (r,s) = (4,4), which can be recovered with all 4 recids. */
			byte[] sigb64 = new byte[]{
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
	};
			ECPubKey pubkeyb;
			SecpRecoverableECDSASignature rsig;
			SecpECDSASignature sig;
			int recid;

			Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sig64, 0, out rsig));
			Assert.False(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkey));
			Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sig64, 1, out rsig));
			Assert.True(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkey));
			Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sig64, 2, out rsig));
			Assert.False(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkey));
			Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sig64, 3, out rsig));
			Assert.False(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkey));

			for (recid = 0; recid < 4; recid++)
			{
				int i;
				int recid2;
				/* (4,4) encoded in DER. */
				byte[] sigbder = { 0x30, 0x06, 0x02, 0x01, 0x04, 0x02, 0x01, 0x04 };
				byte[] sigcder_zr = { 0x30, 0x05, 0x02, 0x00, 0x02, 0x01, 0x01 };
				byte[] sigcder_zs = { 0x30, 0x05, 0x02, 0x01, 0x01, 0x02, 0x00 };
				byte[] sigbderalt1 = {
			0x30, 0x25, 0x02, 0x20, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x04, 0x02, 0x01, 0x04,
		};
				byte[] sigbderalt2 = {
			0x30, 0x25, 0x02, 0x01, 0x04, 0x02, 0x20, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
		};
				byte[] sigbderalt3 = {
			0x30, 0x26, 0x02, 0x21, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x04, 0x02, 0x01, 0x04,
		};
				byte[] sigbderalt4 = {
			0x30, 0x26, 0x02, 0x01, 0x04, 0x02, 0x21, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
		};
				/* (order + r,4) encoded in DER. */
				byte[] sigbderlong = {
			0x30, 0x26, 0x02, 0x21, 0x00, 0xFF, 0xFF, 0xFF,
			0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
			0xFF, 0xFF, 0xFF, 0xFF, 0xFE, 0xBA, 0xAE, 0xDC,
			0xE6, 0xAF, 0x48, 0xA0, 0x3B, 0xBF, 0xD2, 0x5E,
			0x8C, 0xD0, 0x36, 0x41, 0x45, 0x02, 0x01, 0x04
		};
				Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sigb64, recid, out rsig));
				Assert.True(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkeyb));
				Assert.True(SecpECDSASignature.TryCreateFromDer(sigbder, out sig));
				Assert.True(pubkeyb.SigVerify(sig, msg32));
				for (recid2 = 0; recid2 < 4; recid2++)
				{
					ECPubKey pubkey2b;
					Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sigb64, recid2, out rsig));
					Assert.True(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkey2b));
					/* Verifying with (order + r,4) should always fail. */
					Assert.True(SecpECDSASignature.TryCreateFromDer(sigbderlong, out sig));
					Assert.False(pubkeyb.SigVerify(sig, msg32));
				}
				/* DER parsing tests. */
				/* Zero length r/s. */
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigcder_zr, out sig));
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigcder_zs, out sig));
				/* Leading zeros. */
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigbderalt1, out sig));
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigbderalt2, out sig));
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigbderalt3, out sig));
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigbderalt4, out sig));
				sigbderalt3[4] = 1;
				Assert.True(SecpECDSASignature.TryCreateFromDer(sigbderalt3, out sig));
				Assert.False(pubkeyb.SigVerify(sig, msg32));
				sigbderalt4[7] = 1;
				Assert.True(SecpECDSASignature.TryCreateFromDer(sigbderalt4, out sig));
				Assert.False(pubkeyb.SigVerify(sig, msg32));
				/* Damage signature. */
				sigbder[7]++;
				Assert.True(SecpECDSASignature.TryCreateFromDer(sigbder, out sig));
				Assert.False(pubkeyb.SigVerify(sig, msg32));
				sigbder[7]--;
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigbder.AsSpan().Slice(0, 6), out sig));
				Assert.False(SecpECDSASignature.TryCreateFromDer(sigbder.AsSpan().Slice(0, sigbder.Length - 1), out sig));
				for (i = 0; i < 8; i++)
				{
					int c;
					byte orig = sigbder[i];
					/*Try every single-byte change.*/
					for (c = 0; c < 256; c++)
					{
						if (c == orig)
						{
							continue;
						}
						sigbder[i] = (byte)c;
						Assert.True(!SecpECDSASignature.TryCreateFromDer(sigbder, out sig) || !pubkeyb.SigVerify(sig, msg32));
					}
					sigbder[i] = orig;
				}
			}

			/* Test r/s equal to zero */
			{
				/* (1,1) encoded in DER. */
				byte[] sigcder = { 0x30, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x01 };
				byte[] sigc64 = {
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
		};
				ECPubKey pubkeyc;
				Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sigc64, 0, out rsig));
				Assert.True(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkeyc));
				Assert.True(SecpECDSASignature.TryCreateFromDer(sigcder, out sig));
				Assert.True(pubkeyc.SigVerify(sig, msg32));
				sigcder[4] = 0;
				sigc64[31] = 0;
				Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sigc64, 0, out rsig));
				Assert.False(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkeyb));
				Assert.True(SecpECDSASignature.TryCreateFromDer(sigcder, out sig));
				Assert.False(pubkeyc.SigVerify(sig, msg32));
				sigcder[4] = 1;
				sigcder[7] = 0;
				sigc64[31] = 1;
				sigc64[63] = 0;
				Assert.True(SecpRecoverableECDSASignature.TryCreateFromCompact(sigc64, 0, out rsig));
				Assert.False(ECPubKey.TryRecover(ctx, rsig, msg32, out pubkeyb));
				Assert.True(SecpECDSASignature.TryCreateFromDer(sigcder, out sig));
				Assert.False(pubkeyc.SigVerify(sig, msg32));
			}
		}

		/// https://github.com/bitcoin-core/secp256k1/blob/2ed54da18add295668ec71c91534b640d2cc029b/src/tests.c#L2378
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_ec_combine()
		{
			var d = new ECPubKey[6];
			var sum = Scalar.Zero;
			var ctx = Context.Instance;
			for (int i = 1; i <= 6; i++)
			{
				Scalar s = random_scalar_order_test();
				sum = sum.Add(s);
				GEJ qj = ctx.EcMultGenContext.MultGen(in s);
				GE q = qj.ToGroupElementVariable();

				d[i - 1] = new ECPubKey(in q, ctx);

				qj = ctx.EcMultGenContext.MultGen(in sum);
				q = qj.ToGroupElementVariable();
				ECPubKey sd = new ECPubKey(in q, ctx);
				Assert.True(ECPubKey.TryCombine(Context.Instance, d[0..i], out var sd2));
				Assert.Equal(sd, sd2);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_xonly_pubkey_tweak()
		{
			var tweak = new byte[32];
			var sk = new byte[32];
			var zero64 = new byte[64];
			var overflow = new byte[64];
			overflow.AsSpan().Fill(0xff);
			secp256k1_rand256(tweak);
			secp256k1_rand256(sk);
			var internal_pk = Context.Instance.CreateECPrivKey(sk).CreatePubKey();
			var internal_xonly_pk = internal_pk.ToXOnlyPubKey(out var pk_parity);

			// We don't port that
			//ecount = 0;
			//CHECK(secp256k1_xonly_pubkey_tweak_add(none, &output_pk, &internal_xonly_pk, tweak) == 0);
			//CHECK(ecount == 1);
			//CHECK(secp256k1_xonly_pubkey_tweak_add(sign, &output_pk, &internal_xonly_pk, tweak) == 0);
			//CHECK(ecount == 2);
			var output_pk = internal_xonly_pk.AddTweak(tweak);
			/* Invalid tweak zeroes the output_pk */
			Assert.False(internal_xonly_pk.TryAddTweak(overflow.AsSpan().Slice(0, 32), out _));
			/* A zero tweak is fine */
			Assert.True(internal_xonly_pk.TryAddTweak(zero64.AsSpan().Slice(0, 32), out _));

			/* Fails if the resulting key was infinity */
			for (var i = 0; i < count; i++)
			{
				/* Because sk may be negated before adding, we need to try with tweak =
				 * sk as well as tweak = -sk. */
				Scalar scalar_tweak = new Scalar(sk, out _);
				scalar_tweak = scalar_tweak.Negate();
				scalar_tweak.WriteToSpan(tweak);

				Assert.True(!internal_xonly_pk.TryAddTweak(sk, out output_pk) ||
					!internal_xonly_pk.TryAddTweak(tweak, out output_pk));
				Assert.Null(output_pk);
			}
		}

		/* Starts with an initial pubkey and recursively creates N_PUBKEYS - 1
		* additional pubkeys by calling tweak_add. Then verifies every tweak starting
		* from the last pubkey. */
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_xonly_pubkey_tweak_recursive()
		{
			var N_PUBKEYS = 32;
			var sk = new byte[32];
			var pk_serialized = new byte[32];
			var tweak = new byte[N_PUBKEYS - 1][];
			ECPubKey[] pk = new ECPubKey[N_PUBKEYS];
			secp256k1_rand256(sk);
			pk[0] = Context.Instance.CreateECPrivKey(sk).CreatePubKey();
			for (var i = 0; i < N_PUBKEYS - 1; i++)
			{
				tweak[i] = new byte[32];
				tweak[i].AsSpan().Fill((byte)(i + 1));
				var xonly_pk = pk[i].ToXOnlyPubKey(out _);
				pk[i + 1] = xonly_pk.AddTweak(tweak[i]);
			}

			/* Verify tweaks */
			for (var i = N_PUBKEYS - 1; i > 0; i--)
			{
				var tweaked = pk[i].ToXOnlyPubKey(out var pk_parity);
				var xonly_pk = pk[i - 1].ToXOnlyPubKey(out _);
				Assert.True(tweaked.CheckIsTweakedWith(xonly_pk, tweak[i - 1], pk_parity));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_ecmult_multi2()
		{
			int keyCount = 504;
			var scalars = new Scalar[keyCount];
			var points = new GE[keyCount];
			for (int i = 0; i < keyCount; i++)
			{
				scalars[i] = random_scalar_order_test();
				points[i] = random_group_element_test();
			}


			GE r = GE.Infinity;
			foreach (ECMultiImplementation implementation in Enum.GetValues(typeof(ECMultiImplementation)).OfType<ECMultiImplementation>())
			{
				var res = ECMultContext.Instance.MultBatch(null, scalars, points, implementation);

				if (!r.IsInfinity)
				{
					Assert.Equal(r, res.ToGroupElement());
				}
				else
				{
					r = res.ToGroupElement();
				}
			}

			foreach (ECMultiImplementation implementation in Enum.GetValues(typeof(ECMultiImplementation)).OfType<ECMultiImplementation>())
			{
				var res = ECMultContext.Instance.MultBatch(null, scalars, points, implementation);
				Assert.Equal(r, res.ToGroupElement());
			}

			// Show case how to parallelize
			foreach (ECMultiImplementation implementation in Enum.GetValues(typeof(ECMultiImplementation)).OfType<ECMultiImplementation>())
			{
				int fullBatchCount = 100;
				var batches = (int)Math.DivRem(keyCount, fullBatchCount, out var remainder);
				var subResult = new GEJ[remainder == 0 ? batches : batches + 1];
				Parallel.For(0, batches + 1, (int i) =>
				{
					var count = i == batches ? remainder : fullBatchCount;
					if (count == 0)
						return;
					subResult[i] = ECMultContext.Instance.MultBatch(null, scalars.AsSpan().Slice(i * fullBatchCount, count)
									   , points.AsSpan().Slice(i * fullBatchCount, count), implementation);
				});

				GEJ sum = GEJ.Infinity;
				foreach (var item in subResult)
				{
					sum = sum.AddVariable(item);
				}
				Assert.Equal(r, sum.ToGroupElement());
			}
		}


		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_ecmult_multi()
		{
			test_ecmult_multi_core(ECMultiImplementation.Pippenger);
			test_ecmult_multi_core(ECMultiImplementation.Simple);
			test_ecmult_multi_core(ECMultiImplementation.Strauss);
		}

		private void test_ecmult_multi_core(ECMultiImplementation implementation)
		{
			int ncount;
			Scalar szero = Scalar.Zero;
			Span<Scalar> sc = new Scalar[32];
			Span<GE> pt = new GE[32];
			GEJ r;
			GEJ r2;

			/* No points to multiply */
			r = ctx.EcMultContext.MultBatch(null, sc.Slice(0, 0), pt.Slice(0, 0), implementation);
			Assert.True(r.IsInfinity);
			/* Check 1- and 2-point multiplies against ecmult */
			for (ncount = 0; ncount < count; ncount++)
			{
				GE ptg;
				GEJ ptgj;
				sc[0] = random_scalar_order();
				sc[1] = random_scalar_order();

				ptg = random_group_element_test();
				ptgj = ptg.ToGroupElementJacobian();
				pt[0] = ptg;
				pt[1] = EC.G;

				/* only G scalar */
				r2 = ctx.EcMultContext.Mult(ptgj, szero, sc[0]);
				r = ctx.EcMultContext.MultBatch(sc[0], sc.Slice(0, 0), pt.Slice(0, 0), implementation);
				r2 = r2.Negate();
				r = r.AddVariable(r2, out _);
				Assert.True(r.IsInfinity);

				/* 1-point */
				r2 = ctx.EcMultContext.Mult(ptgj, sc[0], szero);
				r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 1), pt.Slice(0, 1), implementation);
				r2 = r2.Negate();
				r = r.AddVariable(r2, out _);
				Assert.True(r.IsInfinity);

				/* Try to multiply 1 point, but scratch space is empty */
				//scratch_empty = secp256k1_scratch_create(&ctx->error_callback, 0, 0);
				//CHECK(!ecmult_multi(&ctx->ecmult_ctx, scratch_empty, &r, &szero, ecmult_multi_callback, &data, 1));
				//secp256k1_scratch_destroy(scratch_empty);

				/* Try to multiply 1 point, but callback returns false */
				//CHECK(!ecmult_multi(&ctx->ecmult_ctx, scratch, &r, &szero, ecmult_multi_false_callback, &data, 1));

				/* 2-point */
				r2 = ctx.EcMultContext.Mult(ptgj, sc[0], sc[1]);
				r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 2), pt.Slice(0, 2), implementation);

				r2 = r2.Negate();
				r = r.AddVariable(r2, out _);
				Assert.True(r.IsInfinity);

				/* 2-point with G scalar */
				r2 = ctx.EcMultContext.Mult(ptgj, sc[0], sc[1]);
				r = ctx.EcMultContext.MultBatch(sc[1], sc.Slice(0, 1), pt.Slice(0, 1), implementation);

				r2 = r2.Negate();
				r = r.AddVariable(r2, out _);
				Assert.True(r.IsInfinity);
			}

			/* Check infinite outputs of various forms */
			for (ncount = 0; ncount < count; ncount++)
			{
				GE ptg;
				int i, j;
				int[] sizes = { 2, 10, 32 };

				for (j = 0; j < 3; j++)
				{
					for (i = 0; i < 32; i++)
					{
						sc[i] = random_scalar_order();
						pt[i] = GE.Infinity;
					}
					r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, sizes[j]), pt.Slice(0, sizes[j]), implementation);

					Assert.True(r.IsInfinity);
				}

				for (j = 0; j < 3; j++)
				{
					for (i = 0; i < 32; i++)
					{
						ptg = random_group_element_test();
						pt[i] = ptg;
						sc[i] = new Scalar(0);
					}
					r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, sizes[j]), pt.Slice(0, sizes[j]), implementation);
					Assert.True(r.IsInfinity);
				}

				for (j = 0; j < 3; j++)
				{
					ptg = random_group_element_test();
					for (i = 0; i < 16; i++)
					{
						sc[2 * i] = random_scalar_order();
						sc[2 * i + 1] = sc[2 * i].Negate();
						pt[2 * i] = ptg;
						pt[2 * i + 1] = ptg;
					}

					r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, sizes[j]), pt.Slice(0, sizes[j]), implementation);
					Assert.True(r.IsInfinity);

					sc[0] = random_scalar_order();
					for (i = 0; i < 16; i++)
					{
						ptg = random_group_element_test();

						sc[2 * i] = sc[0];
						sc[2 * i + 1] = sc[0];
						pt[2 * i] = ptg;
						pt[2 * i + 1] = pt[2 * i].Negate();
					}

					r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, sizes[j]), pt.Slice(0, sizes[j]), implementation);
					Assert.True(r.IsInfinity);
				}

				ptg = random_group_element_test();
				sc[0] = new Scalar(0);
				pt[0] = ptg;
				for (i = 1; i < 32; i++)
				{
					pt[i] = ptg;

					sc[i] = random_scalar_order();
					sc[0] += sc[i];
					sc[i] = sc[i].Negate();
				}

				r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 32), pt.Slice(0, 32), implementation);

				Assert.True(r.IsInfinity);
			}

			/* Check random points, constant scalar */
			for (ncount = 0; ncount < count; ncount++)
			{
				int i;
				r = GEJ.Infinity;

				sc[0] = random_scalar_order();
				for (i = 0; i < 20; i++)
				{
					GE ptg;
					sc[i] = sc[0];
					ptg = random_group_element_test();
					pt[i] = ptg;
					r = r.AddVariable(pt[i], out _);
				}
				r2 = ctx.EcMultContext.Mult(r, sc[0], szero);
				r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 20), pt.Slice(0, 20), implementation);
				r2 = r2.Negate();
				r = r.AddVariable(r2, out _);
				Assert.True(r.IsInfinity);
			}

			/* Check random scalars, constant point */
			for (ncount = 0; ncount < count; ncount++)
			{
				int i;
				GE ptg;
				GEJ p0j;
				Scalar rs = new Scalar(0);

				ptg = random_group_element_test();
				for (i = 0; i < 20; i++)
				{
					sc[i] = random_scalar_order();
					pt[i] = ptg;
					rs += sc[i];
				}

				p0j = pt[0].ToGroupElementJacobian();
				r2 = ctx.EcMultContext.Mult(p0j, rs, szero);

				r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 20), pt.Slice(0, 20), implementation);
				r2 = r2.Negate();
				r = r.AddVariable(r2, out _);
				Assert.True(r.IsInfinity);
			}

			/* Sanity check that zero scalars don't cause problems */
			for (ncount = 0; ncount < 20; ncount++)
			{
				sc[ncount] = random_scalar_order();
				pt[ncount] = random_group_element_test();
			}

			Scalar.Clear(ref sc[0]);
			r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 20), pt.Slice(0, 20), implementation);
			Scalar.Clear(ref sc[1]);
			Scalar.Clear(ref sc[2]);
			Scalar.Clear(ref sc[3]);
			Scalar.Clear(ref sc[4]);

			r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 6), pt.Slice(0, 6), implementation);

			r = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 5), pt.Slice(0, 5), implementation);

			Assert.True(r.IsInfinity);

			/* Run through s0*(t0*P) + s1*(t1*P) exhaustively for many small values of s0, s1, t0, t1 */
			{
				int TOP = 8;
				int s0i, s1i;
				int t0i, t1i;
				GE ptg;
				GEJ ptgj;

				ptg = random_group_element_test();
				ptgj = ptg.ToGroupElementJacobian();

				for (t0i = 0; t0i < TOP; t0i++)
				{
					for (t1i = 0; t1i < TOP; t1i++)
					{
						GEJ t0p, t1p;
						Scalar t0, t1;

						t0 = new Scalar((uint)(t0i + 1) / 2);
						t0.CondNegate(t0i & 1, out t0);
						t1 = new Scalar((uint)(t1i + 1) / 2);
						t1.CondNegate(t1i & 1, out t1);

						t0p = ctx.EcMultContext.Mult(ptgj, t0, szero);
						t1p = ctx.EcMultContext.Mult(ptgj, t1, szero);

						for (s0i = 0; s0i < TOP; s0i++)
						{
							for (s1i = 0; s1i < TOP; s1i++)
							{
								Scalar tmp1, tmp2;
								GEJ expected, actual;

								pt[0] = t0p.ToGroupElement();
								pt[1] = t1p.ToGroupElement();

								sc[0] = new Scalar((uint)(s0i + 1) / 2);
								sc[0].CondNegate(s0i & 1, out sc[0]);
								sc[1] = new Scalar((uint)(s1i + 1) / 2);
								sc[1].CondNegate(s1i & 1, out sc[1]);

								tmp1 = t0 * sc[0];
								tmp2 = t1 * sc[1];
								tmp1 += tmp2;

								expected = ctx.EcMultContext.Mult(ptgj, tmp1, szero);

								actual = ctx.EcMultContext.MultBatch(szero, sc.Slice(0, 2), pt.Slice(0, 2), implementation);

								expected = expected.Negate();
								actual = actual.AddVariable(expected, out _);
								if (!actual.IsInfinity)
								{

								}
								Assert.True(actual.IsInfinity);
							}
						}
					}
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void run_pubkey_comparison()
		{
			byte[] pk1_ser = new byte[33] {
			0x02,
			0x58, 0x84, 0xb3, 0xa2, 0x4b, 0x97, 0x37, 0x88, 0x92, 0x38, 0xa6, 0x26, 0x62, 0x52, 0x35, 0x11,
			0xd0, 0x9a, 0xa1, 0x1b, 0x80, 0x0b, 0x5e, 0x93, 0x80, 0x26, 0x11, 0xef, 0x67, 0x4b, 0xd9, 0x23
		};
			byte[] pk2_ser = new byte[33] {
		0x02,
		0xde, 0x36, 0x0e, 0x87, 0x59, 0x8f, 0x3c, 0x01, 0x36, 0x2a, 0x2a, 0xb8, 0xc6, 0xf4, 0x5e, 0x4d,
		0xb2, 0xc2, 0xd5, 0x03, 0xa7, 0xf9, 0xf1, 0x4f, 0xa8, 0xfa, 0x95, 0xa8, 0xe9, 0x69, 0x76, 0x1c
	};
			ECPubKey pk1;
			ECPubKey pk2;
			//int ecount = 0;

			Assert.True(ECPubKey.TryCreate(pk1_ser, ctx, out _, out pk1));
			Assert.True(ECPubKey.TryCreate(pk2_ser, ctx, out _, out pk2));

			Assert.True(pk1.CompareTo(pk2) < 0);
			Assert.True(pk2.CompareTo(pk1) > 0);
			Assert.True(pk1.CompareTo(pk1) == 0);
			Assert.True(pk2.CompareTo(pk2) == 0);

			/* Make pk2 the same as pk1 but with 3 rather than 2. Note that in
			 * an uncompressed encoding, these would have the opposite ordering */
			pk1_ser[0] = 3;
			Assert.True(ECPubKey.TryCreate(pk1_ser, ctx, out _, out pk2));
			Assert.True(pk1.CompareTo(pk2) < 0);
			Assert.True(pk2.CompareTo(pk1) > 0);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_xonly_pubkey_comparison()
		{
			byte[] pk1_ser = new byte[32] {
		0x58, 0x84, 0xb3, 0xa2, 0x4b, 0x97, 0x37, 0x88, 0x92, 0x38, 0xa6, 0x26, 0x62, 0x52, 0x35, 0x11,
		0xd0, 0x9a, 0xa1, 0x1b, 0x80, 0x0b, 0x5e, 0x93, 0x80, 0x26, 0x11, 0xef, 0x67, 0x4b, 0xd9, 0x23
	};
			byte[] pk2_ser = new byte[32] {
		0xde, 0x36, 0x0e, 0x87, 0x59, 0x8f, 0x3c, 0x01, 0x36, 0x2a, 0x2a, 0xb8, 0xc6, 0xf4, 0x5e, 0x4d,
		0xb2, 0xc2, 0xd5, 0x03, 0xa7, 0xf9, 0xf1, 0x4f, 0xa8, 0xfa, 0x95, 0xa8, 0xe9, 0x69, 0x76, 0x1c
	};
			ECXOnlyPubKey pk1;
			ECXOnlyPubKey pk2;


			Assert.True(ECXOnlyPubKey.TryCreate(pk1_ser, ctx, out pk1));
			Assert.True(ECXOnlyPubKey.TryCreate(pk2_ser, ctx, out pk2));


			Assert.True(pk1.CompareTo(pk2) < 0);
			Assert.True(pk2.CompareTo(pk1) > 0);
			Assert.True(pk1.CompareTo(pk1) == 0);
			Assert.True(pk2.CompareTo(pk2) == 0);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void test_xonly_sort()
		{
			ECXOnlyPubKey[] pk = new ECXOnlyPubKey[5];
			byte[][] pk_ser = new byte[5][];
			for (int o = 0; o < pk_ser.Length; o++)
			{
				pk_ser[o] = new byte[32];
			}
			int i;
			int[] pk_order = new int[5] { 0, 1, 2, 3, 4 };
			pk_ser[0][0] = 5;
			pk_ser[1][0] = 8;
			pk_ser[2][0] = 0x0a;
			pk_ser[3][0] = 0x0b;
			pk_ser[4][0] = 0x0c;
			for (i = 0; i < 5; i++)
			{
				Assert.True(ECXOnlyPubKey.TryCreate(pk_ser[i], ctx, out pk[i]));
			}
			permute(pk_order, 1);
			test_xonly_sort_helper(pk, pk_order, 1);
			permute(pk_order, 2);
			test_xonly_sort_helper(pk, pk_order, 2);
			permute(pk_order, 3);
			test_xonly_sort_helper(pk, pk_order, 3);
			for (i = 0; i < count; i++)
			{
				permute(pk_order, 4);
				test_xonly_sort_helper(pk, pk_order, 4);
			}
			for (i = 0; i < count; i++)
			{
				permute(pk_order, 5);
				test_xonly_sort_helper(pk, pk_order, 5);
			}
			/* Check that sorting also works for random pubkeys */
			for (i = 0; i < count; i++)
			{
				int j;
				ECXOnlyPubKey[] pk_ptr = new ECXOnlyPubKey[5];
				for (j = 0; j < 5; j++)
				{
					pk[j] = rand_xonly_pk();
					pk_ptr[j] = pk[j];
				}
				Array.Sort(pk_ptr, 0, 5);
				for (j = 1; j < 5; j++)
				{
					Assert.True(pk_ptr[j - 1].CompareTo(pk_ptr[j]) <= 0);
				}
			}
		}

		ECXOnlyPubKey rand_xonly_pk()
		{
			ECPrivKey k = null;
			Span<byte> data = stackalloc byte[32];
			while (true)
			{
				secp256k1_rand_bytes_test(data, 32);
				if (NBitcoinContext.Instance.TryCreateECPrivKey(data, out var key) && key is Secp256k1.ECPrivKey)
				{
					k = key;
					break;
				}
			}
			return k.CreatePubKey().ToXOnlyPubKey();
		}

		void test_xonly_sort_helper(ECXOnlyPubKey[] pk, int[] pk_order, int n_pk)
		{
			int i;
			ECXOnlyPubKey[] pk_test = new ECXOnlyPubKey[5];

			for (i = 0; i < n_pk; i++)
			{
				pk_test[i] = pk[pk_order[i]];
			}
			Array.Sort(pk_test, 0, n_pk);
			for (i = 0; i < n_pk; i++)
			{
				Assert.True(pk_test[i].CompareTo(pk[i]) == 0);
			}
		}

		void permute(int[] arr, int n)
		{
			int i;
			for (i = n - 1; i >= 1; i--)
			{
				int tmp, j;
				j = (int)Secp256k1Tests.secp256k1_rand_int((uint)(i + 1));
				tmp = arr[i];
				arr[i] = arr[j];
				arr[j] = tmp;
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		/* In this test we create a combined public key P and a commitment Q = P +
 * hash(P, contract)*G. Then we test that we can sign for both public keys. In
 * order to sign for Q we use the tweak32 argument of partial_sig_combine. */
		public void musig_tweak_test()
		{
			ECPrivKey[] sk = new ECPrivKey[2];
			ECPubKey[] pk = new ECPubKey[2];
			for (int i = 0; i < 2; i++)
			{
				sk[i] = new ECPrivKey(random_scalar_order(), ctx, true);
				pk[i] = sk[i].CreatePubKey();
			}
			byte[] contract = new byte[32];
			secp256k1_rand256(contract);

			var msg32 = new byte[32];
			secp256k1_rand256_test(msg32);
			var pre_session_P = new MusigContext(pk, msg32);
			var P = pre_session_P.AggregatePubKey;
			var P_serialized = P.ToBytes();
			Secp256k1.SHA256 sha = new Secp256k1.SHA256();
			sha.Initialize();
			sha.Write(P_serialized);
			sha.Write(contract);
			byte[] ec_commit_tweak = new byte[32];
			sha.GetHash(ec_commit_tweak);
			var pre_session_Q = pre_session_P.Clone();
			var Q = pre_session_Q.Tweak(ec_commit_tweak);
			var Q_xonly = Q.ToXOnlyPubKey();

			/* Check that musig_pubkey_tweak_add produces same result as
     * xonly_pubkey_tweak_add. */
			Assert.True(Q_xonly.CheckIsTweakedWith(P.ToXOnlyPubKey(), ec_commit_tweak, pre_session_Q.pk_parity));


			/* Test signing for P */
			musig_tweak_test_helper(P.ToXOnlyPubKey(), sk[0], sk[1], pre_session_P);
			/* Test signing for Q */
			musig_tweak_test_helper(Q_xonly, sk[0], sk[1], pre_session_Q);
		}

		void musig_tweak_test_helper(ECXOnlyPubKey combined_pk, ECPrivKey sk0, ECPrivKey sk1, MusigContext pre_session)
		{
			byte[][] session_id = new byte[2][];
			for (int i = 0; i < 2; i++)
			{
				session_id[i] = new byte[32];
				secp256k1_rand256_test(session_id[i]);
			}
			var pk = new[]
			{
				sk0.CreatePubKey(),
				sk1.CreatePubKey(),
			};
			var secnonce = new[]
			{
				MusigPrivNonce.GenerateMusigNonce(ctx, session_id[0], sk0, Array.Empty<byte>(), null, Array.Empty<byte>()),
				MusigPrivNonce.GenerateMusigNonce(ctx, session_id[1], sk1, Array.Empty<byte>(), null, Array.Empty<byte>())
			};
			var pubnonce = new[]
			{
				secnonce[0].CreatePubNonce(),
				secnonce[1].CreatePubNonce(),
			};
			pre_session.ProcessNonces(pubnonce);
			var partial_sig = new[]
			{
				pre_session.Sign(sk0, secnonce[0]),
				pre_session.Sign(sk1, secnonce[1]),
			};
			Assert.True(pre_session.Verify(pk[0], pubnonce[0], partial_sig[0]));
			Assert.True(pre_session.Verify(pk[1], pubnonce[1], partial_sig[1]));
			var final_sig = pre_session.AggregateSignatures(partial_sig);
			Assert.True(combined_pk.SigVerifyBIP340(final_sig, pre_session.msg32));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_MusigTest()
		{
			foreach (var useTweak in new[] { false, true })
				foreach (var useAdaptor in new[] { false, true })
				{
					var msg32 = random_scalar_order().ToBytes();
					var tweak = random_scalar_order().ToBytes();
					var adaptor = ctx.CreateECPrivKey(random_scalar_order());
					var peers = 3;
					var privKeys = new ECPrivKey[peers];
					var privNonces = new MusigPrivNonce[peers];
					var pubNonces = new MusigPubNonce[peers];
					var musig = new MusigContext[peers];
					var sigs = new MusigPartialSignature[peers];
					var pubKeys = new ECPubKey[peers];

					for (int i = 0; i < peers; i++)
					{
						privKeys[i] = ctx.CreateECPrivKey(random_scalar_order());
						pubKeys[i] = privKeys[i].CreatePubKey();
					}

					for (int i = 0; i < peers; i++)
					{
						musig[i] = new MusigContext(pubKeys, msg32);
						privNonces[i] = musig[i].GenerateNonce((uint)i, privKeys[i]);
						pubNonces[i] = privNonces[i].CreatePubNonce();
					}

					for (int i = 0; i < peers; i++)
					{
						if (useTweak)
						{
							musig[i].Tweak(tweak);
						}
						if (useAdaptor)
						{
							musig[i].UseAdaptor(adaptor.CreatePubKey());
						}

						musig[i].ProcessNonces(pubNonces);
						sigs[i] = musig[i].Sign(privKeys[i], privNonces[i]);
					}


					// Verify all the partial sigs
					for (int i = 0; i < peers; i++)
					{
						Assert.True(musig[i].Verify(pubKeys[i], pubNonces[i], sigs[i]));
					}

					// Combine
					var schnorrSig = musig[0].AggregateSignatures(sigs);

					if (useAdaptor)
						schnorrSig = musig[0].Adapt(schnorrSig, adaptor);
					// Verify resulting signature
					// SigningPubKey is the tweaked key if tweaked, or the combined key if not
					Assert.True(musig[0].AggregatePubKey.ToXOnlyPubKey().SigVerifyBIP340(schnorrSig, msg32));
				}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_scriptless_atomic_swap()
		{
			var partial_sig_a = new MusigPartialSignature[2];
			var partial_sig_b = new MusigPartialSignature[2];
			var sk_a = new ECPrivKey[]
				{
						ctx.CreateECPrivKey(random_scalar_order()),
						ctx.CreateECPrivKey(random_scalar_order())
				};
			var sk_b = new ECPrivKey[]
				{
						ctx.CreateECPrivKey(random_scalar_order()),
						ctx.CreateECPrivKey(random_scalar_order())
				};
			var secnonce_a = new MusigPrivNonce[2];
			var secnonce_b = new MusigPrivNonce[2];

			var pk_a = sk_a.Select(p => p.CreatePubKey()).ToArray();
			var pk_b = sk_b.Select(p => p.CreatePubKey()).ToArray();

			var sec_adaptor = ctx.CreateECPrivKey(random_scalar_order());
			var pub_adaptor = sec_adaptor.CreatePubKey();
			byte[] msg32_a = Encoding.ASCII.GetBytes("this is the message blockchain a");
			byte[] msg32_b = Encoding.ASCII.GetBytes("this is the message blockchain b");
			var pre_session_a = new MusigContext(pk_a, msg32_a);
			var combined_pk_a = pre_session_a.AggregatePubKey.ToXOnlyPubKey();
			var pre_session_b = new MusigContext(pk_b, msg32_b);
			var combined_pk_b = pre_session_b.AggregatePubKey.ToXOnlyPubKey();

			for (int i = 0; i < 2; i++)
			{
				secnonce_a[i] = pre_session_a.GenerateNonce((uint)i, sk_a[i]);
				secnonce_b[i] = pre_session_b.GenerateNonce((uint)i, sk_b[i]);
			}

			var pubnonce_a = secnonce_a.Select(p => p.CreatePubNonce()).ToArray();
			var pubnonce_b = secnonce_b.Select(p => p.CreatePubNonce()).ToArray();

			/* Step 2: Exchange nonces */
			pre_session_a.UseAdaptor(pub_adaptor);
			pre_session_a.ProcessNonces(pubnonce_a);

			pre_session_b.UseAdaptor(pub_adaptor);
			pre_session_b.ProcessNonces(pubnonce_b);

			/* Step 3: Signer 0 produces partial signatures for both chains. */
			partial_sig_a[0] = pre_session_a.Sign(sk_a[0], secnonce_a[0]);
			partial_sig_b[0] = pre_session_b.Sign(sk_b[0], secnonce_b[0]);

			/* Step 4: Signer 1 receives partial signatures, verifies them and creates a
     * partial signature to send B-coins to signer 0. */
			Assert.True(pre_session_a.Verify(pk_a[0], pubnonce_a[0], partial_sig_a[0]));
			Assert.True(pre_session_b.Verify(pk_b[0], pubnonce_b[0], partial_sig_b[0]));
			partial_sig_b[1] = pre_session_b.Sign(sk_b[1], secnonce_b[1]);

			/* Step 5: Signer 0 adapts its own partial signature and combines it with the
     * partial signature from signer 1. This results in a complete signature which
     * is broadcasted by signer 0 to take B-coins. */
			var final_sig_b = pre_session_b.AggregateSignatures(partial_sig_b);
			final_sig_b = pre_session_b.Adapt(final_sig_b, sec_adaptor);
			Assert.True(combined_pk_b.SigVerifyBIP340(final_sig_b, msg32_b));

			/* Step 6: Signer 1 extracts adaptor from the published signature, applies it to
   * other partial signature, and takes A-coins. */
			var sec_adaptor_extracted = pre_session_b.Extract(final_sig_b, partial_sig_b);
			Assert.Equal(sec_adaptor, sec_adaptor_extracted);
			partial_sig_a[1] = pre_session_a.Sign(sk_a[1], secnonce_a[1]);
			var final_sig_a = pre_session_a.AggregateSignatures(partial_sig_a);
			final_sig_a = pre_session_a.Adapt(final_sig_a, sec_adaptor);
			Assert.True(combined_pk_a.SigVerifyBIP340(final_sig_a, msg32_a));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_sig_agg_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/sig_agg_vectors.json"));
			var pubkeys = GetArray<string>(root["pubkeys"]).ToArray();
			var msg = Encoders.Hex.DecodeData(root["msg"].Value<string>());
			var pnonces = GetArray<string>(root["pnonces"]).ToArray();
			var tweaks = GetArray<string>(root["tweaks"]).ToArray();
			var psigs = GetArray<string>(root["psigs"]).ToArray();
			foreach (var item in (JArray)root["valid_test_cases"])
			{
				var aggnonce = new MusigPubNonce(Encoders.Hex.DecodeData(item["aggnonce"].Value<string>()));
				var nonces = GetArray<int>(item["nonce_indices"]).Select(p => new MusigPubNonce(Encoders.Hex.DecodeData(pnonces[p]))).ToArray();
				AssertEx.EqualBytes(aggnonce.ToBytes(), MusigPubNonce.Aggregate(nonces).ToBytes());
				var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();

				var ctx = new MusigContext(keys, msg);

				foreach (var tweak in GetArray<int>(item["tweak_indices"])
					.Select(p => Encoders.Hex.DecodeData(tweaks[p]))
					.Zip(
					GetArray<bool>(item["is_xonly"])
				))
				{
					ctx.Tweak(tweak.First, tweak.Second);
				}

				ctx.Process(aggnonce);
				var sigs = GetArray<int>(item["psig_indices"]).Select(p => new MusigPartialSignature(Encoders.Hex.DecodeData(psigs[p]))).ToArray();
				for (int i = 0; i < sigs.Length; i++)
				{
					Assert.True(ctx.Verify(keys[i], nonces[i], sigs[i]));
				}
				var actual = ctx.AggregateSignatures(sigs).ToBytes();
				var expected = item["expected"].Value<string>();
				AssertEx.EqualBytes(expected, actual);
				Logs.WriteLine("Passed");
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_det_sign_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/det_sign_vectors.json"));
			var pubkeys = GetArray<string>(root["pubkeys"]).ToArray();
			var msgs = GetArray<string>(root["msgs"]).ToArray();
			var sk = new ECPrivKey(Encoders.Hex.DecodeData(root["sk"].Value<string>()), null);
			foreach (var item in (JArray)root["valid_test_cases"])
			{
				var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();
				var rand = item["rand"].Type == JTokenType.Null ? null : Encoders.Hex.DecodeData(item["rand"].Value<string>());
				var aggothernonce = new MusigPubNonce(Encoders.Hex.DecodeData(item["aggothernonce"].Value<string>()));
				var msg = Encoders.Hex.DecodeData(msgs[item["msg_index"].Value<int>()]);
				var signed_index = item["signer_index"].Value<int>();
				var signer = ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[signed_index]));
				var expected = GetArray<string>(item["expected"]);
				var ctx = new MusigContext(keys, msg);
				foreach (var tweak in GetArray<string>(item["tweaks"])
					.Select(p => Encoders.Hex.DecodeData(p))
					.Zip(
					GetArray<bool>(item["is_xonly"])
				))
				{
					ctx.Tweak(tweak.First, tweak.Second);
				}
				ctx.Process(aggothernonce);
				var sig = ctx.DeterministicSign(sk, rand);
				AssertEx.EqualBytes(expected[0], sig.PubNonce.ToBytes());
				AssertEx.EqualBytes(expected[1], sig.Signature.ToBytes());
				Assert.True(ctx.Verify(sk.CreatePubKey(), sig.PubNonce, sig.Signature));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_tweak_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/tweak_vectors.json"));
			var sk = new ECPrivKey(Encoders.Hex.DecodeData(root["sk"].Value<string>()), null);
			var pubkeys = GetArray<string>(root["pubkeys"]).ToArray();
			var secnonce = root["secnonce"].Value<string>();
			var pnonces = GetArray<string>(root["pnonces"]).ToArray();
			var ptweaks = GetArray<string>(root["tweaks"]).ToArray();
			var msg = Encoders.Hex.DecodeData(root["msg"].Value<string>());
			foreach (var item in (JArray)root["valid_test_cases"])
			{
				var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();
				var nonces = GetArray<int>(item["nonce_indices"]).Select(p => new MusigPubNonce(Encoders.Hex.DecodeData(pnonces[p]))).ToArray();
				var tweaks = GetArray<int>(item["tweak_indices"]).Select(p => Encoders.Hex.DecodeData(ptweaks[p])).ToArray();
				var is_xonly = GetArray<bool>(item["is_xonly"]);

				var ctx = new MusigContext(keys, msg);
				for (int i = 0; i < tweaks.Length; i++)
				{
					ctx.Tweak(tweaks[i], is_xonly[i]);
				}
				ctx.ProcessNonces(nonces);
				var result = ctx.Sign(sk, ToMusigPrivNonce(secnonce));
				AssertEx.EqualBytes(item["expected"].Value<string>(), result.ToBytes());
			}
		}

		private MusigPrivNonce ToMusigPrivNonce(string hex)
		{
			var b = Encoders.Hex.DecodeData(hex);
			return new MusigPrivNonce(ECPrivKey.Create(b.AsSpan().Slice(0, 32)),
									  ECPrivKey.Create(b.AsSpan().Slice(32, 32)));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_sign_verify_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/sign_verify_vectors.json"));
			var pubkeys = GetArray<string>(root["pubkeys"]).ToArray();
			var msgs = GetArray<string>(root["msgs"]).ToArray();
			var secnonces = GetArray<string>(root["secnonces"]).ToArray();
			var pnonces = GetArray<string>(root["pnonces"]).ToArray();
			var aggnonces = GetArray<string>(root["aggnonces"]).ToArray();
			var sk = new ECPrivKey(Encoders.Hex.DecodeData(root["sk"].Value<string>()), null);
			foreach (var item in (JArray)root["valid_test_cases"])
			{
				var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();
				var nonces = GetArray<int>(item["nonce_indices"]).Select(p => new MusigPubNonce(Encoders.Hex.DecodeData(pnonces[p]))).ToArray();
				var agg_nonce = new MusigPubNonce(Encoders.Hex.DecodeData(aggnonces[item["aggnonce_index"].Value<int>()]));
				var msg = Encoders.Hex.DecodeData(msgs[item["msg_index"].Value<int>()]);
				var ctx = new MusigContext(keys, msg);
				ctx.ProcessNonces(nonces);
				AssertEx.EqualBytes(agg_nonce.ToBytes(), ctx.AggregateNonce.ToBytes());
				var secnonce = Encoders.Hex.DecodeData(secnonces[0]).AsSpan();
				var s = new MusigPrivNonce(
					ECPrivKey.Create(secnonce.Slice(0, 32)),
					ECPrivKey.Create(secnonce.Slice(32, 32))
					);
				AssertEx.EqualBytes(s.CreatePubNonce().ToBytes(), pnonces[0]);
				var sig = ctx.Sign(sk, s);
				var expected = item["expected"].Value<string>();
				AssertEx.EqualBytes(expected, sig.ToBytes());
				Assert.True(ctx.Verify(sk.CreatePubKey(), s.CreatePubNonce(), sig));
			}
			foreach (var item in ((JArray)root["sign_error_test_cases"]).Concat((JArray)root["verify_error_test_cases"]))
			{
				bool invalidPubKey = false;
				bool invalidNonceAgg = false;
				bool invalidSecnonce = false;
				bool invalidNonce = false;
				var comment = item["comment"].Value<string>();
				try
				{
					invalidPubKey = true;
					var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();
					invalidPubKey = false;

					var aggnonce_index = item["aggnonce_index"];
					if (aggnonce_index == null)
					{
						invalidNonce = true;
						GetArray<int>(item["nonce_indices"]).Select(p => new MusigPubNonce(Encoders.Hex.DecodeData(pnonces[p]))).ToArray();
						invalidNonce = false;
					}
					invalidNonceAgg = true;
					var agg_nonce = new MusigPubNonce(Encoders.Hex.DecodeData(aggnonces[aggnonce_index.Value<int>()]));
					invalidNonceAgg = false;
					var msg = Encoders.Hex.DecodeData(msgs[item["msg_index"].Value<int>()]);
					invalidSecnonce = true;
					var secnonce = Encoders.Hex.DecodeData(secnonces[item["secnonce_index"].Value<int>()]).AsSpan();
					var s = new MusigPrivNonce(
						ECPrivKey.Create(secnonce.Slice(0, 32)),
						ECPrivKey.Create(secnonce.Slice(32, 32))
						);
					invalidSecnonce = false;
					Assert.False(true, comment);
				}
				catch (Exception ex) when (!(ex is XunitException))
				{
					var err = item["error"]?["contrib"]?.Value<string>();
					if (invalidPubKey != (err == "pubkey"))
						Assert.False(true, comment);
					if (invalidNonceAgg != (err == "aggnonce"))
						Assert.False(true, comment);
					if (invalidNonce != (err == "pubnonce"))
						Assert.False(true, comment);
					if (invalidSecnonce != (err == null))
						Assert.False(true, comment);
				}
			}
			foreach (var item in (JArray)root["verify_fail_test_cases"])
			{
				var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();
				var nonces = GetArray<int>(item["nonce_indices"]).Select(p => new MusigPubNonce(Encoders.Hex.DecodeData(pnonces[p]))).ToArray();
				var msg = Encoders.Hex.DecodeData(msgs[item["msg_index"].Value<int>()]);
				var signed_index = item["signer_index"].Value<int>();
				var signer = ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[signed_index]));
				var comment = item["comment"].Value<string>();
				MusigPartialSignature sig;
				try
				{
					sig = new MusigPartialSignature(Encoders.Hex.DecodeData(item["sig"].Value<string>()));
				}
				catch (ArgumentException)
				{
					continue;
				}
				var ctx = new MusigContext(keys, msg);
				ctx.ProcessNonces(nonces);
				Assert.False(ctx.Verify(signer, nonces[signed_index], sig));
			}
		}


		private Scalar rand_scalar()
		{
			Span<byte> buf32 = stackalloc byte[32];
			Random.Generate(buf32);
			return new Scalar(buf32);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ecdsaadaptor_testsvectors()
		{
			ECDSAEncryptedSignature GetAdaptorSig(JObject o)
			{
				ECDSAEncryptedSignature.TryCreate(Encoders.Hex.DecodeData(o["adaptor_sig"].Value<string>()), out var sig);
				return sig;
			}
			byte[] GetMessage(JObject o)
			{
				return Encoders.Hex.DecodeData(o["message_hash"].Value<string>());
			}
			ECPubKey GetPublicSigningKey(JObject o)
			{
				return ECPubKey.Create(Encoders.Hex.DecodeData(o["public_signing_key"].Value<string>()));
			}
			ECPubKey GetEncryptionKey(JObject o)
			{
				return ECPubKey.Create(Encoders.Hex.DecodeData(o["encryption_key"].Value<string>()));
			}
			ECPrivKey GetDecryptionKey(JObject o)
			{
				return ECPrivKey.Create(Encoders.Hex.DecodeData(o["decryption_key"].Value<string>()));
			}
			SecpECDSASignature GetSignature(JObject o)
			{
				SecpECDSASignature.TryCreateFromCompact(Encoders.Hex.DecodeData(o["signature"].Value<string>()), out var sig);
				return sig;
			}
			var root = JArray.Parse(File.ReadAllText("data/ecdsaadaptor/ecdsa_adaptor.json"));
			foreach (var test in root.OfType<JObject>())
			{
				var error = test["error"]?.Value<string>();
				var kind = test["kind"].Value<string>();
				if (kind == "verification")
				{
					var adaptorSig = GetAdaptorSig(test);
					var message = GetMessage(test);
					var publicSigningKey = GetPublicSigningKey(test);
					var encryptionKey = GetEncryptionKey(test);
					var decryptionKey = GetDecryptionKey(test);
					var signature = GetSignature(test);

					if (error is null)
					{
						Assert.Equal(decryptionKey.CreatePubKey(), encryptionKey);
						Assert.True(encryptionKey.TryRecoverDecryptionKey(signature, adaptorSig, out var d));
						Assert.Equal(decryptionKey, d);

						Assert.True(publicSigningKey.SigVerifyEncryptedECDSA(adaptorSig, message, encryptionKey));

						var sig = decryptionKey.DecryptECDSASignature(adaptorSig);
						Assert.Equal(signature, sig);

						Assert.True(publicSigningKey.SigVerify(sig, message));
					}
					else
					{
						Assert.Equal(decryptionKey.CreatePubKey(), encryptionKey);
						Assert.False(encryptionKey.TryRecoverDecryptionKey(signature, adaptorSig, out var d));
						Assert.Null(d);
						Assert.False(publicSigningKey.SigVerifyEncryptedECDSA(adaptorSig, message, encryptionKey));
					}
				}
				if (kind == "recovery")
				{
					var adaptorSig = GetAdaptorSig(test);
					var encryptionKey = GetEncryptionKey(test);
					var signature = GetSignature(test);
					if (error is null)
					{
						var decryptionKey = GetDecryptionKey(test);
						Assert.Equal(decryptionKey.CreatePubKey(), encryptionKey);
						Assert.True(encryptionKey.TryRecoverDecryptionKey(signature, adaptorSig, out var d));
					}
					else
					{
						Assert.False(encryptionKey.TryRecoverDecryptionKey(signature, adaptorSig, out var d));
					}
				}
				if (kind == "serialization")
				{
					if (error is not null)
						Assert.Null(GetAdaptorSig(test));
					else
						Assert.NotNull(GetAdaptorSig(test));
				}

			}
		}
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ecdsaadaptor_tests()
		{
			var secKey = Context.Instance.CreateECPrivKey(rand_scalar());
			Span<byte> msg = stackalloc byte[32];
			Random.Generate(msg);
			var adaptor_secret = Context.Instance.CreateECPrivKey(rand_scalar());
			var pubkey = secKey.CreatePubKey();
			var adaptor = adaptor_secret.CreatePubKey();
			Assert.True(secKey.TrySignEncryptedECDSA(msg, adaptor, out var adaptor_sig));
			{
				/* Test adaptor_sig_serialize roundtrip */
				Span<byte> adaptor_sig_tmp = stackalloc byte[162];
				Span<byte> adaptor_sig_tmp2 = stackalloc byte[162];
				adaptor_sig.WriteToSpan(adaptor_sig_tmp);
				Assert.True(ECDSAEncryptedSignature.TryCreate(adaptor_sig_tmp, out var adaptor_sig2));
				adaptor_sig2.WriteToSpan(adaptor_sig_tmp2);
				Assert.True(adaptor_sig_tmp.SequenceEqual(adaptor_sig_tmp2));
			}
			Assert.True(pubkey.SigVerifyEncryptedECDSA(adaptor_sig, msg, adaptor));
			{
				Span<byte> adaptor_sig_tmp = stackalloc byte[162];
				adaptor_sig.WriteToSpan(adaptor_sig_tmp);
				rand_flip_bit(adaptor_sig_tmp);
				if (ECDSAEncryptedSignature.TryCreate(adaptor_sig_tmp, out var sigg))
				{
					Assert.False(pubkey.SigVerifyEncryptedECDSA(sigg, msg, adaptor));
				}
			}
			Assert.False(adaptor.SigVerifyEncryptedECDSA(adaptor_sig, msg, adaptor));
			{
				Span<byte> msg_tmp = stackalloc byte[32];
				msg.CopyTo(msg_tmp);
				rand_flip_bit(msg_tmp);
				Assert.False(pubkey.SigVerifyEncryptedECDSA(adaptor_sig, msg_tmp, adaptor));
			}
			Assert.False(pubkey.SigVerifyEncryptedECDSA(adaptor_sig, msg, pubkey));


			var sig = adaptor_sig.DecryptECDSASignature(adaptor_secret);
			///* Test adaptor_adapt */
			Assert.True(pubkey.SigVerify(sig, msg));
			{
				/* Test adaptor_extract_secret */
				Assert.True(adaptor_sig.TryRecoverDecryptionKey(sig, adaptor, out var adaptor_secret2));
				Assert.Equal(adaptor_secret, adaptor_secret2);
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanVerifyDLEQ()
		{
			var y = new ECPubKey(random_group_element_test(), null);
			var x = new ECPrivKey(random_scalar_order(), null, true);
			var z = new ECPubKey(ECMultContext.Instance.Mult(y.Q.ToGroupElementJacobian(), x.sec, Scalar.Zero).ToGroupElement(), null);
			var proof = x.ProveDLEQ(y, z);
			Assert.True(x.CreatePubKey().VerifyDLEQ(proof, y, z));
			var x2 = new ECPrivKey(random_scalar_order(), null, true);
			Assert.False(x2.CreatePubKey().VerifyDLEQ(proof, y, z));
			var proof2 = new DLEQProof(proof.b.Add(Scalar.One), proof.c);
			Assert.False(x.CreatePubKey().VerifyDLEQ(proof2, y, z));
			proof2 = new DLEQProof(proof.b, proof.c.Add(Scalar.One));
			Assert.False(x.CreatePubKey().VerifyDLEQ(proof2, y, z));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_key_agg_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/key_agg_vectors.json"));
			var pubkeys = GetArray<string>(root["pubkeys"]).ToArray();
			var tweaks = GetArray<string>(root["tweaks"]).Select(p => Encoders.Hex.DecodeData(p)).ToArray();

			foreach (var item in (JArray)root["valid_test_cases"])
			{
				var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();
				var result = ECPubKey.MusigAggregate(keys).ToXOnlyPubKey();
				var expected = ECXOnlyPubKey.Create(Encoders.Hex.DecodeData(item["expected"].Value<string>()));
				AssertEx.EqualBytes(expected.ToBytes(), result.ToBytes());
			}

			foreach (var item in (JArray)root["error_test_cases"])
			{
				var keys_indices = GetArray<int>(item["key_indices"]);
				var tweak_indices = GetArray<int>(item["tweak_indices"]);
				var is_xonly = GetArray<bool>(item["is_xonly"]);
				if (item["error"]["signer"]?.Value<int>() is int signer)
				{
					for (int i = 0; i < keys_indices.Length; i++)
					{
						try
						{
							ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[keys_indices[i]]));
							if (i == signer)
								Assert.False(true);
						}
						catch (FormatException)
						{
							Assert.Equal(signer, i);
						}
					}
				}
				else
				{
					var keys = GetArray<int>(item["key_indices"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(pubkeys[p]))).ToArray();
					var result = ECPubKey.MusigAggregate(keys);
					Assert.ThrowsAny<Exception>(() =>
					{
						for (int i = 0; i < tweak_indices.Length; i++)
						{
							if (is_xonly[i])
							{
								result = result.ToXOnlyPubKey().AddTweak(tweaks[tweak_indices[i]]);
							}
							else
							{
								result = result.AddTweak(tweaks[tweak_indices[i]]);
							}
						}
					});
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void schnorr_adaptor_tests()
		{
			var anotherKey = new ECPrivKey(Encoders.Hex.DecodeData("f44a81a036d700ca59c3c02e3dd92841a389e1aa8ea572492484437714b62aee"), null);
			var anotherPk = anotherKey.CreateXOnlyPubKey();
			var anotherAdaptorKey = new ECPrivKey(Encoders.Hex.DecodeData("4fad346c63cd9b4a5363675d382703c5d1ea4f76d34a60d4babbf8c2bd77a4ef"), null);
			var anotherAdaptor = anotherAdaptorKey.CreatePubKey();
			var anotherMsg = Encoders.Hex.DecodeData("4fad346c63cd9b4a5363675d382703c5d1ea4f76d34a60d4babbf8c2bd77a4ef");
			var vectors = new[]
			{
				new
				{
					PrivateKey = "688c77bc2d5aaff5491cf309d4753b732135470d05b7b2cd21add0744fe97bef",
					Message = "024bdd11f2144e825db05759bdd9041367a420fad14b665fd08af5b42056e5e2",
					AdaptorPrivateKey = "475697a71a74ff3f2a8f150534e9b67d4b0b6561fab86fcaa51f8c9d6c9db8c6"
				},
				new
				{
					PrivateKey = "0af83c21559b2d26cc7cff555bd90e0e3dca63b370e989f9abfa20be6d2ce3d6",
					Message = "c81e4db5d6cf653bfba0dc9edfa23cc507251ce288e0294ef9fca11c5a4636ff",
					AdaptorPrivateKey = "6c4bfbd4c93624d3bd95b177d1f814ee2e11ca3facecdf00475b6c5a638f304d"
				},
				new
				{
					PrivateKey = "e207a43edf9e82329f700f0c60d9b1fa60f9eeb8c9d6d54d246c895807911adc",
					Message = "6e2b7cf1d84886790fe418467cb38f2ef6bf7ba69c8af959b61fe9294b3458a2",
					AdaptorPrivateKey = "0a9b8e3c1b9e5166e3697f6680cf61a4434605d12739b630ddeb6f8f24163952"
				}
			};
			foreach (var v in vectors)
			{
				var key = new ECPrivKey(Encoders.Hex.DecodeData(v.PrivateKey), null);
				var pk = key.CreateXOnlyPubKey();
				var adaptorKey = new ECPrivKey(Encoders.Hex.DecodeData(v.AdaptorPrivateKey), null);
				var adaptor = adaptorKey.CreatePubKey();
				var msg = Encoders.Hex.DecodeData(v.Message);
				var encryptedSignature = key.SignEncryptedBIP340(msg, adaptor);
				Assert.True(pk.SigVerifyEncryptedBIP340(encryptedSignature, msg, adaptor));

				var schnorrSig = adaptorKey.DecryptBIP340Signature(encryptedSignature);
				Assert.True(pk.SigVerifyBIP340(schnorrSig, msg));

				Assert.True(adaptor.TryRecoverDecryptionKey(schnorrSig, encryptedSignature, out var adaptorKey2));
				Assert.Equal(adaptorKey, adaptorKey2);

				// Negative tests
				Assert.False(anotherPk.SigVerifyEncryptedBIP340(encryptedSignature, msg, adaptor));
				Assert.False(pk.SigVerifyEncryptedBIP340(encryptedSignature, msg, anotherAdaptor));
				Assert.False(pk.SigVerifyEncryptedBIP340(encryptedSignature, anotherMsg, adaptor));
				var originalSignature = encryptedSignature;
				encryptedSignature = new SchnorrEncryptedSignature(originalSignature.R, originalSignature.s_hat.Add(Scalar.One), originalSignature.need_negation);
				Assert.False(pk.SigVerifyEncryptedBIP340(encryptedSignature, msg, adaptor));
				encryptedSignature = new SchnorrEncryptedSignature(originalSignature.R, originalSignature.s_hat, !originalSignature.need_negation);
				Assert.False(pk.SigVerifyEncryptedBIP340(encryptedSignature, msg, adaptor));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_key_sort_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/key_sort_vectors.json"));
			var pubkeys = GetArray<string>(root["pubkeys"]).Select(p => new PubKey(Encoders.Hex.DecodeData(p))).ToArray();
			var sorted_pubkeys = GetArray<string>(root["sorted_pubkeys"]).Select(p => new PubKey(Encoders.Hex.DecodeData(p))).ToArray();
			Array.Sort(pubkeys, PubKeyComparer.Instance);
			AssertEx.CollectionEquals(pubkeys, sorted_pubkeys);

			var pubkeys2 = GetArray<string>(root["pubkeys"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(p))).ToArray();
			var sorted_pubkeys2 = GetArray<string>(root["sorted_pubkeys"]).Select(p => ECPubKey.Create(Encoders.Hex.DecodeData(p))).ToArray();
			Array.Sort(pubkeys2);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_nonce_agg_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/nonce_agg_vectors.json"));
			var pnonces = GetArray<string>(root["pnonces"]);
			foreach (var item in (JArray)root["valid_test_cases"])
			{
				var expected = item["expected"].Value<string>().ToLowerInvariant();
				var pnonce_indices = GetArray<int>(item["pnonce_indices"]);
				var nonces = pnonce_indices.Select(i => new MusigPubNonce(Encoders.Hex.DecodeData(pnonces[i]))).ToArray();
				var actual = MusigPubNonce.Aggregate(nonces);
				AssertEx.EqualBytes(expected, actual.ToBytes());

			}
			foreach (var item in (JArray)root["error_test_cases"])
			{
				var pnonce_indices = GetArray<int>(item["pnonce_indices"]);
				var signer = item["error"]["signer"].Value<int>();
				for (int i = 0; i < pnonce_indices.Length; i++)
				{
					try
					{
						new MusigPubNonce(Encoders.Hex.DecodeData(pnonces[pnonce_indices[i]]));
						if (signer == i)
							Assert.False(true);
					}
					catch (FormatException)
					{
						Assert.Equal(signer, i);
					}
				}
			}
		}

		private T[] GetArray<T>(JToken jToken)
		{
			return ((JArray)jToken).Select(c => c.Value<T>()).ToArray();
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void musig_nonce_gen_vectors()
		{
			var root = JObject.Parse(File.ReadAllText("data/musig/nonce_gen_vectors.json"));
			foreach (var item in (JArray)root["test_cases"])
			{
				var rand_ = ParseHex(item["rand_"]);
				var sk = ParseHex(item["sk"]);
				var msg = ParseHex(item["msg"]);
				var aggpk = ParseHex(item["aggpk"]);
				var extra_in = ParseHex(item["extra_in"]);
				var expected = item["expected"].Value<string>().ToLowerInvariant();
				Scalar[] res = new Scalar[2];
				MusigPrivNonce.secp256k1_nonce_function_musig(rand_, sk, aggpk, msg, extra_in, res);
				var actual = new byte[64].AsSpan();
				res[0].WriteToSpan(actual);
				res[1].WriteToSpan(actual.Slice(32));
				AssertEx.EqualBytes(expected, actual);
			}
		}

		private byte[] ParseHex(JToken v)
		{
			if (v is null || v.Type != JTokenType.String)
				return null;
			return Encoders.Hex.DecodeData(v.Value<string>());
		}

		private byte[] ToBytes(SessionValues sessionCache)
		{
			var b = new byte[65];
			sessionCache.b.WriteToSpan(b);
			sessionCache.e.WriteToSpan(b.AsSpan().Slice(32));
			b[64] = (byte)(sessionCache.r.y.IsOdd ? 1 : 0);
			return b;
		}

		private static ECPubKey[] GetPubKeys(string[] privKeys)
		{
			var pubkeys = new ECPubKey[privKeys.Length];
			int i = 0;
			foreach (var k in privKeys)
			{
				Assert.True(ECPrivKey.TryCreate(Encoders.Hex.DecodeData(k.ToLower()), out var eck));
				pubkeys[i++] = eck.CreatePubKey();
			}

			return pubkeys;
		}

	}
}
#endif
