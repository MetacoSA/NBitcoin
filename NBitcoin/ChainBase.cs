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
			if (fromTip)
			{
				foreach (var b in Tip.EnumerateToGenesis())
				{
					yield return b;
				}
			}
			else
			{
				foreach (var b in EnumerateFromStart())
					yield return b;
			}
		}

		public ChainedBlock SetTip(ChainBase otherChain)
		{
			if (otherChain == null)
				throw new ArgumentNullException(nameof(otherChain));
			return SetTip(otherChain.Tip);
		}

		public bool SetTip(BlockHeader header)
		{
			ChainedBlock chainedHeader;
			return TrySetTip(header, out chainedHeader);
		}

		public bool TrySetTip(BlockHeader header, out ChainedBlock chainedHeader)
		{
			if (header == null)
				throw new ArgumentNullException(nameof(header));
			chainedHeader = null;
			var prev = GetBlock(header.HashPrevBlock);
			if (prev == null)
				return false;
			chainedHeader = new ChainedBlock(header, header.GetHash(), GetBlock(header.HashPrevBlock));
			SetTip(chainedHeader);
			return true;
		}

		protected abstract IEnumerable<ChainedBlock> EnumerateFromStart();

		public bool Contains(ChainedBlock blockIndex)
		{
			if (blockIndex == null)
				throw new ArgumentNullException(nameof(blockIndex));
			return GetBlock(blockIndex.Height) != null;
		}

		public bool SameTip(ChainBase chain)
		{
			if (chain == null)
				throw new ArgumentNullException(nameof(chain));
			return Tip.HashBlock == chain.Tip.HashBlock;
		}


		public Target GetWorkRequired(Network network, int height)
		{
			return GetBlock(height).GetWorkRequired(network);
		}

		public bool Validate(Network network, bool fullChain = true)
		{
			var tip = Tip;
			if (tip == null)
				return false;
			if (!fullChain)
				return tip.Validate(network);
			else
			{
				foreach (var block in tip.EnumerateToGenesis())
				{
					if (!block.Validate(network))
						return false;
				}
				return true;
			}
		}


		/// <summary>
		/// Returns the first common block between two chains
		/// </summary>
		/// <param name="chain">The other chain</param>
		/// <returns>First common block or null</returns>
		public ChainedBlock FindFork(ChainBase chain)
		{
			if (chain == null)
				throw new ArgumentNullException(nameof(chain));
			return FindFork(chain.Tip.EnumerateToGenesis().Select(o => o.HashBlock));
		}

		/// <summary>
		/// Returns the first found block
		/// </summary>
		/// <param name="hashes">Hash to search for</param>
		/// <returns>First found block or null</returns>
		public ChainedBlock FindFork(IEnumerable<uint256> hashes)
		{
			if (hashes == null)
				throw new ArgumentNullException(nameof(hashes));
			// Find the first block the caller has in the main chain
			foreach (uint256 hash in hashes)
			{
				ChainedBlock mi = GetBlock(hash);
				if (mi != null)
				{
					return mi;
				}
			}
			return null;
		}

		public ChainedBlock FindFork(BlockLocator locator)
		{
			if (locator == null)
				throw new ArgumentNullException(nameof(locator));
			return FindFork(locator.Blocks);
		}

		public IEnumerable<ChainedBlock> EnumerateAfter(uint256 blockHash)
		{
			var block = GetBlock(blockHash);
			if (block == null)
				return new ChainedBlock[0];
			return EnumerateAfter(block);
		}

		public IEnumerable<ChainedBlock> EnumerateToTip(ChainedBlock block)
		{
			if (block == null)
				throw new ArgumentNullException(nameof(block));
			return EnumerateToTip(block.HashBlock);
		}

		public IEnumerable<ChainedBlock> EnumerateToTip(uint256 blockHash)
		{
			var block = GetBlock(blockHash);
			if (block == null)
				yield break;
			yield return block;
			foreach (var r in EnumerateAfter(blockHash))
				yield return r;
		}

		public virtual IEnumerable<ChainedBlock> EnumerateAfter(ChainedBlock block)
		{
			int i = block.Height + 1;
			var prev = block;
			while (true)
			{
				var b = GetBlock(i);
				if (b == null || b.Previous != prev)
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
