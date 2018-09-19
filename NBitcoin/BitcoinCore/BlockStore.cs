﻿#if !NOFILEIO
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	[Obsolete]
	public class BlockStore : Store<StoredBlock, Block>
	{
		public const int MAX_BLOCKFILE_SIZE = 0x8000000; // 128 MiB



		public BlockStore(string folder, Network network)
			: base(folder, network)
		{
			MaxFileSize = MAX_BLOCKFILE_SIZE;
			FilePrefix = "blk";
		}


		public ConcurrentChain GetChain()
		{
			ConcurrentChain chain = new ConcurrentChain(Network);
			SynchronizeChain(chain);
			return chain;
		}

		public void SynchronizeChain(ChainBase chain)
		{
			Dictionary<uint256, BlockHeader> headers = new Dictionary<uint256, BlockHeader>();
			HashSet<uint256> inChain = new HashSet<uint256>();
			inChain.Add(chain.GetBlock(0).HashBlock);
			foreach(var header in Enumerate(true).Select(b => b.Item.Header))
			{
				var hash = header.GetHash();
				headers.Add(hash, header);
			}
			List<uint256> toRemove = new List<uint256>();
			while(headers.Count != 0)
			{
				foreach(var header in headers)
				{
					if(inChain.Contains(header.Value.HashPrevBlock))
					{
						toRemove.Add(header.Key);
						chain.SetTip(header.Value);
						inChain.Add(header.Key);
					}
				}
				foreach(var item in toRemove)
					headers.Remove(item);
				if(toRemove.Count == 0)
					break;
				toRemove.Clear();
			}
		}

		bool headerOnly;
		// FIXME: this methods doesn't have a path to stop the recursion.
#pragma warning disable CS0612 // Type or member is obsolete
		public IEnumerable<StoredBlock> Enumerate(Stream stream, uint fileIndex = 0, DiskBlockPosRange range = null, bool headersOnly = false)
#pragma warning restore CS0612 // Type or member is obsolete
		{
			using(HeaderOnlyScope(headersOnly))
			{
				foreach(var r in Enumerate(stream, fileIndex, range))
				{
					yield return r;
				}
			}
		}


		private IDisposable HeaderOnlyScope(bool headersOnly)
		{
			var old = headersOnly;
			var oldBuff = BufferSize;
			return new Scope(() =>
			{
				this.headerOnly = headersOnly;
				if(!headerOnly)
					BufferSize = 1024 * 1024;
			}, () =>
			{
				this.headerOnly = old;
				BufferSize = oldBuff;
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="headersOnly"></param>
		/// <param name="blockStart">Inclusive block count</param>
		/// <param name="blockEnd">Exclusive block count</param>
		/// <returns></returns>
		public IEnumerable<StoredBlock> Enumerate(bool headersOnly, int blockCountStart, int count = 999999999)
		{
			int blockCount = 0;
			DiskBlockPos start = null;
			foreach(var block in Enumerate(true, null))
			{
				if(blockCount == blockCountStart)
				{
					start = block.BlockPosition;
				}
				blockCount++;
			}
			if(start == null)
				yield break;


			int i = 0;
#pragma warning disable CS0612 // Type or member is obsolete
			foreach(var result in Enumerate(headersOnly, new DiskBlockPosRange(start)))
#pragma warning restore CS0612 // Type or member is obsolete
			{
				if(i >= count)
					break;
				yield return result;
				i++;
			}

		}

#pragma warning disable CS0612 // Type or member is obsolete
		public IEnumerable<StoredBlock> Enumerate(bool headersOnly, DiskBlockPosRange range = null)
#pragma warning restore CS0612 // Type or member is obsolete
		{
			using(HeaderOnlyScope(headersOnly))
			{
				foreach(var result in Enumerate(range))
				{
					yield return result;
				}
			}
		}


		protected override StoredBlock ReadStoredItem(Stream stream, DiskBlockPos pos)
		{
			StoredBlock block = new StoredBlock(Network, pos);
			block.ParseSkipBlockContent = headerOnly;
			block.ReadWrite(stream, false);
			return block;
		}

		protected override StoredBlock CreateStoredItem(Block item, DiskBlockPos position)
		{
			return new StoredBlock(Network.Magic, item, position);
		}
	}
}
#endif