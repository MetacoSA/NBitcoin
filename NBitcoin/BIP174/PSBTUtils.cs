using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, NBitcoin.RootedKeyPath>;

namespace NBitcoin
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
				jsonWriter.WritePropertyValue("master_fingerprint", Encoders.Hex.EncodeData(keypath.Value.MasterFingerprint.ToBytes()));
				jsonWriter.WritePropertyValue("path", keypath.Value.KeyPath.ToString());
				jsonWriter.WriteEndObject();
			}
			jsonWriter.WriteEndArray();
		}
	}

	internal class PubKeyComparer : IComparer<PubKey>
	{
		PubKeyComparer()
		{

		}
		public static PubKeyComparer Instance { get; } = new PubKeyComparer();
		public int Compare(PubKey x, PubKey y)
		{
			return BytesComparer.Instance.Compare(x.ToBytes(), y.ToBytes());
		}
	}
}
