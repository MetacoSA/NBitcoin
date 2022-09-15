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
	class SessionValues
	{
		internal Scalar b;
		internal Scalar e;
		internal GE r;

		internal SessionValues Clone()
		{
			var c = new SessionValues()
			{
				b = b,
				e = e,
				r = r
			};
			return c;
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
		internal Scalar scalar_tweak;
		internal readonly byte[] msg32;
		internal Scalar gacc;
		internal bool processed_nonce;
		internal SessionValues? SessionCache;

		private MusigPubNonce? aggregateNonce;
		public MusigPubNonce? AggregateNonce => aggregateNonce;
		public ECPubKey AggregatePubKey => aggregatePubKey;

		private ECPubKey aggregatePubKey;
		private Context ctx;


		public MusigContext(MusigContext musigContext)
		{
			if (musigContext == null)
				throw new ArgumentNullException(nameof(musigContext));
			musigContext.pk_hash.CopyTo(pk_hash.AsSpan());
			second_pk_x = musigContext.second_pk_x;
			pk_parity = musigContext.pk_parity;
			scalar_tweak = musigContext.scalar_tweak;
			gacc = musigContext.gacc;
			tacc = musigContext.tacc;
			processed_nonce = musigContext.processed_nonce;
			SessionCache = musigContext.SessionCache?.Clone();
			aggregateNonce = musigContext.aggregateNonce;
			aggregatePubKey = musigContext.aggregatePubKey;
			ctx = musigContext.ctx;
			msg32 = musigContext.msg32;
		}

		public MusigContext Clone()
		{
			return new MusigContext(this);
		}

		internal MusigContext(ECPubKey[] pubKeys, ReadOnlySpan<byte> msg32)
		{
			if (pubKeys == null)
				throw new ArgumentNullException(nameof(pubKeys));
			if (pubKeys.Length is 0)
				throw new ArgumentException(nameof(pubKeys), "There should be at least one pubkey in pubKeys");
			this.aggregatePubKey = ECPubKey.MusigAggregate(pubKeys, this);
			this.ctx = pubKeys[0].ctx;
			this.msg32 = msg32.ToArray();
		}

		/// <summary>
		/// Add tweak to the xonly aggregated pubkey
		/// </summary>
		/// <param name="tweak32"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public ECPubKey Tweak(ReadOnlySpan<byte> tweak32)
		{
			return Tweak(tweak32, true);
		}

		/// <summary>
		/// Add tweak to the xonly aggregated pubkey or to the plain pubkey
		/// </summary>
		/// <param name="tweak32"></param>
		/// <param name="xOnly"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public ECPubKey Tweak(ReadOnlySpan<byte> tweak32, bool xOnly)
		{
			if (processed_nonce)
				throw new InvalidOperationException("This function can only be called before MusigContext.Process");
			if (tweak32.Length != 32)
				throw new ArgumentException(nameof(tweak32), "The tweak should have a size of 32 bytes");
			scalar_tweak = new Scalar(tweak32, out int overflow);
			if (overflow == 1)
				throw new ArgumentException(nameof(tweak32), "The tweak is overflowing");
			var t = scalar_tweak;
			var g = xOnly && pk_parity ? Scalar.MinusOne : Scalar.One;
			var Q_ = ctx.EcMultContext.Mult(aggregatePubKey.Q.ToGroupElementJacobian(), g, t);
			if (Q_.IsInfinity)
				throw new InvalidOperationException("The result of tweaking cannot be infinity");
			gacc = g * gacc;
			tacc = (t + g * tacc);
			aggregatePubKey = new ECPubKey(Q_.ToGroupElement(), ctx);
			pk_parity = aggregatePubKey.Q.y.IsOdd;
			return aggregatePubKey;
		}

		ECPubKey? adaptor;
		private Scalar tacc = Scalar.Zero;

		public void UseAdaptor(ECPubKey adaptor)
		{
			if (processed_nonce)
				throw new InvalidOperationException("This function can only be called before MusigContext.Process");
			this.adaptor = adaptor;
		}

		public void ProcessNonces(MusigPubNonce[] nonces)
		{
			Process(MusigPubNonce.Aggregate(nonces));
		}
		public void Process(MusigPubNonce aggregatedNonce)
		{
			if (processed_nonce)
				throw new InvalidOperationException($"Nonce already processed");
			var q = this.AggregatePubKey;

			SessionValues session_cache = new SessionValues();
			Span<byte> qbytes = stackalloc byte[32];
			Span<GEJ> aggnonce_ptj = stackalloc GEJ[2];
			aggnonce_ptj[0] = aggregatedNonce.K1.ToGroupElementJacobian();
			aggnonce_ptj[1] = aggregatedNonce.K2.ToGroupElementJacobian();

			q.Q.x.WriteToSpan(qbytes);
			/* Add public adaptor to nonce */
			if (adaptor != null)
			{
				aggnonce_ptj[0] = aggnonce_ptj[0].AddVariable(adaptor.Q);
			}

			secp256k1_musig_nonce_process_internal(this.ctx.EcMultContext, out var r, out session_cache.b, aggnonce_ptj, qbytes, msg32);
			Span<byte> rbytes = stackalloc byte[32];
			ECPubKey.secp256k1_xonly_ge_serialize(rbytes, ref r);
			/* Compute messagehash and store in session cache */
			Span<byte> buff = stackalloc byte[32];
			using SHA256 sha = new SHA256();
			sha.InitializeTagged(ECXOnlyPubKey.TAG_BIP0340Challenge);
			sha.Write(rbytes);
			sha.Write(qbytes);
			sha.Write(msg32);
			sha.GetHash(buff);
			session_cache.e = new Scalar(buff);
			session_cache.r = r;

			SessionCache = session_cache;
			processed_nonce = true;
			this.aggregateNonce = aggregatedNonce;
		}

		internal static void secp256k1_musig_nonce_process_internal(
			ECMultContext ecmult_ctx,
			out GE r,
			out Scalar b,
			Span<GEJ> aggnoncej,
			ReadOnlySpan<byte> qbytes,
			ReadOnlySpan<byte> msg)
		{
			Span<byte> noncehash = stackalloc byte[32];
			Span<GE> aggnonce = stackalloc GE[2];
			aggnonce[0] = aggnoncej[0].ToGroupElement();
			aggnonce[1] = aggnoncej[1].ToGroupElement();
			secp256k1_musig_compute_noncehash(noncehash, aggnonce, qbytes, msg);

			/* aggnonce = aggnonces[0] + b*aggnonces[1] */
			b = new Scalar(noncehash);
			var fin_nonce_ptj = ecmult_ctx.Mult(aggnoncej[1], b, null);
			fin_nonce_ptj = fin_nonce_ptj.AddVariable(aggnonce[0]);
			r = fin_nonce_ptj.IsInfinity ? EC.G : fin_nonce_ptj.ToGroupElement();
			r = r.NormalizeYVariable();
		}

		/* hash(summed_nonces[0], summed_nonces[1], agg_pk32, msg) */
		internal static void secp256k1_musig_compute_noncehash(Span<byte> noncehash, Span<GE> aggnonce, ReadOnlySpan<byte> agg_pk32, ReadOnlySpan<byte> msg)
		{
			Span<byte> buf = stackalloc byte[33];
			using SHA256 sha = new SHA256();
			sha.InitializeTagged("MuSig/noncecoef");
			int i;
			for (i = 0; i < 2; i++)
			{
				if (aggnonce[i].IsInfinity)
					buf.Fill(0);
				else
					ECPubKey.secp256k1_eckey_pubkey_serialize(buf, ref aggnonce[i], out _, true);
				sha.Write(buf);
			}
			sha.Write(agg_pk32);
			sha.Write(msg);
			sha.GetHash(noncehash);
		}

		public bool Verify(ECPubKey pubKey, MusigPubNonce pubNonce, MusigPartialSignature partialSignature)
		{
			if (partialSignature == null)
				throw new ArgumentNullException(nameof(partialSignature));
			if (pubNonce == null)
				throw new ArgumentNullException(nameof(pubNonce));
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			if (SessionCache is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");

			var s = partialSignature.E;
			var R_s1 = pubNonce.K1;
			var R_s2 = pubNonce.K2;
			var Re_s_ = ctx.EcMultContext.Mult(R_s2.ToGroupElementJacobian(), SessionCache.b, null).AddVariable(R_s1).ToGroupElement().NormalizeVariable();
			var Re_s = SessionCache.r.y.IsOdd ? Re_s_.Negate() : Re_s_;
			var P = pubKey.Q;
			var a = ECPubKey.secp256k1_musig_keyaggcoef(this, pubKey);
			var g = aggregatePubKey.Q.y.IsOdd ? Scalar.MinusOne : Scalar.One;
			var g_ = g * gacc;

			/* Compute -s*G + e*pkj + rj */
			var res = ctx.EcMultContext.Mult(P.ToGroupElementJacobian(), SessionCache.e * a * g_, s.Negate()).AddVariable(Re_s);
			return res.IsInfinity;
		}

		public SecpSchnorrSignature AggregateSignatures(MusigPartialSignature[] partialSignatures)
		{
			if (partialSignatures == null)
				throw new ArgumentNullException(nameof(partialSignatures));
			if (this.SessionCache is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");
			var s = Scalar.Zero;
			foreach (var sig in partialSignatures)
			{
				s = s + sig.E;
			}
			var g = pk_parity ? Scalar.MinusOne : Scalar.One;
			s = s + g * SessionCache.e * tacc;
			return new SecpSchnorrSignature(this.SessionCache.r.x, s);
		}

		/// <summary>
		/// <inheritdoc cref="DeterministicSign(ECPrivKey, byte[])"/>
		/// </summary>
		/// <param name="privKey"><inheritdoc cref="DeterministicSign(ECPrivKey, byte[])" path="/param/[@name='privKey']"></inheritdoc>/></param>
		/// <returns></returns>
		public (MusigPartialSignature Signature, MusigPubNonce PubNonce) DeterministicSign(ECPrivKey privKey)
		{
			return DeterministicSign(privKey, null);
		}

		/// <summary>
		/// To deterministically sign, you need to call <see cref="Process(MusigPubNonce)"/> or <see cref="ProcessNonces(MusigPubNonce[])"/> with the nonces of all other participants.
		/// See the BIP for more information about deterministic signer.
		/// </summary>
		/// <param name="privKey">The private key of the stateless signer</param>
		/// <param name="rand">An optional random data</param>
		/// <returns>The partial signature with the derived public nonce of this signer</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public (MusigPartialSignature Signature, MusigPubNonce PubNonce) DeterministicSign(ECPrivKey privKey, byte[]? rand)
		{
			if (!processed_nonce)
				throw new InvalidOperationException("You need to call Process or ProcessNonces with the nonces of all other participants");
			if (privKey is null)
				throw new ArgumentNullException(nameof(privKey));
			if (SessionCache is null || aggregateNonce is null)
				throw new InvalidOperationException("You need to run MusigContext.Process first");

			var sk_ = rand is null ? privKey.sec.ToBytes() : xor32(privKey.sec.ToBytes(), tagged_hash("MuSig/aux", rand));
			var aggothernonce = aggregateNonce;
			var k_1 = det_nonce_hash(sk_, aggothernonce, aggregatePubKey, msg32, 0);
			var k_2 = det_nonce_hash(sk_, aggothernonce, aggregatePubKey, msg32, 1);
			Array.Clear(sk_, 0, sk_.Length);

			if (k_1 == Scalar.Zero || k_2 == Scalar.Zero)
				throw new InvalidOperationException("This should never happen (k_1 == Scalar.Zero || k_2 == Scalar.Zero)");

			var secnonce = new MusigPrivNonce(new ECPrivKey(k_1, ctx, false), new ECPrivKey(k_2, ctx, false));
			var pubnonce = secnonce.CreatePubNonce();
			processed_nonce = false;
			ProcessNonces(new[] { pubnonce, aggregateNonce });
			return (Sign(privKey, secnonce), pubnonce);
		}

		private Scalar det_nonce_hash(byte[] sk_, MusigPubNonce aggothernonce, ECPubKey aggregatePubKey, ReadOnlySpan<byte> msg, int i)
		{
			Span<byte> buff = stackalloc byte[66];
			using var sha = new SHA256();
			sha.InitializeTagged("MuSig/deterministic/nonce");
			sha.Write(sk_);
			aggothernonce.WriteToSpan(buff);
			sha.Write(buff.Slice(0, 66));
			aggregatePubKey.Q.x.WriteToSpan(buff);
			sha.Write(buff.Slice(0, 32));
			MusigPrivNonce.ToBE(buff, msg.Length);
			sha.Write(buff.Slice(0, 8));
			sha.Write(msg);
			sha.Write((byte)i);
			sha.GetHash(buff);
			return new Scalar(buff.Slice(0, 32));
		}

		private byte[] xor32(byte[] a, byte[] b)
		{
			var r = new byte[32];
			for (int i = 0; i < 32; i++)
			{
				r[i] = (byte)(a[i] ^ b[i]);
			}
			return r;
		}

		private byte[] tagged_hash(string tag, byte[] b)
		{
			using var sha = new SHA256();
			sha.InitializeTagged(tag);
			sha.Write(b);
			return sha.GetHash();
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
			if (SessionCache.r.y.IsOdd)
			{
				k[0] = k[0].Negate();
				k[1] = k[1].Negate();
			}

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

			var d_ = privKey.sec;
			var ecpk = privKey.CreatePubKey();
			var P = ecpk.Q;

			P = P.NormalizeYVariable();

			var gacc_ = gacc;
			if (aggregatePubKey.Q.y.IsOdd)
				gacc_ = gacc.Negate();
			/* Multiply MuSig coefficient */
			P = P.NormalizeXVariable();
			var a = ECPubKey.secp256k1_musig_keyaggcoef(pre_session, ecpk);
			var g = aggregatePubKey.Q.y.IsOdd ? Scalar.MinusOne : Scalar.One;
			var d = g * gacc * d_;
			var s = (k[0] + SessionCache.b * k[1] + SessionCache.e * a * d);
			Scalar.Clear(ref k[0]);
			Scalar.Clear(ref k[1]);
			privNonce.IsUsed = true;
			return new MusigPartialSignature(s);
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
			if (SessionCache.r.y.IsOdd)
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
			if (!SessionCache.r.y.IsOdd)
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
		/// <param name="sessionId">A unique session_id. It is a "number used once". If null, it will be randomly generated.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce(byte[]? sessionId)
		{
			return GenerateNonce(sessionId, null, null);
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

			byte[] sessionId = new byte[32];
			for (int i = 0; i < 8; i++)
				sessionId[i] = (byte)(counter >> (8*i));
			return GenerateNonce(sessionId, signingKey, Array.Empty<byte>());
		}

		/// <summary>
		/// This function derives a secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <param name="sessionId">A unique session_id32. It is a "number used once". If null, it will be randomly generated.</param>
		/// <param name="signingKey">Provide the message to be signed to increase misuse-resistance. If you do provide a signingKey, sessionId32 can instead be a counter (that must never repeat!). However, it is recommended to always choose session_id32 uniformly at random. Can be null.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce(byte[]? sessionId, ECPrivKey? signingKey)
		{
			return GenerateNonce(sessionId, signingKey, Array.Empty<byte>());
		}
		/// <summary>
		/// This function derives a secret nonce that will be required for signing and
		/// creates a private nonce whose public part intended to be sent to other signers.
		/// </summary>
		/// <param name="sessionId">A unique session_id. It is a "number used once". If null, it will be randomly generated.</param>
		/// <param name="signingKey">Provide the message to be signed to increase misuse-resistance. If you do provide a signingKey, sessionId32 can instead be a counter (that must never repeat!). However, it is recommended to always choose session_id32 uniformly at random. Can be null.</param>
		/// <param name="extraInput">Provide the message to be signed to increase misuse-resistance. The extra_input32 argument can be used to provide additional data that does not repeat in normal scenarios, such as the current time. Can be null.</param>
		/// <returns>A private nonce whose public part intended to be sent to other signers</returns>
		public MusigPrivNonce GenerateNonce(byte[]? sessionId, ECPrivKey? signingKey, byte[]? extraInput)
		{
			return MusigPrivNonce.GenerateMusigNonce(ctx, sessionId, signingKey, this.msg32, this.aggregatePubKey.ToXOnlyPubKey(), extraInput);
		}
	}
}
#endif
