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
			private set
			{
				command = Encoders.ASCII.DecodeData(value.Trim().PadRight(12, '\0'));
			}
		}

		internal byte[] _Buffer;
		Payload _PayloadObject;
		public Payload Payload
		{
			get
			{
				return _PayloadObject;
			}
			set
			{
				_PayloadObject = value;
				Command = _PayloadObject.Command;
			}
		}

		public bool IfPayloadIs<TPayload>(Action<TPayload> action) where TPayload : Payload
		{
			var payload = Payload as TPayload;
			if(payload != null)
				action(payload);
			return payload != null;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(Payload == null && stream.Serializing)
				throw new InvalidOperationException("Payload not affected");
			if(stream.Serializing || (!stream.Serializing && !_SkipMagic))
				stream.ReadWrite(ref magic);
			stream.ReadWrite(ref command);
			int length = 0;
			uint checksum = 0;
			bool hasChecksum = false;
			byte[] payloadBytes = stream.Serializing ? GetPayloadBytes(stream.ProtocolVersion, out length) : null;
			length = payloadBytes == null ? 0 : length;
			stream.ReadWrite(ref length);

			if(stream.ProtocolVersion >= ProtocolVersion.MEMPOOL_GD_VERSION)
			{
				if(stream.Serializing)
					checksum = Hashes.Hash256(payloadBytes, 0, length).GetLow32();
				stream.ReadWrite(ref checksum);
				hasChecksum = true;
			}
			if(stream.Serializing)
			{
				stream.ReadWrite(ref payloadBytes, 0, length);
			}
			else
			{
				if(length > 0x02000000) //MAX_SIZE 0x02000000 Serialize.h
				{
					throw new FormatException("Message payload too big ( > 0x02000000 bytes)");
				}

				payloadBytes = _Buffer == null || _Buffer.Length < length ? new byte[length] : _Buffer;
				stream.ReadWrite(ref payloadBytes, 0, length);

				if(hasChecksum)
				{
					if(!VerifyChecksum(checksum, payloadBytes, length))
					{
						if(NodeServerTrace.Trace.Switch.ShouldTrace(TraceEventType.Verbose))
							NodeServerTrace.Trace.TraceEvent(TraceEventType.Verbose, 0, "Invalid message checksum bytes");
						throw new FormatException("Message checksum invalid");
					}
				}
				BitcoinStream payloadStream = new BitcoinStream(payloadBytes);
				payloadStream.CopyParameters(stream);

				var payloadType = PayloadAttribute.GetCommandType(Command);
				var unknown = payloadType == typeof(UnknowPayload);
				if(unknown)
					NodeServerTrace.Trace.TraceEvent(TraceEventType.Warning, 0, "Unknown command received : " + Command);
				object payload = _PayloadObject;
				payloadStream.ReadWrite(payloadType, ref payload);
				if(unknown)
					((UnknowPayload)payload)._Command = Command;
				Payload = (Payload)payload;
			}
		}

		// FIXME: protocolVersion is not used. Is this a defect?
		private byte[] GetPayloadBytes(ProtocolVersion protocolVersion, out int length)
		{
			MemoryStream ms = _Buffer == null ? new MemoryStream() : new MemoryStream(_Buffer);
			Payload.ReadWrite(new BitcoinStream(ms, true));
			length = (int)ms.Position;
			return _Buffer ?? GetBuffer(ms);
		}

		private static byte[] GetBuffer(MemoryStream ms)
		{
#if !(PORTABLE || NETCORE)
			return ms.GetBuffer();
#else
			return ms.ToArray();
#endif
		}

		#endregion

		internal static bool VerifyChecksum(uint256 checksum, byte[] payload, int length)
		{
			return checksum == Hashes.Hash256(payload, 0, length).GetLow32();
		}



		/// <summary>
		/// When parsing, maybe Magic is already parsed
		/// </summary>
		bool _SkipMagic;

		public override string ToString()
		{
			return String.Format("{0} : {1}", Command, Payload);
		}

#if !NOSOCKET
		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken)
		{
			PerformanceCounter counter;
			return ReadNext(socket, network, version, cancellationToken, out counter);
		}

		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken, out PerformanceCounter counter)
		{
			return ReadNext(socket, network, version, cancellationToken, null, out counter);
		}
		public static Message ReadNext(Socket socket, Network network, ProtocolVersion version, CancellationToken cancellationToken, byte[] buffer, out PerformanceCounter counter)
		{
			var stream = new NetworkStream(socket, false);
			return ReadNext(stream, network, version, cancellationToken, buffer, out counter);
		}
#endif
		public static Message ReadNext(Stream stream, Network network, ProtocolVersion version, CancellationToken cancellationToken)
		{
			PerformanceCounter counter;
			return ReadNext(stream, network, version, cancellationToken, out counter);
		}

		public static Message ReadNext(Stream stream, Network network, ProtocolVersion version, CancellationToken cancellationToken, out PerformanceCounter counter)
		{
			return ReadNext(stream, network, version, cancellationToken, null, out counter);
		}
		public static Message ReadNext(Stream stream, Network network, ProtocolVersion version, CancellationToken cancellationToken, byte[] buffer, out PerformanceCounter counter)
		{
			BitcoinStream bitStream = new BitcoinStream(stream, false)
			{
				ProtocolVersion = version,
				ReadCancellationToken = cancellationToken
			};

			if(!network.ReadMagic(stream, cancellationToken, true))
				throw new FormatException("Magic incorrect, the message comes from another network");

			Message message = new Message();
			message._Buffer = buffer;
			using(message.SkipMagicScope(true))
			{
				message.Magic = network.Magic;
				message.ReadWrite(bitStream);
			}
			counter = bitStream.Counter;
			return message;
		}

		private IDisposable SkipMagicScope(bool value)
		{
			var old = _SkipMagic;
			return new Scope(() => _SkipMagic = value, () => _SkipMagic = old);
		}

	}
}
