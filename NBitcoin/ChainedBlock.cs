using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	//public enum BlockStatus : byte
	//{
	//	VALID_UNKNOWN = 0,
	//	VALID_HEADER = 1, // parsed, version ok, hash satisfies claimed PoW, 1 <= vtx count <= max, timestamp not in future
	//	VALID_TREE = 2, // parent found, difficulty matches, timestamp >= median previous, checkpoint
	//	VALID_TRANSACTIONS = 3, // only first tx is coinbase, 2 <= coinbase input script length <= 100, transactions valid, no duplicate txids, sigops, size, merkle root
	//	VALID_CHAIN = 4, // outputs do not overspend inputs, no double spends, coinbase output ok, immature coinbase spends, BIP30
	//	VALID_SCRIPTS = 5, // scripts/signatures ok
	//	VALID_MASK = 7,

	//	HAVE_DATA = 8, // full block available in blk*.dat
	//	HAVE_UNDO = 16, // undo data available in rev*.dat
	//	HAVE_MASK = 24,

	//	FAILED_VALID = 32, // stage after last reached validness failed
	//	FAILED_CHILD = 64, // descends from failed block
	//	FAILED_MASK = 96
	//}


	/** The block chain is a tree shaped structure starting with the
 * genesis block at the root, with each block potentially having multiple
 * candidates to be the next block. A blockindex may have multiple pprev pointing
 * to it, but at most one of them can be part of the currently active branch.
 */
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

		//DiskBlockPos nDataPos;

		//public DiskBlockPos BlockPosition
		//{
		//	get
		//	{
		//		return nDataPos;
		//	}
		//}

		// Byte offset within rev?????.dat where this block's undo data is stored
		//uint nUndoPos;

		// (memory only) Total amount of work (expected number of hashes) in the chain up to and including this block
		//uint256 nChainWork;

		// Number of transactions in this block.
		// Note: in a potential headers-first mode, this number cannot be relied upon
		//uint nTx;

		// (memory only) Number of transactions in the chain up to and including this block
		//ulong nChainTx; // change to 64-bit type when necessary; won't happen before 2030

		//// Verification status of this block. See enum BlockStatus
		//BlockStatus nStatus;

		//public BlockStatus Status
		//{
		//	get
		//	{
		//		return nStatus;
		//	}
		//}

		BlockHeader header;

		public BlockHeader Header
		{
			get
			{
				return header;
			}
		}




		// (memory only) Sequencial id assigned to distinguish order in which blocks are received.
		//uint nSequenceId;

		public ChainedBlock(BlockHeader header, uint256 headerHash, ChainedBlock previous)
		{
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

			return new BlockLocator(vHave);
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

		static readonly TimeSpan nTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
		static readonly TimeSpan nTargetSpacing = TimeSpan.FromSeconds(10 * 60);
		static readonly long nInterval = nTargetTimespan.Ticks / nTargetSpacing.Ticks;

		public Target GetWorkRequired(Network network)
		{
			// Genesis block
			if(Height == 0)
				return network.ProofOfWorkLimit;
			var nProofOfWorkLimit = new Target(network.ProofOfWorkLimit);
			var pindexLast = this.Previous;
			var height = Height;
			
			if(pindexLast == null)
				return nProofOfWorkLimit;

			// Only change once per interval
			if((height) % nInterval != 0)
			{
				if(network == Network.TestNet)
				{
					// Special difficulty rule for testnet:
					// If the new block's timestamp is more than 2* 10 minutes
					// then allow mining of a min-difficulty block.
					if(this.Header.BlockTime > pindexLast.Header.BlockTime + TimeSpan.FromTicks(nTargetSpacing.Ticks * 2))
						return nProofOfWorkLimit;
					else
					{
						// Return the last non-special-min-difficulty-rules-block
						ChainedBlock pindex = pindexLast;
						while(pindex.Previous != null && (pindex.Height % nInterval) != 0 && pindex.Header.Bits == nProofOfWorkLimit)
							pindex = pindex.Previous;
						return pindex.Header.Bits;
					}
				}
				return pindexLast.Header.Bits;
			}

			// Go back by what we want to be 14 days worth of blocks
			var pastHeight = pindexLast.Height - nInterval + 1;
			ChainedBlock pindexFirst = this.EnumerateToGenesis().FirstOrDefault(o=>o.Height == pastHeight);
			assert(pindexFirst);

			// Limit adjustment step
			var nActualTimespan = pindexLast.Header.BlockTime - pindexFirst.Header.BlockTime;
			if(nActualTimespan < TimeSpan.FromTicks(nTargetTimespan.Ticks / 4))
				nActualTimespan = TimeSpan.FromTicks(nTargetTimespan.Ticks / 4);
			if(nActualTimespan > TimeSpan.FromTicks(nTargetTimespan.Ticks * 4))
				nActualTimespan = TimeSpan.FromTicks(nTargetTimespan.Ticks * 4);

			// Retarget
			var bnNew = pindexLast.Header.Bits.ToBigInteger();
			bnNew *= (ulong)nActualTimespan.TotalSeconds;
			bnNew /= (ulong)nTargetTimespan.TotalSeconds;
			var newTarget = new Target(bnNew);
			if(newTarget > nProofOfWorkLimit)
				newTarget = nProofOfWorkLimit;

			return newTarget;
		}

		private void assert(object obj)
		{
			if(obj == null)
				throw new NotSupportedException("Can only calculate work of a full chain");
		}

		public bool Validate(Network network)
		{
			if(Height != 0 && Previous == null)
				return false;
			var heightCorrect = Height == 0 || Height == Previous.Height + 1;
			var genesisCorrect = Height == 0 ? HashBlock == network.GetGenesis().GetHash() : true;
			var hashPrevCorrect = Height == 0 ? true : Header.HashPrevBlock == Previous.HashBlock;
			var hashCorrect = HashBlock == Header.GetHash();
			var workCorrect = CheckProofOfWorkAndTarget(network);
			return heightCorrect && genesisCorrect && hashPrevCorrect && hashCorrect && workCorrect;
		}

		public bool CheckProofOfWorkAndTarget(Network network)
		{
			return Header.CheckProofOfWork() && Header.Bits <= GetWorkRequired(network);
		}
	}
}
