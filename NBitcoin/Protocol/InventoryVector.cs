﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public enum InventoryType : uint
	{
		Error = 0,
		MSG_TX = 1,
		MSG_BLOCK = 2,
		MSG_FILTERED_BLOCK = 3
	}
	public class InventoryVector : Payload, IBitcoinSerializable
	{
		uint type;
		uint256 hash = new uint256(0);

		public InventoryVector()
		{

		}
		public InventoryVector(InventoryType type, uint256 hash)
		{
			Type = type;
			Hash = hash;
		}
		public InventoryType Type
		{
			get
			{
				return (InventoryType)type;
			}
			set
			{
				type = (uint)value;
			}
		}
		public uint256 Hash
		{
			get
			{
				return hash;
			}
			set
			{
				hash = value;
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref type);
			stream.ReadWrite(ref hash);
		}

		#endregion

		public override string ToString()
		{
			return Type.ToString();
		}
	}
}
