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
		Dictionary<UInt256Struct, int> _HeightsByBlockHash = new Dictionary<UInt256Struct, int>();
		UInt256Struct[] _BlockHashesByHeight = new UInt256Struct[1];
		int _Height;
		ReaderWriterLock _lock = new ReaderWriterLock();

		public SlimChain(in UInt256Struct genesis)
		{
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

		public bool Contains(UInt256Struct blockHash)
		{
			using(_lock.LockRead())
			{
				return _HeightsByBlockHash.ContainsKey(blockHash);
			}
		}

		public bool TryGetHeight(UInt256Struct blockHash, out int height)
		{
			using(_lock.LockRead())
			{
				return _HeightsByBlockHash.TryGetValue(blockHash, out height);
			}
		}

		public bool TryGetHash(int height, out UInt256Struct blockHash)
		{
			using(_lock.LockRead())
			{
				if(height > _Height || height < 0)
				{
					blockHash = default(UInt256Struct);
					return false;
				}
				blockHash = _BlockHashesByHeight[height];
			}
			return true;
		}

		public bool TrySetTip(in UInt256Struct tip, in UInt256Struct previous, bool nopIfContainsTip = false)
		{
			using(_lock.LockWrite())
			{
				return TrySetTipNoLock(tip, previous, nopIfContainsTip);
			}
		}

		private bool TrySetTipNoLock(UInt256Struct tip, UInt256Struct previous, bool nopIfContainsTip)
		{
			if(tip == previous)
				throw new ArgumentException(message: "tip should be different from previous");
			if(tip == _BlockHashesByHeight[_Height])
				return false;
			if(_HeightsByBlockHash.TryGetValue(tip, out int newTipHeight))
			{
				// We can't replace the genesis
				if(newTipHeight - 1 < 0)
					return false;
				if(_BlockHashesByHeight[newTipHeight - 1] != previous)
					throw new ArgumentException(message: "This tip is already inserted with a different previous block, this should never happen");
				if(nopIfContainsTip)
					return false;
			}
			if(!_HeightsByBlockHash.TryGetValue(previous, out var prevHeight))
				return false;
			for(int i = _Height; i > prevHeight; i--)
			{
				_HeightsByBlockHash.Remove(_BlockHashesByHeight[i]);
				_BlockHashesByHeight[i] = UInt256Struct.Zero;
			}
			_Height = prevHeight + 1;
			if(_BlockHashesByHeight.Length <= _Height)
				Array.Resize(ref _BlockHashesByHeight, (int)((_Height + 100) * 1.1));
			_BlockHashesByHeight[_Height] = tip;
			_HeightsByBlockHash.Add(tip, _Height);
			return true;
		}

		public BlockLocator GetTipLocator()
		{
			using(_lock.LockRead())
			{
				return GetLocatorNoLock(_Height);
			}
		}

		private BlockLocator GetLocatorNoLock(int height)
		{
			int nStep = 1;
			var vHave = new List<uint256>();
			while(true)
			{
				vHave.Add(_BlockHashesByHeight[height]);
				// Stop when we have added the genesis block.
				if(height == 0)
					break;
				// Exponentially larger steps back, plus the genesis block.
				height = Math.Max(height - nStep, 0);
				if(vHave.Count > 10)
					nStep *= 2;
			}

			var locators = new BlockLocator();
			locators.Blocks = vHave;
			return locators;
		}

		public UInt256Struct Tip
		{
			get
			{
				using(_lock.LockRead())
				{
					return _BlockHashesByHeight[_Height];
				}
			}
		}

		public SlimChainedBlock TipBlock
		{
			get
			{
				using(_lock.LockRead())
				{
					return CreateSlimBlock(Height);
				}
			}
		}

		public SlimChainedBlock GetBlock(int height)
		{
			using(_lock.LockRead())
			{
				if(height > Height || height < 0)
					return null;
				return CreateSlimBlock(height);
			}
		}

		public SlimChainedBlock GetBlock(UInt256Struct blockHash)
		{
			using(_lock.LockRead())
			{
				if(!_HeightsByBlockHash.TryGetValue(blockHash, out int height))
					return null;
				return CreateSlimBlock(height);
			}
		}

		private SlimChainedBlock CreateSlimBlock(int height)
		{
			return new SlimChainedBlock(_BlockHashesByHeight[height], height == 0 ? null : _BlockHashesByHeight[height - 1].ToUInt256(), height);
		}

		public UInt256Struct Genesis
		{
			get
			{
				using(_lock.LockRead())
				{
					return _BlockHashesByHeight[0];
				}
			}
		}

		public async Task Save(Stream output)
		{
			using(_lock.LockRead())
			{
				var bytes = new byte[UInt256Struct.Length];
				for(int i = 0; i <= _Height; i++)
				{
					_BlockHashesByHeight[i].ToBytes(bytes);
					await output.WriteAsync(bytes, 0, UInt256Struct.Length);
				}
			}
		}

		public async Task Load(Stream input)
		{
			using(_lock.LockWrite())
			{
				var bytes = new byte[UInt256Struct.Length];
				bool prevSet = false;
				UInt256Struct prev = _BlockHashesByHeight[0];

				while(await input.ReadAsync(bytes, 0, UInt256Struct.Length) == UInt256Struct.Length)
				{
					UInt256Struct tip = new UInt256Struct(bytes);
					if(prevSet)
					{
						TrySetTipNoLock(tip, prev, false);
					}
					else if(tip == prev)
					{
						prevSet = true;
					}
					else
					{
						throw new InvalidOperationException("Unexpected genesis block");
					}
				}

			}
		}

		public override string ToString()
		{
			using(_lock.LockRead())
			{
				return $"Height: {Height}, Hash: {_BlockHashesByHeight[_Height]}";
			}
		}
	}

	public class SlimChainedBlock
	{
		public SlimChainedBlock(uint256 hash, uint256 prev, int height)
		{
			if(hash == null)
				throw new ArgumentNullException(nameof(hash));
			if(prev == null && height != 0)
				throw new ArgumentNullException(nameof(prev));
			if(height < 0)
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
