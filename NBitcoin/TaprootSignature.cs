#nullable enable
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TaprootSignature : ITransactionSignature
	{
		public TaprootSignature(SchnorrSignature schnorrSignature): this(schnorrSignature, TaprootSigHash.Default)
		{

		}
		public TaprootSignature(SchnorrSignature schnorrSignature, TaprootSigHash sigHash)
		{
			if (!TaprootExecutionData.IsValidSigHash((byte)sigHash))
				throw new ArgumentException("Invalid hash_type", nameof(sigHash));
			if (schnorrSignature == null)
				throw new ArgumentNullException(nameof(schnorrSignature));
			SigHash = sigHash;
			SchnorrSignature = schnorrSignature;
		}

		public static bool TryParse(byte[] bytes, [MaybeNullWhen(false)] out TaprootSignature signature)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
#if HAS_SPAN
			return TryParse(bytes.AsSpan(), out signature);
#else
			if (bytes.Length == 64)
			{
				if (!SchnorrSignature.TryParse(bytes, out var sig))
				{
					signature = null;
					return false;
				}
				signature = new TaprootSignature(sig);
				return true;
			}
			else if (bytes.Length == 65)
			{
				if (!TaprootExecutionData.IsValidSigHash(bytes[64])  || bytes[64] == 0)
				{
					signature = null;
					return false;
				}
				var sighash = (TaprootSigHash)bytes[64];
				if (sighash == TaprootSigHash.Default)
				{
					signature = null;
					return false;
				}
				var buff = new byte[64];
				Array.Copy(bytes, 0, buff, 0, 64);
				if (!SchnorrSignature.TryParse(buff, out var sig))
				{
					signature = null;
					return false;
				}
				signature = new TaprootSignature(sig, sighash);
				return true;
			}
			else
			{
				signature = null;
				return false;
			}
#endif
		}
		public static TaprootSignature Parse(string hex)
		{
			if (TryParse(hex, out var r))
				return r;
			throw new FormatException("Invalid taproot signature");
		}

		public static bool TryParse(string hex, [MaybeNullWhen(false)] out TaprootSignature signature)
		{
			if (hex == null)
				throw new ArgumentNullException(nameof(hex));
			try
			{
				var bytes = Encoders.Hex.DecodeData(hex);
				if (TryParse(bytes, out signature))
				{
					return true;
				}
				signature = null;
				return false;
			}
			catch(FormatException)
			{
				signature = null;
				return false;
			}
		}

		public static TaprootSignature Parse(byte[] bytes)
		{
			if (TryParse(bytes, out var r))
				return r;
			throw new FormatException("Invalid taproot signature");
		}
#if HAS_SPAN
		public static TaprootSignature Parse(ReadOnlySpan<byte> bytes)
		{
			if (TryParse(bytes, out var r))
				return r;
			throw new FormatException("Invalid taproot signature");
		}
		public static bool TryParse(ReadOnlySpan<byte> bytes, [MaybeNullWhen(false)] out TaprootSignature signature)
		{
			if (bytes.Length == 64)
			{
				if (!SchnorrSignature.TryParse(bytes, out var sig))
				{
					signature = null;
					return false;
				}
				signature = new TaprootSignature(sig);
				return true;
			}
			else if (bytes.Length == 65)
			{
				if (!TaprootExecutionData.IsValidSigHash(bytes[64]))
				{
					signature = null;
					return false;
				}
				var sighash = (TaprootSigHash)bytes[64];
				if (sighash == TaprootSigHash.Default)
				{
					signature = null;
					return false;
				}
				if (!SchnorrSignature.TryParse(bytes.Slice(0, 64), out var sig))
				{
					signature = null;
					return false;
				}
				signature = new TaprootSignature(sig, sighash);
				return true;
			}
			else
			{
				signature = null;
				return false;
			}
		}
#endif

		public TaprootSigHash SigHash { get; }
		public SchnorrSignature SchnorrSignature { get; }

		public int Length => SigHash is TaprootSigHash.Default ? 64 : 65;

		public byte[] ToBytes()
		{
			if (SigHash == TaprootSigHash.Default)
			{
				return SchnorrSignature.ToBytes();
			}
			else
			{
				var sig = new byte[65];
				SchnorrSignature.ToBytes().CopyTo(sig, 0);
				sig[64] = (byte)SigHash;
				return sig;
			}
		}
		public override string ToString()
		{
			return Encoders.Hex.EncodeData(ToBytes());
		}
	}
}
