#pragma warning disable CS0618 // Type or member is obsolete
#region License
/*
CryptSharp
Copyright (c) 2011, 2013 James F. Bellinger <http://www.zer7.com/software/cryptsharp>

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/
#endregion
#if !NO_BC
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Security;
#endif
using NBitcoin.Crypto.Internal;
using System;
#if !USEBC
using System.Security.Cryptography;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Crypto
{
	// See http://www.tarsnap.com/scrypt/scrypt.pdf for algorithm details.
	// TODO: Test on a big-endian machine and make sure it works.
	// TODO: Feel hatred for whatever genius decided C# wouldn't have 'safe'
	//       stack-allocated arrays. He has stricken ugliness upon a thousand codes.
	/// <summary>
	/// Implements the SCrypt key derivation function.
	/// </summary>
	public static class SCrypt
	{
		const int hLen = 32;

		/// <summary>
		/// Computes a derived key.
		/// </summary>
		/// <param name="key">The key to derive from.</param>
		/// <param name="salt">
		///     The salt.
		///     A unique salt means a unique SCrypt stream, even if the original key is identical.
		/// </param>
		/// <param name="cost">
		///     The cost parameter, typically a fairly large number such as 262144.
		///     Memory usage and CPU time scale approximately linearly with this parameter.
		/// </param>
		/// <param name="blockSize">
		///     The mixing block size, typically 8.
		///     Memory usage and CPU time scale approximately linearly with this parameter.
		/// </param>
		/// <param name="parallel">
		///     The level of parallelism, typically 1.
		///     CPU time scales approximately linearly with this parameter.
		/// </param>
		/// <param name="maxThreads">
		///     The maximum number of threads to spawn to derive the key.
		///     This is limited by the <paramref name="parallel"/> value.
		///     <c>null</c> will use as many threads as possible.
		/// </param>
		/// <param name="derivedKeyLength">The desired length of the derived key.</param>
		/// <returns>The derived key.</returns>
		public static byte[] ComputeDerivedKey(byte[] key, byte[] salt,
											   int cost, int blockSize, int parallel, int? maxThreads,
											   int derivedKeyLength)
		{
			Check.Range("derivedKeyLength", derivedKeyLength, 0, int.MaxValue);
#if NO_NATIVE_RFC2898_HMACSHA512 || NO_NATIVE_HMACSHA512
			using (Pbkdf2 kdf = GetStream(key, salt, cost, blockSize, parallel, maxThreads))
			{
				return kdf.Read(derivedKeyLength);
			}
#else
			byte[] B = GetEffectivePbkdf2Salt(key, salt, cost, blockSize, parallel, maxThreads);
			using System.Security.Cryptography.Rfc2898DeriveBytes derive = new System.Security.Cryptography.Rfc2898DeriveBytes(key, B, 1, System.Security.Cryptography.HashAlgorithmName.SHA256);
			Security.Clear(B);
			return derive.GetBytes(derivedKeyLength);
#endif
		}

		/// <summary>
		/// The SCrypt algorithm creates a salt which it then uses as a one-iteration
		/// PBKDF2 key stream with SHA256 HMAC. This method lets you retrieve this intermediate salt.
		/// </summary>
		/// <param name="key">The key to derive from.</param>
		/// <param name="salt">
		///     The salt.
		///     A unique salt means a unique SCrypt stream, even if the original key is identical.
		/// </param>
		/// <param name="cost">
		///     The cost parameter, typically a fairly large number such as 262144.
		///     Memory usage and CPU time scale approximately linearly with this parameter.
		/// </param>
		/// <param name="blockSize">
		///     The mixing block size, typically 8.
		///     Memory usage and CPU time scale approximately linearly with this parameter.
		/// </param>
		/// <param name="parallel">
		///     The level of parallelism, typically 1.
		///     CPU time scales approximately linearly with this parameter.
		/// </param>
		/// <param name="maxThreads">
		///     The maximum number of threads to spawn to derive the key.
		///     This is limited by the <paramref name="parallel"/> value.
		///     <c>null</c> will use as many threads as possible.
		/// </param>
		/// <returns>The effective salt.</returns>
		public static byte[] GetEffectivePbkdf2Salt(byte[] key, byte[] salt,
													int cost, int blockSize, int parallel, int? maxThreads)
		{
			Check.Null("key", key);
			Check.Null("salt", salt);
			return MFcrypt(key, salt, cost, blockSize, parallel, maxThreads);
		}


		/// <summary>
		/// Creates a derived key stream from which a derived key can be read.
		/// </summary>
		/// <param name="key">The key to derive from.</param>
		/// <param name="salt">
		///     The salt.
		///     A unique salt means a unique scrypt stream, even if the original key is identical.
		/// </param>
		/// <param name="cost">
		///     The cost parameter, typically a fairly large number such as 262144.
		///     Memory usage and CPU time scale approximately linearly with this parameter.
		/// </param>
		/// <param name="blockSize">
		///     The mixing block size, typically 8.
		///     Memory usage and CPU time scale approximately linearly with this parameter.
		/// </param>
		/// <param name="parallel">
		///     The level of parallelism, typically 1.
		///     CPU time scales approximately linearly with this parameter.
		/// </param>
		/// <param name="maxThreads">
		///     The maximum number of threads to spawn to derive the key.
		///     This is limited by the <paramref name="parallel"/> value.
		///     <c>null</c> will use as many threads as possible.
		/// </param>
		/// <returns>The derived key stream.</returns>
#if NO_NATIVE_HMACSHA512
		internal static Pbkdf2 GetStream(byte[] key, byte[] salt,
									   int cost, int blockSize, int parallel, int? maxThreads)
		{
			byte[] B = GetEffectivePbkdf2Salt(key, salt, cost, blockSize, parallel, maxThreads);
			var mac = new NBitcoin.BouncyCastle.Crypto.Macs.HMac(new NBitcoin.BouncyCastle.Crypto.Digests.Sha256Digest());
			mac.Init(new KeyParameter(key));
			Pbkdf2 kdf = new Pbkdf2(mac, B, 1);
			Security.Clear(B);
			return kdf;
		}
#elif NO_NATIVE_RFC2898_HMACSHA512
		internal static Pbkdf2 GetStream(byte[] key, byte[] salt, int cost, int blockSize, int parallel, int? maxThreads)
		{
			byte[] B = GetEffectivePbkdf2Salt(key, salt, cost, blockSize, parallel, maxThreads);
			Pbkdf2 kdf = new Pbkdf2(new HMACSHA256(key), B, 1);
			Security.Clear(B);
			return kdf;
		}
#endif

		static byte[] MFcrypt(byte[] P, byte[] S,
							  int cost, int blockSize, int parallel, int? maxThreads)
		{
			int MFLen = blockSize * 128;
			if (maxThreads == null)
			{
				maxThreads = int.MaxValue;
			}

			if (!BitMath.IsPositivePowerOf2(cost))
			{
				throw Exceptions.ArgumentOutOfRange("cost", "Cost must be a positive power of 2.");
			}
			Check.Range("blockSize", blockSize, 1, int.MaxValue / 128);
			Check.Range("parallel", parallel, 1, int.MaxValue / MFLen);
			Check.Range("maxThreads", (int)maxThreads, 1, int.MaxValue);

#if NO_NATIVE_HMACSHA512
			var mac = new NBitcoin.BouncyCastle.Crypto.Macs.HMac(new NBitcoin.BouncyCastle.Crypto.Digests.Sha256Digest());
			mac.Init(new KeyParameter(P));
			byte[] B = Pbkdf2.ComputeDerivedKey(mac, S, 1, parallel * MFLen);
#elif NO_NATIVE_RFC2898_HMACSHA512
			byte[] B = Pbkdf2.ComputeDerivedKey(new HMACSHA256(P), S, 1, parallel * MFLen);
#else
			byte[] B = null;
			if (S.Length >= 8)
			{
				// While we should be able to use Rfc2898DeriveBytes if salt is less than 8 bytes, it sadly does not accept salt less than 8 bytes needed for BIP38
				using System.Security.Cryptography.Rfc2898DeriveBytes derive = new System.Security.Cryptography.Rfc2898DeriveBytes(P, S, 1, System.Security.Cryptography.HashAlgorithmName.SHA256);
				B = derive.GetBytes(parallel * MFLen);
			}
			else
			{
				B = Pbkdf2.ComputeDerivedKey(new HMACSHA256(P), S, 1, parallel * MFLen);
			}
#endif
			uint[] B0 = new uint[B.Length / 4];
			for (int i = 0; i < B0.Length; i++)
			{
				B0[i] = BitPacking.UInt32FromLEBytes(B, i * 4);
			} // code is easier with uint[]
			ThreadSMixCalls(B0, MFLen, cost, blockSize, parallel, (int)maxThreads);
			for (int i = 0; i < B0.Length; i++)
			{
				BitPacking.LEBytesFromUInt32(B0[i], B, i * 4);
			}
			Security.Clear(B0);

			return B;
		}
#if NO_THREAD || NETSTANDARD13
		static void ThreadSMixCalls(uint[] B0, int MFLen,
									int cost, int blockSize, int parallel, int maxThreads)
		{
			int current = 0;
			Action workerThread = delegate ()
			{
				while (true)
				{
					int j = Interlocked.Increment(ref current) - 1;
					if (j >= parallel)
					{
						break;
					}

					SMix(B0, j * MFLen / 4, B0, j * MFLen / 4, (uint)cost, blockSize);
				}
			};

			int threadCount = Math.Max(1, Math.Min(Environment.ProcessorCount, Math.Min(maxThreads, parallel)));
			Task[] threads = new Task[threadCount - 1];
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = Task.Run(workerThread);
			}
			workerThread();
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Wait();
			}
		}
#else
		static void ThreadSMixCalls(uint[] B0, int MFLen,
									int cost, int blockSize, int parallel, int maxThreads)
		{
			int current = 0;
			ThreadStart workerThread = delegate ()
			{
				while (true)
				{
					int j = Interlocked.Increment(ref current) - 1;
					if (j >= parallel)
					{
						break;
					}

					SMix(B0, j * MFLen / 4, B0, j * MFLen / 4, (uint)cost, blockSize);
				}
			};
			int threadCount = Math.Max(1, Math.Min(Environment.ProcessorCount, Math.Min(maxThreads, parallel)));
			Thread[] threads = new Thread[threadCount - 1];
			for (int i = 0; i < threads.Length; i++)
			{
				(threads[i] = new Thread(workerThread, 8192 * 16)).Start();
			}
			workerThread();
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Join();
			}
		}
		// WARNING: Stack size issues on some platforms
		//static void ThreadSMixCalls(uint[] B0, int MFLen,
		//							int cost, int blockSize, int parallel, int maxThreads)
		//{
		//	int current = 0;
		//	ThreadStart workerThread = delegate ()
		//	{
		//		while (true)
		//		{
		//			int j = Interlocked.Increment(ref current) - 1;
		//			if (j >= parallel)
		//			{
		//				break;
		//			}

		//			SMix(B0, j * MFLen / 4, B0, j * MFLen / 4, (uint)cost, blockSize);
		//		}
		//	};
		//	int threadCount = Math.Max(1, Math.Min(Environment.ProcessorCount, Math.Min(maxThreads, parallel)));
		//	Thread[] threads = new Thread[threadCount - 1];
		//	Parallel.For(0, threadCount - 1, (i) =>
		//	{
		//		workerThread();
		//	});
		//	workerThread();
		//}
#endif
		static void SMix(uint[] B, int Boffset, uint[] Bp, int Bpoffset, uint N, int r)
		{
			uint Nmask = N - 1;
			int Bs = 16 * 2 * r;
			uint[] scratch1 = new uint[16];
			uint[] scratchX = new uint[16], scratchY = new uint[Bs];
			uint[] scratchZ = new uint[Bs];

			uint[] x = new uint[Bs];
			uint[][] v = new uint[N][];
			for (int i = 0; i < v.Length; i++)
			{
				v[i] = new uint[Bs];
			}

			Array.Copy(B, Boffset, x, 0, Bs);
			for (uint i = 0; i < N; i++)
			{
				Array.Copy(x, v[i], Bs);
				BlockMix(x, 0, x, 0, scratchX, scratchY, scratch1, r);
			}
			for (uint i = 0; i < N; i++)
			{
				uint j = x[Bs - 16] & Nmask;
				uint[] vj = v[j];
				for (int k = 0; k < scratchZ.Length; k++)
				{
					scratchZ[k] = x[k] ^ vj[k];
				}
				BlockMix(scratchZ, 0, x, 0, scratchX, scratchY, scratch1, r);
			}
			Array.Copy(x, 0, Bp, Bpoffset, Bs);

			for (int i = 0; i < v.Length; i++)
			{
				Security.Clear(v[i]);
			}
			Security.Clear(v);
			Security.Clear(x);
			Security.Clear(scratchX);
			Security.Clear(scratchY);
			Security.Clear(scratchZ);
			Security.Clear(scratch1);
		}

		static void BlockMix
			(uint[] B,        // 16*2*r
			 int Boffset,
			 uint[] Bp,       // 16*2*r
			 int Bpoffset,
			 uint[] x,        // 16
			 uint[] y,        // 16*2*r -- unnecessary but it allows us to alias B and Bp
			 uint[] scratch,  // 16
			 int r)
		{
			int k = Boffset, m = 0, n = 16 * r;
			Array.Copy(B, (2 * r - 1) * 16, x, 0, 16);

			for (int i = 0; i < r; i++)
			{
				for (int j = 0; j < scratch.Length; j++)
				{
					scratch[j] = x[j] ^ B[j + k];
				}
				Salsa20Core.Compute(8, scratch, 0, x, 0);
				Array.Copy(x, 0, y, m, 16);
				k += 16;

				for (int j = 0; j < scratch.Length; j++)
				{
					scratch[j] = x[j] ^ B[j + k];
				}
				Salsa20Core.Compute(8, scratch, 0, x, 0);
				Array.Copy(x, 0, y, m + n, 16);
				k += 16;

				m += 16;
			}

			Array.Copy(y, 0, Bp, Bpoffset, y.Length);
		}

		//REF BIP38
		//The preliminary values of 16384, 8, and 8 are hoped to offer the following properties: 
		//•Encryption/decryption in javascript requiring several seconds per operation
		//•Use of the parallelization parameter provides a modest opportunity for speedups in environments where concurrent threading is available - such environments would be selected for processes that must handle bulk quantities of encryption/decryption operations. Estimated time for an operation is in the tens or hundreds of milliseconds.
		public static byte[] BitcoinComputeDerivedKey(byte[] password, byte[] salt, int outputCount = 64)
		{
			return NBitcoin.Crypto.SCrypt.ComputeDerivedKey(password, salt, 16384, 8, 8, 8, outputCount);
		}

		public static byte[] BitcoinComputeDerivedKey2(byte[] password, byte[] salt, int outputCount = 64)
		{
			return NBitcoin.Crypto.SCrypt.ComputeDerivedKey(password, salt, 1024, 1, 1, 1, outputCount);
		}

		public static byte[] BitcoinComputeDerivedKey(string password, byte[] salt)
		{
			return BitcoinComputeDerivedKey(Encoding.UTF8.GetBytes(password), salt);
		}
	}
}
#pragma warning restore CS0618 // Type or member is obsolete
