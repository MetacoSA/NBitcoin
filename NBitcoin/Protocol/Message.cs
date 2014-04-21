using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
				return Encoders.ASCII.EncodeData(command);
			}
			set
			{
				command = Encoders.ASCII.DecodeData(value.Trim().PadRight(12, '\0'));
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
			if(!_SkipMagic)
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
			this._PayloadObject = payload;
			this.payload = payload.ToBytes(version);
			length = (uint)this.payload.Length;
			checksum = Hashes.Hash256(this.payload).GetLow32();
			Command = payload.Command;
		}


		/// <summary>
		/// When parsing, maybe Magic is already parsed
		/// </summary>
		public bool _SkipMagic;

		public override string ToString()
		{
			return Command + " : " + Payload;
		}



		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken)
		{
			var stream = new NetworkStream(socket, false);
			BitcoinStream bitStream = new BitcoinStream(stream, false)
			{
				ProtocolVersion = version
			};

			var old = socket.ReceiveTimeout;
			try
			{
				socket.ReceiveTimeout = 3000;
				ReadMagic(socket, network.MagicBytes, cancellationToken);
			}
			finally
			{
				socket.ReceiveTimeout = old;
			}
			Message message = new Message();
			message._SkipMagic = true;
			message.Magic = network.Magic;
			message.ReadWrite(bitStream);
			message._SkipMagic = false;
			return message;
		}

		private static void ReadMagic(Socket socket, byte[] magicBytes, CancellationToken cancellation)
		{
			byte[] bytes = new byte[1];
			for(int i = 0 ; i < magicBytes.Length ; i++)
			{
				cancellation.ThrowIfCancellationRequested();
				try
				{
					var read = socket.Receive(bytes);
					if(read != 1)
						i--;
					if(magicBytes[i] != bytes[0])
						i = -1;
				}
				catch(SocketException ex)
				{
					if(ex.SocketErrorCode == SocketError.TimedOut && socket.Connected)
					{
						i--;
					}
					else
						throw;
				}
			}

		}


	}
}
