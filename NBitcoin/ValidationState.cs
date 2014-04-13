using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	[Flags]
	public enum RejectCode : byte
	{
		MALFORMED = 0x01,
		INVALID = 0x10,
		OBSOLETE = 0x11,
		DUPLICATE = 0x12,
		NONSTANDARD = 0x40,
		DUST = 0x41,
		INSUFFICIENTFEE = 0x42,
		CHECKPOINT = 0x43
	}
	public class ValidationState
	{
		static readonly uint MAX_BLOCK_SIZE = 1000000;
		static readonly ulong MAX_MONEY = (ulong)21000000 * (ulong)Money.COIN;
		enum mode_state
		{
			MODE_VALID,   // everything ok
			MODE_INVALID, // network rule violation (DoS value may be set)
			MODE_ERROR,   // run-time error
		};


		mode_state mode;
		int nDoS;
		string strRejectReason;
		RejectCode chRejectCode;
		bool corruptionPossible;

		public ValidationState()
		{
			mode = mode_state.MODE_VALID;
			nDoS = 0;
			corruptionPossible = false;
		}
		public
			bool DoS(int level, bool ret = false,
			 RejectCode chRejectCodeIn = 0, string strRejectReasonIn = "",
			 bool corruptionIn = false)
		{
			chRejectCode = chRejectCodeIn;
			strRejectReason = strRejectReasonIn;
			corruptionPossible = corruptionIn;
			if(mode == mode_state.MODE_ERROR)
				return ret;
			nDoS += level;
			mode = mode_state.MODE_INVALID;
			return ret;
		}

		public bool Invalid(bool ret = false,
				 RejectCode _chRejectCode = 0, string _strRejectReason = "")
		{
			return DoS(0, ret, _chRejectCode, _strRejectReason);
		}
		public bool Error(string strRejectReasonIn = "")
		{
			if(mode == mode_state.MODE_VALID)
				strRejectReason = strRejectReasonIn;
			mode = mode_state.MODE_ERROR;
			return false;
		}

		public bool IsValid
		{
			get
			{
				return mode == mode_state.MODE_VALID;
			}
		}
		public bool IsInvalid
		{
			get
			{
				return mode == mode_state.MODE_INVALID;
			}
		}
		public bool IsError
		{
			get
			{
				return mode == mode_state.MODE_ERROR;
			}
		}
		public bool IsInvalidEx(ref int nDoSOut)
		{
			if(IsInvalid)
			{
				nDoSOut = nDoS;
				return true;
			}
			return false;
		}
		public bool CorruptionPossible()
		{
			return corruptionPossible;
		}
		public RejectCode RejectCode
		{
			get
			{
				return chRejectCode;
			}
		}
		//struct GetRejectReason()  { return strRejectReason; }

		public bool CheckTransaction(Transaction tx)
		{
			// Basic checks that don't depend on any context
			if(tx.VIn.Length == 0)
				return DoS(10, Utils.error("CheckTransaction() : vin empty"),
								 RejectCode.INVALID, "bad-txns-vin-empty");
			if(tx.VOut.Length == 0)
				return DoS(10, Utils.error("CheckTransaction() : vout empty"),
								 RejectCode.INVALID, "bad-txns-vout-empty");
			// Size limits
			if(tx.ToBytes().Length > MAX_BLOCK_SIZE)
				return DoS(100, Utils.error("CheckTransaction() : size limits failed"),
								 RejectCode.INVALID, "bad-txns-oversize");

			// Check for negative or overflow output values
			long nValueOut = 0;
			foreach(var txout in tx.VOut)
			{
				if(txout.Value < 0)
					return DoS(100, Utils.error("CheckTransaction() : txout.nValue negative"),
									 RejectCode.INVALID, "bad-txns-vout-negative");
				if(txout.Value > MAX_MONEY)
					return DoS(100, Utils.error("CheckTransaction() : txout.nValue too high"),
									 RejectCode.INVALID, "bad-txns-vout-toolarge");
				nValueOut += (long)txout.Value;
				if(!((nValueOut >= 0 && nValueOut <= (long)MAX_MONEY)))
					return DoS(100, Utils.error("CheckTransaction() : txout total out of range"),
									 RejectCode.INVALID, "bad-txns-txouttotal-toolarge");
			}

			// Check for duplicate inputs
			HashSet<OutPoint> vInOutPoints = new HashSet<OutPoint>();
			foreach(var txin in tx.VIn)
			{
				if(vInOutPoints.Contains(txin.PrevOut))
					return DoS(100, Utils.error("CheckTransaction() : duplicate inputs"),
									 RejectCode.INVALID, "bad-txns-inputs-duplicate");
				vInOutPoints.Add(txin.PrevOut);
			}

			if(tx.IsCoinBase)
			{
				if(tx.VIn[0].ScriptSig.Length < 2 || tx.VIn[0].ScriptSig.Length > 100)
					return DoS(100, Utils.error("CheckTransaction() : coinbase script size"),
									 RejectCode.INVALID, "bad-cb-length");
			}
			else
			{
				foreach(var txin in tx.VIn)
					if(txin.PrevOut.IsNull)
						return DoS(10, Utils.error("CheckTransaction() : prevout is null"),
										 RejectCode.INVALID, "bad-txns-prevout-null");
			}

			return true;
		}
	}
}
