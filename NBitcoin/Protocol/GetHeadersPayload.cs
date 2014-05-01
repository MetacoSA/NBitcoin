using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("getheaders")]
	public class GetHeadersPayload : Payload
	{
		uint version = (uint)ProtocolVersion.PROTOCOL_VERSION;
		public ProtocolVersion Version
		{
			get
			{
				return (ProtocolVersion)version;
			}
			set
			{
				version = (uint)value;
			}
		}

		List<uint256> blockLocators = new List<uint256>();

		public List<uint256> BlockLocators
		{
			get
			{
				return blockLocators;
			}
		}

		uint256 hashStop;
		public uint256 HashStop
		{
			get
			{
				return hashStop;
			}
			set
			{
				hashStop = value;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref version);
			stream.ReadWrite(ref blockLocators);
			stream.ReadWrite(ref hashStop);
		}
	}
}
