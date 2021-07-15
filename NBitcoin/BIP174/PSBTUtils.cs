using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, NBitcoin.RootedKeyPath>;
using HDTaprootKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.TaprootPubKey, NBitcoin.TaprootKeyPath>;

namespace NBitcoin
{
	internal static class PSBTUtils
	{
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
				jsonWriter.WritePropertyValue("master_fingerprint", Encoders.Hex.EncodeData(keypath.Value.RootedKeyPath.MasterFingerprint.ToBytes()));
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
