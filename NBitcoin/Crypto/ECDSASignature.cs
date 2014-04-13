using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Crypto
{
	public class ECDSASignature
	{
		private readonly BigInteger _R;
		public BigInteger R
		{
			get
			{
				return _R;
			}
		}
		private BigInteger _S;
		public BigInteger S
		{
			get
			{
				return _S;
			}
		}
		public ECDSASignature(BigInteger r, BigInteger s)
		{
			_R = r;
			_S = s;
		}

		public ECDSASignature(BigInteger[] rs)
		{
			_R = rs[0];
			_S = rs[1];
		}

		/**
		* What we get back from the signer are the two components of a signature, r and s. To get a flat byte stream
		* of the type used by Bitcoin we have to encode them using DER encoding, which is just a way to pack the two
		* components into a structure.
		*/
		public byte[] ToDER()
		{
			// Usually 70-72 bytes.
			MemoryStream bos = new MemoryStream(72);
			DerSequenceGenerator seq = new DerSequenceGenerator(bos);
			seq.AddObject(new DerInteger(R));
			seq.AddObject(new DerInteger(S));
			seq.Close();
			return bos.ToArray();

		}

		public static ECDSASignature FromDER(byte[] sig)
		{
			Asn1InputStream decoder = new Asn1InputStream(sig);
			var seq = (DerSequence)decoder.ReadObject();
			return new ECDSASignature(((DerInteger)seq[0]).Value, ((DerInteger)seq[1]).Value);
		}

		public void EnsureCanonical()
		{
			if(this.S.CompareTo(ECKey.HALF_CURVE_ORDER) > 0)
			{
				this._S = ECKey.CreateCurve().N.Subtract(this.S);
			}
		}

		
	}
}
