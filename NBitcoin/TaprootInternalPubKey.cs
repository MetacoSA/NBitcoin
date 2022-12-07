#nullable enable
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TaprootInternalPubKey : IEquatable<TaprootInternalPubKey>
	{
#if HAS_SPAN
		internal readonly ECXOnlyPubKey pubkey;
		internal TaprootInternalPubKey(ECXOnlyPubKey pubkey)
		{
			this.pubkey = pubkey;
		}
#else
		private byte[] pubkey = new byte[32];
#endif


#if HAS_SPAN
		public static TaprootInternalPubKey Parse(string hex)
		{
			if (!TryParse(hex, out var result))
				throw new FormatException($"Failed to parse TaprootInternalKey {hex}");
			return result;
		}
		public static bool TryParse(string hex, [MaybeNullWhen(false)] out TaprootInternalPubKey result)
			=> TryCreate(Encoders.Hex.DecodeData(hex), out result);
#endif
		public static bool TryCreate(byte[] pubkey, [MaybeNullWhen(false)] out TaprootInternalPubKey result)
		{
#if HAS_SPAN
			return TryCreate(pubkey.AsSpan(), out result);
#else
			if (pubkey.Length != 32)
			{
				result = null;
				return false;
			}
			result = new TaprootInternalPubKey(pubkey);
			return true;
#endif
		}

#if HAS_SPAN
		public static bool TryCreate(ReadOnlySpan<byte> pubkey, [MaybeNullWhen(false)] out TaprootInternalPubKey result)
		{
			if (ECXOnlyPubKey.TryCreate(pubkey, out var k))
			{
				result = new TaprootInternalPubKey(k);
				return true;
			}
			result = null;
			return false;
		}
		public TaprootInternalPubKey(ReadOnlySpan<byte> pubkey)
		{
			if (pubkey.Length != 32)
				throw new FormatException("The pubkey size should be 32 bytes");
			if (!ECXOnlyPubKey.TryCreate(pubkey, out var k))
				throw new FormatException("Invalid taproot pubkey");
			this.pubkey = k;
		}
		public TaprootFullPubKey GetTaprootFullPubKey()
		{
			return GetTaprootFullPubKey(null);
		}
		public TaprootFullPubKey GetTaprootFullPubKey(uint256? merkleRoot)
		{
			return TaprootFullPubKey.Create(this, merkleRoot);
		}

#endif
		public TaprootInternalPubKey(byte[] pubkey)
		{
			if (pubkey.Length != 32)
				throw new FormatException("The pubkey size should be 32 bytes");
#if HAS_SPAN
			if (!ECXOnlyPubKey.TryCreate(pubkey, out var k))
				throw new FormatException("Invalid taproot pubkey");
			this.pubkey = k;
#else
			pubkey.CopyTo(this.pubkey, 0);
#endif
		}

		public byte[] ToBytes()
		{
#if HAS_SPAN
			byte[] out32 = new byte[32];
			ToBytes(out32);
			return out32;
#else
			byte[] out32 = new byte[32];
			pubkey.CopyTo(out32, 0);
			return out32;
#endif
		}
#if HAS_SPAN
		public void ToBytes(Span<byte> out32)
		{
			pubkey.WriteToSpan(out32);
		}
#endif

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((TaprootInternalPubKey)obj);
		}
		public bool Equals(TaprootInternalPubKey? other)
		{
			if (ReferenceEquals(null, other)) return false;
#if HAS_SPAN
			return Utils.ArrayEqual(other.pubkey.ToBytes(), pubkey.ToBytes());
#else
		return Utils.ArrayEqual(other.pubkey, pubkey);
#endif
		}

#if HAS_SPAN


		public static bool operator ==(TaprootInternalPubKey a, TaprootInternalPubKey b)
		{
			if (a is TaprootInternalPubKey && b is TaprootInternalPubKey)
				return a.pubkey.Q.x == b.pubkey.Q.x;
			return a is null && b is null;
		}

		public static bool operator !=(TaprootInternalPubKey a, TaprootInternalPubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return pubkey.GetHashCode();
		}
#else

		public static bool operator ==(TaprootInternalPubKey a, TaprootInternalPubKey b)
		{
			if (a is TaprootInternalPubKey && b is TaprootInternalPubKey)
				return Utils.ArrayEqual(a.pubkey, b.pubkey);
			return a is null && b is null;
		}

		public static bool operator !=(TaprootInternalPubKey a, TaprootInternalPubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return System.Collections.StructuralComparisons.StructuralEqualityComparer.GetHashCode(pubkey);
		}
#endif

		public override string ToString()
		{
#if HAS_SPAN
			Span<byte> b = stackalloc byte[32];
			ToBytes(b);
			return Encoders.Hex.EncodeData(b);
#else
			return Encoders.Hex.EncodeData(ToBytes());
#endif
		}

#if HAS_SPAN
		public bool VerifyTaproot(uint256 hash, uint256? merkleRoot, SchnorrSignature signature)
		{
			return this.GetTaprootFullPubKey(merkleRoot).OutputKey.VerifySignature(hash, signature);
		}
		public int CompareTo(TaprootInternalPubKey other)
		{
			return this.pubkey.CompareTo(other.pubkey);
		}
#else
		public int CompareTo(TaprootInternalPubKey other)
		{
			return BytesComparer.Instance.Compare(this.pubkey, other.pubkey);
		}
#endif
	}
}
