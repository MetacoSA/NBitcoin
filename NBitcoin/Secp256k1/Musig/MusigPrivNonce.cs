#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1.Musig
{

#if SECP256K1_LIB
	public
#endif
	class MusigPrivNonce : IDisposable
	{
		readonly static RandomNumberGenerator rand = RandomNumberGenerator.Create();


		/// <summary>
		/// This function derives a secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <param name="context">The context</param>
		/// <param name="sessionId32">A unique session_id32. It is a "number used once". If empty, it will be randomly generated.</param>
		/// <param name="signingKey">Provide the message to be signed to increase misuse-resistance. If you do provide a signingKey, sessionId32 can instead be a counter (that must never repeat!). However, it is recommended to always choose session_id32 uniformly at random. Can be null.</param>
		/// <param name="msg32">Provide the message to be signed to increase misuse-resistance. Can be empty.</param>
		/// <param name="aggregatePubKey">Provide the message to be signed to increase misuse-resistance. Can be null.</param>
		/// <param name="extraInput32">Provide the message to be signed to increase misuse-resistance. The extra_input32 argument can be used to provide additional data that does not repeat in normal scenarios, such as the current time. Can be empty.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public static MusigPrivNonce GenerateMusigNonce(
						   Context? context,
						   ReadOnlySpan<byte> sessionId32,
						   ECPrivKey? signingKey,
						   ReadOnlySpan<byte> msg32,
						   ECXOnlyPubKey? aggregatePubKey,
						   ReadOnlySpan<byte> extraInput32)
		{
			if (!(sessionId32.Length is 32) && !(sessionId32.Length is 0))
				throw new ArgumentException(nameof(sessionId32), "sessionId32 must be 32 bytes or 0 bytes");
			if (sessionId32.Length is 0)
			{
				var bytes = new byte[32];
				sessionId32 = bytes.AsSpan();
				rand.GetBytes(bytes);
			}
			if (!(msg32.Length is 32) && !(msg32.Length is 0))
				throw new ArgumentException(nameof(msg32), "msg32 must be 32 bytes or 0 bytes");
			if (!(extraInput32.Length is 32) && !(extraInput32.Length is 0))
				throw new ArgumentException(nameof(extraInput32), "extraInput32 must be 32 bytes or 0 bytes");

			Span<byte> key32 = stackalloc byte[32];
			if (signingKey is null)
			{
				key32 = key32.Slice(0, 0);
			}
			else
			{
				signingKey.WriteToSpan(key32);
			}

			Span<byte> agg_pk = stackalloc byte[32];
			if (aggregatePubKey is null)
			{
				agg_pk = agg_pk.Slice(0, 0);
			}
			else
			{
				aggregatePubKey.WriteToSpan(agg_pk);
			}

			var k = new Scalar[2];
			secp256k1_nonce_function_musig(k, sessionId32, msg32, key32, agg_pk, extraInput32);
			return new MusigPrivNonce(new ECPrivKey(k[0], context, true), new ECPrivKey(k[1], context, true));
		}

		static void secp256k1_nonce_function_musig_helper(SHA256 sha, uint prefix_size, ReadOnlySpan<byte> data32)
		{
			/* The spec requires length prefix to be 4 bytes for `extra_in`, 1 byte
			 * otherwise */
			if (prefix_size == 4)
			{
				/* Four byte big-endian value, pad first three bytes with 0 */
				sha.Write(stackalloc byte[] { 0, 0, 0 });
			}
			if (data32.Length is 32)
			{
				sha.Write(32);
				sha.Write(data32);
			}
			else
			{
				sha.Write(0);
			}
		}

		internal static void secp256k1_nonce_function_musig(Span<Scalar> k, ReadOnlySpan<byte> session_id, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> agg_pk32, ReadOnlySpan<byte> extra_input32)
		{
			secp256k1_nonce_function_musig(k, session_id, msg32, key32, agg_pk32, extra_input32, 0);
			secp256k1_nonce_function_musig(k, session_id, msg32, key32, agg_pk32, extra_input32, 1);
		}

		internal static void secp256k1_nonce_function_musig(Span<Scalar> k, ReadOnlySpan<byte> session_id, ReadOnlySpan<byte> msg32, ReadOnlySpan<byte> key32, ReadOnlySpan<byte> agg_pk32, ReadOnlySpan<byte> extra_input32, int i)
		{
			using SHA256 sha = new SHA256();
			Span<byte> rand = stackalloc byte[32];

			if (key32.Length is 32)
			{
				sha.InitializeTagged("MuSig/aux");
				sha.Write(session_id);
				sha.GetHash(rand);
				for (int ii = 0; ii < 32; ii++)
				{
					rand[ii] ^= key32[ii];
				}
			}
			else
			{
				session_id.CopyTo(rand);
			}

			sha.InitializeTagged("MuSig/nonce");
			sha.Write(rand);
			secp256k1_nonce_function_musig_helper(sha, 1, agg_pk32);
			secp256k1_nonce_function_musig_helper(sha, 1, msg32);
			secp256k1_nonce_function_musig_helper(sha, 4, extra_input32);

			Span<byte> buf = stackalloc byte[32];
			sha.Write((byte)i);
			sha.GetHash(buf);
			k[i] = new Scalar(buf);
		}

		private readonly ECPrivKey k1;
		private readonly ECPrivKey k2;

		public ECPrivKey K1 => k1;
		public ECPrivKey K2 => k2;

		bool _IsUsed;
		public bool IsUsed
		{
			get
			{
				return _IsUsed;
			}
			internal set
			{
				_IsUsed = value;
			}
		}

		internal MusigPrivNonce(ECPrivKey k1, ECPrivKey k2)
		{
			this.k1 = k1;
			this.k2 = k2;
		}

		public void Dispose()
		{
			k1.Dispose();
			k2.Dispose();
		}

		public MusigPubNonce CreatePubNonce()
		{
			return new MusigPubNonce(k1.CreatePubKey(), k2.CreatePubKey());
		}
	}
}
#endif
