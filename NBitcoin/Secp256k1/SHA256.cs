#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class SHA256 : IDisposable
	{
		SHA256Managed sha = new SHA256Managed();
		int _Pos;
		byte[] _Buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(64);
		public void Write(ReadOnlySpan<byte> buffer)
		{
			int copied = 0;
			int toCopy = 0;
			var innerSpan = new Span<byte>(_Buffer, _Pos, _Buffer.Length - _Pos);
			while (!buffer.IsEmpty)
			{
				toCopy = Math.Min(innerSpan.Length, buffer.Length);
				buffer.Slice(0, toCopy).CopyTo(innerSpan.Slice(0, toCopy));
				buffer = buffer.Slice(toCopy);
				innerSpan = innerSpan.Slice(toCopy);
				copied += toCopy;
				_Pos += toCopy;
				if (ProcessBlockIfNeeded())
					innerSpan = _Buffer.AsSpan();
			}
		}
		private bool ProcessBlockIfNeeded()
		{
			if (_Pos == _Buffer.Length)
			{
				ProcessBlock();
				return true;
			}
			return false;
		}
		private void ProcessBlock()
		{
			sha.TransformBlock(_Buffer, 0, _Pos, null, -1);
			_Pos = 0;
		}
		public void GetHash(Span<byte> output)
		{
			ProcessBlock();
			sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			var hash1 = sha.Hash;
			hash1.AsSpan().CopyTo(output);
		}

		public void Dispose()
		{
			System.Buffers.ArrayPool<byte>.Shared.Return(_Buffer, true);
			sha.Dispose();
		}
	}
}
#endif
