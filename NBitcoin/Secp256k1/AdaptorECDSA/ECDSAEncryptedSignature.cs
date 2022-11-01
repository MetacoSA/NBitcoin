#if HAS_SPAN
#nullable enable
using NBitcoin.Secp256k1;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class ECDSAEncryptedSignature
	{
		public readonly GE r, rp;
		public readonly Scalar sp;
		public DLEQProof Proof { get; }

		internal ECDSAEncryptedSignature(in GE r, in GE rp, in Scalar sp, DLEQProof proof)
		{
			this.sp = sp;
			this.r = r;
			this.rp = rp;
			this.Proof = proof;
		}

		public Scalar GetRAsScalar()
		{
			Span<byte> buf = stackalloc byte[33];
			DLEQProof.secp256k1_dleq_serialize_point(buf, r);
			return new Scalar(buf.Slice(1), out _);
		}

		public static bool TryCreate(ReadOnlySpan<byte> input162, [MaybeNullWhen(false)] out ECDSAEncryptedSignature sig)
		{
			sig = null;
			if (input162.Length != 162)
				return false;
			if (!DLEQProof.secp256k1_dleq_deserialize_point(input162, out var r))
			{
				sig = default;
				return false;
			}
			if (!DLEQProof.secp256k1_dleq_deserialize_point(input162.Slice(33), out var rp))
			{
				sig = default;
				return false;
			}
			var sp = new Scalar(input162.Slice(33 + 33), out var overflow);
			if (overflow != 0 || sp.IsZero)
			{
				sig = default;
				return false;
			}
			if (!DLEQProof.TryCreate(input162.Slice(33 + 33 + 32), out var proof))
			{
				sig = default;
				return false;
			}
			sig = new ECDSAEncryptedSignature(r, rp, sp, proof);
			return true;
		}

		public SecpECDSASignature DecryptECDSASignature(ECPrivKey encryptionKey)
		{
			if (encryptionKey == null)
				throw new ArgumentNullException(nameof(encryptionKey));
			return encryptionKey.DecryptECDSASignature(this);
		}
		internal static bool TryGetScalar(in FE fe, out Scalar scalar)
		{
			Span<byte> s = stackalloc byte[32];
			fe.Normalize().WriteToSpan(s);
			var sc = new Scalar(s, out int overflow);
			if (overflow != 0)
			{
				scalar = default;
				return false;
			}
			scalar = sc;
			return true;
		}

		public void WriteToSpan(Span<byte> out162)
		{
			DLEQProof.secp256k1_dleq_serialize_point(out162, r);
			DLEQProof.secp256k1_dleq_serialize_point(out162.Slice(33), rp);
			sp.WriteToSpan(out162.Slice(33 + 33));
			Proof.WriteToSpan(out162.Slice(33 + 33 + 32));
		}

		public byte[] ToBytes()
		{
			var output = new byte[162];
			WriteToSpan(output);
			return output;
		}

		public bool TryRecoverDecryptionKey(SecpECDSASignature sig, ECPubKey encryptionKey, [MaybeNullWhen(false)] out ECPrivKey? decryptionKey)
		{
			if (sig is null)
				throw new ArgumentNullException(nameof(sig));
			if (encryptionKey is null)
				throw new ArgumentNullException(nameof(encryptionKey));
			return encryptionKey.TryRecoverDecryptionKey(sig, this, out decryptionKey);
		}
	}
}
#endif
