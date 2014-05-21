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
	public abstract class RawBase
	{
		public string ToString(bool pretty)
		{
			StringWriter str = new StringWriter();
			JsonTextWriter writer = new JsonTextWriter(str);
			if(pretty)
			{
				writer.Formatting = Formatting.Indented;
			}
			WriteJson(writer);
			writer.Flush();
			return str.ToString();
		}

		internal void WriteJson(JsonWriter writer)
		{
			writer.WriteStartObject();
			WriteJsonCore(writer);
			writer.WriteEnd();
		}

		internal protected abstract void WriteJsonCore(JsonWriter writer);

		protected void WritePropertyValue<TValue>(JsonWriter writer, string name, TValue value)
		{
			writer.WritePropertyName(name);
			writer.WriteValue(value);
		}
		public override string ToString()
		{
			return ToString(true);
		}
	}

	class RawTxIn : RawBase
	{

		public RawTxIn()
		{

		}
		public RawTxIn(JObject json)
		{
			TxIn = new NBitcoin.TxIn();
			var prevout = (JObject)json.GetValue("prev_out");
			TxIn.PrevOut.Hash = new uint256((string)prevout.GetValue("hash"));
			TxIn.PrevOut.N = (uint)prevout.GetValue("n");
			TxIn.ScriptSig = new Script((string)json.GetValue("scriptSig"));

			var seq = json.GetValue("sequence");
			if(seq != null)
			{
				TxIn.Sequence = (uint)seq;
			}
		}

		public RawTxIn(TxIn txin)
		{
			TxIn = txin;
		}
		public TxIn TxIn
		{
			get;
			set;
		}
		internal protected override void WriteJsonCore(JsonWriter writer)
		{
			writer.WritePropertyName("prev_out");
			writer.WriteStartObject();
			WritePropertyValue(writer, "hash", TxIn.PrevOut.Hash.ToString());
			WritePropertyValue(writer, "n", TxIn.PrevOut.N);
			writer.WriteEndObject();

			WritePropertyValue(writer, "scriptSig", TxIn.ScriptSig.ToString());
			if(TxIn.Sequence != uint.MaxValue)
			{
				WritePropertyValue(writer, "sequence", TxIn.Sequence);
			}
		}
	}
	class RawTxOut : RawBase
	{
		public RawTxOut(TxOut txout)
		{
			TxOut = txout;
		}
		public RawTxOut(JObject json)
		{
			TxOut = new TxOut();
			TxOut.Value = Money.Parse((string)json.GetValue("value"));
			TxOut.ScriptPubKey = new Script((string)json.GetValue("scriptPubKey"));
		}

		public TxOut TxOut
		{
			get;
			set;
		}

		protected internal override void WriteJsonCore(JsonWriter writer)
		{
			WritePropertyValue(writer, "value", TxOut.Value.ToString(false, false));
			WritePropertyValue(writer, "scriptPubKey", TxOut.ScriptPubKey.ToString());
		}
	}
	public class RawTransaction : RawBase
	{
		public RawTransaction()
		{

		}
		public RawTransaction(string json)
			: this(JObject.Parse(json))
		{
		}
		public RawTransaction(JObject json)
		{
			Hash = new uint256((string)json.GetValue("hash"));
			Version = (uint)json.GetValue("ver");
			LockTime = (uint)json.GetValue("lock_time");
			Size = (int)json.GetValue("size");


			var vin = (JArray)json.GetValue("in");
			int vinCount = (int)json.GetValue("vin_sz");
			for(int i = 0 ; i < vinCount ; i++)
			{
				_Inputs.Add(new RawTxIn((JObject)vin[i]).TxIn);
			}

			var vout = (JArray)json.GetValue("out");
			int voutCount = (int)json.GetValue("vout_sz");
			for(int i = 0 ; i < voutCount ; i++)
			{
				_Outputs.Add(new RawTxOut((JObject)vout[i]).TxOut);
			}
		}

		public Transaction ToTransaction()
		{
			Transaction tx = new Transaction();
			tx.Version = Version;
			tx.LockTime = LockTime;
			foreach(var v in Inputs)
			{
				tx.Inputs.Add(v.Clone());
			}
			foreach(var v in Outputs)
			{
				TxOut vout = new TxOut();
				vout.Value = v.Value;
				vout.ScriptPubKey = v.ScriptPubKey;
				tx.Outputs.Add(vout);
			}
			return tx;
		}

		protected internal override void WriteJsonCore(JsonWriter writer)
		{
			WritePropertyValue(writer, "hash", Hash.ToString());
			WritePropertyValue(writer, "ver", Version);
			WritePropertyValue(writer, "vin_sz", Inputs.Count);
			WritePropertyValue(writer, "vout_sz", Outputs.Count);
			WritePropertyValue(writer, "lock_time", LockTime);
			WritePropertyValue(writer, "size", Size);

			writer.WritePropertyName("in");
			writer.WriteStartArray();
			foreach(var v in Inputs)
			{
				RawTxIn raw = new RawTxIn(v);
				raw.WriteJson(writer);
			}
			writer.WriteEndArray();
			writer.WritePropertyName("out");
			writer.WriteStartArray();
			foreach(var v in Outputs)
			{
				RawTxOut raw = new RawTxOut(v);
				raw.WriteJson(writer);
			}
			writer.WriteEndArray();
		}

		public uint256 Hash
		{
			get;
			set;
		}
		public uint Version
		{
			get;
			set;
		}
		public uint LockTime
		{
			get;
			set;
		}
		public int Size
		{
			get;
			set;
		}
		private readonly List<TxIn> _Inputs = new List<TxIn>();
		public List<TxIn> Inputs
		{
			get
			{
				return _Inputs;
			}
		}
		private readonly List<TxOut> _Outputs = new List<TxOut>();
		public List<TxOut> Outputs
		{
			get
			{
				return _Outputs;
			}
		}




	}
}
