#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	partial class ECXOnlyPubKey : IComparable<ECXOnlyPubKey>
	{
		internal static byte[] TAG_BIP0340Challenge = ASCIIEncoding.ASCII.GetBytes("BIP0340/challenge");
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly GE Q;

#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly Context ctx;

		public static bool TryCreate(ReadOnlySpan<byte> input32, [MaybeNullWhen(false)] out ECXOnlyPubKey pubkey)
		{
			return TryCreate(input32, null, out pubkey);
		}
		public static bool TryCreate(ReadOnlySpan<byte> input32, Context? context, [MaybeNullWhen(false)] out ECXOnlyPubKey pubkey)
		{
			pubkey = null;
			if (input32.Length != 32)
				return false;
			if (!FE.TryCreate(input32, out var x))
				return false;
			return TryCreate(x, context, out pubkey);
		}
		public static bool TryCreate(in FE x, [MaybeNullWhen(false)] out ECXOnlyPubKey pubkey)
		{
			return TryCreate(x, null, out pubkey);
		}
		public static bool TryCreate(in FE x, Context? context, [MaybeNullWhen(false)] out ECXOnlyPubKey pubkey)
		{
			if (!GE.TryCreateXOVariable(x, false, out var ge))
			{
				pubkey = null;
				return false;
			}
			pubkey = new ECXOnlyPubKey(ge, context);
			return true;
		}
		public static ECXOnlyPubKey Create(ReadOnlySpan<byte> input32)
		{
			return Create(input32, null);
		}
		public static ECXOnlyPubKey Create(ReadOnlySpan<byte> input32, Context? context)
		{
			if (!TryCreate(input32, context, out var k))
				throw new FormatException(message: "Invalid xonly pubkey");
			return k;
		}
		internal ECXOnlyPubKey(in GE ge, Context? context)
		{
			if (ge.IsInfinity)
			{
				throw new InvalidOperationException("A pubkey can't be an infinite group element");
			}
			var x = ge.x.NormalizeVariable();
			var y = ge.y.NormalizeVariable();
			Q = new GE(x, y);
			this.ctx = context ?? Context.Instance;
		}

		public bool SigVerifyBIP340(SecpSchnorrSignature signature, ReadOnlySpan<byte> msg32)
		{
			if (signature is null)
				return false;
			if (msg32.Length < 32)
				return false;
			Span<byte> buf = stackalloc byte[32];
			SHA256 sha = new SHA256();
			sha.InitializeTagged(TAG_BIP0340Challenge);

			signature.rx.WriteToSpan(buf);
			sha.Write(buf);
			Q.x.WriteToSpan(buf);
			sha.Write(buf);
			sha.Write(msg32.Slice(0, 32));
			sha.GetHash(buf);
			var e = new Scalar(buf, out _);

			/* Compute rj =  s*G + (-e)*pkj */
			e = e.Negate();
			var pkj = this.Q.ToGroupElementJacobian();
			var rj = ctx.EcMultContext.Mult(pkj, e, signature.s);

			var r = rj.ToGroupElementVariable();
			if (r.IsInfinity)
				return false;
			return !r.y.Normalize().IsOdd && signature.rx.EqualsVariable(r.x);
		}

		/// <summary>
		/// Write the 32 bytes of the X value of the public key to output32
		/// </summary>
		/// <param name="output32"></param>
		public void WriteToSpan(Span<byte> output32)
		{
			Q.x.WriteToSpan(output32);
		}

		/// <summary>
		/// Checks that a tweaked pubkey is the result of calling AddTweak on internalPubKey and tweak32
		/// </summary>
		/// <param name="internalPubKey">The internal PubKey</param>
		/// <param name="tweak32">The tweak to add to internalPubKey</param>
		/// <param name="expectedParity">The expected parity</param>
		/// <returns></returns>
		public bool CheckIsTweakedWith(ECXOnlyPubKey internalPubKey, ReadOnlySpan<byte> tweak32, bool expectedParity)
		{
			if (tweak32.Length != 32)
				throw new ArgumentException(nameof(tweak32), "tweak32 should be 32 bytes");
			var tweaked_pubkey32 = this;
			GE pk = internalPubKey.Q;
			if (!ECPubKey.secp256k1_ec_pubkey_tweak_add_helper(ctx.EcMultContext, ref pk, tweak32))
				return false;
			pk = new GE(pk.x.NormalizeVariable(), pk.y.NormalizeVariable());
			Span<byte> expected = stackalloc byte[32];
			Span<byte> actual = stackalloc byte[32];
			this.Q.x.WriteToSpan(actual);
			pk.x.WriteToSpan(expected);
			return ECPubKey.secp256k1_memcmp_var(expected, actual, 32) == 0 && pk.y.IsOdd == expectedParity;
		}
		public ECPubKey AddTweak(ReadOnlySpan<byte> tweak)
		{
			if (TryAddTweak(tweak, out var r))
				return r!;
			throw new ArgumentException(paramName: nameof(tweak), message: "Invalid tweak");
		}
		public bool TryAddTweak(ReadOnlySpan<byte> tweak32, [MaybeNullWhen(false)] out ECPubKey tweakedPubKey)
		{
			if (tweak32.Length != 32)
				throw new ArgumentException(nameof(tweak32), "tweak32 should be 32 bytes");
			tweakedPubKey = null;
			var pk = Q;
			if (!ECPubKey.secp256k1_ec_pubkey_tweak_add_helper(ctx.EcMultContext, ref pk, tweak32))
				return false;
			tweakedPubKey = new ECPubKey(pk, ctx);
			return true;
		}

		public byte[] ToBytes()
		{
			byte[] buf = new byte[32];
			WriteToSpan(buf);
			return buf;
		}

		public int CompareTo(ECXOnlyPubKey? other)
		{
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			Span<byte> pk0 = stackalloc byte[32];
			this.WriteToSpan(pk0);
			Span<byte> pk1 = stackalloc byte[32];
			other.WriteToSpan(pk1);
			return ECPubKey.secp256k1_memcmp_var(pk0, pk1, 32);
		}

		public override int GetHashCode()
		{
			return this.Q.x.GetHashCode();
		}
	}
}
#nullable restore
#endif
