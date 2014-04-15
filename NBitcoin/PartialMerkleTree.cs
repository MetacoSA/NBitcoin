using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class PartialMerkleTree : IBitcoinSerializable
	{
		// the total number of transactions in the block
		uint nTransactions;

		// node-is-parent-of-matched-txid bits
		List<bool> vBits = new List<bool>();

		// txids and internal hashes
		List<uint256> vHash = new List<uint256>();

		// flag set when encountering invalid data
		bool fBad;

		// helper function to efficiently calculate the number of nodes at given height in the merkle tree
		uint CalcTreeWidth(int height)
		{
			return (uint)(nTransactions + (1 << height) - 1) >> height;
		}
	

		// serialization implementation
		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref nTransactions);
			stream.ReadWrite(ref vHash);
			byte[] vBytes = null;
			if(!stream.Serializing)
			{
				stream.ReadWriteAsVarString(ref vBytes);
				PartialMerkleTree us = this; //Might need copy
				us.vBits = us.vBits.Take(vBytes.Length * 8).ToList();
				for(int p = 0 ; p < us.vBits.Count ; p++)
					us.vBits[p] = (vBytes[p / 8] & (1 << (p % 8))) != 0;
				us.fBad = false;

			}
			else
			{
				vBytes = new byte[(vBits.Count + 7) / 8];
				for(int p = 0 ; p < vBits.Count ; p++)
					vBytes[p / 8] |= (byte)(ToByte(vBits[p]) << (p % 8));
				stream.ReadWriteAsVarString(ref vBytes);
			}
		}

		private byte ToByte(bool v)
		{
			return (byte)(v ? 1 : 0);
		}

		#endregion

		// Construct a partial merkle tree from a list of transaction id's, and a mask that selects a subset of them
		public PartialMerkleTree(uint256[] vTxid, bool[] vMatch)
		{
			fBad = false;
			nTransactions = (uint)vTxid.Length;

			// calculate height of tree
			int nHeight = 0;
			while(CalcTreeWidth(nHeight) > 1)
				nHeight++;

			// traverse the partial tree
			TraverseAndBuild(nHeight, 0, vTxid, vMatch);
		}

		// recursive function that traverses tree nodes, storing the data as bits and hashes
		private void TraverseAndBuild(int height, uint pos, uint256[] vTxid, bool[] vMatch)
		{
			// determine whether this node is the parent of at least one matched txid
			bool fParentOfMatch = false;
			for(uint p = pos << height ; p < (pos + 1) << height && p < nTransactions ; p++)
				fParentOfMatch |= vMatch[p];
			// store as flag bit
			vBits.Add(fParentOfMatch);
			if(height == 0 || !fParentOfMatch)
			{
				// if at height 0, or nothing interesting below, store hash and stop
				vHash.Add(CalcHash(height, pos, vTxid));
			}
			else
			{
				// otherwise, don't store any hash, but descend into the subtrees
				TraverseAndBuild(height - 1, pos * 2, vTxid, vMatch);
				if(pos * 2 + 1 < CalcTreeWidth(height - 1))
					TraverseAndBuild(height - 1, pos * 2 + 1, vTxid, vMatch);
			}
		}

		// calculate the hash of a node in the merkle tree (at leaf level: the txid's themself)
		private uint256 CalcHash(int height, uint pos, uint256[] vTxid)
		{
			if(height == 0)
			{
				// hash at height 0 is the txids themself
				return vTxid[pos];
			}
			else
			{
				// calculate left hash
				uint256 left = CalcHash(height - 1, pos * 2, vTxid), right;
				// calculate right hash if not beyong the end of the array - copy left hash otherwise1
				if(pos * 2 + 1 < CalcTreeWidth(height - 1))
					right = CalcHash(height - 1, pos * 2 + 1, vTxid);
				else
					right = left;
				// combine subhashes
				return Hashes.Hash256(left.ToBytes().Concat(right.ToBytes()).ToArray());
			}
		}

		public PartialMerkleTree()
		{
			fBad = true;
			nTransactions = 0;
		}

		// extract the matching txid's represented by this partial merkle tree.
		// returns the merkle root, or 0 in case of failure
		public uint256 ExtractMatches(List<uint256> vMatch)
		{
			vMatch.Clear();
			// An empty set will not work
			if(nTransactions == 0)
				return 0;
			// check for excessively high numbers of transactions
			if(nTransactions > Block.MAX_BLOCK_SIZE / 60) // 60 is the lower bound for the size of a serialized CTransaction
				return 0;
			// there can never be more hashes provided than one for every txid
			if(vHash.Count > nTransactions)
				return 0;
			// there must be at least one bit per node in the partial tree, and at least one node per hash
			if(vBits.Count < vHash.Count)
				return 0;
			// calculate height of tree
			int nHeight = 0;
			while(CalcTreeWidth(nHeight) > 1)
				nHeight++;
			// traverse the partial tree
			int nBitsUsed = 0, nHashUsed = 0;
			uint256 hashMerkleRoot = TraverseAndExtract(nHeight, 0, ref nBitsUsed, ref nHashUsed, vMatch);
			// verify that no problems occured during the tree traversal
			if(fBad)
				return 0;
			// verify that all bits were consumed (except for the padding caused by serializing it as a byte sequence)
			if((nBitsUsed + 7) / 8 != (vBits.Count + 7) / 8)
				return 0;
			// verify that all hashes were consumed
			if(nHashUsed != vHash.Count)
				return 0;
			return hashMerkleRoot;
		}

		// recursive function that traverses tree nodes, consuming the bits and hashes produced by TraverseAndBuild.
		// it returns the hash of the respective node.
		private uint256 TraverseAndExtract(int height, uint pos, ref int nBitsUsed, ref int nHashUsed, List<uint256> vMatch)
		{
			if(nBitsUsed >= vBits.Count)
			{
				// overflowed the bits array - failure
				fBad = true;
				return 0;
			}
			bool fParentOfMatch = vBits[nBitsUsed++];
			if(height == 0 || !fParentOfMatch)
			{
				// if at height 0, or nothing interesting below, use stored hash and do not descend
				if(nHashUsed >= vHash.Count)
				{
					// overflowed the hash array - failure
					fBad = true;
					return 0;
				}
				uint256 hash = vHash[nHashUsed++];
				if(height == 0 && fParentOfMatch) // in case of height 0, we have a matched txid
					vMatch.Add(hash);
				return hash;
			}
			else
			{
				// otherwise, descend into the subtrees to extract matched txids and hashes
				uint256 left = TraverseAndExtract(height - 1, pos * 2, ref nBitsUsed, ref nHashUsed, vMatch), right;
				if(pos * 2 + 1 < CalcTreeWidth(height - 1))
					right = TraverseAndExtract(height - 1, pos * 2 + 1, ref nBitsUsed, ref nHashUsed, vMatch);
				else
					right = left;
				// and combine them before returning
				return Hashes.Hash256(left.ToBytes().Concat(right.ToBytes()).ToArray());
			}
		}
	}
}
