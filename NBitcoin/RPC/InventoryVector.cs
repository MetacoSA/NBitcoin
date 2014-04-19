using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	public enum InventoryType : uint
	{
		Error = 0,
		MSG_TX = 1,
		MSG_BLOCK = 2,
	}
	public class InventoryVector : IBitcoinSerializable
	{
		uint type;
		uint256 hash = new uint256(0);

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
			stream.ReadWrite(ref type);
			stream.ReadWrite(ref hash);
		}

		#endregion
	}
}
