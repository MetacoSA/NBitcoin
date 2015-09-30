using NBitcoin.BouncyCastle.Crypto;
using NBitcoin.BouncyCastle.Crypto.Parameters;
using NBitcoin.BouncyCastle.Crypto.Signers;
using NBitcoin.BouncyCastle.Security;
using System.Linq;

namespace NBitcoin.Crypto
{
	public class DeterministicECDSA : ECDsaSigner
	{
		private byte[] _buffer = new byte[0];
		private readonly IDigest _digest;

		public DeterministicECDSA()
			: this("SHA-256")
		{
		}

		public DeterministicECDSA(string hashName)
			: base(new HMacDsaKCalculator(DigestUtilities.GetDigest(hashName)))
		{
			_digest = DigestUtilities.GetDigest(hashName);
		}

		public void setPrivateKey(ECPrivateKeyParameters ecKey)
		{
			base.Init(true, ecKey);
		}

		public void update(byte[] buf)
		{
			_buffer = _buffer.Concat(buf).ToArray();
		}

		public byte[] sign()
		{
			var hash = new byte[_digest.GetByteLength()];
			_digest.BlockUpdate(_buffer, 0, _buffer.Length);
			_digest.DoFinal(hash, 0);
			_digest.Reset();
			return signHash(hash);
		}

		public byte[] signHash(byte[] hash)
		{
			return new ECDSASignature(GenerateSignature(hash)).ToDER();
		}
	}
}
