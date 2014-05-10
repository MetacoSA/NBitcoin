using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class Chain
	{
		BlockIndex _Genesis;

		public Chain(Network network)
			: this(network.GetGenesis().Header)
		{

		}
		public Chain(Chain copied)
		{
			vChain = copied.vChain.ToList();
			index = copied.index.ToDictionary(k => k.Key, k => k.Value);
			_Genesis = vChain[0];
		}
		public Chain(BlockHeader genesis)
		{
			_Genesis = new BlockIndex(genesis, null);
			Clear();
		}

		private void Clear()
		{
			vChain.Clear();
			index.Clear();
			vChain.Add(_Genesis);
			index.Add(_Genesis.HashBlock, _Genesis);
		}

		List<BlockIndex> vChain = new List<BlockIndex>();
		Dictionary<uint256, BlockIndex> index = new Dictionary<uint256, BlockIndex>();

		public BlockIndex Genesis
		{
			get
			{
				return vChain[0];
			}
		}

		public BlockIndex Tip
		{
			get
			{
				return vChain[Height];
			}
		}

		public int Height
		{
			get
			{
				return vChain.Count - 1;
			}
		}

		public BlockIndex Get(uint256 hash)
		{
			BlockIndex pindex = null;
			index.TryGetValue(hash, out pindex);
			return pindex;
		}
		public BlockIndex GetOrAdd(BlockHeader header)
		{
			BlockIndex pindex = Get(header.GetHash());
			if(pindex != null)
				return pindex;
			BlockIndex previous = Get(header.HashPrevBlock);
			if(previous == null)
				return null;
			pindex = new BlockIndex(header, previous);
			index.AddOrReplace(pindex.HashBlock, pindex);
			if(pindex.Height > Tip.Height)
				SetTip(pindex);
			return pindex;
		}

		/// <summary>
		/// Change tip of the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public BlockIndex SetTip(BlockIndex pindex)
		{
			if(pindex == null)
			{
				Clear();
				return null;
			}

			foreach(var removed in vChain.Resize(pindex.Height + 1))
			{
				index.Remove(removed.HashBlock);
			}
			while(pindex != null && vChain[pindex.Height] != pindex)
			{
				var old = vChain[pindex.Height];
				vChain[pindex.Height] = pindex;
				if(old != null && index.ContainsKey(old.HashBlock))
				{
					index.Remove(old.HashBlock);
				}
				index.AddOrReplace(pindex.HashBlock, pindex);
				pindex = pindex.Previous;
			}
			return pindex;
		}


		public BlockIndex FindFork(BlockLocator locator)
		{
			// Find the first block the caller has in the main chain
			foreach(uint256 hash in locator.Blocks)
			{
				BlockIndex mi = null;
				if(index.TryGetValue(hash, out mi))
				{
					if(Contains(mi))
						return mi;
				}
			}
			return Genesis;
		}

		public BlockIndex GetBlock(int nHeight)
		{
			if(nHeight < 0 || nHeight >= (int)vChain.Count)
				return null;
			return vChain[nHeight];
		}

		public bool Contains(uint256 hash)
		{
			BlockIndex pindex = Get(hash);
			return pindex != null;
		}
		public bool Contains(BlockIndex blockIndex)
		{
			if(vChain.Count - 1 <= blockIndex.Height)
				return false;
			return vChain[blockIndex.Height] == blockIndex;
		}

		public bool Same(Chain chain)
		{
			return Tip.HashBlock == chain.Tip.HashBlock;
		}



		public Chain Clone()
		{
			return new Chain(this);
		}

		public IEnumerable<BlockIndex> ToEnumerable(bool fromTip)
		{
			if(fromTip)
			{
				BlockIndex b = Tip;
				while(b != _Genesis)
				{
					yield return b;
					b = b.Previous;
				}
				yield return b;
			}
			else
			{
				foreach(var b in vChain)
					yield return b;
			}
		}


		public IEnumerable<BlockIndex> EnumerateAfter(BlockIndex block)
		{
			for(int i = block.Height + 1; i < vChain.Count ; i++)
			{
				yield return vChain[i];
			}
		}
	}
}
