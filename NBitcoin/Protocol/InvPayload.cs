using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("inv")]
	public class InvPayload : Payload, IBitcoinSerializable
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

		public override void ReadWriteCore(BitcoinStream stream)
		{
			var old = stream.MaxArraySize;
			stream.MaxArraySize = 5000;
			stream.ReadWrite(ref inventory);
			stream.MaxArraySize = old;
		}

		#endregion

		public override string ToString()
		{
			return "Count: " + Inventory.Length.ToString();
		}
	}
}
