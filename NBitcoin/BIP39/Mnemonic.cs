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
	public class Mnemonic
	{
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

		public Mnemonic(string mnemonic, Wordlist wordlist = null)
		{
			if(mnemonic == null)
				throw new ArgumentNullException("mnemonic");
			_Mnemonic = mnemonic.Trim();

			if(wordlist == null)
				wordlist = Wordlist.AutoDetect(mnemonic) ?? Wordlist.English;

			var words = wordlist.Split(mnemonic);
			//if the sentence is not at least 12 characters or cleanly divisible by 3, it is bad!
			if(words.Length < 12 || words.Length % 3 != 0)
			{
				throw new FormatException("Mnemonic sentence must be at least 12 words and it will increase by 3 words for each increment in entropy. Please ensure your sentence is at leas 12 words and has no remainder when word count is divided by 3");
			}
			_Words = words;
			_WordList = wordlist;
			_Indices = CreateIndicesArrayFromWords(words, wordlist);
		}

		private static int[] CreateIndicesArrayFromWords(string[] words, Wordlist wordList)
		{
			var indices = new int[words.Length];
			for(int i = 0 ; i < words.Length ; i++)
			{
				int idx = -1;

				if(!wordList.WordExists(words[i], out idx))
				{
					throw new FormatException("Word " + words[i] + " is not in the wordlist for this language, cannot continue to rebuild entropy from wordlist");
				}
				indices[i] = idx;
			}
			return indices;
		}

		public Mnemonic(string passphrase, Wordlist wordList, byte[] entropy = null)
		{
			passphrase = passphrase ?? "";
			wordList = wordList ?? Wordlist.English;
			_WordList = wordList;
			if(entropy == null)
				entropy = RandomUtils.GetBytes(32);




			byte[] allChecksumBytes = Hashes.SHA256(entropy); //sha256 the entropy bytes to get all the checksum bits

			int numberOfChecksumBits = (entropy.Length * 8) / 32; //number of bits to take from the checksum bits, varies on entropy size as per spec
			BitArray entropyConcatChecksumBits = new BitArray((entropy.Length * 8) + numberOfChecksumBits);

			allChecksumBytes = SwapEndianBytes(allChecksumBytes); //yet another endianess change of some different bytes to match the test vectors.....             

			entropy = SwapEndianBytes(entropy);

			int index = 0;

			foreach(bool b in new BitArray(entropy))
			{
				entropyConcatChecksumBits.Set(index, b);
				index++;
			}

			/*sooooo I'm doing below for future proofing....I know right now we are using up to 256 bits entropy in real world implementation and therefore max 8 bits (1 byte) of checksum....buuuut I figgure it's easy enough
			to accomodate more entropy by chaining more checksum bytes so maximum 256 * 32 = 8192 theoretical maximum entropy (plus CS).*/
			List<byte> checksumBytesToUse = new List<byte>();

			double byteCount = Math.Ceiling((double)numberOfChecksumBits / 8);

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


			List<int> wordIndexList = new List<int>();

			//yea....loop in a loop....what of it!!! Outer loop is segregating bits into 11 bit groups and the inner loop is processing the 11 bits before sending them to be encoded as an int.
			for(int i = 0 ; i < entropyConcatChecksumBits.Length ; i = i + 11)
			{
				BitArray toInt = new BitArray(11);
				for(int i2 = 0 ; i2 < 11 && i < entropyConcatChecksumBits.Length ; i2++)
				{
					toInt.Set(i2, entropyConcatChecksumBits.Get(i + i2));
				}

				wordIndexList.Add(ToInt(toInt)); //adding encoded int to word index               
			}

			_Indices = wordIndexList.ToArray();
			_Words = new string[_Indices.Length];

			StringBuilder builder = new StringBuilder();
			for(int i = 0 ; i < _Indices.Length ; i++)
			{
				var word = _WordList.GetWordAtIndex(_Indices[i]);
				_Words[i] = word;
				builder.Append(word);
				if(i + 1 < _Indices.Length)
				{
					builder.Append(_WordList.Space);
				}
			}
			_Mnemonic = builder.ToString();
		}

		private int ToInt(BitArray bits)
		{
			if(bits.Length != 11)
			{
				throw new InvalidOperationException("should never happen, bug in nbitcoin");
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

		private readonly Wordlist _WordList;
		public Wordlist WordList
		{
			get
			{
				return _WordList;
			}
		}

		private readonly int[] _Indices;
		public int[] Indices
		{
			get
			{
				return _Indices;
			}
		}
		private readonly string[] _Words;
		public string[] Words
		{
			get
			{
				return _Words;
			}
		}

		public byte[] GetSeed(string passphrase)
		{
			var salt = Concat(UTF8Encoding.UTF8.GetBytes("mnemonic"), UTF8Encoding.UTF8.GetBytes(passphrase.Normalize(NormalizationForm.FormKD)));
			var bytes = Encoding.UTF8.GetBytes(_Mnemonic.Normalize(NormalizationForm.FormKD));
			return Pbkdf2.ComputeDerivedKey(new HMACSHA512(bytes), salt, 2048, 64);
		}
		public ExtKey GetExtKey(string passphrase)
		{
			return new ExtKey(GetSeed(passphrase));
		}

		static Byte[] Concat(Byte[] source1, Byte[] source2)
		{
			//Most efficient way to merge two arrays this according to http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
			Byte[] buffer = new Byte[source1.Length + source2.Length];
			System.Buffer.BlockCopy(source1, 0, buffer, 0, source1.Length);
			System.Buffer.BlockCopy(source2, 0, buffer, source1.Length, source2.Length);

			return buffer;
		}


		string _Mnemonic;
		public override string ToString()
		{
			return _Mnemonic;
		}
	}
}