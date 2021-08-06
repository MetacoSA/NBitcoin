#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1.Musig
{
	class MusigSessionCache
	{
		internal bool CombinedNonceParity;
		internal Scalar B;
		internal Scalar E;
		internal MusigSessionCache Clone()
		{
			return new MusigSessionCache()
			{
				CombinedNonceParity = CombinedNonceParity,
				B = B,
				E = E
			};
		}
	}
#if SECP256K1_LIB
	public
#else
	internal
#endif
	class MusigContext
	{
		internal byte[] pk_hash = new byte[32];
		internal FE second_pk_x;
		internal bool pk_parity;
		internal bool is_tweaked;
		internal Scalar scalar_tweak;
		internal readonly byte[] msg32;
		internal bool internal_key_parity;
		internal bool processed_nonce;
		internal MusigSessionCache? SessionCache;
		internal SecpSchnorrSignature? Template;

		private ECXOnlyPubKey[] pubKeys;
		private MusigPubNonce? combinedNonce;
		public MusigPubNonce? CombinedNonce => combinedNonce;
		public ECXOnlyPubKey CombinedPubKey => combinedPubKey;
		ECPubKey? tweakedPubKey;
		ECXOnlyPubKey? xonlyTweakedPubKey;
		public ECPubKey? TweakedPubKey => tweakedPubKey;
		public ECXOnlyPubKey? XOnlyTweakedPubKey => xonlyTweakedPubKey ??= tweakedPubKey?.ToXOnlyPubKey();


		public ECXOnlyPubKey SigningPubKey => XOnlyTweakedPubKey ?? CombinedPubKey;
		private ECXOnlyPubKey combinedPubKey;
		private Context ctx;


		public MusigContext(MusigContext musigContext)
		{
			if (musigContext == null)
				throw new ArgumentNullException(nameof(musigContext));
			musigContext.pk_hash.CopyTo(pk_hash.AsSpan());
			second_pk_x = musigContext.second_pk_x;
			pk_parity = musigContext.pk_parity;
			is_tweaked = musigContext.is_tweaked;
			scalar_tweak = musigContext.scalar_tweak;
			internal_key_parity = musigContext.internal_key_parity;
			processed_nonce = musigContext.processed_nonce;
			SessionCache = musigContext.SessionCache?.Clone();
			Template = musigContext.Template;
			pubKeys = musigContext.pubKeys.ToArray();
			combinedNonce = musigContext.combinedNonce;
			combinedPubKey = musigContext.combinedPubKey;
			tweakedPubKey = musigContext.tweakedPubKey;
			ctx = musigContext.ctx;
			msg32 = musigContext.msg32;
		}

		public MusigContext Clone()
		{
			return new MusigContext(this);
		}

		internal MusigContext(ECXOnlyPubKey[] pubKeys, ReadOnlySpan<byte> msg32)
		{
			if (pubKeys == null)
				throw new ArgumentNullException(nameof(pubKeys));
			if (pubKeys.Length is 0)
				throw new ArgumentException(nameof(pubKeys), "There should be at least one pubkey in pubKeys");
			if (!(msg32.Length is 32))
				throw new ArgumentNullException(nameof(msg32), "msg32 should be 32 bytes.");
			this.pubKeys = pubKeys;
			this.combinedPubKey = ECXOnlyPubKey.MusigCombine(pubKeys, this);
			this.ctx = pubKeys[0].ctx;
			this.msg32 = msg32.ToArray();
		}

		public ECPubKey Tweak(ReadOnlySpan<byte> tweak32)
		{
			if (processed_nonce)
				throw new InvalidOperationException("This function can only be called before MusigContext.Process");
			if (is_tweaked)
				throw new InvalidOperationException("This function can only be called once");
			if (tweak32.Length != 32)
				throw new ArgumentException(nameof(tweak32), "The tweak should have a size of 32 bytes");
			scalar_tweak = new Scalar(tweak32, out int overflow);
			if (overflow == 1)
				throw new ArgumentException(nameof(tweak32), "The tweak is overflowing");
			var output = combinedPubKey.AddTweak(tweak32);
			internal_key_parity = pk_parity;
			pk_parity = output.Q.y.IsOdd;
			is_tweaked = true;
			tweakedPubKey = output;
			return output;
		}
		ECPubKey? adaptor;
		public void UseAdaptor(ECPubKey adaptor)
		{
			if (processed_nonce)
				throw new InvalidOperationException("This function can only be called before MusigContext.Process");
			this.adaptor = adaptor;
		}

		public void ProcessNonces(MusigPubNonce[] nonces)
		{
			Process(MusigPubNonce.Combine(nonces));
		}
		public void Process(MusigPubNonce combinedNonce)
		{
			if (processed_nonce)
				throw new InvalidOperationException($"Nonce already processed");
			var combined_pk = this.SigningPubKey;

			MusigSessionCache session_cache = new MusigSessionCache();
			Span<byte> noncehash = stackalloc byte[32];
			Span<byte> sig_template_data = stackalloc byte[32];

			Span<byte> combined_pk32 = stackalloc byte[32];
			Span<GEJ> summed_nonces = stackalloc GEJ[2];
			summed_nonces[0] = combinedNonce.K1.Q.ToGroupElementJacobian();
			summed_nonces[1] = combinedNonce.K2.Q.ToGroupElementJacobian();

			combined_pk.WriteToSpan(combined_pk32);
			/* Add public adaptor to nonce */
			if (adaptor != null)
			{
				summed_nonces[0] = summed_nonces[0].AddVariable(adaptor.Q);
			}
			var combined_nonce = secp256k1_musig_process_nonces_internal(
				this.ctx.EcMultContext,
				noncehash,
				summed_nonces,
				combined_pk32,
				msg32);
			session_cache.B = new Scalar(noncehash);

			ECXOnlyPubKey.secp256k1_xonly_ge_serialize(sig_template_data, ref combined_nonce);
			var rx = combined_nonce.x;
			/* Negate nonce if Y coordinate is not square */
			combined_nonce = combined_nonce.NormalizeYVariable();
			/* Store nonce parity in session cache */
			session_cache.CombinedNonceParity = combined_nonce.y.IsOdd;

			/* Compute messagehash and store in session cache */
			ECXOnlyPubKey.secp256k1_musig_compute_messagehash(noncehash, sig_template_data, combined_pk32, msg32);
			session_cache.E = new Scalar(noncehash);

			/* If there is a tweak then set `msghash` times `tweak` to the `s`-part of the sig template.*/
			Scalar s = Scalar.Zero;
			if (is_tweaked)
			{
				Scalar e = session_cache.E;
				if (!ECPrivKey.secp256k1_eckey_privkey_tweak_mul(ref e, scalar_tweak))
					throw new InvalidOperationException("Impossible to sign (secp256k1_eckey_privkey_tweak_mul is false)");
				if (pk_parity)
					e = e.Negate();
				s = s.Add(e);
			}
			SessionCache = session_cache;
			Template = new SecpSchnorrSignature(rx, s);
			processed_nonce = true;
			this.combinedNonce = combinedNonce;
		}

		internal static GE secp256k1_musig_process_nonces_internal(
			ECMultContext ecmult_ctx,
			Span<byte> noncehash,
			Span<GEJ> summed_noncesj,
			ReadOnlySpan<byte> combined_pk32,
			ReadOnlySpan<byte> msg)
		{

			Scalar b;
			GEJ combined_noncej;
			Span<GE> summed_nonces = stackalloc GE[2];
			summed_nonces[0] = summed_noncesj[0].ToGroupElement();
			summed_nonces[1] = summed_noncesj[1].ToGroupElement();
			secp256k1_musig_compute_noncehash(noncehash, summed_nonces, combined_pk32, msg);

			/* combined_nonce = summed_nonces[0] + b*summed_nonces[1] */
			b = new Scalar(noncehash);
			combined_noncej = ecmult_ctx.Mult(summed_noncesj[1], b, null);
			combined_noncej = combined_noncej.Add(summed_nonces[0]);
			return combined_noncej.ToGroupElement();
		}

		/* hash(summed_nonces[0], summed_nonces[1], combined_pk, msg) */
		internal static void secp256k1_musig_compute_noncehash(Span<byte> noncehash, Span<GE> summed_nonces, ReadOnlySpan<byte> combined_pk32, ReadOnlySpan<byte> msg)
		{
			Span<byte> buf = stackalloc byte[32];
			using SHA256 sha = new SHA256();
			sha.Initialize();
			int i;
			for (i = 0; i < 2; i++)
			{
				ECXOnlyPubKey.secp256k1_xonly_ge_serialize(buf, ref summed_nonces[i]);
				sha.Write(buf);
			}
			sha.Write(combined_pk32.Slice(0, 32));
			sha.Write(msg.Slice(0, 32));
			sha.GetHash(noncehash);
		}

		public bool Verify(ECXOnlyPubKey pubKey, MusigPubNonce pubNonce, MusigPartialSignature partialSignature)
		{
			if (partialSignature == null)
				throw new ArgumentNullException(nameof(partialSignature));
			if (pubNonce == null)
				throw new ArgumentNullException(nameof(pubNonce));
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			if (SessionCache is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");
			GEJ pkj;
			Span<GE> nonces = stackalloc GE[2];
			GEJ rj;
			GEJ tmp;
			GE pkp;
			var b = SessionCache.B;
			var pre_session = this;
			/* Compute "effective" nonce rj = nonces[0] + b*nonces[1] */
			/* TODO: use multiexp */

			nonces[0] = pubNonce.K1.Q;
			nonces[1] = pubNonce.K2.Q;

			rj = nonces[1].ToGroupElementJacobian();
			rj = this.ctx.EcMultContext.Mult(rj, b, null);
			rj = rj.AddVariable(nonces[0]);

			pkp = pubKey.Q;
			/* Multiplying the messagehash by the musig coefficient is equivalent
			 * to multiplying the signer's public key by the coefficient, except
			 * much easier to do. */
			var mu = ECXOnlyPubKey.secp256k1_musig_keyaggcoef(pre_session, pkp.x);
			var e = SessionCache.E * mu;
			/* If the MuSig-combined point has an odd Y coordinate, the signers will
     * sign for the negation of their individual xonly public key such that the
     * combined signature is valid for the MuSig aggregated xonly key. If the
     * MuSig-combined point was tweaked then `e` is negated if the combined key
     * has an odd Y coordinate XOR the internal key has an odd Y coordinate.*/
			if (pre_session.pk_parity
					!= (pre_session.is_tweaked
						&& pre_session.internal_key_parity))
			{
				e = e.Negate();
			}

			var s = partialSignature.E;
			/* Compute -s*G + e*pkj + rj */
			s = s.Negate();
			pkj = pkp.ToGroupElementJacobian();
			tmp = ctx.EcMultContext.Mult(pkj, e, s);
			var combined_nonce_parity = SessionCache.CombinedNonceParity;
			if (combined_nonce_parity)
			{
				rj = rj.Negate();
			}
			tmp = tmp.AddVariable(rj);
			return tmp.IsInfinity;
		}

		public SecpSchnorrSignature Combine(MusigPartialSignature[] partialSignatures)
		{
			if (partialSignatures == null)
				throw new ArgumentNullException(nameof(partialSignatures));
			if (Template is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");
			var s = this.Template.s;
			foreach (var sig in partialSignatures)
			{
				s = s + sig.E;
			}
			return new SecpSchnorrSignature(Template.rx, s);
		}

		public MusigPartialSignature Sign(ECPrivKey privKey, MusigPrivNonce privNonce)
		{
			if (privKey == null)
				throw new ArgumentNullException(nameof(privKey));
			if (privNonce == null)
				throw new ArgumentNullException(nameof(privNonce));
			if (privNonce.IsUsed)
				throw new ArgumentNullException(nameof(privNonce), "Nonce already used, a nonce should never be used to sign twice");
			if (SessionCache is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");

			//{
			//	/* Check in constant time if secnonce has been zeroed. */
			//	size_t i;
			//	unsigned char secnonce_acc = 0;
			//	for (i = 0; i < sizeof(*secnonce) ; i++) {
			//		secnonce_acc |= secnonce->data[i];
			//	}
			//	secp256k1_declassify(ctx, &secnonce_acc, sizeof(secnonce_acc));
			//	ARG_CHECK(secnonce_acc != 0);
			//}

			var pre_session = this;
			var session_cache = SessionCache;
			Span<Scalar> k = stackalloc Scalar[2];
			k[0] = privNonce.K1.sec;
			k[1] = privNonce.K2.sec;


			/* Obtain the signer's public key point and determine if the sk is
			 * negated before signing. That happens if if the signer's pubkey has an odd
			 * Y coordinate XOR the MuSig-combined pubkey has an odd Y coordinate XOR
			 * (if tweaked) the internal key has an odd Y coordinate.
			 *
			 * This can be seen by looking at the sk key belonging to `combined_pk`.
			 * Let's define
			 * P' := mu_0*|P_0| + ... + mu_n*|P_n| where P_i is the i-th public key
			 * point x_i*G, mu_i is the i-th musig coefficient and |.| is a function
			 * that normalizes a point to an even Y by negating if necessary similar to
			 * secp256k1_extrakeys_ge_even_y. Then we have
			 * P := |P'| + t*G where t is the tweak.
			 * And the combined xonly public key is
			 * |P| = x*G
			 *      where x = sum_i(b_i*mu_i*x_i) + b'*t
			 *            b' = -1 if P != |P|, 1 otherwise
			 *            b_i = -1 if (P_i != |P_i| XOR P' != |P'| XOR P != |P|) and 1
			 *                otherwise.
			 */

			var sk = privKey.sec;
			var pk = privKey.CreatePubKey().Q;

			pk = pk.NormalizeYVariable();

			int l = 0;
			if (pk.y.IsOdd)
				l++;
			if (pre_session.pk_parity)
				l++;
			if (pre_session.is_tweaked && pre_session.internal_key_parity)
				l++;
			if (l % 2 == 1)
				sk = sk.Negate();

			/* Multiply MuSig coefficient */
			pk = pk.NormalizeXVariable();
			var mu = ECXOnlyPubKey.secp256k1_musig_keyaggcoef(pre_session, pk.x);
			sk = sk * mu;
			if (session_cache.CombinedNonceParity)
			{
				k[0] = k[0].Negate();
				k[1] = k[1].Negate();
			}

			var e = session_cache.E * sk;
			k[1] = session_cache.B * k[1];
			k[0] = k[0] + k[1];
			e = e + k[0];
			Scalar.Clear(ref k[0]);
			Scalar.Clear(ref k[1]);
			privNonce.IsUsed = true;
			return new MusigPartialSignature(e);
		}

		public SecpSchnorrSignature Adapt(SecpSchnorrSignature signature, ECPrivKey adaptorSecret)
		{
			if (adaptorSecret == null)
				throw new ArgumentNullException(nameof(adaptorSecret));
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			if (!processed_nonce || SessionCache is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");
			var s = signature.s;
			var t = adaptorSecret.sec;
			if (SessionCache.CombinedNonceParity)
			{
				t = t.Negate();
			}
			s = s + t;
			return new SecpSchnorrSignature(signature.rx, s);
		}

		public ECPrivKey Extract(SecpSchnorrSignature signature, MusigPartialSignature[] partialSignatures)
		{
			if (partialSignatures == null)
				throw new ArgumentNullException(nameof(partialSignatures));
			if (SessionCache is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");
			var t = signature.s;
			t = t.Negate();
			foreach (var sig in partialSignatures)
			{
				t = t + sig.E;
			}
			if (!SessionCache.CombinedNonceParity)
			{
				t = t.Negate();
			}
			return new ECPrivKey(t, this.ctx, true);
		}

		/// <summary>
		/// This function derives a random secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce()
		{
			return GenerateNonce(Array.Empty<byte>());
		}
		/// <summary>
		/// This function derives a secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <param name="sessionId32">A unique session_id32. It is a "number used once". If empty, it will be randomly generated.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce(ReadOnlySpan<byte> sessionId32)
		{
			return GenerateNonce(sessionId32, null, Array.Empty<byte>());
		}

		/// <summary>
		/// This function derives a secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <param name="counter">A unique counter. Never reuse the same value twice for the same msg32/pubkeys.</param>
		/// <param name="signingKey">Provide the message to be signed to increase misuse-resistance. If you do provide a signingKey, sessionId32 can instead be a counter (that must never repeat!). However, it is recommended to always choose session_id32 uniformly at random. Can be null.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce(ulong counter, ECPrivKey signingKey)
		{
			if (signingKey == null)
				throw new ArgumentNullException(nameof(signingKey));

			Span<byte> sessionId = stackalloc byte[32];
			for (int i = 0; i < 8; i++)
				sessionId[i] = (byte)(counter >> (8*i));
			return GenerateNonce(sessionId, signingKey, Array.Empty<byte>());
		}

		/// <summary>
		/// This function derives a secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <param name="sessionId32">A unique session_id32. It is a "number used once". If empty, it will be randomly generated.</param>
		/// <param name="signingKey">Provide the message to be signed to increase misuse-resistance. If you do provide a signingKey, sessionId32 can instead be a counter (that must never repeat!). However, it is recommended to always choose session_id32 uniformly at random. Can be null.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce(ReadOnlySpan<byte> sessionId32, ECPrivKey? signingKey)
		{
			return GenerateNonce(sessionId32, signingKey, Array.Empty<byte>());
		}
		/// <summary>
		/// This function derives a secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <param name="sessionId32">A unique session_id32. It is a "number used once". If empty, it will be randomly generated.</param>
		/// <param name="signingKey">Provide the message to be signed to increase misuse-resistance. If you do provide a signingKey, sessionId32 can instead be a counter (that must never repeat!). However, it is recommended to always choose session_id32 uniformly at random. Can be null.</param>
		/// <param name="extraInput32">Provide the message to be signed to increase misuse-resistance. The extra_input32 argument can be used to provide additional data that does not repeat in normal scenarios, such as the current time. Can be empty.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce(ReadOnlySpan<byte> sessionId32, ECPrivKey? signingKey, ReadOnlySpan<byte> extraInput32)
		{
			return MusigPrivNonce.GenerateMusigNonce(ctx, sessionId32, signingKey, this.msg32, this.combinedPubKey, extraInput32);
		}
	}
}
#endif
