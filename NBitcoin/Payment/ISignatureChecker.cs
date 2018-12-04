using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	[Obsolete("BIP70 is obsolete")]
	public interface ISignatureChecker
	{
		bool VerifySignature(byte[] certificate, byte[] hash, string hashOID, byte[] signature);
	}
}
