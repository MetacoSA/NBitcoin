﻿using NBitcoin.Crypto;
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

		/// <summary>
		/// Check if valid transaction signature
		/// </summary>
		/// <param name="sig">Signature in bytes</param>
		/// <param name="scriptVerify">Verification rules</param>
		/// <returns>True if valid</returns>
		public static bool IsValid(byte[] sig, ScriptVerify scriptVerify = ScriptVerify.DerSig | ScriptVerify.StrictEnc)
		{
			ScriptError error;
			return IsValid(sig, scriptVerify, out error);
		}


		/// <summary>
		/// Check if valid transaction signature
		/// </summary>
		/// <param name="sig">The signature</param>
		/// <param name="scriptVerify">Verification rules</param>
		/// <param name="error">Error</param>
		/// <returns>True if valid</returns>
		public static bool IsValid(byte[] sig, ScriptVerify scriptVerify, out ScriptError error)
		{
			if(sig == null)
				throw new ArgumentNullException("sig");
			if(sig.Length == 0)
			{
				error = ScriptError.SigDer;
				return false;
			}
			error = ScriptError.OK;
			var ctx = new ScriptEvaluationContext()
			{
				ScriptVerify = scriptVerify
			};
			if(!ctx.CheckSignatureEncoding(sig))
			{
				error = ctx.Error;
				return false;
			}
			return true;
		}
		public TransactionSignature(ECDSASignature signature, SigHash sigHash)
		{
			if(sigHash == SigHash.Undefined)
				throw new ArgumentException("sigHash should not be Undefined");
			_SigHash = sigHash;
			_Signature = signature;
		}
		public TransactionSignature(ECDSASignature signature)
			: this(signature, SigHash.All)
		{

		}
		public TransactionSignature(byte[] sigSigHash)
		{
			_Signature = ECDSASignature.FromDER(sigSigHash);
			_SigHash = (SigHash)sigSigHash[sigSigHash.Length - 1];
		}
		public TransactionSignature(byte[] sig, SigHash sigHash)
		{
			_Signature = ECDSASignature.FromDER(sig);
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

		public bool Check(PubKey pubKey, Script scriptPubKey, IndexedTxIn txIn, ScriptVerify verify = ScriptVerify.Standard)
		{
			return Check(pubKey, scriptPubKey, txIn.Transaction, txIn.Index, verify);
		}

		public bool Check(PubKey pubKey, Script scriptPubKey, Transaction tx, uint nIndex, ScriptVerify verify = ScriptVerify.Standard)
		{
			return new ScriptEvaluationContext()
			{
				ScriptVerify = verify,
				SigHash = SigHash
			}.CheckSig(this, pubKey, scriptPubKey, tx, nIndex);
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

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(ToBytes());
		}

		public bool IsLowS
		{
			get
			{
				return Signature.IsLowS;
			}
		}


		/// <summary>
		/// Enforce LowS on the signature
		/// </summary>
		public TransactionSignature MakeCanonical()
		{
			if(IsLowS)
				return this;
			return new TransactionSignature(Signature.MakeCanonical(), SigHash);
		}
	}
}
