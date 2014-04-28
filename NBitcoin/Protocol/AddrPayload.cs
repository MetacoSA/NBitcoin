using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("addr")]
	public class AddrPayload : Payload, IBitcoinSerializable
	{
		NetworkAddress[] addr_list = new NetworkAddress[0];
		public NetworkAddress[] Addresses
		{
			get
			{
				return addr_list;
			}
		}

		public AddrPayload()
		{

		}
		public AddrPayload(NetworkAddress[] addresses)
		{
			addr_list = addresses;
		}

		#region IBitcoinSerializable Members

		public override void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref addr_list);
		}

		#endregion

		public override string ToString()
		{
			return Addresses.Length + " address(es)";
		}
	}
}
