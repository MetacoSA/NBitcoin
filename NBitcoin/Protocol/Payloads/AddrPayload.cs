#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// An available peer address in the bitcoin network is announced (unsolicited or after a getaddr)
	/// </summary>
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
		public AddrPayload(NetworkAddress address)
		{
			addr_list = new NetworkAddress[] { address };
		}
		public AddrPayload(NetworkAddress[] addresses)
		{
			addr_list = addresses.ToArray();
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref addr_list);
		}

		#endregion

		public override string ToString()
		{
			return Addresses.Length + " address(es)";
		}
	}

	/// <summary>
	/// An available peer address in the bitcoin network is announced (unsolicited or after a getaddrv2)
	/// </summary>
	[Payload("addrv2")]
	public class AddrV2Payload : AddrPayload
	{
		public AddrV2Payload()
			: base()
		{
		}
		public AddrV2Payload(NetworkAddress address)
			: base(address)
		{
		}

		public AddrV2Payload(NetworkAddress[] addresses)
			: base(addresses)
		{
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			using (stream.ProtocolVersionScope(NetworkAddress.AddrV2Format))
			{
				base.ReadWriteCore(stream);
			}
		}
	}
}
#endif