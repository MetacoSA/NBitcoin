﻿
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Crypto;
#if !WINDOWS_UWP && !USEBC
using System.Security.Cryptography;
#endif
using NBitcoin.BouncyCastle.Security;
using NBitcoin.BouncyCastle.Crypto.Parameters;

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
		public Mnemonic(string mnemonic, Wordlist wordlist = null)
		{
			if(mnemonic == null)
				throw new ArgumentNullException(nameof(mnemonic));
			_Mnemonic = mnemonic.Trim();

			if(wordlist == null)
				wordlist = Wordlist.AutoDetect(mnemonic) ?? Wordlist.English;

			var words = mnemonic.Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
			//if the sentence is not at least 12 characters or cleanly divisible by 3, it is bad!
			if(!CorrectWordCount(words.Length))
			{
				throw new FormatException("Word count should be 12,15,18,21 or 24");
			}
			_Words = words;
			_WordList = wordlist;
			_Indices = wordlist.ToIndices(words);
		}

		/// <summary>
		/// Generate a mnemonic
		/// </summary>
		/// <param name="wordList"></param>
		/// <param name="entropy"></param>
		public Mnemonic(Wordlist wordList, byte[] entropy = null)
		{
			wordList = wordList ?? Wordlist.English;
			_WordList = wordList;
			if(entropy == null)
				entropy = RandomUtils.GetBytes(32);

			var i = Array.IndexOf(entArray, entropy.Length * 8);
			if(i == -1)
				throw new ArgumentException("The length for entropy should be : " + String.Join(",", entArray), "entropy");

			int cs = csArray[i];
			byte[] checksum = Hashes.SHA256(entropy);
			BitWriter entcsResult = new BitWriter();

			entcsResult.Write(entropy);
			entcsResult.Write(checksum, cs);
			_Indices = entcsResult.ToIntegers();
			_Words = _WordList.GetWords(_Indices);
			_Mnemonic = _WordList.GetSentence(_Indices);
		}

		public Mnemonic(Wordlist wordList, WordCount wordCount)
			: this(wordList, GenerateEntropy(wordCount))
		{

		}

		private static byte[] GenerateEntropy(WordCount wordCount)
		{
			var ms = (int)wordCount;
			if(!CorrectWordCount(ms))
				throw new ArgumentException("Word count should be 12,15,18,21 or 24", "wordCount");
			int i = Array.IndexOf(msArray, (int)wordCount);
			return RandomUtils.GetBytes(entArray[i] / 8);
		}

		static readonly int[] msArray = new[] { 12, 15, 18, 21, 24 };
		static readonly int[] csArray = new[] { 4, 5, 6, 7, 8 };
		static readonly int[] entArray = new[] { 128, 160, 192, 224, 256 };

		bool? _IsValidChecksum;
		public bool IsValidChecksum
		{
			get
			{
				if(_IsValidChecksum == null)
				{
					int i = Array.IndexOf(msArray, _Indices.Length);
					int cs = csArray[i];
					int ent = entArray[i];

					BitWriter writer = new BitWriter();
					var bits = Wordlist.ToBits(_Indices);
					writer.Write(bits, ent);
					var entropy = writer.ToBytes();
					var checksum = Hashes.SHA256(entropy);

					writer.Write(checksum, cs);
					var expectedIndices = writer.ToIntegers();
					_IsValidChecksum = expectedIndices.SequenceEqual(_Indices);
				}
				return _IsValidChecksum.Value;
			}
		}

		private static bool CorrectWordCount(int ms)
		{
			return msArray.Any(_ => _ == ms);
		}


		// FIXME: this method is not used. Shouldn't we delete it?
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

		static Encoding NoBOMUTF8 = new UTF8Encoding(false);
		public byte[] DeriveSeed(string passphrase = null)
		{
			passphrase = passphrase ?? "";
			var salt = Concat(NoBOMUTF8.GetBytes("mnemonic"), Normalize(passphrase));
			var bytes = Normalize(_Mnemonic);

#if USEBC || WINDOWS_UWP || NETCORE
			var mac = new NBitcoin.BouncyCastle.Crypto.Macs.HMac(new NBitcoin.BouncyCastle.Crypto.Digests.Sha512Digest());
			mac.Init(new KeyParameter(bytes));
			return Pbkdf2.ComputeDerivedKey(mac, salt, 2048, 64);
#else
			return Pbkdf2.ComputeDerivedKey(new HMACSHA512(bytes), salt, 2048, 64);
#endif

		}

		internal static byte[] Normalize(string str)
		{
			return NoBOMUTF8.GetBytes(NormalizeString(str));
		}

		internal static string NormalizeString(string word)
		{
#if !NOSTRNORMALIZE
			if(!SupportOsNormalization())
			{
				return KDTable.NormalizeKD(word);
			}
			else
			{
				return word.Normalize(NormalizationForm.FormKD);
			}
#else
			return KDTable.NormalizeKD(word);
#endif
		}

#if !NOSTRNORMALIZE
		static bool? _SupportOSNormalization;
		internal static bool SupportOsNormalization()
		{
			if(_SupportOSNormalization == null)
			{
				var notNormalized = "あおぞら";
				var normalized = "あおぞら";
				if(notNormalized.Equals(normalized, StringComparison.Ordinal))
				{
					_SupportOSNormalization = false;
				}
				else
				{
					try
					{
						_SupportOSNormalization = notNormalized.Normalize(NormalizationForm.FormKD).Equals(normalized, StringComparison.Ordinal);
					}
					catch { _SupportOSNormalization = false; }
				}
			}
			return _SupportOSNormalization.Value;
		}
#endif

		public ExtKey DeriveExtKey(string passphrase = null)
		{
			return new ExtKey(DeriveSeed(passphrase));
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
	public enum WordCount : int
	{
		Twelve = 12,
		Fifteen = 15,
		Eighteen = 18,
		TwentyOne = 21,
		TwentyFour = 24
	}
}
