#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
#if !NO_BC
using NBitcoin.BouncyCastle.Asn1.X9;
using NBitcoin.BouncyCastle.Math.EC.Custom.Sec;
using NBitcoin.BouncyCastle.Math.EC;
using NBitcoin.BouncyCastle.Asn1;
using NBitcoin.BouncyCastle.Math;
#endif
using NBitcoin.DataEncoders;

namespace NBitcoin.Crypto
{
	public class SchnorrSignature
	{
#if HAS_SPAN
		internal Secp256k1.SecpSchnorrSignature secpShnorr;
#else
		internal BigInteger R { get; }
		internal BigInteger S { get; }
#endif
		public static SchnorrSignature Parse(string hex)
		{
			var bytes = Encoders.Hex.DecodeData(hex);
			return new SchnorrSignature(bytes);
		}
#if HAS_SPAN
		public static bool TryParse(ReadOnlySpan<byte> in64, [MaybeNullWhen(false)] out SchnorrSignature sig)
		{
			sig = null;
			if (in64.Length != 64)
				return false;
			if (!Secp256k1.SecpSchnorrSignature.TryCreate(in64, out var secpShnorr) || secpShnorr is null)
				return false;
			sig = new SchnorrSignature(secpShnorr);
			return true;
		}
		public static bool TryParse(byte[] in64, [MaybeNullWhen(false)] out SchnorrSignature sig)
		{
			return TryParse(in64.AsSpan(), out sig);
		}
#else
		public static bool TryParse(byte[] in64, [MaybeNullWhen(false)] out SchnorrSignature sig)
		{
			if (in64 == null)
				throw new ArgumentNullException(nameof(in64));
			sig = null;
			if (in64.Length != 64)
				return false;
			sig = new SchnorrSignature(in64);
			return true;
		}
#endif

		public SchnorrSignature(byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if (bytes.Length != 64)
				throw new ArgumentException(paramName: nameof(bytes), message:"Invalid schnorr signature length.");
#if HAS_SPAN
			if (!Secp256k1.SecpSchnorrSignature.TryCreate(bytes, out var s))
				throw new ArgumentException(paramName: nameof(bytes), message: "Invalid schnorr signature.");
			secpShnorr = s;
#else
			R = new BigInteger(1, bytes, 0, 32);
			S = new BigInteger(1, bytes, 32, 32);
#endif
		}

#if HAS_SPAN
		internal SchnorrSignature(Secp256k1.SecpSchnorrSignature secpShnorr)
		{
			this.secpShnorr = secpShnorr;
		}
		public byte[] ToBytes()
		{
			var buf = new byte[64];
			this.secpShnorr.WriteToSpan(buf);
			return buf;
		}
#else
		internal SchnorrSignature(BigInteger r, BigInteger s)
		{
			R = r;
			S = s;
		}
		public byte[] ToBytes()
		{
			return Utils.BigIntegerToBytes(R, 32).Concat(Utils.BigIntegerToBytes(S, 32));
		}
#endif
	}

}
