using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Policy
{
	/// <summary>
	/// Error when not enough funds are present for verifying or building a transaction
	/// </summary>
	public class NotEnoughFundsPolicyError : TransactionPolicyError
	{
		public NotEnoughFundsPolicyError()
		{
		}
		public NotEnoughFundsPolicyError(string message, IMoney missing)
			: base(BuildMessage(message, missing))
		{
			Missing = missing;
		}

		private static string BuildMessage(string message, IMoney missing)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(message);
			if (missing != null)
				builder.Append(" with missing amount " + missing);
			return builder.ToString();
		}
		public NotEnoughFundsPolicyError(string message)
			: base(message)
		{
		}


		/// <summary>
		/// Amount of Money missing
		/// </summary>
		public IMoney Missing
		{
			get;
			private set;
		}

		internal Exception AsException()
		{
			return new NotEnoughFundsException(ToString(), null, Missing);
		}
	}
	public class MinerTransactionPolicy : ITransactionPolicy
	{
		MinerTransactionPolicy()
		{

		}

		readonly static MinerTransactionPolicy _Instance = new MinerTransactionPolicy();
		public static MinerTransactionPolicy Instance
		{
			get
			{
				return _Instance;
			}
		}

		#region ITransactionPolicy Members

		public TransactionPolicyError[] Check(Transaction transaction, ICoin[] spentCoins)
		{
			return Check(transaction.CreateValidator(spentCoins));
		}
		public TransactionPolicyError[] Check(TransactionValidator validator)
		{
			if (validator == null)
				throw new ArgumentNullException(nameof(validator));
			var transaction = validator.Transaction;
			List<TransactionPolicyError> errors = new List<TransactionPolicyError>();

			if (transaction.Version > Transaction.CURRENT_VERSION || transaction.Version < 1)
			{
				errors.Add(new TransactionPolicyError("Invalid transaction version, expected " + Transaction.CURRENT_VERSION));
			}

			var dups = transaction.Inputs.AsIndexedInputs().GroupBy(i => i.PrevOut);
			foreach (var dup in dups)
			{
				var duplicates = dup.ToArray();
				if (duplicates.Length != 1)
					errors.Add(new DuplicateInputPolicyError(duplicates));
			}

			foreach (var output in transaction.Outputs.AsCoins())
			{
				if (output.Amount < Money.Zero)
					errors.Add(new OutputPolicyError("Output value should not be less than zero", (int)output.Outpoint.N));
			}

			var fees = transaction.GetFee(validator.SpentOutputs);
			if (fees != null)
			{
				if (fees < Money.Zero)
					errors.Add(new NotEnoughFundsPolicyError("Not enough funds in this transaction", -fees));
			}

			var check = transaction.Check();
			if (check != TransactionCheckResult.Success)
			{
				errors.Add(new TransactionPolicyError("Context free check of the transaction failed " + check));
			}
			return errors.ToArray();
		}

		#endregion
	}
}
