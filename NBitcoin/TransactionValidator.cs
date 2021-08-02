#nullable enable
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TransactionValidator
	{
		internal TransactionValidator(Transaction transaction, TxOut[] spentOutputs)
		{
			PrecomputedTransactionData = transaction.PrecomputeTransactionData(spentOutputs);
			SpentOutputs = spentOutputs;
			Transaction = transaction;
		}

		public Transaction Transaction { get; }
		public TxOut[] SpentOutputs { get; }

		public PrecomputedTransactionData PrecomputedTransactionData { get; }

		public ScriptVerify ScriptVerify { get; set; } = ScriptVerify.Standard;

		public bool TryValidateInput(int index, out InputValidationResult result)
		{
			result = ValidateInput(index);
			return result.Error is null;
		}
		public InputValidationResult ValidateInput(int index)
		{
			if (index < 0 || index >= SpentOutputs.Length)
				throw new ArgumentOutOfRangeException(nameof(index));
			ScriptEvaluationContext ctx = new ScriptEvaluationContext()
			{
				ScriptVerify = ScriptVerify
			};

			if (Transaction is IHasForkId)
				ctx.ScriptVerify |= NBitcoin.ScriptVerify.ForkId;

			var scriptSig = Transaction.Inputs[index].ScriptSig;
			var txout = this.SpentOutputs[index];

			var ok = ctx.VerifyScript(scriptSig, txout.ScriptPubKey, new TransactionChecker(Transaction, index, txout, PrecomputedTransactionData));
			if (!ok)
				return new InputValidationResult(index, ctx.Error, ctx.ExecutionData);
			return new InputValidationResult(index, ctx.ExecutionData);
		}
		public InputValidationResult[] ValidateInputs()
		{
			var inputsResult = new InputValidationResult[Transaction.Inputs.Count];
			for (int i = 0; i < Transaction.Inputs.Count; i++)
			{
				inputsResult[i] = ValidateInput(i);
			}
			return inputsResult;
		}
	}

	public class InputValidationResult
	{
		internal InputValidationResult(int index, ScriptError error, ExecutionData executionData)
		{
			InputIndex = index;
			Error = error;
			ExecutionData = executionData;
		}
		internal InputValidationResult(int index, ExecutionData executionData)
		{
			InputIndex = index;
			ExecutionData = executionData;
		}

		public int InputIndex { get; }
		public ScriptError? Error { get; }
		public ExecutionData ExecutionData { get; }
	}
}
