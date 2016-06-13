using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol.Payloads
{
	[Payload("sendcmpct")]
	public class SendCmpctPayload : Payload
	{

		uint _PreferHeaderAndIDs;
		public bool PreferHeaderAndIDs
		{
			get
			{
				return _PreferHeaderAndIDs == 1;
			}
			set
			{
				_PreferHeaderAndIDs = value ? 1U : 0U;
			}
		}


		uint _Version = 1;
		public uint Version
		{
			get
			{
				return _Version;
			}
			set
			{
				_Version = value;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _PreferHeaderAndIDs);
			stream.ReadWrite(ref _Version);
		}
	}
}
