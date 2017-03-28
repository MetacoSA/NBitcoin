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
		Dictionary<uint256, ChainedBlock> _BlocksById = new Dictionary<uint256, ChainedBlock>();
		Dictionary<int, ChainedBlock> _BlocksByHeight = new Dictionary<int, ChainedBlock>();
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

		public ConcurrentChain(byte[] bytes)
		{
			Load(bytes);
		}

		public void Load(byte[] chain)
		{
			Load(new MemoryStream(chain));
		}

		public void Load(Stream stream)
		{
			Load(new BitcoinStream(stream, false));
		}

		public void Load(BitcoinStream stream)
		{
			using (@lock.LockWrite())
			{
				try
				{
					int height = 0;
					while (true)
					{
						uint256.MutableUint256 id = null;
						stream.ReadWrite<uint256.MutableUint256>(ref id);
						BlockHeader header = null;
						stream.ReadWrite(ref header);
						if (height == 0)
						{
							_BlocksByHeight.Clear();
							_BlocksById.Clear();
							_Tip = null;
							SetTipNoLock(new ChainedBlock(header, 0));
						}
						else
							SetTipNoLock(new ChainedBlock(header, id.Value, Tip));
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
			WriteTo(new BitcoinStream(stream, true));
		}

		public void WriteTo(BitcoinStream stream)
		{
			using (@lock.LockRead())
			{
				for (int i = 0; i < Tip.Height + 1; i++)
				{
					var block = GetBlockNoLock(i);
					stream.ReadWrite(block.HashBlock.AsBitcoinSerializable());
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
				foreach (var kv in _BlocksByHeight)
				{
					chain._BlocksByHeight.Add(kv.Key, kv.Value);
				}
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
				_BlocksByHeight.Remove(orphaned.Height);
				height--;
			}
			var fork = GetBlockNoLock(height);
			foreach (var newBlock in block.EnumerateToGenesis()
				.TakeWhile(c => c != Tip))
			{
				_BlocksById.AddOrReplace(newBlock.HashBlock, newBlock);
				_BlocksByHeight.AddOrReplace(newBlock.Height, newBlock);
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
			_BlocksByHeight.TryGetValue(height, out result);
			return result;
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