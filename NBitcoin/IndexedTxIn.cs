namespace NBitcoin
{
	public class IndexedTxIn
	{
		public TxIn TxIn
		{
			get;
			set;
		}

		/// <summary>
		/// The index of this TxIn in its transaction
		/// </summary>
		public uint Index
		{
			get;
			set;
		}

		public OutPoint PrevOut
		{
			get
			{
				return TxIn.PrevOut;
			}
			set
			{
				TxIn.PrevOut = value;
			}
		}

		public Script ScriptSig
		{
			get
			{
				return TxIn.ScriptSig;
			}
			set
			{
				TxIn.ScriptSig = value;
			}
		}


		public WitScript WitScript
		{
			get
			{
				return TxIn.WitScript;
			}
			set
			{
				TxIn.WitScript = value;
			}
		}
		public Transaction Transaction
		{
			get;
			set;
		}

		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError unused;
			return VerifyScript(scriptPubKey, scriptVerify, out unused);
		}
		public bool VerifyScript(Script scriptPubKey, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int)Index, null, out error);
		}
		public bool VerifyScript(Script scriptPubKey, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int)Index, null, scriptVerify, SigHash.Undefined, out error);
		}
		public bool VerifyScript(Script scriptPubKey, Money value, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(scriptPubKey, Transaction, (int)Index, value, scriptVerify, SigHash.Undefined, out error);
		}

		public bool VerifyScript(ICoin coin, ScriptVerify scriptVerify = ScriptVerify.Standard)
		{
			ScriptError error;
			return VerifyScript(coin, scriptVerify, out error);
		}

		public bool VerifyScript(ICoin coin, ScriptVerify scriptVerify, out ScriptError error)
		{
			return Script.VerifyScript(coin.TxOut.ScriptPubKey, Transaction, (int)Index, coin.TxOut.Value, scriptVerify, SigHash.Undefined, out error);
		}
		public bool VerifyScript(ICoin coin, out ScriptError error)
		{
			return VerifyScript(coin, ScriptVerify.Standard, out error);
		}

		public TransactionSignature Sign(Key key, ICoin coin, SigHash sigHash)
		{
			var hash = GetSignatureHash(coin, sigHash);
			return key.Sign(hash, sigHash);
		}

		public uint256 GetSignatureHash(ICoin coin, SigHash sigHash = SigHash.All)
		{
			return Script.SignatureHash(coin.GetScriptCode(), Transaction, (int)Index, sigHash, coin.TxOut.Value, coin.GetHashVersion());
		}

	}
}
