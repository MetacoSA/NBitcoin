using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class Message : IBitcoinSerializable
	{
		uint magic;

		public uint Magic
		{
			get
			{
				return magic;
			}
			set
			{
				magic = value;
			}
		}
		byte[] command = new byte[12];

		public string Command
		{
			get
			{
				return Utils.BytesToString(command);
			}
			set
			{
				command = Utils.StringToBytes(value.Trim().PadRight(12, '\0'));
			}
		}
		uint length;

		public uint Length
		{
			get
			{
				return length;
			}
			set
			{
				length = value;
			}
		}
		uint checksum;

		public uint Checksum
		{
			get
			{
				return checksum;
			}
			set
			{
				checksum = value;
			}
		}
		byte[] payload;
		object _PayloadObject;
		public object Payload
		{
			get
			{
				return _PayloadObject;
			}
		}

		

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			bool verifyChechksum = false;
			if(!SkipMagic)
				stream.ReadWrite(ref magic);
			stream.ReadWrite(ref command);
			stream.ReadWrite(ref length);
			if(stream.ProtocolVersion >= ProtocolVersion.MEMPOOL_GD_VERSION)
			{
				stream.ReadWrite(ref checksum);
				verifyChechksum = true;
			}
			if(stream.Serializing)
			{
				stream.ReadWrite(ref payload);
			}
			else
			{
				payload = new byte[length];
				stream.ReadWrite(ref payload);

				if(verifyChechksum)
				{
					if(VerifyChecksum())
						throw new FormatException("Message checksum invalid");
				}

				BitcoinStream payloadStream = new BitcoinStream(payload);
				payloadStream.CopyParameters(stream);

				var payloadType = PayloadAttribute.GetCommandType(Command);
				payloadStream.ReadWrite(payloadType, ref _PayloadObject);
			}
		}

		#endregion

		public bool VerifyChecksum()
		{
			return Checksum != Hashes.Hash256(payload).GetLow32();
		}

		public void UpdatePayload(Payload payload, ProtocolVersion version)
		{
			if(payload == null)
				throw new ArgumentNullException("payload");
			this.payload = payload.ToBytes(version);
			length = (uint)this.payload.Length;
			checksum = Hashes.Hash256(this.payload).GetLow32();
			Command = payload.Command;
		}


		/// <summary>
		/// When parsing, maybe Magic is already parsed
		/// </summary>
		public bool SkipMagic
		{
			get;
			set;
		}
	}
}
