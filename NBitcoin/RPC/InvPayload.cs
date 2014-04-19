using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	[Payload("inv")]
	public class InvPayload :  IBitcoinSerializable
	{
		InventoryVector[] inventory = new InventoryVector[0];
		public InventoryVector[] Inventory
		{
			get
			{
				return inventory;
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			var old = stream.MaxArraySize;
			stream.MaxArraySize = 5000;
			stream.ReadWrite(ref inventory);
			stream.MaxArraySize = old;
		}

		#endregion
	}
}
