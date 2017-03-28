using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;

namespace NBitcoin
{
	/// <summary>
	/// Block validation functionality
	/// </summary>
	public class BlockValidator
	{
		private const int MAX_BLOCK_SIZE = 1000000;

		public const int STAKE_TIMESTAMP_MASK = 15;
		public const long COIN = 100000000;
		public const long CENT = 1000000;

		private static bool IsProtocolV1RetargetingFixed(int height)
		{
			return height > 0;
		}

		public static bool IsProtocolV2(int height)
		{
			return height > 0;
		}

		public static bool IsProtocolV3(int nTime)
		{
			return nTime > 1470467000;
		}

		private static BigInteger GetProofOfStakeLimit(Consensus consensus, int height)
		{
			return IsProtocolV2(height) ? consensus.ProofOfStakeLimitV2 : consensus.ProofOfStakeLimit;
		}

		public static int GetTargetSpacing(int height)
		{
			return IsProtocolV2(height) ? 64 : 60;
		}

		private static long FutureDriftV1(long nTime) { return nTime + 10 * 60; }
		private static long FutureDriftV2(long nTime) { return nTime + 128 * 60 * 60; }
		private static long FutureDrift(long nTime, int nHeight) { return IsProtocolV2(nHeight) ? FutureDriftV2(nTime) : FutureDriftV1(nTime); }

		// Get time weight
		public static long GetWeight(long nIntervalBeginning, long nIntervalEnd)
		{
			// Kernel hash weight starts from 0 at the min age
			// this change increases active coins participating the hash and helps
			// to secure the network when proof-of-stake difficulty is low

			return nIntervalEnd - nIntervalBeginning - StakeMinAge;
		}

		public static uint GetPastTimeLimit(ChainedBlock chainedBlock)
		{
			if (IsProtocolV2(chainedBlock.Height))
				return chainedBlock.Header.Time;
			else
				return GetMedianTimePast(chainedBlock);
		}

		private const int MedianTimeSpan = 11;

		public static uint GetMedianTimePast(ChainedBlock chainedBlock)
		{
			var soretedList = new SortedSet<uint>();
			var pindex = chainedBlock;
			for (int i = 0; i < MedianTimeSpan && pindex != null; i++, pindex = pindex.Previous)
				soretedList.Add(pindex.Header.Time);

			return (soretedList.First() - soretedList.Last()) / 2;
		}

		// find last block index up to index
		public static ChainedBlock GetLastBlockIndex(StakeChain stakeChain, ChainedBlock index, bool proofOfStake)
		{
			if (index == null)
				throw new ArgumentNullException(nameof(index));
			var blockStake = stakeChain.Get(index.HashBlock);

			while (index.Previous != null && (blockStake.IsProofOfStake() != proofOfStake))
			{
				index = index.Previous;
				blockStake = stakeChain.Get(index.HashBlock);
			}

			return index;
		}

		public static Target GetNextTargetRequired(StakeChain stakeChain, ChainedBlock indexLast, Consensus consensus, bool proofOfStake)
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
			var pindexPrev = GetLastBlockIndex(stakeChain, indexLast, proofOfStake);
			if (pindexPrev.Previous == null)
				return new Target(targetLimit);

			// second block
			var pindexPrevPrev = GetLastBlockIndex(stakeChain, pindexPrev.Previous, proofOfStake);
			if (pindexPrevPrev.Previous == null)
				return new Target(targetLimit);


			int targetSpacing = GetTargetSpacing(indexLast.Height);
			int actualSpacing = (int)(pindexPrev.Header.Time - pindexPrevPrev.Header.Time);
			if (IsProtocolV1RetargetingFixed(indexLast.Height))
			{
				if (actualSpacing < 0) actualSpacing = targetSpacing;
			}
			if (IsProtocolV3((int)indexLast.Header.Time))
			{
				if (actualSpacing > targetSpacing * 10) actualSpacing = targetSpacing * 10;
			}

			// target change every block
			// retarget with exponential moving toward target spacing
			var targetTimespan = 16 * 60; // 16 mins
			var target = pindexPrev.Header.Bits.ToBigInteger();

			int interval = targetTimespan / targetSpacing;
			target = target.Multiply(BigInteger.ValueOf(((interval - 1) * targetSpacing + actualSpacing + actualSpacing)));
			target = target.Divide(BigInteger.ValueOf(((interval + 1) * targetSpacing)));

			if (target.CompareTo(BigInteger.Zero) <= 0 || target.CompareTo(targetLimit) >= 1)
				//if (target <= 0 || target > targetLimit)
				target = targetLimit;

			return new Target(target);
		}

		public static bool CheckBlockSignature(Block block)
		{
			if (BlockStake.IsProofOfWork(block))
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
			if (BlockStake.IsProofOfWork(block))
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
			if (!block.Transactions.Any() || block.GetSerializedSize() > MAX_BLOCK_SIZE)
				return false; // DoS(100, error("CheckBlock() : size limits failed"));

			// Check proof of work matches claimed amount
			if (checkPow && BlockStake.IsProofOfWork(block) && !block.CheckProofOfWork())
				return false; //DoS(50, error("CheckBlock() : proof of work failed"));

			// Check timestamp
			if (block.Header.Time > FutureDriftV2(DateTime.UtcNow.Ticks)) //GetAdjustedTime()))
				return false; //error("CheckBlock() : block timestamp too far in he future");

			// First transaction must be coinbase, the rest must not be
			if (!block.Transactions[0].IsCoinBase)
				return false; //  DoS(100, error("CheckBlock() : first tx is not coinbase"));

			if (block.Transactions.Skip(1).Any(t => t.IsCoinBase))
				return false; //DoS(100, error("CheckBlock() : more than one coinbase"));

			if (BlockStake.IsProofOfStake(block))
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
			if (block.Transactions.Select(t => t.GetHash()).Any(h => !set.Add(h)))
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
			ChainedBlock pindexPrev, BlockStake prevBlockStake, Transaction tx, uint nBits, out uint256 hashProofOfStake, out uint256 targetProofOfStake)
		{
			targetProofOfStake = null; hashProofOfStake = null;

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
			if (block == null)
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

			if (!CheckStakeKernelHash(pindexPrev, nBits, block, txPrev, prevBlockStake, txIn.PrevOut, tx.Time, out hashProofOfStake, out targetProofOfStake, false))
				return false; // tx.DoS(1, error("CheckProofOfStake() : INFO: check kernel failed on coinstake %s, hashProof=%s", tx.GetHash().ToString(), hashProofOfStake.ToString())); // may occur during initial download or if behind on block chain sync

			return true;
		}

		public const int StakeMinConfirmations = 50;
		public const uint StakeMinAge = 60; // 8 hours
		public const uint ModifierInterval = 10 * 60; // time to elapse before new modifier is computed

		public static bool CheckAndComputeStake(IBlockRepository blockStore, ITransactionRepository trasnactionStore, IBlockTransactionMapStore mapStore, StakeChain stakeChain,
			ChainBase chainIndex, ChainedBlock pindex, Block block, out BlockStake blockStake)
		{
			if (block.GetHash() != pindex.HashBlock)
				throw new ArgumentException();

			blockStake = new BlockStake(block);

			uint256 hashProof = null;
			// Verify hash target and signature of coinstake tx
			if (BlockStake.IsProofOfStake(block))
			{
				var pindexPrev = pindex.Previous;

				var prevBlockStake = stakeChain.Get(pindexPrev.HashBlock);
				if (prevBlockStake == null)
					return false; // the stake proof of the previous block is not set

				uint256 targetProofOfStake;
				if (!CheckProofOfStake(blockStore, trasnactionStore, mapStore, pindexPrev, prevBlockStake, block.Transactions[1],
						pindex.Header.Bits.ToCompact(), out hashProof, out targetProofOfStake))
					return false; // error("AcceptBlock() : check proof-of-stake failed for block %s", hash.ToString());
			}

			// PoW is checked in CheckBlock()
			if (BlockStake.IsProofOfWork(block))
			{
				hashProof = pindex.Header.GetPoWHash();
			}

			// todo: is this the same as chain work?
			// compute chain trust score
			//pindexNew.nChainTrust = (pindexNew->pprev ? pindexNew->pprev->nChainTrust : 0) + pindexNew->GetBlockTrust();

			// compute stake entropy bit for stake modifier
			if (!blockStake.SetStakeEntropyBit(blockStake.GetStakeEntropyBit()))
				return false; //error("AddToBlockIndex() : SetStakeEntropyBit() failed");

			// Record proof hash value
			blockStake.HashProof = hashProof;

			// compute stake modifier
			return ComputeStakeModifier(chainIndex, pindex, blockStake, stakeChain);
		}

		private static bool IsConfirmedInNPrevBlocks(IBlockRepository blockStore, Transaction txPrev, ChainedBlock pindexFrom, int maxDepth, ref int actualDepth)
		{
			// note: this method can be optimized by ensuring that blockstore keeps 
			// in memory at least maxDepth blocks twards genesis

			var hashPrev = txPrev.GetHash();

			for (var pindex = pindexFrom; pindex != null && pindexFrom.Height - pindex.Height < maxDepth; pindex = pindex.Previous)
			{
				var block = blockStore.GetBlock(pindex.HashBlock);
				if (block == null)
					return false;
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
			var ctx = new ScriptEvaluationContext { ScriptVerify = flagScriptVerify };

			return ctx.VerifyScript(input.ScriptSig, output.ScriptPubKey, checker);
		}

		private static bool CheckStakeKernelHash(ChainedBlock pindexPrev, uint nBits, Block blockFrom, Transaction txPrev, BlockStake prevBlockStake,
			OutPoint prevout, uint nTimeTx, out uint256 hashProofOfStake, out uint256 targetProofOfStake, bool fPrintProofOfStake)
		{
			targetProofOfStake = null; hashProofOfStake = null;

			if (IsProtocolV2(pindexPrev.Height + 1))
				return CheckStakeKernelHashV2(pindexPrev, nBits, blockFrom.Header.Time, prevBlockStake, txPrev, prevout, nTimeTx, out hashProofOfStake, out targetProofOfStake, fPrintProofOfStake);
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
		private static bool CheckStakeKernelHashV2(ChainedBlock pindexPrev, uint nBits, uint nTimeBlockFrom, BlockStake prevBlockStake,
			Transaction txPrev, OutPoint prevout, uint nTimeTx, out uint256 hashProofOfStake, out uint256 targetProofOfStake, bool fPrintProofOfStake)
		{
			targetProofOfStake = null; hashProofOfStake = null;

			if (nTimeTx < txPrev.Time) // Transaction timestamp violation
				return false; //error("CheckStakeKernelHash() : nTime violation");

			// Base target
			var bnTarget = new Target(nBits).ToBigInteger();

			// Weighted target
			var nValueIn = txPrev.Outputs[prevout.N].Value.Satoshi;
			var bnWeight = BigInteger.ValueOf(nValueIn);
			bnTarget = bnTarget.Multiply(bnWeight);

			// todo: investigate this issue, is the convertion to uint256 similar to the c++ implementation
			//targetProofOfStake = Target.ToUInt256(bnTarget);

			var nStakeModifier = prevBlockStake.StakeModifier; //pindexPrev.Header.BlockStake.StakeModifier;
			uint256 bnStakeModifierV2 = prevBlockStake.StakeModifierV2; //pindexPrev.Header.BlockStake.StakeModifierV2;
			int nStakeModifierHeight = pindexPrev.Height;
			var nStakeModifierTime = pindexPrev.Header.Time;

			// Calculate hash
			using (var ms = new MemoryStream())
			{
				var serializer = new BitcoinStream(ms, true);
				if (IsProtocolV3((int)nTimeTx))
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
			var hashProofOfStakeTarget = new BigInteger(hashProofOfStake.ToBytes(false));
			if (hashProofOfStakeTarget.CompareTo(bnTarget) > 0)
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

		// Check whether the coinstake timestamp meets protocol
		public static bool CheckCoinStakeTimestamp(int nHeight, long nTimeBlock, long nTimeTx)
		{
			if (IsProtocolV2(nHeight))
				return (nTimeBlock == nTimeTx) && ((nTimeTx & STAKE_TIMESTAMP_MASK) == 0);
			else
				return (nTimeBlock == nTimeTx);
		}

		public static bool CheckKernel(IBlockRepository blockStore, ITransactionRepository trasnactionStore,
			IBlockTransactionMapStore mapStore, StakeChain stakeChain,
			ChainedBlock pindexPrev, uint nBits, long nTime, OutPoint prevout, ref long pBlockTime)
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

			if (IsProtocolV3((int)nTime))
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

			var prevBlockStake = stakeChain.Get(pindexPrev.HashBlock);
			if (prevBlockStake == null)
				return false; // the stake proof of the previous block is not set

			// todo: check this unclear logic
			//if (pBlockTime)
			//	pBlockTime = block.Header.Time;

			return CheckStakeKernelHash(pindexPrev, nBits, block, txPrev, prevBlockStake, prevout, (uint)nTime, out hashProofOfStake, out targetProofOfStake, false);
		}

		public static bool ComputeStakeModifier(ChainBase chainIndex, ChainedBlock pindex, BlockStake blockStake, StakeChain stakeChain)
		{
			var pindexPrev = pindex.Previous;
			var blockStakePrev = pindexPrev == null? null : stakeChain.Get(pindexPrev.HashBlock);

			// compute stake modifier
			ulong nStakeModifier;
			bool fGeneratedStakeModifier;
			if (!ComputeNextStakeModifier(stakeChain, chainIndex, pindexPrev, out nStakeModifier, out fGeneratedStakeModifier))
				return false; //error("AddToBlockIndex() : ComputeNextStakeModifier() failed");

			blockStake.SetStakeModifier(nStakeModifier, fGeneratedStakeModifier);
			blockStake.StakeModifierV2 = ComputeStakeModifierV2(
				pindexPrev, blockStakePrev, blockStake.IsProofOfWork() ? pindex.HashBlock : blockStake.PrevoutStake.Hash);

			return true;
		}

		// Get the last stake modifier and its generation time from a given block
		private static bool GetLastStakeModifier(StakeChain stakeChain, ChainedBlock pindex, out ulong stakeModifier, out long modifierTime)
		{
			stakeModifier = 0;
			modifierTime = 0;

			if (pindex == null)
				return false; // error("GetLastStakeModifier: null pindex");

			var blockStake = stakeChain.Get(pindex.HashBlock);
			while (pindex != null && pindex.Previous != null && !blockStake.GeneratedStakeModifier())
			{
				pindex = pindex.Previous;
				blockStake = stakeChain.Get(pindex.HashBlock);
			}

			if (!blockStake.GeneratedStakeModifier())
				return false; // error("GetLastStakeModifier: no generation at genesis block");

			stakeModifier = blockStake.StakeModifier;
			modifierTime = pindex.Header.Time;

			return true;
		}

		// Get stake modifier selection interval (in seconds)
		private static long GetStakeModifierSelectionInterval()
		{
			long nSelectionInterval = 0;
			for (int nSection = 0; nSection < 64; nSection++)
				nSelectionInterval += GetStakeModifierSelectionIntervalSection(nSection);
			return nSelectionInterval;
		}

		// MODIFIER_INTERVAL_RATIO:
		// ratio of group interval length between the last group and the first group
		const int MODIFIER_INTERVAL_RATIO = 3;

		// Get selection interval section (in seconds)
		private static long GetStakeModifierSelectionIntervalSection(int nSection)
		{
			if (!(nSection >= 0 && nSection < 64))
				throw new ArgumentOutOfRangeException();
			return (ModifierInterval * 63 / (63 + ((63 - nSection) * (MODIFIER_INTERVAL_RATIO - 1))));
		}

		// select a block from the candidate blocks in vSortedByTimestamp, excluding
		// already selected blocks in vSelectedBlocks, and with timestamp up to
		// nSelectionIntervalStop.
		private static bool SelectBlockFromCandidates(StakeChain stakeChain, ChainedBlock chainIndex, SortedDictionary<uint, uint256> sortedByTimestamp,
			Dictionary<uint256, ChainedBlock> mapSelectedBlocks,
			long nSelectionIntervalStop, ulong nStakeModifierPrev, out ChainedBlock pindexSelected)
		{

			bool fSelected = false;
			uint256 hashBest = 0;
			pindexSelected = null;

			foreach (var item in sortedByTimestamp)
			{
				var pindex = chainIndex.FindAncestorOrSelf(item.Value);
				if (pindex == null)
					return false; // error("SelectBlockFromCandidates: failed to find block index for candidate block %s", item.second.ToString());

				if (fSelected && pindex.Header.Time > nSelectionIntervalStop)
					break;

				if (mapSelectedBlocks.Keys.Any(key => key == pindex.HashBlock))
					continue;

				var blockStake = stakeChain.Get(pindex.HashBlock);

				// compute the selection hash by hashing its proof-hash and the
				// previous proof-of-stake modifier
				uint256 hashSelection;
				using (var ms = new MemoryStream())
				{
					var serializer = new BitcoinStream(ms, true);
					serializer.ReadWrite(blockStake.HashProof);
					serializer.ReadWrite(nStakeModifierPrev);

					hashSelection = Hashes.Hash256(ms.ToArray());
				}

				// the selection hash is divided by 2**32 so that proof-of-stake block
				// is always favored over proof-of-work block. this is to preserve
				// the energy efficiency property
				if (blockStake.IsProofOfStake())
					hashSelection >>= 32;

				if (fSelected && hashSelection < hashBest)
				{
					hashBest = hashSelection;
					pindexSelected = pindex;
				}
				else if (!fSelected)
				{
					fSelected = true;
					hashBest = hashSelection;
					pindexSelected = pindex;
				}
			}

			//LogPrint("stakemodifier", "SelectBlockFromCandidates: selection hash=%s\n", hashBest.ToString());
			return fSelected;
		}

		// Stake Modifier (hash modifier of proof-of-stake):
		// The purpose of stake modifier is to prevent a txout (coin) owner from
		// computing future proof-of-stake generated by this txout at the time
		// of transaction confirmation. To meet kernel protocol, the txout
		// must hash with a future stake modifier to generate the proof.
		// Stake modifier consists of bits each of which is contributed from a
		// selected block of a given block group in the past.
		// The selection of a block is based on a hash of the block's proof-hash and
		// the previous stake modifier.
		// Stake modifier is recomputed at a fixed time interval instead of every 
		// block. This is to make it difficult for an attacker to gain control of
		// additional bits in the stake modifier, even after generating a chain of
		// blocks.
		private static bool ComputeNextStakeModifier(StakeChain stakeChain, ChainBase chainIndex, ChainedBlock pindexPrev, out ulong nStakeModifier,
			out bool fGeneratedStakeModifier)
		{
			nStakeModifier = 0;
			fGeneratedStakeModifier = false;
			if (pindexPrev == null)
			{
				fGeneratedStakeModifier = true;
				return true; // genesis block's modifier is 0
			}

			// First find current stake modifier and its generation block time
			// if it's not old enough, return the same stake modifier
			long nModifierTime = 0;
			if (!GetLastStakeModifier(stakeChain, pindexPrev, out nStakeModifier, out nModifierTime))
				return false; //error("ComputeNextStakeModifier: unable to get last modifier");
							  //LogPrint("stakemodifier", "ComputeNextStakeModifier: prev modifier=0x%016x time=%s\n", nStakeModifier, DateTimeStrFormat(nModifierTime));
			if (nModifierTime / ModifierInterval >= pindexPrev.Header.Time / ModifierInterval)
				return true;

			// Sort candidate blocks by timestamp
			var sortedByTimestamp = new SortedDictionary<uint, uint256>();
			long nSelectionInterval = GetStakeModifierSelectionInterval();
			long nSelectionIntervalStart = (pindexPrev.Header.Time / ModifierInterval) * ModifierInterval - nSelectionInterval;
			var pindex = pindexPrev;
			while (pindex != null && pindex.Header.Time >= nSelectionIntervalStart)
			{
				sortedByTimestamp.Add(pindex.Header.Time, pindex.HashBlock);
				pindex = pindex.Previous;
			}
			int nHeightFirstCandidate = pindex?.Height + 1 ?? 0;

			// Select 64 blocks from candidate blocks to generate stake modifier
			ulong nStakeModifierNew = 0;
			long nSelectionIntervalStop = nSelectionIntervalStart;
			var mapSelectedBlocks = new Dictionary<uint256, ChainedBlock>();
			for (int nRound = 0; nRound < Math.Min(64, sortedByTimestamp.Count); nRound++)
			{
				// add an interval section to the current selection round
				nSelectionIntervalStop += GetStakeModifierSelectionIntervalSection(nRound);

				// select a block from the candidates of current round
				if (!SelectBlockFromCandidates(stakeChain, pindexPrev, sortedByTimestamp, mapSelectedBlocks, nSelectionIntervalStop, nStakeModifier, out pindex))
					return false; // error("ComputeNextStakeModifier: unable to select block at round %d", nRound);

				// write the entropy bit of the selected block
				var blockStake = stakeChain.Get(pindex.HashBlock);
				nStakeModifierNew |= ((ulong)blockStake.GetStakeEntropyBit() << nRound);

				// add the selected block from candidates to selected list
				mapSelectedBlocks.Add(pindex.HashBlock, pindex);

				//LogPrint("stakemodifier", "ComputeNextStakeModifier: selected round %d stop=%s height=%d bit=%d\n", nRound, DateTimeStrFormat(nSelectionIntervalStop), pindex->nHeight, pindex->GetStakeEntropyBit());
			}

			//  // Print selection map for visualization of the selected blocks
			//  if (LogAcceptCategory("stakemodifier"))
			//  {
			//      string strSelectionMap = "";
			//      '-' indicates proof-of-work blocks not selected
			//      strSelectionMap.insert(0, pindexPrev->nHeight - nHeightFirstCandidate + 1, '-');
			//      pindex = pindexPrev;
			//      while (pindex && pindex->nHeight >= nHeightFirstCandidate)
			//      {
			//          // '=' indicates proof-of-stake blocks not selected
			//          if (pindex->IsProofOfStake())
			//              strSelectionMap.replace(pindex->nHeight - nHeightFirstCandidate, 1, "=");
			//          pindex = pindex->pprev;
			//      }

			//      BOOST_FOREACH(const PAIRTYPE(uint256, const CBlockIndex*)& item, mapSelectedBlocks)
			//      {
			//          // 'S' indicates selected proof-of-stake blocks
			//          // 'W' indicates selected proof-of-work blocks
			//          strSelectionMap.replace(item.second->nHeight - nHeightFirstCandidate, 1, item.second->IsProofOfStake()? "S" : "W");
			//      }

			//      LogPrintf("ComputeNextStakeModifier: selection height [%d, %d] map %s\n", nHeightFirstCandidate, pindexPrev->nHeight, strSelectionMap);
			//  }

			//LogPrint("stakemodifier", "ComputeNextStakeModifier: new modifier=0x%016x time=%s\n", nStakeModifierNew, DateTimeStrFormat(pindexPrev->GetBlockTime()));

			nStakeModifier = nStakeModifierNew;
			fGeneratedStakeModifier = true;
			return true;
		}

		// Stake Modifier (hash modifier of proof-of-stake):
		// The purpose of stake modifier is to prevent a txout (coin) owner from
		// computing future proof-of-stake generated by this txout at the time
		// of transaction confirmation. To meet kernel protocol, the txout
		// must hash with a future stake modifier to generate the proof.
		private static uint256 ComputeStakeModifierV2(ChainedBlock pindexPrev, BlockStake blockStakePrev, uint256 kernel)
		{
			if (pindexPrev == null)
				return 0; // genesis block's modifier is 0

			uint256 stakeModifier;
			using (var ms = new MemoryStream())
			{
				var serializer = new BitcoinStream(ms, true);
				serializer.ReadWrite(kernel);
				serializer.ReadWrite(blockStakePrev.StakeModifierV2);
				stakeModifier = Hashes.Hash256(ms.ToArray());
			}

			return stakeModifier;
		}

		// ppcoin: total coin age spent in transaction, in the unit of coin-days.
		// Only those coins meeting minimum age requirement counts. As those
		// transactions not in main chain are not currently indexed so we
		// might not find out about their coin age. Older transactions are 
		// guaranteed to be in main chain by sync-checkpoint. This rule is
		// introduced to help nodes establish a consistent view of the coin
		// age (trust score) of competing branches.
		public static bool GetCoinAge(IBlockRepository blockStore, ITransactionRepository trasnactionStore, IBlockTransactionMapStore mapStore,
			Transaction trx, ChainedBlock pindexPrev, out ulong nCoinAge)
		{

			BigInteger bnCentSecond = BigInteger.Zero;  // coin age in the unit of cent-seconds
			nCoinAge = 0;

			if (trx.IsCoinBase)
				return true;

			foreach (var txin in trx.Inputs)
			{
				// First try finding the previous transaction in database
				Transaction txPrev = trasnactionStore.Get(txin.PrevOut.Hash);
				if (txPrev == null)
					continue;  // previous transaction not in main chain
				if (trx.Time < txPrev.Time)
					return false;  // Transaction timestamp violation

				if (IsProtocolV3((int)trx.Time))
				{
					int nSpendDepth = 0;
					if (IsConfirmedInNPrevBlocks(blockStore, txPrev, pindexPrev, StakeMinConfirmations - 1, ref nSpendDepth))
					{
						//LogPrint("coinage", "coin age skip nSpendDepth=%d\n", nSpendDepth + 1);
						continue; // only count coins meeting min confirmations requirement
					}
				}
				else
				{
					// Read block header
					var block = blockStore.GetBlock(txPrev.GetHash());
					if (block == null)
						return false; // unable to read block of previous transaction
					if (block.Header.Time + StakeMinAge > trx.Time)
						continue; // only count coins meeting min age requirement
				}

				long nValueIn = txPrev.Outputs[txin.PrevOut.N].Value;
				var multiplier = BigInteger.ValueOf((trx.Time - txPrev.Time)/CENT);
				bnCentSecond = bnCentSecond.Add(BigInteger.ValueOf(nValueIn).Multiply(multiplier));
				//bnCentSecond += new BigInteger(nValueIn) * (trx.Time - txPrev.Time) / CENT;


				//LogPrint("coinage", "coin age nValueIn=%d nTimeDiff=%d bnCentSecond=%s\n", nValueIn, nTime - txPrev.nTime, bnCentSecond.ToString());
			}

			BigInteger bnCoinDay = bnCentSecond.Multiply(BigInteger.ValueOf(CENT / COIN / (24 * 60 * 60)));
			//BigInteger bnCoinDay = bnCentSecond * CENT / COIN / (24 * 60 * 60);

			//LogPrint("coinage", "coin age bnCoinDay=%s\n", bnCoinDay.ToString());
			nCoinAge = new Target(bnCoinDay).ToCompact();

			return true;
		}

		public static long GetProofOfWorkReward(ConcurrentChain chainIndex, long fees)
		{
			long PreMine = 98000000 * BlockValidator.COIN;

			if (chainIndex.Tip.Height == 1)
				return PreMine;

			return 4 * BlockValidator.COIN;
		}

		// miner's coin stake reward
		public static long GetProofOfStakeReward(ChainedBlock pindexPrev, ulong nCoinAge, long nFees)
		{
			return 1 * BlockValidator.COIN + nFees;
		}

	}
}
