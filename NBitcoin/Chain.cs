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

		public static Chain LoadOrInitialize(ObjectStream<ChainChange> changes, Network network)
		{
			var chain = Load(changes);
			if(chain == null)
			{
				chain = new Chain(network);
				changes.WriteNext(new ChainChange()
				{
					Add = true,
					HeightOrBackstep = 0,
					BlockHeader = chain.Genesis.Header
				});
			}
			return chain;
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

		ChainedBlock _StartBlock;
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
			_StartBlock = new ChainedBlock(genesis, null);
			Clear();
		}

		public Chain(BlockHeader blockHeader, int height)
		{
			StartHeight = height;
			_StartBlock = new ChainedBlock(blockHeader, height);
			Clear();
		}

		private void Clear()
		{
			vChain.Clear();
			index.Clear();
			vChain.Add(_StartBlock);
			index.Add(_StartBlock.HashBlock, _StartBlock);

		}

		List<ChainedBlock> vChain = new List<ChainedBlock>();
		Dictionary<uint256, ChainedBlock> index = new Dictionary<uint256, ChainedBlock>();
		Dictionary<uint256, ChainedBlock> offchainIndex = new Dictionary<uint256, ChainedBlock>();

		public int StartHeight
		{
			get;
			set;
		}

		public ChainedBlock Genesis
		{
			get
			{
				if(StartHeight == 0)
					return vChain[0];
				return null;
			}
		}

		public ChainedBlock Tip
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

		public ChainedBlock GetBlock(uint256 hash, bool includeBranch)
		{
			ChainedBlock pindex = null;
			index.TryGetValue(hash, out pindex);
			if(pindex == null && includeBranch)
			{
				offchainIndex.TryGetValue(hash, out pindex);
			}
			return pindex;
		}
		public ChainedBlock GetBlock(uint256 hash)
		{
			return GetBlock(hash, false);
		}
		public ChainedBlock GetOrAdd(BlockHeader header)
		{
			return GetOrAdd(header, null);
		}
		public ChainedBlock GetOrAdd(BlockHeader header, ObjectStream<ChainChange> changes)
		{
			ChainedBlock pindex = GetBlock(header.GetHash(), true);
			if(pindex != null)
				return pindex;
			ChainedBlock previous = GetBlock(header.HashPrevBlock, true);
			if(previous == null)
				return null;
			pindex = new ChainedBlock(header, previous);
			index.AddOrReplace(pindex.HashBlock, pindex);
			if(pindex.Height > Tip.Height)
			{
				var tipBefore = Tip;
				SetTip(pindex, changes);
			}
			return pindex;
		}



		/// <summary>
		/// Change tip of the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public ChainedBlock SetTip(ChainedBlock pindex)
		{
			return SetTip(pindex, null);
		}
		public ChainedBlock SetTip(ChainedBlock pindex, ObjectStream<ChainChange> changes)
		{
			if(pindex == null)
			{
				Clear();
				return null;
			}

			int backtrackCount = 0;
			foreach(var remove in vChain.Resize(pindex.Height + 1 - StartHeight))
			{
				index.Remove(remove.HashBlock);
				offchainIndex.AddOrReplace(remove.HashBlock, remove);
				backtrackCount++;
			}



			while(pindex != null && vChain[pindex.Height - StartHeight] != pindex)
			{
				var old = vChain[pindex.Height - StartHeight];
				if(old != null)
				{
					index.Remove(old.HashBlock);
					offchainIndex.AddOrReplace(old.HashBlock, old);
					backtrackCount++;
				}
				vChain[pindex.Height - StartHeight] = pindex;
				index.AddOrReplace(pindex.HashBlock, pindex);
				pindex = pindex.Previous;
			}

			if(changes != null)
			{
				if(backtrackCount != 0)
					changes.WriteNext(new ChainChange()
					{
						Add = false,
						HeightOrBackstep = (uint)(backtrackCount)
					});


				for(int i = pindex.Height + 1 ; i <= Tip.Height ; i++)
				{
					var block = vChain[i - StartHeight];
					changes.WriteNext(new ChainChange()
					{
						Add = true,
						BlockHeader = block.Header,
						HeightOrBackstep = (uint)block.Height
					});
				}
			}
			return pindex;
		}


		public ChainedBlock FindFork(Chain chain)
		{
			return FindFork(chain.ToEnumerable(true).Select(o => o.HashBlock));
		}

		public ChainedBlock FindFork(IEnumerable<uint256> hashes)
		{
			// Find the first block the caller has in the main chain
			foreach(uint256 hash in hashes)
			{
				ChainedBlock mi = null;
				if(index.TryGetValue(hash, out mi))
				{
					if(Contains(mi))
						return mi;
				}
			}
			return Genesis;
		}

		public ChainedBlock FindFork(BlockLocator locator)
		{
			return FindFork(locator.Blocks);
		}

		public ChainedBlock GetBlock(int nHeight)
		{
			var index = nHeight - StartHeight;
			if(index < 0 || index >= (int)vChain.Count)
				return null;
			return vChain[index];
		}

		public bool Contains(uint256 hash, bool includeBranch = false)
		{
			ChainedBlock pindex = GetBlock(hash, includeBranch);
			return pindex != null;
		}
		public bool Contains(ChainedBlock blockIndex)
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
				foreach(var b in vChain)
					yield return b;
			}
		}


		public IEnumerable<ChainedBlock> EnumerateAfter(ChainedBlock block)
		{
			for(int i = block.Height + 1 ; i < vChain.Count ; i++)
			{
				yield return vChain[i - StartHeight];
			}
		}
	}
}
