#if !NOJSONNET
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	class SatoshiFormatter : RawFormatter
	{
		protected override void BuildTransaction(JObject json, Transaction tx)
		{
			tx.Version = (uint)json.GetValue("version");
			tx.LockTime = (uint)json.GetValue("locktime");

			var vin = (JArray)json.GetValue("vin");
			for (int i = 0; i < vin.Count; i++)
			{
				var jsonIn = (JObject)vin[i];
				var txin = new TxIn();
				tx.Inputs.Add(txin);

				var script = (JObject)jsonIn.GetValue("scriptSig");
				if (script != null)
				{
					txin.ScriptSig = new Script(Encoders.Hex.DecodeData((string)script.GetValue("hex")));
					txin.PrevOut.Hash = uint256.Parse((string)jsonIn.GetValue("txid"));
					txin.PrevOut.N = (uint)jsonIn.GetValue("vout");
				}
				else
				{
					var coinbase = (string)jsonIn.GetValue("coinbase");
					txin.ScriptSig = new Script(Encoders.Hex.DecodeData(coinbase));
				}

				txin.Sequence = (uint)jsonIn.GetValue("sequence");

			}

			var vout = (JArray)json.GetValue("vout");
			for (int i = 0; i < vout.Count; i++)
			{
				var jsonOut = (JObject)vout[i];
				var txout = new TxOut();
				tx.Outputs.Add(txout);

				var btc = (decimal)jsonOut.GetValue("value");
				var satoshis = btc * Money.COIN;
				txout.Value = new Money((long)(satoshis));

				var script = (JObject)jsonOut.GetValue("scriptPubKey");
				txout.ScriptPubKey = new Script(Encoders.Hex.DecodeData((string)script.GetValue("hex")));
			}
		}

		protected override void WriteTransaction(JsonTextWriter writer, Transaction tx)
		{
			WritePropertyValue(writer, "txid", tx.GetHash().ToString());
			WritePropertyValue(writer, "version", tx.Version);
			WritePropertyValue(writer, "locktime", tx.LockTime.Value);

			writer.WritePropertyName("vin");
			writer.WriteStartArray();
			foreach (var txin in tx.Inputs)
			{
				writer.WriteStartObject();

				if (txin.PrevOut.Hash == uint256.Zero)
				{
					WritePropertyValue(writer, "coinbase", Encoders.Hex.EncodeData(txin.ScriptSig.ToBytes()));
				}
				else
				{
					WritePropertyValue(writer, "txid", txin.PrevOut.Hash.ToString());
					WritePropertyValue(writer, "vout", txin.PrevOut.N);
					writer.WritePropertyName("scriptSig");
					writer.WriteStartObject();

					WritePropertyValue(writer, "asm", txin.ScriptSig.ToString());
					WritePropertyValue(writer, "hex", Encoders.Hex.EncodeData(txin.ScriptSig.ToBytes()));

					writer.WriteEndObject();
				}
				WritePropertyValue(writer, "sequence", (uint)txin.Sequence);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();

			writer.WritePropertyName("vout");
			writer.WriteStartArray();

			int i = 0;
			foreach (var txout in tx.Outputs)
			{
				writer.WriteStartObject();
				writer.WritePropertyName("value");
				//writer.WriteRawValue(ValueFromAmount(txout.Value));--->not use ValueFromAmount
				writer.WriteRawValue(txout.Value.ToString(false,false));
				WritePropertyValue(writer, "n", i);

				writer.WritePropertyName("scriptPubKey");
				writer.WriteStartObject();

				WritePropertyValue(writer, "asm", txout.ScriptPubKey.ToString());
				WritePropertyValue(writer, "hex", Encoders.Hex.EncodeData(txout.ScriptPubKey.ToBytes()));

				var destinations = new List<TxDestination>() { txout.ScriptPubKey.GetDestination() };
				if (destinations[0] == null)
				{
					destinations = txout.ScriptPubKey.GetDestinationPublicKeys()
														.Select(p => p.Hash)
														.ToList<TxDestination>();
				}
				if (destinations.Count == 1)
				{
					WritePropertyValue(writer, "reqSigs", 1);
					WritePropertyValue(writer, "type", GetScriptType(txout.ScriptPubKey.FindTemplate()));
					writer.WritePropertyName("addresses");
					writer.WriteStartArray();
					writer.WriteValue(destinations[0].GetAddress(Network).ToString());
					writer.WriteEndArray();
				}
				else
				{
					var multi = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey);
					if (multi != null)
						WritePropertyValue(writer, "reqSigs", multi.SignatureCount);
					WritePropertyValue(writer, "type", GetScriptType(txout.ScriptPubKey.FindTemplate()));
					if (multi != null)
					{
						writer.WritePropertyName("addresses");
						writer.WriteStartArray();
						foreach (var key in multi.PubKeys)
						{
							writer.WriteValue(key.Hash.GetAddress(Network).ToString());
						}
						writer.WriteEndArray();
					}
				}

				writer.WriteEndObject(); //endscript
				writer.WriteEndObject(); //in out
				i++;
			}
			writer.WriteEndArray();
		}

		//err:if money is 0.0001(double code is 1E-04) btc,the result is "1E-04.0".this result can't jsonformart to double or decimal
		private string ValueFromAmount(Money money)
		{
			var satoshis = (decimal)money.Satoshi;
			var btc = satoshis / Money.COIN;
			//return btc.ToString("0.###E+00", CultureInfo.InvariantCulture);
			var result = ((double)btc).ToString(CultureInfo.InvariantCulture);
			if (!result.ToCharArray().Contains('.'))
				result = result + ".0";
			return result;
		}

		private string GetScriptType(ScriptTemplate template)
		{
			if (template == null)
				return "nonstandard";
			switch (template.Type)
			{
				case TxOutType.TX_PUBKEY:
					return "pubkey";
				case TxOutType.TX_PUBKEYHASH:
					return "pubkeyhash";
				case TxOutType.TX_SCRIPTHASH:
					return "scripthash";
				case TxOutType.TX_MULTISIG:
					return "multisig";
				case TxOutType.TX_NULL_DATA:
					return "nulldata";
			}
			return "nonstandard";
		}
	}
}
#endif
