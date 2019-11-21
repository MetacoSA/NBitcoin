namespace NBitcoin.Scripting.Miniscript
{
	internal static class Utils
	{
		internal static int ScriptNumSize(int n) => ScriptNumSize((ulong)n);
		internal static int ScriptNumSize(ulong n)
		{
			if (n <= 0x10) // OP_n
				return 1;
			if (n < 0x80) // OP_PUSH1 <n>
				return 2;
			if (n < 0x8000) // OP_PUSH2 <n>
				return 3;
			if (n < 0x800000) // OP_PUSH3 <n>
				return 4;
			if (n < 0x80000000) // OP_PUSH4 <n>
				return 5;

			return 6; // OP_PUSH5 <n>
		}
	}
}
