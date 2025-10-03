
using System.Collections;
using System.Collections.Generic;

namespace NBitcoin.Protocol
{
    /// <summary>
    /// Announce ephemeral startup information, such as blocks that can be mined
    /// upon, votes for such blocks and tspends.
    /// </summary>

    public class DecredInitStatePayload : Payload, IBitcoinSerializable
    {
        public override string Command => "initstate";

        // Empty initstate payload, no data to send.
        private ulong zeroCount = 0;

        // private List<uint256> blockHashes;
        // private List<uint256> voteHashes;
        // private List<uint256> tSpendHashes;

        public DecredInitStatePayload() { }

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWriteAsVarInt(ref zeroCount); // zero block hashes
            stream.ReadWriteAsVarInt(ref zeroCount); // zero vote hashes
            stream.ReadWriteAsVarInt(ref zeroCount); // zero tspend hashes
        }
    }
}
