using System;
using System.IO;
using NBitcoin.Protocol;

namespace NBitcoin
{
	public interface IBitcoinSerializable
	{
		void ReadWrite(BitcoinStream stream);
	}

	public static class BitcoinSerializableExtensions
	{
		public static void ReadWrite(this IBitcoinSerializable serializable, Stream stream, bool serializing, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			BitcoinStream s = new BitcoinStream(stream, serializing)
			{
				ProtocolVersion = version
			};
			serializable.ReadWrite(s);
		}
		public static int GetSerializedSize(this IBitcoinSerializable serializable, ProtocolVersion version, SerializationType serializationType)
		{
			BitcoinStream s = new BitcoinStream(Stream.Null, true);
			s.Type = serializationType;
			s.ReadWrite(serializable);
			return (int)s.Counter.WrittenBytes;
		}
		public static int GetSerializedSize(this IBitcoinSerializable serializable, TransactionOptions options)
		{
			var bms = new BitcoinStream(Stream.Null, true);
			bms.TransactionOptions = options;
			serializable.ReadWrite(bms);
			return (int)bms.Counter.WrittenBytes;
		}
		public static int GetSerializedSize(this IBitcoinSerializable serializable, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			return GetSerializedSize(serializable, version, SerializationType.Disk);
		}

		public static string ToHex(this IBitcoinSerializable serializable, SerializationType serializationType = SerializationType.Disk)
		{
			using (var memoryStream = new MemoryStream())
			{
				BitcoinStream bitcoinStream = new BitcoinStream(memoryStream, true);
				bitcoinStream.Type = serializationType;
				bitcoinStream.ReadWrite(serializable);
				memoryStream.Seek(0, SeekOrigin.Begin);
				var bytes = memoryStream.ReadBytes((int)memoryStream.Length);
				return DataEncoders.Encoders.Hex.EncodeData(bytes);
			}
		}

		public static void ReadWrite(this IBitcoinSerializable serializable, byte[] bytes, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			ReadWrite(serializable, new MemoryStream(bytes), false, version);
		}
		public static void FromBytes(this IBitcoinSerializable serializable, byte[] bytes, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			serializable.ReadWrite(new BitcoinStream(bytes)
			{
				ProtocolVersion = version
			});
		}

		public static T Clone<T>(this T serializable, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION) where T : IBitcoinSerializable, new()
		{
			var instance = new T();
			instance.FromBytes(serializable.ToBytes(version), version);
			return instance;
		}
		public static byte[] ToBytes(this IBitcoinSerializable serializable, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			MemoryStream ms = new MemoryStream();
			serializable.ReadWrite(new BitcoinStream(ms, true)
			{
				ProtocolVersion = version
			});
			return ToArrayEfficient(ms);
		}

		public static byte[] ToArrayEfficient(this MemoryStream ms)
		{
#if !(PORTABLE || NETCORE)
			var bytes = ms.GetBuffer();
			Array.Resize(ref bytes, (int)ms.Length);
			return bytes;
#else
			return ms.ToArray();
#endif
		}
	}
}
