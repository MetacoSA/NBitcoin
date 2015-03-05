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

	public class BrainAddress
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
		public static async Task<BrainAddress> FetchAsync(ChainBase chain,
											  IBlockRepository blockRepository,
											  int blockHeight, int txIndex, int txOutIndex)
		{
			var header = chain.GetBlock(blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			var block = await blockRepository.GetBlockAsync(header.HashBlock).ConfigureAwait(false);
			if(block == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");
			var transaction = block.Transactions[txIndex];
			if(transaction.Outputs.Count <= txOutIndex)
				throw new InvalidBrainAddressException("The TxOut index is out of the transaction");

			BitWriter info = new BitWriter();
			info.Write(true); //Version
			info.Write((uint)blockHeight, 22);
			info.Write((uint)txIndex, BitCount(block.Transactions.Count));
			info.Write((uint)txOutIndex, BitCount(transaction.Outputs.Count));

			var bytes = info.ToBytes();
			var hashed = bytes.Concat(header.HashBlock.ToBytes()).ToArray();
			var checksum = Hashes.SHA256(hashed);
			BitReader checksumReader = new BitReader(checksum, ChecksumBitCount);

			BitWriter result = new BitWriter();
			BlendChecksum(result, info.ToReader(), checksumReader);

			return new BrainAddress()
			{
				BlockHeight = blockHeight,
				TransactionIndex = txIndex,
				OutputIndex = txOutIndex,
				Checksum = checksumReader.ToBitArray(),
				Indices = result.ToIntegers(),
				Output = transaction.Outputs[txOutIndex]
			};
		}

		public static async Task<BrainAddress> FetchAsync(ChainBase chain,
											  IBlockRepository blockRepository,
											  Wordlist wordList,
											  string sentence)
		{
			var reader = new BitReader(wordList.GetIndices(sentence));
			var checksum = new BitWriter();
			var info = new BitWriter();
			UnBlendChecksum(reader, info, checksum);

			var infoReader = info.ToReader();
			var version = infoReader.Read();
			if(!version)
				throw new InvalidBrainAddressException("Invalid version number");
			var blockHeight = infoReader.ReadUInt(22);

			var header = chain.GetBlock((int)blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			var block = await blockRepository.GetBlockAsync(header.HashBlock).ConfigureAwait(false);
			if(block == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");

			var txIndex = infoReader.ReadUInt(BitCount(block.Transactions.Count));
			var transaction = block.Transactions[(int)txIndex];
			var txOutIndex = infoReader.ReadUInt(BitCount(block.Transactions[(int)txIndex].Outputs.Count));
			AssertChecksum(checksum, info, header);

			return new BrainAddress()
			{
				BlockHeight = (int)blockHeight,
				OutputIndex = (int)txOutIndex,
				TransactionIndex = (int)txIndex,
				Indices = reader.ToWriter().ToIntegers(),
				Checksum = checksum.ToBitArray(),
				Output = transaction.Outputs[txOutIndex]
			};
		}

		public static BrainAddress Fetch(
											  ChainBase chain,
											  Wordlist wordList,
											  string sentence,
											  Transaction transaction,
											  MerkleBlock merkleBlock)
		{
			var reader = new BitReader(wordList.GetIndices(sentence));
			var checksum = new BitWriter();
			var info = new BitWriter();
			UnBlendChecksum(reader, info, checksum);
			var infoReader = info.ToReader();
			var version = infoReader.Read();
			if(!version)
				throw new InvalidBrainAddressException("Invalid version number");
			var blockHeight = infoReader.ReadUInt(22);
			var header = chain.GetBlock((int)blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			if(header.HashBlock != merkleBlock.Header.GetHash())
				throw new InvalidBrainAddressException("The provided merkleblock do not match the block of the sentence");

			MerkleNode root = merkleBlock.PartialMerkleTree.TryGetMerkleRoot();

			if(root == null || root.Hash != header.Header.HashMerkleRoot)
				throw new InvalidBrainAddressException("Invalid partial merkle tree");

			var txIndex = infoReader.ReadUInt(BitCount((int)merkleBlock.PartialMerkleTree.TransactionCount));
			var txLeaf = root.GetLeafs().Skip((int)txIndex).FirstOrDefault();
			if(txLeaf == null || txLeaf.Hash != transaction.GetHash())
				throw new InvalidBrainAddressException("The transaction do not appear in the block");
			var txOutIndex = infoReader.ReadUInt(BitCount(transaction.Outputs.Count));

			AssertChecksum(checksum, info, header);

			return new BrainAddress()
			{
				BlockHeight = (int)blockHeight,
				OutputIndex = (int)txOutIndex,
				TransactionIndex = (int)txIndex,
				Indices = reader.ToWriter().ToIntegers(),
				Checksum = checksum.ToBitArray(),
				Output = transaction.Outputs[txOutIndex]
			};
		}

		private static void AssertChecksum(BitWriter checksum, BitWriter info, ChainedBlock header)
		{
			var bytes = info.ToBytes();
			var hashed = bytes.Concat(header.HashBlock.ToBytes()).ToArray();
			BitReader expectedChecksum = new BitReader(Hashes.SHA256(hashed), ChecksumBitCount);
			if(!expectedChecksum.ToBitArray().OfType<bool>().SequenceEqual(checksum.ToBitArray().OfType<bool>()))
				throw new InvalidBrainAddressException("Invalid checksum");
		}

		const int ChecksumBitCount = 16;


		static void UnBlendChecksum(BitReader reader, BitWriter info, BitWriter checksum)
		{
			int wordCount = RoundTo(reader.Count, 11) / 11;
			int checksumBitPerWord = ChecksumBitCount / wordCount;

			for(int i = 0 ; i < wordCount - 1 ; i++)
			{
				checksum.Write(reader, checksumBitPerWord);
				info.Write(reader, 11 - checksumBitPerWord);
			}
			checksum.Write(reader, ChecksumBitCount - checksum.Count);
			info.Write(reader);
			while(reader.Count != reader.Position)
				if(reader.Read())
					throw new InvalidBrainAddressException("Invalid checksum");
		}

		static void BlendChecksum(BitWriter result, BitReader info, BitReader checksum)
		{
			int wordCount = RoundTo(checksum.Count + info.Count, 11) / 11;
			int checksumBitPerWord = checksum.Count / wordCount;

			for(int i = 0 ; i < wordCount - 1 ; i++)
			{
				result.Write(checksum, checksumBitPerWord);
				result.Write(info, 11 - checksumBitPerWord);
			}
			result.Write(checksum);
			result.Write(info);
			while(result.Count % 11 != 0)
				result.Write(false);
		}

		static int RoundTo(int value, int roundTo)
		{
			var result = (value / roundTo) * roundTo;
			if(value % roundTo != 0)
				result += roundTo;
			return result;
		}


		static int BitCount(int value)
		{
			int bitCount = 0;
			while(value != 0)
			{
				value = value >> 1;
				bitCount++;
			}
			return bitCount;
		}

		private BrainAddress()
		{

		}

		public override string ToString()
		{
			return ToString(Wordlist.English);
		}

		public string ToString(Wordlist wordlist)
		{
			return wordlist.GetSentence(Indices);
		}

		public int[] Indices
		{
			get;
			private set;
		}

		public int OutputIndex
		{
			get;
			private set;
		}

		public int TransactionIndex
		{
			get;
			private set;
		}

		public int BlockHeight
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
			set;
		}
	}
}
