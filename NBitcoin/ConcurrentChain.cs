using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	/// <summary>
	/// Thread safe class representing a chain of headers from genesis
	/// </summary>
	public class ConcurrentChain : ChainBase
	{
		public class ChainSerializationFormat
		{
			public ChainSerializationFormat()
			{
				SerializePrecomputedBlockHash = true;
				SerializeBlockHeader = true;
			}
			public bool SerializePrecomputedBlockHash
			{
				get; set;
			}
			public bool SerializeBlockHeader
			{
				get; set;
			}
			internal void AssertCoherent()
			{
				if (!SerializePrecomputedBlockHash && !SerializeBlockHeader)
					throw new InvalidOperationException("The ChainSerializationFormat is invalid, SerializePrecomputedBlockHash or SerializeBlockHeader should be true");
			}
		}
		Dictionary<uint256, ChainedBlock> _BlocksById = new Dictionary<uint256, ChainedBlock>();
		ChainedBlock[] _BlocksByHeight = new ChainedBlock[0];
		ReaderWriterLock @lock = new ReaderWriterLock();

		public ConcurrentChain()
		{

		}
		public ConcurrentChain(BlockHeader genesis)
		{
			SetTip(new ChainedBlock(genesis, 0));
		}
		public ConcurrentChain(Network network)
		{
			if (network != null)
			{
				var genesis = network.GetGenesis();
				SetTip(new ChainedBlock(genesis.Header, 0));
			}
		}

		public ConcurrentChain(byte[] bytes, ConsensusFactory consensusFactory) : this(bytes, consensusFactory, null)
		{
		}

		public ConcurrentChain(byte[] bytes, Consensus consensus) : this(bytes, consensus, null)
		{
		}

		public ConcurrentChain(byte[] bytes, Network network) : this(bytes, network, null)
		{
		}

		[Obsolete("Use ConcurrentChain(byte[], ConsensusFactory|Network|Consensus) instead")]
		public ConcurrentChain(byte[] bytes) : this(bytes, Consensus.Main.ConsensusFactory, null)
		{
		}

		public ConcurrentChain(byte[] bytes, ConsensusFactory consensusFactory, ChainSerializationFormat format)
		{
			Load(bytes, consensusFactory, format);
		}

		public ConcurrentChain(byte[] bytes, Consensus consensus, ChainSerializationFormat format)
		{
			Load(bytes, consensus, format);
		}

		public ConcurrentChain(byte[] bytes, Network network, ChainSerializationFormat format)
		{
			Load(bytes, network, format);
		}

		[Obsolete("Use ConcurrentChain(byte[], ConsensusFactory|Network|Consensus, ChainSerializationFormat format) instead")]
		public ConcurrentChain(byte[] bytes, ChainSerializationFormat format)
		{
			Load(bytes, Consensus.Main.ConsensusFactory, format);
		}

		public void Load(byte[] chain, Network network, ChainSerializationFormat format)
		{
			Load(new MemoryStream(chain), network, format);
		}

		public void Load(byte[] chain, Consensus consensus, ChainSerializationFormat format)
		{
			Load(new MemoryStream(chain), consensus, format);
		}

		public void Load(byte[] chain, ConsensusFactory consensusFactory, ChainSerializationFormat format)
		{
			Load(new MemoryStream(chain), consensusFactory, format);
		}

		[Obsolete("Use Load(byte[], ConsensusFactory|Network|Consensus, ChainSerializationFormat format) instead")]
		public void Load(byte[] chain, ChainSerializationFormat format)
		{
			Load(new MemoryStream(chain), Consensus.Main.ConsensusFactory, format);
		}

		public void Load(byte[] chain, ConsensusFactory consensusFactory)
		{
			Load(chain, consensusFactory, null);
		}

		public void Load(byte[] chain, Consensus consensus)
		{
			Load(chain, consensus, null);
		}

		public void Load(byte[] chain, Network network)
		{
			Load(chain, network, null);
		}

		[Obsolete("Use Load(byte[], ConsensusFactory|Network|Consensus) instead")]
		public void Load(byte[] chain)
		{
			Load(new MemoryStream(chain), Consensus.Main.ConsensusFactory, null);
		}

		public void Load(Stream stream, ConsensusFactory consensusFactory, ChainSerializationFormat format)
		{
			if (consensusFactory == null)
				throw new ArgumentNullException(nameof(consensusFactory));
			Load(new BitcoinStream(stream, false) { ConsensusFactory = consensusFactory }, format);
		}

		public void Load(Stream stream, Network network, ChainSerializationFormat format)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			Load(stream, network.Consensus.ConsensusFactory, format);
		}

		public void Load(Stream stream, Consensus consensus, ChainSerializationFormat format)
		{
			if (consensus == null)
				throw new ArgumentNullException(nameof(consensus));
			Load(stream, consensus.ConsensusFactory, format);
		}

		[Obsolete("Use Load(Stream, ConsensusFactory|Network|Consensus, ChainSerializationFormat) instead")]
		public void Load(Stream stream, ChainSerializationFormat format)
		{
			Load(stream, Consensus.Main.ConsensusFactory, format);
		}
		public void Load(Stream stream)
		{
			Load(new BitcoinStream(stream, false), null);
		}

		public void Load(BitcoinStream stream)
		{
			Load(stream, null);
		}
		public void Load(BitcoinStream stream, ChainSerializationFormat format)
		{
			format = format ?? new ChainSerializationFormat();
			format.AssertCoherent();
			var genesis = this.Genesis;
			using (@lock.LockWrite())
			{
				try
				{
					int height = 0;
					while (true)
					{
						uint256.MutableUint256 id = null;
						if (format.SerializePrecomputedBlockHash)
							stream.ReadWrite<uint256.MutableUint256>(ref id);
						BlockHeader header = null;
						if (format.SerializeBlockHeader)
							stream.ReadWrite(ref header);
						if (height == 0)
						{
							_BlocksByHeight = new ChainedBlock[0];
							_BlocksById.Clear();
							_Tip = null;
							if (header != null && genesis != null && header.GetHash() != genesis.HashBlock)
							{
								throw new InvalidOperationException("Unexpected genesis block");
							}
							SetTipNoLock(new ChainedBlock(genesis?.Header ?? header, 0));
						}
						else if (!format.SerializeBlockHeader ||
								(_Tip.HashBlock == header.HashPrevBlock && !(header.IsNull && header.Nonce == 0)))
							SetTipNoLock(new ChainedBlock(header, id?.Value, Tip));
						else
							break;
						height++;
					}
				}
				catch (EndOfStreamException)
				{
				}
			}
		}

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			WriteTo(ms);
			return ms.ToArray();
		}

		public void WriteTo(Stream stream)
		{
			WriteTo(stream, null);
		}
		public void WriteTo(Stream stream, ChainSerializationFormat format)
		{
			WriteTo(new BitcoinStream(stream, true), format);
		}

		public void WriteTo(BitcoinStream stream)
		{
			WriteTo(stream, null);
		}

		public void WriteTo(BitcoinStream stream, ChainSerializationFormat format)
		{
			format = format ?? new ChainSerializationFormat();
			format.AssertCoherent();
			using (@lock.LockRead())
			{
				for (int i = 0; i < Tip.Height + 1; i++)
				{
					var block = GetBlockNoLock(i);
					if (format.SerializePrecomputedBlockHash)
						stream.ReadWrite(block.HashBlock.AsBitcoinSerializable());
					if (format.SerializeBlockHeader)
						stream.ReadWrite(block.Header);
				}
			}
		}

		public ConcurrentChain Clone()
		{
			ConcurrentChain chain = new ConcurrentChain();
			chain._Tip = _Tip;
			using (@lock.LockRead())
			{
				foreach (var kv in _BlocksById)
				{
					chain._BlocksById.Add(kv.Key, kv.Value);
				}
				chain._BlocksByHeight = _BlocksByHeight.ToArray();
			}
			return chain;
		}

		/// <summary>
		/// Force a new tip for the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public override ChainedBlock SetTip(ChainedBlock block)
		{
			using (@lock.LockWrite())
			{
				return SetTipNoLock(block);
			}
		}

		private ChainedBlock SetTipNoLock(ChainedBlock block)
		{
			int height = Tip == null ? -1 : Tip.Height;
			foreach (var orphaned in EnumerateThisToFork(block))
			{
				_BlocksById.Remove(orphaned.HashBlock);
				RemoveBlocksByHeight(orphaned.Height);
				height--;
			}
			var fork = GetBlockNoLock(height);
			foreach (var newBlock in block.EnumerateToGenesis()
				.TakeWhile(c => c != fork))
			{
				_BlocksById.AddOrReplace(newBlock.HashBlock, newBlock);
				AddOrReplaceBlocksByHeight(newBlock.Height, newBlock);
			}
			_Tip = block;
			return fork;
		}

		private IEnumerable<ChainedBlock> EnumerateThisToFork(ChainedBlock block)
		{
			if (_Tip == null)
				yield break;
			var tip = _Tip;
			while (true)
			{
				if (object.ReferenceEquals(null, block) || object.ReferenceEquals(null, tip))
					throw new InvalidOperationException("No fork found between the two chains");
				if (tip.Height > block.Height)
				{
					yield return tip;
					tip = tip.Previous;
				}
				else if (tip.Height < block.Height)
				{
					block = block.Previous;
				}
				else if (tip.Height == block.Height)
				{
					if (tip.HashBlock == block.HashBlock)
						break;
					yield return tip;
					block = block.Previous;
					tip = tip.Previous;
				}
			}
		}

		#region IChain Members

		public override ChainedBlock GetBlock(uint256 id)
		{
			using (@lock.LockRead())
			{
				ChainedBlock result;
				_BlocksById.TryGetValue(id, out result);
				return result;
			}
		}

		private ChainedBlock GetBlockNoLock(int height)
		{
			ChainedBlock result;
			TryGetBlocksByHeight(height, out result);
			return result;
		}

		private bool TryGetBlocksByHeight(int height, out ChainedBlock result)
		{
			result = null;
			if (height >= _BlocksByHeight.Length || height < 0)
				return false;
			result = _BlocksByHeight[height];
			return result != null;
		}

		private void RemoveBlocksByHeight(int height)
		{
			if (height >= _BlocksByHeight.Length)
				return;
			_BlocksByHeight[height] = null;
		}

		private void AddOrReplaceBlocksByHeight(int height, ChainedBlock newBlock)
		{
			while (height >= _BlocksByHeight.Length)
			{
				Array.Resize(ref _BlocksByHeight, (int)((_BlocksByHeight.Length + 100) * 1.1));
			}
			_BlocksByHeight[height] = newBlock;
		}

		public override ChainedBlock GetBlock(int height)
		{
			using (@lock.LockRead())
			{
				return GetBlockNoLock(height);
			}
		}


		volatile ChainedBlock _Tip;
		public override ChainedBlock Tip
		{
			get
			{
				return _Tip;
			}
		}

		public override int Height
		{
			get
			{
				return Tip.Height;
			}
		}

		#endregion

		protected override IEnumerable<ChainedBlock> EnumerateFromStart()
		{
			int i = 0;
			ChainedBlock block = null;
			while (true)
			{
				using (@lock.LockRead())
				{
					block = GetBlockNoLock(i);
					if (block == null)
						yield break;
				}
				yield return block;
				i++;
			}
		}

		public override string ToString()
		{
			return Tip == null ? "no tip" : Tip.Height.ToString();
		}



	}

	internal class ReaderWriterLock
	{
		ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

		public IDisposable LockRead()
		{
			return new ActionDisposable(() => @lock.EnterReadLock(), () => @lock.ExitReadLock());
		}
		public IDisposable LockWrite()
		{
			return new ActionDisposable(() => @lock.EnterWriteLock(), () => @lock.ExitWriteLock());
		}

		internal bool TryLockWrite(out IDisposable locked)
		{
			locked = null;
			if (this.@lock.TryEnterWriteLock(0))
			{
				locked = new ActionDisposable(() =>
				{
				}, () => this.@lock.ExitWriteLock());
				return true;
			}
			return false;
		}
	}
}
