#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public partial class PSBTOutput
	{
		public SortedDictionary<PubKey, PubKey[]> MusigParticipantPubKeys { get; } = new SortedDictionary<PubKey, PubKey[]>(PubKeyComparer.Instance);
	}
}
#endif
