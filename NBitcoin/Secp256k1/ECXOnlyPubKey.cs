#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif

	class ECXOnlyPubKey : ECPubKey
	{
		internal static byte[] TAG_BIP0340Challenge = ASCIIEncoding.ASCII.GetBytes("BIP0340/challenge");
		public static bool TryCreate(ReadOnlySpan<byte> input32, Context context, out ECXOnlyPubKey? pubkey)
		{
			pubkey = null;
			if (input32.Length != 32)
				return false;
			if (!FE.TryCreate(input32, out var x))
				return false;
			return TryCreate(x, context, out pubkey);
		}
		public static bool TryCreate(in FE x, Context context, out ECXOnlyPubKey? pubkey)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!GE.TryCreateXOVariable(x, false, out var ge))
			{
				pubkey = null;
				return false;
			}
			pubkey = new ECXOnlyPubKey(ge, context);
			return true;
		}
		internal ECXOnlyPubKey(in GE ge, Context context) : base(ge, context)
		{
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

		public override ECXOnlyPubKey ToXOnlyPubKey(out bool parity)
		{
			parity = false;
			return this;
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
			if (!internalPubKey.TryAddTweak(tweak32, out var actualTweakedPubKey) || actualTweakedPubKey is null)
				return false;
			var actualTweakedKey = actualTweakedPubKey.ToXOnlyPubKey(out var actualParity);
			return actualParity == expectedParity && actualTweakedKey == this;
		}

		public new byte[] ToBytes()
		{
			byte[] buf = new byte[32];
			WriteToSpan(buf);
			return buf;
		}
	}
}
#nullable restore
#endif
