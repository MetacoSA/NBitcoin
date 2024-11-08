#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public partial class PSBTInput
	{
		public SortedDictionary<PubKey, PubKey[]> MusigParticipantPubKeys { get; } = new SortedDictionary<PubKey, PubKey[]>(PubKeyComparer.Instance);
		public SortedDictionary<MusigTarget, byte[]> MusigPubNonces { get; } = new SortedDictionary<MusigTarget, byte[]>();
		public SortedDictionary<MusigTarget, byte[]> MusigPartialSigs { get; } = new SortedDictionary<MusigTarget, byte[]>();
	}
}
#endif
