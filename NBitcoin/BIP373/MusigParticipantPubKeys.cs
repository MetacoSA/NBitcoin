#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	internal class MusigParticipantPubKeys
	{
		public MusigParticipantPubKeys(PubKey aggregated, PubKey[] pubKeys)
		{
			if (aggregated is null)
				throw new ArgumentNullException(nameof(aggregated));
			if (pubKeys is null)
				throw new ArgumentNullException(nameof(pubKeys));
			if (pubKeys.Length is 0)
				throw new ArgumentException("pubKeys cannot be an empty collection.", nameof(pubKeys));
			if (aggregated.IsCompressed)
				throw new ArgumentException("The aggregated key must be uncompressed.", nameof(aggregated));
			foreach (var pk in pubKeys)
			{
				if (pk is null)
					throw new ArgumentNullException(nameof(pubKeys), "pubKeys cannot contain null elements.");
				if (!pk.IsCompressed)
					throw new ArgumentException("All public keys must be compressed.", nameof(pubKeys));
			}
			Aggregated = aggregated;
			PubKeys = pubKeys;
		}
		/// <summary>
		/// The MuSig2 aggregate plain public key[1] from the KeyAgg algorithm. This key may or may not be in the script directly (as x-only). It may instead be a parent public key from which the public keys in the script were derived.
		/// </summary>
		public PubKey Aggregated { get; private set; }

		/// <summary>
		/// A list of the compressed public keys of the participants in the MuSig2 aggregate key in the order required for aggregation. If sorting was done, then the keys must be in the sorted order.
		/// </summary>
		public PubKey[] PubKeys { get; private set; }

		internal static MusigParticipantPubKeys Parse(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
		{
			var agg = new PubKey(key[1..]);
			var pubKeys = new PubKey[value.Length / 33];
			int index = 0;
			for (int i = 0; i < value.Length; i += 33)
			{
				pubKeys[index++] = new PubKey(value.Slice(i, 33));
			}
			return new MusigParticipantPubKeys(agg, pubKeys);
		}
	}
}
#endif
