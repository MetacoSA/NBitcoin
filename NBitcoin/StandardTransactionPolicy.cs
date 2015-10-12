using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class StandardTransactionPolicy : ITransactionPolicy
	{
		public StandardTransactionPolicy()
		{
			ScriptVerify = NBitcoin.ScriptVerify.Standard;
			MaxTransactionSize = 100000;
			MinTxFee = new FeeRate(Money.Satoshis(1000));
			MaxTxFee = new FeeRate(Money.Coins(0.1m));
			MinRelayTxFee = new FeeRate(Money.Satoshis(5000));
		}

		public int? MaxTransactionSize
		{
			get;
			set;
		}

		public FeeRate MaxTxFee
		{
			get;
			set;
		}
		public FeeRate MinTxFee
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

		public bool UseConsensusLib
		{
			get;
			set;
		}

		#region ITransactionPolicy Members

		public TransactionPolicyError[] Check(Transaction transaction, ICoin[] spentCoins)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");

			List<TransactionPolicyError> errors = new List<TransactionPolicyError>();

			if(transaction.Version > Transaction.CURRENT_VERSION || transaction.Version < 1)
			{
				errors.Add(new TransactionPolicyError("Invalid transaction version, expected " + Transaction.CURRENT_VERSION));
			}

			foreach(var input in transaction.Inputs.AsIndexedInputs())
			{
				if(spentCoins != null)
				{
					var coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut);
					if(coin != null)
					{
						if(ScriptVerify != null)
						{
							ScriptError error;
							if(!VerifyScript(input, coin.TxOut.ScriptPubKey, ScriptVerify.Value, out error))
							{
								errors.Add(new ScriptPolicyError(input, error, ScriptVerify.Value, coin.TxOut.ScriptPubKey));
							}
						}
					}
					else
					{
						errors.Add(new CoinNotFoundPolicyError(input));
					}
				}
				var txin = input.TxIn;
				if(txin.ScriptSig.Length > 1650)
				{
					errors.Add(new InputPolicyError("Max scriptSig length exceeded actual is " + txin.ScriptSig.Length + ", max is " + 1650, input));
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

			foreach(var txout in transaction.Outputs.AsCoins())
			{
				var template = StandardScripts.GetTemplateFromScriptPubKey(txout.ScriptPubKey);
				if(template == null)
					errors.Add(new OutputPolicyError("Non-Standard scriptPubKey", (int)txout.Outpoint.N));
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
				if(MaxTxFee != null)
				{
					var max = MaxTxFee.GetFee(txSize);
					if(fees > max)
						errors.Add(new FeeTooHighPolicyError(fees, max));
				}

				var high = new FeeRate(Money.Satoshis(long.MaxValue - 10));
				var minFee = new FeeRate(Money.Satoshis(Math.Max((MinTxFee ?? high).FeePerK.Satoshi, (MinRelayTxFee ?? high).FeePerK.Satoshi)));
				if(minFee != null)
				{
					var min = minFee.GetFee(txSize);
					if(fees < min)
						errors.Add(new FeeTooLowPolicyError(fees, min));
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

		private bool VerifyScript(IndexedTxIn input, Script scriptPubKey, ScriptVerify scriptVerify, out ScriptError error)
		{
			if(!UseConsensusLib)
				return input.VerifyScript(scriptPubKey, scriptVerify, out error);
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
		}

		#endregion
	}
}
