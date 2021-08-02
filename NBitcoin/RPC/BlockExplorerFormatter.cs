#if !NOJSONNET
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	static class BlockExplorerFormatter
	{
		public static string ToString(Transaction transaction)
		{
			var strWriter = new StringWriter();
			var jsonWriter = new JsonTextWriter(strWriter);
			jsonWriter.Formatting = Formatting.Indented;
			jsonWriter.WriteStartObject();
			WriteTransaction(jsonWriter, transaction);
			jsonWriter.WriteEndObject();
			jsonWriter.Flush();
			return strWriter.ToString();
		}
		static void WritePropertyValue<TValue>(JsonWriter writer, string name, TValue value)
		{
			writer.WritePropertyName(name);
			writer.WriteValue(value);
		}

		static internal void WriteTransaction(JsonTextWriter writer, Transaction tx)
		{
			WritePropertyValue(writer, "hash", tx.GetHash().ToString());
			WritePropertyValue(writer, "ver", tx.Version);

			WritePropertyValue(writer, "vin_sz", tx.Inputs.Count);
			WritePropertyValue(writer, "vout_sz", tx.Outputs.Count);

			WritePropertyValue(writer, "lock_time", tx.LockTime.Value);

			WritePropertyValue(writer, "size", tx.GetSerializedSize());

			writer.WritePropertyName("in");
			writer.WriteStartArray();
			foreach (var input in tx.Inputs.AsIndexedInputs())
			{
				var txin = input.TxIn;
				writer.WriteStartObject();
				writer.WritePropertyName("prev_out");
				writer.WriteStartObject();
				WritePropertyValue(writer, "hash", txin.PrevOut.Hash.ToString());
				WritePropertyValue(writer, "n", txin.PrevOut.N);
				writer.WriteEndObject();

				if (txin.PrevOut.Hash == uint256.Zero)
				{
					WritePropertyValue(writer, "coinbase", Encoders.Hex.EncodeData(txin.ScriptSig.ToBytes()));
				}
				else
				{
					WritePropertyValue(writer, "scriptSig", txin.ScriptSig.ToString());
				}
				if (input.WitScript != WitScript.Empty)
				{
					WritePropertyValue(writer, "witness", input.WitScript.ToString());
				}
				if (txin.Sequence != uint.MaxValue)
				{
					WritePropertyValue(writer, "sequence", (uint)txin.Sequence);
				}
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WritePropertyName("out");
			writer.WriteStartArray();

			foreach (var txout in tx.Outputs)
			{
				writer.WriteStartObject();
				WritePropertyValue(writer, "value", txout.Value.ToString(false, false));
				WritePropertyValue(writer, "scriptPubKey", txout.ScriptPubKey.ToString());
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}
	}
}
#endif
