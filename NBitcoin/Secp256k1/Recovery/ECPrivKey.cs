#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Secp256k1
{

	
#if SECP256K1_LIB
	public
#endif
	partial class ECPrivKey
	{
		public bool TrySignRecoverable(ReadOnlySpan<byte> msg32, out SecpRecoverableECDSASignature? recoverableSignature)
		{
			return TrySignRecoverable(msg32, null, out recoverableSignature);
		}
		public bool TrySignRecoverable(ReadOnlySpan<byte> msg32, INonceFunction? nonceFunction, out SecpRecoverableECDSASignature? recoverableSignature)
		{
			if (msg32.Length != 32)
				throw new ArgumentException(paramName: nameof(msg32), message: "msg32 should be 32 bytes");
			if (this.TrySignECDSA(msg32, nonceFunction, out int recid, out SecpECDSASignature? sig) && sig is SecpECDSASignature)
			{
				recoverableSignature = new SecpRecoverableECDSASignature(sig, recid);
				return true;
			}
			recoverableSignature = null;
			return false;
		}
	}
}
#nullable disable
#endif
