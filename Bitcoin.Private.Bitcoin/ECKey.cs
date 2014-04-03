using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	public class ECKey
	{
		ECPrivateKeyParameters _PrivateKey;
		internal void SetSecretBytes(byte[] vch)
		{
			var secp256k1 = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
			_PrivateKey = new ECPrivateKeyParameters(new Org.BouncyCastle.Math.BigInteger(1, vch), new ECDomainParameters(secp256k1.Curve, secp256k1.G, secp256k1.N, secp256k1.H));
		}

		internal void GetPubKey(PubKey pubkey, bool isCompressed)
		{
			ECPoint q = _PrivateKey.Parameters.G.Multiply(_PrivateKey.D);
			var pub = new ECPublicKeyParameters(_PrivateKey.AlgorithmName, q, _PrivateKey.Parameters);
			//Pub key (q) is composed into X and Y, the compressed form only include X, which can derive Y along with 02 or 03 prepent depending on whether Y in even or odd.
			var result = _PrivateKey.Parameters.Curve.CreatePoint(q.X.ToBigInteger(), q.Y.ToBigInteger(), isCompressed).GetEncoded();
			pubkey.Set(result);
		}
	}
}
