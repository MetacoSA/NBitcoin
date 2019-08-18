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
		MALFORMED = 0x01,
		INVALID = 0x10,
		OBSOLETE = 0x11,
		DUPLICATE = 0x12,
		NONSTANDARD = 0x40,
		DUST = 0x41,
		INSUFFICIENTFEE = 0x42,
		CHECKPOINT = 0x43
	}
	public enum RejectCodeType
	{
		Common,
		Version,
		Transaction,
		Block
	}

	/// <summary>
	/// A transaction or block are rejected being transmitted through tx or block messages
	/// </summary>
	[Payload("reject")]
	public class RejectPayload : Payload
	{
		VarString _Message = new VarString();
		/// <summary>
		/// "tx" or "block"
		/// </summary>
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

		public RejectCodeType CodeType
		{
			get
			{
				switch (Code)
				{
					case RejectCode.MALFORMED:
						return RejectCodeType.Common;
					case RejectCode.OBSOLETE:
						if (Message == "block")
							return RejectCodeType.Block;
						else
							return RejectCodeType.Version;
					case RejectCode.DUPLICATE:
						if (Message == "tx")
							return RejectCodeType.Transaction;
						else
							return RejectCodeType.Version;
					case RejectCode.NONSTANDARD:
					case RejectCode.DUST:
					case RejectCode.INSUFFICIENTFEE:
						return RejectCodeType.Transaction;
					case RejectCode.CHECKPOINT:
						return RejectCodeType.Block;
					case RejectCode.INVALID:
						if (Message == "tx")
							return RejectCodeType.Transaction;
						else
							return RejectCodeType.Block;
					default:
						return RejectCodeType.Common;
				}
			}
		}

		VarString _Reason = new VarString();
		/// <summary>
		/// Details of the error
		/// </summary>
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

		uint256 _Hash;
		/// <summary>
		/// The hash being rejected
		/// </summary>
		public uint256 Hash
		{
			get
			{
				return _Hash;
			}
			set
			{
				_Hash = value;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Message);
			stream.ReadWrite(ref _Code);
			stream.ReadWrite(ref _Reason);
			if (Message == "tx" || Message == "block")
				stream.ReadWrite(ref _Hash);
		}
	}
}
