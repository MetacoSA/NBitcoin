using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	using System.Runtime.CompilerServices;

	using NBitcoin.BouncyCastle.Math;

	

	/// <summary>
	/// A BlockHeader chained with all its ancestors
	/// </summary>
	public class ChainedBlock
	{
		// pointer to the hash of the block, if any. memory is owned by this CBlockIndex
		uint256 phashBlock;

		public uint256 HashBlock
		{
			get
			{
				return phashBlock;
			}
		}


		// pointer to the index of the predecessor of this block
		ChainedBlock pprev;

		public ChainedBlock Previous
		{
			get
			{
				return pprev;
			}
		}

		// height of the entry in the chain. The genesis block has height 0
		int nHeight;

		public int Height
		{
			get
			{
				return nHeight;
			}
		}


		BlockHeader header;

		public BlockHeader Header
		{
			get
			{
				return header;
			}
		}

		System.Numerics.BigInteger _ChainWork;

		public uint256 ChainWork
		{
			get
			{
				return Target.ToUInt256(_ChainWork);
			}
		}

		public ChainedBlock(BlockHeader header, uint256 headerHash, ChainedBlock previous)
		{
			if (header == null)
				throw new ArgumentNullException(nameof(header));

			if (previous != null)
			{
				nHeight = previous.Height + 1;
			}
			this.pprev = previous;
			//this.nDataPos = pos;
			this.header = header;
			this.phashBlock = headerHash ?? header.GetHash();

			if (previous == null)
			{
				if (header.HashPrevBlock != uint256.Zero)
					throw new ArgumentException("Only the genesis block can have no previous block");
			}
			else
			{
				if (previous.HashBlock != header.HashPrevBlock)
					throw new ArgumentException("The previous block has not the expected hash");
			}

			CalculateChainWork();
		}

		private void CalculateChainWork()
		{
			_ChainWork = (Previous == null ? System.Numerics.BigInteger.Zero : Previous._ChainWork) + GetBlockProof();
		}

		static System.Numerics.BigInteger Pow256 = System.Numerics.BigInteger.Pow(2, 256);

		private System.Numerics.BigInteger GetBlockProof()
		{
			var bnTarget = Header.Bits.ToBigInteger();
			if (bnTarget <= System.Numerics.BigInteger.Zero || bnTarget >= Pow256) return System.Numerics.BigInteger.Zero;
			// We need to compute 2**256 / (bnTarget+1), but we can't represent 2**256
			// as it's too large for a arith_uint256. However, as 2**256 is at least as large
			// as bnTarget+1, it is equal to ((2**256 - bnTarget - 1) / (bnTarget+1)) + 1,
			// or ~bnTarget / (nTarget+1) + 1.
			return ((Pow256 - bnTarget - 1) / (bnTarget + 1)) + 1;
		}

		public ChainedBlock(BlockHeader header, int height)
		{
			if (header == null)
				throw new ArgumentNullException("header");

			nHeight = height;
			//this.nDataPos = pos;
			this.header = header;
			this.phashBlock = header.GetHash();
			CalculateChainWork();
		}

		public BlockLocator GetLocator()
		{
			int nStep = 1;
			List<uint256> vHave = new List<uint256>();

			var pindex = this;
			while (pindex != null)
			{
				vHave.Add(pindex.HashBlock);
				
				// Stop when we have added the genesis block.
				if (pindex.Height == 0)
					break;

				// Exponentially larger steps back, plus the genesis block.
				int nHeight = Math.Max(pindex.Height - nStep, 0);
				while (pindex.Height > nHeight)
					pindex = pindex.Previous;
				if (vHave.Count > 10)
					nStep *= 2;
			}

			var locators = new BlockLocator();
			locators.Blocks = vHave;
			return locators;
		}

		public override bool Equals(object obj)
		{
			ChainedBlock item = obj as ChainedBlock;
			if (item == null)
				return false;
			return phashBlock.Equals(item.phashBlock);
		}

		public static bool operator ==(ChainedBlock a, ChainedBlock b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;

			if (((object)a == null) || ((object)b == null))
				return false;

			return a.phashBlock == b.phashBlock;
		}

		public static bool operator !=(ChainedBlock a, ChainedBlock b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return phashBlock.GetHashCode();
		}



		public IEnumerable<ChainedBlock> EnumerateToGenesis()
		{
			var current = this;
			while (current != null)
			{
				yield return current;
				current = current.Previous;
			}
		}

		public override string ToString()
		{
			return Height + " - " + HashBlock;
		}

		public ChainedBlock FindAncestorOrSelf(int height)
		{
			if (height > Height)
				throw new InvalidOperationException("Can only find blocks below or equals to current height");
			if (height < 0)
				throw new ArgumentOutOfRangeException("height");

			ChainedBlock currentBlock = this;
			while (height != currentBlock.Height)
			{
				currentBlock = currentBlock.Previous;
			}
			return currentBlock;
		}

		public ChainedBlock FindAncestorOrSelf(uint256 blockHash)
		{
			ChainedBlock currentBlock = this;
			while (currentBlock != null && currentBlock.HashBlock != blockHash)
			{
				currentBlock = currentBlock.Previous;
			}
			return currentBlock;
		}

		public Target GetWorkRequired(Network network)
		{
			return this.GetWorkRequired(network.Consensus);
		}

		const int nMedianTimeSpan = 11;

		public DateTimeOffset GetMedianTimePast()
		{
			DateTimeOffset[] pmedian = new DateTimeOffset[nMedianTimeSpan];
			int pbegin = nMedianTimeSpan;
			int pend = nMedianTimeSpan;

			ChainedBlock pindex = this;
			for (int i = 0; i < nMedianTimeSpan && pindex != null; i++, pindex = pindex.Previous) pmedian[--pbegin] = pindex.Header.BlockTime;

			Array.Sort(pmedian);
			return pmedian[pbegin + ((pend - pbegin) / 2)];
		}

		private static void assert(object obj)
		{
			if (obj == null)
				throw new NotSupportedException("Can only calculate work of a full chain");
		}

		public bool Validate(Network network)
		{
			if (network == null)
				throw new ArgumentNullException("network");
			if (Height != 0 && Previous == null)
				return false;
			var heightCorrect = Height == 0 || Height == Previous.Height + 1;
			var genesisCorrect = Height != 0 || HashBlock == network.GetGenesis().GetHash();
			var hashPrevCorrect = Height == 0 || Header.HashPrevBlock == Previous.HashBlock;
			var hashCorrect = HashBlock == Header.GetHash();
			var workCorrect = this.CheckPowPosAndTarget(network);
			return heightCorrect && genesisCorrect && hashPrevCorrect && hashCorrect && workCorrect;
		}

		public bool CheckPowPosAndTarget(Network network)
		{
			return this.CheckPowPosAndTarget(network.Consensus);
		}

		public bool CheckPowPosAndTarget(Consensus consensus)
		{
			if (this.Height == 0)
				return true;

			if (this.Header.PosParameters.IsProofOfWork() && !this.Header.CheckProofOfWork())
				return false;

			return this.Header.Bits == this.GetWorkRequired(consensus);
		}

		public Target GetWorkRequired(Consensus consensus)
		{
			this.EnsurePosHeader();
			return GetNextTargetRequired(this.Previous, consensus, this.Header.PosParameters.IsProofOfStake());
		}

		public ChainedBlock GetAncestor(int height)
		{
			if (height > Height || height < 0)
				return null;

			ChainedBlock current = this;
			while (true)
			{
				if (current.Height == height)
					return current;
				current = current.Previous;
			}
		}

		// POS methods
		// -------------------------------------

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

			while (index.pprev != null && (index.header.PosParameters.IsProofOfStake() != proofOfStake))
				index = index.pprev;

			return index;
		}

		public static Target GetNextTargetRequired(ChainedBlock indexLast, Consensus consensus, bool proofOfStake)
		{
			// Genesis block
			if (indexLast == null)
				return consensus.PowLimit;

			// find the last two blocks that correspond to the mining algo 
			// (i.e if this is a POS block we need to find the last two POS blocks)
			var targetLimit = proofOfStake ? GetProofOfStakeLimit(consensus, indexLast.Height) : consensus.PowLimit.ToBigInteger();

			// first block
			var pindexPrev = GetLastBlockIndex(indexLast, proofOfStake);
			if (pindexPrev.pprev == null)
				return new Target(targetLimit);

			// second block
			var pindexPrevPrev = GetLastBlockIndex(pindexPrev.pprev, proofOfStake);
			if (pindexPrevPrev == null)
				return new Target(targetLimit);


			int targetSpacing = GetTargetSpacing(indexLast.Height);
			int actualSpacing = (int)(pindexPrev.header.Time - pindexPrevPrev.header.Time);
			if (IsProtocolV1RetargetingFixed(indexLast.Height))
			{
				if (actualSpacing < 0) actualSpacing = targetSpacing;
			}
			if (IsProtocolV3((int)indexLast.header.Time))
			{
				if (actualSpacing > targetSpacing * 10) actualSpacing = targetSpacing * 10;
			}

			// target change every block
			// retarget with exponential moving toward target spacing
			var targetTimespan = 16 * 60; // 16 mins
			var target = pindexPrev.Header.Bits.ToBigInteger();

			int interval = targetTimespan / targetSpacing;
			target *= ((interval - 1) * targetSpacing + actualSpacing + actualSpacing);
			target /= ((interval + 1) * targetSpacing);

			if (target <= 0 || target > targetLimit)
				target = targetLimit;

			return new Target(target);
		}

		private void EnsurePosHeader()
		{
			if (this.Header.PosParameters == null)
				throw new MissingFieldException("PosParameters");
		}
	}
}
