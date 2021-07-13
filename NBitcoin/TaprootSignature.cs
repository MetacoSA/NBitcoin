using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TaprootSignature
	{
		public TaprootSignature(SchnorrSignature schnorrSignature, SigHash sigHash)
		{
			if (!((byte)sigHash <= 0x03 || ((byte)sigHash >= 0x81 && (byte)sigHash <= 0x83)))
				throw new ArgumentException("Invalid hash_type", nameof(sigHash));
			if (schnorrSignature == null)
				throw new ArgumentNullException(nameof(schnorrSignature));
			SigHash = sigHash;
			SchnorrSignature = schnorrSignature;
		}

		public SigHash SigHash { get; }
		public SchnorrSignature SchnorrSignature { get; }

		public int Length => SigHash is SigHash.Default ? 64 : 65;

		public byte[] ToBytes()
		{
			if (SigHash == SigHash.Default)
			{
				return SchnorrSignature.ToBytes();
			}
			else
			{
				var sig = new byte[65];
				SchnorrSignature.ToBytes().CopyTo(sig, 0);
				sig[64] = (byte)SigHash;
				return sig;
			}
		}
		public override string ToString()
		{
			return base.ToString();
		}
	}
}
