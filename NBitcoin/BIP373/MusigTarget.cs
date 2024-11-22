#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public record MusigTarget(PubKey ParticipantPubKey, PubKey AggregatePubKey, uint256? TapLeaf) : IComparable<MusigTarget>
	{
		internal static MusigTarget Parse(ReadOnlySpan<byte> k)
		{
			var participant = new PubKey(k[1..]);
			if (!participant.IsCompressed)
				throw new FormatException("The participant public key must be compressed.");
			var agg = new PubKey(k[(1 + 33)..]);
			if (!participant.IsCompressed)
				throw new FormatException("The aggregate public key must be compressed.");
			var tapleaf = k[(1 + 33 + 33)..];
			var h = tapleaf.Length is 0 ? null : new uint256(tapleaf);
			return new MusigTarget(participant, agg, h);
		}

		public int CompareTo(MusigTarget? other) => other is null ? 1 : PubKeyComparer.Instance.Compare(ParticipantPubKey, other?.ParticipantPubKey);

		public byte[] ToBytes(byte key)
		{
			var result = new byte[1 + 33 + 33 + (TapLeaf is null ? 0 : 32)];
			result[0] = key;
			ParticipantPubKey.ToBytes(result.AsSpan(1), out _);
			ParticipantPubKey.ToBytes(result.AsSpan(1 + 33), out _);
			if (TapLeaf is not null)
				TapLeaf.ToBytes(result.AsSpan(1 + 33 + 33));
			return result;
		}
	}
}
#endif
