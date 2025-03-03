#nullable enable
using NBitcoin.BIP370;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;

namespace NBitcoin.BIP370;

public class PSBT0 : PSBT
{

	internal PSBT0(Transaction transaction, Network network) : this(CreateMap(transaction, network), network)
	{
		if (transaction == null)
			throw new ArgumentNullException(nameof(transaction));
		tx = transaction.Clone();
		tx.PrecomputeHash(true, true);
	}

	private static Maps CreateMap(Transaction transaction, Network network)
	{
		var maps = new Maps();
		var global = maps.NewMap();
		var noSigTx = transaction.Clone();
		foreach (var input in noSigTx.Inputs)
		{
			input.ScriptSig = Script.Empty;
			input.WitScript = WitScript.Empty;
		}
		global.Add([PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX], noSigTx.ToBytes());
		foreach (var input in transaction.Inputs)
		{
			maps.NewMap();
		}
		foreach (var output in transaction.Outputs)
		{
			maps.NewMap();
		}
		return maps;
	}

	public override PSBT CoinJoin(PSBT other)
	{
		if (other == null)
			throw new ArgumentNullException(nameof(other));
		if (other is not PSBT0)
			throw new ArgumentException("PSBT1 can only coinjoin with PSBT1", nameof(other));

		other.AssertSanity();

		var result = (PSBT0)this.Clone();
		var otx = other.GetGlobalTransaction(false);
		for (int i = 0; i < other.Inputs.Count; i++)
		{
			result.tx.Inputs.Add(otx.Inputs[i]);
			result.Inputs.Add(other.Inputs[i]);
		}
		for (int i = 0; i < other.Outputs.Count; i++)
		{
			result.tx.Outputs.Add(otx.Outputs[i]);
			result.Outputs.Add(other.Outputs[i]);
		}
		return result;
	}

	protected override void WriteCore(JsonTextWriter jsonWriter)
	{
		jsonWriter.WritePropertyName("tx");
		jsonWriter.WriteStartObject();
		RPC.BlockExplorerFormatter.WriteTransaction(jsonWriter, tx);
		jsonWriter.WriteEndObject();
	}

	internal PSBT0(Maps maps, Network network) : base(maps, network, PSBTVersion.PSBTv0)
	{
		if (!maps.Global.TryRemove<byte[]>(PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX, out var txBytes))
			throw new FormatException("Invalid PSBT. No global TX");
		tx = Transaction.Load(txBytes, Network);
		tx.PrecomputeHash(true, true);
		if (tx.Inputs.Any(txin => txin.ScriptSig != Script.Empty || txin.WitScript != WitScript.Empty))
			throw new FormatException("Malformed global tx. It should not contain any scriptsig or witness by itself");
		if (tx.Inputs.Count + tx.Outputs.Count + 1 != maps.Count)
			throw new FormatException("Invalid PSBT. Number of inputs and outputs does not match to the global tx");
		Unknown = maps.Global;
		if (Unknown.Keys.Any(bytes => bytes.Length == 1 && PSBT2Constants.PSBT_V0_GLOBAL_EXCLUSIONSET.Contains(bytes[0])))
			throw new FormatException("Invalid PSBT v0. Contains v2 fields");

		foreach (var indexedInput in tx.Inputs.AsIndexedInputs())
		{
			var map = maps[(int)(indexedInput.Index + 1)];
			Inputs.Add(new PSBT0Input(map, this, indexedInput.Index));
		}
		foreach (var indexedOutput in tx.Outputs.AsIndexedOutputs())
		{
			var index = (int)(1 + Inputs.Count + indexedOutput.N);
			var map = maps[index];
			Outputs.Add(new PSBT0Output(map, this, indexedOutput.N, indexedOutput.TxOut));
		}
		maps.ThrowIfInvalidKeysLeft();
	}

	internal override void FillMap(Map map)
	{
		base.FillMap(map);
		map.Add([PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX], this.GetGlobalTransaction(true).ToBytes());
	}

	readonly Transaction tx;

	internal override Transaction GetGlobalTransaction(bool @unsafe) => @unsafe ? tx : tx.Clone();

	internal class PSBT0Output : PSBTOutput
	{
		public PSBT0Output(PSBT parent, uint index, TxOut txOut) : base(parent, index)
		{
			if (txOut == null)
				throw new ArgumentNullException(nameof(txOut));
			TxOut = txOut;
		}

		public PSBT0Output(Map map, PSBT parent, uint index, TxOut txOut) : base(map, parent, index)
		{
			if (txOut is null)
				throw new ArgumentNullException(nameof(txOut));
			if (map.Keys.Any(bytes => bytes.Length == 1 && PSBT2Constants.PSBT_V0_OUTPUT_EXCLUSIONSET.Contains(bytes[0])))
				throw new FormatException("Invalid PSBT v0. Contains v2 fields");
			TxOut = txOut;
		}

		internal TxOut TxOut { get; }
		public override Script ScriptPubKey
		{
			get => TxOut.ScriptPubKey;
			set
			{
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				TxOut.ScriptPubKey = value;
				((PSBT0)Parent).tx.PrecomputeHash(true, true);
			}
		}
		public override Money Value
		{
			get => TxOut.Value;
			set
			{
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				TxOut.Value = value;
				((PSBT0)Parent).tx.PrecomputeHash(true, true);
			}
		}

		public override TxOut GetTxOut() => TxOut.Clone();
	}
	internal class PSBT0Input : PSBTInput
	{
		internal PSBT0Input(Map map, PSBT0 parent, uint index) : base(map, parent, index)
		{
			if (this.Unknown.Keys.Any(bytes => bytes.Length == 1 && PSBT2Constants.PSBT_V0_INPUT_EXCLUSIONSET.Contains(bytes[0])))
				throw new FormatException("Invalid PSBT v0. Contains v2 fields");
			txIn = parent.tx.Inputs[index];
		}
		TxIn txIn;
		public override OutPoint PrevOut => txIn.PrevOut;

		public override Sequence Sequence
		{
			get => txIn.Sequence;
			set
			{
				txIn.Sequence = value;
				((PSBT0)Parent).tx.PrecomputeHash(true, true);
			}
		}
	}
}
