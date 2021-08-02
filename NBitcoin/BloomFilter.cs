using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	[Flags]
	public enum BloomFlags : byte
	{
		UPDATE_NONE = 0,
		UPDATE_ALL = 1,
		// Only adds outpoints to the filter if the output is a pay-to-pubkey/pay-to-multisig script
		UPDATE_P2PUBKEY_ONLY = 2,
		UPDATE_MASK = 3,
	};

	/// <summary>
	/// Used by SPV client, represent the set of interesting addresses tracked by SPV client with plausible deniability
	/// </summary>
	public class BloomFilter : IBitcoinSerializable
	{
		public BloomFilter()
		{

		}
		// 20,000 items with fp rate < 0.1% or 10,000 items and <0.0001%
		const uint MAX_BLOOM_FILTER_SIZE = 36000; // bytes
		const uint MAX_HASH_FUNCS = 50;
		const decimal LN2SQUARED = 0.4804530139182014246671025263266649717305529515945455M;
		const decimal LN2 = 0.6931471805599453094172321214581765680755001343602552M;


		byte[] vData;
		uint nHashFuncs;
		uint nTweak;
		byte nFlags;
		private bool isFull = false;
		private bool isEmpty;


		public BloomFilter(int nElements, double nFPRate, BloomFlags nFlagsIn = BloomFlags.UPDATE_ALL)
			: this(nElements, nFPRate, RandomUtils.GetUInt32(), nFlagsIn)
		{
		}


		public BloomFilter(int nElements, double nFPRate, uint nTweakIn, BloomFlags nFlagsIn = BloomFlags.UPDATE_ALL)
		{
			// The ideal size for a bloom filter with a given number of elements and false positive rate is:
			// - nElements * log(fp rate) / ln(2)^2
			// We ignore filter parameters which will create a bloom filter larger than the protocol limits
			vData = new byte[Math.Min((uint)(-1 / LN2SQUARED * nElements * (decimal)Math.Log(nFPRate)), MAX_BLOOM_FILTER_SIZE) / 8];
			//vData(min((unsigned int)(-1  / LN2SQUARED * nElements * log(nFPRate)), MAX_BLOOM_FILTER_SIZE * 8) / 8),
			// The ideal number of hash functions is filter size * ln(2) / number of elements
			// Again, we ignore filter parameters which will create a bloom filter with more hash functions than the protocol limits
			// See http://en.wikipedia.org/wiki/Bloom_filter for an explanation of these formulas

			this.nHashFuncs = Math.Min((uint)(vData.Length * 8 / nElements * LN2), MAX_HASH_FUNCS);
			this.nTweak = nTweakIn;
			this.nFlags = (byte)nFlagsIn;


		}

		uint Hash(uint nHashNum, byte[] vDataToHash)
		{
			// 0xFBA4C795 chosen as it guarantees a reasonable bit difference between nHashNum values.
			return (uint)(Hashes.MurmurHash3(nHashNum * 0xFBA4C795 + nTweak, vDataToHash) % (vData.Length * 8));
		}

		public void Insert(byte[] vKey)
		{
			if (isFull)
				return;
			for (uint i = 0; i < nHashFuncs; i++)
			{
				uint nIndex = Hash(i, vKey);
				// Sets bit nIndex of vData
				vData[nIndex >> 3] |= (byte)(1 << (7 & (int)nIndex));
			}
			isEmpty = false;
		}

		public bool Contains(byte[] vKey)
		{
			if (isFull)
				return true;
			if (isEmpty)
				return false;
			for (uint i = 0; i < nHashFuncs; i++)
			{
				uint nIndex = Hash(i, vKey);
				// Checks bit nIndex of vData
				if ((vData[nIndex >> 3] & (byte)(1 << (7 & (int)nIndex))) == 0)
					return false;
			}
			return true;
		}
		public bool Contains(OutPoint outPoint)
		{
			if (outPoint == null)
				throw new ArgumentNullException(nameof(outPoint));
			return Contains(outPoint.ToBytes());
		}

		public bool Contains(uint256 hash)
		{
			if (hash == null)
				throw new ArgumentNullException(nameof(hash));
			return Contains(hash.ToBytes());
		}

		public void Insert(OutPoint outPoint)
		{
			if (outPoint == null)
				throw new ArgumentNullException(nameof(outPoint));
			Insert(outPoint.ToBytes());
		}

		public void Insert(uint256 value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			Insert(value.ToBytes());
		}

		public bool IsWithinSizeConstraints()
		{
			return vData.Length <= MAX_BLOOM_FILTER_SIZE && nHashFuncs <= MAX_HASH_FUNCS;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWriteAsVarString(ref vData);
			stream.ReadWrite(ref nHashFuncs);
			stream.ReadWrite(ref nTweak);
			stream.ReadWrite(ref nFlags);
		}

		#endregion



		public bool IsRelevantAndUpdate(Transaction tx)
		{
			if (tx == null)
				throw new ArgumentNullException(nameof(tx));
			var hash = tx.GetHash();
			bool fFound = false;
			// Match if the filter contains the hash of tx
			//  for finding tx when they appear in a block
			if (isFull)
				return true;
			if (isEmpty)
				return false;
			if (Contains(hash))
				fFound = true;

			for (uint i = 0; i < tx.Outputs.Count; i++)
			{
				TxOut txout = tx.Outputs[(int)i];
				// Match if the filter contains any arbitrary script data element in any scriptPubKey in tx
				// If this matches, also add the specific output that was matched.
				// This means clients don't have to update the filter themselves when a new relevant tx 
				// is discovered in order to find spending transactions, which avoids round-tripping and race conditions.
				foreach (Op op in txout.ScriptPubKey.ToOps())
				{
					if (op.PushData != null && op.PushData.Length != 0 && Contains(op.PushData))
					{
						fFound = true;
						if ((nFlags & (byte)BloomFlags.UPDATE_MASK) == (byte)BloomFlags.UPDATE_ALL)
							Insert(new OutPoint(hash, i));
						else if ((nFlags & (byte)BloomFlags.UPDATE_MASK) == (byte)BloomFlags.UPDATE_P2PUBKEY_ONLY)
						{
							var template = StandardScripts.GetTemplateFromScriptPubKey(txout.ScriptPubKey);
							if (template is PayToPubkeyTemplate || template is PayToMultiSigTemplate)
								Insert(new OutPoint(hash, i));
						}
						break;
					}
				}
			}

			if (fFound)
				return true;

			foreach (TxIn txin in tx.Inputs)
			{
				// Match if the filter contains an outpoint tx spends
				if (Contains(txin.PrevOut))
					return true;

				// Match if the filter contains any arbitrary script data element in any scriptSig in tx
				foreach (Op op in txin.ScriptSig.ToOps())
				{
					if (op.PushData != null && op.PushData.Length != 0 && Contains(op.PushData))
						return true;
				}
			}

			return false;
		}
	}
}
