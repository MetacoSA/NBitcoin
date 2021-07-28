using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public interface IBitcoinSerializable
	{
		void ReadWrite(BitcoinStream stream);
	}

	public static class BitcoinSerializableExtensions
	{
		public static void ReadWrite(this IBitcoinSerializable serializable, Stream stream, bool serializing, Network network, uint? version = null)
		{
			serializable.ReadWrite(stream, serializing, network?.Consensus?.ConsensusFactory, version);
		}
		public static void ReadWrite(this IBitcoinSerializable serializable, Stream stream, bool serializing, ConsensusFactory consensusFactory, uint? version = null)
		{
			BitcoinStream s = new BitcoinStream(stream, serializing)
			{
				ProtocolVersion = version
			};
			if (consensusFactory != null)
				s.ConsensusFactory = consensusFactory;
			serializable.ReadWrite(s);
		}
		public static int GetSerializedSize(this IBitcoinSerializable serializable, uint? version, SerializationType serializationType)
		{
			BitcoinStream s = new BitcoinStream(Stream.Null, true);
			s.Type = serializationType;
			s.ProtocolVersion = version;
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
		public static int GetSerializedSize(this IBitcoinSerializable serializable, uint? version = null)
		{
			return GetSerializedSize(serializable, version, SerializationType.Disk);
		}

		public static void ReadWrite(this IBitcoinSerializable serializable, byte[] bytes, Network network, uint? version = null)
		{
			ReadWrite(serializable, new MemoryStream(bytes), false, network, version);
		}

		public static void ReadWrite(this IBitcoinSerializable serializable, byte[] bytes, ConsensusFactory consensusFactory, uint? version = null)
		{
			ReadWrite(serializable, new MemoryStream(bytes), false, consensusFactory, version);
		}

		public static void FromBytes(this IBitcoinSerializable serializable, byte[] bytes, uint? version = null)
		{
			serializable.ReadWrite(new BitcoinStream(bytes)
			{
				ProtocolVersion = version
			});
		}

		public static T Clone<T>(this T serializable, uint? version = null) where T : IBitcoinSerializable, new()
		{
			var instance = new T();
			instance.FromBytes(serializable.ToBytes(version), version);
			return instance;
		}
		public static byte[] ToBytes(this IBitcoinSerializable serializable, uint? version = null)
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
#if NO_MEM_BUFFER
			return ms.ToArray();
#else
			var bytes = ms.GetBuffer();
			Array.Resize(ref bytes, (int)ms.Length);
			return bytes;
#endif
		}
	}
}
