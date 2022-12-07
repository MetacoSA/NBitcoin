#nullable enable
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Crypto;

namespace NBitcoin
{
	public class TaprootPubKey : IAddressableDestination, IPubKey, IComparable<TaprootPubKey>
	{
#if HAS_SPAN
		internal readonly ECXOnlyPubKey pubkey;
		internal TaprootPubKey(ECXOnlyPubKey pubkey)
		{
			this.pubkey = pubkey;
		}
#else
		private byte[] pubkey = new byte[32];
#endif

		public static bool TryCreate(byte[] pubkey, [MaybeNullWhen(false)] out TaprootPubKey result)
		{
#if HAS_SPAN
			return TryCreate(pubkey.AsSpan(), out result);
#else
			if (pubkey.Length != 32)
			{
				result = null;
				return false;
			}
			result = new TaprootPubKey(pubkey);
			return true;
#endif
		}

#if HAS_SPAN
		public static bool TryCreate(ReadOnlySpan<byte> pubkey, [MaybeNullWhen(false)] out TaprootPubKey result)
		{
			if (ECXOnlyPubKey.TryCreate(pubkey, out var k))
			{
				result = new TaprootPubKey(k);
				return true;
			}
			result = null;
			return false;
		}
		public TaprootPubKey(ReadOnlySpan<byte> pubkey)
		{
			if (pubkey.Length != 32)
				throw new ArgumentException("The pubkey size should be 32 bytes");
			if (!ECXOnlyPubKey.TryCreate(pubkey, out var k))
				throw new ArgumentException("Invalid taproot pubkey");
			this.pubkey = k;
		}

		public static TaprootPubKey Parse(string hex)
		{
			if (!TryCreate(Encoders.Hex.DecodeData(hex), out var result))
				throw new FormatException($"Invalid x-only pubkey {hex}");
			return result;
		}
#endif
			public TaprootPubKey(byte[] pubkey)
		{
			if (pubkey.Length != 32)
				throw new ArgumentException("The pubkey size should be 32 bytes", nameof(pubkey));
#if HAS_SPAN
			if (!ECXOnlyPubKey.TryCreate(pubkey, out var k))
				throw new ArgumentException("Invalid taproot pubkey", nameof(pubkey));
			this.pubkey = k;
#else
			pubkey.CopyTo(this.pubkey, 0);
#endif
		}

		Script? scriptPubKey;
		public Script ScriptPubKey
		{
			get
			{
				if (scriptPubKey is Script s)
					return s;
				var bytes = new byte[34];
				bytes[0] = 0x51;
				bytes[1] = 32;
#if !HAS_SPAN
				Array.Copy(pubkey, 0, bytes, 2, 32);
#else
				ToBytes(bytes.AsSpan().Slice(2));
#endif
				s = Script.FromBytesUnsafe(bytes);
				scriptPubKey = s;
				return s;
			}
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
		public TaprootAddress GetAddress(Network network)
		{
			return new TaprootAddress(this, network);
		}
#if HAS_SPAN
		public override bool Equals(object? obj)
		{
			if (!(obj is TaprootPubKey a))
				return false;
			return a.pubkey.Q.x == this.pubkey.Q.x;
		}
		public static bool operator ==(TaprootPubKey? a, TaprootPubKey? b)
		{
			if (a is TaprootPubKey && b is TaprootPubKey)
				return a.pubkey.Q.x == b.pubkey.Q.x;
			return a is null && b is null;
		}

		public static bool operator !=(TaprootPubKey? a, TaprootPubKey? b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return pubkey.GetHashCode();
		}

		public bool VerifySignature(uint256 hash, SchnorrSignature signature)
		{
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			return this.pubkey.SigVerifyBIP340(signature.secpShnorr, hash.ToBytes());
		}
#else
		public override bool Equals(object obj)
		{
			if (!(obj is TaprootPubKey a))
				return false;
			return Utils.ArrayEqual(pubkey, a.pubkey);
		}
		public static bool operator ==(TaprootPubKey a, TaprootPubKey b)
		{
			if (a is TaprootPubKey && b is TaprootPubKey)
				return Utils.ArrayEqual(a.pubkey, b.pubkey);
			return a is null && b is null;
		}

		public static bool operator !=(TaprootPubKey a, TaprootPubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return System.Collections.StructuralComparisons.StructuralEqualityComparer.GetHashCode(pubkey);
		}
#endif
		BitcoinAddress IAddressableDestination.GetAddress(Network network)
		{
			return this.GetAddress(network);
		}
		Script IDestination.ScriptPubKey => ScriptPubKey;

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
		public int CompareTo(TaprootPubKey? other)
		{
			return this.pubkey.CompareTo(other?.pubkey);
		}
#else
		public int CompareTo(TaprootPubKey other)
		{
			return BytesComparer.Instance.Compare(this.pubkey, other.pubkey);
		}
#endif
		bool IAddressableDestination.IsSupported(Network network)
		{
			return network.Consensus.SupportTaproot;
		}
#if HAS_SPAN
		public bool CheckTapTweak(TaprootInternalPubKey internalPubKey, uint256? merkleRoot, bool parity)
		{
			if (internalPubKey is null)
				throw new ArgumentNullException(nameof(internalPubKey));

			Span<byte> tweak32 = stackalloc byte[32];
			TaprootFullPubKey.ComputeTapTweak(internalPubKey, merkleRoot, tweak32);
			return this.pubkey.CheckIsTweakedWith(internalPubKey.pubkey, tweak32, parity);
		}
#endif
	}
}
