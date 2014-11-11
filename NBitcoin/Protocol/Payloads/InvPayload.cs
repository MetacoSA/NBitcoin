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
		public InvPayload()
		{

		}
		public InvPayload(params Transaction[] transactions)
			: this(transactions.Select(tx => new InventoryVector(InventoryType.MSG_TX, tx.GetHash())).ToArray())
		{

		}
		public InvPayload(params Block[] blocks)
			: this(blocks.Select(b => new InventoryVector(InventoryType.MSG_BLOCK, b.GetHash())).ToArray())
		{

		}
		public InvPayload(InventoryType type, params uint256[] hashes)
			: this(hashes.Select(h => new InventoryVector(type, h)).ToArray())
		{

		}
		public InvPayload(params InventoryVector[] invs)
		{
			_Inventory.AddRange(invs);
		}
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
