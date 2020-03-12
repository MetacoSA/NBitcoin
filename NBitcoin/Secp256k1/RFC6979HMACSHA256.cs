#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#else
	internal
#endif
	class RFC6979HMACSHA256 : IDisposable
	{
		byte[]? v;
		byte[]? k;
		bool retry;

		public void Initialize(ReadOnlySpan<byte> key)
		{
			Span<byte> one = stackalloc byte[1];
			one[0] = 1;
			Span<byte> zero = stackalloc byte[1];
			zero[0] = 0;
			v = new byte[32];
			k = new byte[32];

			v.AsSpan().Fill(1); /* RFC6979 3.2.b. */
			k.AsSpan().Fill(0); /* RFC6979 3.2.c. */

			using var hmac = new HMACSHA256();
			/* RFC6979 3.2.d. */
			hmac.Initialize(k);
			hmac.Write32(v);
			hmac.Write32(zero);
			hmac.Write32(key);
			hmac.Finalize(k);
			hmac.Initialize(k);
			hmac.Write32(v);
			hmac.Finalize(v);

			/* RFC6979 3.2.f. */
			hmac.Initialize(k);
			hmac.Write32(v);
			hmac.Write32(one);
			hmac.Write32(key);
			hmac.Finalize(k);
			hmac.Initialize(k);
			hmac.Write32(v);
			hmac.Finalize(v);
			retry = false;
		}

		public void Generate(Span<byte> output)
		{
			/* RFC6979 3.2.h. */
			Span<byte> zero = stackalloc byte[1];
			zero[0] = 0;
			var outlen = output.Length;
			using var hmac = new HMACSHA256();
			if (retry)
			{
				hmac.Initialize(k);
				hmac.Write32(v);
				hmac.Write32(zero);
				hmac.Finalize(k);
				hmac.Initialize(k);
				hmac.Write32(v);
				hmac.Finalize(v);
			}

			while (outlen > 0)
			{
				int now = outlen;
				hmac.Initialize(k);
				hmac.Write32(v);
				hmac.Finalize(v);
				if (now > 32)
				{
					now = 32;
				}
				v.AsSpan().Slice(0, now).CopyTo(output);
				output = output.Slice(now);
				outlen -= now;
			}
			retry = true;
		}
		public void Dispose()
		{
			k?.AsSpan().Fill(0);
			v?.AsSpan().Fill(0);
			retry = false;
		}
	}
}
#nullable restore
#endif
