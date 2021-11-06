#nullable enable
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
			MinRelayTxFee = new FeeRate(Money.Satoshis(1000), 1000);
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
		public FeeRate? MaxTxFee
		{
			get;
			set;
		}
		public FeeRate? MinRelayTxFee
		{
			get;
			set;
		}
		public Money? MinFee { get; set; }

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
			if (spentCoins == null)
				throw new ArgumentNullException(nameof(spentCoins));
			var validator = transaction.CreateValidator(spentCoins.ToArray());
			if (ScriptVerify is NBitcoin.ScriptVerify v)
				validator.ScriptVerify = v;
			return Check(validator);
		}
		public TransactionPolicyError[] Check(TransactionValidator validator)
		{
			if (validator == null)
				throw new ArgumentNullException(nameof(validator));
			var transaction = validator.Transaction;
			List<TransactionPolicyError> errors = new List<TransactionPolicyError>();
			foreach (var input in validator.Transaction.Inputs.AsIndexedInputs())
			{
				if (this.ScriptVerify is NBitcoin.ScriptVerify)
				{
					ScriptError? error;
					if (!VerifyScript(validator, (int)input.Index, out error) && error is ScriptError err)
					{
						errors.Add(new ScriptPolicyError(input, err, validator.ScriptVerify, validator.SpentOutputs[input.Index].ScriptPubKey));
					}
				}
				var txin = input.TxIn;
				if (txin.ScriptSig.Length > MaxScriptSigLength)
				{
					errors.Add(new InputPolicyError("Max scriptSig length exceeded actual is " + txin.ScriptSig.Length + ", max is " + MaxScriptSigLength, input));
				}
				if (!txin.ScriptSig.IsPushOnly)
				{
					errors.Add(new InputPolicyError("All operation should be push", input));
				}
				if (!txin.ScriptSig.HasCanonicalPushes)
				{
					errors.Add(new InputPolicyError("All operation should be canonical push", input));
				}
			}

			if (CheckScriptPubKey)
			{
				foreach (var txout in transaction.Outputs.AsCoins())
				{
					if (!Strategy.IsStandardOutput(txout.TxOut))
						errors.Add(new OutputPolicyError("Non-Standard scriptPubKey", (int)txout.Outpoint.N));
				}
			}

			int txSize = transaction.GetSerializedSize();
			if (MaxTransactionSize != null)
			{
				if (txSize >= MaxTransactionSize.Value)
					errors.Add(new TransactionSizePolicyError(txSize, MaxTransactionSize.Value));
			}

			var fees = transaction.GetFee(validator.SpentOutputs);
			if (fees != null)
			{
				var virtualSize = transaction.GetVirtualSize();
				if (CheckFee)
				{
					if (MaxTxFee != null)
					{
						var max = MaxTxFee.GetFee(virtualSize);
						if (fees > max)
							errors.Add(new FeeTooHighPolicyError(fees, max));
					}

					if (MinFee != null)
					{
						if (fees < MinFee)
							errors.Add(new FeeTooLowPolicyError(fees, MinFee));
					}

					if (MinRelayTxFee != null)
					{
						if (MinRelayTxFee != null)
						{
							var min = MinRelayTxFee.GetFee(virtualSize);
							if (fees < min)
								errors.Add(new FeeTooLowPolicyError(fees, min));
						}
					}
				}
			}
			if (CheckDust)
			{
				foreach (var output in transaction.Outputs)
				{
					var bytes = output.ScriptPubKey.ToBytes(true);
					if (output.IsDust() && !IsOpReturn(bytes))
						errors.Add(new DustPolicyError(output.Value, output.GetDustThreshold()));
				}
			}
			var opReturnCount = transaction.Outputs.Select(o => o.ScriptPubKey.ToBytes(true)).Count(b => IsOpReturn(b));
			if (opReturnCount > 1)
				errors.Add(new TransactionPolicyError("More than one op return detected"));
			return errors.ToArray();
		}

		private static bool IsOpReturn(byte[] bytes)
		{
			return bytes.Length > 0 && bytes[0] == (byte)OpcodeType.OP_RETURN;
		}

		private bool VerifyScript(TransactionValidator validator, int inputIndex, out ScriptError? error)
		{

#if !NOCONSENSUSLIB
			if (!UseConsensusLib)
#endif
			{
				var res = validator.ValidateInput(inputIndex);
				if (res.Error is ScriptError err)
				{
					error = err;
					return false;
				}
				error = null;
				return true;
			}
#if !NOCONSENSUSLIB
			else
			{
				var scriptVerify = validator.ScriptVerify;
				if (validator.Transaction is IHasForkId)
					scriptVerify |= (NBitcoin.ScriptVerify)(1U << 16);
				var ok = Script.VerifyScriptConsensus(validator.SpentOutputs[inputIndex].ScriptPubKey, validator.Transaction, (uint)inputIndex, scriptVerify);
				if (!ok)
				{
					if (!validator.TryValidateInput(inputIndex, out var res) && res.Error is ScriptError err)
						error = err;
					else
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

		public StandardTransactionPolicyStrategy Strategy { get; set; } = StandardTransactionPolicyStrategy.Instance;

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
				CheckScriptPubKey = CheckScriptPubKey,
				CheckFee = CheckFee,
				Strategy = Strategy,
				CheckDust = CheckDust
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
		public bool CheckDust { get; set; } = true;
	}

	public class StandardTransactionPolicyStrategy
	{
		public static StandardTransactionPolicyStrategy Instance { get; } = new StandardTransactionPolicyStrategy();

		public virtual bool IsStandardOutput(TxOut txout)
		{
			return StandardScripts.GetTemplateFromScriptPubKey(txout.ScriptPubKey) != null;
		}
	}
}
