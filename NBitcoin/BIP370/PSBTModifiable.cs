namespace NBitcoin.BIP370;

public enum PSBTModifiable : byte
{
	InputsModifiable = 0,
	OutputsModifiable = 1,
	HasSigHashSingle = 2,
}