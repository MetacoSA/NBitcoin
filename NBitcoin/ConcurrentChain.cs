using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class ConcurrentChain : ChainBase
	{
		class ReaderWriterLock
		{
			class FuncDisposable : IDisposable
			{
				Action onEnter, onLeave;
				public FuncDisposable(Action onEnter, Action onLeave)
				{
					this.onEnter = onEnter;
					this.onLeave = onLeave;
					onEnter();
				}

				#region IDisposable Members

				public void Dispose()
				{
					onLeave();
				}

				#endregion
			}
			ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

			public IDisposable LockRead()
			{
				return new FuncDisposable(() => @lock.EnterReadLock(), () => @lock.ExitReadLock());
			}
			public IDisposable LockWrite()
			{
				return new FuncDisposable(() => @lock.EnterWriteLock(), () => @lock.ExitWriteLock());
			}
		}
		Dictionary<uint256, ChainedBlock> _BlocksById = new Dictionary<uint256, ChainedBlock>();
		Dictionary<int, ChainedBlock> _BlocksByHeight = new Dictionary<int, ChainedBlock>();
		ReaderWriterLock @lock = new ReaderWriterLock();

		public ConcurrentChain()
		{

		}
		public ConcurrentChain(Network network)
		{
			if(network != null)
			{
				var genesis = network.GetGenesis();
				SetTip(new ChainedBlock(genesis.Header, 0));
			}
		}

		/// <summary>
		/// Force a new tip for the chain
		/// </summary>
		/// <param name="pindex"></param>
		/// <returns>forking point</returns>
		public override ChainedBlock SetTip(ChainedBlock block)
		{
			using(@lock.LockWrite())
			{
				int height = Tip == null ? -1 : Tip.Height;
				foreach(var orphaned in EnumerateThisToFork(block))
				{
					_BlocksById.Remove(orphaned.HashBlock);
					_BlocksByHeight.Remove(orphaned.Height);
					height--;
				}
				var fork = GetBlockNoLock(height);
				foreach(var newBlock in block.EnumerateToGenesis()
					.TakeWhile(c => c != Tip))
				{
					_BlocksById.AddOrReplace(newBlock.HashBlock, newBlock);
					_BlocksByHeight.AddOrReplace(newBlock.Height, newBlock);
				}
				_Tip = block;
				return fork;
			}
		}

		

		private IEnumerable<ChainedBlock> EnumerateThisToFork(ChainedBlock block)
		{
			if(_Tip == null)
				yield break;
			var tip = _Tip;
			while(true)
			{
				if(tip.Height > block.Height)
				{
					yield return tip;
					tip = tip.Previous;
				}
				else if(tip.Height < block.Height)
				{
					block = block.Previous;
				}
				else if(tip.Height == block.Height)
				{
					if(tip.HashBlock == block.HashBlock)
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
			using(@lock.LockRead())
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
			using(@lock.LockRead())
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
			using(@lock.LockRead())
			{
				int i = 0;
				while(true)
				{
					var block = GetBlockNoLock(i);
					if(block == null)
						yield break;
					yield return block;
					i++;
				}
			}
		}
	}
}
