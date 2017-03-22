using System.Collections.Generic;

namespace NBitcoin.Protocol.Payloads
{
	/// <summary>
	/// Ask for transaction, block or merkle block
	/// </summary>
	[Payload("getdata")]
	public class GetDataPayload : Payload
	{
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

