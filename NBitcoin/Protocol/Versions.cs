using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	//
	// network protocol versioning
	//
	public enum ProtocolVersion : uint
	{
		PROTOCOL_VERSION = 70002,

		// intial proto version, to be increased after version/verack negotiation
		INIT_PROTO_VERSION = 209,

		// disconnect from peers older than this proto version
		MIN_PEER_PROTO_VERSION = 209,

		// nTime field added to CAddress, starting with this version;
		// if possible, avoid requesting addresses nodes older than this
		CADDR_TIME_VERSION = 31402,

		// only request blocks from nodes outside this range of versions
		NOBLKS_VERSION_START = 32000,
		NOBLKS_VERSION_END = 32400,

		// BIP 0031, pong message, is enabled for all versions AFTER this one
		BIP0031_VERSION = 60000,

		// "mempool" command, enhanced "getdata" behavior starts with this version:
		MEMPOOL_GD_VERSION = 60002
	}
}
