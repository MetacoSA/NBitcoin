using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class MerkleBlock : IBitcoinSerializable, IEquatable<MerkleBlock>
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


			for(uint i = 0; i < block.Transactions.Count; i++)
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
			for(int i = 0; i < block.Transactions.Count; i++)
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

		#region IEquatable Members

		public override bool Equals(object obj) => obj is MerkleBlock && this == (MerkleBlock)obj;
		public bool Equals(MerkleBlock other) => this == other;
		public override int GetHashCode()
		{
			var hash = Header.GetHash().GetHashCode();
			hash = hash ^ Header.HashPrevBlock.GetHashCode();
			hash = hash ^ Header.HashMerkleRoot.GetHashCode();
			foreach (uint256 txhash in PartialMerkleTree.GetMatchedTransactions())
				hash = hash ^ txhash.GetHashCode();

			return hash;
		}

		public static bool operator ==(MerkleBlock x, MerkleBlock y)
		{
			if (x.Header.GetHash() != y.Header.GetHash())
				return false;
			if (x.Header.HashPrevBlock != y.Header.HashPrevBlock)
				return false;

			if (x.Header.HashMerkleRoot != y.Header.HashMerkleRoot)
				return false;
			if (x.PartialMerkleTree.TransactionCount != y.PartialMerkleTree.TransactionCount)
				return false;
			if (x.PartialMerkleTree.TransactionCount == 0) return true;

			if (!x.PartialMerkleTree.GetMatchedTransactions().SequenceEqual(y.PartialMerkleTree.GetMatchedTransactions()))
				return false;

			return true;
		}

		public static bool operator !=(MerkleBlock x, MerkleBlock y) => !(x == y);

		#endregion
	}
}
