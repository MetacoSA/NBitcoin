#nullable enable
#if HAS_SPAN
using NBitcoin.DataEncoders;
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
	public class TaprootPubKey : IAddressableDestination
	{
#if HAS_SPAN
		private ECXOnlyPubKey pubkey;
		internal TaprootPubKey(ECXOnlyPubKey pubkey)
		{
			this.pubkey = pubkey;
		}
#else
		private byte[] pubkey = new byte[32];
#endif

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
				throw new FormatException("The pubkey size should be 32 bytes");
			if (!ECXOnlyPubKey.TryCreate(pubkey, out var k))
				throw new FormatException("Invalid taproot pubkey");
			this.pubkey = k;
		}
#endif
			public TaprootPubKey(byte[] pubkey)
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
				Array.Copy(pubkey, 0, bytes, 1, 32);
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
		public override bool Equals(object obj)
		{
			if (!(obj is TaprootPubKey a))
				return false;
			return a.pubkey.Q.x == this.pubkey.Q.x;
		}
		public static bool operator ==(TaprootPubKey a, TaprootPubKey b)
		{
			if (a is TaprootPubKey && b is TaprootPubKey)
				return a.pubkey.Q.x == b.pubkey.Q.x;
			return a is null && b is null;
		}

		public static bool operator !=(TaprootPubKey a, TaprootPubKey b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return pubkey.GetHashCode();
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
	}
}
