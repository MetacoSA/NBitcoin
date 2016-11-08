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
	}
}
