using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if !NOSOCKET
using System.Net.Sockets;
#endif
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
			if(stream.Serializing || (!stream.Serializing && !_SkipMagic))
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
				NodeServerTrace.Trace.TraceEvent(TraceEventType.Verbose, 0, "Message type readen : " + Command);
				if(length > 0x02000000) //MAX_SIZE 0x02000000 Serialize.h
				{
					throw new FormatException("Message payload too big ( > 0x02000000 bytes)");
				}
				payload = new byte[length];
				stream.ReadWrite(ref payload);

				if(verifyChechksum)
				{
					if(!VerifyChecksum())
					{
						NodeServerTrace.Trace.TraceEvent(TraceEventType.Verbose, 0, "Invalid message checksum bytes : "
															+ Encoders.Hex.EncodeData(this.ToBytes()));
						throw new FormatException("Message checksum invalid");
					}
				}
				BitcoinStream payloadStream = new BitcoinStream(payload);
				payloadStream.CopyParameters(stream);

				var payloadType = PayloadAttribute.GetCommandType(Command);
				if(payloadType == typeof(UnknowPayload))
					NodeServerTrace.Trace.TraceEvent(TraceEventType.Warning, 0, "Unknown command received : " + Command);
				payloadStream.ReadWrite(payloadType, ref _PayloadObject);
				NodeServerTrace.Verbose("Payload : " + _PayloadObject);
			}
		}

		#endregion

		public bool VerifyChecksum()
		{
			return Checksum == Hashes.Hash256(payload).GetLow32();
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
		bool _SkipMagic;

		public override string ToString()
		{
			return Command + " : " + Payload;
		}

#if !NOSOCKET
		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken)
		{
			PerformanceCounter counter;
			return ReadNext(socket, network, version, cancellationToken, out counter);
		}

		internal class CustomNetworkStream : NetworkStream
		{
			public CustomNetworkStream(Socket socket, bool own)
				: base(socket, own)
			{

			}

			public bool Connected
			{
				get
				{
					return Socket.Connected;
				}
			}
		}
		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken, out PerformanceCounter counter)
		{
			var stream = new CustomNetworkStream(socket, false);
			BitcoinStream bitStream = new BitcoinStream(stream, false)
			{
				ProtocolVersion = version,
				ReadCancellationToken = cancellationToken
			};

			network.ReadMagic(stream, cancellationToken);

			Message message = new Message();
			using(message.SkipMagicScope(true))
			{
				message.Magic = network.Magic;
				message.ReadWrite(bitStream);
			}
			counter = bitStream.Counter;
			return message;
		}
#endif
		private IDisposable SkipMagicScope(bool value)
		{
			var old = _SkipMagic;
			return new Scope(() => _SkipMagic = value, () => _SkipMagic = old);
		}

	}
}
