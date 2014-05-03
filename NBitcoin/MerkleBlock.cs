using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class MerkleBlock : IBitcoinSerializable
	{
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
		PartialMerkleTree txn;

		public PartialMerkleTree Txn
		{
			get
			{
				return txn;
			}
			set
			{
				txn = value;
			}
		}


		// Public only for unit testing and relay testing
		// (not relayed)
		public List<Tuple<uint, uint256>> vMatchedTxn = new List<Tuple<uint, uint256>>();

		// Create from a CBlock, filtering transactions according to filter
		// Note that this will call IsRelevantAndUpdate on the filter for each transaction,
		// thus the filter will likely be modified.
		public MerkleBlock(Block block, BloomFilter filter)
		{
			header = block.Header;

			List<bool> vMatch = new List<bool>();
			List<uint256> vHashes = new List<uint256>();


			for(uint i = 0 ; i < block.Vtx.Length ; i++)
			{
				uint256 hash = block.Vtx[i].GetHash();
				if(filter.IsRelevantAndUpdate(block.Vtx[i]))
				{
					vMatch.Add(true);
					vMatchedTxn.Add(Tuple.Create(i, hash));
				}
				else
					vMatch.Add(false);
				vHashes.Add(hash);
			}

			txn = new PartialMerkleTree(vHashes.ToArray(), vMatch.ToArray());
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref header);
			stream.ReadWrite(ref txn);
		}

		#endregion
	}
}
