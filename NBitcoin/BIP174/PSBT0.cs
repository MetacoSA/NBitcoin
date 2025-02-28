#nullable enable
using NBitcoin.BIP370;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;

namespace NBitcoin.BIP370;

public class PSBT0 : PSBT
{

	internal PSBT0(Transaction transaction, Network network) : base(network, PSBTVersion.PSBTv0)
	{
		if (transaction == null)
			throw new ArgumentNullException(nameof(transaction));
		tx = transaction.Clone();
		Inputs = new PSBTInputList();
		Outputs = new PSBTOutputList();
		for (var i = 0; i < tx.Inputs.Count; i++)
			Inputs.Add(CreatePSBTInput((uint)i, tx.Inputs[i]));
		for (var i = 0; i < tx.Outputs.Count; i++)
			Outputs.Add(CreatePSBTOutput((uint)i, tx.Outputs[i]));
		foreach (var input in tx.Inputs)
		{
			input.ScriptSig = Script.Empty;
			input.WitScript = WitScript.Empty;
		}
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

	internal PSBT0(List<Map> maps, Network network) : base(network, PSBTVersion.PSBTv0)
	{
		var globalMap = maps[0];
		byte[]? xpubBytes = null;
		while (globalMap.Pop(out byte[] k, out byte[] v))
		{

			switch (k[0])
			{
				case PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX:
					if (k.Length != 1)
						throw new FormatException("Invalid PSBT. Contains illegal value in key global tx");
					if (tx != null)
						throw new FormatException("Duplicate Key, unsigned tx already provided");
					tx = Transaction.Load(v, Network);
					if (tx.Inputs.Any(txin => txin.ScriptSig != Script.Empty || txin.WitScript != WitScript.Empty))
						throw new FormatException("Malformed global tx. It should not contain any scriptsig or witness by itself");
					break;
				case PSBTConstants.PSBT_GLOBAL_XPUB:
					xpubBytes ??= Network.GetVersionBytes(Base58Type.EXT_PUBLIC_KEY, false);
					if (xpubBytes is null)
						throw new FormatException("Invalid PSBT. No xpub version bytes");
					var (xpub, rootedKeyPath) = ParseXpub(xpubBytes, k, v);
					GlobalXPubs.Add(xpub.GetWif(Network), rootedKeyPath);
					break;
				default:
					if (!Unknown.TryAdd(k, v))
						throw new FormatException($"Invalid PSBT, duplicate key ({Encoders.Hex.EncodeData(k)}) for unknown value");
					break;
			}
		}


		if (tx is null)
			throw new FormatException("Invalid PSBT. No global TX");

		if (tx.Inputs.Count + tx.Outputs.Count + 1 != maps.Count)
			throw new FormatException("Invalid PSBT. Number of inputs and outputs does not match to the global tx");

		foreach (var indexedInput in tx.Inputs.AsIndexedInputs())
		{
			var map = maps[(int)(indexedInput.Index + 1)];
			if (map.Keys.Any(bytes => bytes.Length == 1 && PSBT2Constants.PSBT_V0_INPUT_EXCLUSIONSET.Contains(bytes[0])))
				throw new FormatException("Invalid PSBT v0. Contains v2 fields");
			Inputs.Add(new PSBT0Input(map, this, indexedInput.Index));
		}
		foreach (var indexedOutput in tx.Outputs.AsIndexedOutputs())
		{
			var index = (int)(1 + Inputs.Count + indexedOutput.N);
			var map = maps[index];
			if (map.Keys.Any(bytes => bytes.Length == 1 && PSBT2Constants.PSBT_V0_OUTPUT_EXCLUSIONSET.Contains(bytes[0])))
				throw new FormatException("Invalid PSBT v0. Contains v2 fields");
			Outputs.Add(new PSBTOutput(map, this, indexedOutput.N, indexedOutput.TxOut));
		}
	}

	internal static (ExtPubKey, RootedKeyPath) ParseXpub(byte[] xpubBytes, byte[] k, byte[] v)
	{
		if (xpubBytes is null)
			throw new FormatException("Invalid PSBT. No xpub version bytes");
		var expectedLength = 1 + xpubBytes.Length + 74;
		if (k.Length != expectedLength)
			throw new FormatException("Malformed global xpub.");
		if (!k.Skip(1).Take(xpubBytes.Length).SequenceEqual(xpubBytes))
		{
			throw new FormatException("Malformed global xpub.");
		}
		var xpub = new ExtPubKey(k, 1 + xpubBytes.Length, 74);

		KeyPath path = KeyPath.FromBytes(v.Skip(4).ToArray());
		var rootedKeyPath = new RootedKeyPath(new HDFingerprint(v.Take(4).ToArray()), path);
		return (xpub, rootedKeyPath);
	}

	Transaction tx;

	internal override Transaction GetGlobalTransaction(bool @unsafe) => @unsafe ? tx : tx.Clone();

	class PSBT0Input : PSBTInput
	{
		public PSBT0Input(PSBT0 parent, uint index) : base(parent, index)
		{
			txIn = parent.tx.Inputs[index];
			originalScriptSig = txIn.ScriptSig ?? Script.Empty;
			originalWitScript = txIn.WitScript ?? WitScript.Empty;
		}
		internal PSBT0Input(SortedDictionary<byte[], byte[]> map, PSBT0 parent, uint index) : base(map, parent, index)
		{
			txIn = parent.tx.Inputs[index];
			originalScriptSig = txIn.ScriptSig ?? Script.Empty;
			originalWitScript = txIn.WitScript ?? WitScript.Empty;
		}
		TxIn txIn;
		public override OutPoint PrevOut => txIn.PrevOut;

		protected override void SetSequenceCore(Sequence sequence)
		{
			txIn.Sequence = sequence;
		}
	}
	protected override PSBTInput CreatePSBTInput(uint index, TxIn txIn)
	{
		return new PSBT0Input(this, index);
	}

	protected override PSBTOutput CreatePSBTOutput(uint index, TxOut txOut)
	{
		return new PSBTOutput(this, index, txOut);
	}
}
