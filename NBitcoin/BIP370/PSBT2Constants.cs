namespace NBitcoin.BIP370;

static class PSBT2Constants
{
	// Global types

	public const byte PSBT_GLOBAL_TX_VERSION = 0x02;
	public const byte PSBT_GLOBAL_FALLBACK_LOCKTIME = 0x03;
	public const byte PSBT_GLOBAL_INPUT_COUNT = 0x04;
	public const byte PSBT_GLOBAL_OUTPUT_COUNT = 0x05;
	public const byte PSBT_GLOBAL_TX_MODIFIABLE = 0x06;

	// Input types
	public const byte PSBT_IN_PREVIOUS_TXID = 0x0e;
	public const byte PSBT_IN_OUTPUT_INDEX = 0x0f;
	public const byte PSBT_IN_SEQUENCE = 0x10;
	public const byte PSBT_IN_REQUIRED_TIME_LOCKTIME = 0x11;
	public const byte PSBT_IN_REQUIRED_HEIGHT_LOCKTIME = 0x12;

	// Output types
	public const byte PSBT_OUT_AMOUNT = 0x03;
	public const byte PSBT_OUT_SCRIPT = 0x04;

	public const uint PSBT2Version = 2;

	public static readonly byte[] PSBT_V0_GLOBAL_EXCLUSIONSET = [PSBT_GLOBAL_TX_VERSION, PSBT_GLOBAL_FALLBACK_LOCKTIME, PSBT_GLOBAL_INPUT_COUNT, PSBT_GLOBAL_OUTPUT_COUNT, PSBT_GLOBAL_TX_MODIFIABLE];
	public static readonly byte[] PSBT_V0_INPUT_EXCLUSIONSET = [PSBT_IN_PREVIOUS_TXID, PSBT_IN_OUTPUT_INDEX, PSBT_IN_SEQUENCE, PSBT_IN_REQUIRED_TIME_LOCKTIME, PSBT_IN_REQUIRED_HEIGHT_LOCKTIME];

	public static readonly byte[] PSBT_V0_OUTPUT_EXCLUSIONSET = [PSBT_OUT_AMOUNT, PSBT_OUT_SCRIPT];
}
