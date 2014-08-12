﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("getblocks")]
	public class GetBlocksPayload : Payload
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

		BlockLocator blockLocators;

		public BlockLocator BlockLocators
		{
			get
			{
				return blockLocators;
			}
			set
			{
				blockLocators = value;
			}
		}
		uint256 _HashStop = new uint256(0);
		public uint256 HashStop
		{
			get
			{
				return _HashStop;
			}
			set
			{
				_HashStop = value;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref version);
			stream.ReadWrite(ref blockLocators);
			stream.ReadWrite(ref _HashStop);
		}
	}
}
