using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
		static readonly uint MAX_BLOCK_SIGOPS = MAX_BLOCK_SIZE / 50;
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

		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}
		public ValidationState(Network network)
		{
			_Network = network;
			mode = mode_state.MODE_VALID;
			nDoS = 0;
			corruptionPossible = false;
			CheckProofOfWork = true;
			CheckMerkleRoot = true;
		}

		public bool CheckProofOfWork
		{
			get;
			set;
		}
		public bool CheckMerkleRoot
		{
			get;
			set;
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
			if(tx.Inputs.Count == 0)
				return DoS(10, Utils.error("CheckTransaction() : vin empty"),
								 RejectCode.INVALID, "bad-txns-vin-empty");
			if(tx.Outputs.Count == 0)
				return DoS(10, Utils.error("CheckTransaction() : vout empty"),
								 RejectCode.INVALID, "bad-txns-vout-empty");
			// Size limits
			if(tx.ToBytes().Length > MAX_BLOCK_SIZE)
				return DoS(100, Utils.error("CheckTransaction() : size limits failed"),
								 RejectCode.INVALID, "bad-txns-oversize");

			// Check for negative or overflow output values
			long nValueOut = 0;
			foreach(var txout in tx.Outputs)
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
			foreach(var txin in tx.Inputs)
			{
				if(vInOutPoints.Contains(txin.PrevOut))
					return DoS(100, Utils.error("CheckTransaction() : duplicate inputs"),
									 RejectCode.INVALID, "bad-txns-inputs-duplicate");
				vInOutPoints.Add(txin.PrevOut);
			}

			if(tx.IsCoinBase)
			{
				if(tx.Inputs[0].ScriptSig.Length < 2 || tx.Inputs[0].ScriptSig.Length > 100)
					return DoS(100, Utils.error("CheckTransaction() : coinbase script size"),
									 RejectCode.INVALID, "bad-cb-length");
			}
			else
			{
				foreach(var txin in tx.Inputs)
					if(txin.PrevOut.IsNull)
						return DoS(10, Utils.error("CheckTransaction() : prevout is null"),
										 RejectCode.INVALID, "bad-txns-prevout-null");
			}

			return true;
		}


		public bool CheckBlock(Block block)
		{
			// These are checks that are independent of context
			// that can be verified before saving an orphan block.

			// Size limits
			if(block.Vtx.Length == 0 || block.Vtx.Length > MAX_BLOCK_SIZE || block.Length > MAX_BLOCK_SIZE)
				return DoS(100, Error("CheckBlock() : size limits failed"),
								 RejectCode.INVALID, "bad-blk-length");

			// Check proof of work matches claimed amount
			if(CheckProofOfWork && !CheckProofOfWorkCore(block))
				return DoS(50, Error("CheckBlock() : proof of work failed"),
								 RejectCode.INVALID, "high-hash");

			// Check timestamp
			if(block.Header.BlockTime > Now + TimeSpan.FromSeconds(2 * 60 * 60))
				return Invalid(Error("CheckBlock() : block timestamp too far in the future"),
									 RejectCode.INVALID, "time-too-new");

			// First transaction must be coinbase, the rest must not be
			if(block.Vtx.Length == 0 || !block.Vtx[0].IsCoinBase)
				return DoS(100, Error("CheckBlock() : first tx is not coinbase"),
								 RejectCode.INVALID, "bad-cb-missing");
			for(int i = 1 ; i < block.Vtx.Length ; i++)
				if(block.Vtx[i].IsCoinBase)
					return DoS(100, Error("CheckBlock() : more than one coinbase"),
									 RejectCode.INVALID, "bad-cb-multiple");

			// Check transactions
			foreach(var tx in block.Vtx)
				if(!CheckTransaction(tx))
					return Error("CheckBlock() : CheckTransaction failed");

			// Build the merkle tree already. We need it anyway later, and it makes the
			// block cache the transaction hashes, which means they don't need to be
			// recalculated many times during this block's validation.
			block.ComputeMerkleRoot();

			// Check for duplicate txids. This is caught by ConnectInputs(),
			// but catching it earlier avoids a potential DoS attack:
			HashSet<uint256> uniqueTx = new HashSet<uint256>();
			for(int i = 0 ; i < block.Vtx.Length ; i++)
			{
				uniqueTx.Add(block.GetTxHash(i));
			}
			if(uniqueTx.Count != block.Vtx.Length)
				return DoS(100, Error("CheckBlock() : duplicate transaction"),
								 RejectCode.INVALID, "bad-txns-duplicate", true);

			int nSigOps = 0;
			foreach(var tx in block.Vtx)
			{
				nSigOps += GetLegacySigOpCount(tx);
			}
			if(nSigOps > MAX_BLOCK_SIGOPS)
				return DoS(100, Error("CheckBlock() : out-of-bounds SigOpCount"),
								 RejectCode.INVALID, "bad-blk-sigops", true);

			// Check merkle root
			if(CheckMerkleRoot && block.Header.HashMerkleRoot != block.vMerkleTree.Last())
				return DoS(100, Error("CheckBlock() : hashMerkleRoot mismatch"),
								 RejectCode.INVALID, "bad-txnmrklroot", true);

			return true;
		}
		public bool CheckProofOfWorkCore(Block block)
		{
			var bnTarget = block.Header.Bits.ToBigInteger();
			// Check range
			if(bnTarget <= 0 || bnTarget > Network.ProofOfWorkLimit)
				return Error("CheckProofOfWork() : nBits below minimum work");

			// Check proof of work matches claimed amount
			if(block.Header.CheckProofOfWork())
				return Error("CheckProofOfWork() : hash doesn't match nBits");
			return true;

		}
		private int GetLegacySigOpCount(Transaction tx)
		{
			uint nSigOps = 0;
			foreach(var txin in tx.Inputs)
			{
				nSigOps += txin.ScriptSig.GetSigOpCount(false);
			}
			foreach(var txout in tx.Outputs)
			{
				nSigOps += txout.ScriptPubKey.GetSigOpCount(false);
			}
			return (int)nSigOps;
		}




		DateTimeOffset _Now;
		public DateTimeOffset Now
		{
			get
			{
				if(_Now == default(DateTimeOffset))
					return DateTimeOffset.UtcNow;
				return _Now;
			}
			set
			{
				_Now = value;
			}
		}
	}
}
