using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin
{
	/// <summary>
	/// A thread safe, memory optimized chain of hashes representing the current chain
	/// </summary>
	public class SlimChain
	{
		Dictionary<uint256, int> _HeightsByBlockHash;
		uint256[] _BlockHashesByHeight;
		int _Height;
		ReaderWriterLock _lock = new ReaderWriterLock();

		public SlimChain(uint256 genesis) : this(genesis, 1)
		{
		}
		public SlimChain(uint256 genesis, int capacity)
		{
			if (capacity < 1)
				throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity should be 1 or more");
			_BlockHashesByHeight = new uint256[capacity];
			_HeightsByBlockHash = new Dictionary<uint256, int>(capacity);
			_BlockHashesByHeight[0] = genesis;
			_HeightsByBlockHash.Add(genesis, 0);
			_Height = 0;
		}

		public int Height
		{
			get
			{
				return _Height;
			}
		}

		public bool Contains(uint256 blockHash)
		{
			using (_lock.LockRead())
			{
				return _HeightsByBlockHash.ContainsKey(blockHash);
			}
		}

		public bool TryGetHeight(uint256 blockHash, out int height)
		{
			using (_lock.LockRead())
			{
				return _HeightsByBlockHash.TryGetValue(blockHash, out height);
			}
		}

		public bool TryGetHash(int height, out uint256 blockHash)
		{
			using (_lock.LockRead())
			{
				if (height > _Height || height < 0)
				{
					blockHash = default(uint256);
					return false;
				}
				blockHash = _BlockHashesByHeight[height];
			}
			return true;
		}

		public void ResetToGenesis()
		{
			TrySetTip(Genesis, null);
		}

		public void SetCapacity(int capacity)
		{
			using (_lock.LockWrite())
			{
				if (capacity <= _BlockHashesByHeight.Length)
					return;
				var old = _BlockHashesByHeight;
				_BlockHashesByHeight = new uint256[capacity];
				Array.Copy(old, 0, _BlockHashesByHeight, 0, old.Length);
				var oldd = _HeightsByBlockHash;
				_HeightsByBlockHash = new Dictionary<uint256, int>(capacity);
				foreach (var item in oldd)
					_HeightsByBlockHash.Add(item.Key, item.Value);
			}
		}

		/// <summary>
		/// Set a new tip in the chain
		/// </summary>
		/// <param name="newTip">The new tip</param>
		/// <param name="previous">The block hash before the new tip</param>
		/// <param name="nopIfContainsTip">If true and the new tip is already included somewhere in the chain, do nothing</param>
		/// <returns>True if newTip is the new tip</returns>
		public bool TrySetTip(uint256 newTip, uint256 previous, bool nopIfContainsTip = false)
		{
			using (_lock.LockWrite())
			{
				return TrySetTipNoLock(newTip, previous, nopIfContainsTip);
			}
		}

		private bool TrySetTipNoLock(in uint256 newTip, in uint256 previous, bool nopIfContainsTip)
		{
			if (newTip == null)
				throw new ArgumentNullException(nameof(newTip));
			if (newTip == previous)
				throw new ArgumentException(message: "newTip should be different from previous");

			if (newTip == _BlockHashesByHeight[_Height])
			{
				if (newTip != _BlockHashesByHeight[0] && _Height > 0 && _BlockHashesByHeight[_Height - 1] != previous)
					throw new ArgumentException(message: "newTip is already inserted with a different previous block, this should never happen");
				return true;
			}

			if (_HeightsByBlockHash.TryGetValue(newTip, out int newTipHeight))
			{
				if (newTipHeight > 0 && _BlockHashesByHeight[newTipHeight - 1] != previous)
					throw new ArgumentException(message: "newTip is already inserted with a different previous block, this should never happen");

				if (newTipHeight == 0 && _BlockHashesByHeight[0] != newTip)
				{
					throw new InvalidOperationException("Unexpected genesis block");
				}

				if (newTipHeight == 0 && previous != null)
					throw new ArgumentException(message: "Genesis block should not have previous block", paramName: nameof(previous));

				if (nopIfContainsTip)
					return false;
			}

			if (previous == null && newTip != _BlockHashesByHeight[0])
				throw new InvalidOperationException("Unexpected genesis block");

			int prevHeight = -1;
			if (previous != null && !_HeightsByBlockHash.TryGetValue(previous, out prevHeight))
				return false;
			for (int i = _Height; i > prevHeight; i--)
			{
				_HeightsByBlockHash.Remove(_BlockHashesByHeight[i]);
				_BlockHashesByHeight[i] = null;
			}
			_Height = prevHeight + 1;
			while (_BlockHashesByHeight.Length <= _Height)
			{
				var old = _BlockHashesByHeight;
				_BlockHashesByHeight = new uint256[(int)((_BlockHashesByHeight.Length) * (_Height < 500_000 ? 2.0 : 1.1))];
				Array.Copy(old, 0, _BlockHashesByHeight, 0, old.Length);
			}
			_BlockHashesByHeight[_Height] = newTip;
			_HeightsByBlockHash.Add(newTip, _Height);
			return true;
		}
		public BlockLocator GetTipLocator()
		{
			using (_lock.LockRead())
			{
				return GetLocatorNoLock(_Height);
			}
		}

		public BlockLocator GetLocator(int height)
		{
			using (_lock.LockRead())
			{
				if (height > _Height || height < 0)
					return null;
				return GetLocatorNoLock(height);
			}
		}

		public BlockLocator GetLocator(uint256 blockHash)
		{
			using (_lock.LockRead())
			{
				if (!_HeightsByBlockHash.TryGetValue(blockHash, out int height))
					return null;
				return GetLocatorNoLock(height);
			}
		}

		private BlockLocator GetLocatorNoLock(int height)
		{
			int nStep = 1;
			var vHave = new List<uint256>();
			while (true)
			{
				vHave.Add(_BlockHashesByHeight[height]);
				// Stop when we have added the genesis block.
				if (height == 0)
					break;
				// Exponentially larger steps back, plus the genesis block.
				height = Math.Max(height - nStep, 0);
				if (vHave.Count > 10)
					nStep *= 2;
			}

			var locators = new BlockLocator();
			locators.Blocks = vHave;
			return locators;
		}

		/// <summary>
		/// Returns the first found block
		/// </summary>
		/// <param name="hashes">Hash to search for</param>
		/// <returns>First found block or null</returns>
		public SlimChainedBlock FindFork(BlockLocator blockLocator)
		{
			if (blockLocator == null)
				throw new ArgumentNullException(nameof(blockLocator));
			// Find the first block the caller has in the main chain
			foreach (uint256 hash in blockLocator.Blocks)
			{
				if (_HeightsByBlockHash.TryGetValue(hash, out int height))
				{
					return CreateSlimBlock(height);
				}
			}
			return null;
		}

		public uint256 Tip
		{
			get
			{
				using (_lock.LockRead())
				{
					return _BlockHashesByHeight[_Height];
				}
			}
		}

		public SlimChainedBlock TipBlock
		{
			get
			{
				using (_lock.LockRead())
				{
					return CreateSlimBlock(Height);
				}
			}
		}

		public SlimChainedBlock GetBlock(int height)
		{
			using (_lock.LockRead())
			{
				if (height > Height || height < 0)
					return null;
				return CreateSlimBlock(height);
			}
		}

		public SlimChainedBlock GetBlock(uint256 blockHash)
		{
			using (_lock.LockRead())
			{
				if (!_HeightsByBlockHash.TryGetValue(blockHash, out int height))
					return null;
				return CreateSlimBlock(height);
			}
		}

		private SlimChainedBlock CreateSlimBlock(int height)
		{
			return new SlimChainedBlock(_BlockHashesByHeight[height], height == 0 ? null : _BlockHashesByHeight[height - 1], height);
		}

		public uint256 Genesis
		{
			get
			{
				using (_lock.LockRead())
				{
					return _BlockHashesByHeight[0];
				}
			}
		}

		public void Save(Stream output)
		{
			using (_lock.LockRead())
			{
				var bytes = new byte[32];
				for (int i = 0; i <= _Height; i++)
				{
					_BlockHashesByHeight[i].ToBytes(bytes);
					output.Write(bytes, 0, 32);
				}
			}
		}

		public void Load(Stream input)
		{
			using (_lock.LockWrite())
			{
				var bytes = new byte[32];
				uint256 prev = null;
				while (input.ReadBytes(32, bytes) == 32)
				{
					uint256 tip = new uint256(bytes);
					if (!TrySetTipNoLock(tip, prev, false))
						throw new InvalidOperationException("Unexpected genesis block");
					prev = tip;
				}

			}
		}

		public override string ToString()
		{
			using (_lock.LockRead())
			{
				return $"Height: {Height}, Hash: {_BlockHashesByHeight[_Height]}";
			}
		}
	}

	public class SlimChainedBlock
	{
		public SlimChainedBlock(uint256 hash, uint256 prev, int height)
		{
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
			if (prev == null && height != 0)
				throw new ArgumentNullException(nameof(prev));
			if (height < 0)
				throw new ArgumentOutOfRangeException(nameof(height));
			_Hash = hash;
			_Previous = prev;
			_Height = height;
		}
		private readonly uint256 _Hash;
		public uint256 Hash
		{
			get
			{
				return _Hash;
			}
		}

		private readonly uint256 _Previous;
		public uint256 Previous
		{
			get
			{
				return _Previous;
			}
		}


		private readonly int _Height;
		public int Height
		{
			get
			{
				return _Height;
			}
		}

		public override string ToString()
		{
			return Hash.ToString();
		}
	}
}
