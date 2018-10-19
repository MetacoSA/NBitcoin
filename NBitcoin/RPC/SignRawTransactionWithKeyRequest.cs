using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.RPC
{
	public class SignRawTransactionRequest
	{
		public class PrevTx
		{
			public OutPoint OutPoint { get; set; }
			public Script ScriptPubKey { get; set; }
			/// <summary>
			/// Redeem script (required for P2SH or P2WSH)
			/// </summary>
			public Script RedeemScript { get; set; }
			public Money Amount { get; set; }
		}
		/// <summary>
		/// An json array of previous dependent transaction outputs
		/// </summary>
		public PrevTx[] PreviousTransactions { get; set; }

		/// <summary>
		/// The transaction to sign
		/// </summary>
		public Transaction Transaction { get; set; }

		/// <summary>
		/// The signature hash type
		/// </summary>
		public SigHash? SigHash { get; set; }
	}
	public class SignRawTransactionWithKeyRequest : SignRawTransactionRequest
	{
		/// <summary>
		/// A json array of base58-encoded private keys for signing
		/// </summary>
		public Key[] PrivateKeys { get; set; }
	}

	public class SignRawTransactionResponse
	{
		public class ScriptError
		{
			/// <summary>
			/// The outpoint referenced
			/// </summary>
			public OutPoint OutPoint { get; set; }

			public Script ScriptSig { get; set; }
			public Sequence Sequence { get; set; }
			public string Error { get; set; }

			public override string ToString()
			{
				return Error;
			}
		}
		/// <summary>
		/// The raw transaction with signature
		/// </summary>
		public Transaction SignedTransaction { get; set; }
		/// <summary>
		/// If the transaction has a complete set of signatures
		/// </summary>
		public bool Complete { get; set; }

		/// <summary>
		/// Script verification errors (if there are any)
		/// </summary>
		public ScriptError[] Errors { get; set; } = new ScriptError[0];
	}
}
