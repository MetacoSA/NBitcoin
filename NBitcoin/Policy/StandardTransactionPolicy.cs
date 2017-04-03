using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Policy
{
	public class StandardTransactionPolicy : ITransactionPolicy
	{
		public StandardTransactionPolicy()
		{
			ScriptVerify = NBitcoin.ScriptVerify.Standard;
			MaxTransactionSize = 100000;
			MaxTxFee = new FeeRate(Money.Coins(0.1m));
			MinRelayTxFee = new FeeRate(Money.Satoshis(5000));
			CheckFee = true;
			CheckScriptPubKey = true;
		}

		public int? MaxTransactionSize
		{
			get;
			set;
		}
		/// <summary>
		/// Safety check, if the FeeRate exceed this value, a policy error is raised
		/// </summary>
		public FeeRate MaxTxFee
		{
			get;
			set;
		}
		public FeeRate MinRelayTxFee
		{
			get;
			set;
		}

		public ScriptVerify? ScriptVerify
		{
			get;
			set;
		}
		public bool CheckFee
		{
			get;
			set;
		}
#if !NOCONSENSUSLIB
		public bool UseConsensusLib
		{
			get;
			set;
		}
#endif
		public const int MaxScriptSigLength = 1650;
		#region ITransactionPolicy Members

		public TransactionPolicyError[] Check(Transaction transaction, ICoin[] spentCoins)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");

			spentCoins = spentCoins ?? new ICoin[0];

			List<TransactionPolicyError> errors = new List<TransactionPolicyError>();



			foreach(var input in transaction.Inputs.AsIndexedInputs())
			{
				var coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut);
				if(coin != null)
				{
					if(ScriptVerify != null)
					{
						ScriptError error;
						if(!VerifyScript(input, coin.TxOut.ScriptPubKey, coin.TxOut.Value, ScriptVerify.Value, out error))
						{
							errors.Add(new ScriptPolicyError(input, error, ScriptVerify.Value, coin.TxOut.ScriptPubKey));
						}
					}
				}

				var txin = input.TxIn;
				if(txin.ScriptSig.Length > MaxScriptSigLength)
				{
					errors.Add(new InputPolicyError("Max scriptSig length exceeded actual is " + txin.ScriptSig.Length + ", max is " + MaxScriptSigLength, input));
				}
				if(!txin.ScriptSig.IsPushOnly)
				{
					errors.Add(new InputPolicyError("All operation should be push", input));
				}
				if(!txin.ScriptSig.HasCanonicalPushes)
				{
					errors.Add(new InputPolicyError("All operation should be canonical push", input));
				}
			}

			if(CheckScriptPubKey)
			{
				foreach(var txout in transaction.Outputs.AsCoins())
				{
					var template = StandardScripts.GetTemplateFromScriptPubKey(txout.ScriptPubKey);
					if(template == null)
						errors.Add(new OutputPolicyError("Non-Standard scriptPubKey", (int)txout.Outpoint.N));
				}
			}

			int txSize = transaction.GetSerializedSize();
			if(MaxTransactionSize != null)
			{
				if(txSize >= MaxTransactionSize.Value)
					errors.Add(new TransactionSizePolicyError(txSize, MaxTransactionSize.Value));
			}

			var fees = transaction.GetFee(spentCoins);
			if(fees != null)
			{
				if(CheckFee)
				{
					if(MaxTxFee != null)
					{
						var max = MaxTxFee.GetFee(txSize);
						if(fees > max)
							errors.Add(new FeeTooHighPolicyError(fees, max));
					}

					if(MinRelayTxFee != null)
					{
						if(MinRelayTxFee != null)
						{
							var min = MinRelayTxFee.GetFee(txSize);
							if(fees < min)
								errors.Add(new FeeTooLowPolicyError(fees, min));
						}
					}
				}
			}
			if(MinRelayTxFee != null)
			{
				foreach(var output in transaction.Outputs)
				{
					var bytes = output.ScriptPubKey.ToBytes(true);
					if(output.IsDust(MinRelayTxFee) && !IsOpReturn(bytes))
						errors.Add(new DustPolicyError(output.Value, output.GetDustThreshold(MinRelayTxFee)));
				}
			}
			var opReturnCount = transaction.Outputs.Select(o => o.ScriptPubKey.ToBytes(true)).Count(b => IsOpReturn(b));
			if(opReturnCount > 1)
				errors.Add(new TransactionPolicyError("More than one op return detected"));
			return errors.ToArray();
		}

		private static bool IsOpReturn(byte[] bytes)
		{
			return bytes.Length > 0 && bytes[0] == (byte)OpcodeType.OP_RETURN;
		}

		private bool VerifyScript(IndexedTxIn input, Script scriptPubKey, Money value, ScriptVerify scriptVerify, out ScriptError error)
		{
#if !NOCONSENSUSLIB
			if(!UseConsensusLib)
#endif
				return input.VerifyScript(scriptPubKey, value, scriptVerify, out error);
#if !NOCONSENSUSLIB
			else
			{
				var ok = Script.VerifyScriptConsensus(scriptPubKey, input.Transaction, input.Index, scriptVerify);
				if(!ok)
				{
					if(input.VerifyScript(scriptPubKey, scriptVerify, out error))
						error = ScriptError.UnknownError;
					return false;
				}
				else
				{
					error = ScriptError.OK;
				}
				return true;
			}
#endif
		}

		#endregion

		public StandardTransactionPolicy Clone()
		{
			return new StandardTransactionPolicy()
			{
				MaxTransactionSize = MaxTransactionSize,
				MaxTxFee = MaxTxFee,
				MinRelayTxFee = MinRelayTxFee,
				ScriptVerify = ScriptVerify,
#if !NOCONSENSUSLIB
				UseConsensusLib = UseConsensusLib,
#endif
				CheckFee = CheckFee
			};
		}

		/// <summary>
		/// Check the standardness of scriptPubKey
		/// </summary>
		public bool CheckScriptPubKey
		{
			get;
			set;
		}
	}
}
