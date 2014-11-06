using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TransactionSignature
	{
		static readonly TransactionSignature _Empty = new TransactionSignature(new ECDSASignature(NBitcoin.BouncyCastle.Math.BigInteger.ValueOf(0), NBitcoin.BouncyCastle.Math.BigInteger.ValueOf(0)), SigHash.All);
		public static TransactionSignature Empty
		{
			get
			{
				return _Empty;
			}
		}
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
			return (67 <= length && length <= 80) || length == 9; //9 = Empty signature
		}


		string _Id;
		private string Id
		{
			get
			{
				if(_Id == null)
					_Id = Encoders.Hex.EncodeData(ToBytes());
				return _Id;
			}
		}

		public override bool Equals(object obj)
		{
			TransactionSignature item = obj as TransactionSignature;
			if(item == null)
				return false;
			return Id.Equals(item.Id);
		}
		public static bool operator ==(TransactionSignature a, TransactionSignature b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
				return false;
			return a.Id == b.Id;
		}

		public static bool operator !=(TransactionSignature a, TransactionSignature b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}
