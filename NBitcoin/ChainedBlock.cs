using NBitcoin.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
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

		BigInteger _ChainWork;
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
				throw new ArgumentNullException("header");
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
			_ChainWork = (Previous == null ? BigInteger.Zero : Previous._ChainWork).Add(GetBlockProof());
		}

		static BigInteger Pow256 = BigInteger.ValueOf(2).Pow(256);
		private BigInteger GetBlockProof()
		{
			var bnTarget = Header.Bits.ToBigInteger();
			if (bnTarget.CompareTo(BigInteger.Zero) <= 0 || bnTarget.CompareTo(Pow256) >= 0)
				return BigInteger.Zero;
			// We need to compute 2**256 / (bnTarget+1), but we can't represent 2**256
			// as it's too large for a arith_uint256. However, as 2**256 is at least as large
			// as bnTarget+1, it is equal to ((2**256 - bnTarget - 1) / (bnTarget+1)) + 1,
			// or ~bnTarget / (nTarget+1) + 1.
			return ((Pow256.Subtract(bnTarget).Subtract(BigInteger.One)).Divide(bnTarget.Add(BigInteger.One))).Add(BigInteger.One);
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
			return GetWorkRequired(network.Consensus);
		}

		public Target GetNextWorkRequired(Network network)
		{
			return GetNextWorkRequired(network.Consensus);
		}
		public Target GetNextWorkRequired(Consensus consensus)
		{
			BlockHeader dummy = new BlockHeader();
			dummy.HashPrevBlock = this.HashBlock;
			dummy.BlockTime = DateTimeOffset.UtcNow;
			return GetNextWorkRequired(dummy, consensus);
		}

		public Target GetNextWorkRequired(BlockHeader block, Network network)
		{
			return GetNextWorkRequired(block, network.Consensus);
		}

		public Target GetNextWorkRequired(BlockHeader block, Consensus consensus)
		{
			return new ChainedBlock(block, block.GetHash(), this).GetWorkRequired(consensus);
		}

		public Target GetWorkRequired(Consensus consensus)
		{
			// Genesis block
			if (Height == 0)
				return consensus.PowLimit;
			var nProofOfWorkLimit = consensus.PowLimit;
			var pindexLast = this.Previous;
			var height = Height;

			if (pindexLast == null)
				return nProofOfWorkLimit;

			// Only change once per interval
			if ((height) % consensus.DifficultyAdjustmentInterval != 0)
			{
				if (consensus.PowAllowMinDifficultyBlocks)
				{
					// Special difficulty rule for testnet:
					// If the new block's timestamp is more than 2* 10 minutes
					// then allow mining of a min-difficulty block.
					if (this.Header.BlockTime > pindexLast.Header.BlockTime + TimeSpan.FromTicks(consensus.PowTargetSpacing.Ticks * 2))
						return nProofOfWorkLimit;
					else
					{
						// Return the last non-special-min-difficulty-rules-block
						ChainedBlock pindex = pindexLast;
						while (pindex.Previous != null && (pindex.Height % consensus.DifficultyAdjustmentInterval) != 0 && pindex.Header.Bits == nProofOfWorkLimit)
							pindex = pindex.Previous;
						return pindex.Header.Bits;
					}
				}
				return pindexLast.Header.Bits;
			}

			// Go back by what we want to be 14 days worth of blocks
			var pastHeight = pindexLast.Height - (consensus.DifficultyAdjustmentInterval - 1);
			ChainedBlock pindexFirst = this.EnumerateToGenesis().FirstOrDefault(o => o.Height == pastHeight);
			assert(pindexFirst);

			if (consensus.PowNoRetargeting)
				return pindexLast.header.Bits;

			// Limit adjustment step
			var nActualTimespan = pindexLast.Header.BlockTime - pindexFirst.Header.BlockTime;
			if (nActualTimespan < TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks / 4))
				nActualTimespan = TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks / 4);
			if (nActualTimespan > TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks * 4))
				nActualTimespan = TimeSpan.FromTicks(consensus.PowTargetTimespan.Ticks * 4);

			// Retarget
			var bnNew = pindexLast.Header.Bits.ToBigInteger();
			bnNew = bnNew.Multiply(BigInteger.ValueOf((long)nActualTimespan.TotalSeconds));
			bnNew = bnNew.Divide(BigInteger.ValueOf((long)consensus.PowTargetTimespan.TotalSeconds));
			var newTarget = new Target(bnNew);
			if (newTarget > nProofOfWorkLimit)
				newTarget = nProofOfWorkLimit;

			return newTarget;
		}


		const int nMedianTimeSpan = 11;
		public DateTimeOffset GetMedianTimePast()
		{
			DateTimeOffset[] pmedian = new DateTimeOffset[nMedianTimeSpan];
			int pbegin = nMedianTimeSpan;
			int pend = nMedianTimeSpan;

			ChainedBlock pindex = this;
			for (int i = 0; i < nMedianTimeSpan && pindex != null; i++, pindex = pindex.Previous)
				pmedian[--pbegin] = pindex.Header.BlockTime;

			Array.Sort(pmedian);
			return pmedian[pbegin + ((pend - pbegin) / 2)];
		}

		private static void assert(object obj)
		{
			if (obj == null)
				throw new NotSupportedException("Can only calculate work of a full chain");
		}

		/// <summary>
		/// Check PoW and that the blocks connect correctly
		/// </summary>
		/// <param name="network">The network being used</param>
		/// <returns>True if PoW is correct</returns>
		public bool Validate(Network network)
		{
			if (network == null)
				throw new ArgumentNullException("network");
			if (Height != 0 && Previous == null)
				return false;

			if (Block.BlockSignature)
				return BlockStake.Validate(network, this);

			var heightCorrect = Height == 0 || Height == Previous.Height + 1;
			var genesisCorrect = Height != 0 || HashBlock == network.GetGenesis().GetHash();
			var hashPrevCorrect = Height == 0 || Header.HashPrevBlock == Previous.HashBlock;
			var hashCorrect = HashBlock == Header.GetHash();
			var workCorrect = CheckProofOfWorkAndTarget(network);
			return heightCorrect && genesisCorrect && hashPrevCorrect && hashCorrect && workCorrect;
		}

		public bool CheckProofOfWorkAndTarget(Network network)
		{
			return CheckProofOfWorkAndTarget(network.Consensus);
		}

		public bool CheckProofOfWorkAndTarget(Consensus consensus)
		{
			return Height == 0 || (Header.CheckProofOfWork() && Header.Bits <= GetWorkRequired(consensus));
		}


		/// <summary>
		/// Find first common block between two chains
		/// </summary>
		/// <param name="block">The tip of the other chain</param>
		/// <returns>First common block or null</returns>
		public ChainedBlock FindFork(ChainedBlock block)
		{
			if (block == null)
				throw new ArgumentNullException("block");

			var highChain = this.Height > block.Height ? this : block;
			var lowChain = highChain == this ? block : this;
			while (highChain.Height != lowChain.Height)
			{
				highChain = highChain.Previous;
			}
			while (highChain.HashBlock != lowChain.HashBlock)
			{
				lowChain = lowChain.Previous;
				highChain = highChain.Previous;
				if (lowChain == null || highChain == null)
					return null;
			}
			return highChain;
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
	}
}
