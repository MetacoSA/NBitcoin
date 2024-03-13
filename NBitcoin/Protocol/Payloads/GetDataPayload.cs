using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Ask for transaction, block or merkle block
	/// </summary>

	public class GetDataPayload : Payload
	{
		public override string Command => "getdata";
		public GetDataPayload()
		{
		}
		public GetDataPayload(params InventoryVector[] vectors)
		{
			inventory.AddRange(vectors);
		}
		List<InventoryVector> inventory = new List<InventoryVector>();

		public List<InventoryVector> Inventory
		{
			set
			{
				inventory = value;
			}
			get
			{
				return inventory;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref inventory);
		}
	}
}

