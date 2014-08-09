﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
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

        public GetDataPayload(IEnumerable<InventoryVector> Invs):this()
        {
            inventory.AddRange(Invs);
        }

        public GetDataPayload(InventoryVector Inv):this()
        {
            inventory.Add(Inv);
        }

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

