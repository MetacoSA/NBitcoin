using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, NBitcoin.RootedKeyPath>;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using HDTaprootKeyPathKVMap =
	System.Collections.Generic.SortedDictionary<NBitcoin.TaprootPubKey, NBitcoin.TaprootKeyPath>;

namespace NBitcoin
{

	public class PSBTSerializer : IBitcoinSerializable
	{
		private Func<(int inputCount, int outputCount)> _expectedInputOutputCount;

		protected PSBTSerializer()
		{

		}
		public PSBTSerializer(Map globalMap, List<Map> inputMap, List<Map> outputMap)
		{
			GlobalMap = globalMap;
			InputMap = inputMap;
			OutputMap = outputMap;
		}

		public PSBTSerializer( Func<(int inputCount, int outputCount)> expectedInputOutputCount)
		{
			_expectedInputOutputCount = expectedInputOutputCount;
			GlobalMap = new Map();
			InputMap = new List<Map>();
			OutputMap = new List<Map>();
		}
		public virtual List<Map> OutputMap { get; private set; }

		public virtual List<Map> InputMap { get; private set; }
		public virtual Map GlobalMap { get; private set; }


		static readonly byte[] PSBT_MAGIC_BYTES = Encoders.ASCII.DecodeData("psbt\xff");

		public void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				stream.ReadWrite(PSBT_MAGIC_BYTES);
				var globalMap = GlobalMap;
				ReadWrite(stream, ref globalMap);
				foreach (var m in InputMap)
				{
					var inputMap = m;
					ReadWrite(stream, ref inputMap);
				}

				foreach (var m in OutputMap)
				{
					var outputMap = m;
					ReadWrite(stream, ref outputMap);
				}
			}
			else
			{
				var magicBytes = stream.Inner.ReadBytes(PSBT_MAGIC_BYTES.Length);
				if (!magicBytes.SequenceEqual(PSBT_MAGIC_BYTES))
				{
					throw new FormatException("Invalid PSBT magic bytes");
				}

				GlobalMap = PSBTUtils.ParseRawMap(stream);
				var expected = _expectedInputOutputCount.Invoke();
				var inMap = new List<Map>();
				for (int i = 0; i < expected.inputCount; i++)
				{
					inMap.Add(PSBTUtils.ParseRawMap(stream));
				}
				InputMap = inMap;
				var outMap = new List<Map>();
				for (int i = 0; i < expected.outputCount; i++)
				{
					outMap.Add(PSBTUtils.ParseRawMap(stream));
				}

				OutputMap = outMap;
			}
		}

		protected virtual void ReadWrite(BitcoinStream stream, ref Map map)
		{
			if (stream.Serializing)
			{
				foreach (var mapItem in map)
				{
					stream.ReadWrite(mapItem.Key);
					stream.ReadWrite(mapItem.Value);
				}
			}
			else
			{
				map = PSBTUtils.ParseRawMap(stream);
			}
		}

	}

	internal static class PSBTUtils
	{
		public static Map ParseRawMap(BitcoinStream data)
		{
			if (data.Serializing)
			{
				throw new InvalidOperationException("This method is for deserialization only");
			}

			var result = new SortedDictionary<byte[], byte[]>(BytesComparer.Instance);

			while (data.Inner.Position != data.Inner.Length)
			{
				var key = Array.Empty<byte>();
				var value = Array.Empty<byte>();
				//peek the next byte
				var next = data.Inner.ReadByte();
				if (next == PSBTConstants.PSBT_SEPARATOR)
				{
					break;
				}

				data.Inner.Position--;
				data.ReadWriteAsVarString(ref key);
				data.ReadWriteAsVarString(ref value);
				result.Add(key, value);
			}

			return result;
		}

		public static void WritePropertyValue<TValue>(this JsonWriter writer, string name, TValue value)
		{
			writer.WritePropertyName(name);
			writer.WriteValue(value);
		}

		public static void WriteBIP32Derivations(this JsonTextWriter jsonWriter, HDTaprootKeyPathKVMap hd_keypaths)
		{
			jsonWriter.WritePropertyName("bip32_taproot_derivs");
			jsonWriter.WriteStartArray();
			foreach (var keypath in hd_keypaths)
			{
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyValue("pubkey", keypath.Key.ToString());
				jsonWriter.WritePropertyValue("master_fingerprint",
					Encoders.Hex.EncodeData(keypath.Value.RootedKeyPath.MasterFingerprint.ToBytes()));
				jsonWriter.WritePropertyValue("path", keypath.Value.RootedKeyPath.KeyPath.ToString());
				jsonWriter.WritePropertyName("leaf_hashes");
				jsonWriter.WriteStartArray();
				foreach (var leaf in keypath.Value.LeafHashes)
				{
					jsonWriter.WriteValue(leaf.ToString());
				}

				jsonWriter.WriteEndArray();
				jsonWriter.WriteEndObject();
			}

			jsonWriter.WriteEndArray();
		}

		public static void WriteBIP32Derivations(this JsonTextWriter jsonWriter, HDKeyPathKVMap hd_keypaths)
		{
			jsonWriter.WritePropertyName("bip32_derivs");
			jsonWriter.WriteStartArray();
			foreach (var keypath in hd_keypaths)
			{
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyValue("pubkey", keypath.Key.ToString());
				jsonWriter.WritePropertyValue("master_fingerprint",
					Encoders.Hex.EncodeData(keypath.Value.MasterFingerprint.ToBytes()));
				jsonWriter.WritePropertyValue("path", keypath.Value.KeyPath.ToString());
				jsonWriter.WriteEndObject();
			}

			jsonWriter.WriteEndArray();
		}
	}

	/// <summary>
	/// Lexicographical comparison of public keys.
	/// Use <see cref="PubKeyComparer.Instance"/> to get an instance of this class."/>
	/// <see cref="IComparable{T}"/> implementation for <see cref="PubKey"/> is using this comparer.
	/// </summary>
	public class PubKeyComparer : IComparer<PubKey>
	{
		PubKeyComparer()
		{
		}

		public static PubKeyComparer Instance { get; } = new PubKeyComparer();

		public int Compare(PubKey x, PubKey y)
		{
#if HAS_SPAN
			Span<byte> xb = stackalloc byte[65];
			x.ToBytes(xb, out var len);
			xb = xb.Slice(0, len);
			Span<byte> yb = stackalloc byte[65];
			y.ToBytes(yb, out len);
			yb = yb.Slice(0, len);
			return BytesComparer.Instance.Compare(xb, yb);
#else
			return BytesComparer.Instance.Compare(x.ToBytes(true), y.ToBytes(true));
#endif
		}
	}
}
