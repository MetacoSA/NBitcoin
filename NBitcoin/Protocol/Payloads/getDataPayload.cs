using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
    [Payload("getdata")]
	public class GetDataPayload : Payload
	{
        List<InventoryVector> inventory;

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

