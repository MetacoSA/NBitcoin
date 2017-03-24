using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class UnknowPayload : Payload
	{
		public UnknowPayload()
		{

		}
		public UnknowPayload(string command)
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
			stream.ReadWrite(ref _Data);
		}
	}
}
