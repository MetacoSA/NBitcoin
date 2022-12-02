using System;
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
		MSG_WTX = 5,
		// Nodes may always request a MSG_FILTERED_BLOCK/MSG_CMPCT_BLOCK in a getdata, however,
		// MSG_FILTERED_BLOCK/MSG_CMPCT_BLOCK should not appear in any invs except as a part of getdata.
		MSG_FILTERED_BLOCK = 3,
		MSG_CMPCT_BLOCK,
		// The following can only occur in getdata. Invs always use TX or BLOCK.
		MSG_TYPE_MASK = 0xffffffff >> 2,
		MSG_WITNESS_FLAG = 1 << 30,
		MSG_WITNESS_BLOCK = MSG_BLOCK | MSG_WITNESS_FLAG,
		MSG_WITNESS_TX = MSG_TX | MSG_WITNESS_FLAG,
		MSG_FILTERED_WITNESS_BLOCK = MSG_FILTERED_BLOCK | MSG_WITNESS_FLAG
	}
	public class InventoryVector : IBitcoinSerializable
	{
		uint type;
		uint256 hash = uint256.Zero;

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

		public void ReadWrite(BitcoinStream stream)
		{
			using (stream.SerializationTypeScope(SerializationType.Network))
			{
				ReadWriteCore(stream);
			}
		}
		public void ReadWriteCore(BitcoinStream stream)
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
