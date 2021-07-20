using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Policy
{
	public class TransactionPolicyError
	{
		public TransactionPolicyError()
			: this(null as string)
		{

		}
		string _Message;
		public TransactionPolicyError(string message)
		{
			_Message = message;
		}
		public override string ToString()
		{
			return _Message;
		}
	}

	public class TransactionSizePolicyError : TransactionPolicyError
	{
		public TransactionSizePolicyError(int actualSize, int maximumSize)
			: base("Transaction's size is too high. Actual value is " + actualSize + ", but the maximum is " + maximumSize)
		{
			_ActualSize = actualSize;
			_MaximumSize = maximumSize;
		}
		private readonly int _ActualSize;
		public int ActualSize
		{
			get
			{
				return _ActualSize;
			}
		}
		private readonly int _MaximumSize;
		public int MaximumSize
		{
			get
			{
				return _MaximumSize;
			}
		}
	}
	public class FeeTooHighPolicyError : TransactionPolicyError
	{
		public FeeTooHighPolicyError(Money fees, Money max)
			: base("Fee too high, actual is " + fees.ToString() + ", policy maximum is " + max.ToString())
		{
			_ExpectedMaxFee = max;
			_Fee = fees;
		}

		private readonly Money _Fee;
		public Money Fee
		{
			get
			{
				return _Fee;
			}
		}
		private readonly Money _ExpectedMaxFee;
		public Money ExpectedMaxFee
		{
			get
			{
				return _ExpectedMaxFee;
			}
		}
	}

	public class DustPolicyError : TransactionPolicyError
	{
		public DustPolicyError(Money value, Money dust)
			: base("Dust output detected, output value is " + value.ToString() + ", policy minimum is " + dust.ToString())
		{
			_Value = value;
			_DustThreshold = dust;
		}

		private readonly Money _Value;
		public Money Value
		{
			get
			{
				return _Value;
			}
		}

		private readonly Money _DustThreshold;
		public Money DustThreshold
		{
			get
			{
				return _DustThreshold;
			}
		}
	}

	public class FeeTooLowPolicyError : TransactionPolicyError
	{
		public FeeTooLowPolicyError(Money fees, Money min)
			: base("Fee too low, actual is " + fees.ToString() + ", policy minimum is " + min.ToString())
		{
			_ExpectedMinFee = min;
			_Fee = fees;
		}

		private readonly Money _Fee;
		public Money Fee
		{
			get
			{
				return _Fee;
			}
		}
		private readonly Money _ExpectedMinFee;
		public Money ExpectedMinFee
		{
			get
			{
				return _ExpectedMinFee;
			}
		}
	}

	public class InputPolicyError : TransactionPolicyError
	{
		public InputPolicyError(string message, IndexedTxIn txIn)
			: base(message)
		{
			_OutPoint = txIn.PrevOut;
			_InputIndex = txIn.Index;
		}

		private readonly OutPoint _OutPoint;
		public OutPoint OutPoint
		{
			get
			{
				return _OutPoint;
			}
		}

		private readonly uint _InputIndex;
		public uint InputIndex
		{
			get
			{
				return _InputIndex;
			}
		}
	}

	public class DuplicateInputPolicyError : TransactionPolicyError
	{
		public DuplicateInputPolicyError(IndexedTxIn[] duplicated)
			: base("Duplicate input " + duplicated[0].PrevOut)
		{
			_OutPoint = duplicated[0].PrevOut;
			_InputIndices = duplicated.Select(d => d.Index).ToArray();
		}

		private readonly OutPoint _OutPoint;
		public OutPoint OutPoint
		{
			get
			{
				return _OutPoint;
			}
		}
		private readonly uint[] _InputIndices;
		public uint[] InputIndices
		{
			get
			{
				return _InputIndices;
			}
		}
	}

	public class OutputPolicyError : TransactionPolicyError
	{
		public OutputPolicyError(string message, int outputIndex) :
			base(message)
		{
			_OutputIndex = outputIndex;
		}
		private readonly int _OutputIndex;
		public int OutputIndex
		{
			get
			{
				return _OutputIndex;
			}
		}
	}

	public class ScriptPolicyError : InputPolicyError
	{
		public ScriptPolicyError(IndexedTxIn input, ScriptError error, ScriptVerify scriptVerify, Script scriptPubKey)
			: base("Script error on input " + input.Index + " (" + error + ")", input)
		{
			_ScriptError = error;
			_ScriptVerify = scriptVerify;
			_ScriptPubKey = scriptPubKey;
		}


		private readonly ScriptError _ScriptError;
		public ScriptError ScriptError
		{
			get
			{
				return _ScriptError;
			}
		}

		private readonly ScriptVerify _ScriptVerify;
		public ScriptVerify ScriptVerify
		{
			get
			{
				return _ScriptVerify;
			}
		}

		private readonly Script _ScriptPubKey;
		public Script ScriptPubKey
		{
			get
			{
				return _ScriptPubKey;
			}

		}
	}
	public interface ITransactionPolicy
	{
		/// <summary>
		/// Check if the given transaction violate the policy
		/// </summary>
		/// <param name="transaction">The transaction</param>
		/// <param name="spentCoins">The previous coins</param>
		/// <returns>Policy errors</returns>
		TransactionPolicyError[] Check(Transaction transaction, ICoin[] spentCoins);
	}
}
