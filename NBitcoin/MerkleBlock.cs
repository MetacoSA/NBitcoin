using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class MerkleBlock : IBitcoinSerializable
	{
		public MerkleBlock()
		{

		}
		// Public only for unit testing
		BlockHeader header;

		public BlockHeader Header
		{
			get
			{
				return header;
			}
			set
			{
				header = value;
			}
		}
		PartialMerkleTree _PartialMerkleTree;

		public PartialMerkleTree PartialMerkleTree
		{
			get
			{
				return _PartialMerkleTree;
			}
			set
			{
				_PartialMerkleTree = value;
			}
		}

		// Create from a CBlock, filtering transactions according to filter
		// Note that this will call IsRelevantAndUpdate on the filter for each transaction,
		// thus the filter will likely be modified.
		public MerkleBlock(Block block, BloomFilter filter)
		{
			header = block.Header;

			List<bool> vMatch = new List<bool>();
			List<uint256> vHashes = new List<uint256>();


			for (uint i = 0; i < block.Transactions.Count; i++)
			{
				uint256 hash = block.Transactions[(int)i].GetHash();
				vMatch.Add(filter.IsRelevantAndUpdate(block.Transactions[(int)i]));
				vHashes.Add(hash);
			}

			_PartialMerkleTree = new PartialMerkleTree(vHashes.ToArray(), vMatch.ToArray());
		}

		public MerkleBlock(Block block, uint256[] txIds)
		{
			header = block.Header;

			List<bool> vMatch = new List<bool>();
			List<uint256> vHashes = new List<uint256>();
			for (int i = 0; i < block.Transactions.Count; i++)
			{
				var hash = block.Transactions[i].GetHash();
				vHashes.Add(hash);
				vMatch.Add(txIds.Contains(hash));
			}
			_PartialMerkleTree = new PartialMerkleTree(vHashes.ToArray(), vMatch.ToArray());
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref header);
			stream.ReadWrite(ref _PartialMerkleTree);
		}

		#endregion
	}
}
