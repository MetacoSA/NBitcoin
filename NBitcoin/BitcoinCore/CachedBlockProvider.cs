#if !NOFILEIO
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BitcoinCore
{
	public class CachedBlockProvider : IBlockProvider
	{
		public CachedBlockProvider(IBlockProvider inner)
		{
			_Inner = inner;
			MaxCachedBlock = 50;
		}
		private readonly IBlockProvider _Inner;
		public IBlockProvider Inner
		{
			get
			{
				return _Inner;
			}
		}

		public int MaxCachedBlock
		{
			get;
			set;
		}
		#region IBlockProvider Members

		ConcurrentDictionary<uint256, Block> _Blocks = new ConcurrentDictionary<uint256, Block>();

		public Block GetBlock(uint256 id, List<byte[]> searchedData)
		{
			Block result = null;
			if(_Blocks.TryGetValue(id, out result))
				return result;
			result = Inner.GetBlock(id, searchedData);
			_Blocks.AddOrUpdate(id, result, (i, b) => b);
			while(_Blocks.Count > MaxCachedBlock)
			{
				var removed = TakeRandom(_Blocks.Keys.ToList());
				Block ignored = null;
				_Blocks.TryRemove(removed, out ignored);
			}
			return result;
		}

		private static uint256 TakeRandom(List<uint256> id)
		{
			if(id.Count == 0)
				return null;
			Random rand = new Random();
			return id[rand.Next(0, id.Count)];
		}

		#endregion
	}
}
#endif