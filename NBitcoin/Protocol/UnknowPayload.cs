using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Obsolete("Use UnknownPayload")]
	public class UnknowPayload : UnknownPayload
	{
	}

	public class UnknownPayload : Payload
	{
		public UnknownPayload()
		{

		}
		public UnknownPayload(string command)
		{
			_Command = command;
		}
		internal string _Command;
		public override string Command
		{
			get
			{
				return _Command;
			}
		}
		private byte[] _Data = new byte[0];
		public byte[] Data
		{
			get
			{
				return _Data;
			}
			set
			{
				_Data = value;
			}
		}
		public override void ReadWriteCore(BitcoinStream stream)
		{
			if (stream.Serializing) {
				stream.ReadWrite(_Data);
			} else {
				MemoryStream ms = new MemoryStream();
				stream.Inner.CopyTo(ms);
				_Data = ms.ToArray();
			}
		}
	}
}
