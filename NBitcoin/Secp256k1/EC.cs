#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	static class EC
	{
		public static readonly GE G = GE.CONST(
			0x79BE667EU, 0xF9DCBBACU, 0x55A06295U, 0xCE870B07U,
			0x029BFCDBU, 0x2DCE28D9U, 0x59F2815BU, 0x16F81798U,
			0x483ADA77U, 0x26A3C465U, 0x5DA4FBFCU, 0x0E1108A8U,
			0xFD17B448U, 0xA6855419U, 0x9C47D08FU, 0xFB10D4B8U
		);
		public static readonly Scalar N = new Scalar(Scalar.SECP256K1_N_0, Scalar.SECP256K1_N_1, Scalar.SECP256K1_N_2, Scalar.SECP256K1_N_3, Scalar.SECP256K1_N_4, Scalar.SECP256K1_N_5, Scalar.SECP256K1_N_6, Scalar.SECP256K1_N_7);
		public static readonly Scalar NC = new Scalar(Scalar.SECP256K1_N_C_0, Scalar.SECP256K1_N_C_1, Scalar.SECP256K1_N_C_2, Scalar.SECP256K1_N_C_3, Scalar.SECP256K1_N_C_4, 0, 0, 0);
		public const uint CURVE_B = 7;
	}
}
#nullable restore
#endif
