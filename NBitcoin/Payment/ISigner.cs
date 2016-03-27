using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	public interface ISigner
	{
		byte[] Sign(byte[] certificate, byte[] hash, string hashOID);
		byte[] StripPrivateKey(byte[] certificate);
	}
}
