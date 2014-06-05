using NBitcoin.Scanning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class ChainChange : IBitcoinSerializable
	{
		bool _Add;
		public bool Add
		{
			get
			{
				return _Add;
			}
			set
			{
				_Add = value;
			}
		}
		uint _HeightOrBackstep;
		public uint HeightOrBackstep
		{
			get
			{
				return _HeightOrBackstep;
			}
			set
			{
				_HeightOrBackstep = value;
			}
		}
		BlockHeader _BlockHeader;
		public BlockHeader BlockHeader
		{
			get
			{
				return _BlockHeader;
			}
			set
			{
				_BlockHeader = value;
			}
		}
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Add);
			stream.ReadWriteAsCompactVarInt(ref _HeightOrBackstep);
			if(_Add)
			{
				stream.ReadWrite(ref _BlockHeader);
			}
		}

		#endregion
	}
	public class Chain
	{
		public static Chain Load(ObjectStream<ChainChange> changes)
		{
			return Load(changes.Enumerate());
		}

		public static Chain Load(IEnumerable<ChainChange> changes)
		{
			Chain chain = null;
			foreach(var change in changes)
			{
				if(chain == null)
				{
					if(change.BlockHeader == null)
						throw new InvalidOperationException("Previous chain changes missing");
					chain = new Chain(change.BlockHeader, (int)change.HeightOrBackstep);
				}
				else
				{
					if(change.Add)
					{
						var result = chain.GetOrAdd(change.BlockHeader);
						if(result == null || result.Height != change.HeightOrBackstep)
							throw new InvalidOperationException("Invalid height in the chain change");
					}
					else
					{
						var back = chain.GetBlock((int)(chain.Height - change.HeightOrBackstep));
						if(back == null)
							throw new InvalidOperationException("Previous chain changes missing");
						chain.SetTip(back);
					}
				}
			}
			return chain;
		}

		public void Write(int startHeight, ObjectStream<ChainChange> output)
		{
			if(startHeight < StartHeight)
				throw new ArgumentException("startHeight should be equal or higher than the StartHeight of this chain", "startHeight");

			int height = startHeight;
			while(height <= Height)
			{
				var block = this.GetBlock(height);
				output.WriteNext(new ChainChange()
				{
					Add = true,
					BlockHeader = block.Header,
					HeightOrBackstep = (uint)block.Height
				});
				height++;
			}
		}

		BlockIndex _StartBlock;
		public bool IsPartial
		{
			get
			{
				return StartHeight != 0;
			}
		}

		public Chain(Network network)
			: this(network.GetGenesis().Header)
		{

		}
		public Chain(Chain copied)
		{
			vChain = copied.vChain.ToList();
			index = copied.index.ToDictionary(k => k.Key, k => k.Value);
			_StartBlock = vChain[0];
		}
		public Chain(BlockHeader genesis)
		{
			_StartBlock = new BlockIndex(genesis, null);
			Clear();
		}

		public Chain(BlockHeader blockHeader, int height)
		{
			StartHeight = height;
			_StartBlock = new BlockIndex(blockHeader, height);
			Clear();
		}

		private void Clear()
		{
			vChain.Clear();
			index.Clear();
			vChain.Add(_StartBlock);
			index.Add(_StartBlock.HashBlock, _StartBlock);

		}

		List<BlockIndex> vChain = new List<BlockIndex>();
		Dictionary<uint256, BlockIndex> index = new Dictionary<uint256, BlockIndex>();

		public int StartHeight
		{
			get;
			set;
		}

		public BlockIndex Genesis
		{
			get
			{
				if(StartHeight == 0)
					return vChain[0];
				return null;
			}
		}

		public BlockIndex Tip
		{
			get
			{
				return vChain[Height - StartHeight];
			}
		}

		public int Height
		{
			get
			{
				return vChain.Count - 1 + StartHeight;
			}
		}

		public BlockIndex GetBlock(uint256 hash)
		{
			BlockIndex pindex = null;
			index.TryGetValue(hash, out pindex);
			return pindex;
		}
		public BlockIndex GetOrAdd(BlockHeader header)
		{
			BlockIndex pindex = GetBlock(header.GetHash());
			if(pindex != null)
				return pindex;
			BlockIndex previous = GetBlock(header.HashPrevBlock);
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

			foreach(var removed in vChain.Resize(pindex.Height + 1 - StartHeight))
			{
				index.Remove(removed.HashBlock);
			}
			while(pindex != null && vChain[pindex.Height - StartHeight] != pindex)
			{
				var old = vChain[pindex.Height - StartHeight];
				vChain[pindex.Height - StartHeight] = pindex;
				if(old != null && index.ContainsKey(old.HashBlock))
				{
					index.Remove(old.HashBlock);
				}
				index.AddOrReplace(pindex.HashBlock, pindex);
				pindex = pindex.Previous;
			}
			return pindex;
		}


		public BlockIndex FindFork(Chain chain)
		{
			return FindFork(chain.ToEnumerable(true).Select(o => o.HashBlock));
		}

		public BlockIndex FindFork(IEnumerable<uint256> hashes)
		{
			// Find the first block the caller has in the main chain
			foreach(uint256 hash in hashes)
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

		public BlockIndex FindFork(BlockLocator locator)
		{
			return FindFork(locator.Blocks);
		}

		public BlockIndex GetBlock(int nHeight)
		{
			var index = nHeight - StartHeight;
			if(index < 0 || index >= (int)vChain.Count)
				return null;
			return vChain[index];
		}

		public bool Contains(uint256 hash)
		{
			BlockIndex pindex = GetBlock(hash);
			return pindex != null;
		}
		public bool Contains(BlockIndex blockIndex)
		{
			if(StartHeight <= blockIndex.Height && blockIndex.Height <= Height)
			{
				return vChain[blockIndex.Height - StartHeight] == blockIndex;
			}
			else
				return false;
		}

		public bool SameTip(Chain chain)
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
				while(b != null)
				{
					yield return b;
					b = b.Previous;
				}
			}
			else
			{
				foreach(var b in vChain)
					yield return b;
			}
		}


		public IEnumerable<BlockIndex> EnumerateAfter(BlockIndex block)
		{
			for(int i = block.Height + 1 ; i < vChain.Count ; i++)
			{
				yield return vChain[i];
			}
		}
	}
}
