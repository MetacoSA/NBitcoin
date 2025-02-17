using System;
using System.Collections.Generic;

namespace NBitcoin.BIP370;

public class PSBT2Output: PSBTOutput
{
	internal PSBT2Output(SortedDictionary<byte[], byte[]> map, PSBT parent, uint index, TxOut output) : base(map, parent, index, output)
	{
	}

	public override void Serialize(BitcoinStream stream)
	{
		// key
		stream.ReadWriteAsVarInt(ref defaultKeyLen);
		var key = PSBT2Constants.PSBT_OUT_AMOUNT;
		stream.ReadWrite(ref key);

		// value
		var data = BitConverter.GetBytes(TxOut.Value.Satoshi);
		stream.ReadWriteAsVarString(ref data);

		// key
		stream.ReadWriteAsVarInt(ref defaultKeyLen);
		key = PSBT2Constants.PSBT_OUT_SCRIPT;
		stream.ReadWrite(ref key);

		// value
		data = TxOut.ScriptPubKey.ToBytes();
		stream.ReadWriteAsVarString(ref data);

		base.Serialize(stream);
	}
}