using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public enum RejectCode : byte
	{
		Malformed = 0x01,
		Invalid = 0x10,
		Obsolete = 0x11,
		Duplicate = 0x12,
		NonStandard = 0x40,
		Dust = 0x41,
		Insufficientfee = 0x42,
		Checkpoint = 0x43
	}
	[Payload("reject")]
	public class RejectPayload : Payload
	{
		VarString _Message= new VarString();
		public string Message
		{
			get
			{
				return Encoders.ASCII.EncodeData(_Message.GetString(true));
			}
			set
			{
				_Message = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}
		byte _Code;
		public RejectCode Code
		{
			get
			{
				return (RejectCode)_Code;
			}
			set
			{
				_Code = (byte)value;
			}
		}
		VarString _Reason= new VarString();
		public string Reason
		{
			get
			{
				return Encoders.ASCII.EncodeData(_Reason.GetString(true));
			}
			set
			{
				_Reason = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Message);
			stream.ReadWrite(ref _Code);
			stream.ReadWrite(ref _Reason);
		}
	}
}
