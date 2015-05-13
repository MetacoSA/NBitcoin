using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class ChainBase
	{
		public virtual ChainedBlock Genesis
		{
			get
			{
				return GetBlock(0);
			}
		}
		public abstract ChainedBlock GetBlock(uint256 id);
		public abstract ChainedBlock GetBlock(int height);
		public abstract ChainedBlock Tip
		{
			get;
		}
		public abstract int Height
		{
			get;
		}

		public bool Contains(uint256 hash)
		{
			ChainedBlock pindex = GetBlock(hash);
			return pindex != null;
		}


		public IEnumerable<ChainedBlock> ToEnumerable(bool fromTip)
		{
			if(fromTip)
			{
				ChainedBlock b = Tip;
				while(b != null)
				{
					yield return b;
					b = b.Previous;
				}
			}
			else
			{
				foreach(var b in EnumerateFromStart())
					yield return b;
			}
		}

		public ChainedBlock SetTip(ChainBase otherChain)
		{
			return SetTip(otherChain.Tip);
		}
		public bool SetTip(BlockHeader header)
		{
			ChainedBlock chainedHeader;
			return TrySetTip(header, out chainedHeader);
		}

		public bool TrySetTip(BlockHeader header, out ChainedBlock chainedHeader)
		{
			chainedHeader = null;
			var prev = GetBlock(header.HashPrevBlock);
			if(prev == null)
				return false;
			chainedHeader = new ChainedBlock(header, header.GetHash(), GetBlock(header.HashPrevBlock));
			SetTip(chainedHeader);
			return true;
		}

		protected abstract IEnumerable<ChainedBlock> EnumerateFromStart();

		public bool Contains(ChainedBlock blockIndex)
		{
			return GetBlock(blockIndex.Height) != null;
		}

		public bool SameTip(ChainBase chain)
		{
			return Tip.HashBlock == chain.Tip.HashBlock;
		}

		
		public Target GetWorkRequired(Network network, int height)
		{
			return GetBlock(height).GetWorkRequired(network);
		}

		public bool Validate(Network network, bool fullChain = true)
		{
			var tip = Tip;
			if(tip == null)
				return false;
			if(!fullChain)
				return tip.Validate(network);
			else
			{
				foreach(var block in tip.EnumerateToGenesis())
				{
					if(!block.Validate(network))
						return false;
				}
				return true;
			}
		}



		public ChainedBlock FindFork(ChainBase chain)
		{
			return FindFork(chain.ToEnumerable(true).Select(o => o.HashBlock));
		}

		public ChainedBlock FindFork(IEnumerable<uint256> hashes)
		{
			// Find the first block the caller has in the main chain
			foreach(uint256 hash in hashes)
			{
				ChainedBlock mi = GetBlock(hash);
				if(mi != null)
				{
					return mi;
				}
			}
			return Genesis;
		}
#if !PORTABLE
		public ChainedBlock FindFork(BlockLocator locator)
		{
			return FindFork(locator.Blocks);
		}
#endif

		public IEnumerable<ChainedBlock> EnumerateAfter(uint256 blockHash)
		{
			var block = GetBlock(blockHash);
			if(block == null)
				return new ChainedBlock[0];
			return EnumerateAfter(block);
		}

		public IEnumerable<ChainedBlock> EnumerateToTip(ChainedBlock block)
		{
			return EnumerateToTip(block.HashBlock);
		}
		public IEnumerable<ChainedBlock> EnumerateToTip(uint256 blockHash)
		{
			var block = GetBlock(blockHash);
			if(block == null)
				yield break;
			yield return block;
			foreach(var r in EnumerateAfter(blockHash))
				yield return r;
		}

		public virtual IEnumerable<ChainedBlock> EnumerateAfter(ChainedBlock block)
		{
			int i = block.Height + 1;
			var prev = block;
			while(true)
			{
				var b = GetBlock(i);
				if(b == null || b.Previous != prev)
					yield break;
				yield return b;
				i++;
				prev = b;
			}
		}

		/// <summary>
		/// Force a new tip for the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public abstract ChainedBlock SetTip(ChainedBlock pindex);
	}
}
