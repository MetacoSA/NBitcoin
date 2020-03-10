#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Secp256k1
{
	public class HMACSHA256 : IDisposable
	{
		Secp256k1.SHA256? inner, outer;
		public HMACSHA256()
		{

		}
		public HMACSHA256(ReadOnlySpan<byte> key)
		{
			Initialize(key);
		}

		public void Initialize(ReadOnlySpan<byte> key)
		{
			int n;
			Span<byte> rkey = stackalloc byte[64];
			rkey.Clear();
			if (key.Length <= 64)
			{
				key.CopyTo(rkey);
			}
			else
			{
				using var sha = new NBitcoin.Secp256k1.SHA256();
				sha.Write(key);
				sha.GetHash(rkey);
			}
			outer = new Secp256k1.SHA256();
			for (n = 0; n < 64; n++)
			{
				rkey[n] ^= 0x5c;
			}
			outer.Write(rkey);

			inner = new Secp256k1.SHA256();
			for (n = 0; n < 64; n++)
			{
				rkey[n] ^= 0x5c ^ 0x36;
			}
			inner.Write(rkey);
			rkey.Clear();
		}

		public void Write32(ReadOnlySpan<byte> data)
		{
			if (inner is null)
				throw new InvalidOperationException("You need to call HMACSHA256.Initialize first");
			inner.Write(data);
		}

		public void Finalize(Span<byte> output)
		{
			if (inner is null || outer is null)
				throw new InvalidOperationException("You need to call HMACSHA256.Initialize first");
			Span<byte> temp = stackalloc byte[32];
			inner.GetHash(temp);
			outer.Write(temp);
			temp.Clear();
			outer.GetHash(output);
		}

		public void Dispose()
		{
			inner?.Dispose();
			outer?.Dispose();
		}
	}
}
#nullable restore
#endif
