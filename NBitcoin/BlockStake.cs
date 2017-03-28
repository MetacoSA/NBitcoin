using System;
using System.Linq;

namespace NBitcoin
{
	[Flags]
	public enum BlockFlag //block index flags

	{
		BLOCK_PROOF_OF_STAKE = (1 << 0), // is proof-of-stake block
		BLOCK_STAKE_ENTROPY = (1 << 1), // entropy bit for stake modifier
		BLOCK_STAKE_MODIFIER = (1 << 2), // regenerated stake modifier
	};

	public class BlockStake : IBitcoinSerializable
	{
		public int Mint;

		public OutPoint PrevoutStake;

		public uint StakeTime;

		public ulong StakeModifier; // hash modifier for proof-of-stake

		public uint256 StakeModifierV2;

		private int flags;

		public uint256 HashProof;

		public BlockStake(Block block)
		{
			this.StakeModifierV2 = uint256.Zero;
			this.HashProof = uint256.Zero;

			if (IsProofOfStake(block))
			{
				this.SetProofOfStake();
				this.StakeTime = block.Transactions[1].Time;
				this.PrevoutStake = block.Transactions[1].Inputs[0].PrevOut;
			}
		}

		public BlockFlag Flags
		{
			get
			{
				return (BlockFlag)this.flags;
			}
			set
			{
				this.flags = (int)value;
			}
		}

		public static bool IsProofOfStake(Block block)
		{
			return block.Transactions.Count > 1 && block.Transactions[1].IsCoinStake;
		}

		public static bool IsProofOfWork(Block block)
		{
			return !IsProofOfStake(block);
		}

		public static Tuple<OutPoint, ulong> GetProofOfStake(Block block)
		{
			return IsProofOfStake(block) ?
			new Tuple<OutPoint, ulong>(block.Transactions[1].Inputs.First().PrevOut, block.Transactions[1].LockTime) :
			new Tuple<OutPoint, ulong>(new OutPoint(), (ulong)0);
		}

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref this.flags);
			stream.ReadWrite(ref this.Mint);
			stream.ReadWrite(ref this.StakeModifier);
			stream.ReadWrite(ref this.StakeModifierV2);
			if (this.IsProofOfStake())
			{
				stream.ReadWrite(ref this.PrevoutStake);
				stream.ReadWrite(ref this.StakeTime);
			}
			stream.ReadWrite(ref this.HashProof);
		}

		public bool IsProofOfWork()
		{
			return !((this.Flags & BlockFlag.BLOCK_PROOF_OF_STAKE) > 0);
		}

		public bool IsProofOfStake()
		{
			return (this.Flags & BlockFlag.BLOCK_PROOF_OF_STAKE) > 0;
		}

		public void SetProofOfStake()
		{
			this.Flags |= BlockFlag.BLOCK_PROOF_OF_STAKE;
		}

		public uint GetStakeEntropyBit()
		{
			return (uint)(this.Flags & BlockFlag.BLOCK_STAKE_ENTROPY) >> 1;
		}

		public bool SetStakeEntropyBit(uint nEntropyBit)
		{
			if (nEntropyBit > 1)
				return false;
			this.Flags |= (nEntropyBit != 0 ? BlockFlag.BLOCK_STAKE_ENTROPY : 0);
			return true;
		}

		public bool GeneratedStakeModifier()
		{
			return (this.Flags & BlockFlag.BLOCK_STAKE_MODIFIER) > 0;
		}

		public void SetStakeModifier(ulong modifier, bool fGeneratedStakeModifier)
		{
			this.StakeModifier = modifier;
			if (fGeneratedStakeModifier)
				this.Flags |= BlockFlag.BLOCK_STAKE_MODIFIER;
		}

		public static bool Check(Block block)
		{
			return block.CheckMerkleRoot() && BlockStake.CheckProofOfWork(block) && BlockStake.CheckProofOfStake(block);
		}

		public static bool CheckProofOfWork(Block block)
		{
			// if POS return true else check POW algo
			return IsProofOfStake(block) || block.Header.CheckProofOfWork();
		}

		public static bool CheckProofOfStake(Block block)
		{
			// todo: move this to the full node code.
			// this code is not the full check of POS 
			// full POS check will be introduced with the full node

			if (IsProofOfWork(block))
				return true;

			// Coinbase output should be empty if proof-of-stake block
			if (block.Transactions[0].Outputs.Count != 1 || !block.Transactions[0].Outputs[0].IsEmpty)
				return false;

			// Second transaction must be coinstake, the rest must not be
			if (!block.Transactions[1].IsCoinStake)
				return false;

			if (block.Transactions.Skip(2).Any(t => t.IsCoinStake))
				return false;

			return true;
		}

		public static ulong GetStakeEntropyBit(Block block)
		{
			// Take last bit of block hash as entropy bit
			ulong nEntropyBit = (block.GetHash().GetLow64() & (ulong)1);

			//LogPrint("stakemodifier", "GetStakeEntropyBit: hashBlock=%s nEntropyBit=%u\n", GetHash().ToString(), nEntropyBit);
			return nEntropyBit;
		}

		/// <summary>
		/// Check PoW and that the blocks connect correctly
		/// </summary>
		/// <param name="network">The network being used</param>
		/// <returns>True if PoW is correct</returns>
		public static bool Validate(Network network, ChainedBlock chainedBlock, StakeChain stakeChain = null)
		{
			if (network == null)
				throw new ArgumentNullException("network");
			if (chainedBlock.Height != 0 && chainedBlock.Previous == null)
				return false;
			var heightCorrect = chainedBlock.Height == 0 || chainedBlock.Height == chainedBlock.Previous.Height + 1;
			var genesisCorrect = chainedBlock.Height != 0 || chainedBlock.HashBlock == network.GetGenesis().GetHash();
			var hashPrevCorrect = chainedBlock.Height == 0 || chainedBlock.Header.HashPrevBlock == chainedBlock.Previous.HashBlock;
			var hashCorrect = chainedBlock.HashBlock == chainedBlock.Header.GetHash();

			if (stakeChain == null)
				return heightCorrect && genesisCorrect && hashPrevCorrect && hashCorrect;

			var workCorrect = stakeChain.CheckPowPosAndTarget(chainedBlock, stakeChain.Get(chainedBlock.HashBlock), network);
			return heightCorrect && genesisCorrect && hashPrevCorrect && hashCorrect && workCorrect;
		}
	}

	public partial class Block
	{
		public static bool BlockSignature = false;

		// block signature - signed by one of the coin base txout[N]'s owner
		private BlockSignature blockSignature = new BlockSignature();

		public BlockSignature BlockSignatur
		{
			get { return this.blockSignature; }
			set { this.blockSignature = value; }
		}
	}

	public abstract class StakeChain
	{
		public abstract BlockStake Get(uint256 blockid);

		public abstract void Set(uint256 blockid, BlockStake blockStake);

		public bool CheckPowPosAndTarget(ChainedBlock chainedBlock, BlockStake blockStake, Network network)
		{
			return this.CheckPowPosAndTarget(chainedBlock, blockStake, network.Consensus);
		}

		public bool CheckPowPosAndTarget(ChainedBlock chainedBlock, BlockStake blockStake, Consensus consensus)
		{
			if (chainedBlock.Height == 0)
				return true;

			if (blockStake.IsProofOfWork() && !chainedBlock.Header.CheckProofOfWork())
				return false;

			return chainedBlock.Header.Bits == this.GetWorkRequired(chainedBlock, blockStake, consensus);
		}

		public Target GetWorkRequired(ChainedBlock chainedBlock, BlockStake blockStake, Consensus consensus)
		{
			return BlockValidator.GetNextTargetRequired(this, chainedBlock.Previous, consensus, blockStake.IsProofOfStake());
		}
	}

	public static class StakeExtentions
	{
		public static bool CheckProofOfStake(this Block block)
		{
			return BlockStake.CheckProofOfStake(block);
		}
	}
}