using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Crypto;

namespace NBitcoin
{
	/// <summary>
	/// Block validation functionality
	/// </summary>
	public class BlockValidator
	{
		private static bool IsProtocolV1RetargetingFixed(int height)
		{
			return height > 0;
		}

		private static bool IsProtocolV2(int height)
		{
			return height > 0;
		}

		private static bool IsProtocolV3(int nTime)
		{
			return nTime > 1470467000;
		}

		private static System.Numerics.BigInteger GetProofOfStakeLimit(Consensus consensus, int height)
		{
			return IsProtocolV2(height) ? consensus.ProofOfStakeLimitV2 : consensus.ProofOfStakeLimit;
		}

		private static int GetTargetSpacing(int height)
		{
			return IsProtocolV2(height) ? 64 : 60;
		}

		private static long FutureDriftV1(long nTime) { return nTime + 10 * 60; }
		private static long FutureDriftV2(long nTime) { return nTime + 128 * 60 * 60; }
		private static long FutureDrift(long nTime, int nHeight) { return IsProtocolV2(nHeight) ? FutureDriftV2(nTime) : FutureDriftV1(nTime); }

		// find last block index up to index
		private static ChainedBlock GetLastBlockIndex(ChainedBlock index, bool proofOfStake)
		{
			if (index == null)
				throw new ArgumentNullException(nameof(index));

			while (index.Previous != null && (index.Header.PosParameters.IsProofOfStake() != proofOfStake))
				index = index.Previous;

			return index;
		}

		public static Target GetNextTargetRequired(ChainedBlock indexLast, Consensus consensus, bool proofOfStake)
		{
			// Genesis block
			if (indexLast == null)
				return consensus.PowLimit;

			// find the last two blocks that correspond to the mining algo 
			// (i.e if this is a POS block we need to find the last two POS blocks)
			var targetLimit = proofOfStake
				? GetProofOfStakeLimit(consensus, indexLast.Height)
				: consensus.PowLimit.ToBigInteger();

			// first block
			var pindexPrev = GetLastBlockIndex(indexLast, proofOfStake);
			if (pindexPrev.Previous == null)
				return new Target(targetLimit);

			// second block
			var pindexPrevPrev = GetLastBlockIndex(pindexPrev.Previous, proofOfStake);
			if (pindexPrevPrev == null)
				return new Target(targetLimit);


			int targetSpacing = GetTargetSpacing(indexLast.Height);
			int actualSpacing = (int) (pindexPrev.Header.Time - pindexPrevPrev.Header.Time);
			if (IsProtocolV1RetargetingFixed(indexLast.Height))
			{
				if (actualSpacing < 0) actualSpacing = targetSpacing;
			}
			if (IsProtocolV3((int) indexLast.Header.Time))
			{
				if (actualSpacing > targetSpacing*10) actualSpacing = targetSpacing*10;
			}

			// target change every block
			// retarget with exponential moving toward target spacing
			var targetTimespan = 16*60; // 16 mins
			var target = pindexPrev.Header.Bits.ToBigInteger();

			int interval = targetTimespan/targetSpacing;
			target *= ((interval - 1)*targetSpacing + actualSpacing + actualSpacing);
			target /= ((interval + 1)*targetSpacing);

			if (target <= 0 || target > targetLimit)
				target = targetLimit;

			return new Target(target);
		}

		public static bool CheckBlockSignature(Block block)
		{
			if (block.IsProofOfWork())
				return block.BlockSignatur.IsEmpty();

			if (block.BlockSignatur.IsEmpty())
				return false;

			var txout = block.Transactions[1].Outputs[1];

			if (PayToPubkeyTemplate.Instance.CheckScriptPubKey(txout.ScriptPubKey))
			{
				var pubKey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey);
				return pubKey.Verify(block.GetHash(), new ECDSASignature(block.BlockSignatur.Signature));
			}

			if (IsProtocolV3((int)block.Header.Time))
			{
				// Block signing key also can be encoded in the nonspendable output
				// This allows to not pollute UTXO set with useless outputs e.g. in case of multisig staking

				var ops = txout.ScriptPubKey.ToOps().ToList();
				if (!ops.Any()) // script.GetOp(pc, opcode, vchPushValue))
					return false;
				if (ops.ElementAt(0).Code != OpcodeType.OP_RETURN) // OP_RETURN)
					return false;
				if (ops.Count < 2) // script.GetOp(pc, opcode, vchPushValue)
					return false;
				var data = ops.ElementAt(1).PushData;
				if (!ScriptEvaluationContext.IsCompressedOrUncompressedPubKey(data))
					return false;
				return new PubKey(data).Verify(block.GetHash(), new ECDSASignature(block.BlockSignatur.Signature));
			}

			return false;
		}

		private const int MAX_BLOCK_SIZE = 1000000;

		// a method to check a block, this may be moved to the full node.
		public static bool CheckBlock(Block block, bool checkPow = true, bool checkMerkleRoot = true, bool checkSig = true)
		{
			// These are checks that are independent of context
			// that can be verified before saving an orphan block.

			// Size limits
			if (block.Transactions.Empty() || block.GetSerializedSize() > MAX_BLOCK_SIZE)
				return false; // DoS(100, error("CheckBlock() : size limits failed"));

			// Check proof of work matches claimed amount
			if (checkPow && block.IsProofOfWork() && !block.CheckProofOfWork())
				return false; //DoS(50, error("CheckBlock() : proof of work failed"));

			// Check timestamp
			if (block.Header.Time > FutureDriftV2(DateTime.UtcNow.Ticks)) //GetAdjustedTime()))
				return false; //error("CheckBlock() : block timestamp too far in he future");

			// First transaction must be coinbase, the rest must not be
			if (!block.Transactions[0].IsCoinBase)
				return false; //  DoS(100, error("CheckBlock() : first tx is not coinbase"));

			if (block.Transactions.Skip(1).Any(t => t.IsCoinBase))
				return false; //DoS(100, error("CheckBlock() : more than one coinbase"));

			if (block.IsProofOfStake())
			{
				// Coinbase output should be empty if proof-of-stake block
				if (block.Transactions[0].Outputs.Count != 1 || !block.Transactions[0].Outputs[0].IsEmpty)
					return false; // DoS(100, error("CheckBlock() : coinbase output not empty for proof-of-stake block"));

				// Second transaction must be coinstake, the rest must not be
				if (!block.Transactions[1].IsCoinStake)
					return false; // DoS(100, error("CheckBlock() : second tx is not coinstake"));

				if (block.Transactions.Skip(2).Any(t => t.IsCoinStake))
					return false; //DoS(100, error("CheckBlock() : more than one coinstake"));
			}

			// Check proof-of-stake block signature
			if (checkSig && !CheckBlockSignature(block))
				return false; //DoS(100, error("CheckBlock() : bad proof-of-stake block signature"));

			// Check transactions
			foreach (var transaction in block.Transactions)
			{
				if (transaction.Check() != TransactionCheckResult.Success)
					return false; // DoS(tx.nDoS, error("CheckBlock() : CheckTransaction failed"));

				// ppcoin: check transaction timestamp
				if (block.Header.Time < transaction.Time)
					return false; // DoS(50, error("CheckBlock() : block timestamp earlier than transaction timestamp"));
			}

			// Check for duplicate txids. This is caught by ConnectInputs(),
			// but catching it earlier avoids a potential DoS attack:
			var set = new HashSet<uint256>();
			if(block.Transactions.Select(t => t.GetHash()).Any(h => !set.Add(h)))
				return false; //DoS(100, error("CheckBlock() : duplicate transaction"));

			// todo: check if this is legacy from older implementtions and actually needed
			//uint nSigOps = 0;
			//foreach (var transaction in block.Transactions)
			//{
			//	nSigOps += GetLegacySigOpCount(transaction);
			//}
			//if (nSigOps > MAX_BLOCK_SIGOPS)
			//	return DoS(100, error("CheckBlock() : out-of-bounds SigOpCount"));

			// Check merkle root
			if (checkMerkleRoot && !block.CheckMerkleRoot())
				return false; //DoS(100, error("CheckBlock() : hashMerkleRoot mismatch"));

			return true;
		}
	}
}
