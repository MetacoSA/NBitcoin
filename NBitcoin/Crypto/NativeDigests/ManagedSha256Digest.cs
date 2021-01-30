using NBitcoin.BouncyCastle.Crypto;
using System;

namespace NBitcoin.Crypto.NativeDigests
{
#if !NETSTANDARD1X && !NONATIVEHASH

	using System.Security.Cryptography;

	/// <summary>
	/// A wrapper around the native SHA256, implements BouncyCastle's IDigest interface in order
	/// to be compatible with BouncyCastle's HMac implementation.
	/// </summary>
	internal class ManagedSha256Digest : IDigest, IDisposable
	{
		private const int DigestLength = 32;
		SHA256Managed nativeSha256 = null;
		byte[] input;

		public ManagedSha256Digest()
		{
			Reset();
		}

		public string AlgorithmName => "SHA-256";

		public void BlockUpdate(byte[] input, int inOff, int length)
		{
			this.input = new byte[length];
			Array.Copy(input, inOff, this.input, 0, length);
		}

		public int DoFinal(byte[] output, int outOff)
		{
			var hash = nativeSha256.ComputeHash(input, 0, input.Length);
			Array.Copy(hash, 0, output, outOff, hash.Length);
			Reset();
			return DigestLength;
		}

		public int GetByteLength() => 64;

		public int GetDigestSize() => DigestLength;

		public void Reset()
		{
			nativeSha256?.Dispose();
			nativeSha256 = new SHA256Managed();
			input = null;
		}

		public void Update(byte input)
		{
			throw new NotImplementedException("Code monkey didn't expect you would need this.");
		}

		public void Dispose()
		{
			nativeSha256?.Dispose();
		}
	}
#endif
}
