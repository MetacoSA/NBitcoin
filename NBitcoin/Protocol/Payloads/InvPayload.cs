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
		List<InventoryVector> _Inventory = new List<InventoryVector>();
		public List<InventoryVector> Inventory
		{
			get
			{
				return _Inventory;
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			var old = stream.MaxArraySize;
			stream.MaxArraySize = 5000;
			stream.ReadWrite(ref _Inventory);
			stream.MaxArraySize = old;
		}

		#endregion

		public override string ToString()
		{
			return "Count: " + Inventory.Count.ToString();
		}
	}
}
