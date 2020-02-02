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
		public Money MinFee { get; set; }

		public ScriptVerify? ScriptVerify
		{
			get;
			set;
		}
		/// <summary>
		/// Check if the transaction is safe from malleability (default: false)
		/// </summary>
		public bool CheckMalleabilitySafe
		{
			get; set;
		} = false;
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
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));

			spentCoins = spentCoins ?? new ICoin[0];

			List<TransactionPolicyError> errors = new List<TransactionPolicyError>();



			foreach (var input in transaction.Inputs.AsIndexedInputs())
			{
				var coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut);
				if (coin != null)
				{
					if (ScriptVerify != null)
					{
						ScriptError error;
						if (!VerifyScript(input, coin.TxOut, ScriptVerify.Value, out error))
						{
							errors.Add(new ScriptPolicyError(input, error, ScriptVerify.Value, coin.TxOut.ScriptPubKey));
						}
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

			if (CheckMalleabilitySafe)
			{
				foreach (var input in transaction.Inputs.AsIndexedInputs())
				{
					var coin = spentCoins.FirstOrDefault(s => s.Outpoint == input.PrevOut);
					if (coin != null && coin.GetHashVersion() != HashVersion.Witness)
						errors.Add(new InputPolicyError("Malleable input detected", input));
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

			var fees = transaction.GetFee(spentCoins);
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
			if (MinRelayTxFee != null)
			{
				foreach (var output in transaction.Outputs)
				{
					var bytes = output.ScriptPubKey.ToBytes(true);
					if (output.IsDust(MinRelayTxFee) && !IsOpReturn(bytes))
						errors.Add(new DustPolicyError(output.Value, output.GetDustThreshold(MinRelayTxFee)));
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

		private bool VerifyScript(IndexedTxIn input, TxOut spentOutput, ScriptVerify scriptVerify, out ScriptError error)
		{

#if !NOCONSENSUSLIB
			if (!UseConsensusLib)
#endif
			{
				if (input.Transaction is IHasForkId)
					scriptVerify |= NBitcoin.ScriptVerify.ForkId;
				return input.VerifyScript(spentOutput, scriptVerify, out error);
			}
#if !NOCONSENSUSLIB
			else
			{
				if (input.Transaction is IHasForkId)
					scriptVerify |= (NBitcoin.ScriptVerify)(1U << 16);
				var ok = Script.VerifyScriptConsensus(spentOutput.ScriptPubKey, input.Transaction, input.Index, scriptVerify);
				if (!ok)
				{
					if (input.VerifyScript(spentOutput, scriptVerify, out error))
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
				CheckMalleabilitySafe = CheckMalleabilitySafe,
				CheckScriptPubKey = CheckScriptPubKey,
				CheckFee = CheckFee,
				Strategy = Strategy
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

	public class StandardTransactionPolicyStrategy
	{
		public static StandardTransactionPolicyStrategy Instance { get; } = new StandardTransactionPolicyStrategy();

		public virtual bool IsStandardOutput(TxOut txout)
		{
			return StandardScripts.GetTemplateFromScriptPubKey(txout.ScriptPubKey) != null;
		}
	}
}
