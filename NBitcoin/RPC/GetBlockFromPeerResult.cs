using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
    public enum GetBlockFromPeerResult
    {
		/// <summary>
		/// Successfully fetched the block from the peer
		/// </summary>
		Fetched,
		/// <summary>
		/// Peer does not exist
		/// </summary>
		UnknownPeerId,
		/// <summary>
		/// Block already downloaded
		/// </summary>
		AlreadyDownloaded,
		/// <summary>
		/// Block header missing
		/// </summary>
		BlockHeaderMissing,
		/// <summary>
		/// In prune mode, only blocks that the node has already synced previously can be fetched from a peer
		/// </summary>
		NeverSynched
	}
}
