using NBitcoin.Crypto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class InvalidBrainAddressException : Exception
	{
		public InvalidBrainAddressException(string message)
			: base(message)
		{

		}
	}

	public class MnemonicReference
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="chain"></param>
		/// <param name="blockRepository"></param>
		/// <param name="blockHeight"></param>
		/// <param name="txIndex"></param>
		/// <param name="txOutIndex"></param>
		/// <exception cref="NBitcoin.InvalidBrainAddressException"></exception>
		/// <returns></returns>
		public static async Task<MnemonicReference> CreateAsync
			(ChainBase chain,
			IBlockRepository blockRepository,
			int blockHeight, int txIndex, int txOutIndex)
		{
			var header = chain.GetBlock(blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			var block = await blockRepository.GetBlockAsync(header.HashBlock).ConfigureAwait(false);
			if(block == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");
			return Create(chain, block, blockHeight, txIndex, txOutIndex);
		}

		public static async Task<MnemonicReference> ParseAsync
			(ChainBase chain,
			IBlockRepository blockRepository,
			Wordlist wordList,
			string sentence)
		{
			var reader = new BitReader(wordList.GetIndices(sentence));
			var info = new BitWriter();
			UnBlendChecksum(reader, info);
			var infoReader = info.ToReader();
			var blockHeight = ReadBlockHeight(infoReader);

			var header = chain.GetBlock((int)blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			var block = await blockRepository.GetBlockAsync(header.HashBlock).ConfigureAwait(false);
			if(block == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");
			var txIndex = infoReader.ReadUInt(BitCount(block.Transactions.Count));
			if(txIndex >= block.Transactions.Count)
				throw new InvalidBrainAddressException("The Transaction Index is out of the bound of the block");

			var transaction = block.Transactions[(int)txIndex];
			return Parse(chain, wordList, sentence, transaction, block);
		}



		public static MnemonicReference Create
			(
			ChainBase chain,
			Block block,
			int blockHeight,
			int txIndex,
			int txOutIndex)
		{
			var header = chain.GetBlock(blockHeight);
			if(header == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");
			if(txIndex >= block.Transactions.Count)
				throw new InvalidBrainAddressException("The Transaction Index is out of the bound of the block");
			var transaction = block.Transactions[txIndex];
			return Create(chain, transaction, block, txOutIndex);
		}

		public static MnemonicReference Create(ChainBase chain, Transaction transaction, Block block, int txOutIndex)
		{
			return Create(chain, transaction, block.Filter(transaction.GetHash()), txOutIndex);
		}

		public static MnemonicReference Create
			(ChainBase chain,
			Transaction transaction,
			MerkleBlock merkleBlock,
			int txOutIndex)
		{
			var blockId = merkleBlock.Header.GetHash();
			var merkleRoot = merkleBlock.PartialMerkleTree.TryGetMerkleRoot();
			if(merkleRoot == null || merkleRoot.Hash != merkleBlock.Header.HashMerkleRoot)
				throw new InvalidBrainAddressException("Invalid merkle block");
			if(txOutIndex >= transaction.Outputs.Count)
				throw new InvalidBrainAddressException("The specified txout index is outside of the transaction bounds");
			var matchedLeaf = merkleRoot.GetLeafs().Select((node, index) => new
			{
				node,
				index
			}).FirstOrDefault(_ => _.node.Hash == transaction.GetHash());
			if(matchedLeaf == null)
				throw new InvalidBrainAddressException("Transaction not included in this merkle block");

			var chainedHeader = chain.GetBlock(blockId);
			if(chainedHeader == null)
				throw new InvalidBrainAddressException("The block provided is not in the current chain");
			var blockHeight = chainedHeader.Height;
			var txIndex = matchedLeaf.index;
			var txOut = transaction.Outputs[txOutIndex];
			var block = chain.GetBlock(blockId);

			BitWriter rawAddress = new BitWriter();
			WriteBlockHeight(rawAddress, blockHeight);
			rawAddress.Write((uint)txIndex, BitCount((int)merkleBlock.PartialMerkleTree.TransactionCount));
			rawAddress.Write((uint)txOutIndex, BitCount(transaction.Outputs.Count));

			var checksumSize = RoundTo(rawAddress.Count, 11) - (rawAddress.Count + 1);
			while(checksumSize < ChecksumBitCount)
				checksumSize += 11;

			var checksum = CalculateChecksum(blockId, txIndex, txOutIndex, txOut.ScriptPubKey);

			var finalAddress = new BitWriter();
			BitReader checksumReader = new BitReader(checksum.ToBytes(), checksumSize);
			rawAddress.Write(checksumReader);
			BlendChecksum(finalAddress, rawAddress.ToReader());

			return new MnemonicReference()
			{
				BlockHeight = blockHeight,
				TransactionIndex = txIndex,
				OutputIndex = txOutIndex,
				Checksum = checksumReader.ToBitArray(),
				WordIndices = finalAddress.ToIntegers(),
				Output = transaction.Outputs[txOutIndex],
				Transaction = transaction,
				BlockId = blockId
			};
		}

		public static MnemonicReference Parse
			(ChainBase chain,
			Wordlist wordList,
			string sentence,
			Transaction transaction,
			Block block)
		{
			return Parse(chain, wordList, sentence, transaction, block.Filter(transaction.GetHash()));
		}
		public static MnemonicReference Parse
			(ChainBase chain,
			Wordlist wordList,
			string sentence,
			Transaction transaction,
			MerkleBlock merkleBlock)
		{
			var reader = new BitReader(wordList.GetIndices(sentence));
			var info = new BitWriter();
			UnBlendChecksum(reader, info);
			var infoReader = info.ToReader();
			var blockHeight = ReadBlockHeight(infoReader);
			var header = chain.GetBlock((int)blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			if(header.HashBlock != merkleBlock.Header.GetHash())
				throw new InvalidBrainAddressException("The provided merkleblock do not match the block of the sentence");
			var blockId = header.HashBlock;
			MerkleNode root = merkleBlock.PartialMerkleTree.TryGetMerkleRoot();

			if(root == null || root.Hash != header.Header.HashMerkleRoot)
				throw new InvalidBrainAddressException("Invalid partial merkle tree");

			var txIndex = infoReader.ReadUInt(BitCount((int)merkleBlock.PartialMerkleTree.TransactionCount));
			var txLeaf = root.GetLeafs().Skip((int)txIndex).FirstOrDefault();
			if(txLeaf == null || txLeaf.Hash != transaction.GetHash())
				throw new InvalidBrainAddressException("The transaction do not appear in the block");
			var txOutIndex = infoReader.ReadUInt(BitCount(transaction.Outputs.Count));
			if(txOutIndex >= transaction.Outputs.Count)
				throw new InvalidBrainAddressException("The specified txout index is outside of the transaction bounds");
			var txOut = transaction.Outputs[txOutIndex];
			var checksum = AssertChecksum(infoReader, blockId, txIndex, txOutIndex, txOut.ScriptPubKey);
			return new MnemonicReference()
			{
				BlockHeight = (int)blockHeight,
				TransactionIndex = (int)txIndex,
				WordIndices = reader.ToWriter().ToIntegers(),
				Checksum = checksum.ToBitArray(),
				Output = transaction.Outputs[txOutIndex],
				OutputIndex = (int)txOutIndex,
				BlockId = blockId,
				Transaction = transaction
			};
		}

		private static void Xor(BitWriter result, BitReader a, int count, BitReader b)
		{
			while(count != 0)
			{
				if(b.Position == b.Count)
					b.Position = 0;
				result.Write(a.Read() ^ b.Read());
				count--;
			}
		}

		private static uint256 CalculateChecksum(uint256 blockId, int txIndex, int txOutIndex, Script scriptPubKey)
		{
			//All in little endian
			var hashed =
				blockId
				.ToBytes()
				.Concat(Utils.ToBytes((uint)txIndex, true))
				.Concat(Utils.ToBytes((uint)txOutIndex, true))
				.Concat(scriptPubKey.ToBytes(true))
				.ToArray();
			return Hashes.Hash256(hashed);
		}

		private static BitReader AssertChecksum(BitReader rawAddress, uint256 blockId, uint txIndex, uint txOutIndex, Script scriptPubKey)
		{
			var expectedChecksum = CalculateChecksum(blockId, (int)txIndex, (int)txOutIndex, scriptPubKey);
			var expectChecksumBits = new BitReader(expectedChecksum.ToBytes(), rawAddress.Count - rawAddress.Position);
			if(!rawAddress.Same(expectChecksumBits))
				throw new InvalidBrainAddressException("Invalid checksum");
			expectChecksumBits.Position = 0;
			return expectChecksumBits;
		}

		private static void WriteBlockHeight(BitWriter info, int blockHeight)
		{
			if(blockHeight <= 1048575)
			{
				info.Write(false);
				info.Write((uint)blockHeight, 20);
			}
			else
			{
				info.Write(true);
				info.Write((uint)blockHeight, 22);
			}
		}
		private static int ReadBlockHeight(BitReader info)
		{
			return (int)(info.Read() ? info.ReadUInt(22) : info.ReadUInt(20));
		}

		const int ChecksumBitCount = 20;


		static void UnBlendChecksum(BitReader finalAddress, BitWriter rawAddress)
		{
			finalAddress.Position = finalAddress.Count - 1 - ChecksumBitCount;
			BitWriter encryptionKey = new BitWriter();
			encryptionKey.Write(finalAddress, ChecksumBitCount);
			if(finalAddress.Read())
				throw new InvalidBrainAddressException("Invalid version bit");
			finalAddress.Position = 0;
			Xor(rawAddress, finalAddress, finalAddress.Count - ChecksumBitCount - 1, encryptionKey.ToReader());
			rawAddress.Write(encryptionKey.ToReader());
		}

		static void BlendChecksum(BitWriter finalAddress, BitReader rawAddress)
		{
			rawAddress.Position = rawAddress.Count - ChecksumBitCount;

			var encryptionKey = new BitWriter();
			encryptionKey.Write(rawAddress);

			rawAddress.Position = 0;

			Xor(finalAddress, rawAddress, rawAddress.Count - ChecksumBitCount, encryptionKey.ToReader());

			finalAddress.Write(encryptionKey.ToReader());
			finalAddress.Write(false); //version
		}

		static int RoundTo(int value, int roundTo)
		{
			var result = (value / roundTo) * roundTo;
			if(value % roundTo != 0)
				result += roundTo;
			return result;
		}
		static int BitCount(int possibilities)
		{
			possibilities = Math.Max(0, possibilities);
			possibilities--;
			int bitCount = 0;
			while(possibilities != 0)
			{
				possibilities = possibilities >> 1;
				bitCount++;
			}
			return bitCount;
		}

		private MnemonicReference()
		{

		}

		public override string ToString()
		{
			return ToString(Wordlist.English);
		}

		public string ToString(Wordlist wordlist)
		{
			return wordlist.GetSentence(WordIndices);
		}

		public int[] WordIndices
		{
			get;
			private set;
		}

		public BitArray Checksum
		{
			get;
			private set;
		}

		public TxOut Output
		{
			get;
			private set;
		}

		public uint256 BlockId
		{
			get;
			private set;
		}

		public int BlockHeight
		{
			get;
			private set;
		}
		public int TransactionIndex
		{
			get;
			private set;
		}
		public int OutputIndex
		{
			get;
			private set;
		}

		public Transaction Transaction
		{
			get;
			private set;
		}
	}
}
