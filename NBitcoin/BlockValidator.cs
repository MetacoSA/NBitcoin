using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin
{
	/// <summary>
	/// Block validation functionality
	/// </summary>
	public class BlockValidator
	{
		private const int MAX_BLOCK_SIZE = 1000000;

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

		public static bool IsCanonicalBlockSignature(Block block, bool checkLowS)
		{
			if (block.IsProofOfWork())
			{
				return block.BlockSignatur.IsEmpty();
			}

			return checkLowS ? 
				ScriptEvaluationContext.IsLowDerSignature(block.BlockSignatur.Signature) : 
				ScriptEvaluationContext.IsValidSignatureEncoding(block.BlockSignatur.Signature);
		}

		public static bool EnsureLowS(BlockSignature blockSignature)
		{
			var signature = new ECDSASignature(blockSignature.Signature);
			if (!signature.IsLowS)
				blockSignature.Signature = signature.MakeCanonical().ToDER();
			return true;
		}

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

		// Check kernel hash target and coinstake signature
		public static bool CheckProofOfStake(IBlockRepository blockStore, ITransactionRepository trasnactionStore, IBlockTransactionMapStore mapStore,
			ChainedBlock pindexPrev, Transaction tx, uint nBits, ref uint256 hashProofOfStake, ref uint256 targetProofOfStake)
		{
			// todo: Comments on this mehtod:
			// the store objects (IBlockRepository and  ITransactionRepository) should be a singleton instance of 
			// the BlockValidator and would be initiated as part of a Dependency Injection freamwork

			if (!tx.IsCoinStake)
				return false; // error("CheckProofOfStake() : called on non-coinstake %s", tx.GetHash().ToString());

			// Kernel (input 0) must match the stake hash target per coin age (nBits)
			var txIn = tx.Inputs[0];
			
			// First try finding the previous transaction in database
			var txPrev = trasnactionStore.Get(txIn.PrevOut.Hash);
			if (txPrev == null)
				return false; // tx.DoS(1, error("CheckProofOfStake() : INFO: read txPrev failed"));  // previous transaction not in main chain, may occur during initial download

			// Verify signature
			if (!VerifySignature(txPrev, tx, 0, ScriptVerify.None))
				return false; // tx.DoS(100, error("CheckProofOfStake() : VerifySignature failed on coinstake %s", tx.GetHash().ToString()));

			// Read block header
			var blockHashPrev = mapStore.GetBlockHash(txIn.PrevOut.Hash);
			var block = blockHashPrev == null ? null : blockStore.GetBlock(blockHashPrev);
			if(block == null)
				return false; //fDebug? error("CheckProofOfStake() : read block failed") : false; // unable to read block of previous transaction

			// Min age requirement
			if (IsProtocolV3((int)tx.Time))
			{
				int nDepth = 0;
				if (IsConfirmedInNPrevBlocks(blockStore, txPrev, pindexPrev, StakeMinConfirmations - 1, ref nDepth))
					return false; // tx.DoS(100, error("CheckProofOfStake() : tried to stake at depth %d", nDepth + 1));
			}
			else
			{
				var nTimeBlockFrom = block.Header.Time;
				if (nTimeBlockFrom + StakeMinAge > tx.Time)
					return false; // error("CheckProofOfStake() : min age violation");
			}

			if (!CheckStakeKernelHash(pindexPrev, nBits, block, txPrev, txIn.PrevOut, tx.Time, ref hashProofOfStake, ref targetProofOfStake, false))
				return false; // tx.DoS(1, error("CheckProofOfStake() : INFO: check kernel failed on coinstake %s, hashProof=%s", tx.GetHash().ToString(), hashProofOfStake.ToString())); // may occur during initial download or if behind on block chain sync

			return true;
		}

		const int StakeMinConfirmations = 50;
		const uint StakeMinAge = 60; // 8 hours
		const uint ModifierInterval = 10 * 60; // time to elapse before new modifier is computed

		private static bool IsConfirmedInNPrevBlocks(IBlockRepository blockStore, Transaction txPrev, ChainedBlock pindexFrom, int maxDepth, ref int actualDepth)
		{
			// note: this method can be optimized by ensuring that blockstore keeps 
			// in memory at least maxDepth blocks twards genesis

			var hashPrev = txPrev.GetHash();

			for (var pindex = pindexFrom; pindex != null && pindexFrom.Height - pindex.Height < maxDepth; pindex = pindex.Previous)
			{
				var block = blockStore.GetBlock(pindex.HashBlock);
				if (block.Transactions.Any(b => b.GetHash() == hashPrev)) //pindex->nBlockPos == txindex.pos.nBlockPos && pindex->nFile == txindex.pos.nFile)
				{
					actualDepth = pindexFrom.Height - pindex.Height;
					return true;
				}
			}

			return false;
		}

		private static bool VerifySignature(Transaction txFrom, Transaction txTo, int txToInN, ScriptVerify flagScriptVerify)
		{
			var input = txTo.Inputs[txToInN];

			if (input.PrevOut.N >= txFrom.Outputs.Count)
				return false;

			if (input.PrevOut.Hash != txFrom.GetHash())
				return false;

			var output = txFrom.Outputs[input.PrevOut.N];

			var txData = new PrecomputedTransactionData(txFrom);
			var checker = new TransactionChecker(txTo, txToInN, output.Value, txData);
			var ctx = new ScriptEvaluationContext {ScriptVerify = flagScriptVerify};
			
			return ctx.VerifyScript(input.ScriptSig, output.ScriptPubKey, checker);
		}

		private static bool CheckStakeKernelHash(ChainedBlock pindexPrev, uint nBits, Block blockFrom, Transaction txPrev,
			OutPoint prevout, uint nTimeTx, ref uint256 hashProofOfStake, ref uint256 targetProofOfStake, bool fPrintProofOfStake)
		{
			if (IsProtocolV2(pindexPrev.Height + 1))
				return CheckStakeKernelHashV2(pindexPrev, nBits, blockFrom.Header.Time, txPrev, prevout, nTimeTx, ref hashProofOfStake, ref targetProofOfStake, fPrintProofOfStake);
			else
				return CheckStakeKernelHashV1();
		}

		// Stratis kernel protocol
		// coinstake must meet hash target according to the protocol:
		// kernel (input 0) must meet the formula
		//     hash(nStakeModifier + txPrev.block.nTime + txPrev.nTime + txPrev.vout.hash + txPrev.vout.n + nTime) < bnTarget * nWeight
		// this ensures that the chance of getting a coinstake is proportional to the
		// amount of coins one owns.
		// The reason this hash is chosen is the following:
		//   nStakeModifier: scrambles computation to make it very difficult to precompute
		//                   future proof-of-stake
		//   txPrev.block.nTime: prevent nodes from guessing a good timestamp to
		//                       generate transaction for future advantage,
		//                       obsolete since v3
		//   txPrev.nTime: slightly scrambles computation
		//   txPrev.vout.hash: hash of txPrev, to reduce the chance of nodes
		//                     generating coinstake at the same time
		//   txPrev.vout.n: output number of txPrev, to reduce the chance of nodes
		//                  generating coinstake at the same time
		//   nTime: current timestamp
		//   block/tx hash should not be used here as they can be generated in vast
		//   quantities so as to generate blocks faster, degrading the system back into
		//   a proof-of-work situation.
		//
		private static bool CheckStakeKernelHashV1()
		{
			// this is not relevant for the stratis blockchain
			throw new NotImplementedException();
		}

		// Stratis kernel protocol
		// coinstake must meet hash target according to the protocol:
		// kernel (input 0) must meet the formula
		//     hash(nStakeModifier + txPrev.block.nTime + txPrev.nTime + txPrev.vout.hash + txPrev.vout.n + nTime) < bnTarget * nWeight
		// this ensures that the chance of getting a coinstake is proportional to the
		// amount of coins one owns.
		// The reason this hash is chosen is the following:
		//   nStakeModifier: scrambles computation to make it very difficult to precompute
		//                   future proof-of-stake
		//   txPrev.block.nTime: prevent nodes from guessing a good timestamp to
		//                       generate transaction for future advantage,
		//                       obsolete since v3
		//   txPrev.nTime: slightly scrambles computation
		//   txPrev.vout.hash: hash of txPrev, to reduce the chance of nodes
		//                     generating coinstake at the same time
		//   txPrev.vout.n: output number of txPrev, to reduce the chance of nodes
		//                  generating coinstake at the same time
		//   nTime: current timestamp
		//   block/tx hash should not be used here as they can be generated in vast
		//   quantities so as to generate blocks faster, degrading the system back into
		//   a proof-of-work situation.
		//
		private static bool CheckStakeKernelHashV2(ChainedBlock pindexPrev, uint nBits, uint nTimeBlockFrom,
			Transaction txPrev, OutPoint prevout, uint nTimeTx, ref uint256 hashProofOfStake, ref uint256 targetProofOfStake,
			bool fPrintProofOfStake)
		{
			if (nTimeTx < txPrev.Time) // Transaction timestamp violation
				return false; //error("CheckStakeKernelHash() : nTime violation");

			// Base target
			var bnTarget = new Target(nBits).ToBigInteger();
			
			// Weighted target
			var nValueIn = txPrev.Outputs[prevout.N].Value.Satoshi;
			var bnWeight = new BigInteger(nValueIn);
			bnTarget *= bnWeight;

			targetProofOfStake = Target.ToUInt256(bnTarget);

			var nStakeModifier = pindexPrev.Header.PosParameters.StakeModifier;
			uint256 bnStakeModifierV2 = pindexPrev.Header.PosParameters.StakeModifierV2;
			int nStakeModifierHeight = pindexPrev.Height;
			var nStakeModifierTime = pindexPrev.Header.Time;

			// Calculate hash
			using (var ms = new MemoryStream())
			{
				var serializer = new BitcoinStream(ms, true);
				if (IsProtocolV3((int) nTimeTx))
				{
					serializer.ReadWrite(bnStakeModifierV2);
				}
				else
				{
					serializer.ReadWrite(nStakeModifier);
					serializer.ReadWrite(nTimeBlockFrom);
				}

				serializer.ReadWrite(txPrev.Time);
				serializer.ReadWrite(prevout.Hash);
				serializer.ReadWrite(prevout.N);
				serializer.ReadWrite(nTimeTx);

				hashProofOfStake = Hashes.Hash256(ms.ToArray());
			}

			if (fPrintProofOfStake)
			{
				//LogPrintf("CheckStakeKernelHash() : using modifier 0x%016x at height=%d timestamp=%s for block from timestamp=%s\n",
				//	nStakeModifier, nStakeModifierHeight,
				//	DateTimeStrFormat(nStakeModifierTime),

				//	DateTimeStrFormat(nTimeBlockFrom));

				//LogPrintf("CheckStakeKernelHash() : check modifier=0x%016x nTimeBlockFrom=%u nTimeTxPrev=%u nPrevout=%u nTimeTx=%u hashProof=%s\n",
				//	nStakeModifier,
				//	nTimeBlockFrom, txPrev.nTime, prevout.n, nTimeTx,
				//	hashProofOfStake.ToString());
			}

			// Now check if proof-of-stake hash meets target protocol
			var hashProofOfStakeTarget = new BigInteger(hashProofOfStake.ToBytes());
			if (hashProofOfStakeTarget > bnTarget)
				return false;
			

			//  if (fDebug && !fPrintProofOfStake)
			//  {
			//		LogPrintf("CheckStakeKernelHash() : using modifier 0x%016x at height=%d timestamp=%s for block from timestamp=%s\n",
			//		nStakeModifier, nStakeModifierHeight,
			//		DateTimeStrFormat(nStakeModifierTime),

			//		DateTimeStrFormat(nTimeBlockFrom));

			//		LogPrintf("CheckStakeKernelHash() : pass modifier=0x%016x nTimeBlockFrom=%u nTimeTxPrev=%u nPrevout=%u nTimeTx=%u hashProof=%s\n",
			//		nStakeModifier,
			//		nTimeBlockFrom, txPrev.nTime, prevout.n, nTimeTx,
			//		hashProofOfStake.ToString());
			//  }

			return true;
		}

		public static bool CheckKernel(IBlockRepository blockStore, ITransactionRepository trasnactionStore,
			IBlockTransactionMapStore mapStore,
			ChainedBlock pindexPrev, uint nBits, uint nTime, OutPoint prevout, uint pBlockTime)
		{
			uint256 hashProofOfStake = null, targetProofOfStake = null;

			var txPrev = trasnactionStore.Get(prevout.Hash);
			if (txPrev == null)
				return false;

			// Read block header
			var blockHashPrev = mapStore.GetBlockHash(prevout.Hash);
			var block = blockHashPrev == null ? null : blockStore.GetBlock(blockHashPrev);
			if (block == null)
				return false;

			if (IsProtocolV3((int) nTime))
			{
				int nDepth = 0;
				if (IsConfirmedInNPrevBlocks(blockStore, txPrev, pindexPrev, StakeMinConfirmations - 1, ref nDepth))
					return false; // tx.DoS(100, error("CheckProofOfStake() : tried to stake at depth %d", nDepth + 1));
			}
			else
			{
				var nTimeBlockFrom = block.Header.Time;
				if (nTimeBlockFrom + StakeMinAge > nTime)
					return false; // error("CheckProofOfStake() : min age violation");
			}

			// todo: check this unclear logic
			//if (pBlockTime)
			//	pBlockTime = block.Header.Time;

			return CheckStakeKernelHash(pindexPrev, nBits, block, txPrev, prevout, nTime, ref hashProofOfStake, ref targetProofOfStake, false);
		}
	}
}
