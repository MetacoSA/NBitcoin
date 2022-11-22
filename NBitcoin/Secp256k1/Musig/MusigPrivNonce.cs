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
		/// <param name="sessionId">A unique session_id32. It is a "number used once". If null, it will be randomly generated.</param>
		/// <param name="signingKey">Provide the message to be signed to increase misuse-resistance. If you do provide a signingKey, sessionId32 can instead be a counter (that must never repeat!). However, it is recommended to always choose session_id32 uniformly at random. Can be null.</param>
		/// <param name="msg">Provide the message to be signed to increase misuse-resistance.</param>
		/// <param name="aggregatePubKey">Provide the message to be signed to increase misuse-resistance. Can be null.</param>
		/// <param name="extraInput">Provide the message to be signed to increase misuse-resistance. The extra_input32 argument can be used to provide additional data that does not repeat in normal scenarios, such as the current time.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public static MusigPrivNonce GenerateMusigNonce(
						   Context? context,
						   byte[]? sessionId,
						   ECPrivKey? signingKey,
						   byte[]? msg,
						   ECXOnlyPubKey? aggregatePubKey,
						   byte[]? extraInput)
		{

			byte[]? key32;
			if (signingKey is null)
			{
				key32 = null;
			}
			else
			{
				key32 = new byte[32];
				signingKey.WriteToSpan(key32.AsSpan());
			}

			byte[]? agg_pk = null;
			if (aggregatePubKey is not null)
			{
				agg_pk = new byte[32];
				aggregatePubKey.WriteToSpan(agg_pk);
			}

			var k = new Scalar[2];
			secp256k1_nonce_function_musig(sessionId, key32, agg_pk, msg, extraInput is null ? Array.Empty<byte>() : extraInput, k);
			return new MusigPrivNonce(new ECPrivKey(k[0], context, true), new ECPrivKey(k[1], context, true));
		}

		internal static void secp256k1_nonce_function_musig(byte[]? rand_, byte[]? key32, byte[]? agg_pk32, byte[]? msg, byte[]? extra_input, Span<Scalar> k)
		{
			if (rand_ is null)
			{
				rand_ = new byte[32];
				MusigPrivNonce.rand.GetBytes(rand_);
			}

			Span<byte> rand = stackalloc byte[32];
			if (key32 is not null)
			{
				using SHA256 sha = new SHA256();
				sha.InitializeTagged("MuSig/aux");
				sha.Write(rand_);
				sha.GetHash(rand);
				for (int ii = 0; ii < 32; ii++)
				{
					rand[ii] ^= key32[ii];
				}
			}
			else
			{
				rand_.CopyTo(rand);
			}

			byte[] msg_prefixed;
			if (msg is null)
				msg_prefixed = empty_msg_prefixed;
			else
			{
				msg_prefixed = new byte[9 + msg.Length];
				msg_prefixed[0] = 1;
				ToBE(msg_prefixed.AsSpan().Slice(1), msg.LongLength);
				msg.CopyTo(msg_prefixed, 9);
			}
			secp256k1_nonce_function_musig(k, rand, msg_prefixed, agg_pk32, extra_input, 0);
			secp256k1_nonce_function_musig(k, rand, msg_prefixed, agg_pk32, extra_input, 1);
		}
		internal static void ToBE(Span<byte> output, long v)
		{
			output[0] = (byte)(v >> 56);
			output[1] = (byte)(v >> 48);
			output[2] = (byte)(v >> 40);
			output[3] = (byte)(v >> 32);
			output[4] = (byte)(v >> 24);
			output[5] = (byte)(v >> 16);
			output[6] = (byte)(v >> 8);
			output[7] = (byte)(v >> 0);
		}
		static byte[] empty_msg_prefixed = new byte[1];
		internal static void secp256k1_nonce_function_musig(Span<Scalar> k, ReadOnlySpan<byte> rand, ReadOnlySpan<byte> msg_prefixed, byte[]? agg_pk, ReadOnlySpan<byte> extra_input, int i)
		{
			using SHA256 sha = new SHA256();
			

			sha.InitializeTagged("MuSig/nonce");
			sha.Write(rand);
			if (agg_pk is null)
			{
				sha.Write((byte)0);
			}
			else
			{
				sha.Write((byte)agg_pk.Length);
				sha.Write(agg_pk);
			}
			sha.Write(msg_prefixed);

			Span<byte> len = stackalloc byte[4];
			len[0] = (byte)(extra_input.Length >> 24);
			len[1] = (byte)(extra_input.Length >> 16);
			len[2] = (byte)(extra_input.Length >> 8);
			len[3] = (byte)(extra_input.Length >> 0);
			sha.Write(len);
			sha.Write(extra_input);
			sha.Write((byte)i);

			Span<byte> buf = stackalloc byte[32];
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
