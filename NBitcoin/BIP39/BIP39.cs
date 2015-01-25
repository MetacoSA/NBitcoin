﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Crypto;
using System.Security.Cryptography;

namespace NBitcoin
{
	/// <summary>
	/// A .NET implementation of the Bitcoin Improvement Proposal - 39 (BIP39)
	/// BIP39 specification used as reference located here: https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki
	/// Made by thashiznets@yahoo.com.au
	/// v1.0.1.1
	/// I ♥ Bitcoin :)
	/// Bitcoin:1ETQjMkR1NNh4jwLuN5LxY7bMsHC9PUPSV
	/// </summary>
	public class BIP39
	{
		#region Private Attributes

		private Wordlist _wordList;
		private List<int> _wordIndexList; //I made this an attribute because then we can keep the same index and swap between languages for experimenting
		private string _mnemonicSentence;

		#endregion

		#region Public Constants and Enums

		public const int cMinimumEntropyBits = 256;
		public const int cMaximumEntropyBits = 8192;
		public const int cEntropyMultiple = 32;
		public const int cBitsInByte = 8;
		public const int cBitGroupSize = 11;
		public const string cEmptyString = "";
		public const string cSaltHeader = "mnemonic"; //this is the first part of the salt as described in the BIP39 spec

		public const string cJPSpaceString = "\u3000"; //ideographic space used by japanese language

		#endregion

		static byte[] SwapEndianBytes(byte[] bytes)
		{
			byte[] output = new byte[bytes.Length];

			int index = 0;

			foreach(byte b in bytes)
			{
				byte[] ba = { b };
				BitArray bits = new BitArray(ba);

				int newByte = 0;
				if(bits.Get(7))
					newByte++;
				if(bits.Get(6))
					newByte += 2;
				if(bits.Get(5))
					newByte += 4;
				if(bits.Get(4))
					newByte += 8;
				if(bits.Get(3))
					newByte += 16;
				if(bits.Get(2))
					newByte += 32;
				if(bits.Get(1))
					newByte += 64;
				if(bits.Get(0))
					newByte += 128;

				output[index] = Convert.ToByte(newByte);

				index++;
			}

			//I love lamp
			return output;
		}

		#region Constructors

		/// <summary>
		/// Constructor to build a BIP39 object from scratch given an entropy size and an optional passphrase. Language is optional and will default to English
		/// </summary>
		/// <param name="entropySize">The size in bits of the entropy to be created</param>
		/// <param name="passphrase">The optional passphrase. Please ensure NFKD Normalized, Empty string will be used if not provided as per spec</param>
		/// <param name="wordList">The optional language. If no language is provided English will be used</param>
		/// <param name="entropy">The entropy bytes which will determine the mnemonic sentence</param>
		public BIP39(string passphrase = cEmptyString, Wordlist wordList = null, byte[] entropy = null)
		{
			if(passphrase == null)
				passphrase = "";
			_passphrase = passphrase;
			pInit(passphrase, wordList, entropy);
		}

		/// <summary>
		/// Constructor to build a BIP39 object using a supplied Mnemonic sentence and passphrase. If you are not worried about saving the entropy bytes, or using custom words not in a wordlist, you should consider the static method to do this instead.
		/// </summary>
		/// <param name="mnemonicSentence">The mnemonic sentences used to derive seed bytes, Please ensure NFKD Normalized</param>
		/// <param name="passphrase">Optional passphrase used to protect seed bytes, defaults to empty</param>
		/// <param name="wordList">Optional language to use for wordlist, if not specified it will auto detect language and if it can't detect it will default to English</param>
		public BIP39(string mnemonicSentence, string passphrase = cEmptyString, Wordlist wordList = null)
		{
			if(passphrase == null)
				passphrase = "";
			_mnemonicSentence = mnemonicSentence.Trim(); //just making sure we don't have any leading or trailing spaces
			_passphrase = passphrase;
			string[] words = _mnemonicSentence.Split(new char[] { ' ', '　' }); //normal space and JP space

			//no language specified try auto detect it
			if(wordList == null)
			{
				wordList = Wordlist.LoadWordList(AutoDetectLanguageOfWords(words)).Result;

				if(wordList == null)
				{
					//yeah.....have a bias to use English as default....
					_wordList = Wordlist.LoadWordList(Language.English).Result;
				}
			}

			//if the sentence is not at least 12 characters or cleanly divisible by 3, it is bad!
			if(words.Length < 12 || words.Length % 3 != 0)
			{
				throw new Exception("Mnemonic sentence must be at least 12 words and it will increase by 3 words for each increment in entropy. Please ensure your sentence is at leas 12 words and has no remainder when word count is divided by 3");
			}

			_wordList = wordList;
			_wordIndexList = pRebuildWordIndexes(words);
		}

		#endregion

		#region Public Static Methods

		/// <summary>
		/// Takes in a string[] of words and detects the language that has the highest number of matching words.
		/// </summary>
		/// <param name="words">The words of which you wish to derive a language</param>
		/// <returns>The best attempt at a guessed Language</returns>
		public static Language AutoDetectLanguageOfWords(string[] words)
		{
			List<int> languageCount = new List<int>(new int[] { 0, 0, 0, 0, 0 });
			int index;

			foreach(string s in words)
			{
				if(Wordlist.English.WordExists(s, out index))
				{
					//english is at 0
					languageCount[0]++;
				}

				if(Wordlist.Japanese.WordExists(s, out index))
				{
					//japanese is at 1
					languageCount[1]++;
				}

				if(Wordlist.Spanish.WordExists(s, out index))
				{
					//spanish is at 2
					languageCount[2]++;
				}

				if(Wordlist.ChineseSimplified.WordExists(s, out index))
				{
					//chinese simplified is at 3
					languageCount[3]++;
				}

				if(Wordlist.ChineseTraditional.WordExists(s, out index) && !Wordlist.ChineseSimplified.WordExists(s, out index))
				{
					//chinese traditional is at 4
					languageCount[4]++;
				}
			}

			//no hits found for any language unknown
			if(languageCount.Max() == 0)
			{
				return Language.Unknown;
			}

			if(languageCount.IndexOf(languageCount.Max()) == 0)
			{
				return Language.English;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 1)
			{
				return Language.Japanese;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 2)
			{
				return Language.Spanish;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 3)
			{
				if(languageCount[4] > 0)
				{
					//has traditional characters so not simplified but instead traditional
					return Language.ChineseTraditional;
				}

				return Language.ChineseSimplified;
			}
			else if(languageCount.IndexOf(languageCount.Max()) == 4)
			{
				return Language.ChineseTraditional;
			}

			return Language.Unknown;
		}

		/// <summary>
		/// Merges two byte arrays
		/// </summary>
		/// <param name="source1">first byte array</param>
		/// <param name="source2">second byte array</param>
		/// <returns>A byte array which contains source1 bytes followed by source2 bytes</returns>
		static Byte[] Concat(Byte[] source1, Byte[] source2)
		{
			//Most efficient way to merge two arrays this according to http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
			Byte[] buffer = new Byte[source1.Length + source2.Length];
			System.Buffer.BlockCopy(source1, 0, buffer, 0, source1.Length);
			System.Buffer.BlockCopy(source2, 0, buffer, source1.Length, source2.Length);

			return buffer;
		}

		public static Byte[] PBKDF2(string mnemonic, string passphrase)
		{
			var salt = Concat(UTF8Encoding.UTF8.GetBytes(cSaltHeader), UTF8Encoding.UTF8.GetBytes(passphrase.Normalize(NormalizationForm.FormKD)));
			var bytes = Encoding.UTF8.GetBytes(mnemonic.Normalize(NormalizationForm.FormKD));
			return Pbkdf2.ComputeDerivedKey(new HMACSHA512(bytes), salt, 2048, 64);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Common initialisation code utilised by all the constructors. It gets all the bits and does a checksum etc. This is the main code to create a BIP39 object.
		/// </summary>
		private void pInit(String passphrase, Wordlist wordlist, byte[] entropyBytes)
		{
			if(entropyBytes == null)
				entropyBytes = RandomUtils.GetBytes(cMinimumEntropyBits / 8);

			_passphrase = passphrase;
			_wordList = wordlist;
			byte[] allChecksumBytes = Hashes.SHA256(entropyBytes); //sha256 the entropy bytes to get all the checksum bits

			int numberOfChecksumBits = (entropyBytes.Length * cBitsInByte) / cEntropyMultiple; //number of bits to take from the checksum bits, varies on entropy size as per spec
			BitArray entropyConcatChecksumBits = new BitArray((entropyBytes.Length * cBitsInByte) + numberOfChecksumBits);

			allChecksumBytes = SwapEndianBytes(allChecksumBytes); //yet another endianess change of some different bytes to match the test vectors.....             

			entropyBytes = SwapEndianBytes(entropyBytes);

			int index = 0;

			foreach(bool b in new BitArray(entropyBytes))
			{
				entropyConcatChecksumBits.Set(index, b);
				index++;
			}

			/*sooooo I'm doing below for future proofing....I know right now we are using up to 256 bits entropy in real world implementation and therefore max 8 bits (1 byte) of checksum....buuuut I figgure it's easy enough
			to accomodate more entropy by chaining more checksum bytes so maximum 256 * 32 = 8192 theoretical maximum entropy (plus CS).*/
			List<byte> checksumBytesToUse = new List<byte>();
			
			double byteCount = Math.Ceiling((double)numberOfChecksumBits / cBitsInByte);

			for(int i = 0 ; i < byteCount ; i++)
			{
				checksumBytesToUse.Add(allChecksumBytes[i]);
			}

			BitArray ba = new BitArray(checksumBytesToUse.ToArray());

			//add checksum bits
			for(int i = 0 ; i < numberOfChecksumBits ; i++)
			{
				entropyConcatChecksumBits.Set(index, ba.Get(i));
				index++;
			}

			_wordIndexList = pGetWordIndexes(entropyConcatChecksumBits);
			_mnemonicSentence = pGetMnemonicSentence();

		}

		/// <summary>
		/// Uses the Wordlist Index to create a scentence ow words provided by the wordlist of this objects language attribute
		/// </summary>
		/// <returns>A scentence of words</returns>
		private string pGetMnemonicSentence()
		{
			//trap for words that were not in the word list when built. If custom words were used, we will not support the rebuild as we don't have the words
			if(_wordIndexList.Contains(-1))
			{
				throw new Exception("the wordlist index contains -1 which means words were used in the mnemonic sentence that cannot be found in the wordlist and the index to sentence feature cannot be used. Perhaps a different language wordlist is needed?");
			}

			StringBuilder builder = new StringBuilder();
			for(int i = 0 ; i < _wordIndexList.Count ; i++)
			{
				builder.Append(_wordList.GetWordAtIndex(_wordIndexList[i]));
				if(i + 1 < _wordIndexList.Count)
				{
					builder.Append(" ");
				}
			}
			return builder.ToString();
		}

		/// <summary>
		/// Process entropy + CS into an index list of words to get from wordlist
		/// </summary>
		/// <returns>An index, each int is a line in the wiordlist for the language of choice</returns>
		private List<int> pGetWordIndexes(BitArray entropyConcatChecksumBits)
		{
			List<int> wordIndexList = new List<int>();

			//yea....loop in a loop....what of it!!! Outer loop is segregating bits into 11 bit groups and the inner loop is processing the 11 bits before sending them to be encoded as an int.
			for(int i = 0 ; i < entropyConcatChecksumBits.Length ; i = i + cBitGroupSize)
			{
				BitArray toInt = new BitArray(cBitGroupSize);
				for(int i2 = 0 ; i2 < cBitGroupSize && i < entropyConcatChecksumBits.Length ; i2++)
				{
					toInt.Set(i2, entropyConcatChecksumBits.Get(i + i2));
				}

				wordIndexList.Add(pProcessBitsToInt(toInt)); //adding encoded int to word index               
			}

			return wordIndexList;
		}

		/// <summary>
		/// Takes in the words of a mnemonic sentence and it rebuilds the word index, having the valid index allows us to hot swap between languages/word lists :)
		/// </summary>
		/// <param name="wordsInMnemonicSentence"> a string array containing each word in the mnemonic sentence</param>
		/// <returns>The word index that can be used to build the mnemonic sentence</returns>
		private List<int> pRebuildWordIndexes(string[] wordsInMnemonicSentence)
		{
			List<int> wordIndexList = new List<int>();
			foreach(string s in wordsInMnemonicSentence)
			{
				int idx = -1;

				if(!_wordList.WordExists(s, out idx))
				{
					throw new Exception("Word " + s + " is not in the wordlist for this language, cannot continue to rebuild entropy from wordlist");
				}

				wordIndexList.Add(idx);
			}

			return wordIndexList;
		}

		/// <summary>
		/// Me encoding an integer between 0 and 2047 from 11 bits...
		/// </summary>
		/// <param name="bits">The bits to encode into an integer</param>
		/// <returns>integer between 0 and 2047</returns>
		private int pProcessBitsToInt(BitArray bits)
		{

			if(bits.Length != cBitGroupSize)
			{
				//to do throw not 11 bits exception
			}

			int number = 0;
			int base2Divide = 1024; //it's all downhill from here...literally we halve this for each bit we move to.

			//literally picture this loop as going from the most significant bit across to the least in the 11 bits, dividing by 2 for each bit as per binary/base 2
			foreach(bool b in bits)
			{
				if(b)
				{
					number = number + base2Divide;
				}

				base2Divide = base2Divide / 2;
			}

			return number;
		}

		#endregion

		#region Properties

		string _passphrase;
		/// <summary>
		/// Sets the pasphrase, this lets us use the same entropy bits to derive many seeds based on different passphrases
		/// </summary>
		public string Passphrase
		{
			get
			{
				return _passphrase;
			}
		}

		/// <summary>
		/// Gets the mnemonic sentence built from ent+cs
		/// </summary>
		public string MnemonicSentence
		{
			get
			{
				return _mnemonicSentence;
			}
		}

		/// <summary>
		/// Gets or Sets the language that will be used to provide the mnemonic sentence, WARNING ensure you get new seed bytes after setting language
		/// </summary>
		public Wordlist Wordlist
		{
			get
			{
				return _wordList;
			}
		}


		byte[] _SeedBytes;
		/// <summary>
		/// Gets the bytes of the seed created from the mnemonic sentence. This could become your root in BIP32
		/// </summary>
		public byte[] SeedBytes
		{
			get
			{
				if(_SeedBytes == null)
				{
					_SeedBytes = PBKDF2(MnemonicSentence, Passphrase);
				}
				return _SeedBytes;
			}
		}

		#endregion

		ExtKey _ExtKey;
		public ExtKey ExtKey
		{
			get
			{
				if(_ExtKey == null)
					_ExtKey = new ExtKey(SeedBytes);
				return _ExtKey;
			}
		}
	}
}