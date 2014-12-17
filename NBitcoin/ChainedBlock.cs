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

		public ChainedBlock(BlockHeader header,uint256 headerHash, ChainedBlock previous)
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
	}
}
