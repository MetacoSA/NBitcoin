using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public enum ChainChangeType : byte
	{
		BackStep = 0,
		SetTip = 1,
		AddBlock = 2
	}
	public class ChainChange : IBitcoinSerializable
	{
		byte _ChangeType;
		public ChainChangeType ChangeType
		{
			get
			{
				return (ChainChangeType)_ChangeType;
			}
			set
			{
				_ChangeType = (byte)value;
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
			stream.ReadWrite(ref _ChangeType);
			stream.ReadWriteAsCompactVarInt(ref _HeightOrBackstep);
			if(ChangeType == ChainChangeType.AddBlock || ChangeType == ChainChangeType.SetTip)
			{
				stream.ReadWrite(ref _BlockHeader);
			}
		}

		#endregion

		public override string ToString()
		{
			return ChangeType.ToString() + "-" + HeightOrBackstep;
		}
	}
	public class PersistantChain : ChainBase
	{
		public bool IsPartial
		{
			get
			{
				return StartHeight != 0;
			}
		}

		public PersistantChain()
			: this(new StreamObjectStream<ChainChange>())
		{

		}

		public PersistantChain(Network network)
			: this(network, null)
		{
		}

		public PersistantChain(Network network, ObjectStream<ChainChange> changes)
			: this(network.GetGenesis().Header, changes)
		{

		}
		public PersistantChain(PersistantChain copied)
			: this(copied, null)
		{
		}
		public PersistantChain(PersistantChain copied, ObjectStream<ChainChange> changes)
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
					PushChange(change, null);
				}
				else
				{
					_Changes.WriteNext(change);
				}
			}
		}
		public PersistantChain(ObjectStream<ChainChange> changes)
		{
			if(changes == null)
				changes = new StreamObjectStream<ChainChange>();
			changes.Rewind();
			_Changes = changes;
			Process();
		}
		public PersistantChain(BlockHeader genesis)
			: this(genesis, 0, null)
		{
		}
		public PersistantChain(BlockHeader genesis, ObjectStream<ChainChange> changes)
			: this(genesis, 0, changes)
		{
		}

		public PersistantChain(BlockHeader blockHeader, int height, ObjectStream<ChainChange> changes)
		{
			if(changes == null)
				changes = new StreamObjectStream<ChainChange>();
			_Changes = changes;
			changes.Rewind();
			if(changes.EOF)
			{
				Initialize(blockHeader, height);
			}
			else
			{
				var first = changes.ReadNext();
				if(first.BlockHeader.GetHash() != blockHeader.GetHash())
				{
					throw new InvalidOperationException("The first block of this stream is different than the expected one at height " + height);
				}
				if(first.HeightOrBackstep != height)
				{
					throw new InvalidOperationException("The first block of this stream has height " + first.HeightOrBackstep + " but expected is " + height);
				}
				changes.Rewind();
				Process();
			}
		}


		int _NextToProcess;

		public void Process(int untilPosition = Int32.MaxValue)
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

		void Process(ChainChange change, uint256 blockHash)
		{
			if(blockHash == null && change.BlockHeader != null)
				blockHash = change.BlockHeader.GetHash();
			if(change.ChangeType == ChainChangeType.SetTip || change.ChangeType == ChainChangeType.AddBlock)
			{
				var previous = GetBlock(change.BlockHeader.HashPrevBlock, true);
				if(Initialized && previous == null && change.HeightOrBackstep != StartHeight)
					throw new InvalidOperationException("Previous block missing");
				var block = Initialized ? new ChainedBlock(change.BlockHeader, blockHash, previous) : new ChainedBlock(change.BlockHeader, (int)change.HeightOrBackstep);
				if(change.ChangeType == ChainChangeType.SetTip)
				{
					List<ChainedBlock> newMain = new List<ChainedBlock>();
					ChainedBlock previousBlock = block;
					while(previousBlock != null && previousBlock.Height >= Height + 1)
					{
						newMain.Add(previousBlock);
						previousBlock = previousBlock.Previous;
					}
					newMain.Reverse();
					foreach(var newBlock in newMain)
					{
						vChain.Add(newBlock);
						index.AddOrReplace(newBlock.HashBlock, newBlock);
						offchainIndex.Remove(newBlock.HashBlock);
					}
				}
				else
				{
					offchainIndex.AddOrReplace(blockHash, block);
				}
			}
			else if(change.ChangeType == ChainChangeType.BackStep)
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

		public override ChainedBlock Genesis
		{
			get
			{
				if(StartHeight == 0)
					return vChain[0];
				return null;
			}
		}

		public override ChainedBlock Tip
		{
			get
			{
				return vChain[Height - StartHeight];
			}
		}

		public override int Height
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
		public override ChainedBlock GetBlock(uint256 hash)
		{
			return GetBlock(hash, false);
		}

		/// <summary>
		/// Set the tip if header is higher than current tip and previous blocks are present
		/// </summary>
		/// <param name="header"></param>
		/// <returns>True if header is the new Tip</returns>
		public bool TrySetTip(BlockHeader header)
		{
			ChainedBlock block;
			return TrySetTip(header, out block);
		}

		/// <summary>
		/// Set the tip if header is higher than current tip and previous blocks are present
		/// </summary>
		/// <param name="header"></param>
		/// <param name="chainedBlock">null if header could not be chained with previous blocks</param>
		/// <returns>True if header is the new Tip</returns>
		public bool TrySetTip(BlockHeader header, out ChainedBlock chainedBlock)
		{
			AssertInitialized();
			var headerHash = header.GetHash();
			chainedBlock = GetBlock(headerHash, true);
			if(chainedBlock != null)
				return false;
			ChainedBlock previous = GetBlock(header.HashPrevBlock, true);
			if(previous == null)
			{
				return false;
			}

			chainedBlock = new ChainedBlock(header, headerHash, previous);
			if(chainedBlock.Height > Tip.Height)
			{
				SetTip(chainedBlock);
			}
			else
			{
				var change = new ChainChange()
				{
					ChangeType = ChainChangeType.AddBlock,
					HeightOrBackstep = (uint)chainedBlock.Height,
					BlockHeader = chainedBlock.Header
				};
				PushChange(change, chainedBlock.HashBlock);
			}
			return true;
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
				ChangeType = ChainChangeType.SetTip,
				BlockHeader = header,
				HeightOrBackstep = (uint)height
			};
			PushChange(change, header.GetHash());
		}

		public void PushChange(ChainChange change, uint256 blockHash)
		{
			Process(change, blockHash);
			_Changes.WriteNext(change);
			_NextToProcess++;
		}


		public ChainedBlock CreateChainedBlock(BlockHeader header)
		{
			AssertInitialized();
			var hash = header.GetHash();
			var result = GetBlock(hash, true);
			if(result == null)
			{
				var previous = GetBlock(header.HashPrevBlock, true);
				if(previous == null)
					throw new InvalidOperationException("Previous block is missing");
				return new ChainedBlock(header, hash, previous);
			}
			return result;
		}

		/// <summary>
		/// Force a new tip for the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <exception cref="System.InvalidOperationException">Previous block not in the chain or any of its branch</exception>
		/// <returns>forking point</returns>
		public ChainedBlock SetTip(BlockHeader header)
		{
			var chained = CreateChainedBlock(header);
			return SetTip(chained);
		}

		/// <summary>
		/// Force a new tip for the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public override ChainedBlock SetTip(ChainedBlock pindex)
		{
			AssertInitialized();
			if(pindex.HashBlock == Tip.HashBlock)
				return pindex;
			var fork = FindFork(pindex.EnumerateToGenesis().Select(g => g.HashBlock));

			var backStep = (uint)(Tip.Height - fork.Height);
			if(backStep != 0)
			{
				PushChange(new ChainChange()
				{
					ChangeType = ChainChangeType.BackStep,
					HeightOrBackstep = backStep
				}, null);
			}
			var newBranch = pindex.EnumerateToGenesis().TakeWhile(b => b.Height != fork.Height).ToList();
			newBranch.Reverse();
			var newTip = newBranch.Count == 0 ? null : newBranch[newBranch.Count - 1];
			foreach(var block in newBranch)
			{
				var existing = GetBlock(block.HashBlock, true);
				if(existing == null || block == newTip)
				{
					PushChange(new ChainChange()
					{
						ChangeType = ChainChangeType.SetTip,
						BlockHeader = block.Header,
						HeightOrBackstep = (uint)block.Height
					}, block.HashBlock);
				}
			}
			return fork;
		}


		private readonly ObjectStream<ChainChange> _Changes;
		public ObjectStream<ChainChange> Changes
		{
			get
			{
				return _Changes;
			}
		}

		public override ChainedBlock GetBlock(int nHeight)
		{
			AssertInitialized();
			var index = nHeight - StartHeight;
			if(index < 0 || index >= (int)vChain.Count)
				return null;
			return vChain[index];
		}

		public bool Contains(uint256 hash, bool includeBranch)
		{
			ChainedBlock pindex = GetBlock(hash, includeBranch);
			return pindex != null;
		}
		


		public PersistantChain Clone()
		{
			return Clone(null);
		}
		public PersistantChain Clone(ObjectStream<ChainChange> changes)
		{
			return new PersistantChain(this, changes);
		}

		public override IEnumerable<ChainedBlock> EnumerateAfter(ChainedBlock block)
		{
			for(int i = block.Height + 1 ; i < vChain.Count ; i++)
			{
				yield return vChain[i - StartHeight];
			}
		}


		

		public PersistantChain CreateSubChain(ChainedBlock from,
									bool fromIncluded,
									ChainedBlock to,
									bool toIncluded,
									ObjectStream<ChainChange> output = null)
		{
			if(output == null)
				output = new StreamObjectStream<ChainChange>();

			var blocks
				=
				to.EnumerateToGenesis()
				.Skip(toIncluded ? 0 : 1)
				.TakeWhile(c => c.HashBlock != from.HashBlock);
			if(fromIncluded)
				blocks = blocks.Concat(new ChainedBlock[] { from });

			var array = blocks.ToArray();
			Array.Reverse(array);
			foreach(var b in array)
			{
				output.WriteNext(new ChainChange()
				{
					ChangeType = ChainChangeType.SetTip,
					BlockHeader = b.Header,
					HeightOrBackstep = (uint)b.Height
				});
			}
			return new PersistantChain(output);
		}

		public void PushChanges(ObjectStream<ChainChange> changes)
		{
			foreach(var change in changes.Enumerate())
			{
				PushChange(change, null);
			}
		}

		public override string ToString()
		{
			if(IsPartial)
				return "Partial " + StartHeight + "-" + Height;
			else
				return "Full chain " + Height;
		}

		protected override IEnumerable<ChainedBlock> EnumerateFromStart()
		{
			foreach(var b in vChain)
				yield return b;
		}
	}
}
