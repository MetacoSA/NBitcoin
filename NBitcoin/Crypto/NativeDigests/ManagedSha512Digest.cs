using NBitcoin.BouncyCastle.Crypto;
using System;

namespace NBitcoin.Crypto.NativeDigests
{
#if !NETSTANDARD1X && !NONATIVEHASH
	using System.Security.Cryptography;

	/// <summary>
	/// A wrapper around the native SHA512, implements BouncyCastle's IDigest interface in order
	/// to be compatible with BouncyCastle's HMac implementation.
	/// </summary>
	internal class ManagedSha512Digest : IDigest, IDisposable
	{
		private const int DigestLength = 64;
		SHA512Managed nativeSha512 = null;
		byte[] input;

		public ManagedSha512Digest()
		{
			Reset();
		}

		public string AlgorithmName => "SHA-512";

		public void BlockUpdate(byte[] input, int inOff, int length)
		{
			this.input = new byte[length];
			Array.Copy(input, inOff, this.input, 0, length);
		}

		public int DoFinal(byte[] output, int outOff)
		{
			var hash = nativeSha512.ComputeHash(input, 0, input.Length);
			Array.Copy(hash, 0, output, outOff, hash.Length);
			Reset();
			return DigestLength;
		}

		public int GetByteLength() => 64;

		public int GetDigestSize() => DigestLength;

		public void Reset()
		{
			nativeSha512?.Dispose();
			nativeSha512 = new SHA512Managed();
			input = null;
		}

		public void Update(byte input)
		{
			throw new NotImplementedException("Code monkey didn't expect you would need this.");
		}

		public void Dispose()
		{
			nativeSha512?.Dispose();
		}
	}
#endif
}
