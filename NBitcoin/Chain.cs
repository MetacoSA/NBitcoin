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
		public bool IsPartial
		{
			get
			{
				return StartHeight != 0;
			}
		}

		public Chain(Network network)
			: this(network, null)
		{
		}

		public Chain(Network network, ObjectStream<ChainChange> changes)
			: this(network.GetGenesis().Header, changes)
		{

		}
		public Chain(Chain copied)
			: this(copied, null)
		{
		}
		public Chain(Chain copied, ObjectStream<ChainChange> changes)
		{
			if(changes == null)
				changes = new StreamObjectStream<ChainChange>();
			AssertEmpty(changes);
			_Changes = changes;
			copied.Changes.Rewind();
			foreach(var change in copied.Changes.Enumerate())
			{
				if(_NextToProcess < copied._NextToProcess)
				{
					ProcessAndRecord(change, null);
				}
				else
				{
					_Changes.WriteNext(change);
				}
			}
			_Changes.GoTo(copied._NextToProcess);
			copied.Changes.GoTo(copied._NextToProcess);
		}
		public Chain(ObjectStream<ChainChange> changes)
		{
			if(changes == null)
				changes = new StreamObjectStream<ChainChange>();
			changes.Rewind();
			_Changes = changes;
			Process();
		}
		public Chain(BlockHeader genesis)
			: this(genesis, 0, null)
		{
		}
		public Chain(BlockHeader genesis, ObjectStream<ChainChange> changes)
			: this(genesis, 0, changes)
		{
		}

		public Chain(BlockHeader blockHeader, int height, ObjectStream<ChainChange> changes)
		{
			if(changes == null)
				changes = new StreamObjectStream<ChainChange>();
			AssertEmpty(changes);
			_Changes = changes;
			Initialize(blockHeader, height);
		}


		int _NextToProcess;

		private void Process(int untilPosition = Int32.MaxValue)
		{
			if(untilPosition <= _NextToProcess)
				return;

			Changes.GoTo(_NextToProcess);
			while(true)
			{
				if(untilPosition == _NextToProcess)
					break;

				var change = Changes.ReadNext();
				if(change == null)
					break;
				Process(change, null);
				_NextToProcess = Changes.Position;
			}
		}

		private void Process(ChainChange change, uint256 blockHash)
		{
			if(blockHash == null && change.BlockHeader != null)
				blockHash = change.BlockHeader.GetHash();
			if(change.Add)
			{
				var previous = GetBlock(change.BlockHeader.HashPrevBlock, false);
				if(Initialized && previous == null && change.HeightOrBackstep != StartHeight)
					throw new InvalidOperationException("Previous block missing");
				var block = Initialized ? new ChainedBlock(change.BlockHeader, blockHash, previous) : new ChainedBlock(change.BlockHeader, (int)change.HeightOrBackstep);
				index.AddOrReplace(blockHash, block);
				vChain.Add(block);
			}
			else
			{
				foreach(var removed in vChain.Resize(vChain.Count - (int)change.HeightOrBackstep).Where(r => r != null))
				{
					offchainIndex.AddOrReplace(removed.HashBlock, removed);
					index.Remove(removed.HashBlock);
				}
			}
		}

		private void AssertEmpty(ObjectStream<ChainChange> changes)
		{
			changes.Rewind();
			if(!changes.EOF)
				throw new ArgumentException("This object stream should be empty", "changes");
		}

		List<ChainedBlock> vChain = new List<ChainedBlock>();
		Dictionary<uint256, ChainedBlock> index = new Dictionary<uint256, ChainedBlock>();
		Dictionary<uint256, ChainedBlock> offchainIndex = new Dictionary<uint256, ChainedBlock>();

		public int StartHeight
		{
			get
			{
				if(vChain.Count == 0)
					return -1;
				return vChain[0].Height;
			}
		}


		public bool Initialized
		{
			get
			{
				return vChain.Count > 0;
			}
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
				if(!Initialized)
					return -1;
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
			AssertInitialized();
			var headerHash = header.GetHash();
			ChainedBlock pindex = GetBlock(headerHash, true);
			if(pindex != null)
				return pindex;
			ChainedBlock previous = GetBlock(header.HashPrevBlock, true);
			if(previous == null)
			{
				return null;
			}

			pindex = new ChainedBlock(header, headerHash, previous);
			if(previous.HashBlock == Tip.HashBlock)
			{
				var change = new ChainChange()
				{
					Add = true,
					BlockHeader = pindex.Header,
					HeightOrBackstep = (uint)pindex.Height
				};
				ProcessAndRecord(change, pindex.HashBlock);
			}
			else
			{
				if(pindex.Height <= Tip.Height)
				{
					offchainIndex.Add(pindex.HashBlock, pindex);
				}
				else
				{
					var fork = FindFork(pindex.EnumerateToGenesis().Select(c => c.HashBlock));
					var change = new ChainChange()
					{
						Add = false,
						HeightOrBackstep = (uint)(Tip.Height - fork.Height)
					};
					ProcessAndRecord(change, null);

					foreach(var block in pindex.EnumerateToGenesis().TakeWhile(s => s != fork).Reverse())
					{
						change = new ChainChange()
						{
							Add = true,
							BlockHeader = block.Header,
							HeightOrBackstep = (uint)block.Height
						};
						ProcessAndRecord(change, block.HashBlock);
					}
				}
			}
			return pindex;
		}

		private void AssertInitialized()
		{
			if(!Initialized)
				throw new InvalidOperationException("Please call Initialize first");
		}

		public void Initialize(BlockHeader header, int height)
		{
			if(Initialized)
				throw new InvalidOperationException("Already initialized");
			var change = new ChainChange()
			{
				Add = true,
				BlockHeader = header,
				HeightOrBackstep = (uint)height
			};
			ProcessAndRecord(change, header.GetHash());
		}

		private void ProcessAndRecord(ChainChange change, uint256 blockHash)
		{
			Process(change, blockHash);
			_Changes.WriteNext(change);
			_NextToProcess++;
		}



		/// <summary>
		/// Change tip of the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		internal ChainedBlock SetTip(ChainedBlock pindex)
		{
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

			if(backtrackCount != 0)
				Changes.WriteNext(new ChainChange()
				{
					Add = false,
					HeightOrBackstep = (uint)(backtrackCount)
				});


			for(int i = pindex.Height + 1 ; i <= Tip.Height ; i++)
			{
				var block = vChain[i - StartHeight];
				Changes.WriteNext(new ChainChange()
				{
					Add = true,
					BlockHeader = block.Header,
					HeightOrBackstep = (uint)block.Height
				});
			}

			return pindex;
		}


		private readonly ObjectStream<ChainChange> _Changes;
		public ObjectStream<ChainChange> Changes
		{
			get
			{
				return _Changes;
			}
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
			return Clone(null);
		}
		public Chain Clone(ObjectStream<ChainChange> changes)
		{
			return new Chain(this, changes);
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


		static readonly TimeSpan nTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
		static readonly TimeSpan nTargetSpacing = TimeSpan.FromSeconds(10 * 60);
		static readonly long nInterval = nTargetTimespan.Ticks / nTargetSpacing.Ticks;

		public Target GetWorkRequired(Network network, int height)
		{
			if(IsPartial)
				throw new InvalidOperationException("You can't calculate work on partial chain");
			var nProofOfWorkLimit = new Target(network.ProofOfWorkLimit);
			var pindexLast = height == 0 ? null : GetBlock(height - 1);

			// Genesis block
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
					if(DateTimeOffset.UtcNow > pindexLast.Header.BlockTime + TimeSpan.FromTicks(nTargetSpacing.Ticks * 2))
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
			ChainedBlock pindexFirst = GetBlock((int)pastHeight);
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
				throw new NotSupportedException("Impossible bug happened, contact NBitcoin devs");
		}
	}
}
