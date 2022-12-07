using System;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif

namespace NBitcoin
{
	public static class TaprootConstants
	{
#if HAS_SPAN
		internal static long VALIDATION_WEIGHT_OFFSET = 50;
		internal const int TAPROOT_CONTROL_BASE_SIZE = 33;
		internal const int TAPROOT_CONTROL_NODE_SIZE = 32;
		internal const int TAPROOT_CONTROL_MAX_NODE_COUNT = 128;
		internal const int TAPROOT_CONTROL_MAX_SIZE = TAPROOT_CONTROL_BASE_SIZE + TAPROOT_CONTROL_NODE_SIZE * TAPROOT_CONTROL_MAX_NODE_COUNT;
		internal const uint TAPROOT_LEAF_MASK = 0xfe;
		internal const uint TAPROOT_LEAF_ANNEX = 0x50;
		public const uint TAPROOT_LEAF_TAPSCRIPT = 0xc0;
#endif
	}
}
