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
		public void Initialize()
		{
			sha.Initialize();
			_Pos = 0;
		}
		/// <summary>
		/// Initializes a sha256 struct and writes the 64 byte string
		/// SHA256(tag)||SHA256(tag) into it.
		/// </summary>
		/// <param name="tag"></param>
		public void InitializeTagged(ReadOnlySpan<byte> tag)
		{
			Span<byte> buf = stackalloc byte[32];
			Initialize();
			Write(tag);
			GetHash(buf);
			Initialize();
			Write(buf);
			Write(buf);
		}
		/// <summary>
		/// Initializes a sha256 struct and writes the 64 byte string
		/// SHA256(tag)||SHA256(tag) into it.
		/// </summary>
		/// <param name="tag"></param>
		public void InitializeTagged(string tag)
		{
			InitializeTagged(Encoding.ASCII.GetBytes(tag));
		}
		System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
		int _Pos;
		byte[] _Buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(64);
		public void Write(ReadOnlySpan<byte> buffer)
		{
			int copied = 0;
			var innerSpan = new Span<byte>(_Buffer, _Pos, _Buffer.Length - _Pos);
			while (!buffer.IsEmpty)
			{
				int toCopy = Math.Min(innerSpan.Length, buffer.Length);
				buffer.Slice(0, toCopy).CopyTo(innerSpan.Slice(0, toCopy));
				buffer = buffer.Slice(toCopy);
				innerSpan = innerSpan.Slice(toCopy);
				copied += toCopy;
				_Pos += toCopy;
				if (ProcessBlockIfNeeded())
					innerSpan = _Buffer.AsSpan();
			}
		}
		public void Write(byte b)
		{
			_Buffer[_Pos] = b;
			_Pos++;
			ProcessBlockIfNeeded();
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

		public byte[] GetHash()
		{
			var r = new byte[32];
			GetHash(r);
			return r;
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
