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
			var w = wordList.GetWords(sentence).Length;
			var finalAddress = wordList.ToBits(sentence);
			var rawAddress = DecryptFinalAddress(finalAddress);

			int blockHeight;
			int x = DecodeBlockHeight(rawAddress, out blockHeight);

			var header = chain.GetBlock(blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			var block = await blockRepository.GetBlockAsync(header.HashBlock).ConfigureAwait(false);
			if(block == null || block.GetHash() != header.HashBlock)
				throw new InvalidBrainAddressException("This block does not exists");

			int y1 = BitCount((int)block.Transactions.Count);
			int y2 = 11 * w - 1 - x - c;
			int y = Math.Min(y1, y2);
			int txIndex = Decode(Substring(rawAddress, x, y));
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


			BitArray encodedBlockHeight = EncodeBlockHeight(blockHeight);
			int x = encodedBlockHeight.Length;

			//ymin = ceiling(log(txIndex + 1, 2))
			int ymin = BitCount(txIndex + 1);
			//zmin = ceiling(log(outputIndex + 1, 2))
			int zmin = BitCount(txOutIndex + 1);

			//w = ceiling((x + ymin + zmin + c + 1)/11)
			int w = RoundTo(x + ymin + zmin + c + 1, 11) / 11;
			int y = 0;
			int z = 0;
			for( ; ; w++)
			{
				int y1 = BitCount((int)merkleBlock.PartialMerkleTree.TransactionCount);
				int y2 = 11 * w - 1 - x - c;
				y = Math.Min(y1, y2);
				if(ymin > y)
					continue;
				int z1 = BitCount(transaction.Outputs.Count);
				int z2 = 11 * w - 1 - x - y - c;
				z = Math.Min(z1, z2);
				if(zmin > z)
					continue;
				break;
			}

			var cs = 11 * w - 1 - x - y - z;
			var checksum = CalculateChecksum(blockId, txIndex, txOutIndex, txOut.ScriptPubKey, cs);

			var rawAddress = Concat(encodedBlockHeight, Encode(txIndex, y), Encode(txOutIndex, z), checksum);

			var finalAddress = EncryptRawAddress(rawAddress);

			return new MnemonicReference()
			{
				BlockHeight = blockHeight,
				TransactionIndex = txIndex,
				OutputIndex = txOutIndex,
				Checksum = checksum,
				WordIndices = Wordlist.ToIntegers(finalAddress),
				Output = transaction.Outputs[txOutIndex],
				Transaction = transaction,
				BlockId = blockId
			};
		}


		private static BitArray Concat(params BitArray[] arrays)
		{
			BitArray result = new BitArray(arrays.Select(a => a.Length).Sum());
			int i = 0;
			foreach(var v in arrays.SelectMany(a => a.OfType<bool>()))
			{
				result.Set(i, v);
				i++;
			}
			return result;
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
			var indices = wordList.ToIndices(sentence);

			//Step1: Determine w = number of words in the mnemonic code 
			int w = indices.Length;

			//Convert mnemonic code into finalAddress following BIP-0039
			var finalAddress = Wordlist.ToBits(indices);

			var rawAddress = DecryptFinalAddress(finalAddress);
			int blockHeight = 0;
			var x = DecodeBlockHeight(rawAddress, out blockHeight);

			var header = chain.GetBlock((int)blockHeight);
			if(header == null)
				throw new InvalidBrainAddressException("This block does not exists");
			if(header.HashBlock != merkleBlock.Header.GetHash())
				throw new InvalidBrainAddressException("The provided merkleblock do not match the block of the sentence");
			var blockId = header.HashBlock;
			MerkleNode root = merkleBlock.PartialMerkleTree.TryGetMerkleRoot();
			if(root == null || root.Hash != header.Header.HashMerkleRoot)
				throw new InvalidBrainAddressException("Invalid partial merkle tree");

			int y1 = BitCount((int)merkleBlock.PartialMerkleTree.TransactionCount);
			int y2 = 11 * w - 1 - x - c;
			int y = Math.Min(y1, y2);
			int txIndex = Decode(Substring(rawAddress, x, y));

			var txLeaf = root.GetLeafs().Skip((int)txIndex).FirstOrDefault();
			if(txLeaf == null || txLeaf.Hash != transaction.GetHash())
				throw new InvalidBrainAddressException("The transaction do not appear in the block");

			int z1 = BitCount(transaction.Outputs.Count);
			int z2 = 11 * w - 1 - x - y - c;
			int z = Math.Min(z1, z2);
			int outputIndex = Decode(Substring(rawAddress, x + y, z));

			if(outputIndex >= transaction.Outputs.Count)
				throw new InvalidBrainAddressException("The specified txout index is outside of the transaction bounds");
			var txOut = transaction.Outputs[outputIndex];


			var cs = 11 * w - 1 - x - y - z;
			var actualChecksum = Substring(rawAddress, x + y + z, cs);
			var expectedChecksum = CalculateChecksum(blockId, txIndex, outputIndex, txOut.ScriptPubKey, cs);

			if(!actualChecksum.OfType<bool>().SequenceEqual(expectedChecksum.OfType<bool>()))
				throw new InvalidBrainAddressException("Invalid checksum");

			return new MnemonicReference()
			{
				BlockHeight = (int)blockHeight,
				TransactionIndex = (int)txIndex,
				WordIndices = indices,
				Checksum = actualChecksum,
				Output = transaction.Outputs[outputIndex],
				OutputIndex = (int)outputIndex,
				BlockId = blockId,
				Transaction = transaction
			};
		}

		const int c = 20;
		private static BitArray DecryptFinalAddress(BitArray finalAddress)
		{
			if(finalAddress[finalAddress.Length - 1] != false)
				throw new InvalidBrainAddressException("Invalid version bit");
			var encryptionKey = Substring(finalAddress, finalAddress.Length - 1 - c, c);
			var encryptedAddress = Xor(Substring(finalAddress, 0, finalAddress.Length - 1 - c), encryptionKey);
			return Concat(encryptedAddress, encryptionKey);
		}
		private static BitArray EncryptRawAddress(BitArray rawAddress)
		{
			var encryptionKey = Substring(rawAddress, rawAddress.Length - c, c);
			var encryptedAddress = Xor(Substring(rawAddress, 0, rawAddress.Length - c), encryptionKey);
			var finalAddress = Concat(encryptedAddress, encryptionKey, new BitArray(new[] { false }));
			return finalAddress;
		}


		static BitArray Xor(BitArray a, BitArray b)
		{
			BitArray result = new BitArray(a.Length);
			for(int i = 0, y = 0 ; i < a.Length ; i++, y++)
			{
				if(y >= b.Length)
					y = 0;
				result.Set(i, a.Get(i) ^ b.Get(y));
			}
			return result;
		}

		private static BitArray Substring(BitArray input, int from, int count)
		{
			return new BitArray(input.OfType<bool>().Skip(from).Take(count).ToArray());
		}

		private static BitArray CalculateChecksum(uint256 blockId, int txIndex, int txOutIndex, Script scriptPubKey, int bitCount)
		{
			//All in little endian
			var hashed =
				blockId
				.ToBytes(true)
				.Concat(Utils.ToBytes((uint)txIndex, true))
				.Concat(Utils.ToBytes((uint)txOutIndex, true))
				.Concat(scriptPubKey.ToBytes(true))
				.ToArray();
			var hash = Hashes.Hash256(hashed);
			var bytes = hash.ToBytes(true);
			BitArray result = new BitArray(bitCount);
			for(int i = 0 ; i < bitCount ; i++)
			{
				int byteIndex = i / 8;
				int bitIndex = i % 8;
				result.Set(i, ((bytes[byteIndex] >> bitIndex) & 1) == 1);
			}
			return result;
		}

		//Step1: Determine the number of bits and encoding of blockHeight
		//blockHeight takes x bits and is encoded as follow:

		//	For height =< 1,048,575 (0-1111-1111-1111-1111-1111), blockHeight is the height as 21bit interger
		//	For 1,048,575 < height =< 8,388,607, blockHeight is Concat(1, height as 23 bit integer), which totally takes 24bit. For example, block 1234567 is 1001-0010-1101-0110-1000-0111
		//	For height > 8,388,607, it is undefined and returns error
		private static BitArray EncodeBlockHeight(int blockHeight)
		{
			if(blockHeight <= 1048575)
			{
				return Concat(new BitArray(new[] { false }), Encode(blockHeight, 20));
			}
			else if(1048575 < blockHeight && blockHeight <= 8388607)
			{
				return Concat(new BitArray(new[] { true }), Encode(blockHeight, 23));
			}
			else
			{
				throw new ArgumentOutOfRangeException("Impossible to reference an output after block 8,388,607");
			}
		}
		private static int DecodeBlockHeight(BitArray rawAddress, out int blockHeight)
		{
			if(!rawAddress.Get(0))
			{
				blockHeight = Decode(Substring(rawAddress, 1, 20));
				return 21;
			}
			else
			{
				blockHeight = Decode(Substring(rawAddress, 1, 23));
				return 24;
			}
		}

		private static BitArray Encode(int value, int bitCount)
		{
			var result = new BitArray(bitCount);
			for(int i = 0 ; i < bitCount ; i++)
			{
				result.Set(i, (((value >> i) & 1) == 1));
			}
			return result;
		}
		static string ToBitString(BitArray bits)
		{
			var sb = new StringBuilder();

			for(int i = 0 ; i < bits.Count ; i++)
			{
				char c = bits[i] ? '1' : '0';
				sb.Append(c);
			}

			return sb.ToString();
		}

		private static int Decode(BitArray array)
		{
			int result = 0;
			for(int i = 0 ; i < array.Length ; i++)
			{
				if(array.Get(i))
					result += 1 << i;
			}
			return result;
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
