using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("sendcmpct")]
	public class SendCmpctPayload : Payload
	{
		public SendCmpctPayload()
		{

		}
		public SendCmpctPayload(bool preferHeaderAndIDs)
		{
			PreferHeaderAndIDs = preferHeaderAndIDs;
		}
		byte _PreferHeaderAndIDs;
		public bool PreferHeaderAndIDs
		{
			get
			{
				return _PreferHeaderAndIDs == 1;
			}
			set
			{
				_PreferHeaderAndIDs = value ? (byte)1 : (byte)0;
			}
		}


		ulong _Version = 1;
		public ulong Version
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
