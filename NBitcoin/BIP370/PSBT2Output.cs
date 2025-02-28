using System;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;

namespace NBitcoin.BIP370;

public class PSBT2Output: PSBTOutput
{

	internal PSBT2Output(Map map, PSBT parent, uint index, TxOut output) : base(map, parent, index, output)
	{
	}

	protected override void SerializeCore(BitcoinStream stream)
	{
		// key
		stream.ReadWriteAsVarInt(ref defaultKeyLen);
		var key = PSBT2Constants.PSBT_OUT_AMOUNT;
		stream.ReadWrite(ref key);

		// value
		var data = Utils.ToBytes((ulong)TxOut.Value.Satoshi, true);
		stream.ReadWriteAsVarString(ref data);

		// key
		stream.ReadWriteAsVarInt(ref defaultKeyLen);
		key = PSBT2Constants.PSBT_OUT_SCRIPT;
		stream.ReadWrite(ref key);

		// value
		data = TxOut.ScriptPubKey.ToBytes();
		stream.ReadWriteAsVarString(ref data);
	}
}
