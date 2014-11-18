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
			var txid = new uint256((string)json.GetValue("txid"));
			tx.Version = (uint)json.GetValue("version");
			tx.LockTime = (uint)json.GetValue("locktime");

			var vin = (JArray)json.GetValue("vin");
			for(int i = 0 ; i < vin.Count ; i++)
			{
				var jsonIn = (JObject)vin[i];
				var txin = new NBitcoin.TxIn();
				tx.Inputs.Add(txin);

				var script = (JObject)jsonIn.GetValue("scriptSig");
				if(script != null)
				{
					txin.ScriptSig = new Script(Encoders.Hex.DecodeData((string)script.GetValue("hex")));
					txin.PrevOut.Hash = new uint256((string)jsonIn.GetValue("txid"));
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
			for(int i = 0 ; i < vout.Count ; i++)
			{
				var jsonOut = (JObject)vout[i];
				var txout = new NBitcoin.TxOut();
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
			foreach(var txin in tx.Inputs)
			{
				writer.WriteStartObject();

				if(txin.PrevOut.Hash == new uint256(0))
				{
					WritePropertyValue(writer, "coinbase", Encoders.Hex.EncodeData(txin.ScriptSig.ToRawScript()));
				}
				else
				{
					WritePropertyValue(writer, "txid", txin.PrevOut.Hash.ToString());
					WritePropertyValue(writer, "vout", txin.PrevOut.N);
					writer.WritePropertyName("scriptSig");
					writer.WriteStartObject();

					WritePropertyValue(writer, "asm", txin.ScriptSig.ToString());
					WritePropertyValue(writer, "hex", Encoders.Hex.EncodeData(txin.ScriptSig.ToRawScript()));

					writer.WriteEndObject();
				}
				WritePropertyValue(writer, "sequence", txin.Sequence);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();

			writer.WritePropertyName("vout");
			writer.WriteStartArray();

			int i = 0;
			foreach(var txout in tx.Outputs)
			{
				writer.WriteStartObject();
				writer.WritePropertyName("value");
				writer.WriteRawValue(ValueFromAmount(txout.Value));
				WritePropertyValue(writer, "n", i);

				writer.WritePropertyName("scriptPubKey");
				writer.WriteStartObject();

				WritePropertyValue(writer, "asm", txout.ScriptPubKey.ToString());
				WritePropertyValue(writer, "hex", Encoders.Hex.EncodeData(txout.ScriptPubKey.ToRawScript()));

				var destinations = new List<TxDestination>() { txout.ScriptPubKey.GetDestination() };
				if(destinations[0] == null)
				{
					destinations = txout.ScriptPubKey.GetDestinationPublicKeys()
														.Select(p => p.ID)
														.ToList<TxDestination>();
				}
				if(destinations.Count == 1)
				{
					WritePropertyValue(writer, "reqSigs", 1);
					WritePropertyValue(writer, "type", GetScriptType(txout.ScriptPubKey.FindTemplate()));
					writer.WritePropertyName("addresses");
					writer.WriteStartArray();
					writer.WriteValue(BitcoinAddress.Create(destinations[0], Network).ToString());
					writer.WriteEndArray();
				}
				else
				{
					var multi = PayToMultiSigTemplate.ExtractScriptPubKeyParameters(txout.ScriptPubKey);
					WritePropertyValue(writer, "reqSigs", multi.SignatureCount);
					WritePropertyValue(writer, "type", GetScriptType(txout.ScriptPubKey.FindTemplate()));
					writer.WriteStartArray();
					foreach(var key in multi.PubKeys)
					{
						writer.WriteValue(BitcoinAddress.Create(key.ID, Network).ToString());
					}
					writer.WriteEndArray();
				}

				writer.WriteEndObject(); //endscript
				writer.WriteEndObject(); //in out
				i++;
			}
			writer.WriteEndArray();
		}

		private string ValueFromAmount(Money money)
		{
			var satoshis = (decimal)money.Satoshi;
			var btc = satoshis / Money.COIN;
			//return btc.ToString("0.###E+00", CultureInfo.InvariantCulture);
			var result = ((double)btc).ToString(CultureInfo.InvariantCulture);
			if(!result.Contains('.'))
				result = result + ".0";
			return result;
		}

		private string GetScriptType(ScriptTemplate template)
		{
			if(template == null)
				return "nonstandard";
			switch(template.Type)
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
