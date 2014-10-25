using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TransactionSignature
	{
		public TransactionSignature(ECDSASignature signature, SigHash sigHash)
		{
			if(sigHash == SigHash.Undefined)
				throw new ArgumentException("sigHash should not be Undefined");
			_SigHash = sigHash;
			_Signature = signature.MakeCanonical();
		}
		public TransactionSignature(ECDSASignature signature)
			: this(signature, SigHash.All)
		{

		}
		public TransactionSignature(byte[] sigSigHash)
		{
			_Signature = ECDSASignature.FromDER(sigSigHash).MakeCanonical();
			_SigHash = (SigHash)sigSigHash[sigSigHash.Length - 1];
		}
		public TransactionSignature(byte[] sig, SigHash sigHash)
		{
			_Signature = ECDSASignature.FromDER(sig).MakeCanonical();
			_SigHash = sigHash;
		}

		private readonly ECDSASignature _Signature;
		public ECDSASignature Signature
		{
			get
			{
				return _Signature;
			}
		}
		private readonly SigHash _SigHash;
		public SigHash SigHash
		{
			get
			{
				return _SigHash;
			}
		}

		public byte[] ToBytes()
		{
			var sig = _Signature.ToDER();
			var result = new byte[sig.Length + 1];
			Array.Copy(sig, 0, result, 0, sig.Length);
			result[result.Length - 1] = (byte)_SigHash;
			return result;
		}

		public static bool ValidLength(int length)
		{
			return 67 <= length && length <= 80;
		}
	}
}
