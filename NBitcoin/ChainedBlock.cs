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



		public ChainedBlock(BlockHeader header, uint256 headerHash, ChainedBlock previous)
		{
			if(header == null)
				throw new ArgumentNullException("header");
			if(previous != null)
			{
				nHeight = previous.Height + 1;
			}
			this.pprev = previous;
			//this.nDataPos = pos;
			this.header = header;
			this.phashBlock = headerHash ?? header.GetHash();

			if(previous == null)
			{
				if(header.HashPrevBlock != 0)
					throw new ArgumentException("Only the genesis block can have no previous block");
			}
			else
			{
				if(previous.HashBlock != header.HashPrevBlock)
					throw new ArgumentException("The previous block has not the expected hash");
			}
		}

		public ChainedBlock(BlockHeader header, int height)
		{
			if(header == null)
				throw new ArgumentNullException("header");
			nHeight = height;
			//this.nDataPos = pos;
			this.header = header;
			this.phashBlock = header.GetHash();
		}

		public BlockLocator GetLocator()
		{
			int nStep = 1;
			List<uint256> vHave = new List<uint256>();

			var pindex = this;
			while(pindex != null)
			{
				vHave.Add(pindex.HashBlock);
				// Stop when we have added the genesis block.
				if(pindex.Height == 0)
					break;
				// Exponentially larger steps back, plus the genesis block.
				int nHeight = Math.Max(pindex.Height - nStep, 0);
				while(pindex.Height > nHeight)
					pindex = pindex.Previous;
				if(vHave.Count > 10)
					nStep *= 2;
			}

			var locators = new BlockLocator();
			locators.Blocks = vHave;
			return locators;
		}

		public override bool Equals(object obj)
		{
			ChainedBlock item = obj as ChainedBlock;
			if(item == null)
				return false;
			return phashBlock.Equals(item.phashBlock);
		}
		public static bool operator ==(ChainedBlock a, ChainedBlock b)
		{
			if(System.Object.ReferenceEquals(a, b))
				return true;
			if(((object)a == null) || ((object)b == null))
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
			while(current != null)
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
			if(height > Height)
				throw new InvalidOperationException("Can only find blocks below or equals to current height");
			if(height < 0)
				throw new ArgumentOutOfRangeException("height");
			ChainedBlock currentBlock = this;
			while(height != currentBlock.Height)
			{
				currentBlock = currentBlock.Previous;
			}
			return currentBlock;
		}
		public ChainedBlock FindAncestorOrSelf(uint256 blockHash)
		{
			ChainedBlock currentBlock = this;
			while(currentBlock != null && currentBlock.HashBlock != blockHash)
			{
				currentBlock = currentBlock.Previous;
			}
			return currentBlock;
		}

		public Target GetWorkRequired(Network network)
		{
			// Genesis block
			if(Height == 0)
				return network.Consensus.PowLimit;
			var nProofOfWorkLimit = network.Consensus.PowLimit;
			var pindexLast = this.Previous;
			var height = Height;

			if(pindexLast == null)
				return nProofOfWorkLimit;

			// Only change once per interval
			if((height) % network.Consensus.DifficultyAdjustmentInterval != 0)
			{
				if(network.Consensus.PowAllowMinDifficultyBlocks)
				{
					// Special difficulty rule for testnet:
					// If the new block's timestamp is more than 2* 10 minutes
					// then allow mining of a min-difficulty block.
					if(this.Header.BlockTime > pindexLast.Header.BlockTime + TimeSpan.FromTicks(network.Consensus.PowTargetSpacing.Ticks * 2))
						return nProofOfWorkLimit;
					else
					{
						// Return the last non-special-min-difficulty-rules-block
						ChainedBlock pindex = pindexLast;
						while(pindex.Previous != null && (pindex.Height % network.Consensus.DifficultyAdjustmentInterval) != 0 && pindex.Header.Bits == nProofOfWorkLimit)
							pindex = pindex.Previous;
						return pindex.Header.Bits;
					}
				}
				return pindexLast.Header.Bits;
			}

			// Go back by what we want to be 14 days worth of blocks
			var pastHeight = pindexLast.Height - (network.Consensus.DifficultyAdjustmentInterval - 1);
			ChainedBlock pindexFirst = this.EnumerateToGenesis().FirstOrDefault(o => o.Height == pastHeight);
			assert(pindexFirst);

			if(network.Consensus.PowNoRetargeting)
				return pindexLast.header.Bits;

			// Limit adjustment step
			var nActualTimespan = pindexLast.Header.BlockTime - pindexFirst.Header.BlockTime;
			if(nActualTimespan < TimeSpan.FromTicks(network.Consensus.PowTargetTimespan.Ticks / 4))
				nActualTimespan = TimeSpan.FromTicks(network.Consensus.PowTargetTimespan.Ticks / 4);
			if(nActualTimespan > TimeSpan.FromTicks(network.Consensus.PowTargetTimespan.Ticks * 4))
				nActualTimespan = TimeSpan.FromTicks(network.Consensus.PowTargetTimespan.Ticks * 4);

			// Retarget
			var bnNew = pindexLast.Header.Bits.ToBigInteger();
			bnNew *= (ulong)nActualTimespan.TotalSeconds;
			bnNew /= (ulong)network.Consensus.PowTargetTimespan.TotalSeconds;
			var newTarget = new Target(bnNew);
			if(newTarget > nProofOfWorkLimit)
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
			for(int i = 0; i < nMedianTimeSpan && pindex != null; i++, pindex = pindex.Previous)
				pmedian[--pbegin] = pindex.Header.BlockTime;

			Array.Sort(pmedian);
			return pmedian[pbegin + ((pend - pbegin) / 2)];
		}

		private static void assert(object obj)
		{
			if(obj == null)
				throw new NotSupportedException("Can only calculate work of a full chain");
		}

		public bool Validate(Network network)
		{
			if(network == null)
				throw new ArgumentNullException("network");
			if(Height != 0 && Previous == null)
				return false;
			var heightCorrect = Height == 0 || Height == Previous.Height + 1;
			var genesisCorrect = Height != 0 || HashBlock == network.GetGenesis().GetHash();
			var hashPrevCorrect = Height == 0 || Header.HashPrevBlock == Previous.HashBlock;
			var hashCorrect = HashBlock == Header.GetHash();
			var workCorrect = CheckProofOfWorkAndTarget(network);
			return heightCorrect && genesisCorrect && hashPrevCorrect && hashCorrect && workCorrect;
		}

		public bool CheckProofOfWorkAndTarget(Network network)
		{
			return Height == 0 || (Header.CheckProofOfWork() && Header.Bits <= GetWorkRequired(network));
		}

		public ChainedBlock GetAncestor(int height)
		{
			if(height > Height || height < 0)
				return null;
			ChainedBlock current = this;

			while(true)
			{
				if(current.Height == height)
					return current;
				current = current.Previous;
			}
		}
	}
}
