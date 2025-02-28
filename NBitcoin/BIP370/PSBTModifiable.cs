using System;

namespace NBitcoin.BIP370;

[Flags]
public enum PSBTModifiable : byte
{
	InputsModifiable = 0b_0000_0001,
	OutputsModifiable = 0b_0000_0010,
	HasSigHashSingle = 0b_0000_0100,
}
