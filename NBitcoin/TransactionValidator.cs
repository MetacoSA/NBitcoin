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

		public TransactionValidator ChangeScriptVerify(ScriptVerify scriptVerify)
		{
			return new TransactionValidator(Transaction, this) { ScriptVerify = scriptVerify };
		}

		private TransactionValidator(Transaction transaction, TransactionValidator validator)
		{
			if (transaction.Inputs.Count != validator.Transaction.Inputs.Count)
				throw new InvalidOperationException("You can't change the inputs of a transaction");
			for (int i = 0; i < transaction.Inputs.Count; i++)
			{
				var a = transaction.Inputs[i];
				var b = validator.Transaction.Inputs[i];
				if (a.PrevOut != b.PrevOut)
					throw new InvalidOperationException("You can't change the input outpoint of a transaction");
				if (a.Sequence != b.Sequence)
					throw new InvalidOperationException("You can't change the input sequence of a transaction");
			}
			Transaction = transaction;
			SpentOutputs = validator.SpentOutputs;
			PrecomputedTransactionData = validator.PrecomputedTransactionData;
			ScriptVerify = validator.ScriptVerify;
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

		public TransactionValidator ChangeTransaction(Transaction transaction)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			return new TransactionValidator(transaction, this);
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
