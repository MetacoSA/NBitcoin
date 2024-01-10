﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// A block received after being asked with a getdata message
	/// </summary>

	public class BlockPayload : BitcoinSerializablePayload<Block>
	{
		public override string Command => "block";
		public BlockPayload()
		{

		}
		public BlockPayload(Block block)
			: base(block)
		{

		}
	}
}
