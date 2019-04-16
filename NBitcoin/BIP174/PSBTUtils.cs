using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, System.Tuple<NBitcoin.HDFingerprint, NBitcoin.KeyPath>>;

namespace NBitcoin.BIP174
{
	internal static class PSBTUtils
	{
		public static void WritePropertyValue<TValue>(this JsonWriter writer, string name, TValue value)
		{
			writer.WritePropertyName(name);
			writer.WriteValue(value);
		}
		public static void WriteBIP32Derivations(this JsonTextWriter jsonWriter, HDKeyPathKVMap hd_keypaths)
		{
			jsonWriter.WritePropertyName("bip32_derivs");
			jsonWriter.WriteStartArray();
			foreach (var keypath in hd_keypaths)
			{
				jsonWriter.WriteStartObject();
				jsonWriter.WritePropertyValue("pubkey", keypath.Key.ToHex());
				jsonWriter.WritePropertyValue("master_fingerprint", Encoders.Hex.EncodeData(keypath.Value.Item1.ToBytes()));
				jsonWriter.WritePropertyValue("path", keypath.Value.Item2.ToString());
				jsonWriter.WriteEndObject();
			}
			jsonWriter.WriteEndArray();
		}
	}

	internal class PubKeyComparer : IComparer<PubKey>
	{
		public int Compare(PubKey x, PubKey y)
		{
			return BytesComparer.Instance.Compare(x.ToBytes(), y.ToBytes());
		}
	}

	internal class KeyIdComparer : IComparer<KeyId>
	{
		public int Compare(KeyId x, KeyId y)
		{
			return BytesComparer.Instance.Compare(x._DestBytes, y._DestBytes);
		}
	}
}
